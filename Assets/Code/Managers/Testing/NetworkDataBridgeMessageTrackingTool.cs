using System;
using System.Collections.Generic;
using Networking;
using SharedTypes;
using UnityEngine;

namespace GameManagers
{
    //this tool is for tracking what messages are in the network data bridges of different 
    //clients
    public class NetworkDataBridgeMessageTrackingTool: MonoBehaviour
    {
        public struct TimeInputPair
        {
            public DateTime m_dtmtime;
            public IInput m_inpInput;

        }
        
        public struct PeerInfo
        {
            public long m_lPeerID;
            public DateTime m_dtmTimeOfStart;
            public List<TimeInputPair> m_tipMessagesInBuffer;
        }

        public int m_iNumberOfActivePeers = 0;
        public int m_iMinMessages = 0;
        public int m_iMaxMessages = 0;
        
        public List<PeerInfo> m_pifPeerMessageInfo = new List<PeerInfo>() ;
        
        void GetActiveClients()
        {
            m_pifPeerMessageInfo.Clear();
            m_iMaxMessages = 0;
            m_iMinMessages = int.MaxValue;
            
            
            ActiveGameManagerSceneTester[] agmGameManagers = FindObjectsOfType<ActiveGameManagerSceneTester>();
            
            //loop through all game managers and extract their message buffers
            foreach (var agmManager in agmGameManagers)
            {
                //early out if nothing is setup
                if (agmManager.m_agmActiveGameManager == null ||
                    agmManager.m_agmActiveGameManager.m_ndbDataBridge == null ||
                    agmManager.m_agmActiveGameManager.m_ncnNetworkConnection == null ||
                    agmManager.m_agmActiveGameManager.m_ncnNetworkConnection
                        .GetPacketProcessor<NetworkGlobalMessengerProcessor>() == null)
                {
                    continue;
                }
                
                //get the object
                PeerInfo pifPeer = new PeerInfo();

                pifPeer.m_tipMessagesInBuffer = new List<TimeInputPair>();
                
                for(int i = 0; i < agmManager.m_agmActiveGameManager.m_ndbDataBridge.m_squInMessageQueue.Count; ++i)
                {
                    //get all messages for peer
                    TimeInputPair tipPair = new TimeInputPair();

                    SortingValue svaSortValue = agmManager.m_agmActiveGameManager.m_ndbDataBridge.m_squInMessageQueue
                        .GetKeyAtIndex(i);
                    
                    tipPair.m_dtmtime =  new DateTime((long)(svaSortValue.m_lSortValueA));
                    tipPair.m_inpInput =  agmManager.m_agmActiveGameManager.m_ndbDataBridge.m_squInMessageQueue
                        .GetValueAtIndex(i);
                    
                    pifPeer.m_tipMessagesInBuffer.Add(tipPair);
               
                }

                //get the time of connect
                pifPeer.m_lPeerID = agmManager.m_lPeerID;

                //try get the start time from the global message processor
                pifPeer.m_dtmTimeOfStart = agmManager.m_agmActiveGameManager.m_ncnNetworkConnection
                    .GetPacketProcessor<NetworkGlobalMessengerProcessor>().m_dtmGlobalMessageSystemActiveStartTime;

                m_pifPeerMessageInfo.Add(pifPeer);

                m_iMaxMessages = Math.Max(m_iMaxMessages, pifPeer.m_tipMessagesInBuffer.Count);
                m_iMinMessages = Math.Min(m_iMinMessages, pifPeer.m_tipMessagesInBuffer.Count);

            }

            m_iNumberOfActivePeers = m_pifPeerMessageInfo.Count;

        }

        private void Update()
        {
            GetActiveClients();
        }
    }
}