using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Networking
{
    //this packet processor tracks the layout of the network
    public class NetworkLayoutProcessor : NetworkPacketProcessor
    {
        public delegate void ConnectionLayoutChange(ConnectionNetworkLayoutProcessor clpLayoutProcessor);
        public event ConnectionLayoutChange m_evtPeerConnectionLayoutChange;

        public struct NetworkLayout
        {
            public struct Connection
            {
                public long m_lConnectionID;
                public DateTime m_dtmTimeOfConnection;

                public Connection(long lID,DateTime dtmTimeOfConnection)
                {
                    m_lConnectionID = lID;
                    m_dtmTimeOfConnection = dtmTimeOfConnection;
                }

            }

            public List<Connection> m_conConnectionDetails;
            
            public NetworkLayout(int iConnectionCount)
            {
                m_conConnectionDetails = new List<Connection>(iConnectionCount);
            }

            public void Add(long lConnectionID,DateTime dtmTimeOfConnection)
            {
                if(m_conConnectionDetails == null)
                {
                    m_conConnectionDetails = new List<Connection>();
                }

                //check if it has already been added 
                for(int i = 0; i < m_conConnectionDetails.Count; i++)
                {
                    if(m_conConnectionDetails[i].m_lConnectionID == lConnectionID )
                    {
                        m_conConnectionDetails[i] = new Connection(lConnectionID, dtmTimeOfConnection);
                        return;
                    }
                }

                m_conConnectionDetails.Add(new Connection(lConnectionID, dtmTimeOfConnection));
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

            for(int i = 0; i < m_ncnNetworkConnection.m_conConnectionList.Count; i++)
            {
                OnNewConnection(m_ncnNetworkConnection.m_conConnectionList[i]);
            }
        }

        public override void OnNewConnection(Connection conConnection)
        {
            TimeNetworkProcessor tnpNetworkTime = m_ncnNetworkConnection.GetPacketProcessor<TimeNetworkProcessor>();

            //create new network layouy tracker
            ConnectionNetworkLayoutProcessor cnlConnectionLayout = new ConnectionNetworkLayoutProcessor(tnpNetworkTime.NetworkTime,this);

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

        public Connection m_conConnection;

        protected NetworkLayoutProcessor m_nlpNetworkProcessor;

        public ConnectionNetworkLayoutProcessor(DateTime dtmTimeOfCreation, NetworkLayoutProcessor nlpNetworkProcessor,Connection conConnection)
        {
            m_dtmTimeOfConnection = dtmTimeOfCreation;
            m_nlpNetworkProcessor = nlpNetworkProcessor;
            m_conConnection = conConnection;
        }

        public override DataPacket ProcessReceivedPacket(Connection conConnection, DataPacket pktInputPacket)
        {
            if(pktInputPacket is NetworkLayoutPacket)
            {
                m_nlaNetworkLayout = (pktInputPacket as NetworkLayoutPacket).m_nlaNetworkLayout;

                m_nlpNetworkProcessor.OnPeerNetworkLayoutChange(this);

                return null;
            }

            return pktInputPacket;
        }
    }
}
