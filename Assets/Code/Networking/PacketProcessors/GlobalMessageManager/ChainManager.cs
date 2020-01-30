using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Networking
{
    /// <summary>
    /// this class manages a list of chain links used by the global state machiene
    /// </summary>
    public class ChainManager
    {
        public enum State
        {
            Connecting, //collecting links from peers and waiting for enough information to pick a base link
            Ready, //running normally 
            Broken //something is wrong, peer has desynced 
        }

        #region LinkRebasing
        //the percent of active peers that need to acknowledge a link to make it 
        //a valid base link candidate
        public static float PercentOfAcknowledgementsToRebase { get; } = 0.50f;

        public static uint MinCycleAge { get; } = 10;

        public static uint MaxCycleAge { get; } = 100;

        public static int MinChainLenght { get; } = 5;
       
        #endregion


        #region LinkTiming
        public static TimeSpan TimeBetweenLinks { get; } = TimeSpan.FromSeconds(0.1f);

        //each chain is based off the start of the year
        public static DateTime GetChainBaseTime(DateTime dtmCurrentNetworkTime)
        {
            int iYear = dtmCurrentNetworkTime.Year;
            return new DateTime(iYear, 1, 1);
        }
        #endregion


        //the stage the state machine is in
        public State m_staState;

        //buffer of all recieved chain links
        public SortedList<SortingValue,ChainLink> ChainLinks { get; } = new SortedList<SortingValue, ChainLink>();

        //the starting connection state for the chain
        public GlobalMessagingState m_gmsChainStartState;

        //the oldest link in the validated chain
        public ChainLink m_chlChainBase;

        //the highest ranked chain 
        public ChainLink m_chlBestChainHead;

        //max number of peers in the global message system at once 
        public int m_iChannelCount;

        public void SetStartState(long lFirstPeerID)
        {
            //set the state and the first peer
            m_gmsChainStartState.AssignFirstPeer(lFirstPeerID);
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
            for(int i = 0; i< m_iChannelCount; i++)
            {
                chkChainLink.m_bIsChannelBranch.Add(false);
            }

            chkChainLink.m_bIsChannelBranch[0] = true;

            m_staState = State.Ready;
        }

        public void AddChainLink(long lLocalPeerID, ChainLink chlLink, GlobalMessageKeyManager gkmKeyManager,GlobalMessageBuffer gmbGlobalMessageBuffer)
        {
            //check that chain link is valid 
            ValidateChainSource(chlLink, gkmKeyManager);
                       
            //get the chain link index to start at 
            SortingValue svaLinkSortValue = chlLink.m_svaChainSortingValue;

            //check if link already exists in buffer
            if(ChainLinks.ContainsKey(svaLinkSortValue))
            {
                //dont double add chain link
                return;
            }

            //add chain links to buffer 
            ChainLinks.Add(svaLinkSortValue, chlLink);

            //merge messages into the message buffer 
            MergeChainLinkMessagesIntoBuffer(chlLink, gmbGlobalMessageBuffer);
                       
            //recalculate chain values
            ReprocessAllChainLinks(lLocalPeerID);

            //store the old chain head
            ChainLink chlOldHead = m_chlBestChainHead;

            //update the best chain
            m_chlBestChainHead = FindBestHeadChainLink();

            //check that chain head changed 
            if (m_chlBestChainHead != chlOldHead)
            {
                //remove old items from the chain 
                UpdateBaseLink();
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
                if ( chlLink.m_lLinkPayloadHash == lHash)
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
            if(chlAcknowledgedLink.m_bIsChannelBranch[iChannelIndex])
            {
                return;
            }

            //clear out existing acknowledements 
            //and acknowledge new branch
            for(int i = ChainLinks.Count -1; i > -1; i-- )
            {
                if(ChainLinks.Values[i] == chlAcknowledgedLink && chlAcknowledgedLink != null)
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
            if(chlLink == m_chlChainBase)
            {
                return;
            }

            //check if acknowledgements need setting up
            if (chlLink.m_bIsChannelBranch == null)
            {
                //create ack array
                chlLink.m_bIsChannelBranch = new List<bool>(m_iChannelCount);

                for (int i = 0; i < m_iChannelCount; i++)
                {
                    chlLink.m_bIsChannelBranch.Add(false);
                }
            }

            //check if link needs to find parent and is not base which will have no parent
            if (chlLink.m_chlParentChainLink == null)
            {
                //get the chain link index to start at 
                SortingValue svaLinkSortValue = chlLink.m_svaChainSortingValue;

                //loop through all items before chain link and try and get parent link
                int iIndex = ChainLinks.IndexOfKey(svaLinkSortValue);

                //start search before index of link just added
                iIndex--;

                //get parent of chain link
                ChainLink chlParentLink = FindLink(iIndex, chlLink.m_lPreviousLinkHash);

                //check if valid parent that was created before link was created
                if(chlParentLink != null && chlParentLink.m_iLinkIndex < chlLink.m_iLinkIndex)
                {
                    //set linkage in chain 
                    chlLink.m_chlParentChainLink = chlParentLink;
                }             

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
            if(chlLink.m_chlParentChainLink.m_bIsConnectedToBase == true || m_chlChainBase == chlLink.m_chlParentChainLink)
            {
                chlLink.m_bIsConnectedToBase = true;
            }
            else
            {
                chlLink.m_bIsConnectedToBase = false;
            }

            //check if state needs to be calculated 
            if(chlLink.m_bIsConnectedToBase && chlLink.m_gmsState == null)
            {
                chlLink.CaluclateGlobalMessagingStateAtEndOflink(lLocalPeerID, chlLink.m_chlParentChainLink.m_gmsState);
            }

            //TODO: do this in a more efficient way that is not redone for every link when a new link is 
            //accepted 
            //set peer as acknowledging chain 
            if (m_gmsChainStartState.TryGetIndexForPeer(chlLink.m_lPeerID, out int iChannelIndex))
            {
                //set acknowledgement for peer 
                SetChannelAcknowledgements(iChannelIndex, chlLink);
            }

            //calculate chain length
            chlLink.m_lChainMessageCount = chlLink.m_chlParentChainLink.m_lChainMessageCount + (ulong)chlLink.m_pmnMessages.Count;
            chlLink.m_iChainLength = chlLink.m_chlParentChainLink.m_iChainLength + 1;

            Debug.Log($"Processed new chain link with length {chlLink.m_iChainLength}");
        }

        //reprocess all the chain links
        public void ReprocessAllChainLinks(long lLocalPeerID)
        {
            for(int i = 0; i <ChainLinks.Count; i++)
            {
                ProceesChainLink(lLocalPeerID,ChainLinks.Values[i]);
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
        public void MergeChainLinkMessagesIntoBuffer(ChainLink chlChain, GlobalMessageBuffer gmbBuffer)
        {
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
                }
            }
        }

        //get best Chain Head
        //gets the best chain to extend with next link 
        protected ChainLink FindBestHeadChainLink()
        {
            int iBestChainLink = 0;
            ChainLink chlBestLink = null;

            for(int i = 0; i < ChainLinks.Values.Count; i++)
            {
                int iScore = ScoreChainLink(ChainLinks.Values[i]);

                if(iBestChainLink <= iScore)
                {
                    iBestChainLink = iScore;

                    chlBestLink = ChainLinks.Values[i];
                }
            }

            return chlBestLink;
        }

        //function to evaluate the best chain link
        protected int ScoreChainLink(ChainLink chlLink)
        {
            int iValue = 0;

            iValue += ScoreChainLinkLenght(chlLink);
            iValue += ScoreChainAchnowledgement(chlLink);
            iValue = Math.Max(0, iValue + ScoreChainLinkMessages(chlLink));

            return iValue;            
        }
        
        //function to score chain links on number of links and 
        //diversity of links
        protected int ScoreChainLinkLenght(ChainLink chlLink)
        {
            //check if connected to base
            if(chlLink.m_bIsConnectedToBase == false)
            {
                return 0;
            }

            int iRelativeLength = (int)Math.Min(chlLink.m_iChainLength - m_chlChainBase.m_iChainLength,int.MaxValue);

            return iRelativeLength;
        }
        
        //does the chain link hold all the messages recieved in the target time frame
        //does the chain include all messages sent by peer
        protected int ScoreChainLinkMessages(ChainLink chlLink)
        {
            //check if connected to base
            if (chlLink.m_bIsConnectedToBase == false)
            {
                return 0;
            }

            //compare the number of messages in chain vs total number of messages over same time period 
            int iMessageLenghDifference = (int) Math.Min(chlLink.m_lChainMessageCount - (m_chlChainBase.m_lChainMessageCount - (uint)m_chlChainBase.m_pmnMessages.Count),int.MaxValue);

            return iMessageLenghDifference;
        }

        //which links have been acknowledged by peers
        //how diverse is the chain acknowledgement
        protected int ScoreChainAchnowledgement(ChainLink chlLink)
        {
            int iAcknowledgements = 0;
            for(int i = 0; i < chlLink.m_bIsChannelBranch.Count; i++)
            {
                if (chlLink.m_bIsChannelBranch[i] == true)
                {
                    iAcknowledgements++;
                }
            }

            return iAcknowledgements;
        }

        protected int ScoreDistanceBehindHead(ChainLink chlLink, uint iLatestCycleIndex)
        {
            if(chlLink.m_iLinkIndex > iLatestCycleIndex)
            {
                return 0;
            }

            return -(int)Math.Min(iLatestCycleIndex - chlLink.m_iLinkIndex, int.MaxValue);
        }
        
        //checks the link chain and removes links that are "aggread upon "
        protected void UpdateBaseLink()
        {            
            

            ChainLink chlNewBase = m_chlBestChainHead.m_chlParentChainLink;

            //check if parent is base
            if (chlNewBase == m_chlChainBase)
            {
                return;
            }

            //get the first new base in chain history
            for ( int i = 0; i < ChainLinks.Count; i++)
            {
                if(chlNewBase == null)
                {
                    break;
                }

                //check if valid base 
                if(IsValidBaseLink(chlNewBase, m_chlBestChainHead))
                {
                    break;
                }

                //check if forces base 
                if(IsForcedBase(chlNewBase,m_chlBestChainHead))
                {
                    break;
                }

                //move to previous chain link
                chlNewBase = chlNewBase.m_chlParentChainLink;

            }

            //check that this is a new base
            if(chlNewBase == null || chlNewBase == m_chlChainBase)
            {
                return;
            }

            //perform rebase 
            DoRebase(chlNewBase);
        }

        protected bool IsValidBaseLink(ChainLink chlBaseCandidate, ChainLink chlCurrentHead)
        {
            //check if it has enough acks 
            //TODO:: Move this to the channel link to match active channle count
            float fAcks = 0; 
            for(int i = 0; i < chlBaseCandidate.m_bIsChannelBranch.Count; i++)
            {
                if(chlBaseCandidate.m_bIsChannelBranch[i])
                {
                    fAcks++;
                }
            }

            if( (float)fAcks / chlBaseCandidate.m_gmsState.ActiveChannelCount() <  PercentOfAcknowledgementsToRebase)
            {
                return false;
            }

            //check if enough links have been attached
            if(chlCurrentHead.m_iChainLength - chlBaseCandidate.m_iChainLength < MinChainLenght)
            {
                return false;
            }

            //check if enough cycles have passed 
            if(chlCurrentHead.m_iLinkIndex - chlBaseCandidate.m_iLinkIndex < MinCycleAge)
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

            if(chlCurrentHead.m_iLinkIndex - chlBaseCandidate.m_chlParentChainLink.m_iLinkIndex > MaxCycleAge)
            {
                return true;
            }

            return false;
        }

        protected void DoRebase(ChainLink chlNewBase)
        {
            //set base state
            m_gmsChainStartState.ResetToState(chlNewBase.m_chlParentChainLink.m_gmsState);
            
            //set new base
            m_chlChainBase = chlNewBase;

            //remove outdated chain links
            //TODO: do this with a better algorithm that doesn't produce piles of garbage
            while(ChainLinks.Count > 0 && ChainLinks.Values[0] != chlNewBase)
            {
                ChainLinks.RemoveAt(0);
            }

            //TODO:: remove old messages ?
        }
    }
}