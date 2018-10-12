using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Networking
{
    public class ConnectionUnitTest : MonoBehaviour
    {

        public float m_fSendRate = 0.5f;

        public InternetConnectionSimulator m_icsConnectionSim;

        private float m_fTimeUntilNextMessage;

        private Connection m_conConnection1;
        private Connection m_conConnection2;

        private byte m_bLastInputSent;
        private int m_iTick;

        // Use this for initialization
        void Start()
        {
            m_conConnection1 = new Connection(1);
            m_conConnection1.m_icsConnectionSim = m_icsConnectionSim;

            m_conConnection2 = new Connection(2);
            m_conConnection2.m_icsConnectionSim = m_icsConnectionSim;

            m_conConnection1.m_conConnectionTarget = m_conConnection2;
            m_conConnection2.m_conConnectionTarget = m_conConnection1;
        }

        // Update is called once per frame
        void Update()
        {
            //increment tick
            m_iTick++;

            m_fTimeUntilNextMessage -= Time.deltaTime;

            if (m_fTimeUntilNextMessage < 0)
            {
                m_fTimeUntilNextMessage += m_fSendRate;

                SendInput();
            }

            m_conConnection1.UpdateConnection(m_iTick);
            m_conConnection2.UpdateConnection(m_iTick);

            GetReceivedMessages();
        }

        private void SendInput()
        {
            m_bLastInputSent = (byte)((m_bLastInputSent + 1) % byte.MaxValue);

            m_conConnection1.QueuePacketToSend(new InputPacket(m_bLastInputSent, m_iTick));
        }

        private void GetReceivedMessages()
        {
            for (int i = 0; i < m_conConnection1.m_pakReceivedPackets.Count; i++)
            {
                Packet pktPacket = m_conConnection1.m_pakReceivedPackets.Dequeue();

                if (pktPacket is InputPacket)
                {
                    Debug.Log("Connection 1 Packet With ID :" + (pktPacket as InputPacket).input + " received");
                }

                if (pktPacket is PingPacket)
                {
                    Debug.Log("Connection 1 Ping Packet received");
                }
            }

            for (int i = 0; i < m_conConnection2.m_pakReceivedPackets.Count; i++)
            {
                Packet pktPacket = m_conConnection2.m_pakReceivedPackets.Dequeue();

                if (pktPacket is InputPacket)
                {
                    Debug.Log("Connection 2 Packet With ID :" + (pktPacket as InputPacket).input + " received");
                }

                if (pktPacket is PingPacket)
                {
                    Debug.Log("Connection 2 Ping Packet received");
                }
            }
        }
    }
}