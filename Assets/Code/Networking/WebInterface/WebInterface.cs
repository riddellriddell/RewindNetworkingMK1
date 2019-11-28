using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// This class pull and pushes data exposed by the external web api or the local FakeWebAPI
/// </summary>
namespace Networking
{
    public class WebInterface
    {
        public struct WebAPICommunicationTracker
        {
            public enum CommunctionStatus
            {
                NotStarted,
                InProgress,
                Failed,
                Succedded,
                Cancled
            }

            public CommunctionStatus m_cmsStatus;
            public DateTime m_dtmTimeOfLastCommunication;
            public int m_iCommunicationAttemptNumber;
            public int m_iCommunicationMaxAttemptNumber;

            //sets up the default inital web tracker
            public static WebAPICommunicationTracker StartState(int iRetryAttempts)
            {

                WebAPICommunicationTracker wctCommunicationTracker = new WebAPICommunicationTracker()
                {
                    m_dtmTimeOfLastCommunication = DateTime.UtcNow,
                    m_cmsStatus = CommunctionStatus.NotStarted,
                    m_iCommunicationAttemptNumber = 0,
                    m_iCommunicationMaxAttemptNumber = iRetryAttempts

                };

                return wctCommunicationTracker;

            }

            public float TimeSinceLastCommunication()
            {
                if (m_cmsStatus == CommunctionStatus.NotStarted)
                {
                    return 0;
                }

                TimeSpan tspTimeSinceLastComs = DateTime.UtcNow - m_dtmTimeOfLastCommunication;

                return (float)tspTimeSinceLastComs.TotalSeconds;
            }

            public WebAPICommunicationTracker StartNewAttempt()
            {
                WebAPICommunicationTracker wctNewState = new WebAPICommunicationTracker()
                {
                    m_dtmTimeOfLastCommunication = DateTime.UtcNow,
                    m_iCommunicationAttemptNumber = this.m_iCommunicationAttemptNumber + 1,
                    m_cmsStatus = CommunctionStatus.InProgress,
                    m_iCommunicationMaxAttemptNumber = this.m_iCommunicationMaxAttemptNumber
                };

                return wctNewState;
            }

            public WebAPICommunicationTracker CommunicationFailed()
            {
                WebAPICommunicationTracker wctNewState = new WebAPICommunicationTracker()
                {
                    m_dtmTimeOfLastCommunication = DateTime.UtcNow,
                    m_iCommunicationAttemptNumber = this.m_iCommunicationAttemptNumber,
                    m_cmsStatus = CommunctionStatus.Failed,
                    m_iCommunicationMaxAttemptNumber = this.m_iCommunicationMaxAttemptNumber
                };

                return wctNewState;
            }

            public WebAPICommunicationTracker CommunicationSuccessfull()
            {
                WebAPICommunicationTracker wctNewState = new WebAPICommunicationTracker()
                {
                    m_dtmTimeOfLastCommunication = DateTime.UtcNow,
                    m_iCommunicationAttemptNumber = this.m_iCommunicationAttemptNumber,
                    m_cmsStatus = CommunctionStatus.Succedded,
                    m_iCommunicationMaxAttemptNumber = this.m_iCommunicationMaxAttemptNumber
                };

                return wctNewState;
            }

            public WebAPICommunicationTracker Reset()
            {
                WebAPICommunicationTracker wctNewState = new WebAPICommunicationTracker()
                {
                    m_dtmTimeOfLastCommunication = DateTime.UtcNow,
                    m_iCommunicationAttemptNumber = 0,
                    m_cmsStatus = CommunctionStatus.NotStarted,
                    m_iCommunicationMaxAttemptNumber = this.m_iCommunicationMaxAttemptNumber
                };

                return wctNewState;
            }

            public WebAPICommunicationTracker Cancel()
            {
                WebAPICommunicationTracker wctNewState = new WebAPICommunicationTracker()
                {
                    m_dtmTimeOfLastCommunication = DateTime.UtcNow,
                    m_iCommunicationAttemptNumber = 0,
                    m_cmsStatus = CommunctionStatus.Cancled,
                    m_iCommunicationMaxAttemptNumber = this.m_iCommunicationMaxAttemptNumber
                };

                return wctNewState;
            }

            public bool ShouldRestart()
            {
                if (m_cmsStatus == CommunctionStatus.Failed)
                {
                    if (m_iCommunicationMaxAttemptNumber <= m_iCommunicationAttemptNumber)
                    {
                        return false;
                    }

                    return true;
                }

                return false;
            }
        }

        //the id of the player
        public long PlayerID { get; private set; }
        public bool PlayerIDAlreadyExistsOnServer { get; private set; } = true;
        public WebAPICommunicationTracker PlayerIDCommunicationStatus { get; private set; } = WebAPICommunicationTracker.StartState(5);

        //all the messages fetched from the server
        public Queue<UserMessage> MessagesFromServer { get; } = new Queue<UserMessage>();
        public WebAPICommunicationTracker MessageFetchStatus { get; private set; } = WebAPICommunicationTracker.StartState(5);
        public float TimeBetweenMessageUpdates { get; } = 2;

        protected Queue<SendMessageCommand> MessagesToSend { get; } = new Queue<SendMessageCommand>();
        public int MessageSendQueueCount
        {
            get
            {
                return MessagesToSend.Count;
            }
        }
        public WebAPICommunicationTracker MessageSendStatus { get; private set; } = WebAPICommunicationTracker.StartState(5);
        public float TimeBetweenMessageSendAttempts { get; } = 3;

        public SetGatewayCommand LocalGatewaySimStatus { get; private set; }
        public WebAPICommunicationTracker SetGatewayStatus { get; private set; } = WebAPICommunicationTracker.StartState(5);
        public float TimeBetweenGatewayUpdates { get; } = 5;

        public Gateway? ExternalGateway { get; private set; }
        public WebAPICommunicationTracker ExternalGatewayCommunicationStatus { get; private set; } = WebAPICommunicationTracker.StartState(5);

        protected string m_strUniqueDeviceIdentifier = string.Empty;

        public void UpdateCommunication()
        {
            //check if get player ID needs restarting
            if (PlayerIDCommunicationStatus.ShouldRestart())
            {
                //check what sort of action should be taken
                if (PlayerIDAlreadyExistsOnServer)
                {
                    //restart get player id
                    InternalStartGetPlayerID();
                }
                else
                {
                    //restart getting player id
                    InternalStartCreatePlayerID();
                }
            }

            //check if message fetch needs updating 
            if (
                MessageFetchStatus.m_cmsStatus != WebAPICommunicationTracker.CommunctionStatus.NotStarted ||
                MessageFetchStatus.m_cmsStatus != WebAPICommunicationTracker.CommunctionStatus.Cancled)
            {
                //check if it should be restarted
                if (MessageFetchStatus.ShouldRestart())
                {
                    InternalStartGettingMessagesFromServer();
                }
                else if (MessageFetchStatus.TimeSinceLastCommunication() > TimeBetweenMessageUpdates)
                {
                    MessageFetchStatus = MessageFetchStatus.Reset();

                    InternalStartGettingMessagesFromServer();
                }
            }

            //check if there are messages to send and a message is not currently being sent and message sending has not been canceled
            if (
                (MessageSendStatus.m_cmsStatus == WebAPICommunicationTracker.CommunctionStatus.Succedded ||
                MessageSendStatus.m_cmsStatus == WebAPICommunicationTracker.CommunctionStatus.NotStarted) &&
                MessagesToSend.Count > 0)
            {
                //get the next message to send
                SendMessageCommand smcMessageCommand = MessagesToSend.Peek();

                MessageSendStatus = MessageSendStatus.Reset().StartNewAttempt();
                InternalStartSendingMessage(smcMessageCommand);
            }
            else if (
                MessagesToSend.Count > 0 &&
                MessageSendStatus.m_cmsStatus == WebAPICommunicationTracker.CommunctionStatus.Failed
                )
            {
                //get the next message to send
                SendMessageCommand smcMessageCommand = MessagesToSend.Peek();

                if (MessageSendStatus.ShouldRestart())
                {
                    MessageSendStatus = MessageSendStatus.StartNewAttempt();
                    InternalStartSendingMessage(smcMessageCommand);
                }
                else if (MessageSendStatus.TimeSinceLastCommunication() > TimeBetweenMessageSendAttempts)
                {
                    MessageSendStatus = MessageSendStatus.Reset().StartNewAttempt();
                    InternalStartSendingMessage(smcMessageCommand);
                }
            }

            //check if gateway needs updating 
            if (SetGatewayStatus.ShouldRestart())
            {
                SetGatewayStatus = SetGatewayStatus.StartNewAttempt();
                InternalStartSetGateway();
            }
            else if (
                (SetGatewayStatus.m_cmsStatus != WebAPICommunicationTracker.CommunctionStatus.NotStarted ||
                SetGatewayStatus.m_cmsStatus != WebAPICommunicationTracker.CommunctionStatus.Cancled) &&
                SetGatewayStatus.TimeSinceLastCommunication() > TimeBetweenGatewayUpdates)
            {
                SetGatewayStatus = SetGatewayStatus.Reset().StartNewAttempt();
                InternalStartSetGateway();
            }

            //check if gateway serch needs restarting
            if (ExternalGatewayCommunicationStatus.ShouldRestart())
            {
                InternalStartSearchForGateway();
            }

        }

        //get the players ID
        public bool GetPlayerID(string strUniqueDeviceID = "")
        {
            if (strUniqueDeviceID != string.Empty)
            {
                m_strUniqueDeviceIdentifier = strUniqueDeviceID;
            }
            else
            {
                m_strUniqueDeviceIdentifier = SystemInfo.deviceUniqueIdentifier;
            }

            //check that another process is not already doing somehting with the player id 
            if (PlayerIDCommunicationStatus.m_cmsStatus == WebAPICommunicationTracker.CommunctionStatus.InProgress)
            {
                return false;
            }

            InternalStartGetPlayerID();

            return true;
        }

        //get any sent or recieved messages
        public bool StartGettingMessages()
        {
            //check that user id has been found
            if (PlayerIDCommunicationStatus.m_cmsStatus != WebAPICommunicationTracker.CommunctionStatus.Succedded)
            {
                return false;
            }

            InternalStartGettingMessagesFromServer();

            return true;
        }

        //stop getting messages from the server
        public void StopGettingMessages()
        {
            MessageFetchStatus = MessageFetchStatus.Cancel();
        }

        //send message 
        public bool SendMessage(long lTarget, int iMessageType, string strMessage)
        {
            //check if local id found
            if (PlayerIDCommunicationStatus.m_cmsStatus != WebAPICommunicationTracker.CommunctionStatus.Succedded)
            {
                //dont have a local player id and cant send messages 
                return false;
            }

            //create message command
            SendMessageCommand smcMessageCommand = new SendMessageCommand()
            {
                m_iType = iMessageType,
                m_lFromID = PlayerID,
                m_lToID = lTarget,
                m_strMessage = strMessage
            };

            //add to message queue
            MessagesToSend.Enqueue(smcMessageCommand);

            //check if message sending should be restarted 
            if (MessageSendStatus.m_cmsStatus == WebAPICommunicationTracker.CommunctionStatus.Cancled ||
                MessageSendStatus.ShouldRestart() == false)

            {
                MessageSendStatus = MessageSendStatus.Reset();
            }

            return true;
        }

        //create gateway
        public bool SetGateway(SimStatus stsSimStatus)
        {
            //check if has id
            if (PlayerIDCommunicationStatus.m_cmsStatus != WebAPICommunicationTracker.CommunctionStatus.Succedded)
            {
                return false;
            }

            LocalGatewaySimStatus = new SetGatewayCommand()
            {
                m_sstStatus = stsSimStatus,
                m_lOwningPlayerId = PlayerID
            };

            //check if communication needs starting
            if (SetGatewayStatus.m_cmsStatus == WebAPICommunicationTracker.CommunctionStatus.NotStarted ||
                SetGatewayStatus.m_cmsStatus == WebAPICommunicationTracker.CommunctionStatus.Cancled)
            {
                SetGatewayStatus = SetGatewayStatus.Reset().StartNewAttempt();
                InternalStartSetGateway();
            }

            return true;

        }

        //stop running a gateway for people to connect through 
        public void CloseGateway()
        {
            SetGatewayStatus = SetGatewayStatus.CommunicationFailed();
        }

        /// <summary>
        /// Search for gateway returns false if already looking for gateway or player does not have an ID
        /// </summary>
        /// <returns></returns>
        public bool SearchForGateway()
        {
            //check if has id
            if (PlayerIDCommunicationStatus.m_cmsStatus != WebAPICommunicationTracker.CommunctionStatus.Succedded)
            {
                return false;
            }

            //check if already searching for a gateway 
            if (
                ExternalGatewayCommunicationStatus.m_cmsStatus != WebAPICommunicationTracker.CommunctionStatus.NotStarted &&
                ExternalGatewayCommunicationStatus.m_cmsStatus != WebAPICommunicationTracker.CommunctionStatus.Failed &&
                ExternalGatewayCommunicationStatus.m_cmsStatus != WebAPICommunicationTracker.CommunctionStatus.Cancled)
            {
                return false;
            }

            ExternalGatewayCommunicationStatus = ExternalGatewayCommunicationStatus.Reset();

            InternalStartSearchForGateway();

            return true;

        }

        protected void InternalStartGetPlayerID()
        {
            PlayerIDCommunicationStatus = PlayerIDCommunicationStatus.StartNewAttempt();
            FakeWebAPI.Instance.GetUserWithLoginCredentials(m_strUniqueDeviceIdentifier, InternalOnFinishGetPlayerID);
        }

        protected void InternalOnFinishGetPlayerID(bool bWasSuccess, string strResult)
        {
            //check if communication was canceled 
            if (PlayerIDCommunicationStatus.m_cmsStatus == WebAPICommunicationTracker.CommunctionStatus.Cancled)
            {
                //dont apply changes to local state
                return;
            }

            //check if it was a success 
            if (bWasSuccess == false)
            {
                //check what kind of error it was

                //check if user does not exist
                if (strResult.Contains("404"))
                {
                    PlayerIDAlreadyExistsOnServer = false;
                }

                //mark coms attempt as failed
                PlayerIDCommunicationStatus = PlayerIDCommunicationStatus.CommunicationFailed();

                return;
            }

            //try and decode response
            try
            {

                if (long.TryParse(strResult, out long lPlayerIDFromServer))
                {
                    PlayerID = lPlayerIDFromServer;

                    //mark coms attempt as failed
                    PlayerIDCommunicationStatus = PlayerIDCommunicationStatus.CommunicationSuccessfull();

                    return;

                }
                else
                {
                    //check error type

                    Debug.LogError($"Failed to decode json string : {strResult} int player id from server");

                    //mark coms attempt as failed
                    PlayerIDCommunicationStatus = PlayerIDCommunicationStatus.CommunicationFailed();

                    return;
                }
            }
            catch
            {
                Debug.LogError($"Failed to decode json string : {strResult} int player id from server");

                //mark coms attempt as failed
                PlayerIDCommunicationStatus = PlayerIDCommunicationStatus.CommunicationFailed();

                return;
            }
        }

        protected void InternalStartCreatePlayerID()
        {
            PlayerIDCommunicationStatus = PlayerIDCommunicationStatus.StartNewAttempt();

            FakeWebAPI.Instance.CreateUserWithLoginCredentials(m_strUniqueDeviceIdentifier, InternalOnFinishCreatePlayerID);
        }

        protected void InternalOnFinishCreatePlayerID(bool bWasSuccess, string strResult)
        {
            //check if communication was canceled 
            if (PlayerIDCommunicationStatus.m_cmsStatus == WebAPICommunicationTracker.CommunctionStatus.Cancled)
            {
                //dont apply changes to local state
                return;
            }

            //check if it was a success 
            if (bWasSuccess == false)
            {

                //check if user already exist
                if (strResult.Contains("403"))
                {
                    PlayerIDAlreadyExistsOnServer = true;
                }

                //mark coms attempt as failed
                PlayerIDCommunicationStatus = PlayerIDCommunicationStatus.CommunicationFailed();

                return;
            }

            //try and decode response
            try
            {

                if (long.TryParse(strResult, out long lPlayerIDFromServer))
                {
                    PlayerID = lPlayerIDFromServer;

                    //mark coms attempt as failed
                    PlayerIDCommunicationStatus = PlayerIDCommunicationStatus.CommunicationSuccessfull();

                    return;

                }
                else
                {
                    //check error type

                    Debug.LogError($"Failed to decode json string : {strResult} int player id from server");

                    //mark coms attempt as failed
                    PlayerIDCommunicationStatus = PlayerIDCommunicationStatus.CommunicationFailed();

                    return;
                }
            }
            catch
            {
                Debug.LogError($"Failed to decode json string : {strResult} int player id from server");

                //mark coms attempt as failed
                PlayerIDCommunicationStatus = PlayerIDCommunicationStatus.CommunicationFailed();

                return;
            }

        }

        protected void InternalStartGettingMessagesFromServer()
        {
            //start next attempt
            MessageFetchStatus = MessageFetchStatus.StartNewAttempt();

            FakeWebAPI.Instance.GetDeleteUserMessages(PlayerID.ToString(), InternalOnFinishGettingMessagesFromServer);

        }

        protected void InternalOnFinishGettingMessagesFromServer(bool bWasSuccess, string strResult)
        {
            //check for cancel 
            if (MessageFetchStatus.m_cmsStatus == WebAPICommunicationTracker.CommunctionStatus.Cancled)
            {
                return;
            }

            //check if failed
            if (bWasSuccess == false)
            {
                MessageFetchStatus = MessageFetchStatus.CommunicationFailed();

                return;
            }

            //try to decode the message 
            try
            {
                GetMessageReturn gmrMessageReturn = JsonUtility.FromJson<GetMessageReturn>(strResult);

                List<UserMessage> mesMessages = new List<UserMessage>(gmrMessageReturn.m_usmUserMessages);

                mesMessages = mesMessages.OrderBy(x => x.m_lTimeOfMessage).ToList();

                //add new messages to message list
                foreach (UserMessage mesMessage in mesMessages)
                {
                    MessagesFromServer.Enqueue(mesMessage);
                }

            }
            catch
            {
                MessageFetchStatus = MessageFetchStatus.CommunicationFailed();

                return;
            }
        }

        protected void InternalStartSendingMessage(SendMessageCommand smcMessage)
        {
            MessageSendStatus = MessageSendStatus.StartNewAttempt();

            string strMessageDetails = JsonUtility.ToJson(smcMessage);
            FakeWebAPI.Instance.AddNewMessage(strMessageDetails, InternalOnFinishSendingMessage);
        }

        protected void InternalOnFinishSendingMessage(bool bWasSuccess, string strResult)
        {
            if (MessageSendStatus.m_cmsStatus == WebAPICommunicationTracker.CommunctionStatus.Cancled)
            {
                return;
            }

            if (bWasSuccess == false)
            {
                MessageSendStatus = MessageSendStatus.CommunicationFailed();

                return;
            }

            //remove the sent message from the queue
            MessagesToSend.Dequeue();

            MessageSendStatus = MessageSendStatus.CommunicationSuccessfull();
        }

        protected void InternalStartSetGateway()
        {
            SetGatewayStatus = SetGatewayStatus.StartNewAttempt();

            string strGatewayCommand = JsonUtility.ToJson(LocalGatewaySimStatus);

            FakeWebAPI.Instance.SetGateway(strGatewayCommand, InternalOnFinishCreateGateway);
        }

        protected void InternalOnFinishCreateGateway(bool bWasSuccess, string strResult)
        {
            if (SetGatewayStatus.m_cmsStatus == WebAPICommunicationTracker.CommunctionStatus.Cancled)
            {
                return;
            }

            if (bWasSuccess)
            {
                SetGatewayStatus = SetGatewayStatus.CommunicationSuccessfull();
            }
            else
            {
                SetGatewayStatus = SetGatewayStatus.CommunicationFailed();
            }
        }

        protected void InternalStartSearchForGateway()
        {
            ExternalGatewayCommunicationStatus = ExternalGatewayCommunicationStatus.StartNewAttempt();

            FakeWebAPI.Instance.SearchForGateway(PlayerID.ToString(), InternalOnFinishSearchForGateway);
        }

        protected void InternalOnFinishSearchForGateway(bool bWasSuccess, string strResult)
        {
            if (ExternalGatewayCommunicationStatus.m_cmsStatus == WebAPICommunicationTracker.CommunctionStatus.Cancled)
            {
                return;
            }

            if (bWasSuccess == false)
            {
                ExternalGatewayCommunicationStatus = ExternalGatewayCommunicationStatus.CommunicationFailed();
            }

            try
            {
                //decode external gate
                ExternalGateway = JsonUtility.FromJson<Gateway>(strResult);

                ExternalGatewayCommunicationStatus = ExternalGatewayCommunicationStatus.CommunicationSuccessfull();
            }
            catch
            {
                ExternalGatewayCommunicationStatus = ExternalGatewayCommunicationStatus.CommunicationFailed();
            }
        }
    }
}
