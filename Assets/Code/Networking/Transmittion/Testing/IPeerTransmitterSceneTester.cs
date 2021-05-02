#if !UNITY_WEBGL || UNITY_EDITOR_WIN

using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Networking
{
    public class IPeerTransmitterSceneTester : MonoBehaviour
    {
        public bool m_bUseFakeTransmitters = true;

        public IPeerTransmitter m_ptrTransmitter1;

        public IPeerTransmitter m_ptrTransmitter2;

        public int m_iConnectionsMade = 0;

        public int m_iDataRecieved = 0;

        // Start is called before the first frame update
        void Start()
        {
            StartCoroutine(Test());
        }

        private void OnDestroy()
        {
            m_ptrTransmitter1?.OnCleanup();
            m_ptrTransmitter2?.OnCleanup();
        }

        // Update is called once per frame
        void Update()
        {
         
        }
        public IEnumerator Test()
        {
            yield return null;

            Debug.Log("Peer Transmitter Test Started");

            if (m_bUseFakeTransmitters)
            {
                SetupFakeTransmitters();
            }
            else
            {
                SetupWebTRCTransmitters();
            }

            m_ptrTransmitter1.OnNegotiationMessageCreated += OnTransmitter1NegotiationMessage;
            m_ptrTransmitter2.OnNegotiationMessageCreated += OnTransmitter2NegotiationMessage;

            m_ptrTransmitter1.OnConnectionEstablished += OnConnection1Established;
            m_ptrTransmitter2.OnConnectionEstablished += OnConnection2Established;

            m_ptrTransmitter1.OnDataReceive += OnConnection1DataRecieved;
            m_ptrTransmitter2.OnDataReceive += OnConnection2DataRecieved;

            Debug.Log("Starting connection negotiation");

            m_ptrTransmitter1.StartNegotiation();

            while (m_iConnectionsMade < 2)
            {
                yield return null;
            }

            Debug.Log("Both connections made test sending data");
            byte[] bTestData = Encoding.ASCII.GetBytes("TestMessage1");
            m_ptrTransmitter1.SentData(bTestData);

            while (m_iDataRecieved < 1)
            {
                yield return null;
            }

            Debug.Log("Sending message the other way");

            bTestData = Encoding.ASCII.GetBytes("TestMessage1");
            m_ptrTransmitter2.SentData(bTestData);

            while (m_iDataRecieved < 2)
            {
                yield return null;
            }

            Debug.Log("TestCompleted");

        }

        protected void OnTransmitter1NegotiationMessage(string strMessage)
        {
            Debug.Log($"Transmitter1 sending negotiation message: {strMessage}");

            m_ptrTransmitter2.ProcessNegotiationMessage(strMessage);
        }

        protected void OnTransmitter2NegotiationMessage(string strMessage)
        {
            Debug.Log($"Transmitter2 sending negotiation message: {strMessage}");
            m_ptrTransmitter1.ProcessNegotiationMessage(strMessage);
        }

        protected void OnConnection1Established()
        {
            Debug.Log("Transmitter1 connection established");
            m_iConnectionsMade++;
        }

        protected void OnConnection2Established()
        {
            Debug.Log("Transmitter2 connection established");
            m_iConnectionsMade++;
        }

        protected void OnConnection1DataRecieved(byte[] bData)
        {
            Debug.Log($"Connection1 recieved Data {Encoding.ASCII.GetString(bData)}");
            m_iDataRecieved++;
        }

        protected void OnConnection2DataRecieved(byte[] bData)
        {
            Debug.Log($"Connection2 recieved Data {Encoding.ASCII.GetString(bData)}");
            m_iDataRecieved++;
        }


        protected void SetupFakeTransmitters()
        {
            m_ptrTransmitter1 = new FakeWebRTCTransmitter();
            m_ptrTransmitter2 = new FakeWebRTCTransmitter();
        }

        protected void SetupWebTRCTransmitters()
        {
            m_ptrTransmitter1 = new WebRTCTransmitter(this);
            m_ptrTransmitter2 = new WebRTCTransmitter(this);
        }
    }
}
#endif