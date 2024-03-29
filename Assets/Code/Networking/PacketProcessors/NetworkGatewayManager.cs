﻿using Assets.Code.Utility;
using System;
using System.Collections.Generic;
using UnityEngine;
using Utility;

namespace Networking
{
    /// <summary>
    /// this class keeps track of the number of gateways connectint the cluster to the matchmaking
    /// server and if no gateway exists decides if it wants to become a gateway 
    /// </summary>
    public class NetworkGatewayManager : ManagedNetworkPacketProcessor<ConnectionGatewayManager>
    {
        /// <summary>
        /// how many secconds between anouncing you currently have a gateway running
        /// </summary>
        public TimeSpan GatewayAnounceRate { get; private set; }

        /// <summary>
        /// if another gateway anounce is not recieved in this time a users gateway is considered closed
        /// </summary>
        public TimeSpan GatewayTimeout { get; private set; }
    

        /// <summary>
        /// based on the current peer network should this peer have a gateway to the matchmaker open
        /// </summary>
        public bool NeedsOpenGateway { get; private set; } = false;

        /// <summary>
        /// messages to send through gateway (if active)
        /// </summary>
        public Queue<SendMessageCommand> MessagesToSend { get; } = new Queue<SendMessageCommand>();


        //defines the order that packet processors process packets
        public override int Priority { get; } = 10;


        protected NetworkLayoutProcessor m_nlpNetworkLayoutProcessor;

        protected NetworkGlobalMessengerProcessor m_gmpGLobalMessagingProcessor;

        public override void Update()
        {
            base.Update();

            //check if enabled and is not currently an active gate 
            if (ParentNetworkConnection.m_bIsConnectedToSwarm == true && NeedsOpenGateway == false)
            {
                //check if there is any connections this client is missing
                if (m_nlpNetworkLayoutProcessor.MissingConnections.Count > 0 ||
                    m_nlpNetworkLayoutProcessor.HasRecievedNetworkLayoutDataFromAllConnectedPeers() == false)
                {
                    return;
                }

                //check that this client is connected to the global messaging system and can accept new connections into the system
                if (m_gmpGLobalMessagingProcessor == null)
                {
                    m_gmpGLobalMessagingProcessor = ParentNetworkConnection.GetPacketProcessor<NetworkGlobalMessengerProcessor>();
                }

                if (m_gmpGLobalMessagingProcessor != null)
                {
                    if (m_gmpGLobalMessagingProcessor.m_staState != NetworkGlobalMessengerProcessor.State.Active)
                    {
                        return;
                    }
                }
                else
                {
                    return;
                }

                //is there an active gate in the cluster
                bool bActiveGate = false;

                bool bHadEnoughTimeToRecieveGatewayNotification = true;


                //check if there is a client that has an open gateway
                foreach (ConnectionGatewayManager cgmConnection in ChildConnectionProcessors.Values)
                {
                    //skip disconnected peers
                    if (cgmConnection.ParentConnection.Status != Connection.ConnectionStatus.Connected)
                    {
                        continue;
                    }

                    if (cgmConnection.HasActiveGateway)
                    {
                        bActiveGate = true;
                        break;
                    }

                    if (cgmConnection.HadTimeToRecieveGateNotification == false)
                    {
                        bHadEnoughTimeToRecieveGatewayNotification = false;
                        break;
                    }
                }

                //if there is no active gate and enough time has passed on all connections to detect open gates
                if (bActiveGate == false && bHadEnoughTimeToRecieveGatewayNotification == true)
                {
                    bool bShouldOpenGate = true;

                    //check if this user is highest user ID and should open gate 
                    foreach (ConnectionGatewayManager userID in ChildConnectionProcessors.Values)
                    {
                        if (userID.ParentConnection.Status == Connection.ConnectionStatus.Connected && userID.ParentConnection.m_lUserUniqueID > ParentNetworkConnection.m_lPeerID)
                        {
                            bShouldOpenGate = false;

                            break;
                        }
                    }

                    if (bShouldOpenGate)
                    {
                        Debug.Log($"User:{ParentNetworkConnection.m_lPeerID} Opening Gateway");

                        //open gate 
                        NeedsOpenGateway = true;
                    }
                }
            }
        }

        //code to handle messages sent and recieved throught the WebInterface
        #region GatewayMessages

        public void ProcessMessageToGateway(long lTargetUserID, DataPacket dpkDataPacket)
        {

            WriteByteStream wbsWriteStream = new WriteByteStream(dpkDataPacket.PacketTotalSize);

            byte bPacketType = (byte)dpkDataPacket.GetTypeID;

            //store the type of packet being sent 
            ByteStream.Serialize(wbsWriteStream, ref bPacketType);

            //encode packet data
            dpkDataPacket.EncodePacket(wbsWriteStream);

            SendMessageCommand smcSendMessageCommand = new SendMessageCommand()
            {
                m_iType = (int)MessageType.GatewayMessage,
                m_lFromID = ParentNetworkConnection.m_lPeerID,
                m_lToID = lTargetUserID,
                m_strMessage = JsonUtility.ToJson(new JsonByteArrayWrapper(wbsWriteStream.GetData()))
            };

            //add to send message list
            MessagesToSend.Enqueue(smcSendMessageCommand);
        }

        //process message from matchmaking server
        public void ProcessMessageFromGateway(UserMessage usmMessage)
        {
            //check message is fully formed
            if(usmMessage.m_lFromUser == 0 || 
                string.IsNullOrEmpty(usmMessage.m_strMessage) ||
               usmMessage.m_iMessageType == 0 ||
               usmMessage.m_dtmTimeOfMessage == 0)
            {
                Debug.LogError($"Mallformed message from server, message was { JsonUtility.ToJson(usmMessage) }");

                return;
            }

            //check message type
            switch (usmMessage.m_iMessageType)
            {
                case (int)MessageType.GatewayMessage:

                    //get packet data
                    JsonByteArrayWrapper jbwWrapper = JsonUtility.FromJson<JsonByteArrayWrapper>(usmMessage.m_strMessage);

                    //convert to read stream
                    ReadByteStream rbsByteStream = new ReadByteStream(jbwWrapper.m_bWrappedArray);

                    byte bPackageType = 0;

                    //get packet type id
                    ByteStream.Serialize(rbsByteStream, ref bPackageType);

                    //get packet type from id
                    DataPacket dpkPacket = ParentNetworkConnection.PacketFactory.CreateType<DataPacket>(bPackageType);

                    //convert sent data to target packet
                    dpkPacket.DecodePacket(rbsByteStream);

                    //process any connection propegation messages 
                    if (dpkPacket is ConnectionNegotiationBasePacket)
                    {
                        NetworkConnectionPropagatorProcessor ncpPropegator = ParentNetworkConnection.GetPacketProcessor<NetworkConnectionPropagatorProcessor>();

                        //TODO:: this should be replaced by a less fragile system 
                        ncpPropegator.ProcessReceivedPacket(usmMessage.m_lFromUser, dpkPacket);
                    }

                    break;
            }
        }

        #endregion

        public override void OnAddToNetwork(NetworkConnection ncnNetwork)
        {
            base.OnAddToNetwork(ncnNetwork);
            m_nlpNetworkLayoutProcessor = ParentNetworkConnection.GetPacketProcessor<NetworkLayoutProcessor>();
        }

        public override void ApplyNetworkSettings(NetworkConnectionSettings ncsSettings)
        {
            base.ApplyNetworkSettings(ncsSettings);

            GatewayAnounceRate = TimeSpan.FromSeconds(ncsSettings.m_fGatewayAnounceRate);
            GatewayTimeout = TimeSpan.FromSeconds(ncsSettings.m_fGatewayTimeout);
        }

        protected override void AddDependentPacketsToPacketFactory(ClassWithIDFactory cifPacketFactory)
        {
            cifPacketFactory.AddType<GatewayActiveAnouncePacket>(GatewayActiveAnouncePacket.TypeID);
        }
    }

    public class ConnectionGatewayManager : ManagedConnectionPacketProcessor<NetworkGatewayManager>
    {
        public bool HasActiveGateway
        {
            get
            {
                if (m_tnpNetworkTime.BaseTime - TimeOfLastGatewayNotification < m_tParentPacketProcessor.GatewayTimeout)
                {
                    return true;
                }

                return false;
            }
        }

        //has enough time passed for the peer to send an open gate notification
        public bool HadTimeToRecieveGateNotification { get; private set; } = false;

        public DateTime TimeOfLastGatewayNotification { get; private set; } = DateTime.MinValue;

        public DateTime TimeOfFistGatewatActivation { get; private set; } = DateTime.MinValue;

        public override int Priority { get; } = 10;

        protected DateTime m_dtmTimeOfLastOpenGateNotification = DateTime.MinValue;

        protected TimeNetworkProcessor m_tnpNetworkTime;

        public override void Start()
        {
            base.Start();
            m_tnpNetworkTime = m_tParentPacketProcessor.ParentNetworkConnection.GetPacketProcessor<TimeNetworkProcessor>();
        }

        public override void Update()
        {
            //check if connected
            if (ParentConnection.Status != Connection.ConnectionStatus.Connected)
            {
                return;
            }

            //check if local peer has active gateway
            if (m_tParentPacketProcessor.ParentNetworkConnection.m_bIsConnectedToSwarm && m_tParentPacketProcessor.NeedsOpenGateway)
            {
                //check if gateway has timed out
                if (m_tnpNetworkTime.BaseTime - m_dtmTimeOfLastOpenGateNotification > m_tParentPacketProcessor.GatewayAnounceRate)
                {
                    //create packet to send
                    GatewayActiveAnouncePacket gapAnouncePacket = ParentConnection.m_cifPacketFactory.CreateType<GatewayActiveAnouncePacket>(GatewayActiveAnouncePacket.TypeID);

                    //make new announcement 
                    ParentConnection.QueuePacketToSend(gapAnouncePacket);

                    //update the last time a gateway announce was sent 
                    m_dtmTimeOfLastOpenGateNotification = m_tnpNetworkTime.BaseTime;
                }
            }

            if (HadTimeToRecieveGateNotification == false)
            {
                if (ParentConnection.Status == Connection.ConnectionStatus.Connected && ParentConnection.m_dtmConnectionEstablishTime + m_tParentPacketProcessor.GatewayTimeout < m_tnpNetworkTime.BaseTime)
                {
                    HadTimeToRecieveGateNotification = true;
                }
            }

            base.Update();
        }

        public override DataPacket ProcessReceivedPacket(Connection conConnection, DataPacket pktInputPacket)
        {
            //check if packet is gateway announcement
            if (pktInputPacket.GetTypeID == GatewayActiveAnouncePacket.TypeID)
            {
                //check if time of gateway activation

                //update the last time seeing a notification for a gateway
                TimeOfLastGatewayNotification = m_tnpNetworkTime.BaseTime;

                return null;
            }

            return base.ProcessReceivedPacket(conConnection, pktInputPacket);
        }

        public override void OnConnectionStateChange(Connection.ConnectionStatus cstOldState, Connection.ConnectionStatus cstNewState)
        {
            if (cstNewState == Connection.ConnectionStatus.Disconnected || cstNewState == Connection.ConnectionStatus.Disconnecting)
            {
                TimeOfLastGatewayNotification = DateTime.MinValue;
            }
        }

        public override void OnConnectionReset()
        {
            //has enough time passed for the peer to send an open gate notification
            HadTimeToRecieveGateNotification = false;

            TimeOfLastGatewayNotification = DateTime.MinValue;

            TimeOfFistGatewatActivation = DateTime.MinValue;

            m_dtmTimeOfLastOpenGateNotification = DateTime.MinValue;

            base.OnConnectionReset();
        }
    }
}
