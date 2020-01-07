using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// this class global messages and stores them in a time arranged buffer
/// </summary>
namespace Networking
{
    public class GlobalMessageBuffer
    {
        //this class is used internally when getting subsets of the main buffer
        private class PeerBoundry : IPeerMessageNode
        {
            public static PeerBoundry MinTimeInclusive(DateTime dtmMinTime)
            {
                PeerBoundry pbdMin = new PeerBoundry();

                pbdMin.SourcePeerID = long.MinValue;
                pbdMin.MessageCreationTime = dtmMinTime;
                pbdMin.TypeSorting = int.MinValue;
                pbdMin.PeerMessageIndex = int.MinValue;
                pbdMin.NodeHash = long.MinValue;

                return pbdMin;
            }

            public static PeerBoundry MaxTimeExclusive(DateTime dtmMinTime)
            {
                PeerBoundry pbdMax = new PeerBoundry();

                pbdMax.SourcePeerID = long.MaxValue;
                pbdMax.MessageCreationTime = new DateTime(dtmMinTime.Ticks -1);
                pbdMax.TypeSorting = int.MaxValue;
                pbdMax.PeerMessageIndex = int.MaxValue;
                pbdMax.NodeHash = long.MaxValue;

                return pbdMax;
            }

            public long SourcePeerID { get; set;}

            public DateTime MessageCreationTime { get; set; }

            public int TypeSorting { get; set; }

            public int PeerMessageIndex { get; set; }

            public long NodeHash { get; set; }

            public GlobalMessageBase GlobalMessage => throw new NotImplementedException();

            public object Clone()
            {
                throw new NotImplementedException();
            }

            public bool Validate(IPeerMessageNode pmnPreviousNode, long lSourcePeer, Dictionary<long, byte[]> bPublicKeyDictionary, DateTime dtmNetworkTimeRecieved)
            {
                throw new NotImplementedException();
            }

            public int CompareTo(IPeerMessageNode pmnCompareTo)
            {

                if (pmnCompareTo  == null)
                {
                    return 1;
                }

                //sort items from oldest to youngest
                if (MessageCreationTime < pmnCompareTo.MessageCreationTime)
                {
                    return -1;
                }

                if (MessageCreationTime > pmnCompareTo.MessageCreationTime)
                {
                    return 1;
                }

                //sort items for order
                if (PeerMessageIndex < pmnCompareTo.PeerMessageIndex)
                {
                    return -1;
                }

                if (PeerMessageIndex > pmnCompareTo.PeerMessageIndex)
                {
                    return 1;
                }

                //sort items by type order
                if (TypeSorting < pmnCompareTo.TypeSorting)
                {
                    return -1;
                }

                if (TypeSorting > pmnCompareTo.TypeSorting)
                {
                    return 1;
                }

                //sort by user 
                if (SourcePeerID < pmnCompareTo.SourcePeerID)
                {
                    return -1;
                }

                if (SourcePeerID > pmnCompareTo.SourcePeerID)
                {
                    return 1;
                }

                //if all else is equal sort by hash
                if (NodeHash < pmnCompareTo.NodeHash)
                {
                    return -1;
                }
                else if (NodeHash > pmnCompareTo.NodeHash)
                {
                    return 1;
                }
                else
                {
                    //must be same message 
                    return 0;
                }
            }

            public bool ValidateNodeSources(Dictionary<long, byte[]> bPublicKeyDictionary)
            {
                throw new NotImplementedException();
            }
        }

        //sorts messages from all peers onto a timeline 
        private class MessageComparer : IComparer<IPeerMessageNode>
        {
            public int Compare(IPeerMessageNode x, IPeerMessageNode y)
            {
                if(x == null && y == null)
                {
                    return 0;
                }

                if(x == null)
                {
                    return -1;
                }

                return x.CompareTo(y);
            }
        }
               
        //a sorted array of all the unconfirmed messages from all the peers
        public SortedSet<IPeerMessageNode> UnConfirmedMessageBuffer { get; } = new SortedSet<IPeerMessageNode>(new MessageComparer());

        //tracks the most recent message recieved from a peer
        public Dictionary<long, IPeerMessageNode> LastMessageRecievedFromPeer { get; } = new Dictionary<long, IPeerMessageNode>();

        //the total number of messages that have ever been sent on this server up to the start of the 
        //unconfirmed message buffer
        public int m_iBufferStartIndex;

        //a rolling hash of all the messages from the start of the message buffer
        public GlobalMessageNodeHash m_gnhHashAtBufferStart;

        //the total number of times the message chain has recieved a message behind recieve all head message
        //this is used to keep track of what messages to echo to peers
        public int m_iBufferBranchNumber;

        //the last index hashed up too
        //public int m_iHashHeadIndex;

        //the last index inputs were recieved for all users 
        public int m_iRecieveAllHeadIndex;

        //the index of the last unhashed input node
        public int m_iDirtyNodeIndex;

        //function to add messages to buffer
        public void AddMessageToBuffer(IPeerMessageNode pmnMessage,List<long> lTrackedPeers)
        {
            //check if buffer already has item
            if (UnConfirmedMessageBuffer.Contains(pmnMessage) == false)
            {
                //set message branch number
                pmnMessage.GlobalBranchNumber = m_iBufferBranchNumber;

                UnConfirmedMessageBuffer.Add(pmnMessage);

                //re number message indexes in buffer
                //TODO:: optimize this so it only happens when it needs to
                ReNumberMessageIndexes();

                //check if latest peer messages are being tracked
                if (LastMessageRecievedFromPeer.TryGetValue(pmnMessage.SourcePeerID,out IPeerMessageNode pmnLastMessage))
                {
                    if(pmnLastMessage == null)
                    {
                        LastMessageRecievedFromPeer[pmnMessage.SourcePeerID] = pmnMessage;

                        //increment branch number
                        m_iBufferBranchNumber++;

                        //flag all messages after index as branch
                        MarkMessagesAsBranched(pmnMessage.GlobalMessageIndex, m_iBufferBranchNumber);
                    }
                    else
                    {
                        //check if new message is more recent than previouse message
                        if(LastMessageRecievedFromPeer[pmnMessage.SourcePeerID].GlobalMessageIndex < pmnMessage.GlobalMessageIndex)
                        {
                            LastMessageRecievedFromPeer[pmnMessage.SourcePeerID] = pmnMessage;

                            m_iRecieveAllHeadIndex = GlobalIndexOfLastRecievedMessagesFromAllPeers(lTrackedPeers);
                        }
                        else
                        {
                            //Flag all messages after added message as branch
                            
                            //increment branch number
                            m_iBufferBranchNumber++;

                            //flag all messages after index as branch
                            MarkMessagesAsBranched(pmnMessage.GlobalMessageIndex, m_iBufferBranchNumber);
                        }
                    }
                }

                //check if message is a kick message 

            }
        }

        //the last global index that messages were recieved from all peers that are being tracked 
        public int GlobalIndexOfLastRecievedMessagesFromAllPeers(List<long> lTrackedPeers)
        {
            int iGlobalIndex = int.MaxValue; 

            foreach(long lPeerID in lTrackedPeers)
            {
                if(LastMessageRecievedFromPeer.TryGetValue(lPeerID,out IPeerMessageNode pmnNode))
                {
                    iGlobalIndex = Mathf.Min(iGlobalIndex, pmnNode.GlobalMessageIndex);
                }
            }

            if(iGlobalIndex == int.MaxValue)
            {
                iGlobalIndex = m_iBufferStartIndex;
            }

            return iGlobalIndex;
        }
               
        //function to pull messages from buffer 
        public SortedSet<IPeerMessageNode> GetMessagesBetweenTimes(DateTime dtmStartInclusive, DateTime dtmEndExclusive)
        {
           return UnConfirmedMessageBuffer.GetViewBetween(PeerBoundry.MinTimeInclusive(dtmStartInclusive), PeerBoundry.MaxTimeExclusive(dtmEndExclusive));
        }

        //function to remove messages upto point in buffer 
        public void RemoveItemsUpTo(IPeerMessageNode pmnRemoveToo)
        {
            UnConfirmedMessageBuffer.RemoveWhere(pmnMessage => pmnRemoveToo.CompareTo(pmnMessage) > 0);
        }
               
        //update the hash for the buffer
        //public void UpdateBufferHash(List<long> lPlayersInGame)
        //{
        //    GlobalMessageNodeHash gnhLastNodeHash = m_gnhHashAtBufferStart;
        //
        //    m_iHashHeadIndex = GlobalIndexOfLastRecievedMessagesFromAllPeers(lPlayersInGame);
        //
        //    //check if any nodes need to be rehashed 
        //    if(m_iHashHeadIndex < m_iDirtyNodeIndex)
        //    {
        //        return;
        //    }
        //
        //    //TODO:: only rehash from the index of the last added item
        //    foreach (IPeerMessageNode pmnNode in UnConfirmedMessageBuffer)
        //    {
        //        //skip nodes that have already been hashed and have not had a hash chain change
        //        if(pmnNode.GlobalMessageIndex < m_iDirtyNodeIndex)
        //        {
        //            //if this is the last node before node chain is dirty set hash
        //            if(pmnNode.GlobalMessageIndex == m_iDirtyNodeIndex -1)
        //            {
        //                gnhLastNodeHash = pmnNode.GlobalHash;
        //            }
        //
        //            continue;
        //        }
        //        
        //        //only hash while you have inputs for all tracked peers 
        //        //(no point in hashing when new valid inputs could invalidate hash)
        //        if(pmnNode.GlobalMessageIndex > m_iHashHeadIndex)
        //        {
        //            break;
        //        }
        //
        //        //calclate node hash
        //        GlobalMessageNodeHash gnhNodeHash = CalculateNodeHash(pmnNode, gnhLastNodeHash);
        //
        //        //store node hash
        //        pmnNode.GlobalHash = gnhNodeHash;
        //
        //        gnhLastNodeHash = gnhNodeHash;
        //    }
        //}

        //adds the correct buffer index numbers 
        protected void ReNumberMessageIndexes()
        {
            int iStartNumber = m_iBufferStartIndex;

            foreach(IPeerMessageNode pmnNode in UnConfirmedMessageBuffer)
            {
                iStartNumber++;
                pmnNode.GlobalMessageIndex = iStartNumber;
            }
        }

        //when a message is recieved that is out of order / before a message recieved from the same peer
        //it causes a branch / change in the acknowledged message timeline. these branches are numbered 
        //to keep track of what messages need to be echoed to which peers 
        protected void MarkMessagesAsBranched(int iStartIndex, int iBranchNumber)
        {
            //loop through all messages 
            foreach(IPeerMessageNode pmnMessage in UnConfirmedMessageBuffer)
            {
                //check if node is before branch
                if(pmnMessage.GlobalMessageIndex < iStartIndex)
                {
                    //skip message
                    continue;
                }

                //change message branch number
                pmnMessage.GlobalBranchNumber = iBranchNumber;
            }
        }

        protected bool CheckHash(int iStartIndex, int iEndIndex,long lHash)
        {

        }

        //generate hash for range
        public long GetShortHashForRange(int iStartIndex, int iEndIndex)
        {

        }

        //calculate node hash
        //protected GlobalMessageNodeHash CalculateNodeHash(IPeerMessageNode pmnNode, GlobalMessageNodeHash gnhPastNodeHash)
        //{
        //    throw new NotImplementedException();
        //
        //    //return new GlobalMessageNodeHash();
        //}
    }
}