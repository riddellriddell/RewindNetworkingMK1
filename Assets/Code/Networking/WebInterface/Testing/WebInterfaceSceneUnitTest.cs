using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Networking
{
    public class WebInterfaceSceneUnitTest : MonoBehaviour
    {
#if UNITY_EDITOR

        public bool m_bUseFakeWebApi = true;

        protected WebInterface m_winWebInterface1;
        protected WebInterface m_winWebInterface2;

        // Start is called before the first frame update
        void Start()
        {
            //create web interface 
            m_winWebInterface1 = new WebInterface(this);
            m_winWebInterface2 = new WebInterface(this);

            m_winWebInterface1.TestLocally = m_bUseFakeWebApi;
            m_winWebInterface2.TestLocally = m_bUseFakeWebApi;

            StartCoroutine(TestCoroutine());
        }

        private void Update()
        {
            m_winWebInterface1.UpdateCommunication();
            m_winWebInterface2.UpdateCommunication();
        }

        public IEnumerator TestCoroutine()
        {
            yield return StartCoroutine(GetPlayerIDs());

            yield return StartCoroutine(SetupGateway());

            yield return StartCoroutine(FindGateway());

            yield return StartCoroutine(SendMessage());
        }

        public IEnumerator GetPlayerIDs()
        {
            yield return null;

            Debug.Log("Start getting player1 ID");

            m_winWebInterface1.GetPlayerID("UniqueID1");

            //wait to get first player id 
            while(
                m_winWebInterface1.PlayerIDCommunicationStatus.m_cmsStatus != WebInterface.WebAPICommunicationTracker.CommunctionStatus.Succedded &&
                m_winWebInterface1.PlayerIDCommunicationStatus.m_cmsStatus != WebInterface.WebAPICommunicationTracker.CommunctionStatus.Failed)
            {
                yield return null;
            }

            Debug.Log("Start getting player2 ID");

            m_winWebInterface2.GetPlayerID("UniqueID2");

            //wait to get seccond player id 
            while (
                m_winWebInterface2.PlayerIDCommunicationStatus.m_cmsStatus != WebInterface.WebAPICommunicationTracker.CommunctionStatus.Succedded &&
                m_winWebInterface2.PlayerIDCommunicationStatus.m_cmsStatus != WebInterface.WebAPICommunicationTracker.CommunctionStatus.Failed)
            {
                yield return null;
            }

            Debug.Assert(m_winWebInterface1.PlayerIDCommunicationStatus.m_cmsStatus == WebInterface.WebAPICommunicationTracker.CommunctionStatus.Succedded, " Web Interface 1 Failed to get player id");

            Debug.Assert(m_winWebInterface2.PlayerIDCommunicationStatus.m_cmsStatus == WebInterface.WebAPICommunicationTracker.CommunctionStatus.Succedded, " Web Interface 2 Failed to get player id");

            int iInterface1ConnectionAttempts = m_winWebInterface1.PlayerIDCommunicationStatus.m_iCommunicationAttemptNumber;
            int iInterface2ConnectionAttempts = m_winWebInterface2.PlayerIDCommunicationStatus.m_iCommunicationAttemptNumber;

            Debug.Log($"Interface 1 connected after {iInterface1ConnectionAttempts} connection attempts with id {m_winWebInterface1.UserID}");
            Debug.Log($"Interface 2 connected after {iInterface2ConnectionAttempts} connection attempts with id {m_winWebInterface2.UserID}");
        }

        public IEnumerator SetupGateway()
        {
            yield return null;

            Debug.Log("Starting gateway setup");

            m_winWebInterface1.SetGateway(new SimStatus() { m_iRemainingSlots = 2, m_iSimStatus = (int)SimStatus.State.Lobby });

            yield return new WaitForSeconds(5);

            Debug.Assert(m_winWebInterface1.SetGatewayStatus.m_cmsStatus != WebInterface.WebAPICommunicationTracker.CommunctionStatus.Failed, "Gateway setup failed");

            Debug.Log("gateway setup finished");
        }

        public IEnumerator FindGateway()
        {
            yield return null;

            Debug.Log("Starting search for gateway");

            m_winWebInterface2.SearchForGateway();

            //wait for gateway to be found
            while(m_winWebInterface2.ExternalGatewayCommunicationStatus.m_cmsStatus != WebInterface.WebAPICommunicationTracker.CommunctionStatus.Succedded)
            {
                yield return null;
            }

            Debug.Assert(m_winWebInterface2.ExternalGatewayCommunicationStatus.m_cmsStatus != WebInterface.WebAPICommunicationTracker.CommunctionStatus.Failed);

            Debug.Log($"Found Gateway {m_winWebInterface2.ExternalGateway.Value.m_lGateOwnerUserID.ToString()} ");
        }

        public IEnumerator SendMessage()
        {
            yield return null;

            Debug.Log("Starting sending message");

            m_winWebInterface2.SendMessage(m_winWebInterface2.ExternalGateway.Value.m_lGateOwnerUserID, 0, "Test Message");

            yield return new WaitForSeconds(5);

            Debug.Log("Start liseneing for message");
            m_winWebInterface1.StartGettingMessages();

            while(m_winWebInterface1.MessagesFromServer.Count == 0)
            {
                yield return null;
            }

            Debug.Log($" message: {m_winWebInterface1.MessagesFromServer.Peek().m_strMessage} recieved From: {m_winWebInterface1.MessagesFromServer.Peek().m_lFromUser}");
        }
               
#endif

    }
}
