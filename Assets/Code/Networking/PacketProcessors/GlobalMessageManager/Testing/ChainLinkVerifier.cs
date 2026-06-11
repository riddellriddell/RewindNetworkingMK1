using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;
using Utility;

namespace Networking
{
    //this class is used to make sure the calculated state at the end of a chain link is the same for all peers
    public class ChainLinkVerifier
    {
        public class ChainLinkRegistry
        {
            public long m_lChainLinkHash;

            public ChainLink m_chlLink;

            public List<long> m_lAckedPeers;
        }

        public class PeerChainLinkHistory
        {
            public SortingValue m_svaCurrentBase;

            public List<SortingValue> m_lstChainLinkHistory = new List<SortingValue>();
        }
        
        public static int s_iIndexRangeToKeep = 100;

        public static int s_iLinkHistoryToKeep = 100;
        
        public static Dictionary<ulong, ChainLinkRegistry> s_lsrLinkRegistry = new Dictionary<ulong, ChainLinkRegistry>();

        //for each peer what is their active chain links, this is to help debug if a peer starts at a disconnected state 
        public static Dictionary<long, PeerChainLinkHistory> s_dicPeerActiveChainHistory =
            new Dictionary<long, PeerChainLinkHistory>();

        public static void RegisterLink(ChainLink chlLink, long lPeerRegistering)
        {
            //remove old link data
            CleanUpRegistry(chlLink.m_iLinkIndex);

            //generate key
            ulong lKey = 0;
            lKey += chlLink.m_iLinkIndex;
            lKey = lKey << sizeof(uint);

            ulong peerIdAsLong = (ulong)Math.Max(0, chlLink.m_lPeerID) + (ulong)long.MaxValue - (ulong)Math.Max(0, -chlLink.m_lPeerID);

            lKey += peerIdAsLong % UInt32.MaxValue;

            //calculate link end state hash 
            long lChainLinkHash = 0;

            //create byte stream big enough for state to write to
            WriteByteStream wbsStream = new WriteByteStream(chlLink.LinkDataSize());

            //serialize state
            chlLink.EncodePacket(wbsStream);

            //generate hash
            //compute hash
            using (MD5 md5Hash = MD5.Create())
            {
                //compute hash and store it
                Byte[] bHash = md5Hash.ComputeHash(wbsStream.GetData());

                //get the first 8 of the 16 bytes of the hash
                lChainLinkHash = BitConverter.ToInt64(bHash, 0);
            }

            //check if key exists in dictionary
            if (s_lsrLinkRegistry.TryGetValue(lKey, out ChainLinkRegistry lsrLinkState))
            {
                //check if state matches 
                if (lsrLinkState.m_lChainLinkHash != lChainLinkHash)
                {
                    //chain link state does not match up
                    Debug.LogError($"Chain Link Verifier: Peer {lPeerRegistering} does not have the same link {chlLink.m_iLinkIndex} as existing peers for link index");
                }
                else
                {
                    //add this peer to the list of peers who have calculated the same state 
                    if (lsrLinkState.m_lAckedPeers.Contains(lPeerRegistering) == false)
                    {
                        lsrLinkState.m_lAckedPeers.Add(lPeerRegistering);
                    }
                }
            }
            else
            {
                ChainLinkRegistry lsrNewLink = new ChainLinkRegistry()
                {
                    m_chlLink = chlLink,
                    m_lAckedPeers = new List<long>(),
                    m_lChainLinkHash = lChainLinkHash
                };

                lsrNewLink.m_lAckedPeers.Add(lPeerRegistering);

                s_lsrLinkRegistry.Add(lKey, lsrNewLink);
            }
        }

        public static void CleanUpRegistry(uint iCurrentIndex)
        {
            List<ulong> lkeysToRemove = new List<ulong>();

            foreach (ulong lKey in s_lsrLinkRegistry.Keys)
            {
                uint iIndex = (uint)(lKey >> sizeof(uint));

                if (iCurrentIndex - iIndex > s_iIndexRangeToKeep)
                {
                    lkeysToRemove.Add(lKey);
                }
            }

            for (int i = 0; i < lkeysToRemove.Count; i++)
            {
                s_lsrLinkRegistry.Remove(lkeysToRemove[i]);
            }
        }

        public static void RegisterLinkAsPeerHistory(ChainLink chlNewBaseLink, ChainLink chlNewHead, long lPeerRegistering)
        {
            SortingValue svaNewBaseSortValue = chlNewBaseLink.m_svaChainSortingValue;
            
            PeerChainLinkHistory pchPeerHistory;
            
            //check if peer has an entry in the history dictionary
            if (!s_dicPeerActiveChainHistory.TryGetValue(lPeerRegistering, out pchPeerHistory))
            {
                pchPeerHistory = new PeerChainLinkHistory();
                
                s_dicPeerActiveChainHistory.Add( lPeerRegistering, pchPeerHistory);
                
                //add the base link in 
                pchPeerHistory.m_lstChainLinkHistory.Add( chlNewBaseLink.m_svaChainSortingValue);
            }
            
            //build list of links attached to 

            List<SortingValue> lstNewHistory = new List<SortingValue>();

            while (chlNewHead != chlNewBaseLink )
            {
                lstNewHistory.Add( chlNewHead.m_svaChainSortingValue);

                chlNewHead = chlNewHead.m_chlParentChainLink;
            }
            
            lstNewHistory.Reverse();


            int iNewBaseHistoryIndex = -1;
            
            //loop through all entries in this peers chain to find the new base
            for (int i = 0; i < pchPeerHistory.m_lstChainLinkHistory.Count; ++i)
            {
                //check if chain link matches
                if (pchPeerHistory.m_lstChainLinkHistory[i].Equals(svaNewBaseSortValue))
                {
                    iNewBaseHistoryIndex = i;

                    break;
                }
            }
            
            //check that the link was found in the history
            if (iNewBaseHistoryIndex == -1)
            {
                //the new base is not in the history for this peer
                return;
            }
            
            //loop through all the chains and see if we are branching off 
            for (int i = iNewBaseHistoryIndex + 1; i < pchPeerHistory.m_lstChainLinkHistory.Count; ++i)
            {
                //relative index in new history
                int iNewHistoryIndex = i - (iNewBaseHistoryIndex + 1);
                
                //check if the index is off the end of the new history
                if (iNewBaseHistoryIndex >= lstNewHistory.Count)
                {
                    break;
                }
                
                //check if it matches 
                if (!pchPeerHistory.m_lstChainLinkHistory[i].Equals(lstNewHistory[iNewHistoryIndex]))
                {
                    //we have branched our history with this new node
                }
            }
            
            //remove all the old links that overlap with the history for this peer
            pchPeerHistory.m_lstChainLinkHistory.RemoveRange(iNewBaseHistoryIndex + 1, pchPeerHistory.m_lstChainLinkHistory.Count - (iNewBaseHistoryIndex +1) );
            
            //add in the new history
            pchPeerHistory.m_lstChainLinkHistory.AddRange( lstNewHistory);
            
            //check if new history is too big
            if (pchPeerHistory.m_lstChainLinkHistory.Count > s_iLinkHistoryToKeep)
            {
                pchPeerHistory.m_lstChainLinkHistory.RemoveRange(0, pchPeerHistory.m_lstChainLinkHistory.Count - s_iLinkHistoryToKeep);
            }
        }

        public static bool DoAllPeersHaveChainLinkInHistory(SortingValue svaLinkSortValue)
        {
            //loop through all peers
            foreach (var kvpPeerHistory in s_dicPeerActiveChainHistory)
            {
                if (kvpPeerHistory.Value.m_lstChainLinkHistory.Contains(svaLinkSortValue) == false)
                {
                    return false;
                }
            }

            return true;
        }
        
        public static bool DoAnyPeersHaveChainLinkInHistory(SortingValue svaLinkSortValue)
        {
            //loop through all peers
            foreach (var kvpPeerHistory in s_dicPeerActiveChainHistory)
            {
                if (kvpPeerHistory.Value.m_lstChainLinkHistory.Contains(svaLinkSortValue))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
