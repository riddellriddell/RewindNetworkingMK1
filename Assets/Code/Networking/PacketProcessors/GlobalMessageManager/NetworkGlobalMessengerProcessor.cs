using System;
using System.Collections.Generic;

namespace Networking
{
    public class NetworkGlobalMessengerProcessor : ManagedNetworkPacketProcessor<ConnectionGlobalMessengerProcessor>
    {
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

        public void Initalize(int iMaxPlayerCount)
        {
            //setup the chain manager for the max player count 
            m_chmChainManager = new ChainManager(iMaxPlayerCount);
        }

        public override void Update()
        {
            base.Update();

            //check if chain manager has a base to build chain links off
            if (m_chmChainManager.m_staState == ChainManager.State.Ready)
            {

                //get network time
                DateTime dtmNetworkTime = m_tnpNetworkTime.NetworkTime;

                if (CheckIfShouldCreateChainLink(dtmNetworkTime))
                {
                    //create the next link by peer
                    ChainLink chlNextLink = CreateChainLink(dtmNetworkTime);

                    //update the next time this peer should create a link
                    SetTimeOfNextPeerChainLink(dtmNetworkTime);

                    //send link to peers

                    //add link to local link tracker 

                }
            }
        }
        
        //set this peer as the first in the global message system
        //and start producing input chain links
        public void StartAsFirstPeerInSystem()
        {
            //setup the inital state of the chain
            m_chmChainManager.m_gmsChainStartState = new GlobalMessagingState(m_chmChainManager.m_iChannelCount, ParentNetworkConnection.m_lUserUniqueID);

            //setup the chain peer selection system
            m_chmChainManager.SetRandomFullCycleIncrement();

            //get the last time that this peer should have created a chain link
            uint iLastChainLinkForPeer = m_chmChainManager.GetLastChainLinkForChannel(
                0,
                m_chmChainManager.m_iChannelCount,
                m_tnpNetworkTime.NetworkTime,
                ChainManager.TimeBetweenLinks,
                ChainManager.GetChainBaseTime(m_tnpNetworkTime.NetworkTime));

            //create base chain link
            ChainLink chlLink = CreateFirstChainLink(iLastChainLinkForPeer);

            //add link to chain manager
            m_chmChainManager.AddFirstChainLink(chlLink);
        }

        public void StartAsConnectorToSystem()
        {

        }

        public ChainLink CreateFirstChainLink(uint iChainCycleIndex)
        {
            ChainLink chlChainLink = new ChainLink();

            chlChainLink.m_sigSignatureData = new ChainLink.SignedData();

            chlChainLink.m_sigSignatureData.m_iLinkIndex = iChainCycleIndex;

            chlChainLink.m_sigSignatureData.m_bHashOfChainLink = new byte[0];

            return chlChainLink;
        }

        public void SetTimeOfNextPeerChainLink(DateTime dtmCurrentTime)
        {
            //get the channel that correxsponds to peer id
            if (m_chmChainManager.m_chlChainBase.m_gmsState.TryGetIndexForPeer(ParentNetworkConnection.m_lUserUniqueID, out int iPeerChannel))
            {
                //get the next time the channel will be addding a chain link
                m_chmChainManager.TimeAndIndexOfNextLinkFromChannel(
                    iPeerChannel,
                    m_chmChainManager.m_iChannelCount,
                    dtmCurrentTime,
                    ChainManager.TimeBetweenLinks,
                    ChainManager.GetChainBaseTime(dtmCurrentTime),
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

        public ChainLink CreateChainLink(DateTime dtmNetworkTime)
        {

            //get all inputs from head to latest
            List<IPeerMessageNode> pmnLinkMessages = m_gmbMessageBuffer.GetChainLinkMessages(
                m_chmChainManager.m_chlBestChainHead.m_gmsState.m_msvLastMessageSortValue,
                ChannelTimeOutTime,
                m_dtmNextLinkBuildTime);

            //create chain link
            ChainLink chlNewLink = new ChainLink();

            chlNewLink.m_pmnMessages = pmnLinkMessages;
            chlNewLink.m_lPeerID = ParentNetworkConnection.m_lUserUniqueID;
            chlNewLink.m_sigSignatureData.m_iLinkIndex = m_iNextLinkIndex;
            chlNewLink.m_chlParentChainLink = m_chmChainManager.m_chlBestChainHead;

            //encript chan link components
            chlNewLink.Encript(m_gkmKeyManager);

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
