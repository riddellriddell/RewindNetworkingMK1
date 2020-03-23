using System;
using System.Collections;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Networking
{
    public class FakeWebAPISceneUnitTest : MonoBehaviour
    {
#if UNITY_EDITOR

        // Start is called before the first frame update
        void Start()
        {
            StartCoroutine(RunAllTests());
        }

        public IEnumerator RunAllTests()
        {
            yield return StartCoroutine(CreateUserTest());

            yield return StartCoroutine(SendMessageTest());

            yield return StartCoroutine(CreateGatewayTest());
        }

        public IEnumerator CreateUserTest()
        {
            yield return null;

            string strUser1LoginCreds = $"LogInCred{Random.Range(0, int.MaxValue)}";

            bool bUser1Finished = false;

            bool bUser1Success = false;

            string strUser1ReturnValue = "";

            Action<bool, string> actCallback1 = (bCallbackSucceed, strCallbackValue) =>
            {
                bUser1Success = bCallbackSucceed;
                strUser1ReturnValue = strCallbackValue;
                bUser1Finished = true;
            };

            FakeWebAPI.Instance.GetUserWithLoginCredentials(strUser1LoginCreds, actCallback1);

            while (bUser1Finished == false)
            {
                yield return null;
            }

            //check if event was called
            Debug.Assert(bUser1Success == true, $"Failed to create user 1 with credentials {strUser1LoginCreds} error returned result {strUser1ReturnValue}");

            Debug.Log($"Create User 1 succeded with return value {strUser1ReturnValue}");

            //create a seccond user
            string strUser2LoginCreds = $"LogInCred{Random.Range(0, int.MaxValue)}";

            bool bUser2Finished = false;

            bool bUser2Success = false;

            string strUser2ReturnValue = "";

            Action<bool, string> actCallback2 = (bCallbackSucceed, strCallbackValue) =>
            {
                bUser2Success = bCallbackSucceed;
                strUser2ReturnValue = strCallbackValue;
                bUser2Finished = true;
            };

            FakeWebAPI.Instance.GetUserWithLoginCredentials(strUser2LoginCreds, actCallback2);

            while (bUser2Finished == false)
            {
                yield return null;
            }

            //check if event was called
            Debug.Assert(bUser2Success == true, $"Failed to create user 2 with credentials {strUser2LoginCreds} error returned result {strUser2ReturnValue}");

            Debug.Log($"Create User 2 succeded with return value {strUser2ReturnValue}");

            //try and get user 2

            bool bGetUser2Finished = false;

            bool bGetUser2Success = false;

            string strGetUser2ReturnValue = "";

            Action<bool, string> actCallback3 = (bCallbackSucceed, strCallbackValue) =>
            {
                bGetUser2Success = bCallbackSucceed;
                strGetUser2ReturnValue = strCallbackValue;
                bGetUser2Finished = true;
            };

            FakeWebAPI.Instance.GetUserWithLoginCredentials(strUser2LoginCreds, actCallback3);

            while (bGetUser2Finished == false)
            {
                yield return null;
            }

            //check if event was called
            Debug.Assert(bUser2Success == true, $"Failed to get user 2 with credentials {strUser2LoginCreds} error returned result {strGetUser2ReturnValue}");

            Debug.Log($"get User 2 succeded with return value {strGetUser2ReturnValue}");

            Debug.Assert(strGetUser2ReturnValue == strUser2ReturnValue, $"Value {strGetUser2ReturnValue} returned when getting user with creds: {strUser2LoginCreds} does not match value: {strUser2ReturnValue} returned on creation");

        }

        public IEnumerator SendMessageTest()
        {
            yield return null;

            Debug.Log("Starting send message Test");

            string strUser1UDID = "asdf";
            long strUser1ID = 0;
            long strUser1Key = 0;
            bool bFinishedGetUser1ID = false;


            string strUser2UDID = "fghj";
            long strUser2ID = 1;
            long strUser2Key = 1;
            bool bFinishedGetUser2ID = false;

            Action<bool, string> actOnGetUser1 = (bool bWasSuccess, string strUserDetails) =>
                 {
                     UserIDDetails uidUserDetails = JsonUtility.FromJson<UserIDDetails>(strUserDetails);
                     strUser1ID = uidUserDetails.m_lUserID;
                     strUser1Key = uidUserDetails.m_lUserKey;
                     bFinishedGetUser1ID = true;
                 };

            Action<bool, string> actOnGetUser2 = (bool bWasSuccess, string strUserDetails) =>
            {
                UserIDDetails uidUserDetails = JsonUtility.FromJson<UserIDDetails>(strUserDetails);
                strUser2ID = uidUserDetails.m_lUserID;
                strUser2Key = uidUserDetails.m_lUserKey;
                bFinishedGetUser2ID = true;
            };

            //create users
            FakeWebAPI.Instance.GetUserWithLoginCredentials(strUser1UDID, actOnGetUser1);
            FakeWebAPI.Instance.GetUserWithLoginCredentials(strUser2UDID, actOnGetUser2);

            //wait for users to be created
            while (bFinishedGetUser1ID == false || bFinishedGetUser2ID == false)
            {
                yield return null;
            }

            bool bLoop = true;
            bool bActionResult = false;
            string strActionValue = "";

            // get user 1 messages
            GetMessageRequest gmrGetMessageRequest = new GetMessageRequest()
            {
                m_lUserKey = strUser1Key,
                m_lUserID = strUser1ID
            };


            Action<bool, string> actGetMessagesCallback = (bool bDidSucceed, string strReturnValue) =>
             {
                 bLoop = false;
                 bActionResult = bDidSucceed;
                 strActionValue = strReturnValue;
             };
                        
            Debug.Log($"running inital get messages test on user {JsonUtility.ToJson(gmrGetMessageRequest) }");
            FakeWebAPI.Instance.GetDeleteUserMessages(JsonUtility.ToJson(gmrGetMessageRequest), actGetMessagesCallback);

            while (bLoop)
            {
                yield return null;
            }

            Debug.Assert(bActionResult, $"Get delete messages for user {strUser1ID} Failed with return value {strActionValue}");

            //user 2 send messages 
            SendMessageCommand smcSendMessageCommand = new SendMessageCommand()
            {
                m_iType = 0,
                m_lFromID = strUser2ID,
                m_lToID = strUser1ID,
                m_strMessage = "Test Message From User 2"

            };

            string strMessageCommand = JsonUtility.ToJson(smcSendMessageCommand);

            bLoop = true;

            Debug.Log($"Sending Message: {strMessageCommand} to user {strUser1ID.ToString()}");
            FakeWebAPI.Instance.AddNewMessage(strMessageCommand, actGetMessagesCallback);

            while (bLoop)
            {
                yield return null;
            }

            Debug.Assert(bActionResult, $"Send message command: {strMessageCommand} Failed with return value {strActionValue}");

            Debug.Log($"Message: {strMessageCommand} sent successfully");

            // get user 1 messages again
            bLoop = true;
            Debug.Log($" get messages test on user {JsonUtility.ToJson(gmrGetMessageRequest)} again to get new message");
            FakeWebAPI.Instance.GetDeleteUserMessages(JsonUtility.ToJson(gmrGetMessageRequest), actGetMessagesCallback);

            while (bLoop)
            {
                yield return null;
            }

            Debug.Assert(bActionResult, $"Get messages for user: {strUser1ID.ToString()} Failed with return value {strActionValue}");

            Debug.Log($"User:{strUser1ID.ToString()} Messages: {strActionValue} retrieved successfully");

            GetMessageReturn gmrMessageReturn = JsonUtility.FromJson<GetMessageReturn>(strActionValue);

            Debug.Assert(
                gmrMessageReturn.m_usmUserMessages.Length > 0 && gmrMessageReturn.m_usmUserMessages[0].m_strMessage == smcSendMessageCommand.m_strMessage,
                "Returned message did not match sent message"
                );

            Debug.Log($"User:{strUser1ID.ToString()} recieved Message: {gmrMessageReturn.m_usmUserMessages[0].m_strMessage} successfully");
        }

        public IEnumerator CreateGatewayTest()
        {
            yield return null;

            Debug.Log("Starting Gateway test");

            bool bLoop = true;
            bool bActionResult = false;
            string strActionValue = "";


            Action<bool, string> actWebAPICallback = (bool bDidSucceed, string strReturnValue) =>
            {
                bLoop = false;
                bActionResult = bDidSucceed;
                strActionValue = strReturnValue;
            };

            long strUser1ID = 0;
            long strUser1Key = 0;

            //----------- Set Gateway --------------------------------------------------


            SetGatewayCommand ugwUpdateGatewayCommand = new SetGatewayCommand()
            {

                m_staGameState = new SimStatus()
                {
                    m_iRemainingSlots = 2,
                    m_iSimStatus = (int)SimStatus.State.Lobby
                },
                m_lUserID = strUser1ID,
                m_lUserKey = strUser1Key
            };

            string strUpdateGatewayCommand = JsonUtility.ToJson(ugwUpdateGatewayCommand);

            bLoop = true;


            Debug.Log($"trying to update gateway for user: {strUser1ID} with command: {strUpdateGatewayCommand}");

            FakeWebAPI.Instance.SetGateway(strUpdateGatewayCommand, actWebAPICallback);

            while (bLoop)
            {
                yield return null;
            }

            Debug.Assert(bActionResult, $"Update Gateway command: {strUpdateGatewayCommand} Failed with result {strActionValue}");

            Debug.Log($"gateway for user: {strUser1ID} updated ");



            //----------- search for gateway ----------------------------------------------

            string strUser2ID = "fghj";

            bLoop = true;

            Debug.Log($"Searching for gateway for user: {strUser2ID}");

            FakeWebAPI.Instance.SearchForGateway(strUser2ID, actWebAPICallback);

            while (bLoop)
            {
                yield return null;
            }

            Debug.Assert(bActionResult && strActionValue != string.Empty, $"Failed to find gateway for user {strUser2ID.ToString()} with result {strActionValue}");

            Debug.Log($"gateway : {strActionValue} found for user: {strUser2ID.ToString()} ");
        }

#endif

    }
}