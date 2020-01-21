using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Networking
{
    public class NetworkConnectionPropagatorProcessor : ManagedNetworkPacketProcessor<ConnectionPropagatorProcessor>
    {

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

        public override void OnAddToNetwork(NetworkConnection ncnNetwork)
        {
            m_nlpNetworkLayout = ncnNetwork.GetPacketProcessor<NetworkLayoutProcessor>();

            m_tnpNetworkTime = ncnNetwork.GetPacketProcessor<TimeNetworkProcessor>();

            m_ngmGatewayManager = ncnNetwork.GetPacketProcessor<NetworkGatewayManager>();

            m_nlpNetworkLayout.m_evtPeerConnectionLayoutChange += OnPeerNetworkLayoutChange;

            base.OnAddToNetwork(ncnNetwork);
        }

        public override DataPacket ProcessReceivedPacket(long lUserID, DataPacket pktInputPacket)
        {
            if (pktInputPacket is ConnectionNegotiationBasePacket)
            {
                //process connection negotiation packet
                ProcessConnectionNegotiationMessage(pktInputPacket as ConnectionNegotiationBasePacket);

                return null;
            }

            return base.ProcessReceivedPacket(lUserID, pktInputPacket);
        }

        public override void Update()
        {
            base.Update();

            SendConnectionNegotiationMessages();
        }

        public void OnPeerNetworkLayoutChange(ConnectionNetworkLayoutProcessor clpNetworkLayoutProcessor)
        {
            //check if peer has a connection to a peer not connected to this peer
            ConnectToMissingConnections();
        }

        public void StartRequest(long lUserID)
        {
            DateTime dtmStartTime = m_tnpNetworkTime.BaseTime;

            //make new connection
            Connection conConnection = ParentNetworkConnection.CreateOrResetConnection(dtmStartTime, lUserID);

            //start making connection offer 
            conConnection.StartConnectionNegotiation();
        }

        /// <summary>
        /// find any peers that are not connected to this peer but are connected through a mutural peer and start the connection process
        /// </summary>
        protected void ConnectToMissingConnections()
        {
            //check if there are any peers that this user is not connected too
            if (m_nlpNetworkLayout.MissingConnections.Count > 0)
            {
                List<long> lMissingConnections = new List<long>(m_nlpNetworkLayout.MissingConnections);

                foreach (long lMissingUserID in lMissingConnections)
                {
                    //check if connecting to this peer is blockd for some reason

                    //check if this is the correct peer to be making the connection attempt
                    if (ShouldPeerStartConnection(ParentNetworkConnection.m_lUserUniqueID, lMissingUserID) == false)
                    {
                        //skip user
                        continue;
                    }

                    //start connection proccess by making new connection object and generating 
                    //connection offer negotiation message
                    Debug.Log($"Starting connection request from: {ParentNetworkConnection.m_lUserUniqueID} to {lMissingUserID}");
                    StartRequest(lMissingUserID);
                }
            }
        }

        /// <summary>
        /// to connect 2 peers over the network one peer has to be selected to initiate the connection
        /// peer selection is done based on user id
        /// </summary>
        protected bool ShouldPeerStartConnection(long lPeerDecidingToConnect, long lTargetPeer)
        {
            //this may need to be changed in the future 
            return lPeerDecidingToConnect > lTargetPeer;
        }

        //check active connection negotiations to see if a message needs sending 
        protected void SendConnectionNegotiationMessages()
        {
            List<long> lConnectionsToRemove = new List<long>();

            //loop through connections
            foreach (ConnectionPropagatorProcessor cppProcessor in ChildConnectionProcessors.Values)
            {
                //update packets to send 
                cppProcessor.UpdateNegotiationMessagesToSend();

                //check for messages to send 
                if (cppProcessor.NegotiationPacketsToSend.Count == 0)
                {
                    //no messages continue to next connection negotiation
                    continue;
                }

                //check for client that messages can be sent through
                List<long> lMutualPeers = m_nlpNetworkLayout.PeersWithConnection(cppProcessor.ParentConnection.m_lUserUniqueID);

                //loop through packets to send 
                while (cppProcessor.NegotiationPacketsToSend.Count > 0)
                {
                    //get the next packet to send
                    ConnectionNegotiationBasePacket cnpPacket = cppProcessor.NegotiationPacketsToSend.Dequeue();

                    //check if message can be sent through a mutural peer 
                    if (lMutualPeers.Count != 0)
                    {
                        //pick a random open peer to send message through 
                        long lRelayConnection = lMutualPeers[Random.Range(0, lMutualPeers.Count)];


                        Debug.Log($"Sending conneciton negotiation packet: {cnpPacket.m_iIndex} from peer {cnpPacket.m_lFrom} through peer {lRelayConnection} to peer {cnpPacket.m_lTo}");

                        //send packet to peer
                        ParentNetworkConnection.SendPacket(lRelayConnection, cnpPacket);
                    }
                    //if no client exists check if user has active gateway or this is the first connection to the swarm
                    else if (m_ngmGatewayManager.NeedsOpenGateway == true || ParentNetworkConnection.m_bIsConnectedToSwarm == false)
                    {
                        m_ngmGatewayManager.ProcessMessageToGateway(cnpPacket.m_lTo, cnpPacket);
                    }
                    else //if there is no way to send message close connection negotiation
                    {
                        lConnectionsToRemove.Add(cnpPacket.m_lTo);
                        break;
                    }
                }
            }

            //check if there are any connections that should be removed 
            for (int i = 0; i < lConnectionsToRemove.Count; i++)
            {
                ParentNetworkConnection.DestroyConnection(lConnectionsToRemove[i]);
            }
        }

        protected void ProcessConnectionNegotiationMessage(ConnectionNegotiationBasePacket cnpPacket)
        {
            //validate message 

            //check if message is intended for user
            if (IsTargetForConnectionNegotiationMessage(cnpPacket) == false)
            {
                //todo validate packet and check that new connection public key matches
                //this users public key for the target connection

                //forward packet
                ParentNetworkConnection.SendPacket(cnpPacket.m_lTo, cnpPacket);

                //packet handled
                return;
            }

            ConnectionPropagatorProcessor cppFromConnection = null;

            //if packet is intended for this peer
            //check if a connection has already been opened 
            if (ChildConnectionProcessors.TryGetValue(cnpPacket.m_lFrom, out cppFromConnection))
            {
                //check if packet is from new connection request and requires the connection to be destroyed and recreated
                if (cppFromConnection.DoesNegotiationMessageInvalidateConnection(cnpPacket))
                {
                    //replace connection with new one
                    ParentNetworkConnection.CreateOrResetConnection(cnpPacket.m_dtmNegotiationStart, cnpPacket.m_lFrom);

                    //get new connection propegator 
                    ChildConnectionProcessors.TryGetValue(cnpPacket.m_lFrom, out cppFromConnection);
                }

            }
            else
            {
                //create new connection to handle connection negotiation 
                ParentNetworkConnection.CreateOrResetConnection(cnpPacket.m_dtmNegotiationStart, cnpPacket.m_lFrom);

                //get new connection propegator
                ChildConnectionProcessors.TryGetValue(cnpPacket.m_lFrom, out cppFromConnection);
            }

            //check if packet is outdated and should be discarded
            if (cppFromConnection.IsOutdatedMessage(cnpPacket))
            {
                return;
            }

            //add message to connection propegator
            cppFromConnection.QueueNegotiationMessageForProcessing(cnpPacket);

        }

        protected bool IsTargetForConnectionNegotiationMessage(ConnectionNegotiationBasePacket cnpPacket)
        {
            if (cnpPacket.m_lTo == ParentNetworkConnection.m_lUserUniqueID)
            {
                return true;
            }

            return false;
        }

        protected override void AddDependentPacketsToPacketFactory(ClassWithIDFactory cifPacketFactory)
        {
            cifPacketFactory.AddType<ConnectionNegotiationMessagePacket>(ConnectionNegotiationMessagePacket.TypeID);
        }

    }

    public class ConnectionPropagatorProcessor : ManagedConnectionPacketProcessor<NetworkConnectionPropagatorProcessor>
    {
        public Queue<ConnectionNegotiationBasePacket> NegotiationPacketsToSend { get; } = new Queue<ConnectionNegotiationBasePacket>();
        protected Dictionary<int, ConnectionNegotiationBasePacket> UnprocessedNegotiationPackets { get; } = new Dictionary<int, ConnectionNegotiationBasePacket>();
        protected int m_iProcessedMessagesHead = 0;
        protected int m_iNextMessageIndex = 0;

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

        /// <summary>
        /// checks if new connection attempt has started and this message is part of that new conneciton attempt
        /// if a new attempt is started then this connection should be destroyed and a new one created
        /// </summary>
        /// <param name="cnpPacket"></param>
        /// <returns></returns>
        public bool DoesNegotiationMessageInvalidateConnection(ConnectionNegotiationBasePacket cnpPacket)
        {
            if (ParentConnection.m_conConnectionSetupStart < cnpPacket.m_dtmNegotiationStart)
            {
                Debug.Log("Conneciton Negotiation message invalidates connection ");

                return true;
            }

            return false;
        }

        /// <summary>
        /// is this message from a previouse connection attempt
        /// </summary>
        /// <param name="cnpPacket"></param>
        /// <returns></returns>
        public bool IsOutdatedMessage(ConnectionNegotiationBasePacket cnpPacket)
        {
            if (ParentConnection.m_conConnectionSetupStart > cnpPacket.m_dtmNegotiationStart)
            {
                Debug.Log("Conneciton Negotiation message is outdated");
                return true;
            }

            return false;
        }

        /// <summary>
        /// add the connection negotiation packet to a queue so they will only be executed in order
        /// </summary>
        /// <param name="cnpPacket"></param>
        public void QueueNegotiationMessageForProcessing(ConnectionNegotiationBasePacket cnpPacket)
        {
            Debug.Log($"Queueing message: {cnpPacket.m_iIndex} from: {cnpPacket.m_lFrom} ");

            //add message to queue
            UnprocessedNegotiationPackets[cnpPacket.m_iIndex] = cnpPacket;

            //if process messages that have arrived in order /  are next to process
            ProcessNegotiationMessages();
        }

        public void UpdateNegotiationMessagesToSend()
        {
            while (ParentConnection.TransmittionNegotiationMessages.Count > 0)
            {
                string strMessage = ParentConnection.TransmittionNegotiationMessages.Dequeue();

                Debug.Log($"Connection:{ParentConnection.m_lUserUniqueID} Processing Negotiation messages:{strMessage} to send to user:{ParentConnection.m_lUserUniqueID} from User {m_tParentPacketProcessor.ParentNetworkConnection.m_lUserUniqueID}");

                ConnectionNegotiationMessagePacket cnmPacket = ParentConnection.m_cifPacketFactory.CreateType<ConnectionNegotiationMessagePacket>(ConnectionNegotiationMessagePacket.TypeID);

                cnmPacket.m_dtmNegotiationStart = ParentConnection.m_conConnectionSetupStart;
                cnmPacket.m_iIndex = m_iNextMessageIndex;
                cnmPacket.m_lFrom = m_tParentPacketProcessor.ParentNetworkConnection.m_lUserUniqueID;
                cnmPacket.m_lTo = ParentConnection.m_lUserUniqueID;
                cnmPacket.m_strConnectionNegotiationMessage = strMessage;

                //queue up packet to send
                NegotiationPacketsToSend.Enqueue(cnmPacket);

                //update message index
                m_iNextMessageIndex++;
            }
        }

        public override void OnConnectionReset()
        {
            NegotiationPacketsToSend.Clear();
            UnprocessedNegotiationPackets.Clear();
            m_iProcessedMessagesHead = 0;
            m_iNextMessageIndex = 0;

            base.OnConnectionReset();
        }

        /// <summary>
        /// negotiation messages that are sent to
        /// </summary>
        protected void ProcessNegotiationMessages()
        {
            while (UnprocessedNegotiationPackets.TryGetValue(m_iProcessedMessagesHead, out ConnectionNegotiationBasePacket cnpPacket))
            {
                //move to next packet
                m_iProcessedMessagesHead++;

                //send message to be processed
                if (cnpPacket is ConnectionNegotiationMessagePacket)
                {
                    Debug.Log($"Processing Packet: {m_iProcessedMessagesHead} from {cnpPacket.m_lFrom}");

                    ProcessMessage(cnpPacket as ConnectionNegotiationMessagePacket);
                }
            }
        }

        protected void ProcessMessage(ConnectionNegotiationMessagePacket cnmPacket)
        {
            ParentConnection.ProcessNetworkNegotiationMessage(cnmPacket.m_strConnectionNegotiationMessage);
        }

    }
}
