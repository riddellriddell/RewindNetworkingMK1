using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Utility;

namespace Networking
{

    /// <summary>
    /// this class simulates the behaviour of a firebase web api for testing purposes 
    /// </summary>
    public class FakeWebAPI : MonoBehaviour
    {
        //simulated API Database
        protected class FakeDatabase
        {
            //the number of seconds before a message by the user has timed out and can be discarded
            public static float s_fUserMessageTimeOut = 10f;

            public static float s_fGatewayTimeOut = 6f;

            public Dictionary<string, UserIDDetails> m_uicUserIDCredentialsPairs = new Dictionary<string, UserIDDetails>();
            public Dictionary<long, UserIDDetails> m_uicUserIDs = new Dictionary<long, UserIDDetails>();
            public Dictionary<long, UserMessages> m_umsUserMessages = new Dictionary<long, UserMessages>();
            public Dictionary<long, Gateway> m_gtwGateways = new Dictionary<long, Gateway>();
            
            public DeterministicRandomNumberGenerator m_rngRandomNumberGenerator = new DeterministicRandomNumberGenerator(1231456789ul);

            public ITimeSource m_tscTimeSource;

            public FakeDatabase(ITimeSource timeSource)
            {
                m_tscTimeSource = timeSource;
            }
            
            //gets the id for the passed in identifier or returns long min value if not found
            public UserIDDetails GetUserIDWithCredentials(string strLoginCredentials)
            {
                if(m_uicUserIDCredentialsPairs.TryGetValue(strLoginCredentials, out UserIDDetails uidUserDetails))
                {
                    return uidUserDetails;
                }
                else
                {
                    UserIDDetails uidNewUser = new UserIDDetails()
                    {
                        m_lUserID = m_rngRandomNumberGenerator.GetRandomSignedInt(),
                        m_lUserKey = m_rngRandomNumberGenerator.GetRandomSignedInt()
                    };

                    m_uicUserIDCredentialsPairs.Add(strLoginCredentials, uidNewUser);
                    m_uicUserIDs.Add(uidNewUser.m_lUserID, uidNewUser);
                    m_umsUserMessages.Add(uidNewUser.m_lUserID, new UserMessages() { m_lAccountID = uidNewUser.m_lUserID, m_umUserMessages = new UserMessage[0] });

                    return uidNewUser;
                }
            }

            //get and delete messages
            public List<UserMessage> GetDeleteUserMessages(long lUserID)
            {
                //get user messages
                UserMessages umsUserMessages = m_umsUserMessages[lUserID];

                //messages to return
                List<UserMessage> mesRetunMessages = new List<UserMessage>();

                //filter out old messages
                foreach (UserMessage mesMessage in umsUserMessages.m_umUserMessages)
                {
                    //get time dif
                    TimeSpan tspTimeSpan = m_tscTimeSource.UTCNow - new DateTime(mesMessage.m_dtmTimeOfMessage);

                    //add message to list of messages to return
                    if (tspTimeSpan.TotalSeconds < s_fUserMessageTimeOut)
                    {
                        mesRetunMessages.Add(mesMessage);
                    }
                }

                //clear old messages
                m_umsUserMessages[lUserID] = new UserMessages() { m_lAccountID = umsUserMessages.m_lAccountID, m_umUserMessages = new UserMessage[0] };

                //return result
                return mesRetunMessages;
            }

            //add new message 
            public bool AddNewMessage(long lToUserID, long lFromUserID, int iMessageType, string strMessage)
            {
                //check use exists
                if (m_umsUserMessages.ContainsKey(lToUserID) == false)
                {
                    return false;
                }

                //build message 
                UserMessage mesNewMessage = new UserMessage()
                {
                    m_iMessageType = iMessageType,
                    m_lFromUser = lFromUserID,
                    m_dtmTimeOfMessage = m_tscTimeSource.UTCNow.Ticks,
                    m_strMessage = strMessage

                };

                //get user
                UserMessages umsUserMessages = m_umsUserMessages[lToUserID];

                List<UserMessage> mesMessages = new List<UserMessage>(umsUserMessages.m_umUserMessages);

                mesMessages.Add(mesNewMessage);

                umsUserMessages.m_umUserMessages = mesMessages.ToArray();

                m_umsUserMessages[lToUserID] = umsUserMessages;

                return true;
            }

            //update gateway 
            public bool SetGateway(long lUserID, long lAccessKey, int iRemainingSlots, int iGameType, int iFlags, string strGameState)
            {
                Gateway gtwNewGate = new Gateway()
                {
                    m_lUserID = lUserID,
                    m_lUserKey = lAccessKey,
                    m_dtmLastActiveTime = m_tscTimeSource.UTCNow.Ticks,
                    m_gwsGateState = new GatewayState()
                    {
                        m_iRemainingSlots = iRemainingSlots,
                    },
                    m_lGameType = iGameType,
                    m_lFlags = iFlags,
                    m_gstGameState = new GameState()
                    {
                        m_strGameState = strGameState
                    }
                };

                m_gtwGateways[lUserID] = gtwNewGate;

                return true;
            }

            //get a gateway made by user 
            public Gateway? GetGateway(long lUserID)
            {
                if(m_gtwGateways.TryGetValue(lUserID,out Gateway gtwGate))
                {
                    return gtwGate;
                }

                return null;
            }

            public bool CheckFlags(Gateway gtwGate, long iGameType, long iFlags)
            {
                if(gtwGate.m_lGameType != iGameType)
                {
                    return false;
                }

                if((gtwGate.m_lFlags & iFlags) != iFlags)
                {
                    return false;
                }

                return true;
            }

            //search for gateway
            public Gateway? SearchForGateway(long iGameType, long iFlags)
            {
                RemoveOldGates();

                foreach (Gateway gtwGate in m_gtwGateways.Values)
                {
                    //check if there are empty player slots
                    if (gtwGate.m_gwsGateState.m_iRemainingSlots <= 0)
                    {
                        continue;
                    }

                    //check if there are empty player slots
                    if (!CheckFlags(gtwGate, iGameType, iFlags))
                    {
                        continue;
                    }

                    return gtwGate;
                }

                //no gate found
                return null;
            }

            public Gateway[] SearchForGatewayList(long iGameType, long iFlags)
            {
                RemoveOldGates();

                List<Gateway> validGates = new List<Gateway>();

                foreach (Gateway gtwGate in m_gtwGateways.Values)
                {

                    //check if there are empty player slots
                    if (gtwGate.m_gwsGateState.m_iRemainingSlots <= 0)
                    {
                        continue;
                    }

                    //check if there are empty player slots
                    if (!CheckFlags(gtwGate, iGameType, iFlags))
                    {
                        continue;
                    }

                    validGates.Add(gtwGate);
                }

                //no gate found
                return validGates.ToArray();
            }


            //check if string identifier already exists in user id list
            protected bool DoesIdentifierExist(string strLoginCredentials)
            {
                return m_uicUserIDCredentialsPairs.ContainsKey(strLoginCredentials);
            }

            //remove old gates that are nolonger in use 
            protected void RemoveOldGates()
            {
                List<long> lGatesToRemove = new List<long>();

                foreach (Gateway gtwGate in m_gtwGateways.Values)
                {
                    TimeSpan tspTimeSinceLastUpdate = m_tscTimeSource.UTCNow - new DateTime(gtwGate.m_dtmLastActiveTime);

                    if (tspTimeSinceLastUpdate.Seconds > s_fGatewayTimeOut)
                    {
                        lGatesToRemove.Add(gtwGate.m_lUserID);
                    }
                }

                foreach (long strTargetToRemove in lGatesToRemove)
                {
                    m_gtwGateways.Remove(strTargetToRemove);
                }
            }
        }

        protected interface IDelayedAction
        {
            public abstract void Execute();

        }
        public static FakeWebAPI Instance { get; private set; } = null;


        [SerializeField]
        public DebugLoggingLevel dllLogLevel = DebugLoggingLevel.Verbose;

        //variables

        //the standard delay between making a request and getting an answer
        [SerializeField]
        public float m_fLatncy = 0.6f;

        //the chance a bad connection results in the server timing out
        [SerializeField]
        public float m_fTimeOutChance = 0.01f;

        //the time it takes for the server to time out
        [SerializeField]
        public float m_fTimeOutTime = 5.0f;

        //the result returned when timout occurs 
        protected string m_strTimeOutResponse = "408 Request Timeout";

        [SerializeField]
        public float m_fActionErrorChance = 0.25f;
        
        [SerializeField]
        public TimeSourceComponentBase m_tscTimeSource;

        protected string m_strServerErrorResponse = "500 Internal Server Error";

        protected string m_strItemDoesNoteExistResponse = "404 Item Does Not Exist";

        protected string m_strDoNotHavePermissionResponse = "403 Action Denied Error";

        protected FakeDatabase m_fdbFakeDatabase;

        protected SortedList<DateTime, Action> m_actDelayedActions = new SortedList<DateTime, Action>();

        protected DeterministicRandomNumberGenerator m_rngRandomNumberGenerator = new DeterministicRandomNumberGenerator(1231456789ul);

        
        public void Start()
        {
            //ensure singleton pattern 
            if (Instance == null)
            {
                Instance = this;
                
                //setup fake database
                m_fdbFakeDatabase = new FakeDatabase(m_tscTimeSource);
            }
            else if (Instance != this)
            {
                if (Application.isPlaying)
                {
                    Destroy(this);
                }
                else
                {
                    DestroyImmediate(this);
                }
            }
        }

        public void Update()
        {
            //get the time
            DateTime dtmNow = m_tscTimeSource.UTCNow;
            
            //loop through the delayed actions and execute them
            while (m_actDelayedActions.Count > 0)
            {
                DateTime dtmActionExecuteTime = GetNextActionTime();

                if (dtmActionExecuteTime >= dtmNow)
                {
                    break;
                }

                Action actActionToExecute = DequeueAction();
                
                actActionToExecute.Invoke();
            }
        }

        public void GetUserWithLoginCredentials(string strLoginCredentials, Action<bool, string> actGetUserCallback)
        {
            InternalGetUserWithLoginCredentials(strLoginCredentials, actGetUserCallback);
        }

        public void GetDeleteUserMessages(string strUserIDandKey, Action<bool, string> actGetMessagesCallback)
        {
            InternalGetDeleteUserMessages(strUserIDandKey, actGetMessagesCallback);
        }

        public void AddNewMessage(string strNewMessageDetails, Action<bool, string> actAddMessageCallback)
        {
            InternalAddNewMessage(strNewMessageDetails, actAddMessageCallback);
        }

        public void SetGateway(string strSetGatewayCommand, Action<bool, string> actGatewayUpdateCallback)
        {
            InternalSetGateway(strSetGatewayCommand, actGatewayUpdateCallback);
        }

        public void SearchForGateway(string strGatewayRequest, Action<bool, string> actSearchForGateCallback)
        {
            InternalSearchForGateway(strGatewayRequest, actSearchForGateCallback);
        }

        public void SearchForGatewayList(string strUserID, Action<bool, string> actSearchForGateCallback)
        {
            InternalSearchForGatewayList(strUserID, actSearchForGateCallback);
        }

        protected void InternalGetUserWithLoginCredentials(string strLoginCredentials, Action<bool, string> actGetUserCallback)
        {

            //check for timeout 
            if (m_rngRandomNumberGenerator.GetRandomRangeFloat(0.0f, 1.0f) < m_fTimeOutChance)
            {
                QueueAction(m_fTimeOutTime, () => { actGetUserCallback?.Invoke(false, m_strTimeOutResponse); }  );

                return;
            }

            QueueAction(m_fLatncy, () =>
            {
                //check if user could not be created due to conflicts / bad connection or other conflicts
                if (m_rngRandomNumberGenerator.GetRandomRangeFloat(0.0f, 1.0f) < m_fActionErrorChance)
                {
                    //return error result
                    actGetUserCallback?.Invoke(false, m_strServerErrorResponse);

                    return;
                }

                //try get user
                UserIDDetails strUserDetails = m_fdbFakeDatabase.GetUserIDWithCredentials(strLoginCredentials);

                if (LogHelp.LogVerbose(dllLogLevel))
                    Debug.Log($"FakeWebAPI.InternalGetUserWithLogCredentials: get user account with credentials: {strLoginCredentials} returned : {strUserDetails} ");

                //return success
                actGetUserCallback?.Invoke(true, JsonUtility.ToJson(strUserDetails));
            });
        }

        protected void InternalGetDeleteUserMessages(string strUserDetails, Action<bool, string> actGetMessagesCallback)
        {
            //check for timeout 
            if (m_rngRandomNumberGenerator.GetRandomRangeFloat(0.0f, 1.0f) < m_fTimeOutChance)
            {
                QueueAction(m_fTimeOutTime, () => { actGetMessagesCallback?.Invoke(false, m_strTimeOutResponse); }  );

                return;
            }
            
            QueueAction(m_fLatncy, () =>
            {

                //check if user could not be created due to conflicts / bad connection or other conflicts
                if (m_rngRandomNumberGenerator.GetRandomRangeFloat(0.0f, 1.0f) < m_fActionErrorChance)
                {
                    //return error result
                    actGetMessagesCallback?.Invoke(false, m_strServerErrorResponse);

                    return;
                }

                GetMessageRequest gmdGetMessageRequest = JsonUtility.FromJson<GetMessageRequest>(strUserDetails);

                //check if message request was properly formed
                if (gmdGetMessageRequest.m_lUserKey == 0 || gmdGetMessageRequest.m_lUserID == 0)
                {
                    if (LogHelp.LogError(dllLogLevel))
                        Debug.LogError($"FakeWebAPI.InternalGetDeleteUserMessages: Failed to parse user details : {strUserDetails}");

                    //return error result
                    actGetMessagesCallback?.Invoke(false, m_strServerErrorResponse);

                    return;
                }

                //check that the user key matches
                UserIDDetails uidUserDetails = m_fdbFakeDatabase.m_uicUserIDs[gmdGetMessageRequest.m_lUserID];

                if (uidUserDetails.m_lUserKey != gmdGetMessageRequest.m_lUserKey)
                {
                    if (LogHelp.LogError(dllLogLevel))
                        Debug.LogError(
                            $"FakeWebAPI.InternalGetDeleteUserMessages: User access key incorrect  Request:{strUserDetails} User Key: {gmdGetMessageRequest.m_lUserKey}");

                    //return error result
                    actGetMessagesCallback?.Invoke(false, m_strServerErrorResponse);

                    return;
                }

                List<UserMessage> mesMessages = m_fdbFakeDatabase.GetDeleteUserMessages(gmdGetMessageRequest.m_lUserID);

                GetMessageReturn gmrReturn = new GetMessageReturn()
                {
                    m_usmUserMessages = mesMessages.ToArray()
                };

                //serialize result 
                string strResult = JsonUtility.ToJson(gmrReturn);

                if (LogHelp.LogVerbose(dllLogLevel))
                    Debug.Log($"FakeWebAPI.InternalGetDeleteUserMessages: Get Delete Messages with ID: {strUserDetails} returned: {strResult}");

                actGetMessagesCallback?.Invoke(true, strResult);
            });
        }

        protected void InternalAddNewMessage(string strNewMessageDetails, Action<bool, string> actSendMessageCallback)
        {
            //check for timeout 
            if (m_rngRandomNumberGenerator.GetRandomRangeFloat(0.0f, 1.0f) < m_fTimeOutChance)
            {
                QueueAction(m_fTimeOutTime, () => { actSendMessageCallback?.Invoke(false, m_strTimeOutResponse); }  );

                return;
            }

            QueueAction(m_fLatncy, () =>
            {

                //check if user could not be created due to conflicts / bad connection or other conflicts
                if (m_rngRandomNumberGenerator.GetRandomRangeFloat(0.0f, 1.0f) < m_fActionErrorChance)
                {
                    //return error result
                    actSendMessageCallback?.Invoke(false, m_strServerErrorResponse);

                    return;
                }

                //try and convert user details to long
                SendMessageCommand smcSendMessageCommand =
                    JsonUtility.FromJson<SendMessageCommand>(strNewMessageDetails);

                //try to add the message to the users database entry
                bool bWasMessageAdded = m_fdbFakeDatabase.AddNewMessage(smcSendMessageCommand.m_lToID,
                    smcSendMessageCommand.m_lFromID, smcSendMessageCommand.m_iType, smcSendMessageCommand.m_strMessage);
                
                if (bWasMessageAdded == false)
                {
                    //return error if accound does not exist
                    actSendMessageCallback?.Invoke(false, m_strItemDoesNoteExistResponse);

                    return;
                }

                if (LogHelp.LogVerbose(dllLogLevel))
                    Debug.Log($"FakeWebAPI.InternalAddNewMessage: added message from: {smcSendMessageCommand.m_lFromID} To: {smcSendMessageCommand.m_lToID} Of Type: {smcSendMessageCommand.m_iType} with payload: {smcSendMessageCommand.m_strMessage}" );

                //message sent successfully
                actSendMessageCallback?.Invoke(true, string.Empty);
            });
        }

        protected void InternalSetGateway(string strSetGatewayCommand, Action<bool, string> actSetGateway)
        {
            //check for timeout 
            if (m_rngRandomNumberGenerator.GetRandomRangeFloat(0.0f, 1.0f) < m_fTimeOutChance)
            {
                QueueAction(m_fTimeOutTime, () => { actSetGateway?.Invoke(false, m_strTimeOutResponse); }  );

                return;
            }

            QueueAction(m_fLatncy, () => 
                {

            //check if user could not be created due to conflicts / bad connection or other conflicts
            if (m_rngRandomNumberGenerator.GetRandomRangeFloat(0.0f, 1.0f) < m_fActionErrorChance)
            {
                //return error result
                actSetGateway?.Invoke(false, m_strServerErrorResponse);
                return;
            }

            //deserialize gateway changes
            SetGatewayCommand ugcUpdateGateCommand = JsonUtility.FromJson<SetGatewayCommand>(strSetGatewayCommand);

            Gateway? gtwGate = m_fdbFakeDatabase.GetGateway(ugcUpdateGateCommand.m_lUserID);

            //check access key
            if (gtwGate.HasValue == true && gtwGate.Value.m_lUserKey != ugcUpdateGateCommand.m_lUserKey)
            {
                //return error result
                actSetGateway?.Invoke(false, m_strServerErrorResponse);
                return;
            }

            //try and find the target gateway
            if (m_fdbFakeDatabase.SetGateway(
                ugcUpdateGateCommand.m_lUserID, 
                ugcUpdateGateCommand.m_lUserKey,
                ugcUpdateGateCommand.m_gwsGateState.m_iRemainingSlots, 
                ugcUpdateGateCommand.m_lGameType, 
                ugcUpdateGateCommand.m_lFlags, 
                ugcUpdateGateCommand.m_gstGameState.m_strGameState) == false)
            {
                //return error result
                actSetGateway?.Invoke(false, m_strServerErrorResponse);
                return;
            }

            actSetGateway?.Invoke(true, string.Empty);
            }  );
        }
        
        protected void InternalSearchForGateway(string strGatewayDetails, Action<bool, string> actSearchCallback)
        {
            //check for timeout 
            if (m_rngRandomNumberGenerator.GetRandomRangeFloat(0.0f, 1.0f) < m_fTimeOutChance)
            {
                QueueAction(m_fTimeOutTime, () => { actSearchCallback?.Invoke(false, m_strTimeOutResponse); }  );

                return;
            }

            QueueAction(m_fLatncy, () =>
            {

                //check if user could not be created due to conflicts / bad connection or other conflicts
                if (m_rngRandomNumberGenerator.GetRandomRangeFloat(0.0f, 1.0f) < m_fActionErrorChance)
                {
                    //return error result
                    actSearchCallback?.Invoke(false, m_strServerErrorResponse);

                    return;
                }

                GetGatewayRequest gwrRequest = JsonUtility.FromJson<GetGatewayRequest>(strGatewayDetails);

                Gateway? gtwGate = m_fdbFakeDatabase.SearchForGateway(gwrRequest.m_lGameType, gwrRequest.m_lFlags);

                if (gtwGate.HasValue == false)
                {
                    actSearchCallback?.Invoke(false, m_strItemDoesNoteExistResponse);

                    return;
                }

                GatewayReturnDetails sgrReturnValue = new GatewayReturnDetails
                {
                    m_lGateOwnerUserID = gtwGate.Value.m_lUserID,
                    m_gwsGateState = gtwGate.Value.m_gwsGateState,
                    m_lGameFlags = gtwGate.Value.m_lFlags,
                    m_lGameState = gtwGate.Value.m_gstGameState
                };

                string strGateReturnValue = JsonUtility.ToJson(sgrReturnValue);

                actSearchCallback?.Invoke(true, strGateReturnValue);
            });
        }

        protected void InternalSearchForGatewayList(string strGatewayDetails, Action<bool, string> actSearchCallback)
        {
            //check for timeout 
            if (m_rngRandomNumberGenerator.GetRandomRangeFloat(0.0f, 1.0f) < m_fTimeOutChance)
            {
                QueueAction(m_fTimeOutTime, () => { actSearchCallback?.Invoke(false, m_strTimeOutResponse); });
                return;
            }

            QueueAction(m_fLatncy, () =>
            {
                //check if user could not be created due to conflicts / bad connection or other conflicts
                if (m_rngRandomNumberGenerator.GetRandomRangeFloat(0.0f, 1.0f) < m_fActionErrorChance)
                {
                    //return error result
                    actSearchCallback?.Invoke(false, m_strServerErrorResponse);

                    return;
                }

                GetGatewayRequest gwrRequest = new GetGatewayRequest();

                //get user id
                try
                {
                    gwrRequest = JsonUtility.FromJson<GetGatewayRequest>(strGatewayDetails);
                }
                catch
                {
                    Debug.LogError("bad gate request json message, recieved: " + strGatewayDetails +
                                   " but was expecting something like: " + JsonUtility.ToJson(gwrRequest));
                }

                Gateway[] gtwGate = m_fdbFakeDatabase.SearchForGatewayList(gwrRequest.m_lGameType, gwrRequest.m_lFlags);

                if (gtwGate.Length == 0)
                {
                    actSearchCallback?.Invoke(false, m_strItemDoesNoteExistResponse);

                    return;
                }

                GatewayReturnDetails[] sgrReturnValue = new GatewayReturnDetails[gtwGate.Length];

                for (int i = 0; i < gtwGate.Length; i++)
                {
                    sgrReturnValue[i] = new GatewayReturnDetails
                    {
                        m_lGateOwnerUserID = gtwGate[i].m_lUserID,
                        m_gwsGateState = gtwGate[i].m_gwsGateState,
                        m_lGameFlags = gtwGate[i].m_lFlags,
                        m_lGameState = gtwGate[i].m_gstGameState
                    };
                }

                SearchForGatewayReturnList grlGateList = new SearchForGatewayReturnList();

                grlGateList.m_grdGateList = sgrReturnValue;

                //string strGateReturnValue = "[";
                //for (int i = 0; i < sgrReturnValue.Length; i++)
                //{
                //    strGateReturnValue += JsonUtility.ToJson(sgrReturnValue[i]);
                //
                //    if(i != sgrReturnValue.Length -1)
                //    {
                //        strGateReturnValue += ",";
                //    }
                //}
                //strGateReturnValue += "]";

                string strGateReturnValue = JsonUtility.ToJson(grlGateList);


                actSearchCallback?.Invoke(true, strGateReturnValue);
            });
        }


        protected void QueueAction(DateTime dtmTimeToExecute, Action actDelayedAction)
        {
            m_actDelayedActions.Add(dtmTimeToExecute, actDelayedAction);
        }
        
        protected void QueueAction(TimeSpan tspDelayUntilExecute, Action actDelayedAction)
        {
            DateTime dtmNow = m_tscTimeSource.UTCNow;
            m_actDelayedActions.Add(dtmNow + tspDelayUntilExecute, actDelayedAction);
        }

        protected void QueueAction(float fDelayUntilExecute, Action actDelayedAction)
        {
            DateTime dtmNow = m_tscTimeSource.UTCNow;

            DateTime dtmKey = dtmNow + TimeSpan.FromSeconds(fDelayUntilExecute);
            
            
            //check if something already exists, if it does then delay the smallest amount possible
            while (m_actDelayedActions.ContainsKey(dtmKey))
            {
                dtmKey += TimeSpan.FromTicks(1);
            }
            
            m_actDelayedActions.Add(dtmKey, actDelayedAction);
        }
        
        protected DateTime GetNextActionTime()
        {
            if (m_actDelayedActions.Count > 0)
            {
                return m_actDelayedActions.Keys.First();
            }

            return DateTime.MaxValue;
        }

        protected Action DequeueAction()
        {
            
            Action actFirstAction = m_actDelayedActions.FirstOrDefault().Value;
            
            m_actDelayedActions.RemoveAt(0);

            return actFirstAction;
        }
    }
}