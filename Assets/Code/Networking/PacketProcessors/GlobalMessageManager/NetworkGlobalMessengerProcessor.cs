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

        //fixed max player count but in future will be dynamic? 
        public static int MaxPlayerCount { get; } = 32;

        //the state of the global message system
        public State m_staState = State.WaitingForConnection;

        //bufer of all the valid messages recieved from all peers through the global message system
        protected GlobalMessageBuffer m_gmbMessageBuffer;

        //index in buffer of most recent confirmed message

        //the max number of peers to include in the global messenging system

        //the time to build the next chain link from peer
        protected DateTime m_dtmNextLinkBuildTime = DateTime.MinValue;

        //the index of next chain link build
        protected uint m_iNextLinkIndex = uint.MinValue;

        //Chain manager 
        protected ChainManager m_chmChainManager;

        protected TimeNetworkProcessor m_tnpNetworkTime;

        protected GlobalMessageKeyManager m_gkmKeyManager;

        public NetworkGlobalMessengerProcessor() :base()
        {
            Initalize();
        }

        public void Initalize()
        {
            //setup the chain manager for the max player count 
            m_chmChainManager = new ChainManager(MaxPlayerCount);
            m_gmbMessageBuffer = new GlobalMessageBuffer();
        }

        public override void OnAddToNetwork(NetworkConnection ncnNetwork)
        {
            base.OnAddToNetwork(ncnNetwork);

            m_tnpNetworkTime = ParentNetworkConnection.GetPacketProcessor<TimeNetworkProcessor>();            
        }

        public override void Update()
        {
            base.Update();

            switch(m_staState)
            {
                case State.WaitingForConnection:

                    break;

                case State.ConnectAsFirstPeer:
                    StartAsFirstPeerInSystem();

                    m_staState = State.Connected;
                    break;
                case State.ConnectAsAdditionalPeer:
                    break;

                case State.Connected:
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
            if(m_staState != State.ConnectAsFirstPeer)
            {
                m_staState = State.ConnectAsAdditionalPeer;
            }
        }

        public void MakeNewChainLinkIfTimeTo()
        {
            //check if chain manager has a base to build chain links off
            if (m_chmChainManager.m_staState == ChainManager.State.Ready)
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
                    m_chmChainManager.AddChainLink(ParentNetworkConnection.m_lPeerID, chlNextLink, m_gkmKeyManager, m_gmbMessageBuffer);
                    
                    //send link to peers

                }
            }
        }

        //set this peer as the first in the global message system
        //and start producing input chain links
        public void StartAsFirstPeerInSystem()
        {
            //setup the inital state of the chain
            m_chmChainManager.SetStartState(ParentNetworkConnection.m_lPeerID); 

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
        }

        public void StartAsConnectorToSystem()
        {

        }

        public ChainLink CreateFirstChainLink(uint iChainCycleIndex)
        {
            ChainLink chlChainLink = new ChainLink();
            chlChainLink.Init(new List<PeerMessageNode>(0), ParentNetworkConnection.m_lPeerID, iChainCycleIndex,0); 
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
    }
}
