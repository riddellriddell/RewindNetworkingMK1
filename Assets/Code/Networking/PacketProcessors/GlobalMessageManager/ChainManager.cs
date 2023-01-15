using System;
using System.Collections.Generic;
using UnityEngine;

namespace Networking
{
    /// <summary>
    /// this class manages a list of chain links used by the global state machiene
    /// </summary>
    public class ChainManager
    {
        #region LinkRebasing
        //the percent of active peers that need to acknowledge a link to make it 
        //a valid base link candidate
        public float PercentOfAcknowledgementsToRebase { get; private set; }

        public uint MinCycleAge { get; private set; }

        public uint MaxCycleAge { get; private set; }

        public int MinChainLenght { get; private set; }

        public int MaxChannelCount { get; private set; }

        #endregion
        #region Voting

        //the amount of time a vote lasts and peers can make a vote before it times out the results disgarded
        public TimeSpan VoteTimeout { get; private set; }

        #endregion

        #region LinkTiming
        public static TimeSpan TimeBetweenLinks { get; private set; }

        //each chain is based off the start of the year
        public static DateTime GetChainBaseTime(DateTime dtmCurrentNetworkTime)
        {
            int iYear = dtmCurrentNetworkTime.Year;
            return new DateTime(iYear, 1, 1);
        }
        #endregion

        //list of all the potential starting states for the global message system
        //this is used when the peer is connecting to the system for the first time 
        public Dictionary<long, GlobalMessageStartStateCandidate> StartStateCandidates { get; } = new Dictionary<long, GlobalMessageStartStateCandidate>();

        public int m_iStartStatesRecieved = 0;

        //buffer of all recieved chain links
        public SortedList<SortingValue, ChainLink> ChainLinks { get; } = new SortedList<SortingValue, ChainLink>();

        //the starting connection state for the chain
        public GlobalMessagingState m_gmsChainStartState;

        //the oldest link in the validated chain
        public ChainLink m_chlChainBase;

        //the highest ranked chain 
        public ChainLink m_chlBestChainHead;

        public void SetStartState(long lFirstPeerID, int iMaxPeerCount, DateTime dtmStartTime)
        {
            m_gmsChainStartState = new GlobalMessagingState(iMaxPeerCount, lFirstPeerID, dtmStartTime);
        }

        public void AddFirstChainLink(long lLocalPeerID, bool bActivePeer, ChainLink chkChainLink, NetworkingDataBridge ndbNetworkDataBridge)
        {
            //add chain links to buffer 
            ChainLinks.Add(chkChainLink.m_svaChainSortingValue, chkChainLink);

            //validate chainlink to make sure all peers are seeing the samme thing 
            ChainLinkVerifier.RegisterLink(chkChainLink, lLocalPeerID);

            //set as base chain link
            m_chlChainBase = chkChainLink;

            m_chlBestChainHead = chkChainLink;

            chkChainLink.m_bIsConnectedToBase = true;

            //calculate chain state 
            chkChainLink.CaluclateGlobalMessagingStateAtEndOflink(lLocalPeerID, bActivePeer, m_gmsChainStartState, VoteTimeout, MaxChannelCount, ndbNetworkDataBridge);

            //setup acknoledgements 
            chkChainLink.m_bIsChannelBranch = new List<bool>(MaxChannelCount);

            //set acknowledgement of first link
            //this assumes the inital peer is at position 0;
            for (int i = 0; i < MaxChannelCount; i++)
            {
                chkChainLink.m_bIsChannelBranch.Add(false);
            }

            chkChainLink.m_bIsChannelBranch[0] = true;
        }

        public void AddChainLink(long lLocalPeerID, bool bActivePeer, ChainLink chlLink, GlobalMessageKeyManager gkmKeyManager, GlobalMessageBuffer gmbGlobalMessageBuffer, NetworkingDataBridge ndbNetworkingDataBridge, out bool bDirtyUnconfirmedMessageBufferState)
        {
            //validate chainlink to make sure all peers are seeing the samme thing 
            ChainLinkVerifier.RegisterLink(chlLink, lLocalPeerID);

            bDirtyUnconfirmedMessageBufferState = false;

            //check that chain link is valid 
            ValidateChainSource(chlLink, gkmKeyManager);

            //get the chain link index to start at 
            SortingValue svaLinkSortValue = chlLink.m_svaChainSortingValue;

            //check if link already exists in buffer
            if (ChainLinks.ContainsKey(svaLinkSortValue))
            {
                //dont double add chain link
                return;
            }

            //add chain links to buffer 
            ChainLinks.Add(svaLinkSortValue, chlLink);

            //merge messages into the message buffer 
            //update data bridge if new messages have been added to the message buffer that the sim will need to process
            //bIsDirty is true if new messages were added
            MergeChainLinkMessagesIntoBuffer(chlLink, gmbGlobalMessageBuffer, ndbNetworkingDataBridge, out bDirtyUnconfirmedMessageBufferState);

            //recalculate chain values
            ReprocessAllChainLinks(lLocalPeerID, bActivePeer);

            //update the best chain
            ChainLink chlBestLink = GetBestHeadChainLink(gmbGlobalMessageBuffer);

            //check that chain head changed 
            if (chlBestLink != m_chlBestChainHead)
            {
                //apply change to message buffer 
                OnBestHeadChange(chlBestLink, lLocalPeerID, bActivePeer, ndbNetworkingDataBridge, gmbGlobalMessageBuffer);

                bDirtyUnconfirmedMessageBufferState = true;
            }
        }

        //find a link searching back from iStartIndex that matches bHash
        public ChainLink FindLink(int iStartIndex, long lHash)
        {
            // loop backwards through chain links and try and find parent link
            for (int i = iStartIndex; i > -1; i--)
            {
                ChainLink chlLink = ChainLinks.Values[i];

                //check if link matches target hash
                if (chlLink.m_lLinkPayloadHash == lHash)
                {
                    return chlLink;
                }
            }

            return null;
        }

        public ChainLink FindLink(long lHash)
        {
            //start at the most recent as thats the most likely
            // link to be searching for
            return FindLink(ChainLinks.Count - 1, lHash);
        }

        //finds all the chain links from a shared base to GetChainLinksTo returns false if no shared base is found
        //list is returned with the newews link first and oldest last 
        public bool GetChainLinksFromSharedBase(ChainLink chlGetLinksTo, ChainLink chlFromSharedBase, ref List<ChainLink> chlLinksFromSharedBase)
        {
            if (chlFromSharedBase == null)
            {
                while(chlGetLinksTo != null)
                {
                    chlLinksFromSharedBase.Add(chlGetLinksTo);

                    chlGetLinksTo = chlGetLinksTo.m_chlParentChainLink;
                }

                return false;
            }

            chlLinksFromSharedBase.Add(chlGetLinksTo);

            while(chlGetLinksTo.m_chlParentChainLink != chlFromSharedBase)
            {
                //early out if this chain has hit the base link or is the fist link in the chain
                if(chlGetLinksTo.m_chlParentChainLink == null )
                {
                    return false;
                }

                //walk back up the chain, steping the newest link back until a shared link is found
                if(chlFromSharedBase == null || chlGetLinksTo.m_chlParentChainLink.m_iLinkIndex > chlFromSharedBase.m_iLinkIndex)
                {
                    chlGetLinksTo = chlGetLinksTo.m_chlParentChainLink;

                    chlLinksFromSharedBase.Add(chlGetLinksTo);
                }
                else
                {
                    chlFromSharedBase = chlFromSharedBase.m_chlParentChainLink;
                }
            }

            return true;
        }

        //TODO::this forces the processed up to point back to the start of the message chain even if all the messages are the same
        //the m_svaSimProcessedMessagesUpTo should only change if the chain link has different messages than the buffer
        //the new chain links should only clear the buffer up to the end of the last chain link. the extra links at the end that do not fall within a chain link should not be 
        //cleared
        public void ApplyChangesToSimMessageBuffer(long lLocalPeer, bool bIsActive, List<ChainLink> chlLinkChanges, NetworkingDataBridge ndbNetworkingDataBridge)
        {
            GlobalMessagingState gsmMessageState = chlLinkChanges[chlLinkChanges.Count - 1].m_chlParentChainLink.m_gmsState.Clone() as GlobalMessagingState;

            ChainLink chlParentLinkWithMessage = chlLinkChanges[chlLinkChanges.Count - 1].m_chlParentChainLink;

            //get the end of the parent chain link to clear our any messages in the buffer that fall between the old and new chain links
            while (chlParentLinkWithMessage != null)
            {
                if(chlParentLinkWithMessage.m_pmnMessages.Count > 0)
                {
                    SortingValue svaLastMessage = chlParentLinkWithMessage.m_pmnMessages[chlParentLinkWithMessage.m_pmnMessages.Count - 1].m_svaMessageSortingValue;

                    SortingValue svaOldestSyncState = ndbNetworkingDataBridge.m_svaOldestActiveSimTime;

                    SortingValue svaOldestConfirmedMessage = SortingValue.Max( svaLastMessage, svaOldestSyncState);

                    if (m_chlChainBase.m_gmsState.m_svaLastMessageSortValue.CompareTo(svaOldestConfirmedMessage) >= 0)
                    {
                        Debug.LogError("Trying to set the last new message added earlier than the end of the base state");
                    }

                    //make sure the sim reprocess the message queue starting from the end of the last chain
                    ndbNetworkingDataBridge.UpdateProcessedTimeOnNewMessageAdded(svaOldestConfirmedMessage);
            
                    //remove all  the messages after the parent chain last message
                    ndbNetworkingDataBridge.m_squInMessageQueue.ClearFrom(svaOldestConfirmedMessage);
            
                    break;
                }
                else
                {
                    chlParentLinkWithMessage = chlParentLinkWithMessage.m_chlParentChainLink;
                }
            }

            //loop through all the new chain links and apply the changes to the sim message buffer 
            for (int i = chlLinkChanges.Count -1; i  > -1; i--)
            {
                //loop through all the messages and apply them to the game state 
                for(int j = 0; j < chlLinkChanges[i].m_pmnMessages.Count; j++)
                {
                    gsmMessageState.ProcessMessage(lLocalPeer, bIsActive, chlLinkChanges[i].m_pmnMessages[j], VoteTimeout, MaxChannelCount, ndbNetworkingDataBridge);
                }
            }
        }

        public void SetChannelAcknowledgements(int iChannelIndex, ChainLink chlAcknowledgedLink)
        {
            //check if alreadty acked 
            if (chlAcknowledgedLink.m_bIsChannelBranch[iChannelIndex])
            {
                return;
            }

            //clear out existing acknowledements 
            //and acknowledge new branch
            for (int i = ChainLinks.Count - 1; i > -1; i--)
            {
                //check if acknowledgements need setting up
                if (ChainLinks.Values[i].m_bIsChannelBranch == null || ChainLinks.Values[i].m_bIsChannelBranch.Count != MaxChannelCount)
                {
                    SetupChannelAckArray(ChainLinks.Values[i], MaxChannelCount);
                }

                if (ChainLinks.Values[i] == chlAcknowledgedLink && chlAcknowledgedLink != null)
                {
                    //mark link as part of branch
                    ChainLinks.Values[i].m_bIsChannelBranch[iChannelIndex] = true;

                    //get the next chain link that needs to be acknowledged
                    chlAcknowledgedLink = ChainLinks.Values[i].m_chlParentChainLink;
                }
                else
                {
                    //clear any acknowledgement 
                    ChainLinks.Values[i].m_bIsChannelBranch[iChannelIndex] = false;
                }
            }
        }

        public bool ValidateChainSource(ChainLink chlLink, GlobalMessageKeyManager gkmKeyManager)
        {
            //perform black magic 
            return true;
        }

        //get parent for chain link if it has not already been found, update chain length 
        public void ProceesChainLink(long lLocalPeerID, bool bActivePeer, ChainLink chlLink)
        {
            //skip if base link as it should have been processed already 
            if (chlLink == m_chlChainBase)
            {
                return;
            }

            //check if acknowledgements need setting up
            if (chlLink.m_bIsChannelBranch == null || chlLink.m_bIsChannelBranch.Count != MaxChannelCount)
            {
                SetupChannelAckArray(chlLink, MaxChannelCount);
            }
            
            //check if link needs to find parent and is not base which will have no parent
            if (chlLink.m_chlParentChainLink == null)
            {
                //try and find the parent for this chain link based on the 
                //link parent hash
                SetParentForLink(chlLink);

                //check if parent link was found 
                if (chlLink.m_chlParentChainLink == null)
                {
                    chlLink.m_bIsConnectedToBase = false;
                    chlLink.m_iChainLength = 0;
                    chlLink.m_lChainMessageCount = (ulong)chlLink.m_pmnMessages.Count;

                    //cant do chain link analisis 
                    return;
                }
            }

            //check if parent link is connected to base
            if (chlLink.m_chlParentChainLink.m_bIsConnectedToBase == true || m_chlChainBase == chlLink.m_chlParentChainLink)
            {
                chlLink.m_bIsConnectedToBase = true;
            }
            else
            {
                chlLink.m_bIsConnectedToBase = false;
            }

            //check if state needs to be calculated 
            if (chlLink.m_bIsConnectedToBase && chlLink.m_gmsState == null)
            {
                chlLink.CaluclateGlobalMessagingStateAtEndOflink(lLocalPeerID, bActivePeer, chlLink.m_chlParentChainLink.m_gmsState, VoteTimeout, MaxChannelCount);
            }

            //TODO: do this in a more efficient way that is not redone for every link when a new link is 
            //accepted 
            //set peer as acknowledging chain 
            if (m_chlBestChainHead.m_gmsState.TryGetIndexForPeer(chlLink.m_lPeerID, out int iChannelIndex))
            {
                //set acknowledgement for peer 
                SetChannelAcknowledgements(iChannelIndex, chlLink);
            }

            //calculate chain length
            chlLink.m_lChainMessageCount = chlLink.m_chlParentChainLink.m_lChainMessageCount + (ulong)chlLink.m_pmnMessages.Count;
            chlLink.m_iChainLength = chlLink.m_chlParentChainLink.m_iChainLength + 1;

            //Debug.Log($"Processed new chain link with length {chlLink.m_iChainLength}");
        }

        public void SetupChannelAckArray(ChainLink chlLink, int iMaxPlayerCount)
        {
            //create ack array
            chlLink.m_bIsChannelBranch = new List<bool>(iMaxPlayerCount);

            for (int i = 0; i < iMaxPlayerCount; i++)
            {
                chlLink.m_bIsChannelBranch.Add(false);
            }
        }

        public void SetParentForLink(ChainLink chlTargetLink)
        {
            //get the chain link index to start at 
            SortingValue svaLinkSortValue = chlTargetLink.m_svaChainSortingValue;

            //loop through all items before chain link and try and get parent link
            int iIndex = ChainLinks.IndexOfKey(svaLinkSortValue);

            //start search before index of link just added
            iIndex--;

            //get parent of chain link
            ChainLink chlParentLink = FindLink(iIndex, chlTargetLink.m_lPreviousLinkHash);

            //check if valid parent that was created before link was created
            if (chlParentLink != null && chlParentLink.m_iLinkIndex < chlTargetLink.m_iLinkIndex)
            {
                //set linkage in chain 
                chlTargetLink.m_chlParentChainLink = chlParentLink;
            }
            else
            {
                chlTargetLink.m_chlParentChainLink = null;
            }
        }

        //add a new potential chain start state 
        public void AddNewStartCandidate(GlobalMessageStartStateCandidate sscStateCandidate)
        {
            //increment the number of start states recieved 
            m_iStartStatesRecieved++;

            //check if already added to candidate buffer
            if (StartStateCandidates.TryGetValue(sscStateCandidate.m_lHashOfStateCandidate,out GlobalMessageStartStateCandidate sscStartState) == true)
            {
                sscStartState.m_iStartStateScore += 1;

                return;
            }
            else
            {
                //add to start candidaate list 
                StartStateCandidates.Add(sscStateCandidate.m_lHashOfStateCandidate, sscStateCandidate);
            }
        }

        //evaluate the start state options to select the best one
        //this process looks for the oldest start state with as many child states linked to it / based off it
        public void EvaluateStartCandidates(long lLocalPeer, bool bActivePeer)
        {
            foreach (GlobalMessageStartStateCandidate sscCandidate in StartStateCandidates.Values)
            {
                //check if not oldest start state in chain 
                if (sscCandidate.m_bIsOldestStateOnChain == false)
                {
                    continue;
                }

                //check if first link attached
                if (sscCandidate.m_chlNextLink == null)
                {
                    sscCandidate.m_chlNextLink = FindLink(sscCandidate.m_lNextLinkHash);

                    //check if link was found
                    if (sscCandidate.m_chlNextLink == null)
                    {
                        //skip this link as it is not fully set up
                        continue;
                    }
                }

                //get the start index
                int iStartIndex = ChainLinks.IndexOfKey(sscCandidate.m_chlNextLink.m_svaChainSortingValue);

                //check if chain link is in main chain 
                if (iStartIndex < 0)
                {
                    continue;
                }

                GlobalMessagingState gmsPreviousLinkEndState = sscCandidate.m_gmsStateCandidate;

                //reprocess all chain links
                for (int i = iStartIndex; i < ChainLinks.Count; i++)
                {
                    //reset state 
                    ChainLinks.Values[i].m_gmsState = null;

                    if (i != iStartIndex)
                    {
                        //check if correctly linked to parent
                        if (ChainLinks.Values[i].m_chlParentChainLink == null)
                        {
                            SetParentForLink(ChainLinks.Values[i]);

                            //check if it has a valid parent state 
                            if (ChainLinks.Values[i].m_chlParentChainLink == null)
                            {
                                continue;
                            }
                        }

                        //check if link is linking to before the current state 
                        if (ChainLinks.Values[i].m_chlParentChainLink.m_svaChainSortingValue.CompareTo(sscCandidate.m_chlNextLink.m_svaChainSortingValue) < 0)
                        {
                            continue;
                        }

                        //check that parent state has a valid end state
                        if (ChainLinks.Values[i].m_chlParentChainLink.m_gmsState == null)
                        {
                            continue;
                        }

                        //set the prevouse state to buld off
                        gmsPreviousLinkEndState = ChainLinks.Values[i].m_chlParentChainLink.m_gmsState;

                        //check if a start candidate exists for chain link
                        long lHashForLinkState = GlobalMessageStartStateCandidate.GenerateHash(gmsPreviousLinkEndState, ChainLinks.Values[i].m_lLinkPayloadHash);

                        if (StartStateCandidates.TryGetValue(lHashForLinkState, out GlobalMessageStartStateCandidate sscChildStartStateCandidate))
                        {
                            if (sscChildStartStateCandidate.m_bIsOldestStateOnChain == true)
                            {
                                sscChildStartStateCandidate.m_bIsOldestStateOnChain = false;

                                sscCandidate.m_iStartStateScore += sscChildStartStateCandidate.m_iStartStateScore;
                            }                            
                        }
                    }

                    //recalculate state at end of chain link
                    ChainLinks.Values[i].CaluclateGlobalMessagingStateAtEndOflink(lLocalPeer, bActivePeer, gmsPreviousLinkEndState, VoteTimeout, MaxChannelCount);
                }
            }
        }

        //Pick Best Start Candidate 
        //returns true if best state passes threshold 
        public bool GetBestStartStateCandidate(out GlobalMessageStartStateCandidate sscBestCandidate)
        {
            //the largest number of proposed peers 
            int iMaxProposedScore = 0;

            sscBestCandidate = null;

            //loop through all states
            foreach (GlobalMessageStartStateCandidate sscCandidate in StartStateCandidates.Values)
            {
                //get the max number of peers active in a state 
                iMaxProposedScore = Mathf.Max(sscCandidate.m_gmsStateCandidate.ActiveChannelCount(), iMaxProposedScore);

                //get the highest voted state
                if (sscBestCandidate == null || sscBestCandidate.m_iStartStateScore < sscCandidate.m_iStartStateScore)
                {
                    sscBestCandidate = sscCandidate;
                }
            }

            if(sscBestCandidate == null)
            {
                return false;
            }

            //get the difference between the most votes vs the most possible votes 
            int iVotingDifference = sscBestCandidate.m_iStartStateScore - iMaxProposedScore;

            if (iVotingDifference < 0)
            {
                return false;
            }

            return true;
        }

        //adds a new chain link but does not process it as the start state has not been finilized yet 
        public void AddChainLinkPreConnection(long lLocalPeerID, ChainLink chlLink, GlobalMessageBuffer gmbGlobalMessageBuffer, NetworkingDataBridge ndbNetworkDataBridge)
        {
            //validate chainlink to make sure all peers are seeing the samme thing 
            ChainLinkVerifier.RegisterLink(chlLink, lLocalPeerID);

            //get the chain link index to start at 
            SortingValue svaLinkSortValue = chlLink.m_svaChainSortingValue;

            //check if link already exists in buffer
            if (ChainLinks.ContainsKey(svaLinkSortValue))
            {
                //dont double add chain link
                return;
            }

            //add chain links to buffer 
            ChainLinks.Add(svaLinkSortValue, chlLink);

            //merge messages into the message buffer 
            MergeChainLinkMessagesIntoBuffer(chlLink, gmbGlobalMessageBuffer, ndbNetworkDataBridge, out bool bIsMessageBufferDirty);
        }

        //set the first chain link and associated start state
        public void SetChainStartState(long lLocalPeerID, bool bActivePeer, int iMaxPlayerCount, GlobalMessagingState gmsStartState, ChainLink chlFirstLink, NetworkingDataBridge ndbNetworkingDataBridge)
        {
            m_gmsChainStartState = gmsStartState;
            m_chlChainBase = chlFirstLink;
            m_chlBestChainHead = chlFirstLink;
            chlFirstLink.m_bIsConnectedToBase = true;

            chlFirstLink.m_iChainLength = 1;
            chlFirstLink.m_chlParentChainLink = null;
            chlFirstLink.m_lChainMessageCount = (ulong)m_chlChainBase.m_pmnMessages.Count;

            SetupChannelAckArray(chlFirstLink, iMaxPlayerCount);

            //get state at end of first link
            chlFirstLink.CaluclateGlobalMessagingStateAtEndOflink(lLocalPeerID, bActivePeer, gmsStartState, VoteTimeout, MaxChannelCount, ndbNetworkingDataBridge);

            foreach (ChainLink chlLink in ChainLinks.Values)
            {
                if (chlLink != chlFirstLink)
                {
                    chlLink.m_gmsState = null;
                }
            }

            ReprocessAllChainLinks(lLocalPeerID, bActivePeer);

        }

        //reprocess all the chain links
        public void ReprocessAllChainLinks(long lLocalPeerID, bool bActivePeer)
        {
            for (int i = 0; i < ChainLinks.Count; i++)
            {
                ProceesChainLink(lLocalPeerID, bActivePeer, ChainLinks.Values[i]);
            }
        }

        //perform inital setup
        public ChainManager(int iMaxPlayerCount, NetworkConnectionSettings ncsSettings)
        {
            MaxChannelCount = iMaxPlayerCount;

            m_gmsChainStartState = new GlobalMessagingState(MaxChannelCount);

            MinCycleAge = ncsSettings.m_iMinCycleAge;

            MaxCycleAge = ncsSettings.m_iMaxCycleAge;

            MinChainLenght = ncsSettings.m_iMinChainLenght;

            TimeBetweenLinks = TimeSpan.FromSeconds(ncsSettings.m_fTimeBetweenLinks);

            VoteTimeout = TimeSpan.FromSeconds(ncsSettings.m_fVoteTimeout);
        }

        //get chain link for time
        public uint GetChainlinkCycleIndexForTime(DateTime dtmTargetTime, TimeSpan tspTimePerChain, DateTime dtmSystemStartTime)
        {
            //get time elapsed
            TimeSpan tspElapsedTime = dtmTargetTime - dtmSystemStartTime;

            return (uint)(tspElapsedTime.Ticks / tspTimePerChain.Ticks);
        }

        // returns the channel that will create the chain link for a given cycle index
        public int GetCreatorForLinkCycle(uint iChainLinkCycleIndex, int iChannelCount)
        {
            return (int)(iChainLinkCycleIndex % iChannelCount);
        }

        // the time of the most recent message that can be included in 
        // a chain link of cycle x
        public DateTime GetEndTimeForChainLink(uint iChainLinkCycleIndex, TimeSpan tspTimePerChain, DateTime dtmSystemStartTime)
        {
            return dtmSystemStartTime + TimeSpan.FromTicks(tspTimePerChain.Ticks * iChainLinkCycleIndex);
        }

        public void GetNextChainLinkForChannel(int iChannel, int iChannelCount, uint iCurrentChainLink, TimeSpan tspTimePerChain, DateTime dtmSystemStartTime, out DateTime dtmTimeOfChainLink, out uint iLinkCycleIndex)
        {
            //TODO: Replace with something that is not brute force
            //loop forwards to find next chain link that matches 
            for (uint i = 0; i < iChannelCount; i++)
            {
                iLinkCycleIndex = iCurrentChainLink + i + 1;

                //get channel for chain link
                int iCurrentChannel = GetCreatorForLinkCycle(iLinkCycleIndex, iChannelCount);

                if (iCurrentChannel == iChannel)
                {
                    dtmTimeOfChainLink = GetEndTimeForChainLink(iLinkCycleIndex, tspTimePerChain, dtmSystemStartTime);
                    return;
                }
            }

            //should not be here
            throw new Exception("Should have found chain link index for next channel link");
        }

        //excluding the current link index when was the last time this channel should have created a link
        public void GetPreviousChainLinkForChannel(int iChannel, int iChannelCount, uint iCurrentChainLink, TimeSpan tspTimePerChain, DateTime dtmSystemStartTime, out DateTime dtmTimeOfChainLink, out uint iLinkCycleIndex)
        {
            //TODO: Replace with something that is not brute force
            //loop forwards to find next chain link that matches 
            for (uint i = 0; i < iChannelCount; i++)
            {
                iLinkCycleIndex = iCurrentChainLink - (i + 1);

                //get channel for chain link
                int iCurrentChannel = GetCreatorForLinkCycle(iLinkCycleIndex, iChannelCount);

                if (iCurrentChannel == iChannel)
                {
                    dtmTimeOfChainLink = GetEndTimeForChainLink(iLinkCycleIndex, tspTimePerChain, dtmSystemStartTime);
                    return;
                }
            }

            //should not be here
            throw new Exception("Should have found chain link index for next channel link");
        }

        //put the messages in the chain link into the main buffer and replce any messages
        // in the chain with their duplicates in the buffer if they have already been added
        public void MergeChainLinkMessagesIntoBuffer(ChainLink chlChain, GlobalMessageBuffer gmbBuffer, NetworkingDataBridge ndbNetworkingDataBridge, out bool bDirtyMessageBufferState)
        {
            bDirtyMessageBufferState = false;

            //loop through all the messages 
            for (int i = 0; i < chlChain.m_pmnMessages.Count; i++)
            {
                //check if message has already been added
                if (gmbBuffer.UnConfirmedMessageBuffer.TryGetValue(chlChain.m_pmnMessages[i].m_svaMessageSortingValue, out PeerMessageNode pmsMessage))
                {
                    //replace chain message with the one thats already in the buffer
                    chlChain.m_pmnMessages[i] = pmsMessage;
                }
                else
                {
                    SortingValue svaChainHead;

                    if(m_chlBestChainHead != null && m_chlBestChainHead.m_gmsState != null)
                    {
                        svaChainHead = m_chlBestChainHead.m_gmsState.m_svaLastMessageSortValue;
                    }
                    else
                    {
                        svaChainHead = SortingValue.MinValue;
                    }

                    //add message to the buffer
                    gmbBuffer.AddMessageToBuffer(chlChain.m_pmnMessages[i], svaChainHead);

                    bDirtyMessageBufferState = true;
                }
            }
        }

        #region ChainBaseSelection

        //get best Chain Head
        //gets the best chain to extend with next link 
        //returns true if best chainlink head changed
        public ChainLink GetBestHeadChainLink(GlobalMessageBuffer gmbMessageBuffer)
        {
            int iBestChainLink = int.MinValue;
            ChainLink chlBestLink = null;

            for (int i = 0; i < ChainLinks.Values.Count; i++)
            {
                int iScore = ScoreChainLink(ChainLinks.Values[i], gmbMessageBuffer);

                if (iBestChainLink <= iScore)
                {
                    iBestChainLink = iScore;

                    chlBestLink = ChainLinks.Values[i];
                }
            }

            return chlBestLink;
        }

        public void OnBestHeadChange(ChainLink chlNewLink, long lLocalPeerID, bool bActivePeer, NetworkingDataBridge ndbNetworkingDataBridge, GlobalMessageBuffer gmbGlobalMessageBuffer)
        {
            List<ChainLink> chlNewBranchLinks = new List<ChainLink>();

            //reset the processed up to time in the message buffer
            gmbGlobalMessageBuffer.m_svaStateProcessedUpTo = chlNewLink.m_gmsState.m_svaLastMessageSortValue.NextSortValue();

            //get list of all the new chain links in the new branch to the new chian link head
            GetChainLinksFromSharedBase(chlNewLink, m_chlBestChainHead, ref chlNewBranchLinks);

            //apply messages from new branch to sim messages 
            ApplyChangesToSimMessageBuffer(lLocalPeerID, bActivePeer, chlNewBranchLinks, ndbNetworkingDataBridge);

            //set new best chain head
            m_chlBestChainHead = chlNewLink;


            //TODO::FIX THIS, the number of global messages does not match the number of messages in the sim buffer. global messages needs to be filtered to exclude voting join leave messages 
            //check if any inputs have snuck in to the sim buffer that are not in the active chain 
            //ChainBaseStateVerifier.ValidateSimMessaageBufferMatchesUpToLink(m_chlBestChainHead, ndbNetworkDataBridge, lLocalPeerID);


            //remove old links from the chain 
            UpdateBaseLink(lLocalPeerID, bActivePeer, gmbGlobalMessageBuffer, ndbNetworkingDataBridge);
        }

        //function to evaluate the best chain link
        protected int ScoreChainLink(ChainLink chlLink, GlobalMessageBuffer gmbMessageBuffer)
        {
            int iValue = 0;

            iValue += ScoreChainLinkLenght(chlLink);
            //iValue += ScoreChainAchnowledgement(chlLink);
            //iValue = Math.Max(0, iValue + ScoreChainLinkMessages(chlLink, gmbMessageBuffer));

            return iValue;
        }

        //function to score chain links on number of links and 
        //diversity of links
        protected int ScoreChainLinkLenght(ChainLink chlLink)
        {
            //check if connected to base
            if (chlLink.m_bIsConnectedToBase == false)
            {
                return 0;
            }

            int iRelativeLength = (int)Math.Min((chlLink.m_iChainLength - m_chlChainBase.m_iChainLength) + 1, int.MaxValue);

            return iRelativeLength;
        }

        //does the chain link hold all the messages recieved in the target time frame
        //does the chain include all messages sent by peer
        protected int ScoreChainLinkMessages(ChainLink chlLink, GlobalMessageBuffer gmbMessageBuffer)
        {
            //check if connected to base
            if (chlLink.m_bIsConnectedToBase == false)
            {
                return int.MinValue;
            }

            //check if there was any messages in the same time period that were missed 
            int iMissedMessages = 0;

            gmbMessageBuffer.GetMessageStartAndEndIndexesBetweenStates(m_chlChainBase.m_gmsState, chlLink.m_gmsState, out int iStartIndex, out int iEndIndex);

            //get messages in chain up to target link
            int iMessagesInChain = Mathf.Max(0, (int)(chlLink.m_lChainMessageCount - m_chlChainBase.m_lChainMessageCount));

            iMissedMessages =  Mathf.Max(0,(iEndIndex - iStartIndex) - iMessagesInChain);

            return -iMissedMessages;
        }

        //which links have been acknowledged by peers
        //how diverse is the chain acknowledgement
        protected int ScoreChainAchnowledgement(ChainLink chlLink)
        {
            //check that link is setup and connected
            if(chlLink.m_gmsState == null || chlLink.m_bIsConnectedToBase == false)
            {
                return -10;
            }

            int iActivePeersOnBranch = chlLink.m_gmsState.ActiveChannelCount();

            if(iActivePeersOnBranch == 0)
            {
                return - 10;
            }

            int iAcknowledgements = 0;
            for (int i = 0; i < chlLink.m_bIsChannelBranch.Count; i++)
            {
                if (chlLink.m_bIsChannelBranch[i] == true)
                {
                    iAcknowledgements++;
                }
            }

            int ackPercent = (iAcknowledgements * 10) / (iActivePeersOnBranch * 10);

            return ackPercent;
        }

        protected int ScoreDistanceBehindHead(ChainLink chlLink, uint iLatestCycleIndex)
        {
            if (chlLink.m_iLinkIndex > iLatestCycleIndex)
            {
                return 0;
            }

            return -(int)Math.Min(iLatestCycleIndex - chlLink.m_iLinkIndex, int.MaxValue);
        }

        //checks the link chain and removes links that are "aggread upon "
        protected void UpdateBaseLink(long lLocalPeerID, bool bActivePeer, GlobalMessageBuffer gmbGlobalMessageBuffer, NetworkingDataBridge ndbNetworkDataBridge)
        {
            ChainLink chlNewBase = m_chlBestChainHead.m_chlParentChainLink;

            //check if parent is base
            if (chlNewBase == m_chlChainBase)
            {
                return;
            }

            //get the first new base in chain history
            for (int i = 0; i < ChainLinks.Count; i++)
            {
                if (chlNewBase == null)
                {
                    break;
                }

                //check if valid base 
                if (IsValidBaseLink(chlNewBase, m_chlBestChainHead))
                {
                    break;
                }

                //check if forces base 
                //if (IsForcedBase(chlNewBase, m_chlBestChainHead))
                //{
                //    break;
                //}

                //move to previous chain link
                chlNewBase = chlNewBase.m_chlParentChainLink;

            }

            //check that this is a new base
            if (chlNewBase == null || chlNewBase == m_chlChainBase)
            {
                return;
            }

            //perform rebase 
            DoRebase(lLocalPeerID, bActivePeer, chlNewBase, gmbGlobalMessageBuffer, ndbNetworkDataBridge);
        }

        //check if a chain link is valid enough to turn into the base link
        //this is done by checking if a link has enough parents, is old enough and enough peers aggree on it
        protected bool IsValidBaseLink(ChainLink chlBaseCandidate, ChainLink chlCurrentHead)
        {
            //check if it has enough acks 
            //TODO:: Move this to the channel link to match active channle count
            float fAcks = 0;
            for (int i = 0; i < chlBaseCandidate.m_bIsChannelBranch.Count; i++)
            {
                if (chlBaseCandidate.m_bIsChannelBranch[i])
                {
                    fAcks++;
                }
            }

            if (fAcks / (float)chlBaseCandidate.m_gmsState.ActiveChannelCount() < PercentOfAcknowledgementsToRebase)
            {
                return false;
            }

            //check if enough links have been attached
            if (chlCurrentHead.m_iChainLength - chlBaseCandidate.m_iChainLength < MinChainLenght)
            {
                return false;
            }

            //check if enough cycles have passed 
            if (chlCurrentHead.m_iLinkIndex - chlBaseCandidate.m_iLinkIndex < MinCycleAge)
            {
                return false;
            }

            return true;
        }

        //check if parent link is too old and has to be removed
        protected bool IsForcedBase(ChainLink chlBaseCandidate, ChainLink chlCurrentHead)
        {
            //check if has parent / is base
            if (chlBaseCandidate.m_chlParentChainLink == null)
            {
                return true;
            }

            if (chlCurrentHead.m_iLinkIndex - chlBaseCandidate.m_chlParentChainLink.m_iLinkIndex > MaxCycleAge)
            {
                return true;
            }

            return false;
        }

        protected void DoRebase(long lLocalPeerID, bool bActivePeer, ChainLink chlNewBase,GlobalMessageBuffer gmbMessageBuffer, NetworkingDataBridge ndbNetworkDataBridge)
        {
            //debug testing

            //TODO::FIX THIS, the number of global messages does not match the number of messages in the sim buffer. global messages needs to be filtered to exclude voting join leave messages 
            //check if any inputs have snuck in to the sim buffer that are not in the active chain 
            //ChainBaseStateVerifier.ValidateSimMessaageBufferMatchesUpToLink(m_chlBestChainHead, ndbNetworkDataBridge, lLocalPeerID);

            //check if base states match 
            ChainBaseStateVerifier.RegisterAllStatesUpToLink(chlNewBase, lLocalPeerID);

            //set base state
            m_gmsChainStartState.ResetToState(chlNewBase.m_chlParentChainLink.m_gmsState);

            //set new base
            m_chlChainBase = chlNewBase;

            //remove outdated chain links
            //TODO: do this with a better algorithm that doesn't produce piles of garbage
            while (ChainLinks.Count > 0 && ChainLinks.Values[0] != chlNewBase)
            {
                ChainLinks.RemoveAt(0);
            }

            //remove refference to old links
            m_chlChainBase.m_chlParentChainLink = null;

            //remove old messages
            gmbMessageBuffer.RemoveItemsUpTo(m_gmsChainStartState.m_svaLastMessageSortValue);

            if(m_chlChainBase.m_pmnMessages.Count != 0 && m_chlChainBase.m_gmsState.m_svaLastMessageSortValue.CompareTo(m_chlChainBase.m_pmnMessages[m_chlChainBase.m_pmnMessages.Count -1].m_svaMessageSortingValue) != 0 )
            {
                Debug.LogError("Chain state sort value and newest chain message dont have the same sort value");
            }

            //update the comfirmed message time 
            ndbNetworkDataBridge.SetValidatedMesageBaseTime(m_chlChainBase.m_gmsState.m_svaLastMessageSortValue);

            //recalculate if linked to base 
            foreach (ChainLink chlLink in ChainLinks.Values)
            {
                if(chlLink == m_chlChainBase)
                {
                    continue;
                }

                if(chlLink.m_chlParentChainLink != null )
                {
                    //check if link is too old
                    if(chlLink.m_chlParentChainLink.m_svaChainSortingValue.CompareTo(m_chlChainBase.m_svaChainSortingValue) < 0)
                    {
                        chlLink.m_chlParentChainLink = null;
                        chlLink.m_bIsConnectedToBase = false;
                        continue;
                    }
                    else
                    {
                        chlLink.m_bIsConnectedToBase = chlLink.m_chlParentChainLink.m_bIsConnectedToBase;
                    }
                }
                else
                {
                    chlLink.m_bIsConnectedToBase = false;
                }
               
            }

            ReprocessAllChainLinks(lLocalPeerID, bActivePeer);

        }
        #endregion

    }
}