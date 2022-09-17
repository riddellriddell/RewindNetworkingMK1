using System;
using System.Collections.Generic;

/// <summary>
/// this class global messages and stores them in a time arranged buffer
/// </summary>
namespace Networking
{
    public class GlobalMessageBuffer
    {
        //a sorted array of all the unconfirmed messages from all the peers
        public SortedList<SortingValue, PeerMessageNode> UnConfirmedMessageBuffer { get; } = new SortedList<SortingValue, PeerMessageNode>();


        //the messaging state once all the messages after the best chain head have been processed 
        public GlobalMessagingState LatestState { get; } = new GlobalMessagingState();

        //this code is intended for future use in detecting peers missing messages and echoing them  to peer without having to use the chain link system
        #region MessageEchoing
        //
        ////tracks the most recent message recieved from a peer
        //public Dictionary<long, PeerMessageNode> LastMessageRecievedFromPeer { get; } = new Dictionary<long, PeerMessageNode>();
        //
        ////the total number of messages that have ever been sent on this server up to the start of the 
        ////unconfirmed message buffer
        //public int m_iBufferStartIndex;
        //
        ////a rolling hash of all the messages from the start of the message buffer
        //public GlobalMessageNodeHash m_gnhHashAtBufferStart;
        //
        //
        ////the total number of times the message chain has recieved a message behind recieve all head message
        ////this is used to keep track of what messages to echo to peers
        //public int m_iBufferBranchNumber;
        //
        ////the last index hashed up too
        ////public int m_iHashHeadIndex;
        //
        ////the last index inputs were recieved for all users 
        //public int m_iRecieveAllHeadIndex;
        //
        ////the index of the last unhashed input node
        //public int m_iDirtyNodeIndex;
        //
        #endregion

        public SortingValue m_svaStateProcessedUpTo = SortingValue.MinValue;

        //function to add messages to buffer
        public void AddMessageToBuffer(PeerMessageNode pmnMessage, SortingValue svaBestLinkHeadEnd)
        {
            //check if buffer already has item
            if (UnConfirmedMessageBuffer.ContainsKey(pmnMessage.m_svaMessageSortingValue) == false)
            {

                UnConfirmedMessageBuffer.Add(pmnMessage.m_svaMessageSortingValue, pmnMessage);

                //update the earliest change to the message buffer that has not processed
                //check that this change is happening after the best message head (changes before the best link head will be handled when the best head changes)
                if(svaBestLinkHeadEnd.CompareTo(pmnMessage.m_svaMessageSortingValue) < 0)
                {
                    //check if this message is the earliest change to the buffer
                    if(m_svaStateProcessedUpTo.CompareTo(pmnMessage.m_svaMessageSortingValue) > 0)
                    {
                        //store new earliest change so the sim knowes where to reprocess from 
                        m_svaStateProcessedUpTo = pmnMessage.m_svaMessageSortingValue;
                    }
                }

                //re number message indexes in buffer
                //TODO:: optimize this so it only happens when it needs to
                //ReNumberMessageIndexes();

                ////check if latest peer messages are being tracked
                //if (LastMessageRecievedFromPeer.TryGetValue(pmnMessage.m_lPeerID, out PeerMessageNode pmnLastMessage))
                //{
                //    if (pmnLastMessage == null)
                //    {
                //        LastMessageRecievedFromPeer[pmnMessage.m_lPeerID] = pmnMessage;
                //    }
                //    else
                //    {
                //        //check if new message is more recent than previouse message
                //        if (LastMessageRecievedFromPeer[pmnMessage.m_lPeerID].m_svaMessageSortingValue.CompareTo(pmnMessage.m_svaMessageSortingValue) < 0)
                //        {
                //            LastMessageRecievedFromPeer[pmnMessage.m_lPeerID] = pmnMessage;
                //        }
                //    }
                //}
                //else
                //{
                //    LastMessageRecievedFromPeer[pmnMessage.m_lPeerID] = pmnMessage;
                //}
            }
        }

        //adds the effect of all the messages after the last message processed by gmsStartMessageState state 
        //and stores the result in LatestState
        public void UpdateFinalMessageState(long lLocalPeerID, bool bActivePeer, GlobalMessagingState gmsStartMessageState,NetworkingDataBridge ndbNetworkingDataBridge, TimeSpan tspVoteTimeout, int iMaxPlayerCount)
        {
            LatestState.ResetToState(gmsStartMessageState);

            //get the index of the last message processed
            int iStartIndex = UnConfirmedMessageBuffer.IndexOfKey(LatestState.m_svaLastMessageSortValue);
            
            //move on to next message 
            iStartIndex++;

            for (int i = iStartIndex; i < UnConfirmedMessageBuffer.Count; i++)
            {
                //check if message is new and should be added to network bridge message buffer
                if(UnConfirmedMessageBuffer.Values[i].m_svaMessageSortingValue.CompareTo(m_svaStateProcessedUpTo) > -1)
                {
                    LatestState.ProcessMessage(lLocalPeerID, bActivePeer, UnConfirmedMessageBuffer.Values[i], tspVoteTimeout, iMaxPlayerCount, ndbNetworkingDataBridge);
                }
                else    
                {
                    LatestState.ProcessMessage(lLocalPeerID, bActivePeer, UnConfirmedMessageBuffer.Values[i], tspVoteTimeout, iMaxPlayerCount, null);
                }                
            }

            if(UnConfirmedMessageBuffer.Count > 0)
            {
                m_svaStateProcessedUpTo = UnConfirmedMessageBuffer.Values[UnConfirmedMessageBuffer.Count - 1].m_svaMessageSortingValue;
            }            
        }

        public void GetMessageStartAndEndIndexesBetweenStates(GlobalMessagingState gmsStartState, GlobalMessagingState gmsEndState, out int iStartIndex, out int iEndIndex)
        {
            iStartIndex = UnConfirmedMessageBuffer.IndexOfKey(gmsStartState.m_svaLastMessageSortValue);
            iEndIndex = UnConfirmedMessageBuffer.IndexOfKey(gmsEndState.m_svaLastMessageSortValue);

            if(iEndIndex < iStartIndex)
            {
                //TODO: check if this is valid and catch error if it is not
                iEndIndex = iStartIndex;
            }
        }

        //returns a subset of the message buffer that is older than the get message sort value but
        //still contains messages recieved from all active channels excluding channes that have 
        //timed out and are being treated as disconnected or disabled
        public List<PeerMessageNode> GetChainLinkMessages(SortingValue msvGetMessagesFrom, TimeSpan tspConnectionTimeOutTime, DateTime dtmLinkEndTime)
        {
            List<PeerMessageNode> pmnOutput = new List<PeerMessageNode>();

            if (UnConfirmedMessageBuffer.Count == 0)
            {
                return pmnOutput;
            }

            //get the start index
            int iStartIndex = UnConfirmedMessageBuffer.IndexOfKey(msvGetMessagesFrom) + 1;


            //get the last message in time band 
            //PeerMessageNode pmnLastMessage = UnConfirmedMessageBuffer.Values[UnConfirmedMessageBuffer.Values.Count - 1];

            //if no messages for a channel have been recieved for more than tspTreatAsLatestIfOlderThan
            //treat that channel as disconnected / inactive and dont wait to recieve more messages
            //from it before including it in the node list 
            DateTime dtmConnectionTimeOutTime = dtmLinkEndTime;

            if(dtmLinkEndTime.Ticks > tspConnectionTimeOutTime.Ticks)
            {
                dtmConnectionTimeOutTime = dtmConnectionTimeOutTime - tspConnectionTimeOutTime;
            }

            SortingValue msvConnectionTimeOutTime = PeerMessageNode.SortingValueForTime(dtmConnectionTimeOutTime);

            SortingValue msvOldestActiveChannel = LatestState.m_svaLastMessageSortValue;

            //get the last time messages were recieved for all channels 
            //excluding the channels being treated as disconnected;
            for (int i = 0; i < LatestState.m_gmcMessageChannels.Count; i++)
            {
                GlobalMessageChannelState mcsState = LatestState.m_gmcMessageChannels[i];

                //check if channel is being treated as disconnected
                if (mcsState.m_msvLastSortValue.CompareTo(msvConnectionTimeOutTime) > 0)
                {
                    //check when the last channel activity was recorded
                    if (msvOldestActiveChannel.CompareTo(mcsState.m_msvLastSortValue) > 0)
                    {
                        //set the new oldest time / sorting value
                        msvOldestActiveChannel = mcsState.m_msvLastSortValue;
                    }
                }
            }

            //try and get buffer index associated with that time
            int iIndexOfLastValidMessage = UnConfirmedMessageBuffer.IndexOfKey(msvOldestActiveChannel);

            //check if there are any nodes inside that range 
            if (iIndexOfLastValidMessage < iStartIndex)
            {
                return pmnOutput;
            }

            for (int i = iStartIndex; i <= iIndexOfLastValidMessage; i++)
            {
                PeerMessageNode pmnMessage = UnConfirmedMessageBuffer.Values[i];

                //make sure no messages past the end of the link are added 
                if (pmnMessage.m_dtmMessageCreationTime > dtmLinkEndTime)
                {
                    break;
                }

                pmnOutput.Add(pmnMessage);
            }

            return pmnOutput;

        }

        ////the last global index that messages were recieved from all peers that are being tracked 
        //public long GlobalIndexOfLastRecievedMessagesFromAllPeers(List<long> lTrackedPeers)
        //{
        //    long lGlobalIndex = long.MaxValue;
        //
        //    foreach (long lPeerID in lTrackedPeers)
        //    {
        //        if (LastMessageRecievedFromPeer.TryGetValue(lPeerID, out PeerMessageNode pmnNode))
        //        {
        //            lGlobalIndex = Math.Min(lGlobalIndex, pmnNode.m_lGlobalMessageIndex);
        //        }
        //    }
        //
        //    if (lGlobalIndex == int.MaxValue)
        //    {
        //        lGlobalIndex = m_iBufferStartIndex;
        //    }
        //
        //    return lGlobalIndex;
        //}

        //function to remove messages upto point in buffer 
        public void RemoveItemsUpTo(SortingValue msvRemoveToo)
        {
            while(UnConfirmedMessageBuffer.Count > 0)
            {
                if(UnConfirmedMessageBuffer.Keys[0].CompareTo(msvRemoveToo) < 0)
                {
                    UnConfirmedMessageBuffer.RemoveAt(0);
                }
                else
                {
                    break;
                }
            }            
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
        //protected void ReNumberMessageIndexes()
        //{
        //    int iStartNumber = m_iBufferStartIndex;
        //
        //    foreach (PeerMessageNode pmnNode in UnConfirmedMessageBuffer.Values)
        //    {
        //        iStartNumber++;
        //
        //        pmnNode.m_lGlobalMessageIndex = iStartNumber;
        //
        //    }
        //}

        protected bool CheckHash(int iStartIndex, int iEndIndex, long lHash)
        {
            return false;
        }

        //generate hash for range
        public long GetShortHashForRange(int iStartIndex, int iEndIndex)
        {
            return 0;
        }

    }
}