using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Networking
{
    public class NetworkConnectionPropagatorProcessor : ManagedNetworkPacketProcessor<ConnectionPropagatorProcessor>
    {

        public class ActiveConnectionNegotiation
        {
            public ActiveConnectionNegotiation(long lConnectionID, DateTime dtmTime)
            {
                m_lConnectionID = lConnectionID;
                m_dtmBaseTimeConnectionNegotionStart = dtmTime;
                m_cnpUnprocessedNegotiationPackets = new Dictionary<int,ConnectionNegotiationPacket>();
                m_iNextMessageIndex = 0;
            }

            public long m_lConnectionID;
            public DateTime m_dtmBaseTimeConnectionNegotionStart;

            //list of all the negotiation packets recieved so they can be processed in order
            public Dictionary<int,ConnectionNegotiationPacket> m_cnpUnprocessedNegotiationPackets;

            //the index of the last sent message 
            public int m_iNextMessageIndex;
        }

        public override int Priority
        {
            get
            {
                return 7;
            }
        }

        protected NetworkLayoutProcessor m_nlpNetworkLayout;

        protected TimeNetworkProcessor m_tnpNetworkTime;

        //due to cross dependency gateway cant be set on class creation 
        protected NetworkGatewayManager m_ngmGatewayManager;

        protected List<ActiveConnectionNegotiation> m_acnActiveConnectionRequests = new List<ActiveConnectionNegotiation>();
        
        public override void OnAddToNetwork(NetworkConnection ncnNetwork)
        {
            m_nlpNetworkLayout = ncnNetwork.GetPacketProcessor<NetworkLayoutProcessor>();

            m_tnpNetworkTime = ncnNetwork.GetPacketProcessor<TimeNetworkProcessor>();

            m_nlpNetworkLayout.m_evtPeerConnectionLayoutChange += OnPeerNetworkLayoutChange;

            base.OnAddToNetwork(ncnNetwork);
        }
        
        public void OnPeerNetworkLayoutChange(ConnectionNetworkLayoutProcessor clpNetworkLayoutProcessor)
        {

        }

        public void StartReply(List<Byte> bRequestDetails, long lTargetID)
        {
            //send off to connection to create new webrtc connection 
        }

        public void ProcessReply(List<Byte> bReplyDetails, long lTargetID)
        {
            //check if connection already exists
            for (int i = 0; i < ParentNetworkConnection.ConnectionList.Count; i++)
            {
                if (ParentNetworkConnection.ConnectionList[i].m_lUserUniqueID == lTargetID)
                {
                    return;
                }
            }

            //process connection ID
        }

        //protected void OnRequestFinish(List<Byte> bRequestOffer, long lTargetID)
        //{
        //    NetworkLayoutProcessor nlpNetworkLayoutProcessor = ParentNetworkConnection.GetPacketProcessor<NetworkLayoutProcessor>();

        //    List<long> lPeersToSendThrough = nlpNetworkLayoutProcessor.PeersWithConnection(lTargetID);

        //    if (lPeersToSendThrough.Count > 0)
        //    {
        //        //send request to target
        //        ConnectionNegotiationPacket crpConnectionRequestPacket = ParentNetworkConnection.m_cifPacketFactory.CreateType<ConnectionNegotiationPacket>(ConnectionNegotiationPacket.TypeID);

        //        crpConnectionRequestPacket.m_strConnectionNegotiationMessage = bRequestOffer;
        //        crpConnectionRequestPacket.m_lFrom = ParentNetworkConnection.m_lUserUniqueID;
        //        crpConnectionRequestPacket.m_lTo = lTargetID;

        //        ParentNetworkConnection.SendPackage(lPeersToSendThrough[0], crpConnectionRequestPacket);
        //    }
        //    else
        //    {
        //        //if there was no connection to send through remove connection attempt
        //        RemoveMissingConnectionID(lTargetID);
        //    }
        //}

        //protected void OnReplyFinish(List<Byte> bReplyOffer, long lTargetID)
        //{
        //    NetworkLayoutProcessor nlpNetworkLayoutProcessor = ParentNetworkConnection.GetPacketProcessor<NetworkLayoutProcessor>();

        //    List<long> lPeersToSendThrough = nlpNetworkLayoutProcessor.PeersWithConnection(lTargetID);

        //    if (lPeersToSendThrough.Count > 0)
        //    {
        //        //send request to target
        //        ConnectionReplyPacket crpConnectionReplyPacket = ParentNetworkConnection.m_cifPacketFactory.CreateType<ConnectionReplyPacket>(ConnectionReplyPacket.TypeID);

        //        crpConnectionReplyPacket.m_bConnectionReplyDetails = bReplyOffer;
        //        crpConnectionReplyPacket.m_lFrom = ParentNetworkConnection.m_lUserUniqueID;
        //        crpConnectionReplyPacket.m_lTo = lTargetID;

        //        ParentNetworkConnection.SendPackage(lPeersToSendThrough[0], crpConnectionReplyPacket);
        //    }
        //    else
        //    {
        //        //if there was no connection to send through remove connection attempt
        //        RemoveMissingConnectionID(lTargetID);
        //    }
        //}

        protected void ConnectToMissingConnections()
        {
            //check if there are any peers that this user is not connected too
            if(m_nlpNetworkLayout.MissingConnections.Count > 0)
            {
                List<long> lMissingConnections = new List<long>(m_nlpNetworkLayout.MissingConnections);

                foreach (long lUserID in lMissingConnections)
                {
                    //check if connecting to this peer is blockd for some reason

                    //check if this is the correct peer to be making the connection attempt
                    if(ShouldPeerStartConnection(ParentNetworkConnection.m_lUserUniqueID,lUserID) == false)
                    {
                        //skip user
                        continue;
                    }

                    //start connection proccess by making new connection object and generating 
                    //connection offer negotiation message
                    StartRequest(lUserID);
                }
            }
        }
        
        protected void StartRequest(long lUserID)
        {
            DateTime dtmStartTime = m_tnpNetworkTime.BaseTime;

            m_acnActiveConnectionRequests.Add(new ActiveConnectionNegotiation(lUserID, dtmStartTime));

            //make request for connection data

            //make new connection
            Connection conConnection = ParentNetworkConnection.CreateNewConnection(lUserID);

            //start making connection offer 
            conConnection.StartConnectionNegotiation();
        }
        
        /// <summary>
        /// to connect 2 peers over the network one peer has to be selected to initiate the connection
        /// peer selection is done based on user id
        /// </summary>
        protected bool ShouldPeerStartConnection(long lPeerDecidingToConnect, long lTargetPeer )
        {
            //this may need to be changed in the future 
            return lPeerDecidingToConnect > lTargetPeer;
        }

        //check active connection negotiations to see if a message needs sending 
        protected void SendConnectionNegotiationMessages()
        {
            //loop through all active connection negatiations 
            for (int i = m_acnActiveConnectionRequests.Count - 1; i > -1; i--)
            {
                //target connection id
                long lTargetID = m_acnActiveConnectionRequests[i].m_lConnectionID;

                //try get associated connection
                if (ParentNetworkConnection.ConnectionList.TryGetValue(lTargetID, out Connection conTargetConnection) == true)
                {
                    //check for messages to send 
                    if(conTargetConnection.TransmittionNegotiationMessages.Count == 0)
                    {
                        //no messages continue to next connection negotiation
                        continue;
                    }

                    //increment number of messages sent
                    int iMessageIndex = m_acnActiveConnectionRequests[i].m_iNextMessageIndex++;
                                       
                    //convert message into packet form 
                    ConnectionNegotiationPacket cnpPacket = ParentNetworkConnection.m_cifPacketFactory.CreateType<ConnectionNegotiationPacket>(ConnectionNegotiationPacket.TypeID);

                    cnpPacket.m_lFrom = ParentNetworkConnection.m_lUserUniqueID;
                    cnpPacket.m_lTo = lTargetID;
                    cnpPacket.m_iIndex = iMessageIndex;
                    cnpPacket.m_strConnectionNegotiationMessage = conTargetConnection.TransmittionNegotiationMessages.Dequeue();

                    //check for client that messages can be sent through
                    List<long> lMutualPeers = m_nlpNetworkLayout.PeersWithConnection(lTargetID);

                    
                    if(lMutualPeers.Count != 0)
                    {
                        //pick a random open peer to send message through 
                        long lRelayConnection = lMutualPeers[Random.Range(0, lMutualPeers.Count)];

                        //send packet to peer
                        ParentNetworkConnection.SendPackage(lRelayConnection, cnpPacket);
                    }
                    else if(m_ngmGatewayManager.NeedsOpenGateway == true) //if no client exists check if user has active gateway
                    {
                        m_ngmGatewayManager.ProcessPacketForSending(cnpPacket);
                    }
                    else //if there is no way to send message close connection negotiation
                    {

                    }                    
                }
                else
                {
                    //remove connection negotiations that have no connection
                    m_acnActiveConnectionRequests.RemoveAt(i);
                }
            }
        }
        
    }

    public class ConnectionPropagatorProcessor : ManagedConnectionPacketProcessor<NetworkConnectionPropagatorProcessor>
    {
        public override int Priority
        {
            get
            {
                return 7;
            }
        }

        public ConnectionPropagatorProcessor() : base()
        {
        }

        //public override DataPacket ProcessReceivedPacket(Connection conConnection, DataPacket pktInputPacket)
        //{
        //    if (pktInputPacket is ConnectionNegotiationPacket)
        //    {
        //        ConnectionNegotiationPacket crpConnectionRequest = pktInputPacket as ConnectionNegotiationPacket;

        //        //check if this request is intended for this peer
        //        if (crpConnectionRequest.m_lTo == m_tParentPacketProcessor.ParentNetworkConnection.m_lUserUniqueID)
        //        {
        //            //start reply
        //            m_tParentPacketProcessor.StartReply(crpConnectionRequest.m_strConnectionNegotiationMessage, crpConnectionRequest.m_lFrom);
        //        }
        //        else
        //        {
        //            //send to target peer
        //            m_tParentPacketProcessor.ParentNetworkConnection.SendPackage(crpConnectionRequest.m_lTo, crpConnectionRequest);
        //        }

        //        return null;
        //    }

        //    if (pktInputPacket is ConnectionReplyPacket)
        //    {
        //        ConnectionReplyPacket crpConnectionReply = pktInputPacket as ConnectionReplyPacket;

        //        //check if this request is intended for this peer
        //        if (crpConnectionReply.m_lTo == m_tParentPacketProcessor.ParentNetworkConnection.m_lUserUniqueID)
        //        {
        //            //start reply
        //            m_tParentPacketProcessor.ProcessReply(crpConnectionReply.m_bConnectionReplyDetails, crpConnectionReply.m_lFrom);
        //        }
        //        else
        //        {
        //            //send to target peer
        //            m_tParentPacketProcessor.ParentNetworkConnection.SendPackage(crpConnectionReply.m_lTo, crpConnectionReply);
        //        }

        //        return null;
        //    }

        //    return base.ProcessReceivedPacket(conConnection, pktInputPacket);
        //}

    }
}
