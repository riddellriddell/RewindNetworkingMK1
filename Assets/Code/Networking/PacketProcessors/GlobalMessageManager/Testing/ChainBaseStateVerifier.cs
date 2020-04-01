using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;

namespace Networking
{
    //this class is used to make sure the calculated state at the end of a chain link is the same for all peers
    public class ChainBaseStateVerifier
    {
        public class ChainLinkBaseStateRegistry
        {
            public long m_lChainLinkStateHash;

            public List<long> m_lAckedPeers;
        }

        public static int s_iIndexRangeToKeep = 100;

        public static Dictionary<uint, ChainLinkBaseStateRegistry> s_bsrBaseStateRegistry = new Dictionary<uint, ChainLinkBaseStateRegistry>();

        public static void RegisterAllStatesUpToLink(ChainLink chlLink, long lPeerRegistering)
        {
            chlLink = chlLink.m_chlParentChainLink;

            while (chlLink != null)
            {
                if (chlLink.m_gmsState != null)
                {
                    RegisterState(chlLink.m_gmsState, chlLink.m_iLinkIndex, lPeerRegistering);
                }

                chlLink = chlLink.m_chlParentChainLink;
            }
        }

        public static void RegisterState(GlobalMessagingState gmdState,uint iIndex, long lPeerRegistering)
        {

            //remove old link data
            CleanUpRegistry(iIndex);

            //calculate link end state hash 
            long lChainLinkHash = 0;

            //create byte stream big enough for state to write to
            WriteByteStream wbsStream = new WriteByteStream(ByteStream.DataSize(gmdState));

            //serialize state
            ByteStream.Serialize(wbsStream, ref gmdState);

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
            if (s_bsrBaseStateRegistry.TryGetValue(iIndex, out ChainLinkBaseStateRegistry bsrBaseState))
            {
                //check if state matches 
                if (bsrBaseState.m_lChainLinkStateHash != lChainLinkHash)
                {
                    //chain link state does not match up
                    Debug.LogError($"Peer {lPeerRegistering} does not have the same state for base state for index:{iIndex} as existing peers");
                }
                else
                {
                    //add this peer to the list of peers who have calculated the same state 
                    if (bsrBaseState.m_lAckedPeers.Contains(lPeerRegistering) == false)
                    {
                        bsrBaseState.m_lAckedPeers.Add(lPeerRegistering);
                    }
                }
            }
            else
            {
                ChainLinkBaseStateRegistry lsrNewLink = new ChainLinkBaseStateRegistry()
                {
                    m_lAckedPeers = new List<long>(),
                    m_lChainLinkStateHash = lChainLinkHash
                };

                lsrNewLink.m_lAckedPeers.Add(lPeerRegistering);

                s_bsrBaseStateRegistry.Add(iIndex, lsrNewLink);
            }
        }

        public static void CleanUpRegistry(uint iCurrentIndex)
        {
            List<uint> lkeysToRemove = new List<uint>();

            foreach (uint lKey in s_bsrBaseStateRegistry.Keys)
            {
                uint iIndex = (uint)(lKey >> sizeof(uint));

                if (iCurrentIndex - iIndex > s_iIndexRangeToKeep)
                {
                    lkeysToRemove.Add(lKey);
                }
            }

            for (int i = 0; i < lkeysToRemove.Count; i++)
            {
                s_bsrBaseStateRegistry.Remove(lkeysToRemove[i]);
            }
        }
    }
}