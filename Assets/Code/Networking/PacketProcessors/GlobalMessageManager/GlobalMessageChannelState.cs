using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Networking
{
    //this represents one channel of input (usually for one player in the sim)
    public class GlobalMessageChannelState : ICloneable
    {
        //the state of the channel
        public enum State : byte
        {
            Empty, // not in use
            Voting, // peers are voting on if channel should be assigned to peer
            Assigned //channel assigned to peer
        }

        //any votes that are associated with this channel
        public struct ChannelVote
        {
            public enum VoteType: byte
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

            public bool IsActive(DateTime dtmCurrentTime,TimeSpan tspTimeOutTime)
            {
                if(dtmCurrentTime < m_dtmVoteTime)
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
        public DateTime m_dtmVoteStartTime;

        //the current state of this channel
        public State m_staState;

        //list of all the active votes by this channel on other channels
        public List<ChannelVote> m_chvVotes;

        public GlobalMessageChannelState(int iNumberOfPeers)
        {
            Init(iNumberOfPeers);
        }

        //setup
        public void Init(int iMaxPlayerCount)
        {
            //mark channel as empty
            m_lChannelPeer = long.MinValue;

            m_staState = State.Empty;

            m_dtmVoteStartTime = DateTime.MinValue;

            m_chvVotes = new List<ChannelVote>(iMaxPlayerCount);

            for(int i = 0; i < m_chvVotes.Count; i++)
            {
                m_chvVotes.Add(new ChannelVote()
                {
                    m_lPeerID = 0,
                    m_dtmVoteTime = DateTime.MinValue,
                    m_vtpVoteType = ChannelVote.VoteType.None
                });
            }
        }

        //process channel change message
        public void AddKickVote(int iPeerChannelIndex, DateTime dtmCreationTime,long lTargetPeerID)
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
        public void AddConnectionVote(int iPeerChannelIndex,DateTime dtmCreationTime,long lTargetPeerID)
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
            m_dtmVoteStartTime = DateTime.MinValue;
            m_staState = State.Empty;
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
        public void StartVoteForPeer(long lPeerID, DateTime dtmVoteStartTime)
        {
            ClearVotes();
            m_lChannelPeer = lPeerID;
            m_dtmVoteStartTime = dtmVoteStartTime;
            m_staState = State.Voting;
        }

        //make the peer with id lPeerID in control of this channel
        public void AssignPeerToChannel(long lPeerID)
        {
            ClearVotes();

            m_lChannelPeer = lPeerID;
            m_staState = State.Assigned;
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
            if(TryGetVoteForPeer(lTargetPeerID, out int iIndex, out ChannelVote chvVote))
            { 
                ClearVotesForChannelIndex(iIndex);
            }
        }


        public object Clone()
        {
            throw new NotImplementedException();
        }
    }
}
