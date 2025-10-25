using System;
using System.Collections.Generic;
using UnityEngine;
using Utility;

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
                Add, // add a player to the channel
                AntiAdd, // add a player to the channel
                Kick, //kick the player 
                AntiKick, //don't aggree with the current kick command for this channle
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
                    Debug.LogError($"The time of vote { m_dtmVoteTime.ToString()} is later than the current time { dtmCurrentTime.ToString()}. this should never happen.");
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
        public DateTime m_dtmJoinVoteTime;
        
        //the time when voting on assigning peer to this channel started
        public DateTime m_dtmKickVoteTime;
        
        //the time when the target peer was assigned
        public DateTime m_dtmAssignedTime;

        //the current state of this channel
        public State m_staState;

        //list of all votes on this channel made by other peers
        public Dictionary<long, ChannelVote.VoteType> m_vtyVotesOnChannelByPeers;
        
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
        public void ResetToState(in GlobalMessageChannelState chsChannelState)
        {
            m_lChannelPeer = chsChannelState.m_lChannelPeer;

            m_dtmJoinVoteTime = chsChannelState.m_dtmJoinVoteTime;
            
            m_dtmKickVoteTime = chsChannelState.m_dtmKickVoteTime;
            
            m_dtmAssignedTime = chsChannelState.m_dtmAssignedTime;

            m_staState = chsChannelState.m_staState;
            
            m_vtyVotesOnChannelByPeers = new Dictionary<long, ChannelVote.VoteType>(chsChannelState.m_vtyVotesOnChannelByPeers);

            m_chvVotes = new List<ChannelVote>(chsChannelState.m_chvVotes);

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
            
            m_vtyVotesOnChannelByPeers = new Dictionary<long, ChannelVote.VoteType>(iMaxPeerCount);
            
            m_dtmJoinVoteTime = DateTime.MinValue;
            m_dtmKickVoteTime = DateTime.MinValue;
            m_dtmAssignedTime = DateTime.MinValue;

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
        
        public void AddOldStyleVote(int iPeerChannelIndex, DateTime dtmCreationTime, long lTargetPeerID,
            ChannelVote.VoteType vtyVoteType)
        {
            //channel vote 
            m_chvVotes[iPeerChannelIndex] = new ChannelVote()
            {
                m_dtmVoteTime = dtmCreationTime,
                m_lPeerID = lTargetPeerID,
                m_vtpVoteType = vtyVoteType
            };
        }

        public void AddNewStyleVote(long lPeerMakingTheVote, ChannelVote.VoteType vtyVoteType)
        {
            //add or overwrite the vote 
            m_vtyVotesOnChannelByPeers[lPeerMakingTheVote] = vtyVoteType;
        }

        public void RemoveVoteByPeer(long lPeerToRemoveTheVoteOf)
        {
            m_vtyVotesOnChannelByPeers.Remove(lPeerToRemoveTheVoteOf);
        }
        
        //has enough people voted on this channel that there is no chance for the 
        //vote result to change, eg if you need 51% approve out of 10 people an 6 have voted yes
        //then we don't need to wait for the last 4 people
        public bool IsVoteResultCertain(int iNumberOfActivePeers)
        {
            //number of votes needed (50%)
            int iMinVotesForSuccess = (iNumberOfActivePeers + 1) / 2;
            
            //check what type of vote we are looking for
            ChannelVote.VoteType vtyYesVote = ChannelVote.VoteType.None;
            ChannelVote.VoteType vtyNoVote = ChannelVote.VoteType.None;

            if (m_staState == State.VoteJoin)
            {
                vtyYesVote = ChannelVote.VoteType.Add;
                vtyNoVote = ChannelVote.VoteType.AntiAdd;
            }
            else if (m_staState == State.VoteKick)
            {
                vtyYesVote = ChannelVote.VoteType.Kick;
                vtyNoVote = ChannelVote.VoteType.AntiKick;
            }

            int iNumberOfYesVotes = 0;
            int iNumberOfNoVotes = 0;
            
            //get votes 
            foreach (var vtyVote in m_vtyVotesOnChannelByPeers)
            {
                if (vtyVote.Value == vtyYesVote)
                {
                    iNumberOfYesVotes++;
                }
                else if (vtyVote.Value == vtyNoVote)
                {
                    iNumberOfNoVotes++;
                }
                
            }
            
            //check ratio of vote to not vote 
            if (iNumberOfYesVotes >= iMinVotesForSuccess)
            {
                return true;
            }

            return false;
        }
        
        //check if vote is for or against 
        //this does not factor in time 
        public bool IsMajorityForVote()
        {
              
            //check what type of vote we are looking for
            ChannelVote.VoteType vtyYesVote = ChannelVote.VoteType.None;
            ChannelVote.VoteType vtyNoVote = ChannelVote.VoteType.None;

            if (m_staState == State.VoteJoin)
            {
                vtyYesVote = ChannelVote.VoteType.Add;
                vtyNoVote = ChannelVote.VoteType.AntiAdd;
            }
            else if (m_staState == State.VoteKick)
            {
                vtyYesVote = ChannelVote.VoteType.Kick;
                vtyNoVote = ChannelVote.VoteType.AntiKick;
            }

            
            int iNumberOfYesVotes = 0;
            int iNumberOfNoVotes = 0;
            
            //get votes 
            foreach (var vtyVote in m_vtyVotesOnChannelByPeers)
            {
                if (vtyVote.Value == vtyYesVote)
                {
                    iNumberOfYesVotes++;
                }
                else if (vtyVote.Value == vtyNoVote)
                {
                    iNumberOfNoVotes++;
                }
                
            }

            if (iNumberOfYesVotes > iNumberOfNoVotes)
            {
                return true;
            }
            
            return false;
        }
        

        //clear channel
        public void ClearChannel()
        {
            ClearVotesByChannel();

            //reset peer
            m_lChannelPeer = long.MinValue;
            m_dtmJoinVoteTime = DateTime.MinValue;
            m_dtmKickVoteTime = DateTime.MinValue;
            m_dtmAssignedTime = DateTime.MinValue;
            m_staState = State.Empty;
            m_vtyVotesOnChannelByPeers.Clear();

            //reset hash head
            m_lHashOfLastNodeProcessed = 0;
            m_iLastMessageIndexProcessed = 0;
        }

        //clear all votes by channel
        public void ClearVotesByChannel()
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
            ClearVotesByChannel();
            
            //clear new style votes
            m_vtyVotesOnChannelByPeers.Clear();
            
            m_lChannelPeer = lPeerID;
            m_dtmJoinVoteTime = dtmVoteStartTime;
            m_staState = State.VoteJoin;
            
        }

        public void StartVoteKickForPeer(DateTime dtmVoteStartTime)
        {
            //clear all new style votes on this chanel
            m_vtyVotesOnChannelByPeers.Clear();
            
            m_dtmKickVoteTime = dtmVoteStartTime;
            m_staState = State.VoteKick;
        }

        //make the peer with id lPeerID in control of this channel
        public void AssignPeerToChannel(long lPeerID, DateTime dtmTimeOfJoin)
        {
            ClearVotesByChannel();

            m_lChannelPeer = lPeerID;
            m_staState = State.Assigned;
            m_dtmJoinVoteTime = dtmTimeOfJoin;
            m_dtmAssignedTime = m_dtmJoinVoteTime;
            m_vtyVotesOnChannelByPeers.Clear();
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
            GlobalMessageChannelState mcsOutState = new GlobalMessageChannelState(m_chvVotes.Count);

            //the peer that is currently assigned to this input channel
            mcsOutState.m_lChannelPeer = m_lChannelPeer;

            //the time when voting on assigning peer to this channel started
            mcsOutState.m_dtmJoinVoteTime = m_dtmJoinVoteTime;
            
            //get the time when kicking the peer was started
            mcsOutState.m_dtmKickVoteTime = m_dtmKickVoteTime;
            
            //clone the time a client was assigned to this channel 
            mcsOutState.m_dtmAssignedTime = m_dtmAssignedTime;

            //the current state of this channel
            mcsOutState.m_staState = m_staState;
            
            //copy the votes dictionary
            mcsOutState.m_vtyVotesOnChannelByPeers = new Dictionary<long, ChannelVote.VoteType>(m_vtyVotesOnChannelByPeers);

            //copy the votes list
            mcsOutState.m_chvVotes = new List<ChannelVote>(m_chvVotes);

            //the hash of the last valid node processed for this channel
            mcsOutState.m_lHashOfLastNodeProcessed = m_lHashOfLastNodeProcessed;

            //the index of the last valid message processed 
            mcsOutState.m_iLastMessageIndexProcessed = m_iLastMessageIndexProcessed;

            //the chain link head this peer is using in the last valid message 
            mcsOutState.m_lChainLinkHeadHash = m_lChainLinkHeadHash;

            //the message sort value of the last valid message processed by this channel
            mcsOutState.m_msvLastSortValue = m_msvLastSortValue;

            return mcsOutState;
        }
    }

    public partial class NetworkingByteStream
    {
        //read and write one vote
        public static void Serialize(ReadByteStream rbsByteStream, ref GlobalMessageChannelState.ChannelVote Output)
        {
            byte bVoteType = 0;

            ByteStream.Serialize(rbsByteStream, ref bVoteType);

            Output.m_vtpVoteType = (GlobalMessageChannelState.ChannelVote.VoteType)bVoteType;

            ByteStream.Serialize(rbsByteStream, ref Output.m_dtmVoteTime);

            ByteStream.Serialize(rbsByteStream, ref Output.m_lPeerID);
        }

        public static void Serialize(WriteByteStream wbsByteStream, ref GlobalMessageChannelState.ChannelVote Input)
        {
            byte bVoteType = (byte)Input.m_vtpVoteType;

            ByteStream.Serialize(wbsByteStream, ref bVoteType);

            ByteStream.Serialize(wbsByteStream, ref Input.m_dtmVoteTime);

            ByteStream.Serialize(wbsByteStream, ref Input.m_lPeerID);
        }

        public static int DataSize(GlobalMessageChannelState.ChannelVote Input)
        {
            int iSize = 0;
            iSize += ByteStream.DataSize(Input.m_dtmVoteTime);
            iSize += ByteStream.DataSize(Input.m_lPeerID);
            iSize += ByteStream.DataSize((byte)Input.m_vtpVoteType);
            return iSize;
        }

        //serialize guns
        public static void Serialize(ReadByteStream rbsByteStream, ref GlobalMessageChannelState Output)
        {
            //player count
            int iPlayerCount = 0;

            ByteStream.Serialize(rbsByteStream, ref iPlayerCount);

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

            //TODO::JackR check if this is deterministic
            int iVoteCount = 0;
            
            ByteStream.Serialize(rbsByteStream, ref iVoteCount);

            Output.m_vtyVotesOnChannelByPeers = new Dictionary<long, GlobalMessageChannelState.ChannelVote.VoteType>(iVoteCount);
            
            //add in all the votes and the peers that did the vots
            for (int i = 0; i < iVoteCount; i++)
            {
                Byte bVoteType = 0;
                ByteStream.Serialize(rbsByteStream, ref bVoteType);
                
                long lPeerID = 0;
                ByteStream.Serialize(rbsByteStream, ref lPeerID);
                
                Output.m_vtyVotesOnChannelByPeers[lPeerID] = (GlobalMessageChannelState.ChannelVote.VoteType)bVoteType;
            }
            

            //assigned peer
            ByteStream.Serialize(rbsByteStream, ref Output.m_lChannelPeer);

            //time of last join vote start
            ByteStream.Serialize(rbsByteStream, ref Output.m_dtmJoinVoteTime);
            
            //time of last kick vote started
            ByteStream.Serialize(rbsByteStream, ref Output.m_dtmKickVoteTime);
            
            //time last peer was assigned 
            ByteStream.Serialize(rbsByteStream, ref Output.m_dtmAssignedTime);

            //state
            byte bState = 0;
            ByteStream.Serialize(rbsByteStream, ref bState);
            Output.m_staState = (GlobalMessageChannelState.State)bState;

            //hash of last node processed
            ByteStream.Serialize(rbsByteStream, ref Output.m_lHashOfLastNodeProcessed);

            //last message index processed 
            ByteStream.Serialize(rbsByteStream, ref Output.m_iLastMessageIndexProcessed);

            //best chain link hash 
            ByteStream.Serialize(rbsByteStream, ref Output.m_lChainLinkHeadHash);
            
            //the sorting value of the last valid message processed 
            Serialize(rbsByteStream, ref Output.m_msvLastSortValue);
        }

        public static void Serialize(WriteByteStream wbsByteStream, ref GlobalMessageChannelState Input)
        {
            //player count
            int iPlayerCount = Input.m_chvVotes.Count;

            ByteStream.Serialize(wbsByteStream, ref iPlayerCount);

            //votes
            for (int i = 0; i < iPlayerCount; i++)
            {
                GlobalMessageChannelState.ChannelVote chvVote = Input.m_chvVotes[i];

                Serialize(wbsByteStream, ref chvVote);
            }
            
            
            //todo:: move this to a dictionary serialization func
            int iVoteCount = Input.m_vtyVotesOnChannelByPeers.Count;
            
            ByteStream.Serialize(wbsByteStream, ref iVoteCount );

            //add in all the votes and the peers that did the vots
            foreach ( var kvp in Input.m_vtyVotesOnChannelByPeers)
            {
                Byte bVoteType = (Byte)kvp.Value;
                ByteStream.Serialize(wbsByteStream, ref bVoteType);
                
                long lPeerID = kvp.Key;
                ByteStream.Serialize(wbsByteStream, ref lPeerID);
            }

            //assigned peer
            ByteStream.Serialize(wbsByteStream, ref Input.m_lChannelPeer);

            //time of last join vote
            ByteStream.Serialize(wbsByteStream, ref Input.m_dtmJoinVoteTime);
            
            //time of last kick vote
            ByteStream.Serialize(wbsByteStream, ref Input.m_dtmKickVoteTime);
            
            //time last peer was assigned 
            ByteStream.Serialize(wbsByteStream, ref Input.m_dtmAssignedTime);

            //state
            byte bState = (byte)Input.m_staState;
            ByteStream.Serialize(wbsByteStream, ref bState);

            //hash of last node processed 
            ByteStream.Serialize(wbsByteStream, ref Input.m_lHashOfLastNodeProcessed);

            //last message index processed 
            ByteStream.Serialize(wbsByteStream, ref Input.m_iLastMessageIndexProcessed);

            //best chain link hash
            ByteStream.Serialize(wbsByteStream, ref Input.m_lChainLinkHeadHash);

            //the sorting value of the last valid message processed 
            Serialize(wbsByteStream, ref Input.m_msvLastSortValue);
        }

        public static int DataSize(GlobalMessageChannelState Input)
        {
            int iSize = 0;
            iSize += ByteStream.DataSize(Input.m_chvVotes.Count);

            for(int i = 0; i < Input.m_chvVotes.Count; i++)
            {
                iSize += DataSize(Input.m_chvVotes[i]);
            }
            
            //add the size of dictionary of votes
            iSize += ByteStream.DataSize(Input.m_vtyVotesOnChannelByPeers.Count);
            
            iSize += sizeof(long) * Input.m_vtyVotesOnChannelByPeers.Count;
            iSize += sizeof(byte) * Input.m_vtyVotesOnChannelByPeers.Count;

            iSize += ByteStream.DataSize(Input.m_lChainLinkHeadHash);
            iSize += ByteStream.DataSize(Input.m_dtmJoinVoteTime);
            iSize += ByteStream.DataSize(Input.m_dtmKickVoteTime);
            iSize += ByteStream.DataSize(Input.m_dtmAssignedTime);
            iSize += ByteStream.DataSize(Input.m_iLastMessageIndexProcessed);
            iSize += ByteStream.DataSize(Input.m_lChannelPeer);
            iSize += ByteStream.DataSize(Input.m_lHashOfLastNodeProcessed);
            iSize += NetworkingByteStream.DataSize(Input.m_msvLastSortValue);
            iSize += ByteStream.DataSize((byte)Input.m_staState);

            return iSize;
        }
    }
}
