using System;
using System.Collections.Generic;

namespace Networking
{
    //this represents one channel of input (usually for one player in the sim)
    public class GlobalMessageChannelState : ICloneable
    {
        //the state of the channel
        public enum State : byte
        {
            Empty, // not in use
            VoteJoin, // peers are voting on if channel should be assigned to peer
            VoteKick, // peers are voting of if peed should be kicked / split off
            Assigned //channel assigned to peer
        }

        //any votes that are associated with this channel
        public struct ChannelVote
        {
            public enum VoteType : byte
            {
                None, //no active vote for channel
                Kick, //kick the player 
                Add, // add a player to the channel
            }

            public VoteType m_vtpVoteType;

            //the time this vote was made
            public DateTime m_dtmVoteTime;

            //the target peer
            public long m_lPeerID;

            public bool IsActive(DateTime dtmCurrentTime, TimeSpan tspTimeOutTime)
            {
                if (dtmCurrentTime < m_dtmVoteTime)
                {
                    //should not get into this state 
                    return false;
                }

                if (dtmCurrentTime - m_dtmVoteTime > tspTimeOutTime)
                {
                    return false;
                }

                return true;
            }
        }

        //the peer that is currently assigned to this input channel
        public long m_lChannelPeer;

        //the time when voting on assigning peer to this channel started
        public DateTime m_dtmVoteTime;

        //the current state of this channel
        public State m_staState;

        //list of all the active votes by this channel on other channels
        public List<ChannelVote> m_chvVotes;

        //the hash of the last valid node processed for this channel
        public long m_lHashOfLastNodeProcessed;

        //the index of the last valid message processed 
        public UInt32 m_iLastMessageIndexProcessed;
        
        //the chain link head this peer is using in the last valid message 
        public long m_lChainLinkHeadHash;
        
        //the message sort value of the last valid message processed by this channel
        public SortingValue m_msvLastSortValue;

        public GlobalMessageChannelState(int iMaxNumberOfPeers)
        {
            Init(iMaxNumberOfPeers);
        }

        //changes the data in this class to match that of the passed channel state 
        public void ResetToState(GlobalMessageChannelState chsChannelState)
        {
            m_lChannelPeer = chsChannelState.m_lChannelPeer;

            m_dtmVoteTime = chsChannelState.m_dtmVoteTime;

            m_staState = chsChannelState.m_staState;

            //copy across votes 
            m_chvVotes.Clear();
            for (int i = 0; i < chsChannelState.m_chvVotes.Count; i++)
            {
                m_chvVotes.Add(chsChannelState.m_chvVotes[i]);
            }

            m_lHashOfLastNodeProcessed = chsChannelState.m_lHashOfLastNodeProcessed;

            m_iLastMessageIndexProcessed = chsChannelState.m_iLastMessageIndexProcessed;

            m_lChainLinkHeadHash = chsChannelState.m_lChainLinkHeadHash;

            m_msvLastSortValue = chsChannelState.m_msvLastSortValue;
        }

        //setup
        public void Init(int iMaxPeerCount)
        {
            //mark channel as empty
            m_lChannelPeer = long.MinValue;

            m_staState = State.Empty;

            m_dtmVoteTime = DateTime.MinValue;

            m_chvVotes = new List<ChannelVote>(iMaxPeerCount);

            for (int i = 0; i < iMaxPeerCount; i++)
            {
                m_chvVotes.Add(new ChannelVote()
                {
                    m_lPeerID = 0,
                    m_dtmVoteTime = DateTime.MinValue,
                    m_vtpVoteType = ChannelVote.VoteType.None
                });
            }

            m_lHashOfLastNodeProcessed = 0;
            m_iLastMessageIndexProcessed = 0;
            m_msvLastSortValue = SortingValue.MinValue;

        }

        //process channel change message
        public void AddKickVote(int iPeerChannelIndex, DateTime dtmCreationTime, long lTargetPeerID)
        {
            //channel vote 
            m_chvVotes[iPeerChannelIndex] = new ChannelVote()
            {
                m_dtmVoteTime = dtmCreationTime,
                m_lPeerID = lTargetPeerID,
                m_vtpVoteType = ChannelVote.VoteType.Kick
            };
        }

        //add connection vote
        public void AddConnectionVote(int iPeerChannelIndex, DateTime dtmCreationTime, long lTargetPeerID)
        {
            //channel vote 
            m_chvVotes[iPeerChannelIndex] = new ChannelVote()
            {
                m_dtmVoteTime = dtmCreationTime,
                m_lPeerID = lTargetPeerID,
                m_vtpVoteType = ChannelVote.VoteType.Add
            };
        }

        //clear channel
        public void ClearChannel()
        {
            ClearVotes();

            //reset peer
            m_lChannelPeer = long.MinValue;
            m_dtmVoteTime = DateTime.MinValue;
            m_staState = State.Empty;

            //reset hash head
            m_lHashOfLastNodeProcessed = 0;
            m_iLastMessageIndexProcessed = 0;
        }

        //clear all votes by channel
        public void ClearVotes()
        {
            //clear all votes
            for (int i = 0; i < m_chvVotes.Count; i++)
            {
                m_chvVotes[i] = new ChannelVote()
                {
                    m_lPeerID = 0,
                    m_dtmVoteTime = DateTime.MinValue,
                    m_vtpVoteType = ChannelVote.VoteType.None
                };
            }
        }

        //start vote on channel to assign peer to it
        public void StartVoteJoinForPeer(long lPeerID, DateTime dtmVoteStartTime)
        {
            ClearVotes();
            m_lChannelPeer = lPeerID;
            m_dtmVoteTime = dtmVoteStartTime;
            m_staState = State.VoteJoin;
        }

        public void StartVoteKickForPeer(DateTime dtmVoteStartTime)
        {
            m_dtmVoteTime = dtmVoteStartTime;
            m_staState = State.VoteKick;
        }

        //make the peer with id lPeerID in control of this channel
        public void AssignPeerToChannel(long lPeerID, DateTime dtmTimeOfJoin)
        {
            ClearVotes();

            m_lChannelPeer = lPeerID;
            m_staState = State.Assigned;
            m_dtmVoteTime = dtmTimeOfJoin;
        }

        //gets index of any vote for peer 
        public bool TryGetVoteForPeer(long lTargetPeerID, out int iIndex, out ChannelVote chvVote)
        {
            for (int i = 0; i < m_chvVotes.Count; i++)
            {
                if (m_chvVotes[i].m_lPeerID == lTargetPeerID)
                {
                    iIndex = i;
                    chvVote = m_chvVotes[i];

                    return true;
                }
            }

            iIndex = 0;
            chvVote = m_chvVotes[0];

            return false;
        }

        //clear votes on channel
        //this happens when a vote on a channel is completed 
        public void ClearVotesForChannelIndex(int iChannelToClearVotesFor)
        {
            m_chvVotes[iChannelToClearVotesFor] = new ChannelVote()
            {
                m_lPeerID = 0,
                m_dtmVoteTime = DateTime.MinValue,
                m_vtpVoteType = ChannelVote.VoteType.None
            };
        }

        //clear connect votes for peer
        public void ClearVoteForPeer(long lTargetPeerID)
        {
            if (TryGetVoteForPeer(lTargetPeerID, out int iIndex, out ChannelVote chvVote))
            {
                ClearVotesForChannelIndex(iIndex);
            }
        }

        public object Clone()
        {
            throw new NotImplementedException();
        }

    }

    public partial class ByteStream
    {
        //read and write one vote
        public static void Serialize(ReadByteStream rbsByteStream, ref GlobalMessageChannelState.ChannelVote Output)
        {
            byte bVoteType = 0;

            Serialize(rbsByteStream, ref bVoteType);

            Output.m_vtpVoteType = (GlobalMessageChannelState.ChannelVote.VoteType)bVoteType;

            Serialize(rbsByteStream, ref Output.m_dtmVoteTime);

            Serialize(rbsByteStream, ref Output.m_lPeerID);
        }

        public static void Serialize(WriteByteStream wbsByteStream, ref GlobalMessageChannelState.ChannelVote Input)
        {
            byte bVoteType = (byte)Input.m_vtpVoteType;

            Serialize(wbsByteStream, ref bVoteType);

            Serialize(wbsByteStream, ref Input.m_dtmVoteTime);

            Serialize(wbsByteStream, ref Input.m_lPeerID);
        }

        public static int DataSize(GlobalMessageChannelState.ChannelVote Input)
        {
            int iSize = 0;
            iSize += DataSize(Input.m_dtmVoteTime);
            iSize += DataSize(Input.m_lPeerID);
            iSize += DataSize((byte)Input.m_vtpVoteType);
            return iSize;
        }

        //serialize guns
        public static void Serialize(ReadByteStream rbsByteStream, ref GlobalMessageChannelState Output)
        {
            //player count
            int iPlayerCount = 0;

            Serialize(rbsByteStream, ref iPlayerCount);

            if(Output == null)
            {
                Output = new GlobalMessageChannelState(iPlayerCount);
            }

            //votes 
            Output.m_chvVotes = new List<GlobalMessageChannelState.ChannelVote>(iPlayerCount);

            for(int i = 0; i < iPlayerCount; i++)
            {
                GlobalMessageChannelState.ChannelVote cvhtVote = new GlobalMessageChannelState.ChannelVote();

                Serialize(rbsByteStream, ref cvhtVote);

                Output.m_chvVotes.Add(cvhtVote);
            }

            //assigned peer
            Serialize(rbsByteStream, ref Output.m_lChannelPeer);
            
            //time of last vote start
            Serialize(rbsByteStream, ref Output.m_dtmVoteTime);

            //state
            byte bState = 0;
            Serialize(rbsByteStream, ref bState);
            Output.m_staState = (GlobalMessageChannelState.State)bState;

            //hash of last node processed
            Serialize(rbsByteStream, ref Output.m_lHashOfLastNodeProcessed);

            //last message index processed 
            Serialize(rbsByteStream, ref Output.m_iLastMessageIndexProcessed);

            //best chain link hash 
            Serialize(rbsByteStream, ref Output.m_lChainLinkHeadHash);
            
            //the sorting value of the last valid message processed 
            Serialize(rbsByteStream, ref Output.m_msvLastSortValue);
        }

        public static void Serialize(WriteByteStream wbsByteStream, ref GlobalMessageChannelState Input)
        {
            //player count
            int iPlayerCount = Input.m_chvVotes.Count;

            Serialize(wbsByteStream, ref iPlayerCount);

            //votes
            for (int i = 0; i < iPlayerCount; i++)
            {
                GlobalMessageChannelState.ChannelVote chvVote = Input.m_chvVotes[i];

                Serialize(wbsByteStream, ref chvVote);
            }

            //assigned peer
            Serialize(wbsByteStream, ref Input.m_lChannelPeer);

            //time of last vote
            Serialize(wbsByteStream, ref Input.m_dtmVoteTime);

            //state
            byte bState = (byte)Input.m_staState;
            Serialize(wbsByteStream, ref bState);

            //hash of last node processed 
            Serialize(wbsByteStream, ref Input.m_lHashOfLastNodeProcessed);

            //last message index processed 
            Serialize(wbsByteStream, ref Input.m_iLastMessageIndexProcessed);

            //best chain link hash
            Serialize(wbsByteStream, ref Input.m_lChainLinkHeadHash);

            //the sorting value of the last valid message processed 
            Serialize(wbsByteStream, ref Input.m_msvLastSortValue);
        }

        public static int DataSize(GlobalMessageChannelState Input)
        {
            int iSize = 0;
            iSize += DataSize(Input.m_chvVotes.Count);

            for(int i = 0; i < Input.m_chvVotes.Count; i++)
            {
                iSize += DataSize(Input.m_chvVotes[i]);
            }

            iSize += DataSize(Input.m_lChainLinkHeadHash);
            iSize += DataSize(Input.m_dtmVoteTime);
            iSize += DataSize(Input.m_iLastMessageIndexProcessed);
            iSize += DataSize(Input.m_lChannelPeer);
            iSize += DataSize(Input.m_lHashOfLastNodeProcessed);
            iSize += DataSize(Input.m_msvLastSortValue);
            iSize += DataSize((byte)Input.m_staState);

            return iSize;
        }
    }
}
