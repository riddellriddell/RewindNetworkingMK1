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

        public static TimeSpan TimeBetweenLinks { get; } = TimeSpan.FromSeconds(0.1f);

        //each chain is based off the start of the year
        public static DateTime GetChainBaseTime(DateTime dtmCurrentNetworkTime)
        {
            return new DateTime(dtmCurrentNetworkTime.Year, 0, 0);
        }

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

        //the time the global message system started
        public DateTime m_dtmSystemStartTime;

        //the coprime offset used to do a full cycle random
        public int m_iFullCycleRandomIncrement;

        public void SetStartState(long lFirstPeerID)
        {
            //set the state and the first peer
            m_gmsChainStartState = new GlobalMessagingState(m_iChannelCount,lFirstPeerID);
        }

        public void AddFirstChainLink(ChainLink chkChainLink)
        {
            //add chain links to buffer 
            ChainLinks.Add(chkChainLink.m_svaChainSortingValue, chkChainLink);

            //set as base chain link
            m_chlChainBase = chkChainLink;

            m_chlBestChainHead = chkChainLink;         

            ReprocessAllChainLinks();
        }

        public void AddChainLink(ChainLink chlLink, GlobalMessageKeyManager gkmKeyManager,GlobalMessageBuffer gmbGlobalMessageBuffer)
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
            ReprocessAllChainLinks();

            //update the best chain
            m_chlBestChainHead = FindBestHeadChainLink();
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
            //clear out existing acknowledements 
            //and acknowledge new branch
            for(int i = ChainLinks.Count -1; i > -1; i-- )
            {
                if(ChainLinks.Values[i] == chlAcknowledgedLink)
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
        public void ProceesChainLink(ChainLink chlLink)
        {
            //check if link needs to find parent
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

                //set linkage in chain 
                chlLink.m_chlParentChainLink = chlParentLink;

                //check if parent link was found 
                if (chlParentLink == null)
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

            //calculate chain length
            chlLink.m_lChainMessageCount = chlLink.m_chlParentChainLink.m_lChainMessageCount + (ulong)chlLink.m_pmnMessages.Count;
            chlLink.m_iChainLength = chlLink.m_chlParentChainLink.m_iChainLength + 1;
        }

        //reprocess all the chain links
        public void ReprocessAllChainLinks()
        {
            for(int i = 0; i <ChainLinks.Count; i++)
            {
                ProceesChainLink(ChainLinks.Values[i]);
            }
        }
        //perform inital setup
        public ChainManager(int iMaxPlayerCount)
        {
            m_iChannelCount = iMaxPlayerCount;
        }

        public void SetRandomFullCycleIncrement()
        {
            List<int> iIncrementOptions = HelperFunctions.CoPrimes(m_iChannelCount, m_iChannelCount);

            m_iFullCycleRandomIncrement = iIncrementOptions[Random.Range(0, iIncrementOptions.Count)];
        }

        //given a start time get the last chain cycle this channel could have created a link 
        public uint GetLastChainLinkForChannel(int iChannel, int iChannelCount, DateTime dtmTargetTime, TimeSpan tspTimePerChain, DateTime dtmSystemStartTime)
        {
            //get the channel index for target time
            uint iCycleIndex = ChainlinkCycleIndexForTime(dtmTargetTime, tspTimePerChain, dtmSystemStartTime);
            
            //check for underflow
            if(iCycleIndex < iChannelCount)
            {
                iCycleIndex += (uint)iChannelCount;
            }

            for (uint i = 0; i < iChannelCount; i++)
            {
                int iChannelForCycle = ChainLinkCreator(iCycleIndex - i, iChannelCount);

                if (iChannelForCycle == iChannel)
                {
                    return iCycleIndex - i;
                }
            }

            //shound not have gotten here
            throw new Exception("Should have found a cycle for target channel before reaching this point in code");
        }
        
        //build a chain
        public ChainLink BuildChain()
        {
            return null;
        }

        //get the next time a peer should build a chain link
        public void TimeAndIndexOfNextLinkFromChannel(int iChannel, int iChannelCount, DateTime dtmCurrentTime, TimeSpan tspTimePerChain, DateTime dtmSystemStartTime, out DateTime dtmTimeOfChainLink, out uint iLinkCycleIndex)
        {
            //get current cycle index
            uint iCycleIndex = ChainlinkCycleIndexForTime(dtmCurrentTime, tspTimePerChain, dtmSystemStartTime);

            //loop through random number generator to get next time
            for(uint i = 0; i < iChannelCount; i++)
            {
                int iLinkCreatorChannelForIndex = ChainLinkCreator(iCycleIndex + i, iChannelCount);

                if (iLinkCreatorChannelForIndex == iChannel)
                {
                    iLinkCycleIndex = iCycleIndex + i;
                    dtmTimeOfChainLink = ChainLinkEndTime(iLinkCycleIndex, tspTimePerChain, dtmSystemStartTime);

                    return;
                }
            }

            //should not be here the only way this happens if the passed channel 
            //is not in the channel count range
            dtmTimeOfChainLink = DateTime.MinValue;
            iLinkCycleIndex = 0;
        }

        //put the messages in the chain link into the main buffer and replce any messages
        // in the chain with their duplicates in the buffer if they have already been added
        public void MergeChainLinkMessagesIntoBuffer(ChainLink chlChain, GlobalMessageBuffer gmbBuffer)
        {
            //loop through all the messages 
            for (int i = 0; i < chlChain.m_pmnMessages.Count; i++)
            {
                //check if message has already been added
                if (gmbBuffer.UnConfirmedMessageBuffer.TryGetValue(chlChain.m_pmnMessages[i].SortingValue, out ISortedMessage smsMessage))
                {
                    //replace chain message with the one thats already in the buffer
                    chlChain.m_pmnMessages[i] = smsMessage as IPeerMessageNode;
                }
                else
                {
                    //add message to the buffer
                    gmbBuffer.UnConfirmedMessageBuffer.Add(chlChain.m_pmnMessages[i].SortingValue, chlChain.m_pmnMessages[i]);
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

                if(iScore <= iBestChainLink)
                {
                    iScore = iBestChainLink;

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

        //get chain link for time
        protected uint ChainlinkCycleIndexForTime(DateTime dtmTargetTime, TimeSpan tspTimePerChain, DateTime dtmSystemStartTime)
        {
            //get time elapsed
            TimeSpan tspElapsedTime = dtmTargetTime - dtmSystemStartTime;

            return (uint)(tspElapsedTime.Ticks / tspTimePerChain.Ticks);
        }

        // returns the channel that will create the chain link for a given cycle index
        protected int ChainLinkCreator(uint iChainLinkCycleIndex, int iChannelCount)
        {
            long lIncrementScaledIndex = ((long)iChainLinkCycleIndex) * ((long)m_iFullCycleRandomIncrement);

            return (int)(lIncrementScaledIndex % m_iChannelCount);
        }

        // the time of the most recent message that can be included in 
        // a chain link of cycle x
        protected DateTime ChainLinkEndTime(uint iChainLinkCycleIndex, TimeSpan tspTimePerChain, DateTime dtmSystemStartTime)
        {
            return dtmSystemStartTime + TimeSpan.FromTicks(tspTimePerChain.Ticks * iChainLinkCycleIndex);
        }
    }
}