using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Networking
{

    public class NetworkConnection : MonoBehaviour
    {

        // Defines a comparer to create a sorted set
        // that is sorted by the file extensions.
        private class PacketProcessorComparer : IComparer<NetworkPacketProcessor>
        {
            public int Compare(NetworkPacketProcessor x, NetworkPacketProcessor y)
            {
                if (x == null && y == null)
                {
                    return 0;
                }

                if (x == null)
                {
                    return -1;
                }

                if (y == null)
                {
                    return 1;
                }

                return x.Priority - y.Priority;
            }
        }

        public InternetConnectionSimulator m_icwConnectionSimulation;

        //list of all the network connection PacketManagers 
        public SortedSet<NetworkPacketProcessor> m_nppNetworkPacketProcessors;

        //all the connections 
        public List<Connection> m_conConnectionList = new List<Connection>();

        //the local id of the player
        public byte m_bPlayerID;

        //the unique id for this play
        public long m_lPlayerUniqueID;

        public delegate void PacketDataIn(byte bPlayerID, Packet pktInput);
        public event PacketDataIn m_evtPacketDataIn;

        public void Start()
        {
            m_nppNetworkPacketProcessors = new SortedSet<NetworkPacketProcessor>(new PacketProcessorComparer());
        }

        public void AddPacketProcessor(NetworkPacketProcessor nppProcessor)
        {
            m_nppNetworkPacketProcessors.Add(nppProcessor);

            nppProcessor.OnAddToNetwork(this);
        }

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

            //process new connection 
            ProcessNewConnection(conDebugConnection);
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

        public void UpdateConnectionsAndProcessors()
        {
            //update all processors 
            foreach (NetworkPacketProcessor nppProcessor in m_nppNetworkPacketProcessors)
            {
                nppProcessor.Update();
            }

            //update connections with current tick
            for(int i = 0; i < m_conConnectionList.Count; i++)
            {
                m_conConnectionList[i].UpdateConnection();
            }
        }

        //send packet to all connected players 
        public bool TransmitPacketToAll(Packet pktPacket)
        {
            //process packet for sending 
            pktPacket = ProcessPacketForSending(pktPacket);

            if(pktPacket == null)
            {
                return false;
            }

            for (int i = 0; i < m_conConnectionList.Count; i++)
            {
                m_conConnectionList[i].QueuePacketToSend(pktPacket);
            }

            return true;
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

        protected Packet ProcessPacketForSending(Packet pktPacket)
        {
            foreach (NetworkPacketProcessor nppProcessor in m_nppNetworkPacketProcessors)
            {
                pktPacket = nppProcessor.ProcessPacketForSending(pktPacket);

                if(pktPacket == null)
                {
                    return null;
                }
            }

            return pktPacket;
        }

        protected Packet ProcessReceivedPacket(Packet pktPacket)
        {
            foreach (NetworkPacketProcessor nppProcessor in m_nppNetworkPacketProcessors)
            {
                pktPacket = nppProcessor.ProcessReceivedPacket(pktPacket);

                if (pktPacket == null)
                {
                    return null;
                }
            }

            return pktPacket;
        }

        protected void ProcessNewConnection(Connection conConnection)
        {
            //process the new connection 
            foreach (NetworkPacketProcessor nppProcessor in m_nppNetworkPacketProcessors)
            {
                nppProcessor.OnNewConnection(conConnection);
            }
        }

    }
}
