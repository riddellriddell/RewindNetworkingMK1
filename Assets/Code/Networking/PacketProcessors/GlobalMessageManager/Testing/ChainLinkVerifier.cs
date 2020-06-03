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

        public static int s_iIndexRangeToKeep = 100;

        public static Dictionary<ulong, ChainLinkRegistry> s_lsrLinkRegistry = new Dictionary<ulong, ChainLinkRegistry>();

        public static void RegisterLink(ChainLink chlLink, long lPeerRegistering)
        {

            //remove old link data
            CleanUpRegistry(chlLink.m_iLinkIndex);

            //generate key
            ulong lKey = 0;
            lKey += chlLink.m_iLinkIndex;
            lKey = lKey << sizeof(uint);
            lKey += (ulong)Math.Max(0, Math.Min(chlLink.m_lPeerID, uint.MinValue));

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
                    Debug.LogError($"Peer {lPeerRegistering} does not have the same link {chlLink.m_iLinkIndex} as existing peers for link index");
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
    }
}
