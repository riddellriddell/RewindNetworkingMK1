using System;
using System.Collections.Generic;

namespace Networking
{
    public class NetworkGlobalMessengerProcessor : ManagedNetworkPacketProcessor<ConnectionGlobalMessengerProcessor>
    {
        public enum State
        {
            WaitingForConnection,
            ConnectAsFirstPeer,
            ConnectAsAdditionalPeer,
            Connected,
            Active,
            Error,
            Disconnected,
        }

        public override int Priority
        {
            get
            {
                return 5;
            }
        }

        //the max time a channel can not send a message before being considered 
        //disconnected
        public static TimeSpan ChannelTimeOutTime
        {
            get
            {
                return TimeSpan.FromSeconds(0.5);
            }
        }

        #region StartStateSelection

        //the max time to wait before selecting a start state 
        public static TimeSpan StateCollectionTimeOutTime { get; } = TimeSpan.FromSeconds(3);

        //the min amount of time to wait before selecting start state 
        public static TimeSpan StateCollactionMinTime { get; } = TimeSpan.FromSeconds(2);

        //the min percent of states from peers to collect before initalising 
        public static float MinPercentOfStartStatesFromPeers { get; } = 0.75f;

        #endregion

        //fixed max player count but in future will be dynamic? 
        public static int MaxPlayerCount { get; } = 32;

        //the state of the global message system
        public State m_staState = State.WaitingForConnection;

        //Chain manager 
        public ChainManager m_chmChainManager;

        //factory for creating the message payload classes 
        public ClassWithIDFactory m_cifGlobalMessageFactory;

        //sim interface for selecting which peers to add or kick from game 
        public IGlobalMessageKickJoinSimVerificationInterface m_sviSimKickJoinInterface;

        //the local time this peer started connecting  / syncronizing with the global message system 
        protected DateTime m_dtmTimeOfStateCollectionStart = DateTime.MinValue;

        //should the best start state be reevaluated 
        protected bool m_bStartStateCandidatesDirty = true;

        //the time to build the next chain link from peer
        protected DateTime m_dtmNextLinkBuildTime = DateTime.MinValue;

        //the index of next chain link build
        protected uint m_iNextLinkIndex = uint.MinValue;

        //buffer of all the valid messages recieved from all peers through the global message system
        protected GlobalMessageBuffer m_gmbMessageBuffer;

        protected TimeNetworkProcessor m_tnpNetworkTime;

        protected GlobalMessageKeyManager m_gkmKeyManager;

        public NetworkGlobalMessengerProcessor() : base()
        {
            Initalize();
        }

        public void Initalize()
        {
            //setup the chain manager for the max player count 
            m_chmChainManager = new ChainManager(MaxPlayerCount);
            m_gmbMessageBuffer = new GlobalMessageBuffer();

            m_cifGlobalMessageFactory = new ClassWithIDFactory();

            //add all the different types of messages
            VoteMessage.TypeID = m_cifGlobalMessageFactory.AddType<VoteMessage>(VoteMessage.TypeID);
        }

        public override void OnAddToNetwork(NetworkConnection ncnNetwork)
        {
            base.OnAddToNetwork(ncnNetwork);

            m_tnpNetworkTime = ParentNetworkConnection.GetPacketProcessor<TimeNetworkProcessor>();


            //add all the data packet classes this processor relies on to the main class factory 
            GlobalMessagePacket.TypeID = ParentNetworkConnection.PacketFactory.AddType<GlobalMessagePacket>(GlobalMessagePacket.TypeID);

            GlobalLinkRequest.TypeID = ParentNetworkConnection.PacketFactory.AddType<GlobalLinkRequest>(GlobalLinkRequest.TypeID);

            GLobalChainLinkPacket.TypeID = ParentNetworkConnection.PacketFactory.AddType<GLobalChainLinkPacket>(GLobalChainLinkPacket.TypeID);

            GlobalChainStatePacket.TypeID = ParentNetworkConnection.PacketFactory.AddType<GlobalChainStatePacket>(GlobalChainStatePacket.TypeID);


        }

        public override void Update()
        {
            base.Update();

            switch (m_staState)
            {
                case State.WaitingForConnection:

                    break;

                case State.ConnectAsFirstPeer:
                    StartAsFirstPeerInSystem();

                    m_staState = State.Active;
                    break;
                case State.ConnectAsAdditionalPeer:
                    if (m_bStartStateCandidatesDirty)
                    {
                        m_chmChainManager.EvaluateStartCandidates(ParentNetworkConnection.m_lPeerID);
                        m_bStartStateCandidatesDirty = false;
                    }

                    CheckForSuccessfulStartStateSetup();
                    break;

                case State.Connected:
                    //check if peer has been assigned to a channel
                    if (m_chmChainManager.m_gmsChainStartState.TryGetIndexForPeer(ParentNetworkConnection.m_lPeerID, out int iIndex))
                    {
                        m_staState = State.Active;

                        SetTimeOfNextPeerChainLink(m_tnpNetworkTime.NetworkTime);
                    }
                    break;

                case State.Active:

                    //vote to add new peers to the system
                    UpdateAddingPeersToSystem();

                    MakeNewChainLinkIfTimeTo();
                    break;
            }
        }

        public override void OnFirstPeerInSwarm()
        {
            m_staState = State.ConnectAsFirstPeer;
        }

        public override void OnConnectToSwarm()
        {
            if (m_staState != State.ConnectAsFirstPeer)
            {
                m_staState = State.ConnectAsAdditionalPeer;

                //start the connection process
                StartAsConnectorToSystem();
            }
        }

        public override DataPacket ProcessReceivedPacket(long lFromUserID, DataPacket pktInputPacket)
        {
            if (pktInputPacket is GlobalMessagePacket)
            {
                GlobalMessagePacket gmpPacket = pktInputPacket as GlobalMessagePacket;

                ProcessMessagePacket(gmpPacket);

                return null;
            }
            else if (pktInputPacket is GLobalChainLinkPacket)
            {
                GLobalChainLinkPacket clpChainLinkPacket = pktInputPacket as GLobalChainLinkPacket;

                ProcessLinkPacket(clpChainLinkPacket);

                return null;
            }
            else if (pktInputPacket is GlobalChainStatePacket)
            {
                ProcessStatePacket(pktInputPacket as GlobalChainStatePacket);

                return null;
            }


            return pktInputPacket;
        }

        //if in the connecting as additional peer state peer collects start state candidates and
        //selectes the best candidate to use based on what other peers are using
        public void ProcessStatePacket(GlobalChainStatePacket cspStatePacket)
        {
            //check if still collecting start states
            if (m_staState == State.ConnectAsAdditionalPeer)
            {
                m_chmChainManager.AddNewStartCandidate(cspStatePacket.m_sscStartStateCandidate);

                m_bStartStateCandidatesDirty = true;
            }
        }

        public void ProcessLinkPacket(GLobalChainLinkPacket clpChainLinkPacket)
        {
            //check if still collecting start states
            if (m_staState == State.ConnectAsAdditionalPeer)
            {
                //decode payload 
                clpChainLinkPacket.m_chlLink.DecodePayloadArray(m_cifGlobalMessageFactory);

                clpChainLinkPacket.m_chlLink.CalculateLocalValuesForRecievedLink(m_cifGlobalMessageFactory);

                m_chmChainManager.AddChainLinkPreConnection(clpChainLinkPacket.m_chlLink, m_gmbMessageBuffer);

                m_bStartStateCandidatesDirty = true;
            }
            else if (m_staState == State.Connected || m_staState == State.Active)
            {
                //decode payload 
                clpChainLinkPacket.m_chlLink.DecodePayloadArray(m_cifGlobalMessageFactory);

                clpChainLinkPacket.m_chlLink.CalculateLocalValuesForRecievedLink(m_cifGlobalMessageFactory);

                m_chmChainManager.AddChainLink(ParentNetworkConnection.m_lPeerID, clpChainLinkPacket.m_chlLink, m_gkmKeyManager, m_gmbMessageBuffer, out bool bIsMessageBufferDirty);

                if (bIsMessageBufferDirty)
                {
                    //update the final unconfirmed message state 
                    m_gmbMessageBuffer.UpdateFinalMessageState(ParentNetworkConnection.m_lPeerID, m_chmChainManager.m_chlBestChainHead.m_gmsState);
                }
            }
        }

        public void ProcessMessagePacket(GlobalMessagePacket gmpMessagePacket)
        {
            //get message 
            PeerMessageNode pmnMessage = gmpMessagePacket.m_pmnMessage;

            //decode message 
            pmnMessage.DecodePayloadArray(m_cifGlobalMessageFactory);

            //add message to unconfirmed message buffer
            ProcessMessage(pmnMessage);
        }

        public void MakeNewChainLinkIfTimeTo()
        {
            //get network time
            DateTime dtmNetworkTime = m_tnpNetworkTime.NetworkTime;

            if (CheckIfShouldCreateChainLink(dtmNetworkTime))
            {
                //create the next link by peer
                ChainLink chlNextLink = CreateChainLink(m_iNextLinkIndex);

                //update the next time this peer should create a link
                SetTimeOfNextPeerChainLink(dtmNetworkTime);

                //add link to local link tracker 
                m_chmChainManager.AddChainLink(ParentNetworkConnection.m_lPeerID, chlNextLink, m_gkmKeyManager, m_gmbMessageBuffer, out bool bIsMessageBufferDirty);

                if (bIsMessageBufferDirty)
                {
                    //update the final unconfirmed message state 
                    m_gmbMessageBuffer.UpdateFinalMessageState(ParentNetworkConnection.m_lPeerID, m_chmChainManager.m_chlBestChainHead.m_gmsState);
                }

                //send link to peers
                SendChainLinkToPeers(chlNextLink);
            }
        }

        public void SendChainLinkToPeers(ChainLink chlLink)
        {
            //create packet
            GLobalChainLinkPacket clpLinkPacket = ParentNetworkConnection.PacketFactory.CreateType<GLobalChainLinkPacket>(GLobalChainLinkPacket.TypeID);

            clpLinkPacket.m_chlLink = chlLink;

            ParentNetworkConnection.TransmitPacketToAll(clpLinkPacket);

        }

        //set this peer as the first in the global message system
        //and start producing input chain links
        public void StartAsFirstPeerInSystem()
        {
            //setup the inital state of the chain
            m_chmChainManager.SetStartState(ParentNetworkConnection.m_lPeerID, MaxPlayerCount);

            DateTime dtmNetworkTime = m_tnpNetworkTime.NetworkTime;

            //get current chain link
            uint iCurrentChainLink = m_chmChainManager.GetChainlinkCycleIndexForTime(
                dtmNetworkTime,
                ChainManager.TimeBetweenLinks,
                ChainManager.GetChainBaseTime(dtmNetworkTime));

            //get the last time that this peer should have created a chain link
            m_chmChainManager.GetPreviousChainLinkForChannel(
                0,
                m_chmChainManager.m_iChannelCount,
                iCurrentChainLink,
                ChainManager.TimeBetweenLinks,
                ChainManager.GetChainBaseTime(m_tnpNetworkTime.NetworkTime),
                out DateTime m_dtmTimeOfPreviousLink,
                out uint iLastChainLinkForPeer);

            //create base chain link
            ChainLink chlLink = CreateFirstChainLink(iLastChainLinkForPeer);

            //update the time of the next chian link
            SetTimeOfNextPeerChainLink(dtmNetworkTime);

            //add link to chain manager
            m_chmChainManager.AddFirstChainLink(ParentNetworkConnection.m_lPeerID, chlLink);

            //update buffer final state
            m_gmbMessageBuffer.UpdateFinalMessageState(ParentNetworkConnection.m_lPeerID, m_chmChainManager.m_chlBestChainHead.m_gmsState);
        }

        public void StartAsConnectorToSystem()
        {
            //reset the start time for connection
            m_dtmTimeOfStateCollectionStart = m_tnpNetworkTime.BaseTime;
        }

        //check if enough start states have been recieved to pick start state
        public void CheckForSuccessfulStartStateSetup()
        {
            //check if enough candidates have been recieved
            int iConnectedPeers = ChildConnectionProcessors.Count;

            //check if there are any connected peers
            if (iConnectedPeers == 0)
            {
                return;
            }

            //check if enough states have been recieved 
            float fPercentOfStatesRecieved = m_chmChainManager.StartStateCandidates.Count / iConnectedPeers;

            //check if enough ststes have been recieved
            if (fPercentOfStatesRecieved < MinPercentOfStartStatesFromPeers)
            {
                return;
            }

            // get best connection candidate 
            bool IsAcknowledgedStartState = m_chmChainManager.GetBestStartStateCandidate(out GlobalMessageStartStateCandidate sscStartStateCandidate);

            //check that a state exists with at least one link
            if (sscStartStateCandidate == null || sscStartStateCandidate.m_chlNextLink == null)
            {
                return;
            }

            //check if too much time has passed and a start state should be forced
            if (m_tnpNetworkTime.BaseTime - m_dtmTimeOfStateCollectionStart > StateCollectionTimeOutTime)
            {
                IsAcknowledgedStartState = true;
            }

            //if there is a state that has recieved enough validation set it as the start state 
            if (IsAcknowledgedStartState)
            {
                m_staState = State.Connected;

                m_chmChainManager.SetChainStartState(ParentNetworkConnection.m_lPeerID, MaxPlayerCount, sscStartStateCandidate.m_gmsStateCandidate, sscStartStateCandidate.m_chlNextLink);

                //update message buffer final state
                m_gmbMessageBuffer.UpdateFinalMessageState(ParentNetworkConnection.m_lPeerID, m_chmChainManager.m_chlBestChainHead.m_gmsState);
            }
        }

        public ChainLink CreateFirstChainLink(uint iChainCycleIndex)
        {
            ChainLink chlChainLink = new ChainLink();
            chlChainLink.Init(new List<PeerMessageNode>(0), ParentNetworkConnection.m_lPeerID, iChainCycleIndex, 0);
            return chlChainLink;
        }

        public void SetTimeOfNextPeerChainLink(DateTime dtmCurrentNetworkTime)
        {
            //get the channel that corresponds to peer id
            //may change this to use state at end of message buffer
            if (m_chmChainManager.m_gmsChainStartState.TryGetIndexForPeer(ParentNetworkConnection.m_lPeerID, out int iPeerChannel))
            {
                //get current chain link
                uint iCurrentChainLink = m_chmChainManager.GetChainlinkCycleIndexForTime(dtmCurrentNetworkTime, ChainManager.TimeBetweenLinks, ChainManager.GetChainBaseTime(dtmCurrentNetworkTime));

                //get the next time the channel will be addding a chain link
                m_chmChainManager.GetNextChainLinkForChannel(
                    iPeerChannel,
                    m_chmChainManager.m_iChannelCount,
                    iCurrentChainLink,
                    ChainManager.TimeBetweenLinks,
                    ChainManager.GetChainBaseTime(dtmCurrentNetworkTime),
                    out m_dtmNextLinkBuildTime,
                    out m_iNextLinkIndex
                    );
            }
        }

        //check if this peer should create a chain link
        public bool CheckIfShouldCreateChainLink(DateTime dtmNetworkTime)
        {
            if (m_chmChainManager.m_chlBestChainHead == null)
            {
                //cant make a chain if there is no head to build off
                return false;
            }

            //check if it is this peers turn to add a link onto the chain
            if (dtmNetworkTime > m_dtmNextLinkBuildTime)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public ChainLink CreateChainLink(UInt32 iChainLinkIndex)
        {
            //get all inputs from head to latest
            List<PeerMessageNode> pmnLinkMessages = m_gmbMessageBuffer.GetChainLinkMessages(
                m_chmChainManager.m_chlBestChainHead.m_gmsState.m_svaLastMessageSortValue,
                ChannelTimeOutTime,
                m_dtmNextLinkBuildTime);

            //create chain link
            ChainLink chlNewLink = new ChainLink();

            //setup new link to link to best chain head and to have all messages that have happened since chain head 
            chlNewLink.Init(pmnLinkMessages, ParentNetworkConnection.m_lPeerID, iChainLinkIndex, m_chmChainManager.m_chlBestChainHead.m_lLinkPayloadHash);

            return chlNewLink;
        }

        //adds peers to system if empty spot is available
        public void UpdateAddingPeersToSystem()
        {
            //get number of players playing
            int iPlayerCount = m_gmbMessageBuffer.LatestState.ActiveChannelCount();

            //check if all possible candidates have already been joined 
            //TODO: finter out kicked or otherwise excluded candidates? 
            if (ChildConnectionProcessors.Count + 1 == iPlayerCount)
            {
                return;
            }

            //check if game is full
            if (iPlayerCount == MaxPlayerCount)
            {
                return;
            }

            //gather all peers that are connected but not part of the system
            SortedList<long, long> lJoinCandidates = new SortedList<long, long>();

            foreach (KeyValuePair<long, ConnectionGlobalMessengerProcessor> kvpEntries in ChildConnectionProcessors)
            {
                //check if connected peer is already part of the system
                if (m_gmbMessageBuffer.LatestState.TryGetIndexForPeer(kvpEntries.Key, out int iIndex) == false)
                {
                    //get the time the candidate connected 
                    long lCOnnectionTime = kvpEntries.Value.ParentConnection.m_dtmConnectionEstablishTime.Ticks;

                    lJoinCandidates.Add(lCOnnectionTime, kvpEntries.Key);
                }
            }

            //send peer list to sim for clearence 
            //m_sviSimKickJoinInterface.PeerConnectionCandidates(lJoinCandidates);

            //check if there is anyone left to join
            if (lJoinCandidates.Count == 0)
            {
                return;
            }

            //number of peers to add
            int iPeersToAdd = Math.Min((MaxPlayerCount - iPlayerCount), lJoinCandidates.Count);

            //create add message payload
            VoteMessage vmsVoteMessage = m_cifGlobalMessageFactory.CreateType<VoteMessage>(VoteMessage.TypeID);

            vmsVoteMessage.m_tupActionPerPeer = new Tuple<byte, long>[iPeersToAdd];

            for (int i = 0; i < iPeersToAdd; i++)
            {
                vmsVoteMessage.m_tupActionPerPeer[i] = new Tuple<byte, long>(1, lJoinCandidates.Values[i]);
            }

            //create message node and send to all peers
            CreateMessageNode(vmsVoteMessage);
        }

        public void CreateMessageNode(GlobalMessageBase gmbMessageToSend)
        {
            long lPeerID = ParentNetworkConnection.m_lPeerID;

            if (m_gmbMessageBuffer.LatestState.TryGetIndexForPeer(lPeerID, out int iChannelIndex) == false)
            {
                //not part of the peer message system so cant send message 
                return;
            }

            //create new message node 
            PeerMessageNode pmnMessageNode = new PeerMessageNode();

            pmnMessageNode.m_lPeerID = lPeerID;

            pmnMessageNode.m_dtmMessageCreationTime = m_tnpNetworkTime.NetworkTime;

            pmnMessageNode.m_lChainLinkHeadHash = m_chmChainManager.m_chlBestChainHead.m_lLinkPayloadHash;

            pmnMessageNode.m_bMessageType = (byte)gmbMessageToSend.TypeNumber;

            pmnMessageNode.m_gmbMessage = gmbMessageToSend;

            pmnMessageNode.m_iPeerMessageIndex = m_gmbMessageBuffer.LatestState.m_gmcMessageChannels[iChannelIndex].m_iLastMessageIndexProcessed++;

            pmnMessageNode.m_lPreviousMessageHash = m_gmbMessageBuffer.LatestState.m_gmcMessageChannels[iChannelIndex].m_lHashOfLastNodeProcessed;

            pmnMessageNode.BuildPayloadArray();

            pmnMessageNode.BuildPayloadHash();

            pmnMessageNode.SignMessage();

            pmnMessageNode.CalculateSortingValue();

            //process new message and add it to the local unconfirmed message buffer 
            ProcessMessage(pmnMessageNode);

            //send message to all peers
            GlobalMessagePacket gmpMessagePacket = ParentNetworkConnection.PacketFactory.CreateType<GlobalMessagePacket>(GlobalMessagePacket.TypeID);

            gmpMessagePacket.m_pmnMessage = pmnMessageNode;

            ParentNetworkConnection.TransmitPacketToAll(gmpMessagePacket);

        }

        public void ProcessMessage(PeerMessageNode pmnMessage)
        {
            //add to message buffer
            m_gmbMessageBuffer.AddMessageToBuffer(pmnMessage);

            //if connected or active
            if (m_staState == State.Connected || m_staState == State.Active)
            {
                //check if message is being created behind head
                if (m_chmChainManager.m_chlBestChainHead.m_gmsState.m_svaLastMessageSortValue.CompareTo(pmnMessage.m_svaMessageSortingValue) > 0)
                {
                    //recalculate best head 
                    bool bDidHeadCHange = m_chmChainManager.UpdateBestHeadChainLink(m_gmbMessageBuffer);

                    if (bDidHeadCHange)
                    {
                        //rebuild message state 
                        m_gmbMessageBuffer.UpdateFinalMessageState(ParentNetworkConnection.m_lPeerID, m_chmChainManager.m_chlBestChainHead.m_gmsState);
                    }
                }
                else
                {
                    //rebuild message state 
                    m_gmbMessageBuffer.UpdateFinalMessageState(ParentNetworkConnection.m_lPeerID, m_chmChainManager.m_chlBestChainHead.m_gmsState);
                }
            }
        }
    }

    public class ConnectionGlobalMessengerProcessor : ManagedConnectionPacketProcessor<NetworkGlobalMessengerProcessor>
    {
        public override int Priority
        {
            get
            {
                return 5;
            }
        }

        //the current chain link this peer is working off
        public long m_lCurrentChainHead;

        public override void OnConnectionStateChange(Connection.ConnectionStatus cstOldState, Connection.ConnectionStatus cstNewState)
        {
            base.OnConnectionStateChange(cstOldState, cstNewState);

            if (cstNewState == Connection.ConnectionStatus.Connected)
            {
                StartStateSync();
            }
        }

        public override void OnConnectionReset()
        {
            StartStateSync();
        }

        public override DataPacket ProcessReceivedPacket(Connection conConnection, DataPacket pktInputPacket)
        {
            //check if a peer is requesting an a link from the local peer
            if (pktInputPacket is GlobalLinkRequest)
            {
                GlobalLinkRequest glrRequest = pktInputPacket as GlobalLinkRequest;

                //try get link with hash
                ChainLink chlLink = m_tParentPacketProcessor.m_chmChainManager.FindLink(glrRequest.m_lRequestedLinkHash);

                //if link found send it back to the requester
                if (chlLink != null)
                {
                    GLobalChainLinkPacket clpLinkPacket = ParentConnection.m_cifPacketFactory.CreateType<GLobalChainLinkPacket>(GLobalChainLinkPacket.TypeID);

                    clpLinkPacket.m_chlLink = chlLink;

                    m_tParentPacketProcessor.ParentNetworkConnection.SendPacket(conConnection, clpLinkPacket);

                }

                return null;
            }

            return pktInputPacket;
        }

        //send the local peers start state and chain links to newly connected peer
        public void StartStateSync()
        {
            //check if local peer is connected to global messaging system
            if (m_tParentPacketProcessor.m_staState == NetworkGlobalMessengerProcessor.State.Active &&
                m_tParentPacketProcessor.m_chmChainManager.m_gmsChainStartState != null &&
                 m_tParentPacketProcessor.m_chmChainManager.m_chlBestChainHead != null)
            {
                //create new state packet
                GlobalChainStatePacket cspStatePacket = ParentConnection.m_cifPacketFactory.CreateType<GlobalChainStatePacket>(GlobalChainStatePacket.TypeID);

                //get all chain links to send 
                List<ChainLink> chlLinksToSend = new List<ChainLink>(10);

                ChainLink chlLink = m_tParentPacketProcessor.m_chmChainManager.m_chlBestChainHead;

                while (chlLink != null)
                {
                    chlLinksToSend.Add(chlLink);
                    chlLink = chlLink.m_chlParentChainLink;
                }


                //set state
                cspStatePacket.m_sscStartStateCandidate = new GlobalMessageStartStateCandidate();

                cspStatePacket.m_sscStartStateCandidate.Init(m_tParentPacketProcessor.m_chmChainManager.m_gmsChainStartState,
                    m_tParentPacketProcessor.m_chmChainManager.m_chlChainBase.m_lLinkPayloadHash);

                //send state to peer
                m_tParentPacketProcessor.ParentNetworkConnection.SendPacket(ParentConnection, cspStatePacket);


                //send all the chain links attached to local peers chain link head (best link) starting from the oldest 
                for (int i = chlLinksToSend.Count - 1; i > -1; i--)
                {
                    GLobalChainLinkPacket clpChainLinkPacket = ParentConnection.m_cifPacketFactory.CreateType<GLobalChainLinkPacket>(GLobalChainLinkPacket.TypeID);

                    clpChainLinkPacket.m_chlLink = chlLinksToSend[i];

                    //send state to peer
                    m_tParentPacketProcessor.ParentNetworkConnection.SendPacket(ParentConnection, clpChainLinkPacket);
                }
            }
        }
    }
}
