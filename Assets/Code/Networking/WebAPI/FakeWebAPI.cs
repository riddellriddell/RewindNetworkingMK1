﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

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

            public List<UserIDCredentialsPair> m_uicUserIDCredentialsPairs = new List<UserIDCredentialsPair>();
            public Dictionary<long, UserMessages> m_umsUserMessages = new Dictionary<long, UserMessages>();
            public Dictionary<long, Gateway> m_gtwGateways = new Dictionary<long, Gateway>();


            //gets the id for the passed in identifier or returns long min value if not found
            public long? GetUserIDWithCredentials(string strLoginCredentials)
            {
                foreach (UserIDCredentialsPair uicPair in m_uicUserIDCredentialsPairs)
                {
                    if (uicPair.m_dliDeviceLoginIdentifier.m_strLoginCredentials == strLoginCredentials)
                    {
                        return uicPair.m_lAccountID;
                    }
                }

                return null;
            }

            //create a new user account and return the ID
            public long? CreateUserWithCredentialsAndReturnID(string strLoginCredentials)
            {
                //check if user already exists with id
                if (DoesIdentifierExist(strLoginCredentials))
                {
                    //cant have 2 id's with the same login credentials
                    return null;
                }

                long lNewID = -1;
                bool isUnique = false;

                //repeate untill unique id found
                while (isUnique == false)
                {
                    //create unique id
                    lNewID++;

                    isUnique = true;

                    //loop through all existing accounts
                    foreach (UserIDCredentialsPair uicPair in m_uicUserIDCredentialsPairs)
                    {
                        //check if id is unique
                        if (uicPair.m_lAccountID == lNewID)
                        {
                            //move on to next id                            
                            isUnique = false;
                            break;
                        }
                    }
                }

                //add new account to db
                m_uicUserIDCredentialsPairs.Add(new UserIDCredentialsPair()
                {
                    m_lAccountID = lNewID,
                    m_dliDeviceLoginIdentifier = new DeviceLoginIdentifier() { m_strLoginCredentials = strLoginCredentials }
                });

                //add new message post box for player
                m_umsUserMessages.Add(lNewID, new UserMessages() { m_lAccountID = lNewID, m_umUserMessages = new UserMessage[0] });

                return lNewID;
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
                    TimeSpan tspTimeSpan = DateTime.UtcNow - new DateTime(mesMessage.m_lTimeOfMessage);

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
                    m_lTimeOfMessage = DateTime.UtcNow.Ticks,
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
            public bool SetGateway(long lUserID, int iStatus, int iRemainingSlots)
            {
                Gateway gtwNewGate = new Gateway()
                {
                    m_lOwningPlayerId = lUserID,
                    m_lTimeOfLastUpdate = DateTime.UtcNow.Ticks,
                    m_sstSimStatus = new SimStatus()
                    {
                        m_iRemainingSlots = iRemainingSlots,
                        m_iSimStatus = iStatus
                    }
                };

                m_gtwGateways[lUserID] = gtwNewGate;

                return true;
            }

            //search for gateway
            public Gateway? SearchForGateway(long lUserID)
            {
                RemoveOldGates();

                foreach (Gateway gtwGate in m_gtwGateways.Values)
                {
                    //check if not currently owned
                    if (gtwGate.m_lOwningPlayerId == lUserID)
                    {
                        continue;
                    }

                    //check if in lobby state 
                    if (gtwGate.m_sstSimStatus.m_iSimStatus != (int)SimStatus.State.Lobby)
                    {
                        continue;
                    }

                    //check if there are empty player slots
                    if (gtwGate.m_sstSimStatus.m_iRemainingSlots <= 0)
                    {
                        continue;
                    }

                    return gtwGate;
                }

                //no gate found
                return null;
            }

            //check if string identifier already exists in user id list
            protected bool DoesIdentifierExist(string strLoginCredentials)
            {
                foreach (UserIDCredentialsPair uicPair in m_uicUserIDCredentialsPairs)
                {
                    if (uicPair.m_dliDeviceLoginIdentifier.m_strLoginCredentials == strLoginCredentials)
                    {
                        return true;
                    }
                }

                return false;
            }

            //remove old gates that are nolonger in use 
            protected void RemoveOldGates()
            {
                List<long> lGatesToRemove = new List<long>();

                foreach (Gateway gtwGate in m_gtwGateways.Values)
                {
                    TimeSpan tspTimeSinceLastUpdate = DateTime.UtcNow - new DateTime(gtwGate.m_lTimeOfLastUpdate);

                    if (tspTimeSinceLastUpdate.Seconds > s_fGatewayTimeOut
                        || gtwGate.m_sstSimStatus.m_iSimStatus == (int)SimStatus.State.Broken
                        || gtwGate.m_sstSimStatus.m_iSimStatus == (int)SimStatus.State.Closed)
                    {
                        lGatesToRemove.Add(gtwGate.m_lOwningPlayerId);
                    }
                }

                foreach (long lTargetToRemove in lGatesToRemove)
                {
                    m_gtwGateways.Remove(lTargetToRemove);
                }
            }
        }

        public static FakeWebAPI Instance { get; private set; } = null;

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

        /// <summary>
        /// create a user on the server
        /// </summary>
        /// <param name="strLoginCredentials"></param>
        /// <param name="cucCreateUserCallback"></param>
        public void CreateUserWithLoginCredentials(string strLoginCredentials, Action<bool, string> actCreateUserCallback)
        {
            StartCoroutine(InternalCreateUser(strLoginCredentials, actCreateUserCallback));
        }

        public void GetUserWithLoginCredentials(string strLoginCredentials, Action<bool, string> actGetUserCallback)
        {
            StartCoroutine(InternalGetUserWithLoginCredentials(strLoginCredentials, actGetUserCallback));
        }

        public void GetDeleteUserMessages(string strIDOfUser, Action<bool, string> actGetMessagesCallback)
        {
            StartCoroutine(InternalGetDeleteUserMessages(strIDOfUser, actGetMessagesCallback));
        }

        public void AddNewMessage(string strNewMessageDetails, Action<bool, string> actAddMessageCallback)
        {
            StartCoroutine(InternalAddNewMessage(strNewMessageDetails, actAddMessageCallback));
        }

        public void SetGateway(string strSetGatewayCommand, Action<bool, string> actGatewayUpdateCallback)
        {
            StartCoroutine(InternalSetGateway(strSetGatewayCommand, actGatewayUpdateCallback));
        }

        public void SearchForGateway(string strUserID, Action<bool, string> actSearchForGateCallback)
        {
            StartCoroutine(InternalSearchForGateway(strUserID, actSearchForGateCallback));
        }

        protected IEnumerator InternalCreateUser(string strLoginCredentials, Action<bool, string> actCreateUserCallback)
        {
            //check for timeout 
            if (Random.Range(0, 1) < m_fTimeOutChance)
            {
                yield return new WaitForSeconds(m_fTimeOutTime);

                actCreateUserCallback?.Invoke(false, m_strTimeOutResponse);

                yield break;
            }

            yield return new WaitForSeconds(m_fLatncy);

            //check if user could not be created due to conflicts / bad connection or other conflicts
            if (Random.Range(0, 1) < m_fActionErrorChance)
            {
                //return error result
                actCreateUserCallback?.Invoke(false, m_strServerErrorResponse);
                yield break;
            }

            //create user
            long? lUserID = m_fdbFakeDatabase.CreateUserWithCredentialsAndReturnID(strLoginCredentials);

            if (lUserID.HasValue == false)
            {
                actCreateUserCallback?.Invoke(false, m_strDoNotHavePermissionResponse);

                yield break;
            }

            //serialize response from db
            string strResponse = lUserID.Value.ToString();

            Debug.Log($"Created user account with credentials: {strLoginCredentials} returned : {strResponse} ");

            //return success
            actCreateUserCallback?.Invoke(true, strResponse);
        }

        protected IEnumerator InternalGetUserWithLoginCredentials(string strLoginCredentials, Action<bool, string> actGetUserCallback)
        {
            //check for timeout 
            if (Random.Range(0, 1) < m_fTimeOutChance)
            {
                yield return new WaitForSeconds(m_fTimeOutTime);

                actGetUserCallback?.Invoke(false, m_strTimeOutResponse);

                yield break;
            }

            yield return new WaitForSeconds(m_fLatncy);

            //check if user could not be created due to conflicts / bad connection or other conflicts
            if (Random.Range(0, 1) < m_fActionErrorChance)
            {
                //return error result
                actGetUserCallback?.Invoke(false, m_strServerErrorResponse);

                yield break;
            }

            //try get user
            long? lUserID = m_fdbFakeDatabase.GetUserIDWithCredentials(strLoginCredentials);

            if (lUserID.HasValue == false)
            {
                actGetUserCallback?.Invoke(false, m_strItemDoesNoteExistResponse);

                yield break;
            }

            //serialize response from db
            string strResponse = lUserID.Value.ToString();

            Debug.Log($"get user account with credentials: {strLoginCredentials} returned : {strResponse} ");

            //return success
            actGetUserCallback?.Invoke(true, strResponse);
        }

        protected IEnumerator InternalGetDeleteUserMessages(string strUserDetails, Action<bool, string> actGetMessagesCallback)
        {
            //check for timeout 
            if (Random.Range(0, 1) < m_fTimeOutChance)
            {
                yield return new WaitForSeconds(m_fTimeOutTime);

                actGetMessagesCallback?.Invoke(false, m_strTimeOutResponse);

                yield break;
            }

            yield return new WaitForSeconds(m_fLatncy);

            //check if user could not be created due to conflicts / bad connection or other conflicts
            if (Random.Range(0, 1) < m_fActionErrorChance)
            {
                //return error result
                actGetMessagesCallback?.Invoke(false, m_strServerErrorResponse);

                yield break;
            }

            long lUserID = long.MinValue;

            //try and convert user details to long
            if (long.TryParse(strUserDetails, out lUserID) == false)
            {
                Debug.LogError($"Failed to parse user id : {strUserDetails}");

                //return error result
                actGetMessagesCallback?.Invoke(false, m_strServerErrorResponse);

                yield break;
            }

            List<UserMessage> mesMessages = m_fdbFakeDatabase.GetDeleteUserMessages(lUserID);

            GetMessageReturn gmrReturn = new GetMessageReturn()
            {
                m_usmUserMessages = mesMessages.ToArray()
            };

            //serialize result 
            string strResult = JsonUtility.ToJson(gmrReturn);

            Debug.Log($"Get Delete Messages with ID: {strUserDetails} returned: {strResult}");

            actGetMessagesCallback?.Invoke(true, strResult);
        }

        protected IEnumerator InternalAddNewMessage(string strNewMessageDetails, Action<bool, string> actSendMessageCallback)
        {
            //check for timeout 
            if (Random.Range(0, 1) < m_fTimeOutChance)
            {
                yield return new WaitForSeconds(m_fTimeOutTime);

                actSendMessageCallback?.Invoke(false, m_strTimeOutResponse);

                yield break;
            }

            yield return new WaitForSeconds(m_fLatncy);

            //check if user could not be created due to conflicts / bad connection or other conflicts
            if (Random.Range(0, 1) < m_fActionErrorChance)
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
            if (Random.Range(0, 1) < m_fTimeOutChance)
            {
                yield return new WaitForSeconds(m_fTimeOutTime);

                actSetGateway?.Invoke(false, m_strTimeOutResponse);

                yield break;
            }

            yield return new WaitForSeconds(m_fLatncy);

            //check if user could not be created due to conflicts / bad connection or other conflicts
            if (Random.Range(0, 1) < m_fActionErrorChance)
            {
                //return error result
                actSetGateway?.Invoke(false, m_strServerErrorResponse);

                yield break;
            }

            //deserialize gateway changes
            SetGatewayCommand ugcUpdateGateCommand = JsonUtility.FromJson<SetGatewayCommand>(strSetGatewayCommand);

            //try and find the target gateway
            if (m_fdbFakeDatabase.SetGateway(ugcUpdateGateCommand.m_lOwningPlayerId, ugcUpdateGateCommand.m_sstStatus.m_iSimStatus, ugcUpdateGateCommand.m_sstStatus.m_iRemainingSlots) == false)
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
            if (Random.Range(0, 1) < m_fTimeOutChance)
            {
                yield return new WaitForSeconds(m_fTimeOutTime);

                actSearchCallback?.Invoke(false, m_strTimeOutResponse);

                yield break;
            }

            yield return new WaitForSeconds(m_fLatncy);

            //check if user could not be created due to conflicts / bad connection or other conflicts
            if (Random.Range(0, 1) < m_fActionErrorChance)
            {
                //return error result
                actSearchCallback?.Invoke(false, m_strServerErrorResponse);

                yield break;
            }

            long lUserID = long.MinValue;

            //try and convert user details to long
            if (long.TryParse(strGatewayDetails, out lUserID) == false)
            {
                //return error result
                actSearchCallback?.Invoke(false, m_strServerErrorResponse);

                yield break;
            }

            Gateway? gtwGate = m_fdbFakeDatabase.SearchForGateway(lUserID);

            if (gtwGate.HasValue == false)
            {
                actSearchCallback?.Invoke(false, m_strItemDoesNoteExistResponse);

                yield break;
            }

            string strGateReturnValue = JsonUtility.ToJson(gtwGate.Value);

            actSearchCallback?.Invoke(true, strGateReturnValue);
        }

    }
}