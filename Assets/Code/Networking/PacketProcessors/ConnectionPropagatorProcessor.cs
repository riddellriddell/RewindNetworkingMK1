using System;
using System.Collections.Generic;

namespace Networking
{
    public class NetworkConnectionPropagatorProcessor : NetworkPacketProcessor
    {

        public struct ConnectionRequestProgress
        {
            public ConnectionRequestProgress(long lConnectionID, DateTime dtmTime)
            {
                m_lConnectionID = lConnectionID;
                m_dtmTimeOfMessageSend = dtmTime;
            }

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

        public override void OnAddToNetwork(NetworkConnection ncnNetwork)
        {
            NetworkLayoutProcessor nlpLayoutProcessor = ncnNetwork.GetPacketProcessor<NetworkLayoutProcessor>();

            nlpLayoutProcessor.m_evtPeerConnectionLayoutChange += OnPeerNetworkLayoutChange;

            base.OnAddToNetwork(ncnNetwork);

            m_ncnNetworkConnection = ncnNetwork;
        }

        public override void OnNewConnection(Connection conConnection)
        {
            conConnection.AddPacketProcessor(new ConnectionPropagatorProcessor(m_ncnNetworkConnection, this));

            //remove any active attemts to connect to target 
            RemoveMissingConnectionID(conConnection.m_lUniqueID);

            base.OnNewConnection(conConnection);
        }

        public void OnPeerNetworkLayoutChange(ConnectionNetworkLayoutProcessor clpNetworkLayoutProcessor)
        {
            CheckForMissingConnections();
        }

        protected void CheckForMissingConnections()
        {
            m_lMissingConnectionIDs.Clear();

            for (int i = 0; i < m_ncnNetworkConnection.m_conConnectionList.Count; i++)
            {
                //check if peer has missing connections 
                CheckPeerForMissingConnection(m_ncnNetworkConnection.m_conConnectionList[i]);
            }

            //check if missing connection already being created 
            List<long> lUnhandledMissingConnections = GetMissingPeerThatAreNotBeingConnectedTo();

            for (int i = 0; i < lUnhandledMissingConnections.Count; i++)
            {

            }
        }

        protected void CheckPeerForMissingConnection(Connection conConnection)
        {
            ConnectionNetworkLayoutProcessor clpConnectionLayout = conConnection.GetPacketProcessor<ConnectionNetworkLayoutProcessor>();

            List<NetworkLayoutProcessor.NetworkLayout.ConnectionState> conMissingConnections = clpConnectionLayout.m_nlaNetworkLayout.ConnectionsNotInList(m_ncnNetworkConnection.m_conConnectionList);

            for (int i = 0; i < conMissingConnections.Count; i++)
            {
                TryAddMissingConnectionID(conMissingConnections[i].m_lConnectionID);
            }
        }

        protected List<long> GetMissingPeerThatAreNotBeingConnectedTo()
        {
            List<long> lOutput = new List<long>();

            for (int i = 0; i < m_lMissingConnectionIDs.Count; i++)
            {
                bool bIsBeingConnectedTo = false;

                for (int j = 0; j < m_crpConnectionRequests.Count; j++)
                {
                    if (m_crpConnectionRequests[j].m_lConnectionID == m_lMissingConnectionIDs[i])
                    {
                        bIsBeingConnectedTo = true;

                        break;
                    }
                }

                if (bIsBeingConnectedTo == false)
                {
                    lOutput.Add(m_lMissingConnectionIDs[i]);
                }
            }

            return lOutput;
        }

        protected void TryAddMissingConnectionID(long lConnectionID)
        {
            for (int i = 0; i < m_lMissingConnectionIDs.Count; i++)
            {
                if (m_lMissingConnectionIDs[i] == lConnectionID)
                {
                    return;
                }
            }

            m_lMissingConnectionIDs.Add(lConnectionID);
        }

        protected void RemoveMissingConnectionID(long lConnectionID)
        {
            for (int i = m_lMissingConnectionIDs.Count - 1; i > -1; i--)
            {
                if (m_lMissingConnectionIDs[i] == lConnectionID)
                {
                    m_lMissingConnectionIDs.RemoveAt(i);
                }
            }
        }

        protected void StartRequest(long lConnection)
        {
            TimeNetworkProcessor tnpTimeProcessor = m_ncnNetworkConnection.GetPacketProcessor<TimeNetworkProcessor>();

            DateTime dtmStartTime = tnpTimeProcessor.NetworkTime;

            m_crpConnectionRequests.Add(new ConnectionRequestProgress(lConnection, dtmStartTime));

            //make request for connection data
        }

        public void StartReply(List<Byte> bRequestDetails, long lTargetID)
        {
            //send off to connection to create new webrtc connection 
        }

        public void ProcessReply(List<Byte> bReplyDetails, long lTargetID)
        {
            //check if connection already exists
            for(int i = 0; i < m_ncnNetworkConnection.m_conConnectionList.Count; i++)
            {
                if(m_ncnNetworkConnection.m_conConnectionList[i].m_lUniqueID == lTargetID)
                {
                    return;
                }
            }

            //process connection ID
        }

        protected void OnRequestFinish(List<Byte> bRequestOffer, long lTargetID)
        {
            NetworkLayoutProcessor nlpNetworkLayoutProcessor = m_ncnNetworkConnection.GetPacketProcessor<NetworkLayoutProcessor>();

            List<long> lPeersToSendThrough = nlpNetworkLayoutProcessor.PeersWithConnection(lTargetID);

            if (lPeersToSendThrough.Count > 0)
            {
                //send request to target
                ConnectionRequestPacket crpConnectionRequestPacket = m_ncnNetworkConnection.m_cifPacketFactory.CreateType<ConnectionRequestPacket>(ConnectionRequestPacket.TypeID);

                crpConnectionRequestPacket.m_bConnectionRequestDetails = bRequestOffer;
                crpConnectionRequestPacket.m_lFrom = m_ncnNetworkConnection.m_lPlayerUniqueID;
                crpConnectionRequestPacket.m_lTo = lTargetID;

                m_ncnNetworkConnection.SendPackage(lPeersToSendThrough[0], crpConnectionRequestPacket);
            }
            else
            {
                //if there was no connection to send through remove connection attempt
                RemoveMissingConnectionID(lTargetID);
            }
        }

        protected void OnReplyFinish(List<Byte> bReplyOffer, long lTargetID)
        {
            NetworkLayoutProcessor nlpNetworkLayoutProcessor = m_ncnNetworkConnection.GetPacketProcessor<NetworkLayoutProcessor>();

            List<long> lPeersToSendThrough = nlpNetworkLayoutProcessor.PeersWithConnection(lTargetID);

            if (lPeersToSendThrough.Count > 0)
            {
                //send request to target
                ConnectionReplyPacket crpConnectionReplyPacket = m_ncnNetworkConnection.m_cifPacketFactory.CreateType<ConnectionReplyPacket>(ConnectionReplyPacket.TypeID);

                crpConnectionReplyPacket.m_bConnectionReplyDetails = bReplyOffer;
                crpConnectionReplyPacket.m_lFrom = m_ncnNetworkConnection.m_lPlayerUniqueID;
                crpConnectionReplyPacket.m_lTo = lTargetID;

                m_ncnNetworkConnection.SendPackage(lPeersToSendThrough[0], crpConnectionReplyPacket);
            }
            else
            {
                //if there was no connection to send through remove connection attempt
                RemoveMissingConnectionID(lTargetID);
            }
        }


    }

    class ConnectionPropagatorProcessor : ConnectionPacketProcessor
    {
        public override int Priority
        {
            get
            {
                return 7;
            }
        }

        protected NetworkConnectionPropagatorProcessor m_ncpNetworkConnectionPropegator;

        protected NetworkConnection m_ncnNetworkConnection;

        public ConnectionPropagatorProcessor(NetworkConnection ncnNetworkConneciton, NetworkConnectionPropagatorProcessor ncpConnectionPropegator)
        {
            m_ncnNetworkConnection = ncnNetworkConneciton;
            m_ncpNetworkConnectionPropegator = ncpConnectionPropegator;
        }

        public override DataPacket ProcessReceivedPacket(Connection conConnection, DataPacket pktInputPacket)
        {
            if (pktInputPacket is ConnectionRequestPacket)
            {
                ConnectionRequestPacket crpConnectionRequest = pktInputPacket as ConnectionRequestPacket;

                //check if this request is intended for this peer
                if (crpConnectionRequest.m_lTo == m_ncnNetworkConnection.m_lPlayerUniqueID)
                {
                    //start reply
                    m_ncpNetworkConnectionPropegator.StartReply(crpConnectionRequest.m_bConnectionRequestDetails, crpConnectionRequest.m_lFrom);
                }
                else
                {
                    //send to target peer
                    m_ncnNetworkConnection.SendPackage(crpConnectionRequest.m_lTo, crpConnectionRequest);
                }

                return null;
            }

            if (pktInputPacket is ConnectionReplyPacket)
            {
                ConnectionReplyPacket crpConnectionReply = pktInputPacket as ConnectionReplyPacket;

                //check if this request is intended for this peer
                if (crpConnectionReply.m_lTo == m_ncnNetworkConnection.m_lPlayerUniqueID)
                {
                    //start reply
                    m_ncpNetworkConnectionPropegator.ProcessReply(crpConnectionReply.m_bConnectionReplyDetails, crpConnectionReply.m_lFrom);
                }
                else
                {
                    //send to target peer
                    m_ncnNetworkConnection.SendPackage(crpConnectionReply.m_lTo, crpConnectionReply);
                }

                return null;
            }

            return base.ProcessReceivedPacket(conConnection, pktInputPacket);
        }

    }
}
