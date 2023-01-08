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

            public List<long> m_lAckedPeers;
        }

        public static int s_iIndexRangeToKeep = 100;

        public static Dictionary<ulong, ChainLinkEndStateRegistry> s_lsrLinkStateRegistry = new Dictionary<ulong, ChainLinkEndStateRegistry>();

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
            WriteByteStream wbsStream = new WriteByteStream(NetworkingByteStream.DataSize(chlLink.m_gmsState));

            //serialize state
            NetworkingByteStream.Serialize(wbsStream, ref chlLink.m_gmsState);

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
            if(s_lsrLinkStateRegistry.TryGetValue(lKey,out ChainLinkEndStateRegistry lsrLinkState))
            {
                //check if state matches 
                if(lsrLinkState.m_lChainLinkStateHash != lChainLinkHash)
                {
                    //chain link state does not match up
                    Debug.LogError($"Peer {lPeerRegistering} does not have the same state for link {chlLink.m_iLinkIndex} as existing peers");
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
                    m_lAckedPeers = new List<long>(),
                    m_lChainLinkStateHash = lChainLinkHash
                };

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
