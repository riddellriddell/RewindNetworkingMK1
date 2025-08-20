using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using SharedTypes;
using UnityEngine;
using Utility;

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

        public static void ValidateSimMessageBufferMatchesUpToLink(ChainLink chlLink, NetworkingDataBridge ndmDataBridge, long lPeerUpdatingBase)
        {
            //values to store the start and end times of this chain
            SortingValue svlOldSortValue = SortingValue.MaxValue;
            SortingValue svlNewSortValue = SortingValue.MinValue;

            ulong lMessagesCounted = 0;
            ulong iMessagesReportedAtEndOfNewest = chlLink.m_lChainMessageCount;
            ulong iMessagesReportedAtStartOfOldest = chlLink.m_lChainMessageCount;

            Dictionary<SortingValue, string> dicNodeTypeAtTime = new Dictionary<SortingValue, string>();

            //get the oldest chain link
            //while getting the oldest chain link add up all the messages 
            //found along the way
            while (chlLink.m_chlParentChainLink != null)
            {
                //get the reported message count at the start instead of the end of the chain
                iMessagesReportedAtStartOfOldest = chlLink.m_lChainMessageCount - (ulong)chlLink.m_pmnMessages.Count;

                //add up all the messages
                if (chlLink.m_pmnMessages.Count > 0)
                {
                    lMessagesCounted += (ulong)chlLink.m_pmnMessages.Count;

                    //try get the newest value if the newest value has not yet been calculated
                    if (svlNewSortValue.CompareTo(SortingValue.MinValue) == 0)
                    {
                        svlNewSortValue = chlLink.m_pmnMessages[chlLink.m_pmnMessages.Count - 1].m_svaMessageSortingValue;
                    }

                    //get the start time of this chain
                    svlOldSortValue = chlLink.m_pmnMessages[0].m_svaMessageSortingValue;

                    for(int i = 0; i < chlLink.m_pmnMessages.Count; i++)
                    {
                        //check that the oldest message is the oldest and the newest messages is the newest
                        if((svlOldSortValue > chlLink.m_pmnMessages[i].m_svaMessageSortingValue) || (svlNewSortValue < chlLink.m_pmnMessages[i].m_svaMessageSortingValue))
                        {
                            Debug.LogError($"Peer: {lPeerUpdatingBase} Chain Link Message List Not Sorted");
                        }
                        
                        //sanity check that there are not more than 1 message occuring at the same time
                        if(dicNodeTypeAtTime.ContainsKey(chlLink.m_pmnMessages[i].m_svaMessageSortingValue ))
                        {
                            Debug.LogError($"Peer: {lPeerUpdatingBase} Chain Link Message contains multiple messages with" +
                                           $"same sorting values" );
                        }
                        else
                        {
                            //add message time and type to help with debugging
                            dicNodeTypeAtTime.Add(chlLink.m_pmnMessages[i].m_svaMessageSortingValue, $"Chain link message Type ID: { chlLink.m_pmnMessages[i].m_bMessageType.ToString()}");
                        }
                        

                    }
                }

                chlLink = chlLink.m_chlParentChainLink;
            }

            ulong lReportedMessageCountInTargetChainToBase = iMessagesReportedAtEndOfNewest - iMessagesReportedAtStartOfOldest;

            if(lReportedMessageCountInTargetChainToBase != lMessagesCounted)
            {
                Debug.LogError($"Peer: {lPeerUpdatingBase} Error when comparing reported chain lengths to count lenght. reported length {lReportedMessageCountInTargetChainToBase}, Counted Length {lMessagesCounted}");
            }

            //get the number of messages in the buffer sent to the sim
            ndmDataBridge.m_squInMessageQueue.TryGetFirstIndexGreaterThan(svlOldSortValue, out int iNextStartIndex, out bool bStartCollision, out int iStartCollisionIndex);
            ndmDataBridge.m_squInMessageQueue.TryGetFirstIndexLessThan(svlNewSortValue, out int iNextEndIndex, out bool bEndCollision, out int iEndCollisionIndex);

            if ((bStartCollision == false || bEndCollision == false) && lReportedMessageCountInTargetChainToBase > 0)
            {
                Debug.LogError($"Peer: {lPeerUpdatingBase} Start or end messages in chain were not found in sim message buffer, start message collision:{ bStartCollision } end message collision:{ bEndCollision }");
            }

            long lMessagesInBuffer = iEndCollisionIndex - iStartCollisionIndex + 1;

            if (lReportedMessageCountInTargetChainToBase > 0 && lReportedMessageCountInTargetChainToBase != (ulong)lMessagesInBuffer)
            {
                Debug.LogError($"Peer: {lPeerUpdatingBase} The number of messages in the message buffer: {lMessagesInBuffer} dont match the number of messages in the chain links over the same time {lReportedMessageCountInTargetChainToBase}");
                
                //loop through mesasges and try and find the messages that were not the same
                for (int i = iStartCollisionIndex; i <= iEndCollisionIndex; i++)
                {
                    SortingValue svaInputTime = ndmDataBridge.m_squInMessageQueue.GetKeyAtIndex(i);
                    
                    //get the key to see if it already exists 
                    if (dicNodeTypeAtTime.ContainsKey(svaInputTime))
                    {
                        dicNodeTypeAtTime.Remove(svaInputTime);
                        //not a unique message
                        continue;
                    }
                    else
                    {
                        //add it to the dictionary
                        dicNodeTypeAtTime.Add( svaInputTime, $"Data bridge message type: {ndmDataBridge.m_squInMessageQueue.GetValueAtIndex(i).GetType().ToString()}");
                    }
                }
                
                //loop over all the types and print them out
                foreach( var kvpType in dicNodeTypeAtTime)
                {
                    Debug.LogError( $"Miss matched message info: Key:{ kvpType.Key.ToString() }, Type: { kvpType.Value.ToString() }");
                }
                
            }
        }

        public static void RegisterState(GlobalMessagingState gmdState,uint iIndex, long lPeerRegistering)
        {

            //remove old link data
            CleanUpRegistry(iIndex);

            //calculate link end state hash 
            long lChainLinkHash = 0;

            //create byte stream big enough for state to write to
            WriteByteStream wbsStream = new WriteByteStream(NetworkingByteStream.DataSize(gmdState));

            //serialize state
            NetworkingByteStream.Serialize(wbsStream, ref gmdState);

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