using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Security.Cryptography;
using System.Text;
using UnityEngine.Networking;
using System.Collections;

/// <summary>
/// This class pull and pushes data exposed by the external web api or the local FakeWebAPI
/// </summary>
namespace Networking
{
    public class WebInterface
    {
        //the web addresses for the web rest api
        public static string s_strGetIDWithUDUDAddress = "https://us-central1-rollbacknetworkingprototype.cloudfunctions.net/GetPeerIDForUDID";
        public static string s_strSendMessageAddress = "https://us-central1-rollbacknetworkingprototype.cloudfunctions.net/SendMessageToPeer";
        public static string s_strGetMessageAddress = "https://us-central1-rollbacknetworkingprototype.cloudfunctions.net/GetMessagesForPeer";
        public static string s_strSetGatewayAddress = "https://us-central1-rollbacknetworkingprototype.cloudfunctions.net/SetGateway";
        public static string s_strGetGatewayAddress = "https://us-central1-rollbacknetworkingprototype.cloudfunctions.net/GetGateway";
        public static string s_strGetGatewayListAddress = "https://us-central1-rollbacknetworkingprototype.cloudfunctions.net/GetGatewayList";

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

        //should use the actual internet server or simulate locally 
        public bool TestLocally { get; set; } = false;

        //the id of the player
        public long UserID { get; private set; }

        //the secret access key used to verify user with the server
        public long UserKey { get; private set; }

        //when running coroutunes this is the object the routines will be run off
        public MonoBehaviour CoroutineExecutionObject { get; private set; }

        public WebAPICommunicationTracker PlayerIDCommunicationStatus { get; private set; } = WebAPICommunicationTracker.StartState(3);

        //all the messages fetched from the server
        public Queue<UserMessage> MessagesFromServer { get; } = new Queue<UserMessage>();
        public WebAPICommunicationTracker MessageFetchStatus { get; private set; } = WebAPICommunicationTracker.StartState(3);
        public float MaxTimeBetweenMessageUpdates { get; } = 3;
        public float MinTimeBetweenMessageUpdates { get; } = 0.1f;
        public float TimeBetweenMessageUpdatesCooldownTime { get; } = 6.0f;

        public DateTime TimeOfLastMessage { get; private set; } = DateTime.MinValue;

        protected Queue<SendMessageCommand> MessagesToSend { get; } = new Queue<SendMessageCommand>();
        public int MessageSendQueueCount
        {
            get
            {
                return MessagesToSend.Count;
            }
        }
        public WebAPICommunicationTracker MessageSendStatus { get; private set; } = WebAPICommunicationTracker.StartState(3);
        public float TimeBetweenMessageSendAttempts { get; } = 5;

        public SetGatewayCommand LocalGatewaySimStatus { get; private set; }
        public WebAPICommunicationTracker SetGatewayStatus { get; private set; } = WebAPICommunicationTracker.StartState(3);
        public float TimeBetweenGatewayUpdates { get; } = 5;


        public GetGatewayRequest GetGatewayRequestData {  get; private set;}
        public SearchForGatewayReturn? ExternalGateway { get; private set; }
        public WebAPICommunicationTracker SearchForGatewayStatus { get; private set; } = WebAPICommunicationTracker.StartState(3);

        public SearchForGatewayReturn[] ExternalGatewayList { get; private set; }
        public WebAPICommunicationTracker SearchForGatewayListStatus { get; private set; } = WebAPICommunicationTracker.StartState(3);
        
        public bool NoGatewayExistsOnServer { get; private set; } = false;

        protected string m_strUniqueDeviceIdentifier = string.Empty;

        public WebInterface(MonoBehaviour mbhCoroutineRunner)
        {
            CoroutineExecutionObject = mbhCoroutineRunner;
        }

        public void UpdateCommunication()
        {
            //check if get player ID needs restarting
            if (PlayerIDCommunicationStatus.ShouldRestart())
            {
                //restart get player id
                InternalStartGetPlayerID();

            }

            //check if message fetch needs updating 
            if (
                MessageFetchStatus.m_cmsStatus != WebAPICommunicationTracker.CommunctionStatus.NotStarted &&
                MessageFetchStatus.m_cmsStatus != WebAPICommunicationTracker.CommunctionStatus.Cancled)
            {
                //work out what the cooldown time should be 
                float fTimeSinceLastMessage = (float)(DateTime.Now - TimeOfLastMessage).TotalSeconds;
                float fMessageGetCooldown = Math.Min(1.0f, fTimeSinceLastMessage / TimeBetweenMessageUpdatesCooldownTime);
                float fScaledMaxTimeBetweenUpdates = Mathf.Lerp(MinTimeBetweenMessageUpdates, MaxTimeBetweenMessageUpdates, fMessageGetCooldown);

                //check if it should be restarted
                if (MessageFetchStatus.ShouldRestart())
                {
                    InternalStartGettingMessagesFromServer();
                }
                else if (MessageFetchStatus.TimeSinceLastCommunication() > fScaledMaxTimeBetweenUpdates)
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

                MessageSendStatus = MessageSendStatus.Reset();
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
                    MessageSendStatus = MessageSendStatus.Reset();
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
                SetGatewayStatus.m_cmsStatus != WebAPICommunicationTracker.CommunctionStatus.NotStarted &&
                SetGatewayStatus.m_cmsStatus != WebAPICommunicationTracker.CommunctionStatus.Cancled &&
                SetGatewayStatus.TimeSinceLastCommunication() > TimeBetweenGatewayUpdates)
            {
                SetGatewayStatus = SetGatewayStatus.Reset();
                InternalStartSetGateway();
            }

            //check if gateway serch needs restarting
            if (SearchForGatewayStatus.ShouldRestart())
            {
                InternalStartSearchForGateway();
            }

            if(SearchForGatewayListStatus.ShouldRestart())
            {
                InternalStartSearchForGatewayList();
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

        //check if the web interface is currently getting messages from the server
        public bool IsGettingMessagesFromServer()
        {
            if (
                MessageFetchStatus.m_cmsStatus == WebAPICommunicationTracker.CommunctionStatus.Cancled ||
                MessageFetchStatus.m_cmsStatus == WebAPICommunicationTracker.CommunctionStatus.NotStarted)
            {
                return false;
            }

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
            //create message command
            SendMessageCommand smcMessageCommand = new SendMessageCommand()
            {
                m_iType = iMessageType,
                m_lFromID = UserID,
                m_lToID = lTarget,
                m_strMessage = strMessage
            };

            return SendMessage(smcMessageCommand);
        }

        public bool SendMessage(SendMessageCommand smcMessageCommand)
        {
            //check if local id found
            if (PlayerIDCommunicationStatus.m_cmsStatus != WebAPICommunicationTracker.CommunctionStatus.Succedded)
            {
                //dont have a local player id and cant send messages 
                return false;
            }
             
            //add to message queue
            MessagesToSend.Enqueue(smcMessageCommand);

            //check if message sending should be restarted 
            if (MessageSendStatus.m_cmsStatus == WebAPICommunicationTracker.CommunctionStatus.Cancled)
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
                m_gwsGateState = stsSimStatus,
                m_lUserID = UserID,
                m_lUserKey = UserKey
            };

            //check if communication needs starting
            if (SetGatewayStatus.m_cmsStatus == WebAPICommunicationTracker.CommunctionStatus.NotStarted ||
                SetGatewayStatus.m_cmsStatus == WebAPICommunicationTracker.CommunctionStatus.Cancled)
            {
                SetGatewayStatus = SetGatewayStatus.Reset();
                InternalStartSetGateway();
            }

            return true;

        }

        //stop running a gateway for people to connect through 
        public void CloseGateway()
        {
            if (SetGatewayStatus.m_cmsStatus != WebAPICommunicationTracker.CommunctionStatus.Cancled)
            {
                SetGatewayStatus = SetGatewayStatus.Cancel();
            }
        }

        /// <summary>
        /// Search for gateway returns false if already looking for gateway or player does not have an ID
        /// </summary>
        /// <returns></returns>
        public bool SearchForGateway(GetGatewayRequest gwrGatewayRequest)
        {
            //check if has id
            if (PlayerIDCommunicationStatus.m_cmsStatus != WebAPICommunicationTracker.CommunctionStatus.Succedded)
            {
                Debug.Log("User ID has not been fetched from server before looking for gateway");
                return false;
            }

            //check if already searching for a gateway 
            if (
                SearchForGatewayStatus.m_cmsStatus != WebAPICommunicationTracker.CommunctionStatus.NotStarted &&
                SearchForGatewayStatus.m_cmsStatus != WebAPICommunicationTracker.CommunctionStatus.Failed &&
                SearchForGatewayStatus.m_cmsStatus != WebAPICommunicationTracker.CommunctionStatus.Cancled &&
                SearchForGatewayStatus.m_cmsStatus != WebAPICommunicationTracker.CommunctionStatus.Succedded)
            {
                Debug.Log("Starting search for gateway before previouse one has finished");
                return false;
            }

            //set the data to request
            GetGatewayRequestData = gwrGatewayRequest;

            SearchForGatewayStatus = SearchForGatewayStatus.Reset();

            InternalStartSearchForGateway();

            return true;

        }


        /// <summary>
        /// Search for gateway returns false if already looking for gateway or player does not have an ID
        /// </summary>
        /// <returns></returns>
        public bool SearchForGatewayList(GetGatewayRequest gwrGatewayRequest)
        {
            //check if has id
            if (PlayerIDCommunicationStatus.m_cmsStatus != WebAPICommunicationTracker.CommunctionStatus.Succedded)
            {
                Debug.Log("User ID has not been fetched from server before looking for gateway");
                return false;
            }

            //check if already searching for a gateway 
            if (
                SearchForGatewayStatus.m_cmsStatus != WebAPICommunicationTracker.CommunctionStatus.NotStarted &&
                SearchForGatewayStatus.m_cmsStatus != WebAPICommunicationTracker.CommunctionStatus.Failed &&
                SearchForGatewayStatus.m_cmsStatus != WebAPICommunicationTracker.CommunctionStatus.Cancled &&
                SearchForGatewayStatus.m_cmsStatus != WebAPICommunicationTracker.CommunctionStatus.Succedded)
            {
                Debug.Log("Starting search for gateway before previouse one has finished");
                return false;
            }

            //set the data to request
            GetGatewayRequestData = gwrGatewayRequest;

            SearchForGatewayStatus = SearchForGatewayStatus.Reset();

            InternalStartSearchForGatewayList();

            return true;

        }


        protected IEnumerator WebRequest(string strAddress, string strBody, Action<bool,string> actCallback)
        {
            using (UnityWebRequest www = UnityWebRequest.Put(strAddress, strBody))
            {
                www.downloadHandler = new DownloadHandlerBuffer();
                www.SetRequestHeader("Content-Type", "application/json");
                www.SetRequestHeader("cache-control", "no-cache");
                www.method = UnityWebRequest.kHttpVerbPOST;

                yield return www.SendWebRequest();

                if (www.isNetworkError || www.isHttpError)
                {
                    Debug.Log(www.error);
                    actCallback.Invoke(false, www.responseCode.ToString());
                }
                else
                {
                    Debug.Log("Form upload complete!");

                    actCallback.Invoke(true, www.downloadHandler.text);
                }
            }
        }

        protected void InternalStartGetPlayerID()
        {
            PlayerIDCommunicationStatus = PlayerIDCommunicationStatus.StartNewAttempt();

            if (TestLocally)
            {
                FakeWebAPI.Instance.GetUserWithLoginCredentials(m_strUniqueDeviceIdentifier, InternalOnFinishGetPlayerID);
            }
            else
            {
                //build requrest
                string strRequest = $"{{ \"m_strUdid\":\"{m_strUniqueDeviceIdentifier}\"}}";

                CoroutineExecutionObject.StartCoroutine(WebRequest(s_strGetIDWithUDUDAddress, strRequest, InternalOnFinishGetPlayerID));
            }
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
                //mark coms attempt as failed
                PlayerIDCommunicationStatus = PlayerIDCommunicationStatus.CommunicationFailed();

                return;
            }

            //try and decode response
            try
            {
                UserIDDetails uidUserDetails = JsonUtility.FromJson<UserIDDetails>(strResult);

                if(uidUserDetails.m_lUserID == 0 || uidUserDetails.m_lUserKey == 0)
                {
                    //check error type

                    Debug.LogError($"Failed to decode json string : {strResult} int player id from server");

                    //mark coms attempt as failed
                    PlayerIDCommunicationStatus = PlayerIDCommunicationStatus.CommunicationFailed();

                    return;
                }

                UserID = uidUserDetails.m_lUserID;

                UserKey = uidUserDetails.m_lUserKey;
                
                //mark coms attempt as succeess
                PlayerIDCommunicationStatus = PlayerIDCommunicationStatus.CommunicationSuccessfull();
                
                return;

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

            GetMessageRequest gmrGetMessageRequest = new GetMessageRequest
            {
                m_lUserID = UserID,
                m_lUserKey = UserKey
            };

            string strRequest = JsonUtility.ToJson(gmrGetMessageRequest);

            if (TestLocally)
            {
                FakeWebAPI.Instance.GetDeleteUserMessages(strRequest, InternalOnFinishGettingMessagesFromServer);
            }
            else
            {
                CoroutineExecutionObject.StartCoroutine(WebRequest(s_strGetMessageAddress, strRequest, InternalOnFinishGettingMessagesFromServer));
            }
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

                mesMessages = mesMessages.OrderBy(x => x.m_dtmTimeOfMessage).ToList();

                //add new messages to message list
                foreach (UserMessage mesMessage in mesMessages)
                {
                    MessagesFromServer.Enqueue(mesMessage);
                }

                //update the time since last message recieved 
                if(mesMessages.Count > 0)
                {
                    TimeOfLastMessage = DateTime.Now;
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

            string strRequest = JsonUtility.ToJson(smcMessage);

            Debug.Log($"Sending message: {strRequest}");

            if (TestLocally)
            {
                FakeWebAPI.Instance.AddNewMessage(strRequest, InternalOnFinishSendingMessage);

              }
            else
            {
                CoroutineExecutionObject.StartCoroutine(WebRequest(s_strSendMessageAddress, strRequest, InternalOnFinishSendingMessage));
            }
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
            Debug.Log("Setting Gateway Status");

            SetGatewayStatus = SetGatewayStatus.StartNewAttempt();

            string strRequest = JsonUtility.ToJson(LocalGatewaySimStatus);

            if (TestLocally)
            {
                FakeWebAPI.Instance.SetGateway(strRequest, InternalOnFinishSetGateway);
            }
            else
            {
                CoroutineExecutionObject.StartCoroutine(WebRequest(s_strSetGatewayAddress, strRequest, InternalOnFinishSetGateway));
            }
        }

        protected void InternalOnFinishSetGateway(bool bWasSuccess, string strResult)
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
            SearchForGatewayStatus = SearchForGatewayStatus.StartNewAttempt();

            NoGatewayExistsOnServer = false;

            if (TestLocally)
            {
                //build requrest
                string strRequest = JsonUtility.ToJson(GetGatewayRequestData);
                FakeWebAPI.Instance.SearchForGateway(strRequest, InternalOnFinishSearchForGateway);
            }
            else
            {
                //build requrest
                string strRequest = JsonUtility.ToJson(GetGatewayRequestData);

                CoroutineExecutionObject.StartCoroutine(WebRequest(s_strGetGatewayAddress, strRequest, InternalOnFinishSearchForGateway));
            }
        }

        protected void InternalOnFinishSearchForGateway(bool bWasSuccess, string strResult)
        {
            if (SearchForGatewayStatus.m_cmsStatus == WebAPICommunicationTracker.CommunctionStatus.Cancled)
            {
                return;
            }

            if (bWasSuccess == false)
            {
                //check if the reason the conenction failed was because the server has no matching games
                if (strResult.Contains("404"))
                {
                    NoGatewayExistsOnServer = true;
                }

                SearchForGatewayStatus = SearchForGatewayStatus.CommunicationFailed();

                return;
            }

            try
            {
                Debug.Log($"External Gateway: {strResult} found");

                //decode external gate
                ExternalGateway = JsonUtility.FromJson<SearchForGatewayReturn>(strResult);

                SearchForGatewayStatus = SearchForGatewayStatus.CommunicationSuccessfull();

                NoGatewayExistsOnServer = false;
            }
            catch
            {
                SearchForGatewayStatus = SearchForGatewayStatus.CommunicationFailed();
            }
        }

        protected void InternalStartSearchForGatewayList()
        {
            SearchForGatewayStatus = SearchForGatewayStatus.StartNewAttempt();

            NoGatewayExistsOnServer = false;

            if (TestLocally)
            {
                FakeWebAPI.Instance.SearchForGatewayList(UserID.ToString(), InternalOnFinishSearchForGatewayList);
            }
            else
            {
                //build requrest
                string strRequest = JsonUtility.ToJson(GetGatewayRequestData);

                CoroutineExecutionObject.StartCoroutine(WebRequest(s_strGetGatewayListAddress, strRequest, InternalOnFinishSearchForGatewayList));
            }
        }

        protected void InternalOnFinishSearchForGatewayList(bool bWasSuccess, string strResult)
        {
            if (SearchForGatewayStatus.m_cmsStatus == WebAPICommunicationTracker.CommunctionStatus.Cancled)
            {
                return;
            }

            if (bWasSuccess == false)
            {
                //check if the reason the conenction failed was because the server has no matching games
                if (strResult.Contains("404"))
                {
                    ExternalGatewayList = new SearchForGatewayReturn[0];
                    NoGatewayExistsOnServer = true;
                }

                SearchForGatewayStatus = SearchForGatewayStatus.CommunicationFailed();

                return;
            }

            try
            {
                Debug.Log($"External Gateway: {strResult} found");

                //decode external gate
                ExternalGatewayList = JsonUtility.FromJson<SearchForGatewayReturn[]>(strResult);

                SearchForGatewayStatus = SearchForGatewayStatus.CommunicationSuccessfull();

                NoGatewayExistsOnServer = false;
            }
            catch
            {
                SearchForGatewayStatus = SearchForGatewayStatus.CommunicationFailed();
            }
        }


    }
}
