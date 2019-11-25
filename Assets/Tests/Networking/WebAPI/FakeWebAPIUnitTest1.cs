using Networking;
using NUnit.Framework;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.TestTools;
using Random = UnityEngine.Random;

namespace Tests
{
    public class FakeWebAPIUnitTest1
    {
        // A Test behaves as an ordinary method
        [Test]
        public void FakeWebAPIUnitTest1SimplePasses()
        {
            // Use the Assert class to test conditions
        }

        // A UnityTest behaves like a coroutine in Play Mode. In Edit Mode you can use
        // `yield return null;` to skip a frame.
        [UnityTest]
        public IEnumerator FakeWebAPIUnitTest1TestCreateUser()
        {

            //setup fake API
            SetupFakeWebAPIForSuccess();

            yield return null;

            string strLoginCreds = $"LogInCred{Random.Range(0, int.MaxValue)}";

            bool bFinished = false;

            bool bSuccess = false;

            string strReturnValue = "";

            Action<bool, string> actCallback = (bCallbackSucceed, strCallbackValue) => 
            {
                bSuccess = bCallbackSucceed;
                strReturnValue = strCallbackValue;
                bFinished = true;
            } ;

            FakeWebAPI.Instance.CreateUserWithLoginCredentials(strLoginCreds, actCallback);

            while(bFinished == false)
            {
                yield return null;
            }

            //check if event was called
            Debug.Assert(bSuccess == true, $"Failed to create user with credentials {strLoginCreds} error returned result {strReturnValue}");

            Debug.Log($"Create User succeded with return value {strReturnValue}");

            // Use the Assert class to test conditions.
            // Use yield to skip a frame.
            yield return null;
        }

        protected void SetupFakeWebAPIForSuccess()
        {
            GameObject objFakeAPI = new GameObject();
            FakeWebAPI fwaApi = objFakeAPI.AddComponent<FakeWebAPI>();

            fwaApi.Start();
            fwaApi.m_fTimeOutChance = 0;
            fwaApi.m_fActionErrorChance = 0;
        }

        protected void SetupFakeWebAPIForFailure()
        {
            GameObject objFakeAPI = new GameObject();
            FakeWebAPI fwaApi = objFakeAPI.AddComponent<FakeWebAPI>();

            fwaApi.Start();
            fwaApi.m_fTimeOutChance = 1;
            fwaApi.m_fActionErrorChance = 1;
        }
    }
}
