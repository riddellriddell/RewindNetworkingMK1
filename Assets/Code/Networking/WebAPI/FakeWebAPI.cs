using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Networking
{

    /// <summary>
    /// this class simulates the behaviour of a firebase web api for testing purposes 
    /// </summary>
    public class FakeWebAPI : MonoBehaviour
    {
        public static string GenerateRandomString(int iCharacters)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, iCharacters)
              .Select(s => s[Random.Range(0,s.Length)]).ToArray());
        }

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
                        m_lUserID = Random.Range(int.MinValue,int.MaxValue),
                        m_lUserKey = Random.Range(int.MinValue, int.MaxValue)
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
                    TimeSpan tspTimeSpan = DateTime.UtcNow - new DateTime(mesMessage.m_dtmTimeOfMessage);

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
                    m_dtmTimeOfMessage = DateTime.UtcNow.Ticks,
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
                    m_dtmLastActiveTime = DateTime.UtcNow.Ticks,
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
                    TimeSpan tspTimeSinceLastUpdate = DateTime.UtcNow - new DateTime(gtwGate.m_dtmLastActiveTime);

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

        public static FakeWebAPI Instance { get; private set; } = null;


        [SerializeField]
        public DebugLoggingLevel dllLogLevel = DebugLoggingLevel.None;

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

        protected string m_strServerErrorResponse = "500 Internal Server Error";

        protected string m_strItemDoesNoteExistResponse = "404 Item Does Not Exist";

        protected string m_strDoNotHavePermissionResponse = "403 Action Denied Error";
        
        protected FakeDatabase m_fdbFakeDatabase = new FakeDatabase();

        public void Start()
        {
            //ensure singleton pattern 
            if (Instance == null)
            {
                Instance = this;
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

        public void GetUserWithLoginCredentials(string strLoginCredentials, Action<bool, string> actGetUserCallback)
        {
            StartCoroutine(InternalGetUserWithLoginCredentials(strLoginCredentials, actGetUserCallback));
        }

        public void GetDeleteUserMessages(string strUserIDandKey, Action<bool, string> actGetMessagesCallback)
        {
            StartCoroutine(InternalGetDeleteUserMessages(strUserIDandKey, actGetMessagesCallback));
        }

        public void AddNewMessage(string strNewMessageDetails, Action<bool, string> actAddMessageCallback)
        {
            StartCoroutine(InternalAddNewMessage(strNewMessageDetails, actAddMessageCallback));
        }

        public void SetGateway(string strSetGatewayCommand, Action<bool, string> actGatewayUpdateCallback)
        {
            StartCoroutine(InternalSetGateway(strSetGatewayCommand, actGatewayUpdateCallback));
        }

        public void SearchForGateway(string strGatewayRequest, Action<bool, string> actSearchForGateCallback)
        {
            StartCoroutine(InternalSearchForGateway(strGatewayRequest, actSearchForGateCallback));
        }

        public void SearchForGatewayList(string strUserID, Action<bool, string> actSearchForGateCallback)
        {
            StartCoroutine(InternalSearchForGatewayList(strUserID, actSearchForGateCallback));
        }

        protected IEnumerator InternalGetUserWithLoginCredentials(string strLoginCredentials, Action<bool, string> actGetUserCallback)
        {

            //check for timeout 
            if (Random.Range(0.0f, 1.0f) < m_fTimeOutChance)
            {
                yield return new WaitForSeconds(m_fTimeOutTime);

                actGetUserCallback?.Invoke(false, m_strTimeOutResponse);

                yield break;
            }

            yield return new WaitForSeconds(m_fLatncy);

            //check if user could not be created due to conflicts / bad connection or other conflicts
            if (Random.Range(0.0f, 1.0f) < m_fActionErrorChance)
            {
                //return error result
                actGetUserCallback?.Invoke(false, m_strServerErrorResponse);

                yield break;
            }

            //try get user
            UserIDDetails strUserDetails = m_fdbFakeDatabase.GetUserIDWithCredentials(strLoginCredentials);

            if (LogHelp.LogVerbose(dllLogLevel)) Debug.Log($"get user account with credentials: {strLoginCredentials} returned : {strUserDetails} ");

            //return success
            actGetUserCallback?.Invoke(true, JsonUtility.ToJson(strUserDetails));
        }

        protected IEnumerator InternalGetDeleteUserMessages(string strUserDetails, Action<bool, string> actGetMessagesCallback)
        {
            //check for timeout 
            if (Random.Range(0.0f, 1.0f) < m_fTimeOutChance)
            {
                yield return new WaitForSeconds(m_fTimeOutTime);

                actGetMessagesCallback?.Invoke(false, m_strTimeOutResponse);

                yield break;
            }

            yield return new WaitForSeconds(m_fLatncy);

            //check if user could not be created due to conflicts / bad connection or other conflicts
            if (Random.Range(0.0f, 1.0f) < m_fActionErrorChance)
            {
                //return error result
                actGetMessagesCallback?.Invoke(false, m_strServerErrorResponse);

                yield break;
            }

            GetMessageRequest gmdGetMessageRequest = JsonUtility.FromJson<GetMessageRequest>(strUserDetails);

            //check if message request was properly formed
            if (gmdGetMessageRequest.m_lUserKey == 0 || gmdGetMessageRequest.m_lUserID == 0)
            {
                if (LogHelp.LogError(dllLogLevel)) Debug.LogError($"Failed to parse user details : {strUserDetails}");

                //return error result
                actGetMessagesCallback?.Invoke(false, m_strServerErrorResponse);

                yield break;
            }

            //check that the user key matches
            UserIDDetails uidUserDetails = m_fdbFakeDatabase.m_uicUserIDs[gmdGetMessageRequest.m_lUserID];

            if(uidUserDetails.m_lUserKey != gmdGetMessageRequest.m_lUserKey)
            {
                if (LogHelp.LogError(dllLogLevel)) Debug.LogError($"User access key incorrect  Request:{strUserDetails} User Key: {gmdGetMessageRequest.m_lUserKey}");

                //return error result
                actGetMessagesCallback?.Invoke(false, m_strServerErrorResponse);

                yield break;
            }

            List<UserMessage> mesMessages = m_fdbFakeDatabase.GetDeleteUserMessages(gmdGetMessageRequest.m_lUserID);

            GetMessageReturn gmrReturn = new GetMessageReturn()
            {
                m_usmUserMessages = mesMessages.ToArray()
            };

            //serialize result 
            string strResult = JsonUtility.ToJson(gmrReturn);

            if (LogHelp.LogVerbose(dllLogLevel)) Debug.Log($"Get Delete Messages with ID: {strUserDetails} returned: {strResult}");

            actGetMessagesCallback?.Invoke(true, strResult);
        }

        protected IEnumerator InternalAddNewMessage(string strNewMessageDetails, Action<bool, string> actSendMessageCallback)
        {
            //check for timeout 
            if (Random.Range(0.0f, 1.0f) < m_fTimeOutChance)
            {
                yield return new WaitForSeconds(m_fTimeOutTime);

                actSendMessageCallback?.Invoke(false, m_strTimeOutResponse);

                yield break;
            }

            yield return new WaitForSeconds(m_fLatncy);

            //check if user could not be created due to conflicts / bad connection or other conflicts
            if (Random.Range(0.0f, 1.0f) < m_fActionErrorChance)
            {
                //return error result
                actSendMessageCallback?.Invoke(false, m_strServerErrorResponse);

                yield break;
            }
            
            //try and convert user details to long
            SendMessageCommand smcSendMessageCommand = JsonUtility.FromJson<SendMessageCommand>(strNewMessageDetails);

            //try to add the message to the users database entry
            bool bWasMessageAdded = m_fdbFakeDatabase.AddNewMessage(smcSendMessageCommand.m_lToID, smcSendMessageCommand.m_lFromID, smcSendMessageCommand.m_iType, smcSendMessageCommand.m_strMessage);

            if (bWasMessageAdded == false)
            {
                //return error if accound does not exist
                actSendMessageCallback?.Invoke(false, m_strItemDoesNoteExistResponse);

                yield break;
            }

            //Debug.Log($"message {strNewMessageDetails} sent successfully");

            //message sent successfully
            actSendMessageCallback?.Invoke(true, string.Empty);
        }

        protected IEnumerator InternalSetGateway(string strSetGatewayCommand, Action<bool, string> actSetGateway)
        {
            //check for timeout 
            if (Random.Range(0.0f, 1.0f) < m_fTimeOutChance)
            {
                yield return new WaitForSeconds(m_fTimeOutTime);

                actSetGateway?.Invoke(false, m_strTimeOutResponse);

                yield break;
            }

            yield return new WaitForSeconds(m_fLatncy);

            //check if user could not be created due to conflicts / bad connection or other conflicts
            if (Random.Range(0.0f, 1.0f) < m_fActionErrorChance)
            {
                //return error result
                actSetGateway?.Invoke(false, m_strServerErrorResponse);

                yield break;
            }

            //deserialize gateway changes
            SetGatewayCommand ugcUpdateGateCommand = JsonUtility.FromJson<SetGatewayCommand>(strSetGatewayCommand);

            Gateway? gtwGate = m_fdbFakeDatabase.GetGateway(ugcUpdateGateCommand.m_lUserID);

            //check access key
            if (gtwGate.HasValue == true && gtwGate.Value.m_lUserKey != ugcUpdateGateCommand.m_lUserKey)
            {
                //return error result
                actSetGateway?.Invoke(false, m_strServerErrorResponse);
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

                yield break;
            }

            actSetGateway?.Invoke(true, string.Empty);
        }

        protected IEnumerator InternalSearchForGateway(string strGatewayDetails, Action<bool, string> actSearchCallback)
        {
            //check for timeout 
            if (Random.Range(0.0f, 1.0f) < m_fTimeOutChance)
            {
                yield return new WaitForSeconds(m_fTimeOutTime);

                actSearchCallback?.Invoke(false, m_strTimeOutResponse);

                yield break;
            }

            yield return new WaitForSeconds(m_fLatncy);

            //check if user could not be created due to conflicts / bad connection or other conflicts
            if (Random.Range(0.0f, 1.0f) < m_fActionErrorChance)
            {
                //return error result
                actSearchCallback?.Invoke(false, m_strServerErrorResponse);

                yield break;
            }

            GetGatewayRequest gwrRequest = JsonUtility.FromJson<GetGatewayRequest>(strGatewayDetails);

            Gateway? gtwGate = m_fdbFakeDatabase.SearchForGateway(gwrRequest.m_lGameType,gwrRequest.m_lFlags);

            if (gtwGate.HasValue == false)
            {
                actSearchCallback?.Invoke(false, m_strItemDoesNoteExistResponse);

                yield break;
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
        }

        protected IEnumerator InternalSearchForGatewayList(string strGatewayDetails, Action<bool, string> actSearchCallback)
        {
            //check for timeout 
            if (Random.Range(0.0f, 1.0f) < m_fTimeOutChance)
            {
                yield return new WaitForSeconds(m_fTimeOutTime);

                actSearchCallback?.Invoke(false, m_strTimeOutResponse);

                yield break;
            }

            yield return new WaitForSeconds(m_fLatncy);

            //check if user could not be created due to conflicts / bad connection or other conflicts
            if (Random.Range(0.0f, 1.0f) < m_fActionErrorChance)
            {
                //return error result
                actSearchCallback?.Invoke(false, m_strServerErrorResponse);

                yield break;
            }

            GetGatewayRequest gwrRequest = new GetGatewayRequest();

            //get user id
            try
            {
                gwrRequest = JsonUtility.FromJson<GetGatewayRequest>(strGatewayDetails);
            }
            catch
            {
                Debug.LogError("bad gate request json message, recieved: " + strGatewayDetails + " but was expecting something like: " + JsonUtility.ToJson(gwrRequest));    
            }

            Gateway[] gtwGate = m_fdbFakeDatabase.SearchForGatewayList(gwrRequest.m_lGameType, gwrRequest.m_lFlags);

            if (gtwGate.Length == 0)
            {
                actSearchCallback?.Invoke(false, m_strItemDoesNoteExistResponse);

                yield break;
            }

            GatewayReturnDetails[] sgrReturnValue = new GatewayReturnDetails[gtwGate.Length];

            for(int i = 0; i < gtwGate.Length; i++)
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
        }

    }
}