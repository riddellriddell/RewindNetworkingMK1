using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Networking
{
    /// <summary>
    /// this class keeps track of which peers are assigned to which global message channels 
    /// </summary>
    public class GlobalMessagingState : ICloneable
    {
        public static TimeSpan s_tspVoteTimeout = TimeSpan.FromSeconds(2f);

        //array for each of the available player slots holding the channel state
        public List<GlobalMessageChannelState> m_gmcMessageChannels;

        public GlobalMessagingState()
        {

        }

        public GlobalMessagingState(int iNumberOfItems)
        {
            Init(iNumberOfItems);
        }

        //perform deep clone of this object
        public object Clone()
        {
            GlobalMessagingState gmsCloneState = new GlobalMessagingState();

            //clone peer channel states
            gmsCloneState.m_gmcMessageChannels = new List<GlobalMessageChannelState> (this.m_gmcMessageChannels);

            for(int i = 0; i < m_gmcMessageChannels.Count; i++)
            {
                gmsCloneState.m_gmcMessageChannels[i] = (GlobalMessageChannelState) this.m_gmcMessageChannels[i].Clone();
            }

            return gmsCloneState;
        }

        protected void Init(int iNumberOfItems)
        {
            m_gmcMessageChannels = new List<GlobalMessageChannelState>(iNumberOfItems);

            for(int i = 0; i < iNumberOfItems; i++)
            {
                GlobalMessageChannelState gmcChannelState = new GlobalMessageChannelState(iNumberOfItems); 

                //queue up chain start node 
                m_gmcMessageChannels.Add(gmcChannelState);
            }
        }

        //process a message factoring in that lLocalPeer is the user in controll of this computer
        //and must be in the "Kept" group when split occures 
        public void ProcessMessage(long lLocalPeer, IPeerChannelVoteMessageNode smnMessageNode)
        {
            //check if peer is in game 
            if (lLocalPeer != long.MinValue && TryGetIndexForPeer(lLocalPeer,out int iIndex))
            {
                //process any split commands

                //process any join commands
            }
            else
            {
                //peer cant vote because they are not part of the global message system
            }
        }
               
        //get the channel index for a peer with id lPeerID
        protected bool TryGetIndexForPeer(long lPeerID,out int iIndex)
        {
            for(int i = 0; i < m_gmcMessageChannels.Count; i++)
            {
                if(m_gmcMessageChannels[i].m_lChannelPeer == lPeerID)
                {
                    iIndex = i;
                    return true;
                }
            }

            iIndex = 0;
            return false;
        }

        //try and find a channel that is not in use
        protected bool TryGetEmptyChannel(out int iIndex)
        {
            for(int i = 0; i < m_gmcMessageChannels.Count; i++)
            {
                if(m_gmcMessageChannels[i].m_staState == GlobalMessageChannelState.State.Empty)
                {
                    iIndex = i;
                    return true;
                }
            }

            iIndex = 0;
            return false;
        }

        //apply the vote command to the peer
        protected void ApplyVotesToChannel(int iMessageChannel, IPeerChannelVoteMessageNode smnMessageNode)
        {
            for(int i = 0; i < smnMessageNode.ActionPerPeer.Length; i++)
            {
                // a value of 0 is kick 1 is join
                byte bIsJoin = smnMessageNode.ActionPerPeer[i].Item1;
                long lPeerID = smnMessageNode.ActionPerPeer[i].Item2;

                //process join commands
                if (bIsJoin > 0)
                {
                    int iIndex = int.MinValue;

                    //clear any previous add votes that have failed  
                    RemoveFailedVotes(smnMessageNode.MessageCreationTime);

                    //check if vote is already in progress for channel
                    if (TryGetIndexForPeer(lPeerID, out iIndex))
                    {
                        //check if peer is not alreadty added
                        if (m_gmcMessageChannels[iIndex].m_staState == GlobalMessageChannelState.State.Voting)
                        {
                            //add joim vote to channel
                            m_gmcMessageChannels[iMessageChannel].AddConnectionVote(iIndex, smnMessageNode.MessageCreationTime, lPeerID);
                        }
                    }
                    else if (TryGetEmptyChannel(out iIndex))//try get empty channel
                    {
                        //set the channel to starting voting process
                        m_gmcMessageChannels[iIndex].StartVoteForPeer(lPeerID, smnMessageNode.MessageCreationTime);

                        //add join vote to channel
                        m_gmcMessageChannels[iMessageChannel].AddConnectionVote(iIndex, smnMessageNode.MessageCreationTime, lPeerID);
                    }
                }
                else //process kick commands
                {
                    //get peer ID for kick target
                    if (TryGetIndexForPeer(lPeerID, out int iKickTarget))
                    {
                        //add kick vote to channel
                        m_gmcMessageChannels[iMessageChannel].AddKickVote(iKickTarget, smnMessageNode.MessageCreationTime, lPeerID);
                    }
                }
            }
        }

        //process split vote
        protected void ProcessSplitVotes(int iPeerNodeIndex, int iChangedMessageChannel,DateTime dtmTimeOfVote)
        {
            //get list of kick and non kick
            List<int> iKeepList = new List<int>();
            List<int> iKickList = new List<int>();

            GlobalMessageChannelState gmcChangedChannel = m_gmcMessageChannels[iChangedMessageChannel];

            //for each vote
            for (int i = 0; i < gmcChangedChannel.m_chvVotes.Count;i++)
            {
                //check if vote is for kicking peer and is still active
                if(gmcChangedChannel.m_chvVotes[i].m_vtpVoteType == GlobalMessageChannelState.ChannelVote.VoteType.Kick &&
                    gmcChangedChannel.m_chvVotes[i].IsActive(dtmTimeOfVote,s_tspVoteTimeout))
                {
                    iKickList.Add(i);
                }
                else if(m_gmcMessageChannels[i].m_staState == GlobalMessageChannelState.State.Assigned) //get non kick peer list
                {
                    iKeepList.Add(i);
                }                
            }

            //check if anyone is getting kicked
            if(iKickList.Count == 0)
            {
                return;
            }

            //while there are users in the inGroup
            for(int i = 0; i < iKeepList.Count; i++)
            {
                //skip when processing votes by local player
                if(iKeepList[i] == iChangedMessageChannel)
                {
                    continue;
                }

                //get next peer
                int iChannelToProcess = iKeepList[i];

                GlobalMessageChannelState gmcInGroupChannel = m_gmcMessageChannels[iChannelToProcess];

                //cycle through all the channels to kick
                for (int j = iKickList.Count -1; j > -1; j--)
                {
                    //get kick target
                    int iKickTargetChannel = iKickList[j];

                    //get in group peer vote for target
                    GlobalMessageChannelState.ChannelVote cvtVote = gmcInGroupChannel.m_chvVotes[iKickTargetChannel];

                    //check if in group peer is not voting to kick out group peer
                    if (cvtVote.IsActive(dtmTimeOfVote,s_tspVoteTimeout) == false || 
                        cvtVote.m_vtpVoteType != GlobalMessageChannelState.ChannelVote.VoteType.Kick ||
                        m_gmcMessageChannels[iKickTargetChannel].m_lChannelPeer != cvtVote.m_lPeerID)
                    {
                        //remove kick target from kick group and add them to the in group
                        iKickList.RemoveAt(j);
                        iKeepList.Add(iKickTargetChannel);
                    }
                }

                //check if there is anyone left in the kick group
                if (iKickList.Count == 0)
                {
                    //stop processing kick vote (vote has failed at this point)
                    return;
                }
            }

            //check if there is a split happening 
            bool bIsInKickGroup = false;

            for (int i = 0; i < iKickList.Count; i++)
            {
                if(iKickList[i] == iPeerNodeIndex)
                {
                    bIsInKickGroup = true;

                    break;
                }
            }
            
            //invert kick group if peer is in wrong group
            if(bIsInKickGroup)
            {
                List<int> iTemp = iKeepList;
                iKeepList = iKickList;
                iKickList = iTemp;
            }

            //remove peer channels for kicked group
            KickPeers(iKickList);
        }

        //perfotm split
        protected void KickPeers(List<int> iKickList)
        {
            //for each item in the kick list
            for(int i = 0; i < iKickList.Count; i++)
            {
                int ikickTarget = iKickList[i];

                //clear kicked channel
                m_gmcMessageChannels[ikickTarget].ClearChannel();

                for(int j = 0; j < m_gmcMessageChannels.Count; j++)
                {
                    //clear any votes for kicked player
                    m_gmcMessageChannels[j].ClearVotesForChannelIndex(ikickTarget);
                }
            }
        }
        
        //process join vote
        protected void ProcessJoin(int iMessagingChannel, DateTime dtmTimeOfVote)
        {
            //first value is the channel seccond is number of voted
            List<Tuple<int, int>> tupJoinRequest = new List<Tuple<int, int>>();

            GlobalMessageChannelState gcsUpdatedChannel = m_gmcMessageChannels[iMessagingChannel];

            for(int i = 0; i < gcsUpdatedChannel.m_chvVotes.Count; i++)
            {
                GlobalMessageChannelState.ChannelVote cvtVote = gcsUpdatedChannel.m_chvVotes[i];

                //check if vote is for join and is still valid and peer 
                if (cvtVote.m_vtpVoteType == GlobalMessageChannelState.ChannelVote.VoteType.Add &&
                    cvtVote.IsActive(dtmTimeOfVote,s_tspVoteTimeout) && 
                    m_gmcMessageChannels[i].m_staState == GlobalMessageChannelState.State.Voting && 
                    m_gmcMessageChannels[i].m_lChannelPeer == cvtVote.m_lPeerID)
                {
                    //add peer to the list
                    tupJoinRequest.Add(new Tuple<int, int>(i, 0));
                }
            }

            int iActiveChannels = 0;

            //loop through all channels and add up votes
            for(int i = 0; i < m_gmcMessageChannels.Count; i++)
            {
                GlobalMessageChannelState gcsChannel = m_gmcMessageChannels[iMessagingChannel];

                //check if channel is active
                if(gcsChannel.m_staState != GlobalMessageChannelState.State.Assigned)
                {
                    //skip channel as it has not been assigned to a peer
                    continue;
                }

                iActiveChannels++;

                //loop through all join requests 
                for (int j = 0; j < tupJoinRequest.Count; j++)
                {
                    Tuple<int,int> tupRequest = tupJoinRequest[j];

                    GlobalMessageChannelState.ChannelVote cvtVote = gcsChannel.m_chvVotes[tupRequest.Item1];

                    //check if channel voted to add peer
                    if (cvtVote.m_vtpVoteType == GlobalMessageChannelState.ChannelVote.VoteType.Add &&
                   cvtVote.IsActive(dtmTimeOfVote, s_tspVoteTimeout) &&
                   m_gmcMessageChannels[tupRequest.Item1].m_lChannelPeer == cvtVote.m_lPeerID)
                    {
                        //increment votes for add                        
                        tupJoinRequest[j] = new Tuple<int, int>(tupRequest.Item1, tupRequest.Item2 + 1);
                    }
                }
            }

            int iMinVotesNeeded = (iActiveChannels + 1) / 2;

            //loop through join requests and check if any have enough votes
            for(int i = 0; i < tupJoinRequest.Count; i++)
            {
                if(tupJoinRequest[i].Item2 >= iMinVotesNeeded)
                {
                    AddPeerToGlobalMessenger(tupJoinRequest[i].Item1);
                }
            }
        }

        //perform join
        public void AddPeerToGlobalMessenger(int iChannelIndex)
        {
            //assign peer to channel
            m_gmcMessageChannels[iChannelIndex].AssignPeerToChannel(m_gmcMessageChannels[iChannelIndex].m_lChannelPeer);

            //clear any votes on channel
            for(int i = 0; i < m_gmcMessageChannels.Count; i++)
            {
                m_gmcMessageChannels[i].ClearVotesForChannelIndex(iChannelIndex);
            }
        }

        //check for failed votes and reset 
        public void RemoveFailedVotes(DateTime dtmTime)
        {
            //get list of all votes in action
            List<int> iVotesInAction = new List<int>();
            List<int> iActivePeers = new List<int>();
            for(int i = 0; i < m_gmcMessageChannels.Count; i++)
            {
                if(m_gmcMessageChannels[i].m_staState == GlobalMessageChannelState.State.Voting)
                {
                    iVotesInAction.Add(i);
                }
                else if(m_gmcMessageChannels[i].m_staState == GlobalMessageChannelState.State.Assigned)
                {
                    iActivePeers.Add(i);
                }
            }

            //check if anyone is still voting on peer
            for(int i = 0; i < iVotesInAction.Count; i++)
            {
                bool bIsActive = false;

                for(int j = 0; j < iActivePeers.Count;j++)
                {
                    //get vote for peer
                    GlobalMessageChannelState.ChannelVote cvtVote = m_gmcMessageChannels[iActivePeers[j]].m_chvVotes[iVotesInAction[i]];

                    //check if vote is to add peer and is still active
                    if(cvtVote.m_vtpVoteType == GlobalMessageChannelState.ChannelVote.VoteType.Add &&
                        cvtVote.IsActive(dtmTime,s_tspVoteTimeout) &&
                        m_gmcMessageChannels[iVotesInAction[i]].m_lChannelPeer == cvtVote.m_lPeerID)
                    {
                        bIsActive = true;
                        break;
                    }
                }

                //check if anyone still actively voting to add peer
                if(bIsActive == false)
                {
                    //removre peer
                    m_gmcMessageChannels[iVotesInAction[i]].ClearChannel();

                    //clear any votes for peer
                    for (int j = 0; j < m_gmcMessageChannels.Count; j++)
                    {
                        m_gmcMessageChannels[j].ClearVotesForChannelIndex(iVotesInAction[i]);
                    }
                }
            }
        }
    }
}
