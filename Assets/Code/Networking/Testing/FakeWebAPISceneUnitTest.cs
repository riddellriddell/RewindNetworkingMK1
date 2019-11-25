using Networking;
using System;
using System.Collections;
using UnityEngine;
using Random = UnityEngine.Random;

public class FakeWebAPISceneUnitTest : MonoBehaviour
{
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

        FakeWebAPI.Instance.CreateUserWithLoginCredentials(strUser1LoginCreds, actCallback1);

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

        FakeWebAPI.Instance.CreateUserWithLoginCredentials(strUser2LoginCreds, actCallback2);

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

        long lUser1ID = 0;

        long lUser2ID = 1;

        bool bLoop = true;
        bool bActionResult = false;
        string strActionValue = "";

        // get user 1 messages

        Action<bool, string> actGetMessagesCallback = (bool bDidSucceed, string strReturnValue) =>
         {
             bLoop = false;
             bActionResult = bDidSucceed;
             strActionValue = strReturnValue;
         };

        Debug.Log($"running inital get messages test on user {lUser1ID.ToString()}");
        FakeWebAPI.Instance.GetDeleteUserMessages(lUser1ID.ToString(), actGetMessagesCallback);

        while (bLoop)
        {
            yield return null;
        }

        Debug.Assert(bActionResult, $"Get delete messages for user {lUser1ID.ToString()} Failed with return value {strActionValue}");

        //user 2 send messages 
        SendMessageCommand smcSendMessageCommand = new SendMessageCommand()
        {
            m_iType = 0,
            m_lFromID = lUser2ID,
            m_lToID = lUser1ID,
            m_strMessage = "Test Message From User 2"

        };

        string strMessageCommand = JsonUtility.ToJson(smcSendMessageCommand);

        bLoop = true;

        Debug.Log($"Sending Message: {strMessageCommand} to user {lUser1ID.ToString()}");
        FakeWebAPI.Instance.AddNewMessage(strMessageCommand, actGetMessagesCallback);

        while (bLoop)
        {
            yield return null;
        }

        Debug.Assert(bActionResult, $"Send message command: {strMessageCommand} Failed with return value {strActionValue}");

        Debug.Log($"Message: {strMessageCommand} sent successfully");

        // get user 1 messages again
        bLoop = true;
        Debug.Log($" get messages test on user {lUser1ID.ToString()} again to get new message");
        FakeWebAPI.Instance.GetDeleteUserMessages(lUser1ID.ToString(), actGetMessagesCallback);

        while (bLoop)
        {
            yield return null;
        }

        Debug.Assert(bActionResult, $"Get messages for user: {lUser1ID.ToString()} Failed with return value {strActionValue}");

        Debug.Log($"User:{lUser1ID.ToString()} Messages: {strActionValue} retrieved successfully");

        GetMessageReturn gmrMessageReturn = JsonUtility.FromJson<GetMessageReturn>(strActionValue);

        Debug.Assert(
            gmrMessageReturn.m_usmUserMessages.Length > 0 && gmrMessageReturn.m_usmUserMessages[0].m_strMessage == smcSendMessageCommand.m_strMessage,
            "Returned message did not match sent message"
            );

        Debug.Log($"User:{lUser1ID.ToString()} recieved Message: {gmrMessageReturn.m_usmUserMessages[0].m_strMessage} successfully");
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

        long lUser1ID = 0;

        Debug.Log($"create gateway for user: {lUser1ID.ToString()}");

        bLoop = true;

        FakeWebAPI.Instance.CreateGateway(lUser1ID.ToString(),actWebAPICallback);

        while(bLoop)
        {
            yield return null;
        }

        Debug.Assert(bActionResult, $"Failed to create gateway for user: {lUser1ID.ToString()}");

        Debug.Log($"Gateway for user: {lUser1ID.ToString()} created");

        //------- check that use cant have 2 active gateways --------------------------

        Debug.Log($"trying to create another gateway for user: {lUser1ID.ToString()}");

        bLoop = true;

        FakeWebAPI.Instance.CreateGateway(lUser1ID.ToString(), actWebAPICallback);

        while (bLoop)
        {
            yield return null;
        }

        Debug.Assert(!bActionResult, $"should not have been able to create a seccond gateway for user: {lUser1ID.ToString()}");

        Debug.Log($"seccond gateway for user: {lUser1ID.ToString()} not created ");

        //----------- Update Gateway --------------------------------------------------


        UpdateGatewayCommand ugwUpdateGatewayCommand = new UpdateGatewayCommand()
        {
            m_iRemainingSlots = 2,
            m_iStatus = (int)SimStatus.State.Lobby,
            m_lOwningPlayerId = lUser1ID
        };

        string strUpdateGatewayCommand = JsonUtility.ToJson(ugwUpdateGatewayCommand);

        bLoop = true;


        Debug.Log($"trying to update gateway for user: {lUser1ID.ToString()} with command: {strUpdateGatewayCommand}" );

        FakeWebAPI.Instance.UpdateGateway(strUpdateGatewayCommand, actWebAPICallback);

        while (bLoop)
        {
            yield return null;
        }

        Debug.Assert(bActionResult, $"Update Gateway command: {strUpdateGatewayCommand} Failed with result {strActionValue}");

        Debug.Log($"gateway for user: {lUser1ID.ToString()} updated ");



        //----------- search for gateway ----------------------------------------------

        long lUser2ID = 1;

        bLoop = true;

        Debug.Log($"Searching for gateway for user: {lUser2ID.ToString()}");

        FakeWebAPI.Instance.SearchForGateway(lUser2ID.ToString(), actWebAPICallback);

        while (bLoop)
        {
            yield return null;
        }

        Debug.Assert(bActionResult && strActionValue != string.Empty, $"Failed to find gateway for user {lUser2ID.ToString()} with result {strActionValue}");

        Debug.Log($"gateway : {strActionValue} found for user: {lUser1ID.ToString()} ");
    }
}
