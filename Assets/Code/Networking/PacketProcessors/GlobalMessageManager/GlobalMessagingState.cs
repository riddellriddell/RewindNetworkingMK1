using System;
using System.Collections.Generic;

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

        //process a message lLocalPeer is the user in controll of this computer
        //and must be in the "Kept" group when split occures 
        public void ProcessMessage(long lLocalPeer, bool bActivePeer, PeerMessageNode pmnMessageNode, GlobalSimMessageBuffer smbSimMessageBuffer = null)
        {
            //update the most recent sorting value 
            m_svaLastMessageSortValue = SortingValue.Max(m_svaLastMessageSortValue, pmnMessageNode.m_svaMessageSortingValue);

            //check if peer is in game 
            if (pmnMessageNode.m_lPeerID != long.MinValue && TryGetIndexForPeer(pmnMessageNode.m_lPeerID, out int iIndexOfMessageChannel))
            {
                //validate message 
                bool bIsValidMessage = ValidateAndApplyMessageChangeToChannel(iIndexOfMessageChannel, pmnMessageNode);

                //filter invalid messages
                if (bIsValidMessage == true)
                {
                    //check if message is a vote
                    if (pmnMessageNode.m_bMessageType == VoteMessage.TypeID)
                    {
                        //apply votes
                        ApplyVotesToChannel(lLocalPeer, bActivePeer, iIndexOfMessageChannel, pmnMessageNode, smbSimMessageBuffer);
                    }
                    else if (smbSimMessageBuffer != null && pmnMessageNode.m_gmbMessage is ISimMessagePayload)
                    {
                        //store sim message 
                        smbSimMessageBuffer.QueueSimMessage(pmnMessageNode.m_svaMessageSortingValue, pmnMessageNode.m_lPeerID, iIndexOfMessageChannel, pmnMessageNode.m_gmbMessage as ISimMessagePayload);
                    }
                }
            }
            else
            {
                //peer cant vote or create inputs because they are not part of the global message system
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

        //TODO: track chanel activation and store in an int
        //instead of recalculating every time
        public int ActiveChannelCount()
        {
            int iActiveChannels = 0;

            for(int i = 0; i < m_gmcMessageChannels.Count;i++)
            {
                if(m_gmcMessageChannels[i].m_staState == GlobalMessageChannelState.State.Assigned)
                {
                    iActiveChannels++;
                }
            }

            return iActiveChannels;
        }

        //check for failed votes and reset 
        public void RemoveFailedVotesDeprecated(DateTime dtmTime)
        {
            //get list of all votes in action
            List<int> iVotesInAction = new List<int>();
            List<int> iActivePeers = new List<int>();
            for (int i = 0; i < m_gmcMessageChannels.Count; i++)
            {
                if (m_gmcMessageChannels[i].m_staState == GlobalMessageChannelState.State.VoteJoin)
                {
                    iVotesInAction.Add(i);
                }
                else if (m_gmcMessageChannels[i].m_staState == GlobalMessageChannelState.State.Assigned)
                {
                    iActivePeers.Add(i);
                }
            }

            //check if anyone is still voting on peer
            for (int i = 0; i < iVotesInAction.Count; i++)
            {
                bool bIsActive = false;

                for (int j = 0; j < iActivePeers.Count; j++)
                {
                    //get vote for peer
                    GlobalMessageChannelState.ChannelVote cvtVote = m_gmcMessageChannels[iActivePeers[j]].m_chvVotes[iVotesInAction[i]];

                    //check if vote is to add peer and is still active
                    if (cvtVote.m_vtpVoteType == GlobalMessageChannelState.ChannelVote.VoteType.Add &&
                        cvtVote.IsActive(dtmTime, s_tspVoteTimeout) &&
                        m_gmcMessageChannels[iVotesInAction[i]].m_lChannelPeer == cvtVote.m_lPeerID)
                    {
                        bIsActive = true;
                        break;
                    }
                }

                //check if anyone still actively voting to add peer
                if (bIsActive == false)
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

        public void RemoveFailedVotes(DateTime dtmTime)
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
                TimeSpan tspTimeSinceVoteStart =  dtmTime - m_gmcMessageChannels[i].m_dtmVoteTime;

                //check if vote has timed out
                if(tspTimeSinceVoteStart > s_tspVoteTimeout)
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
        protected bool ValidateAndApplyMessageChangeToChannel(int iMessageChannel, PeerMessageNode pmnMessageNode)
        {
            //check if message is next in queue for peer
            UInt32 iMessageChannelIndex = pmnMessageNode.m_iPeerMessageIndex;
            UInt32 iCurrentChannelIndex = m_gmcMessageChannels[iMessageChannel].m_iLastMessageIndexProcessed;

            //check if message is next in peer message chain
            if (iCurrentChannelIndex + 1 != iMessageChannelIndex)
            {
                return false;
            }

            //get hash of previouse data
            long lMessageParentHash = pmnMessageNode.m_lPreviousMessageHash;
            long lHashOfLastValidMessage = m_gmcMessageChannels[iMessageChannel].m_lHashOfLastNodeProcessed;

            //check if message parent hash matches last processed message
            if (lMessageParentHash != lHashOfLastValidMessage)
            {
                return false;
            }

            //update the hash head for this channel
            m_gmcMessageChannels[iMessageChannel].m_lHashOfLastNodeProcessed = pmnMessageNode.m_lMessagePayloadHash;
            m_gmcMessageChannels[iMessageChannel].m_iLastMessageIndexProcessed = iMessageChannelIndex;

            //update the chain link this channel is using as head 
            m_gmcMessageChannels[iMessageChannel].m_lChainLinkHeadHash = pmnMessageNode.m_lChainLinkHeadHash;

            //flag node as valid?
            return true;
        }

        //apply the vote command to the peer
        protected void ApplyVotesToChannel(long lLocalPeerID, bool bActivePeer, int iMessageChannel,PeerMessageNode pmnMessage , GlobalSimMessageBuffer smbSimMessageBuffer = null)
        {

            DateTime dtmMessageCreationTime = pmnMessage.m_dtmMessageCreationTime;
            VoteMessage vmsMessageNode = pmnMessage.m_gmbMessage as VoteMessage;

            for (int i = 0; i < vmsMessageNode.m_tupActionPerPeer.Length; i++)
            {
                // a value of 0 is kick 1 is join
                byte bIsJoin = vmsMessageNode.m_tupActionPerPeer[i].Item1;
                long lPeerID = vmsMessageNode.m_tupActionPerPeer[i].Item2;

                //clear any previous votes that have failed  
                RemoveFailedVotes(dtmMessageCreationTime);

                //process join commands
                if (bIsJoin > 0)
                {
                    int iIndex = int.MinValue;
                                       
                    //check if vote is already in progress for channel
                    if (TryGetIndexForPeer(lPeerID, out iIndex))
                    {
                        //check if peer is not alreadty added
                        if (m_gmcMessageChannels[iIndex].m_staState == GlobalMessageChannelState.State.VoteJoin)
                        {
                            //add joim vote to channel
                            m_gmcMessageChannels[iMessageChannel].AddConnectionVote(iIndex, dtmMessageCreationTime, lPeerID);
                        }
                    }
                    else if (TryGetEmptyChannel(out iIndex))//try get empty channel
                    {
                        //set the channel to start voting process
                        m_gmcMessageChannels[iIndex].StartVoteJoinForPeer(lPeerID, dtmMessageCreationTime);

                        //add join vote to channel
                        m_gmcMessageChannels[iMessageChannel].AddConnectionVote(iIndex, dtmMessageCreationTime, lPeerID);
                    }
                }
                else //process kick commands
                {
                    //get peer ID for kick target
                    if (TryGetIndexForPeer(lPeerID, out int iKickTarget))
                    {
                        //check that peer has joined 
                        if (m_gmcMessageChannels[iKickTarget].m_staState != GlobalMessageChannelState.State.VoteJoin)
                        {
                            //check if kick action is alredy happening 
                            if (m_gmcMessageChannels[iKickTarget].m_staState != GlobalMessageChannelState.State.VoteKick)
                            {
                                m_gmcMessageChannels[iKickTarget].StartVoteKickForPeer(dtmMessageCreationTime);
                            }

                            //add kick vote to channel
                            m_gmcMessageChannels[iMessageChannel].AddKickVote(iKickTarget, dtmMessageCreationTime, lPeerID);
                        }
                    }
                }
            }
            
            //process join votes
            ProcessJoin(iMessageChannel, dtmMessageCreationTime, out List<int> iJoinPeers);
                                  
            //process kick messages 
            ProcessSplitVotes(lLocalPeerID, bActivePeer, iMessageChannel, dtmMessageCreationTime, out List<int> iKickPeers);

            //changes are only stored in the sim messsage buffer if updating the main branch or unconfimed message head 
            if (smbSimMessageBuffer != null)
            {
                //create a sim message for peers joining or leaving game
                AddPeerChangeMessageToSimBuffer(pmnMessage.m_svaMessageSortingValue, iKickPeers, iJoinPeers, smbSimMessageBuffer);
            }

            //assign peers to channels
            AddPeersToGlobalMessenger(iJoinPeers, dtmMessageCreationTime);

            //remove peer channels for kicked group
            KickPeers(iKickPeers);
        }

        //process split vote
        protected void ProcessSplitVotes(long lLocalPeerID, bool bActivePeer, int iChangedMessageChannel, DateTime dtmTimeOfVote, out List<int> iKickPeers)
        {
            //get list of kick and non kick
            List<int> iKeepList = new List<int>();
            iKickPeers = new List<int>();

            GlobalMessageChannelState gmcChangedChannel = m_gmcMessageChannels[iChangedMessageChannel];

            //for each vote
            for (int i = 0; i < gmcChangedChannel.m_chvVotes.Count; i++)
            {
                //check if vote is for kicking peer and is still active
                if (gmcChangedChannel.m_chvVotes[i].m_vtpVoteType == GlobalMessageChannelState.ChannelVote.VoteType.Kick &&
                    gmcChangedChannel.m_chvVotes[i].IsActive(dtmTimeOfVote, s_tspVoteTimeout))
                {
                    iKickPeers.Add(i);
                }
                else if (m_gmcMessageChannels[i].m_staState == GlobalMessageChannelState.State.Assigned) //get non kick peer list
                {
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
                    if (cvtVote.IsActive(dtmTimeOfVote, s_tspVoteTimeout) == false ||
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
        protected void ProcessJoin(int iMessagingChannel, DateTime dtmTimeOfVote, out List<int> iJoinPeers)
        {
            //first value is the channel seccond is number of voted
            List<Tuple<int, int>> tupJoinRequest = new List<Tuple<int, int>>();

            iJoinPeers = new List<int>();

            GlobalMessageChannelState gcsUpdatedChannel = m_gmcMessageChannels[iMessagingChannel];

            for (int i = 0; i < gcsUpdatedChannel.m_chvVotes.Count; i++)
            {
                GlobalMessageChannelState.ChannelVote cvtVote = gcsUpdatedChannel.m_chvVotes[i];

                //check if vote is for join and is still valid and peer 
                if (cvtVote.m_vtpVoteType == GlobalMessageChannelState.ChannelVote.VoteType.Add &&
                    cvtVote.IsActive(dtmTimeOfVote, s_tspVoteTimeout) &&
                    m_gmcMessageChannels[i].m_staState == GlobalMessageChannelState.State.VoteJoin &&
                    m_gmcMessageChannels[i].m_lChannelPeer == cvtVote.m_lPeerID)
                {
                    //add peer to the list
                    tupJoinRequest.Add(new Tuple<int, int>(i, 0));
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
                    Tuple<int, int> tupRequest = tupJoinRequest[j];

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
            for (int i = 0; i < tupJoinRequest.Count; i++)
            {
                if (tupJoinRequest[i].Item2 >= iMinVotesNeeded)
                {

                    iJoinPeers.Add(tupJoinRequest[i].Item1);
                }
            }
        }
        
        //adds a messaget to the sim message buffer that a peer or peers have joined or left the global messaging system 
        protected void AddPeerChangeMessageToSimBuffer(SortingValue svaChangeTime, in List<int> iPeersToKick, in List<int> iPeersToAdd, GlobalSimMessageBuffer smbSimMessageBuffer)
        {
            if(iPeersToKick.Count == 0 || iPeersToAdd.Count == 0)
            {
                return;
            }

            //build kick and join message
            GlobalSimMessageBuffer.UserConnecionChange uccConnectionChange = new GlobalSimMessageBuffer.UserConnecionChange(iPeersToKick.Count, iPeersToAdd.Count);

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

            smbSimMessageBuffer.QueuePlayerChangeMessage(svaChangeTime, uccConnectionChange);
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
            }
        }

        //perfotm split
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
            }
        }

    }

    public partial class ByteStream
    {
        public static void Serialize(ReadByteStream rbsByteStream, ref GlobalMessagingState Output)
        {

            if (Output == null)
            {
                Output = new GlobalMessagingState();
            }

            int iPlayerCount = 0;
            Serialize(rbsByteStream, ref iPlayerCount);

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
            Serialize(wbsByteStream, ref iPlayerCount);

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
            iSize += DataSize(Input.m_gmcMessageChannels.Count);
            
            for(int i = 0; i < Input.m_gmcMessageChannels.Count; i++)
            {
                iSize += DataSize(Input.m_gmcMessageChannels[i]);
            }

            iSize += DataSize(Input.m_svaLastMessageSortValue);

            return iSize;
        }
    }
}
