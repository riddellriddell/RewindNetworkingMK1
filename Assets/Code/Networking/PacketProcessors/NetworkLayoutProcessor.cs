using System;
using System.Collections.Generic;

namespace Networking
{
    //this packet processor tracks the layout of the network
    public class NetworkLayoutProcessor : NetworkPacketProcessor
    {
        public delegate void ConnectionLayoutChange(ConnectionNetworkLayoutProcessor clpLayoutProcessor);
        public event ConnectionLayoutChange m_evtPeerConnectionLayoutChange;

        public struct NetworkLayout
        {
            public struct ConnectionState
            {
                public long m_lConnectionID;
                public DateTime m_dtmTimeOfConnection;

                public ConnectionState(long lID, DateTime dtmTimeOfConnection)
                {
                    m_lConnectionID = lID;
                    m_dtmTimeOfConnection = dtmTimeOfConnection;
                }

            }

            public List<ConnectionState> m_conConnectionDetails;

            public NetworkLayout(int iConnectionCount)
            {
                m_conConnectionDetails = new List<ConnectionState>(iConnectionCount);
            }

            public void Add(long lConnectionID, DateTime dtmTimeOfConnection)
            {
                if (m_conConnectionDetails == null)
                {
                    m_conConnectionDetails = new List<ConnectionState>();
                }

                //check if it has already been added 
                for (int i = 0; i < m_conConnectionDetails.Count; i++)
                {
                    if (m_conConnectionDetails[i].m_lConnectionID == lConnectionID)
                    {
                        m_conConnectionDetails[i] = new ConnectionState(lConnectionID, dtmTimeOfConnection);
                        return;
                    }
                }

                m_conConnectionDetails.Add(new ConnectionState(lConnectionID, dtmTimeOfConnection));
            }

            public List<ConnectionState> ConnectionsNotInList(List<Connection> nlaTargetConnections)
            {
                List<ConnectionState> conOutput = new List<ConnectionState>();

                for (int i = 0; i < m_conConnectionDetails.Count; i++)
                {
                    long lconnection = m_conConnectionDetails[i].m_lConnectionID;

                    bool bNotInTarget = true;

                    for (int j = 0; j < nlaTargetConnections.Count; j++)
                    {
                        if (nlaTargetConnections[j].m_lUniqueID == lconnection)
                        {
                            bNotInTarget = false;

                            break;
                        }

                        if (bNotInTarget)
                        {
                            conOutput.Add(m_conConnectionDetails[i]);
                        }
                    }
                }

                return conOutput;
            }

            public bool HasTarget(long lConnectionID)
            {
                for (int i = 0; i < m_conConnectionDetails.Count; i++)
                {
                    if (m_conConnectionDetails[i].m_lConnectionID == lConnectionID)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public override int Priority
        {
            get
            {
                return 2;
            }
        }

        protected NetworkConnection m_ncnNetworkConnection;

        public override void OnAddToNetwork(NetworkConnection ncnNetwork)
        {
            m_ncnNetworkConnection = ncnNetwork;

            for (int i = 0; i < m_ncnNetworkConnection.m_conConnectionList.Count; i++)
            {
                OnNewConnection(m_ncnNetworkConnection.m_conConnectionList[i]);
            }
        }

        public override void OnNewConnection(Connection conConnection)
        {
            TimeNetworkProcessor tnpNetworkTime = m_ncnNetworkConnection.GetPacketProcessor<TimeNetworkProcessor>();

            //create new network layouy tracker
            ConnectionNetworkLayoutProcessor cnlConnectionLayout = new ConnectionNetworkLayoutProcessor(tnpNetworkTime.NetworkTime, this, conConnection);

            conConnection.AddPacketProcessor(cnlConnectionLayout);

            //send out updated network layout accross the network 
            base.OnNewConnection(conConnection);

            //generate network layout packet and send it to all connections
            NetworkLayoutPacket nlpNetworkLayoutPacket = m_ncnNetworkConnection.m_cifPacketFactory.CreateType<NetworkLayoutPacket>(NetworkLayoutPacket.TypeID);
            nlpNetworkLayoutPacket.m_nlaNetworkLayout = GenterateNetworkLayout();

            //send packet out to all connections updating them on what users this computer is conencted to 
            m_ncnNetworkConnection.TransmitPacketToAll(nlpNetworkLayoutPacket);
        }

        public void OnPeerNetworkLayoutChange(ConnectionNetworkLayoutProcessor clpConnectionNetworkLayoutProcessor)
        {
            m_evtPeerConnectionLayoutChange.Invoke(clpConnectionNetworkLayoutProcessor);
        }

        public List<long> PeersWithConnection(long lConnection)
        {
            List<long> lOutput = new List<long>();

            for (int i = 0; i < m_ncnNetworkConnection.m_conConnectionList.Count; i++)
            {
                ConnectionNetworkLayoutProcessor clpConnectionLayoutProcessor = m_ncnNetworkConnection.m_conConnectionList[i].GetPacketProcessor<ConnectionNetworkLayoutProcessor>();

                if (clpConnectionLayoutProcessor.m_nlaNetworkLayout.HasTarget(lConnection))
                {
                    lOutput.Add(m_ncnNetworkConnection.m_conConnectionList[i].m_lUniqueID);
                }
            }

            return lOutput;
        }

        protected NetworkLayout GenterateNetworkLayout()
        {
            NetworkLayout networkLayouy = new NetworkLayout(m_ncnNetworkConnection.m_conConnectionList.Count);

            for (int i = 0; i < m_ncnNetworkConnection.m_conConnectionList.Count; i++)
            {
                DateTime dtmTimeOfConnection = m_ncnNetworkConnection.m_conConnectionList[i].GetPacketProcessor<ConnectionNetworkLayoutProcessor>().m_dtmTimeOfConnection;

                networkLayouy.Add(m_ncnNetworkConnection.m_conConnectionList[i].m_lUniqueID, dtmTimeOfConnection);
            }

            return networkLayouy;
        }
    }

    public class ConnectionNetworkLayoutProcessor : ConnectionPacketProcessor
    {
        public override int Priority
        {
            get
            {
                return 2;
            }
        }

        //the time this connection was made 
        public DateTime m_dtmTimeOfConnection;

        //the connection layout at the other end of this connection
        public NetworkLayoutProcessor.NetworkLayout m_nlaNetworkLayout;

        protected NetworkLayoutProcessor m_nlpNetworkProcessor;

        public ConnectionNetworkLayoutProcessor(DateTime dtmTimeOfCreation, NetworkLayoutProcessor nlpNetworkProcessor, Connection conConnection)
        {
            m_dtmTimeOfConnection = dtmTimeOfCreation;
            m_nlpNetworkProcessor = nlpNetworkProcessor;
            m_conConnection = conConnection;
        }

        public override DataPacket ProcessReceivedPacket(Connection conConnection, DataPacket pktInputPacket)
        {
            if (pktInputPacket is NetworkLayoutPacket)
            {
                m_nlaNetworkLayout = (pktInputPacket as NetworkLayoutPacket).m_nlaNetworkLayout;

                m_nlpNetworkProcessor.OnPeerNetworkLayoutChange(this);

                return null;
            }

            return pktInputPacket;
        }
    }
}
