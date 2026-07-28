using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;
using Utility;

namespace Networking
{
    //this class is used to make sure the calculated state at the end of a chain link is the same for all peers
    public class ChainLinkEndStateVerrifier
    {
        public class ChainLinkEndStateRegistry
        {
            public long m_lChainLinkStateHash;

            public ChainLink m_chlLink;

            public GlobalMessagingState m_gmsStateAtLinkStart;

            public List<long> m_lAckedPeers;
        }

        public static int s_iIndexRangeToKeep = 1000000000;

        public static Dictionary<ulong, ChainLinkEndStateRegistry> s_lsrLinkStateRegistry = new Dictionary<ulong, ChainLinkEndStateRegistry>();

        public static void RegisterLink(
            ChainLink chlLink, 
            long lPeerRegistering, 
            GlobalMessagingState gmsStateAtLinkStart,
            TimeSpan tspVoteTimeout,
            int iMaxPlayerCount,
            NetworkingDataBridge ndbNetworkingDataBridge = null )
        {

            //disable clean up for testing
            if (false)
            {
                //remove old link data
                CleanUpRegistry(chlLink.m_iLinkIndex);
            }

            //generate key
            ulong lKey = 0;
            lKey += chlLink.m_iLinkIndex;
            lKey = lKey << sizeof(uint);

            ulong peerIdAsLong = ((ulong)Math.Max(0, chlLink.m_lPeerID) + (ulong)long.MaxValue) + (ulong)Math.Min(0, chlLink.m_lPeerID);

            lKey += peerIdAsLong % UInt32.MaxValue;

            //calculate link end state hash 
            long lChainLinkHash = chlLink.m_gmsState.GetHashOfState();

            //check if key exists in dictionary
            if(s_lsrLinkStateRegistry.TryGetValue(lKey,out ChainLinkEndStateRegistry lsrLinkState))
            {
                //check if state matches 
                if(lsrLinkState.m_lChainLinkStateHash != lChainLinkHash)
                {
                    String strExistingPeers = "";
                    for (int i = 0; i < lsrLinkState.m_lAckedPeers.Count; i++)
                    {
                        strExistingPeers += $"{lsrLinkState.m_lAckedPeers[i]},";
                    }
                    
                    //chain link state does not match up
                    Debug.LogError($"Peer {lPeerRegistering} does not have the same state for link {chlLink.m_iLinkIndex} as existing peers {strExistingPeers}");
                    
                    //check if the link has a different number of inputs 
                    int iExistingMessageCount = lsrLinkState.m_chlLink.m_pmnMessages.Count;
                    int iThisPeersMessageCount = chlLink.m_pmnMessages.Count;
                    
                    if (iExistingMessageCount != iThisPeersMessageCount)
                    {
                        Debug.LogError($"Peer {lPeerRegistering} for link {chlLink.m_iLinkIndex} has {iThisPeersMessageCount} messages while existing link has {iExistingMessageCount} messages");
                    }
                    
                    long lHashOfExistingPreviousState = lsrLinkState.m_gmsStateAtLinkStart.GetHashOfState();
                    long lHasOfThisPeersPrevousLinkState = gmsStateAtLinkStart.GetHashOfState();

                    if (lHashOfExistingPreviousState != lHasOfThisPeersPrevousLinkState)
                    {
                        Debug.LogError($"Peer {lPeerRegistering} for link {chlLink.m_iLinkIndex} has a link start hash of {lHasOfThisPeersPrevousLinkState} while existing link had a start has of {lHashOfExistingPreviousState}");
                    }
                    
                    
                    //recompute the state to see what the difference is 
                    GlobalMessagingState gmsThisPeerState = (GlobalMessagingState)gmsStateAtLinkStart.Clone();
                    GlobalMessagingState gmsOtherPeerState = (GlobalMessagingState)lsrLinkState.m_gmsStateAtLinkStart.Clone();
                    
                    //add the effects of all the messages, queueing them into the network data bridge allong with
                    //and connection change messges
                    for(int i = 0; i < chlLink.m_pmnMessages.Count; i++)
                    {
                        PeerMessageNode pmnNode = chlLink.m_pmnMessages[i];
                        
                        gmsThisPeerState.ProcessMessage( pmnNode, tspVoteTimeout, iMaxPlayerCount, ndbNetworkingDataBridge);
                        gmsOtherPeerState.ProcessMessage(pmnNode, tspVoteTimeout, iMaxPlayerCount, ndbNetworkingDataBridge);

                        long lThisPeerStateAfterMessage = gmsThisPeerState.GetHashOfState();
                        long lOtherPeerStateAfterMessage = gmsOtherPeerState.GetHashOfState();

                        if (lThisPeerStateAfterMessage != lOtherPeerStateAfterMessage)
                        {
                            Debug.LogError($"Peer {lPeerRegistering} desynced with other peers on message {i} for link {chlLink.m_iLinkIndex}");
                            
                            break;
                        }
                    }
                }
                else
                {
                    //add this peer to the list of peers who have calculated the same state 
                    if(lsrLinkState.m_lAckedPeers.Contains(lPeerRegistering) == false)
                    {
                        lsrLinkState.m_lAckedPeers.Add(lPeerRegistering);
                    }
                }
            }
            else
            {
                ChainLinkEndStateRegistry lsrNewLink = new ChainLinkEndStateRegistry()
                {
                    m_chlLink = chlLink,
                    m_gmsStateAtLinkStart = gmsStateAtLinkStart,
                    m_lAckedPeers = new List<long>(),
                    m_lChainLinkStateHash = lChainLinkHash
                };
                
                //indicate new link state is being registered
                Debug.Log($"Peer {lPeerRegistering} is registering new chain link {chlLink.m_iLinkIndex} with hash {lChainLinkHash}");

                lsrNewLink.m_lAckedPeers.Add(lPeerRegistering);

                s_lsrLinkStateRegistry.Add(lKey,lsrNewLink);
            }
        }

        public static void CleanUpRegistry(uint iCurrentIndex)
        {
            List<ulong> lkeysToRemove = new List<ulong>();

            foreach (ulong lKey in s_lsrLinkStateRegistry.Keys)
            {
                uint iIndex = (uint)(lKey >> sizeof(uint));

                if (iCurrentIndex - iIndex > s_iIndexRangeToKeep)
                {
                    lkeysToRemove.Add(lKey);
                }
            }

            for (int i = 0; i < lkeysToRemove.Count; i++)
            {
                s_lsrLinkStateRegistry.Remove(lkeysToRemove[i]);
            }            
        }
    }
}
