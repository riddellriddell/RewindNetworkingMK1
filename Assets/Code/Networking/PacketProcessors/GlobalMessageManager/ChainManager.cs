﻿using System;
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
        public static float PercentOfAcknowledgementsToRebase { get; } = 0.5f;

        public static uint MinCycleAge { get; } = 10;

        public static uint MaxCycleAge { get; } = 100;

        public static int MinChainLenght { get; } = 10;

        #endregion

        #region LinkTiming
        public static TimeSpan TimeBetweenLinks { get; } = TimeSpan.FromSeconds(1f);

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

        //max number of peers in the global message system at once 
        public int m_iChannelCount;

        public void SetStartState(long lFirstPeerID, int iMaxPeerCount)
        {
            m_gmsChainStartState = new GlobalMessagingState(iMaxPeerCount, lFirstPeerID);
        }

        public void AddFirstChainLink(long lLocalPeerID, ChainLink chkChainLink)
        {
            //add chain links to buffer 
            ChainLinks.Add(chkChainLink.m_svaChainSortingValue, chkChainLink);

            //set as base chain link
            m_chlChainBase = chkChainLink;

            m_chlBestChainHead = chkChainLink;

            chkChainLink.m_bIsConnectedToBase = true;

            //calculate chain state 
            chkChainLink.CaluclateGlobalMessagingStateAtEndOflink(lLocalPeerID, m_gmsChainStartState);

            //setup acknoledgements 
            chkChainLink.m_bIsChannelBranch = new List<bool>(m_iChannelCount);

            //set acknowledgement of first link
            //this assumes the inital peer is at position 0;
            for (int i = 0; i < m_iChannelCount; i++)
            {
                chkChainLink.m_bIsChannelBranch.Add(false);
            }

            chkChainLink.m_bIsChannelBranch[0] = true;
        }

        public void AddChainLink(long lLocalPeerID, ChainLink chlLink, GlobalMessageKeyManager gkmKeyManager, GlobalMessageBuffer gmbGlobalMessageBuffer, out bool bDirtyUnconfirmedMessageBufferState)
        {
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
            //bIsDirty is true if new messages were added
            MergeChainLinkMessagesIntoBuffer(chlLink, gmbGlobalMessageBuffer, out bool bIsDirty);

            //recalculate chain values
            ReprocessAllChainLinks(lLocalPeerID);

            //store the old chain head
            ChainLink chlOldHead = m_chlBestChainHead;

            //update the best chain
            bool bDidHeadChange = UpdateBestHeadChainLink(gmbGlobalMessageBuffer);

            //check that chain head changed 
            if (bDidHeadChange)
            {
                //remove old items from the chain 
                UpdateBaseLink(lLocalPeerID, gmbGlobalMessageBuffer);

                bDirtyUnconfirmedMessageBufferState = true;
            }

            bDirtyUnconfirmedMessageBufferState = bIsDirty;
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
                if (ChainLinks.Values[i].m_bIsChannelBranch == null || ChainLinks.Values[i].m_bIsChannelBranch.Count != m_iChannelCount)
                {
                    SetupChannelAckArray(ChainLinks.Values[i], m_iChannelCount);
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
        public void ProceesChainLink(long lLocalPeerID, ChainLink chlLink)
        {
            //skip if base link as it should have been processed already 
            if (chlLink == m_chlChainBase)
            {
                return;
            }

            //check if acknowledgements need setting up
            if (chlLink.m_bIsChannelBranch == null || chlLink.m_bIsChannelBranch.Count != m_iChannelCount)
            {
                SetupChannelAckArray(chlLink, m_iChannelCount);
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
                chlLink.CaluclateGlobalMessagingStateAtEndOflink(lLocalPeerID, chlLink.m_chlParentChainLink.m_gmsState);
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
        public void EvaluateStartCandidates(long lLocalPeer)
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
                    ChainLinks.Values[i].CaluclateGlobalMessagingStateAtEndOflink(lLocalPeer, gmsPreviousLinkEndState);
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
        public void AddChainLinkPreConnection(ChainLink chlLink, GlobalMessageBuffer gmbGlobalMessageBuffer)
        {
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
            MergeChainLinkMessagesIntoBuffer(chlLink, gmbGlobalMessageBuffer, out bool bIsMessageBufferDirty);
        }

        //set the first chain link and associated start state
        public void SetChainStartState(long lLocalPeerID,int iMaxPlayerCount, GlobalMessagingState gmsStartState, ChainLink chlFirstLink)
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
            chlFirstLink.CaluclateGlobalMessagingStateAtEndOflink(lLocalPeerID, gmsStartState);

            foreach (ChainLink chlLink in ChainLinks.Values)
            {
                if (chlLink != chlFirstLink)
                {
                    chlLink.m_gmsState = null;
                }
            }

            ReprocessAllChainLinks(lLocalPeerID);

        }

        //reprocess all the chain links
        public void ReprocessAllChainLinks(long lLocalPeerID)
        {
            for (int i = 0; i < ChainLinks.Count; i++)
            {
                ProceesChainLink(lLocalPeerID, ChainLinks.Values[i]);
            }
        }

        //perform inital setup
        public ChainManager(int iMaxPlayerCount)
        {
            m_iChannelCount = iMaxPlayerCount;
            m_gmsChainStartState = new GlobalMessagingState(m_iChannelCount);
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

        //build a chain
        public ChainLink BuildChain()
        {
            return null;
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
        public void MergeChainLinkMessagesIntoBuffer(ChainLink chlChain, GlobalMessageBuffer gmbBuffer, out bool bDirtyMessageBufferState)
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
                    //add message to the buffer
                    gmbBuffer.UnConfirmedMessageBuffer.Add(chlChain.m_pmnMessages[i].m_svaMessageSortingValue, chlChain.m_pmnMessages[i]);

                    bDirtyMessageBufferState = true;
                }
            }
        }

        #region ChainBaseSelection

        //get best Chain Head
        //gets the best chain to extend with next link 
        //returns true if best chainlink head changed
        public bool UpdateBestHeadChainLink(GlobalMessageBuffer gmbMessageBuffer)
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

            if(chlBestLink != m_chlBestChainHead)
            {
                m_chlBestChainHead = chlBestLink;

                return true;
            }

            return false;
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

            int iRelativeLength = (int)Math.Min(chlLink.m_iChainLength - m_chlChainBase.m_iChainLength, int.MaxValue);

            return iRelativeLength;
        }

        //does the chain link hold all the messages recieved in the target time frame
        //does the chain include all messages sent by peer
        protected int ScoreChainLinkMessages(ChainLink chlLink, GlobalMessageBuffer gmbMessageBuffer)
        {
            //check if connected to base
            if (chlLink.m_bIsConnectedToBase == false)
            {
                return 0;
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
        protected void UpdateBaseLink(long lLocalPeerID, GlobalMessageBuffer gmbGlobalMessageBuffer)
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
            DoRebase(lLocalPeerID, chlNewBase, gmbGlobalMessageBuffer);
        }

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

        protected void DoRebase(long lLocalPeerID, ChainLink chlNewBase,GlobalMessageBuffer gmbMessageBuffer)
        {
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

            //recalculate if linked to base 
            foreach(ChainLink chlLink in ChainLinks.Values)
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

            ReprocessAllChainLinks(lLocalPeerID);

        }
        #endregion

    }
}