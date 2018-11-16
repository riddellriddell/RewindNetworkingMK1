using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Networking
{

    public class NetworkConnection : MonoBehaviour
    {

        public InternetConnectionSimulator m_icwConnectionSimulation;

        //all the connections 
        public List<Connection> m_conConnectionList = new List<Connection>();

        //the local id of the player
        public byte m_bPlayerID;

        //the unique id for this play
        public long m_lPlayerUniqueID;



        public delegate void PacketDataIn(byte bPlayerID, Packet pktInput);
        public event PacketDataIn m_evtPacketDataIn;

        //when connection is first made default to the connection Tick 
        public void MakeFirstConnection(int startTick)
        {

        }

        public void MakeConnection(string strConnectionDetails)
        {

        }

        public void MakeConnection(Connection conDebugConnection)
        {
            //add connection to connection list
            m_conConnectionList.Add(conDebugConnection);
        }

        public void MakeTestingConnection(NetworkConnection nwcConnectionTarget)
        {
            //create new connection 
            Connection m_conLocalConnection = new Connection(nwcConnectionTarget.m_bPlayerID);
            m_conLocalConnection.m_icsConnectionSim = m_icwConnectionSimulation;

            Connection m_conTargetConnection = new Connection(m_bPlayerID);
            m_conTargetConnection.m_icsConnectionSim = m_icwConnectionSimulation;

            m_conLocalConnection.m_conConnectionTarget = m_conTargetConnection;
            m_conTargetConnection.m_conConnectionTarget = m_conLocalConnection;

            MakeConnection(m_conLocalConnection);
            nwcConnectionTarget.MakeConnection(m_conTargetConnection);

        }

        public void UpdateConnections(int iCurrentTick)
        {
            //update connections with current tick
            for(int i = 0; i < m_conConnectionList.Count; i++)
            {
                m_conConnectionList[i].UpdateConnection(iCurrentTick);
            }
        }

        //send packet to all connected players 
        public void TransmitPacketToAll(Packet pktPacket)
        {
            for (int i = 0; i < m_conConnectionList.Count; i++)
            {
                m_conConnectionList[i].QueuePacketToSend(pktPacket);
            }
        }

        //get the number of conenctions that are functioning correctly 
        public int ActiveConnectionCount()
        {
            int iConnectionCount = 0; 

            for(int i = 0; i < m_conConnectionList.Count; i++)
            {
                if(m_conConnectionList[i] != null)
                {
                    iConnectionCount++;
                }
            }

            return iConnectionCount;
        }

        //get the synchronised network time 
        public float NetworkTime()
        {
            return Time.timeSinceLevelLoad;
        } 

        public void SendPackage(byte bPlayerConnection, Packet pktPacket)
        {
            for(int i = 0; i < m_conConnectionList.Count; i++)
            {
                if(m_conConnectionList[i].m_bConnectionID == bPlayerConnection)
                {
                    m_conConnectionList[i].QueuePacketToSend(pktPacket);

                    break;
                }
            }
        }

        public void DestributeReceivedPackets()
        {
            //loop through all the connections 
            for (int i = 0; i < m_conConnectionList.Count; i++)
            {
                while (m_conConnectionList[i].m_pakReceivedPackets.Count > 0)
                {
                    Packet pktPacket = m_conConnectionList[i].m_pakReceivedPackets.Dequeue();

                    ProcessPacket(m_conConnectionList[i].m_bConnectionID, pktPacket);
                }
            }
        }

        public Connection MakeConnectionOffer()
        {
            Connection conOffer = new Connection(m_bPlayerID);

            return conOffer;
        }

        public Connection MakeReply(Connection conConnectionOffer)
        {
            MakeConnection(conConnectionOffer);


            Connection conOffer = new Connection(m_bPlayerID);

            return conOffer;
        }

        public void RecieveConnectionReply(Connection conConnectionReply)
        {
            MakeConnection(conConnectionReply);
        }

        protected void ProcessPacket(byte bPlayerConnection, Packet pktPacket)
        {
            //check if packet was for networking only
            switch (pktPacket.m_ptyPacketType)
            {
 
                default:
                    //fire event 
                    m_evtPacketDataIn?.Invoke(bPlayerConnection, pktPacket);
                    break;
            }
        }

    }
}
