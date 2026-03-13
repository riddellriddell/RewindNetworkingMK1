using SharedTypes;
using System;
using System.Collections.Generic;
using UnityEngine;
using Utility;

namespace Networking
{
    /// <summary>
    /// this class keeps track of which peers are assigned to which global message channels 
    /// </summary>
    public class GlobalMessagingState : ICloneable
    {
        //array for each of the available player slots holding the channel state
        public List<GlobalMessageChannelState> m_gmcMessageChannels;

        //the sorting value of the last processed message
        public SortingValue m_svaLastMessageSortValue;

        public GlobalMessagingState()
        {

        }

        public GlobalMessagingState(int iNumberOfItems)
        {
            Init(iNumberOfItems);
        }

        public GlobalMessagingState(int iNumberOfItems, long lFirstPeer, DateTime dtmStartTime)
        {
            Init(iNumberOfItems);

            AssignFirstPeer(lFirstPeer, dtmStartTime);
        }

        public void AssignFirstPeer(long lFirstPeer, DateTime dtmTimeOfAdd)
        {
            m_gmcMessageChannels[0].AssignPeerToChannel(lFirstPeer, dtmTimeOfAdd);
        }

        //perform deep clone of this object
        public object Clone()
        {
            GlobalMessagingState gmsCloneState = new GlobalMessagingState();

            //clone peer channel states
            gmsCloneState.m_gmcMessageChannels = new List<GlobalMessageChannelState>(this.m_gmcMessageChannels);

            for (int i = 0; i < m_gmcMessageChannels.Count; i++)
            {
                gmsCloneState.m_gmcMessageChannels[i] = (GlobalMessageChannelState)this.m_gmcMessageChannels[i].Clone();
            }

            return gmsCloneState;
        }

        //process a message and its effect on who is in the global messaging system
        public void ProcessMessage(PeerMessageNode pmnMessageNode, TimeSpan tspVoteTimeout, int iMaxPlayerCount, NetworkingDataBridge ndbNetworkingDataBridge = null)
        {
            //update the most recent sorting value 
            m_svaLastMessageSortValue = SortingValue.Max(m_svaLastMessageSortValue, pmnMessageNode.m_svaMessageSortingValue);

            //check if peer is in game 
            if (pmnMessageNode.m_lPeerID != long.MinValue && TryGetIndexForPeer(pmnMessageNode.m_lPeerID, out int iIndexOfMessageChannel))
            {
                //validate message (check if it is the next message for this peer and is based on the correct previous message) 
                bool bIsValidMessage = ValidateAndApplyMessageChangeToChannel(iIndexOfMessageChannel, pmnMessageNode, ndbNetworkingDataBridge == null ? ndbNetworkingDataBridge.m_lLocalPeerID : 0);

                //filter invalid messages
                if (bIsValidMessage == true)
                {
                    //check if message is a vote
                    if (pmnMessageNode.m_bMessageType == VoteMessage.TypeID)
                    {
                        //apply votes
                        ApplyVotesToChannel( iIndexOfMessageChannel, pmnMessageNode, tspVoteTimeout, iMaxPlayerCount, ndbNetworkingDataBridge);
                    }
                    else if (ndbNetworkingDataBridge != null && pmnMessageNode.m_gmbMessage is ISimMessagePayload)
                    {
                        //store sim message 
                        ndbNetworkingDataBridge.QueueSimMessage(pmnMessageNode.m_svaMessageSortingValue, pmnMessageNode.m_lPeerID, iIndexOfMessageChannel, pmnMessageNode.m_gmbMessage as ISimMessagePayload);
                    }
                    
                    //apply the effects of any votes that may have been cast or might have expired
                    ApplyAnyConnectionVotes(pmnMessageNode.m_dtmMessageCreationTime,  pmnMessageNode.m_svaMessageSortingValue, tspVoteTimeout, iMaxPlayerCount,ndbNetworkingDataBridge);
                }
                else
                {
                    Debug.LogError("Message from peer" + pmnMessageNode.m_lPeerID + " failed validation");
                }
            }
            else
            {
                //peer cant vote or create inputs because they are not part of the global message system
                Debug.LogError("Peer" + pmnMessageNode.m_lPeerID + " is not part of the global messaging system");
            }

        }

        //returns a list of all the peer id's that are in an active state 
        public List<Tuple<int,long>> GetActivePeerIndexAndID()
        {
            List<Tuple<int, long>> lOutput = new List<Tuple<int, long>>();

            for (int i = 0; i < m_gmcMessageChannels.Count; i++)
            {
                if(m_gmcMessageChannels[i].m_staState == GlobalMessageChannelState.State.Assigned ||
                   m_gmcMessageChannels[i].m_staState == GlobalMessageChannelState.State.VoteKick )
                {
                    lOutput.Add(new Tuple<int, long>(i, m_gmcMessageChannels[i].m_lChannelPeer));
                }
            }

            return lOutput;
        }

        //returns a list of the active peer ID's
        public List<long> GetActivePeerIDs()
        {
            List<long> lOutput = new List<long>();

            for (int i = 0; i < m_gmcMessageChannels.Count; i++)
            {
                if (m_gmcMessageChannels[i].m_staState == GlobalMessageChannelState.State.Assigned ||
                   m_gmcMessageChannels[i].m_staState == GlobalMessageChannelState.State.VoteKick)
                {
                    lOutput.Add(m_gmcMessageChannels[i].m_lChannelPeer);
                }
            }

            return lOutput;
        }

        //get the channel index for a peer with id lPeerID
        public bool TryGetIndexForPeer(long lPeerID, out int iIndex)
        {
            for (int i = 0; i < m_gmcMessageChannels.Count; i++)
            {
                if (m_gmcMessageChannels[i].m_lChannelPeer == lPeerID)
                {
                    iIndex = i;
                    return true;
                }
            }

            iIndex = 0;
            return false;
        }

        public void ResetToState(GlobalMessagingState gmsState)
        {
            if(m_gmcMessageChannels == null || m_gmcMessageChannels.Count != gmsState.m_gmcMessageChannels.Count)
            {
                Init(gmsState.m_gmcMessageChannels.Count);
            }

            for (int i = 0; i < m_gmcMessageChannels.Count; i++)
            {
                m_gmcMessageChannels[i].ResetToState(gmsState.m_gmcMessageChannels[i]);
            }
            m_svaLastMessageSortValue = gmsState.m_svaLastMessageSortValue;
        }

        public int AssignedChannelCount()
        {
            int iActiveChannels = 0;

            for (int i = 0; i < m_gmcMessageChannels.Count; i++)
            {
                if (m_gmcMessageChannels[i].m_staState == GlobalMessageChannelState.State.Assigned)
                {
                    iActiveChannels++;
                }
            }

            return iActiveChannels;
        }

        //TODO: track chanel activation and store in an int
        //instead of recalculating every time
        public int ActiveChannelCount()
        {
            int iActiveChannels = 0;

            for(int i = 0; i < m_gmcMessageChannels.Count;i++)
            {
                if(m_gmcMessageChannels[i].m_staState == GlobalMessageChannelState.State.Assigned &&
                   m_gmcMessageChannels[i].m_iLastMessageIndexProcessed != 0)
                {
                    iActiveChannels++;
                }
            }

            return iActiveChannels;
        }

        public void RemoveFailedVotes(DateTime dtmTime, TimeSpan tspVoteTimeout)
        {
            for(int i = 0; i < m_gmcMessageChannels.Count; i++)
            {
                //skip channels not currently voting on something
                if(m_gmcMessageChannels[i].m_staState != GlobalMessageChannelState.State.VoteJoin &&
                   m_gmcMessageChannels[i].m_staState != GlobalMessageChannelState.State.VoteKick)
                {
                    continue;
                }

                //compare vote start time to current time
                TimeSpan tspTimeSinceVoteStart = TimeSpan.Zero;

                if (m_gmcMessageChannels[i].m_staState == GlobalMessageChannelState.State.VoteJoin)
                {
                    tspTimeSinceVoteStart = dtmTime - m_gmcMessageChannels[i].m_dtmJoinVoteTime;
                }
                else if (m_gmcMessageChannels[i].m_staState == GlobalMessageChannelState.State.VoteKick)
                {
                    tspTimeSinceVoteStart = dtmTime - m_gmcMessageChannels[i].m_dtmKickVoteTime;
                }

                //check if vote has timed out
                if(tspTimeSinceVoteStart > tspVoteTimeout)
                {
                    //clear any votes for peer
                    for (int j = 0; j < m_gmcMessageChannels.Count; j++)
                    {
                        m_gmcMessageChannels[j].ClearVotesForChannelIndex(i);
                    }

                    if(m_gmcMessageChannels[i].m_staState == GlobalMessageChannelState.State.VoteJoin)
                    {
                        m_gmcMessageChannels[i].ClearChannel();
                    }
                    else if(m_gmcMessageChannels[i].m_staState == GlobalMessageChannelState.State.VoteKick)
                    {
                        m_gmcMessageChannels[i].m_staState = GlobalMessageChannelState.State.Assigned;
                    }
                }
            }
        }

        //try ang get time based on the sorting value
        public DateTime TimeOfLastMessage()
        {
            //get the sorting value
            return new DateTime((long)m_svaLastMessageSortValue.m_lSortValueA);
        }
        
        //setup channel for a global messenging system with a maximum number of peers
        protected void Init(int iMaxChannelCount)
        {
            m_gmcMessageChannels = new List<GlobalMessageChannelState>(iMaxChannelCount);

            for (int i = 0; i < iMaxChannelCount; i++)
            {
                GlobalMessageChannelState gmcChannelState = new GlobalMessageChannelState(iMaxChannelCount);

                //queue up chain start node 
                m_gmcMessageChannels.Add(gmcChannelState);
            }

            m_svaLastMessageSortValue = SortingValue.MinValue;
        }

        //try and find a channel that is not in use
        protected bool TryGetEmptyChannel(out int iIndex)
        {
            for (int i = 0; i < m_gmcMessageChannels.Count; i++)
            {
                if (m_gmcMessageChannels[i].m_staState == GlobalMessageChannelState.State.Empty)
                {
                    iIndex = i;
                    return true;
                }
            }

            iIndex = 0;
            return false;
        }

        //validate a message and apply any relevent changes to channel state
        protected bool ValidateAndApplyMessageChangeToChannel(int iMessageChannel, PeerMessageNode pmnMessageNode, long lLocalPeer)
        {
            //check if message is next in queue for peer
            UInt32 iMessageChannelIndex = pmnMessageNode.m_iPeerMessageIndex;
            UInt32 iCurrentChannelIndex = m_gmcMessageChannels[iMessageChannel].m_iLastMessageIndexProcessed;

            //check if message is next in peer message chain
            if (iCurrentChannelIndex + 1 != iMessageChannelIndex)
            {
                Debug.LogError($"peer: {lLocalPeer} tried to process message that was not correctly ordered for channel:{iMessageChannel}," +
                               $" current index:{iCurrentChannelIndex}, message index:{iMessageChannelIndex}" +
                               $" New message is a: {pmnMessageNode.m_bMessageType.ToString()} " +
                               $" with a sort value of : {pmnMessageNode.m_svaMessageSortingValue.ToString()} " +
                               $" and a hash of: {pmnMessageNode.m_lMessagePayloadHash.ToString()} " +
                               $" The last message for the channel had a sort value of: { m_gmcMessageChannels[iMessageChannel].m_msvLastSortValue.ToString()} " +
                               $" and a hash of :  { m_gmcMessageChannels[iMessageChannel].m_lHashOfLastNodeProcessed.ToString()} ");
                
                
                return false;
            }

            //get hash of previous data
            long lMessageParentHash = pmnMessageNode.m_lPreviousMessageHash;
            long lHashOfLastValidMessage = m_gmcMessageChannels[iMessageChannel].m_lHashOfLastNodeProcessed;

            //check if message parent hash matches last processed message
            if (lMessageParentHash != lHashOfLastValidMessage)
            {
                Debug.LogError("previous message hash for message did not match actual hash");
                return false;
            }

            //update the hash head for this channel
            m_gmcMessageChannels[iMessageChannel].m_lHashOfLastNodeProcessed = pmnMessageNode.m_lMessagePayloadHash;
            m_gmcMessageChannels[iMessageChannel].m_iLastMessageIndexProcessed = iMessageChannelIndex;
            m_gmcMessageChannels[iMessageChannel].m_msvLastSortValue = pmnMessageNode.m_svaMessageSortingValue;

            //update the chain link this channel is using as head 
            m_gmcMessageChannels[iMessageChannel].m_lChainLinkHeadHash = pmnMessageNode.m_lChainLinkHeadHash;

            //flag node as valid?
            return true;
        }

        //apply the vote command to the peer
        protected void ApplyVotesToChannel(int iMessageChannel,PeerMessageNode pmnMessage , TimeSpan tspVoteTimeout, int iMaxPlayerCount, NetworkingDataBridge ndbNetworkingDataBridge = null)
        {
            DateTime dtmMessageCreationTime = pmnMessage.m_dtmMessageCreationTime;
            VoteMessage vmsMessageNode = pmnMessage.m_gmbMessage as VoteMessage;

            //clear any previous votes that have failed  
            //RemoveFailedVotes(dtmMessageCreationTime, tspVoteTimeout);
            
            for (int i = 0; i < vmsMessageNode.m_tupActionPerPeer.Length; i++)
            {
                //
                GlobalMessageChannelState.ChannelVote.VoteType vacVoteAction = (GlobalMessageChannelState.ChannelVote.VoteType)vmsMessageNode.m_tupActionPerPeer[i].Item1;
                long lPeerID = vmsMessageNode.m_tupActionPerPeer[i].Item2;

                //process join commands
                if (vacVoteAction == GlobalMessageChannelState.ChannelVote.VoteType.Add || vacVoteAction == GlobalMessageChannelState.ChannelVote.VoteType.AntiAdd)
                {
                    int iIndex = int.MinValue;
                                       
                    //check if vote is already in progress for channel
                    if (TryGetIndexForPeer(lPeerID, out iIndex))
                    {
                        //check if peer is not already added
                        if (m_gmcMessageChannels[iIndex].m_staState == GlobalMessageChannelState.State.VoteJoin)
                        {
                            //add join vote to channel
                            m_gmcMessageChannels[iMessageChannel].AddOldStyleVote(iIndex, dtmMessageCreationTime, lPeerID,  vacVoteAction);
                            
                            //add new style vote 
                            m_gmcMessageChannels[iIndex].AddNewStyleVote(pmnMessage.m_lPeerID, vacVoteAction);
                        }
                    }
                    //if we are adding but no add vote exists then find an empty channel to start the vote on
                    else if (TryGetEmptyChannel(out iIndex) &&  
                             vacVoteAction != GlobalMessageChannelState.ChannelVote.VoteType.AntiAdd)
                    {
                        //set the channel to start voting process
                        m_gmcMessageChannels[iIndex].StartVoteJoinForPeer(lPeerID, dtmMessageCreationTime);

                        //add join vote to channel
                        m_gmcMessageChannels[iMessageChannel].AddOldStyleVote(iIndex, dtmMessageCreationTime, lPeerID, vacVoteAction);
                        
                        //add new style vote 
                        m_gmcMessageChannels[iIndex].AddNewStyleVote(pmnMessage.m_lPeerID, vacVoteAction);
                    }
                }
                else //process kick commands
                {
                    //get peer ID for kick target
                    if (TryGetIndexForPeer(lPeerID, out int iKickTarget))
                    {
                        //check that peer is not in the middle of joining
                        if (m_gmcMessageChannels[iKickTarget].m_staState != GlobalMessageChannelState.State.VoteJoin)
                        {
                            //check if kick action is already happening 
                            if (m_gmcMessageChannels[iKickTarget].m_staState != GlobalMessageChannelState.State.VoteKick)
                            {
                                m_gmcMessageChannels[iKickTarget].StartVoteKickForPeer(dtmMessageCreationTime);
                            }

                            //add kick vote to channel
                            m_gmcMessageChannels[iMessageChannel].AddOldStyleVote(iKickTarget, dtmMessageCreationTime, lPeerID, vacVoteAction);
                            
                            //add new style vote 
                            m_gmcMessageChannels[iKickTarget].AddNewStyleVote(pmnMessage.m_lPeerID, vacVoteAction);
                        }
                    }
                }
            }
            
            // //process join votes
            // ProcessJoin(iMessageChannel, dtmMessageCreationTime, tspVoteTimeout, out List<int> iJoinPeers);
            //                       
            // //process kick messages 
            // ProcessSplitVotes(lLocalPeerID, bActivePeer, iMessageChannel, dtmMessageCreationTime, tspVoteTimeout, iMaxPlayerCount, out List<int> iKickPeers);
            //
            // //changes are only stored in the sim message buffer if updating the main branch or unconfirmed message head 
            // if (ndbNetworkingDataBridge != null)
            // {
            //     //create a sim message for peers joining or leaving game
            //     AddPeerChangeMessageToSimBuffer(pmnMessage.m_svaMessageSortingValue, iKickPeers, iJoinPeers, ndbNetworkingDataBridge);
            // }
            //
            // //assign peers to channels
            // AddPeersToGlobalMessenger(iJoinPeers, dtmMessageCreationTime);
            //
            // //remove peer channels for kicked group
            // KickPeers(iKickPeers);
        }

        protected void ApplyAnyConnectionVotes( DateTime dtmTimeToCheckAt, SortingValue svaMessageSortVal, TimeSpan tspVoteTimeout, int iMaxPlayerCount, NetworkingDataBridge ndbNetworkingDataBridge = null)
        {
            //new version of agent kicking, works by timing out commands
            CheckForSuccessfulVotes( out List<int> iKickPeers, out List<int> iJoinPeers, dtmTimeToCheckAt, tspVoteTimeout);
            
            //changes are only stored in the sim message buffer if updating the main branch or unconfirmed message head 
            if (ndbNetworkingDataBridge != null)
            {
                //create a sim message for peers joining or leaving game
                AddPeerChangeMessageToSimBuffer(svaMessageSortVal, iKickPeers, iJoinPeers, ndbNetworkingDataBridge);
            }

            //assign peers to channels
            AddPeersToGlobalMessenger(iJoinPeers, dtmTimeToCheckAt);

            //remove peer channels for kicked group
            KickPeers(iKickPeers);
        }

        
        //check if anyone should be kicked
        protected void CheckForSuccessfulVotes(out List<int> iKickChannels, out List<int> iJoinChannels,  DateTime dtmTimeToCheck, TimeSpan tspVoteTimeout)
        {
            //loop through channels to get number of active channels
            int iActiveChannelCount = ActiveChannelCount();
            
            //create an array for all the votes
            iJoinChannels = new List<int>();
            iKickChannels = new List<int>();
            
            //loop through all clients 
            for(int i = 0 ; i < m_gmcMessageChannels.Count ; i++)
            {
                DateTime dtmTimeOfVoteStart = dtmTimeToCheck;
                
                //check if the vote is about to end
                if (m_gmcMessageChannels[i].m_staState == GlobalMessageChannelState.State.VoteKick)
                {
                    dtmTimeOfVoteStart = m_gmcMessageChannels[i].m_dtmKickVoteTime;
                }
                else if (m_gmcMessageChannels[i].m_staState == GlobalMessageChannelState.State.VoteJoin)
                {
                    dtmTimeOfVoteStart = m_gmcMessageChannels[i].m_dtmJoinVoteTime;
                }
                else
                {
                    continue;
                }
                
                //decide if this peer should be added to the keep or kick array depending on the type of vote
                List<int> targetOut = m_gmcMessageChannels[i].m_staState == GlobalMessageChannelState.State.VoteKick ? iKickChannels : iJoinChannels;
                
                //check time out or enough people have voted that the result can't change
                if ((dtmTimeToCheck - dtmTimeOfVoteStart ) > tspVoteTimeout)
                {
                    //evaluate if the vote was successful despite timing out
                    if (m_gmcMessageChannels[i].IsMajorityForVote())
                    {
                        //get the channel index
                        if (m_gmcMessageChannels[i].m_staState == GlobalMessageChannelState.State.VoteKick)
                        
                        //add to list of all kick targets
                        targetOut.Add(i);
                    }
                }
                else if (m_gmcMessageChannels[i].IsVoteResultCertain(iActiveChannelCount) &&
                         m_gmcMessageChannels[i].IsMajorityForVote())
                {
                    targetOut.Add(i);
                }
                else
                {
                    continue;
                }
                
                //clear and reset votes
                //this only happens if the result was certain or the vote timed out
                m_gmcMessageChannels[i].m_staState = GlobalMessageChannelState.State.Assigned;
                m_gmcMessageChannels[i].m_vtyVotesOnChannelByPeers.Clear();
            }
        }
        
        //process split vote
        protected void ProcessSplitVotes(long lLocalPeerID, bool bActivePeer, int iChangedMessageChannel, DateTime dtmTimeOfVote, TimeSpan tspVoteTimeout, int iMaxPlayerCount, out List<int> iKickPeers)
        {
            //get list of kick and non kick
            List<int> iKeepList = new List<int>();
            iKickPeers = new List<int>();

            GlobalMessageChannelState gmcChangedChannel = m_gmcMessageChannels[iChangedMessageChannel];

            //for each vote
            for (int i = 0; i < gmcChangedChannel.m_chvVotes.Count; i++)
            {
                //check if vote is for kicking peer and is still active
                if (gmcChangedChannel.m_chvVotes[i].m_vtpVoteType == GlobalMessageChannelState.ChannelVote.VoteType.Kick)
                {
                    if (gmcChangedChannel.m_chvVotes[i].IsActive(dtmTimeOfVote, tspVoteTimeout))

                    {
                        //todo remove this code
                        if (i < 0 || i > iMaxPlayerCount)
                        {
                            Debug.LogError("Attempting to kick peer out of bounds");
                        }

                        iKickPeers.Add(i);
                    }
                }
                else if (m_gmcMessageChannels[i].m_staState == GlobalMessageChannelState.State.Assigned) //get non kick peer list
                {
                    if (i < 0 || i > iMaxPlayerCount)
                    {
                        Debug.LogError("Attempting to add peer out of bounds");
                    }
                    iKeepList.Add(i);
                }
            }

            //check if anyone is getting kicked
            if (iKickPeers.Count == 0)
            {
                return;
            }

            //while there are users in the inGroup
            for (int i = 0; i < iKeepList.Count; i++)
            {
                //skip when processing votes by local player
                if (iKeepList[i] == iChangedMessageChannel)
                {
                    continue;
                }

                //get next peer
                int iChannelToProcess = iKeepList[i];

                GlobalMessageChannelState gmcInGroupChannel = m_gmcMessageChannels[iChannelToProcess];

                //cycle through all the channels to kick
                for (int j = iKickPeers.Count - 1; j > -1; j--)
                {
                    //get kick target
                    int iKickTargetChannel = iKickPeers[j];

                    //get in group peer vote for target
                    GlobalMessageChannelState.ChannelVote cvtVote = gmcInGroupChannel.m_chvVotes[iKickTargetChannel];

                    //check if in group peer is not voting to kick out group peer
                    if (cvtVote.IsActive(dtmTimeOfVote, tspVoteTimeout) == false ||
                        cvtVote.m_vtpVoteType != GlobalMessageChannelState.ChannelVote.VoteType.Kick ||
                        m_gmcMessageChannels[iKickTargetChannel].m_lChannelPeer != cvtVote.m_lPeerID)
                    {
                        //remove kick target from kick group and add them to the in group
                        iKickPeers.RemoveAt(j);
                        iKeepList.Add(iKickTargetChannel);
                    }
                }

                //check if there is anyone left in the kick group
                if (iKickPeers.Count == 0)
                {
                    //stop processing kick vote (vote has failed at this point)
                    return;
                }
            }

            //check if there is a split happening 
            bool bIsInKickGroup = false;

            //get the channel controlled by the local peer
            if (bActivePeer && TryGetIndexForPeer(lLocalPeerID, out int iIndexOfLoclPeerChannel))
            {
                for (int i = 0; i < iKickPeers.Count; i++)
                {
                    if (iKickPeers[i] == iIndexOfLoclPeerChannel)
                    {
                        bIsInKickGroup = true;

                        break;
                    }
                }
            }

            //invert kick group if peer is in wrong group
            if (bIsInKickGroup)
            {
                List<int> iTemp = iKeepList;
                iKeepList = iKickPeers;
                iKickPeers = iTemp;
            }

        }

        //process join vote
        protected void ProcessJoin(int iMessagingChannel, DateTime dtmTimeOfVote, TimeSpan tspVoteTimeout, out List<int> iJoinPeers)
        {
            //first value is the channel second is number of votes for, third is votes against
            List<Tuple<int, int, int>> tupJoinRequest = new List<Tuple<int, int, int>>();

            iJoinPeers = new List<int>();

            GlobalMessageChannelState gcsUpdatedChannel = m_gmcMessageChannels[iMessagingChannel];

            for (int i = 0; i < gcsUpdatedChannel.m_chvVotes.Count; i++)
            {
                GlobalMessageChannelState.ChannelVote cvtVote = gcsUpdatedChannel.m_chvVotes[i];

                //check if vote is for join and is still valid and peer 
                if (cvtVote.m_vtpVoteType == GlobalMessageChannelState.ChannelVote.VoteType.Add &&
                    cvtVote.IsActive(dtmTimeOfVote, tspVoteTimeout) &&
                    m_gmcMessageChannels[i].m_staState == GlobalMessageChannelState.State.VoteJoin &&
                    m_gmcMessageChannels[i].m_lChannelPeer == cvtVote.m_lPeerID)
                {
                    //add peer to the list
                    tupJoinRequest.Add(new Tuple<int, int, int>(i, 0, 0));
                }
            }

            int iActiveChannels = 0;

            //loop through all channels and add up votes
            for (int i = 0; i < m_gmcMessageChannels.Count; i++)
            {
                GlobalMessageChannelState gcsChannel = m_gmcMessageChannels[i];

                //check if channel is active
                if (gcsChannel.m_staState != GlobalMessageChannelState.State.Assigned)
                {
                    //skip channel as it has not been assigned to a peer
                    continue;
                }

                iActiveChannels++;

                //loop through all join requests 
                for (int j = 0; j < tupJoinRequest.Count; j++)
                {
                    Tuple<int, int, int> tupRequest = tupJoinRequest[j];

                    GlobalMessageChannelState.ChannelVote cvtVote = gcsChannel.m_chvVotes[tupRequest.Item1];

                    //check if channel voted to add peer
                    if (cvtVote.m_vtpVoteType == GlobalMessageChannelState.ChannelVote.VoteType.Add &&
                   cvtVote.IsActive(dtmTimeOfVote, tspVoteTimeout) &&
                   m_gmcMessageChannels[tupRequest.Item1].m_lChannelPeer == cvtVote.m_lPeerID)
                    {
                        //increment votes for add                        
                        tupJoinRequest[j] = new Tuple<int, int, int>(tupRequest.Item1, tupRequest.Item2 + 1, 0);
                    }
                }
            }

            int iMinVotesNeeded = (iActiveChannels + 1) / 2;

            //loop through join requests and check if any have enough votes
            for (int i = 0; i < tupJoinRequest.Count; i++)
            {
                if (tupJoinRequest[i].Item2 >= iMinVotesNeeded)
                {

                    iJoinPeers.Add(tupJoinRequest[i].Item1);
                }
            }
        }
        
        //adds a message to the sim message buffer that a peer or peers have joined or left the global messaging system 
        protected void AddPeerChangeMessageToSimBuffer(SortingValue svaChangeTime, in List<int> iPeersToKick, in List<int> iPeersToAdd, NetworkingDataBridge ndbNetworkingDataBridge)
        {
            //check that there is a change in the game layout
            if(iPeersToKick.Count == 0 && iPeersToAdd.Count == 0)
            {
                return;
            }

            //build kick and join message
            UserConnecionChange uccConnectionChange = new UserConnecionChange(iPeersToKick.Count, iPeersToAdd.Count);

            for(int i = 0; i < iPeersToKick.Count; i++)
            {
                uccConnectionChange.m_lKickPeerID[i] = m_gmcMessageChannels[iPeersToKick[i]].m_lChannelPeer;
                uccConnectionChange.m_iKickPeerChannelIndex[i] = iPeersToKick[i];
            }

            for (int i = 0; i < iPeersToAdd.Count; i++)
            {
                uccConnectionChange.m_lJoinPeerID[i] = m_gmcMessageChannels[iPeersToAdd[i]].m_lChannelPeer;
                uccConnectionChange.m_iJoinPeerChannelIndex[i] = iPeersToAdd[i];
            }

            ndbNetworkingDataBridge.QueueSimMessage(svaChangeTime, uccConnectionChange);
        }

        //perform join
        protected void AddPeersToGlobalMessenger(List<int> iJoinList, DateTime dtmTimeOfJoin)
        {
            for (int i = 0; i < iJoinList.Count; i++)
            {
                //assign peer to channel
                m_gmcMessageChannels[iJoinList[i]].AssignPeerToChannel(m_gmcMessageChannels[iJoinList[i]].m_lChannelPeer, dtmTimeOfJoin);

                //clear any votes on channel
                for (int j = 0; j < m_gmcMessageChannels.Count; j++)
                {
                    m_gmcMessageChannels[j].ClearVotesForChannelIndex(iJoinList[i]);
                }
                
                //clear new style votes
                m_gmcMessageChannels[iJoinList[i]].m_vtyVotesOnChannelByPeers.Clear();
            }

        }

        //perform split
        protected void KickPeers(List<int> iKickList)
        {
            //for each item in the kick list
            for (int i = 0; i < iKickList.Count; i++)
            {
                int ikickTarget = iKickList[i];

                //clear kicked channel
                m_gmcMessageChannels[ikickTarget].ClearChannel();

                for (int j = 0; j < m_gmcMessageChannels.Count; j++)
                {
                    //clear any votes for kicked player
                    m_gmcMessageChannels[j].ClearVotesForChannelIndex(ikickTarget);
                }
                
                //clear new style votes
                m_gmcMessageChannels[ikickTarget].m_vtyVotesOnChannelByPeers.Clear();
            }
        }

    }

    public partial class NetworkingByteStream
    {
        public static void Serialize(ReadByteStream rbsByteStream, ref GlobalMessagingState Output)
        {

            if (Output == null)
            {
                Output = new GlobalMessagingState();
            }

            int iPlayerCount = 0;
            ByteStream.Serialize(rbsByteStream, ref iPlayerCount);

            Output.m_gmcMessageChannels = new List<GlobalMessageChannelState>(iPlayerCount);

            for (int i = 0; i < iPlayerCount; i++)
            {
                GlobalMessageChannelState gmcChannelState = null;

                Serialize(rbsByteStream, ref gmcChannelState);

                Output.m_gmcMessageChannels.Add(gmcChannelState);                   
            }

            Serialize(rbsByteStream, ref Output.m_svaLastMessageSortValue);
        }

        public static void Serialize(WriteByteStream wbsByteStream, ref GlobalMessagingState Input)
        {
            int iPlayerCount = Input.m_gmcMessageChannels.Count;
            ByteStream.Serialize(wbsByteStream, ref iPlayerCount);

            for (int i = 0; i < iPlayerCount; i++)
            {
                GlobalMessageChannelState gmcChannelState = Input.m_gmcMessageChannels[i];

                Serialize(wbsByteStream, ref gmcChannelState);
            }

            Serialize(wbsByteStream, ref Input.m_svaLastMessageSortValue);
        }

        public static int DataSize(GlobalMessagingState Input)
        {
            int iSize = 0;
            iSize += ByteStream.DataSize(Input.m_gmcMessageChannels.Count);
            
            for(int i = 0; i < Input.m_gmcMessageChannels.Count; i++)
            {
                iSize += DataSize(Input.m_gmcMessageChannels[i]);
            }

            iSize += DataSize(Input.m_svaLastMessageSortValue);

            return iSize;
        }
    }
}
