using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Networking
{
    public class ConnectionUnitTest : MonoBehaviour
    {
        public float m_fTickUpdateRate = 0.1f;
        
        public bool m_bResetConnectionTick;

        public float m_fSendRate = 0.5f;
        public int m_iMaxNumberOfPacketsToSend = 5;

        public InternetConnectionSimulator m_icsConnectionSim;

        private float m_fTimeUntilNextTick;
        private float m_fTimeUntilNextMessage;

        private Connection m_conConnection1;
        private Connection m_conConnection2;

        private byte m_bLastInputSent;
        private int m_iTick;

        private Dictionary<byte, int> m_dicInputCompare;
                
        // Use this for initialization
        void Start()
        {
            m_conConnection1 = new Connection(1);
            m_conConnection1.m_icsConnectionSim = m_icsConnectionSim;

            m_conConnection2 = new Connection(2);
            m_conConnection2.m_icsConnectionSim = m_icsConnectionSim;

            m_conConnection1.m_conConnectionTarget = m_conConnection2;
            m_conConnection2.m_conConnectionTarget = m_conConnection1;

            m_dicInputCompare = new Dictionary<byte, int>();
        }

        // Update is called once per frame
        void Update()
        {
            m_fTimeUntilNextTick -= Time.deltaTime;
            m_fTimeUntilNextMessage -= Time.deltaTime;

            if (m_fTimeUntilNextTick < 0)
            {
                m_fTimeUntilNextTick = m_fTickUpdateRate;

                //increment tick
                m_iTick++;



                if (m_fTimeUntilNextMessage < 0)
                {
                    m_fTimeUntilNextMessage += m_fSendRate;

                    //int iNumberOfPacketsToSend = Random.Range(0, m_iMaxNumberOfPacketsToSend);
                    //
                    //for (int i = 0; i < iNumberOfPacketsToSend; i++)
                    //{
                    SendInput();
                    //}
                }

                if (m_bResetConnectionTick)
                {
                    m_bResetConnectionTick = false;

                    SendTickReset();
                }

                m_conConnection1.UpdateConnection(m_iTick);
                m_conConnection2.UpdateConnection(m_iTick);

            }
            GetReceivedMessages();
        }

        private void SendInput()
        {
            m_bLastInputSent = (byte)((m_bLastInputSent + 1) % byte.MaxValue);

            m_conConnection1.QueuePacketToSend(new InputPacket(m_bLastInputSent, m_iTick));
            m_dicInputCompare[m_bLastInputSent] = m_iTick;
        }

        private void CompareTickPakets(InputPacket pktPacket)
        {
            int iTickForPacket = 0;

            if(m_dicInputCompare.TryGetValue(pktPacket.m_bInput, out iTickForPacket))
            {
                if(iTickForPacket != pktPacket.m_iTick)
                {
                    Debug.Log("Tick Missmatch for input " + pktPacket.m_bInput + " Correct Tick:" + iTickForPacket + " Decoded Tick:" + pktPacket.m_iTick);
                }
            }
        }

        private void SendTickReset()
        {
            m_conConnection1.QueuePacketToSend(new ResetTickCountPacket());
            m_iTick = 0;
        }

        private void GetReceivedMessages()
        {
            for (int i = 0; i < m_conConnection1.m_pakReceivedPackets.Count; i++)
            {
                Packet pktPacket = m_conConnection1.m_pakReceivedPackets.Dequeue();

                if (pktPacket is ResetTickCountPacket)
                {
                    Debug.Log("Con1 Tick Reset :--------------------------------------");

                }

                if (pktPacket is InputPacket)
                {
                    Debug.Log("Con1 Packet:" + (pktPacket as InputPacket).m_bInput + " with tick:" + (pktPacket as InputPacket).m_iTick + " received");
                    CompareTickPakets(pktPacket as InputPacket);
                }

                if (pktPacket is PingPacket)
                {
                    Debug.Log("Con1 Ping Packet received");
                }
            }

            for (int i = 0; i < m_conConnection2.m_pakReceivedPackets.Count; i++)
            {
                Packet pktPacket = m_conConnection2.m_pakReceivedPackets.Dequeue();

                if(pktPacket is ResetTickCountPacket)
                {
                    Debug.Log("Con2 Tick Reset :--------------------------------------");
                    m_iTick = 0;
                }

                if (pktPacket is InputPacket)
                {
                    Debug.Log("Con2 Packet:" + (pktPacket as InputPacket).m_bInput + " with tick:" + (pktPacket as InputPacket).m_iTick + " received");
                    CompareTickPakets(pktPacket as InputPacket);
                }

                if (pktPacket is PingPacket)
                {
                    Debug.Log("Con2 Ping Packet received");
                }
            }
        }
    }
}