using System.Collections.Generic;

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
    }
}