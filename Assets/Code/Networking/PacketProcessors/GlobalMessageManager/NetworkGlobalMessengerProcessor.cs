using SharedTypes;
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
        public TimeSpan ChannelTimeOutTime { get; private set; }

        #region StartStateSelection

        //the max time to wait before selecting a start state 
        public TimeSpan StateCollectionTimeOutTime { get; private set; }

        //the min percent of states from peers to collect before initalising 
        public float MinPercentOfStartStatesFromPeers { get; private set; }

        //for debug reasons 
        public float m_fPercentOfStartStatesRecieved;

        #endregion

        //when a peer initaly conencted wait this time before deciding to kick a peer for
        // disconnect, this is to protect against unneccesary kicking while peer conenction is
        // propegating through the swarm 
        public TimeSpan JoinVoteGracePeriod { get; private set; }

        //to stop a connecting peer thinking its in the global messaging system too early by mistaking an old connection
        //with the same user id that is still in the process of being kicked as a new one voting in reconnecting peer connections are filtered 
        //by connection start time, this is not garanteed to be 100% accurate so this padding is added just in case
        public TimeSpan OldConnectionFilterPadding { get; private set; }

        //fixed max player count but in future will be dynamic? 
        public int MaxChannelCount { get; private set; }

        //the state of the global message system
        public State m_staState = State.WaitingForConnection;

        //Chain manager 
        public ChainManager m_chmChainManager;

        //buffer of all the valid messages recieved from all peers through the global message system
        public GlobalMessageBuffer m_gmbMessageBuffer;

        //buffer for all the messages going to the game sim
        public NetworkingDataBridge m_ndbNetworkDataBridge;

        //factory for creating the message payload classes 
        public ClassWithIDFactory m_cifGlobalMessageFactory;

        //sim interface for selecting which peers to add or kick from game 
        public IGlobalMessageKickJoinSimVerificationInterface m_sviSimKickJoinInterface;

        //the local time this peer started connecting  / syncronizing with the global message system 
        protected DateTime m_dtmTimeOfStateCollectionStart = DateTime.MinValue;

        //the local time the connection process was started by the game manager 
        protected DateTime m_dtmConnectionStartTime = DateTime.MinValue;

        //should the best start state be reevaluated 
        protected bool m_bStartStateCandidatesDirty = true;

        //the time to build the next chain link from peer
        protected DateTime m_dtmNextLinkBuildTime = DateTime.MinValue;

        //the index of next chain link build
        protected uint m_iNextLinkIndex = uint.MinValue;

        protected TimeNetworkProcessor m_tnpNetworkTime;

        protected GlobalMessageKeyManager m_gkmKeyManager;

        public NetworkGlobalMessengerProcessor(NetworkingDataBridge ndbDataBridge) : base()
        {
            m_ndbNetworkDataBridge = ndbDataBridge;
            
            m_gmbMessageBuffer = new GlobalMessageBuffer();

            m_cifGlobalMessageFactory = new ClassWithIDFactory();

            //add all the different types of messages
            VoteMessage.TypeID = m_cifGlobalMessageFactory.AddType<VoteMessage>(VoteMessage.TypeID);
        }

        public void Initalize(int iMaxPlayerCount)
        {
            MaxChannelCount = iMaxPlayerCount;

            //setup the chain manager for the max player count 
            m_chmChainManager = new ChainManager(MaxChannelCount, ParentNetworkConnection.m_ncsConnectionSettings);

        }

        //register a new message type to the global message payload factory
        public int RegisterCustomMessageType<T>(int iCurrentTypeID)
        {
            return m_cifGlobalMessageFactory.AddType<T>(iCurrentTypeID);
        }

        public override void OnAddToNetwork(NetworkConnection ncnNetwork)
        {
            base.OnAddToNetwork(ncnNetwork);

            m_tnpNetworkTime = ParentNetworkConnection.GetPacketProcessor<TimeNetworkProcessor>();

            m_dtmConnectionStartTime = m_tnpNetworkTime.BaseTime;

            //add all the data packet classes this processor relies on to the main class factory 
            GlobalMessagePacket.TypeID = ParentNetworkConnection.PacketFactory.AddType<GlobalMessagePacket>(GlobalMessagePacket.TypeID);

            GlobalLinkRequest.TypeID = ParentNetworkConnection.PacketFactory.AddType<GlobalLinkRequest>(GlobalLinkRequest.TypeID);

            GlobalChainLinkPacket.TypeID = ParentNetworkConnection.PacketFactory.AddType<GlobalChainLinkPacket>(GlobalChainLinkPacket.TypeID);

            GlobalChainStatePacket.TypeID = ParentNetworkConnection.PacketFactory.AddType<GlobalChainStatePacket>(GlobalChainStatePacket.TypeID);

        }

        public override void ApplyNetworkSettings(NetworkConnectionSettings ncsSettings)
        {
            base.ApplyNetworkSettings(ncsSettings);
                
            ChannelTimeOutTime = TimeSpan.FromSeconds(ncsSettings.m_fChannelTimeOutTime);

            StateCollectionTimeOutTime = TimeSpan.FromSeconds(ncsSettings.m_fStateCollectionTimeOutTime);

            MinPercentOfStartStatesFromPeers = ncsSettings.m_fMinPercentOfStartStatesFromPeers;

            JoinVoteGracePeriod = TimeSpan.FromSeconds(ncsSettings.m_fJoinVoteGracePeriod);

            OldConnectionFilterPadding = TimeSpan.FromSeconds(ncsSettings.m_fOldConnectionFilterPadding);
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
                        m_chmChainManager.EvaluateStartCandidates(ParentNetworkConnection.m_lPeerID, false);
                        m_bStartStateCandidatesDirty = false;
                    }

                    CheckForSuccessfulStartStateSetup();
                    break;

                case State.Connected:
                    //get state of connection
                    GlobalMessagingState gmsState = m_chmChainManager.m_chlBestChainHead.m_gmsState;

                    //check if peer has been assigned to a channel
                    if (gmsState.TryGetIndexForPeer(ParentNetworkConnection.m_lPeerID, out int iIndex))
                    {
                        //convert the date time of the connection to synced network time 
                        DateTime NetworkTimeOfConnectionEst = TimeNetworkProcessor.ConvertFromBaseToNetworkTime(
                            m_dtmConnectionStartTime, 
                            m_tnpNetworkTime.CalculateTimeOffsetExcludingLocalPeer());

                        //check if peer was assigned that channel recently 
                        if (gmsState.m_gmcMessageChannels[iIndex].m_dtmVoteTime > (NetworkTimeOfConnectionEst - OldConnectionFilterPadding))
                        {
                            m_staState = State.Active;

                            SetTimeOfNextPeerChainLink(m_tnpNetworkTime.NetworkTime);
                        }
                    }
                    break;

                case State.Active:

                    //vote to add new peers to the system
                    UpdateAddingPeersToSystem();

                    //vote to kick disconnected peers 
                    UpdateKickingPeersFromSystem();

                    //handle messages from local peer and sim
                    HandleOutMessagesFromNetworkDataBuffer();

                    //add new links to chain 
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
            else if (pktInputPacket is GlobalChainLinkPacket)
            {
                GlobalChainLinkPacket clpChainLinkPacket = pktInputPacket as GlobalChainLinkPacket;

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

        public void ProcessLinkPacket(GlobalChainLinkPacket clpChainLinkPacket)
        {
            //check if still collecting start states
            if (m_staState == State.ConnectAsAdditionalPeer)
            {
                //decode payload 
                clpChainLinkPacket.m_chlLink.DecodePayloadArray(m_cifGlobalMessageFactory);

                clpChainLinkPacket.m_chlLink.CalculateLocalValuesForRecievedLink(m_cifGlobalMessageFactory);

                m_chmChainManager.AddChainLinkPreConnection(ParentNetworkConnection.m_lPeerID, clpChainLinkPacket.m_chlLink, m_gmbMessageBuffer, m_ndbNetworkDataBridge);

                m_bStartStateCandidatesDirty = true;
            }
            else if (m_staState == State.Connected || m_staState == State.Active)
            {
                //decode payload 
                clpChainLinkPacket.m_chlLink.DecodePayloadArray(m_cifGlobalMessageFactory);

                clpChainLinkPacket.m_chlLink.CalculateLocalValuesForRecievedLink(m_cifGlobalMessageFactory);

                bool bIsActivePeer = false;

                if (m_staState == State.Active)
                {
                    bIsActivePeer = true;
                }

                m_chmChainManager.AddChainLink(
                    ParentNetworkConnection.m_lPeerID, 
                    bIsActivePeer, 
                    clpChainLinkPacket.m_chlLink, 
                    m_gkmKeyManager, 
                    m_gmbMessageBuffer, 
                    m_ndbNetworkDataBridge, 
                    out bool bIsMessageBufferDirty);

                if (bIsMessageBufferDirty)
                {
                    //update the final unconfirmed message state 
                    m_gmbMessageBuffer.UpdateFinalMessageState(
                        ParentNetworkConnection.m_lPeerID, 
                        bIsActivePeer, 
                        m_chmChainManager.m_chlBestChainHead.m_gmsState, 
                        m_ndbNetworkDataBridge, 
                        m_chmChainManager.VoteTimeout, 
                        m_chmChainManager.MaxChannelCount);
                }
            }
        }

        public void ProcessMessagePacket(GlobalMessagePacket gmpMessagePacket)
        {
            //get message 
            PeerMessageNode pmnMessage = gmpMessagePacket.m_pmnMessage;

            //decode message 
            pmnMessage.DecodePayloadArray(m_cifGlobalMessageFactory);

            //calculate the hash for the payload
            pmnMessage.BuildPayloadHash();

            //crate the sorting value for the message 
            pmnMessage.CalculateSortingValue();

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
                   
                //get lock on network data bridge values

                //add link to local link tracker 
                m_chmChainManager.AddChainLink(
                    ParentNetworkConnection.m_lPeerID, 
                    true, 
                    chlNextLink, 
                    m_gkmKeyManager, 
                    m_gmbMessageBuffer, 
                    m_ndbNetworkDataBridge, 
                    out bool bIsMessageBufferDirty);

                if (bIsMessageBufferDirty)
                {
                    //update the final unconfirmed message state 
                    m_gmbMessageBuffer.UpdateFinalMessageState(
                        ParentNetworkConnection.m_lPeerID, 
                        true, 
                        m_chmChainManager.m_chlBestChainHead.m_gmsState, 
                        m_ndbNetworkDataBridge, 
                        m_chmChainManager.VoteTimeout,
                        m_chmChainManager.MaxChannelCount);
                }

                //unlock network data bridge values 

                //send link to peers
                SendChainLinkToPeers(chlNextLink);
            }
            else
            {
                //check if should set new link creation time
                if (m_dtmNextLinkBuildTime.Ticks == DateTime.MinValue.Ticks)
                {
                    SetTimeOfNextPeerChainLink(dtmNetworkTime);
                }
            }
        }

        public void SendChainLinkToPeers(ChainLink chlLink)
        {
            //create packet
            GlobalChainLinkPacket clpLinkPacket = ParentNetworkConnection.PacketFactory.CreateType<GlobalChainLinkPacket>(GlobalChainLinkPacket.TypeID);

            clpLinkPacket.m_chlLink = chlLink;

            ParentNetworkConnection.TransmitPacketToAll(clpLinkPacket);

        }

        //set this peer as the first in the global message system
        //and start producing input chain links
        public void StartAsFirstPeerInSystem()
        {
            //setup the inital state of the chain
            m_chmChainManager.SetStartState(ParentNetworkConnection.m_lPeerID, MaxChannelCount, m_tnpNetworkTime.NetworkTime);

            DateTime dtmNetworkTime = m_tnpNetworkTime.NetworkTime;

            //get current chain link
            uint iCurrentChainLink = m_chmChainManager.GetChainlinkCycleIndexForTime(
                dtmNetworkTime,
                ChainManager.TimeBetweenLinks,
                ChainManager.GetChainBaseTime(dtmNetworkTime));

            //get the last time that this peer should have created a chain link
            m_chmChainManager.GetPreviousChainLinkForChannel(
                0,
                m_chmChainManager.MaxChannelCount,
                iCurrentChainLink,
                ChainManager.TimeBetweenLinks,
                ChainManager.GetChainBaseTime(m_tnpNetworkTime.NetworkTime),
                out DateTime m_dtmTimeOfPreviousLink,
                out uint iLastChainLinkForPeer);

            //create base chain link
            ChainLink chlLink = CreateFirstChainLink(iLastChainLinkForPeer);

            //add link to chain manager
            m_chmChainManager.AddFirstChainLink(ParentNetworkConnection.m_lPeerID, true, chlLink, m_ndbNetworkDataBridge);

            //reset th proessed up to point on the global message buffer
            m_gmbMessageBuffer.m_svaStateProcessedUpTo = m_chmChainManager.m_chlBestChainHead.m_gmsState.m_svaLastMessageSortValue.NextSortValue();

            //update buffer final state
            m_gmbMessageBuffer.UpdateFinalMessageState(ParentNetworkConnection.m_lPeerID, true, m_chmChainManager.m_chlBestChainHead.m_gmsState, m_ndbNetworkDataBridge, m_chmChainManager.VoteTimeout, m_chmChainManager.MaxChannelCount);

            //update the time of the next chian link
            SetTimeOfNextPeerChainLink(dtmNetworkTime);
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
            float fPercentOfStatesRecieved = m_chmChainManager.m_iStartStatesRecieved / (float)iConnectedPeers;

            //TODO: Remove this debug code when startup problem fixed 
            m_fPercentOfStartStatesRecieved = fPercentOfStatesRecieved;

            //should a connection be forced
            //this occurs in anomilus conditions 
            bool bForceConnection = false;

            if (m_tnpNetworkTime.BaseTime - m_dtmTimeOfStateCollectionStart > StateCollectionTimeOutTime)
            {
                bForceConnection = true;
            }

            //check if enough states have been recieved
            if (fPercentOfStatesRecieved < MinPercentOfStartStatesFromPeers && bForceConnection == false)
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
            if (bForceConnection)
            {
                IsAcknowledgedStartState = true;
            }

            //if there is a state that has recieved enough validation set it as the start state 
            if (IsAcknowledgedStartState)
            {
                m_staState = State.Connected;

                m_chmChainManager.SetChainStartState(ParentNetworkConnection.m_lPeerID, false, MaxChannelCount, sscStartStateCandidate.m_gmsStateCandidate, sscStartStateCandidate.m_chlNextLink, m_ndbNetworkDataBridge);

                //reset the last message processed value on the message buffer
                m_gmbMessageBuffer.m_svaStateProcessedUpTo = m_chmChainManager.m_chlBestChainHead.m_gmsState.m_svaLastMessageSortValue;

                //update message buffer final state
                m_gmbMessageBuffer.UpdateFinalMessageState(ParentNetworkConnection.m_lPeerID, false, m_chmChainManager.m_chlBestChainHead.m_gmsState, m_ndbNetworkDataBridge, m_chmChainManager.VoteTimeout, m_chmChainManager.MaxChannelCount);
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
            if (m_chmChainManager.m_chlBestChainHead.m_gmsState.TryGetIndexForPeer(ParentNetworkConnection.m_lPeerID, out int iPeerChannel))
            {
                //get current chain link
                uint iCurrentChainLink = m_chmChainManager.GetChainlinkCycleIndexForTime(dtmCurrentNetworkTime, ChainManager.TimeBetweenLinks, ChainManager.GetChainBaseTime(dtmCurrentNetworkTime));

                uint iNextLinkIndex = 0;

                DateTime dtmNextLinkTime = DateTime.MinValue;

                //get the next time the channel will be addding a chain link
                m_chmChainManager.GetNextChainLinkForChannel(
                    iPeerChannel,
                    m_chmChainManager.MaxChannelCount,
                    iCurrentChainLink,
                    ChainManager.TimeBetweenLinks,
                    ChainManager.GetChainBaseTime(dtmCurrentNetworkTime),
                    out dtmNextLinkTime,
                    out iNextLinkIndex
                    );

                m_dtmNextLinkBuildTime = dtmNextLinkTime;
                m_iNextLinkIndex = iNextLinkIndex;
            }
            else
            {
                m_dtmNextLinkBuildTime = DateTime.MinValue;
                m_iNextLinkIndex = uint.MinValue;
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

            //check if still part of the system
            if (m_chmChainManager.m_chlBestChainHead.m_gmsState.TryGetIndexForPeer(ParentNetworkConnection.m_lPeerID, out int iIndex) == false)
            {
                return false;
            }


            //check if it is this peers turn to add a link onto the chain
            if (dtmNetworkTime > m_dtmNextLinkBuildTime && m_dtmNextLinkBuildTime.Ticks != DateTime.MinValue.Ticks)
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
            int iPlayerCount = m_gmbMessageBuffer.LatestState.AssignedChannelCount();

            //check if all possible candidates have already been joined 
            //TODO: finter out kicked or otherwise excluded candidates? 
            if (ChildConnectionProcessors.Count + 1 == iPlayerCount)
            {
                return;
            }

            //check if game is full
            if (iPlayerCount == MaxChannelCount)
            {
                return;
            }

            //gather all peers that are connected but not part of the system
            SortedList<long, long> lJoinCandidates = new SortedList<long, long>();

            //get the channel index of the current local peer;
            if (m_gmbMessageBuffer.LatestState.TryGetIndexForPeer(ParentNetworkConnection.m_lPeerID, out int iLocalPeerChannel) == false)
            {
                return;
            }

            foreach (KeyValuePair<long, ConnectionGlobalMessengerProcessor> kvpEntries in ChildConnectionProcessors)
            {
                //check that peer is connected or connecting 
                if (kvpEntries.Value.ParentConnection.Status == Connection.ConnectionStatus.Initializing ||
                    kvpEntries.Value.ParentConnection.Status == Connection.ConnectionStatus.Disconnecting ||
                    kvpEntries.Value.ParentConnection.Status == Connection.ConnectionStatus.Disconnected ||
                    kvpEntries.Value.ParentConnection.Status == Connection.ConnectionStatus.New)
                {
                    //skip this peer
                    continue;
                }

                //check if connected peer is already part of the system
                // or the local peer has not voted to add them to the system
                //TODO:: clean up this monstrosity of a statement
                if (m_gmbMessageBuffer.LatestState.TryGetIndexForPeer(kvpEntries.Key, out int iIndex) == false ||
                    (m_gmbMessageBuffer.LatestState.m_gmcMessageChannels[iIndex].m_staState == GlobalMessageChannelState.State.VoteJoin &&
                    (m_gmbMessageBuffer.LatestState.m_gmcMessageChannels[iLocalPeerChannel].m_chvVotes[iIndex].m_vtpVoteType != GlobalMessageChannelState.ChannelVote.VoteType.Add ||
                    m_gmbMessageBuffer.LatestState.m_gmcMessageChannels[iLocalPeerChannel].m_chvVotes[iIndex].IsActive(m_tnpNetworkTime.NetworkTime, m_chmChainManager.VoteTimeout) == false)))
                {
                    //get the time the candidate connected 
                    long lConnectionTime = kvpEntries.Value.ParentConnection.m_dtmConnectionEstablishTime.Ticks;

                    //protect against matching connection times
                    while (lJoinCandidates.ContainsKey(lConnectionTime))
                    {
                        lConnectionTime++;
                    }

                    lJoinCandidates.Add(lConnectionTime, kvpEntries.Key);
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
            int iPeersToAdd = Math.Min((MaxChannelCount - iPlayerCount), lJoinCandidates.Count);

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

        //checks for peers the local peer has lost conenctio to and votes to kick them 
        public void UpdateKickingPeersFromSystem()
        {
            //get all the active peers
            List<Tuple<int, long>> lActivePeers = m_gmbMessageBuffer.LatestState.GetActivePeerIndexAndID();

            //list of all the peers to kick
            List<long> lPeersToKick = new List<long>();

            if (m_gmbMessageBuffer.LatestState.TryGetIndexForPeer(ParentNetworkConnection.m_lPeerID, out int iLocalPeerIndex) == false)
            {
                //local peer is not part of the global messaging system so cant send messages 
                return;
            }

            //get local peer channel state
            GlobalMessageChannelState gcsLocalPeerChannelState = m_gmbMessageBuffer.LatestState.m_gmcMessageChannels[iLocalPeerIndex];


            //check if any peers in the global messaging system are not conencted to
            //the local peer
            for (int i = 0; i < lActivePeers.Count; i++)
            {
                //check if target peer is local peer
                if (lActivePeers[i].Item2 == ParentNetworkConnection.m_lPeerID)
                {
                    continue;
                }

                //check if peer has just connected and local peer has not had time to make conenction
                if (m_tnpNetworkTime.NetworkTime - m_gmbMessageBuffer.LatestState.m_gmcMessageChannels[lActivePeers[i].Item1].m_dtmVoteTime < JoinVoteGracePeriod)
                {
                    continue;
                }

                //if the target peer has no connection to the local pper or the target peer is in the process of disconnectin 
                if (ChildConnectionProcessors.TryGetValue(lActivePeers[i].Item2, out ConnectionGlobalMessengerProcessor cgmProcessor) == false
                    || cgmProcessor.ParentConnection.Status == Connection.ConnectionStatus.New
                    || cgmProcessor.ParentConnection.Status == Connection.ConnectionStatus.Initializing
                    || cgmProcessor.ParentConnection.Status == Connection.ConnectionStatus.Disconnecting
                    || cgmProcessor.ParentConnection.Status == Connection.ConnectionStatus.Disconnected)
                {
                    //get vote for channel
                    GlobalMessageChannelState.ChannelVote chvVote = gcsLocalPeerChannelState.m_chvVotes[lActivePeers[i].Item1];

                    //check if already kicking peer 
                    if (chvVote.m_vtpVoteType == GlobalMessageChannelState.ChannelVote.VoteType.Kick &&
                        chvVote.IsActive(m_tnpNetworkTime.NetworkTime, m_chmChainManager.VoteTimeout))
                    {
                        //skip peer because already kicking
                        continue;
                    }

                    lPeersToKick.Add(lActivePeers[i].Item2);
                }
            }

            if (lPeersToKick.Count == 0)
            {
                return;
            }


            //create add message payload
            VoteMessage vmsVoteMessage = m_cifGlobalMessageFactory.CreateType<VoteMessage>(VoteMessage.TypeID);

            vmsVoteMessage.m_tupActionPerPeer = new Tuple<byte, long>[lPeersToKick.Count];

            for (int i = 0; i < lPeersToKick.Count; i++)
            {
                vmsVoteMessage.m_tupActionPerPeer[i] = new Tuple<byte, long>(0, lPeersToKick[i]);
            }

            //TODO::Temp test to see if kicking is causing the disconnecting
            //create message node and send to all peers
            CreateMessageNode(vmsVoteMessage);
        }

        public void HandleOutMessagesFromNetworkDataBuffer()
        {
            //get lock on out message buffer 
            for(int i = 0; i < m_ndbNetworkDataBridge.m_gmbOutMessageBuffer.Count; i++)
            {
                CreateMessageNode(m_ndbNetworkDataBridge.m_gmbOutMessageBuffer[i]);
            }

            m_ndbNetworkDataBridge.m_gmbOutMessageBuffer.Clear();

            //release lock on out message buffer 
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

            pmnMessageNode.m_iPeerMessageIndex = m_gmbMessageBuffer.LatestState.m_gmcMessageChannels[iChannelIndex].m_iLastMessageIndexProcessed + 1;

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
            //get the last message in the chain system
            SortingValue svaSortingValue;

            if(m_chmChainManager.m_chlBestChainHead != null && m_chmChainManager.m_chlBestChainHead.m_gmsState != null)
            {
                svaSortingValue = m_chmChainManager.m_chlBestChainHead.m_gmsState.m_svaLastMessageSortValue;
            }
            else
            {
                svaSortingValue = SortingValue.MinValue;
            }                         

            //add to message buffer
            m_gmbMessageBuffer.AddMessageToBuffer(pmnMessage, svaSortingValue);

            //if connected or active
            if (m_staState == State.Connected || m_staState == State.Active)
            {
                bool bIsActivePeer = false;

                if (m_staState == State.Active)
                {
                    bIsActivePeer = true;
                }

                //check if message is being created behind head
                if (m_chmChainManager.m_chlBestChainHead.m_gmsState.m_svaLastMessageSortValue.CompareTo(pmnMessage.m_svaMessageSortingValue) > 0)
                {
                    //update the best chain
                    ChainLink chlBestLink = m_chmChainManager.GetBestHeadChainLink(m_gmbMessageBuffer);

                    //check that chain head changed 
                    if (chlBestLink != m_chmChainManager.m_chlBestChainHead)
                    {
                        m_chmChainManager.OnBestHeadChange(chlBestLink, ParentNetworkConnection.m_lPeerID, bIsActivePeer, m_ndbNetworkDataBridge, m_gmbMessageBuffer);

                        //rebuild message state 
                        m_gmbMessageBuffer.UpdateFinalMessageState(ParentNetworkConnection.m_lPeerID, bIsActivePeer, m_chmChainManager.m_chlBestChainHead.m_gmsState, m_ndbNetworkDataBridge, m_chmChainManager.VoteTimeout, m_chmChainManager.MaxChannelCount);
                    }
;                }
                else
                {
                    //rebuild message state 
                    m_gmbMessageBuffer.UpdateFinalMessageState(ParentNetworkConnection.m_lPeerID, bIsActivePeer, m_chmChainManager.m_chlBestChainHead.m_gmsState, m_ndbNetworkDataBridge, m_chmChainManager.VoteTimeout, m_chmChainManager.MaxChannelCount);
                }
            }
        }
    }

    public class ConnectionGlobalMessengerProcessor : ManagedConnectionPacketProcessor<NetworkGlobalMessengerProcessor>
    {
        //has this peer sent a start state 
        public bool m_bHasRecievedStartState = false;

        public override int Priority
        {
            get
            {
                return 5;
            }
        }

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
                    GlobalChainLinkPacket clpLinkPacket = ParentConnection.m_cifPacketFactory.CreateType<GlobalChainLinkPacket>(GlobalChainLinkPacket.TypeID);

                    clpLinkPacket.m_chlLink = chlLink;

                    m_tParentPacketProcessor.ParentNetworkConnection.SendPacket(conConnection, clpLinkPacket);

                }

                return null;
            }
            else if (pktInputPacket is GlobalChainStatePacket)
            {
                //stop peer from sending multiple start states
                if (m_bHasRecievedStartState == false)
                {
                    m_bHasRecievedStartState = true;

                    return pktInputPacket;
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

                //get all chain links to send starting at the chain link this client has
                //decided is the best chain link and looping back unitl a max of 10 chain links have been found
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
                    GlobalChainLinkPacket clpChainLinkPacket = ParentConnection.m_cifPacketFactory.CreateType<GlobalChainLinkPacket>(GlobalChainLinkPacket.TypeID);

                    clpChainLinkPacket.m_chlLink = chlLinksToSend[i];

                    //send state to peer
                    m_tParentPacketProcessor.ParentNetworkConnection.SendPacket(ParentConnection, clpChainLinkPacket);
                }
            }
        }
    }
}
