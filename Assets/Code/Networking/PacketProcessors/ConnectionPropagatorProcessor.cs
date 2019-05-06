using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Networking
{
    public class ConnectionPropagatorProcessor : NetworkPacketProcessor
    {


        public struct ConnectionRequestProgress
        {
            public long m_lConnectionID;
            public DateTime m_dtmTimeOfMessageSend;
        }

        public override int Priority
        {
            get
            {
                return 7;
            }
        }

        private NetworkConnection m_ncnNetworkConnection;

        private List<long> m_lMissingConnectionIDs = new List<long>();

        private List<ConnectionRequestProgress> m_crpConnectionRequests = new List<ConnectionRequestProgress>();

        public ConnectionPropagatorProcessor(NetworkLayoutProcessor nlpNetworkLayoutProcessor)
        {
            nlpNetworkLayoutProcessor.m_evtPeerConnectionLayoutChange += OnPeerNetworkLayoutChange;
        }

        public override void OnAddToNetwork(NetworkConnection ncnNetwork)
        {
            base.OnAddToNetwork(ncnNetwork);

            m_ncnNetworkConnection = ncnNetwork;
        }

        public void OnPeerNetworkLayoutChange(ConnectionNetworkLayoutProcessor clpNetworkLayoutProcessor)
        {
            m_lMissingConnectionIDs.Clear();

            List<NetworkLayoutProcessor.NetworkLayout.Connection> conConnections = clpNetworkLayoutProcessor.m_nlaNetworkLayout.m_conConnectionDetails;

            //loop through all the peers connections and see if any are missign from current connections
            for (int i = 0; i < conConnections.Count; i++)
            {
                //check if connection exists 
                if (!m_ncnNetworkConnection.HasConnection(conConnections[i].m_lConnectionID))
                {
                    m_lMissingConnectionIDs.Add(conConnections[i].m_lConnectionID);
                }
            }

            for (int i = 0; i < conConnections.Count; i++)
            {
                bool bAlreadyConnectingTo = false;

                //check if connection request already in progress 
                for (int j = 0; j < m_crpConnectionRequests.Count; j++)
                {
                    if (conConnections[i].m_lConnectionID == m_crpConnectionRequests[i].m_lConnectionID)
                    {
                        bAlreadyConnectingTo = true;
                        break;
                    }
                }

                if(bAlreadyConnectingTo == false)
                {
                    ConnectionRequestPacket crpConnectionRequest = m_ncnNetworkConnection.m_cifPacketFactory.CreateType<ConnectionRequestPacket>(ConnectionRequestPacket.TypeID);

                    crpConnectionRequest.m_lFrom = m_ncnNetworkConnection.m_lPlayerUniqueID;
                    crpConnectionRequest.m_lTo = conConnections[i].m_lConnectionID;

                    //generate connection request info here 
                    crpConnectionRequest.m_bConnectionRequestDetails = new List<byte>();

                    //send a connection request to target via peer 
                    m_ncnNetworkConnection.SendPackage(clpNetworkLayoutProcessor.m_conConnection.m_lUniqueID, crpConnectionRequest);
                }
            }
        }
    }
}
