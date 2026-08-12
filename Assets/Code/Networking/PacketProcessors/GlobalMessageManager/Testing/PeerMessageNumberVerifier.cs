using System;
using System.Collections.Generic;
using System.Numerics;
using UnityEngine;

namespace Networking
{
    public class PeerMessageNumberVerifier
    {
        private static Dictionary<long, uint> s_dicLastPeerNum = new Dictionary<long, uint>();

        public static bool ValidateMessageWithSameSortValueDoesntExist(PeerMessageNode pmnNodeToAdd, GlobalMessageBuffer gmbMessageBuffer)
        {
            foreach (PeerMessageNode pmnMessage in gmbMessageBuffer.UnConfirmedMessageBuffer.Values)
            {
                if (pmnMessage.m_svaMessageSortingValue.CompareTo(pmnNodeToAdd.m_svaMessageSortingValue) == 0)
                {
                    return false;
                }
                    
            }
            
            foreach (SortingValue svaKey in gmbMessageBuffer.UnConfirmedMessageBuffer.Keys)
            {
                if (svaKey.CompareTo(pmnNodeToAdd.m_svaMessageSortingValue) == 0)
                {
                    return false;
                }
                    
            }

            return true;
        }
        
        public static bool ValidateNewMessageIndex(long lPeerID, uint lMessageNumber)
        {
            //check if peer exists
            if (s_dicLastPeerNum.ContainsKey(lPeerID) == false)
            {
                s_dicLastPeerNum.Add(lPeerID, lMessageNumber);

                return true;
            }
            else
            {
                //check if peer message is one less than the message that is about to be created
                if (s_dicLastPeerNum[lPeerID] != lMessageNumber - 1)
                {
                    return false;
                }
                
                s_dicLastPeerNum[lPeerID] = lMessageNumber;
            }

            return true;
        }

        public static uint GetPeerMessageIndex(long lPeerID)
        {
            if (s_dicLastPeerNum.ContainsKey(lPeerID) == false)
            {
                return 0;
            }
            
            return s_dicLastPeerNum[lPeerID];
        }

        public static void GivenAMessageTrackIndexBackAndCheckForIndexGap(GlobalMessageBuffer gmbMessageBuffer,
            PeerMessageNode msgMessage)
        {
            //find the message in the unconfirmed message buffer
            int index = gmbMessageBuffer.UnConfirmedMessageBuffer.IndexOfKey(msgMessage.m_svaMessageSortingValue);
            
            //check if failed
            if (index == -1)
            {
                //this message might not have been added yet
                index = gmbMessageBuffer.UnConfirmedMessageBuffer.Count;
            }

            uint iActiveIndex = msgMessage.m_iPeerMessageIndex -1;

            List<uint> lstMissingIndexes = new List<uint>();
            uint iYoungestIndex = msgMessage.m_iPeerMessageIndex;

            string strMissingMessageIndexes = "";
            
            for (int i = index -1; i >= 0 && iActiveIndex != uint.MaxValue; i--)
            {
                PeerMessageNode pmnMessageAtIndex = gmbMessageBuffer.UnConfirmedMessageBuffer.Values[i];
                
                //skip if not by peer 
                if (pmnMessageAtIndex.m_lPeerID != msgMessage.m_lPeerID)
                {
                    continue;
                }
                
                //check if index is target index
                if (pmnMessageAtIndex.m_iPeerMessageIndex != iActiveIndex)
                {
                    //we have missed an index
                    lstMissingIndexes.Add(iActiveIndex);
                    strMissingMessageIndexes += $"{iActiveIndex}, ";
                }
                else if ( pmnMessageAtIndex.m_iPeerMessageIndex > iActiveIndex)
                {
                    Debug.LogError($"Indexes for peer {pmnMessageAtIndex.m_lPeerID} out of order, " +
                                   $"index expected {iActiveIndex}, " +
                                   $"index found {pmnMessageAtIndex.m_iPeerMessageIndex}");
                }
                else
                {
                    
                }

                iYoungestIndex = Math.Min(iYoungestIndex, pmnMessageAtIndex.m_iPeerMessageIndex);
                iActiveIndex--;
            }

            if (lstMissingIndexes.Count > 0)
            {
                Debug.LogError($"PeerMessageNumberVerifier.GivenAMessageTrackIndexBackAndCheckForIndexGap: " +
                               $"Peer message {msgMessage.m_lPeerID} with index {msgMessage.m_iPeerMessageIndex} skipping messages {strMissingMessageIndexes} " +
                               $"with youngest message for peer {iYoungestIndex}");
            }
        }
    }
}