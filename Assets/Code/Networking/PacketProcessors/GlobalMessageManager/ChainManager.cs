using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

        //the stage the state machine is in
        public State m_staState;

        //buffer of all recieved chain links
        public SortedSet<ChainLink> ChainLinks { get; } = new SortedSet<ChainLink>();

        //max number of players in the game at once 
        public int m_iChannelCount;

        //the coprime offset used to do a full cycle random
        public int m_iFullCycleRandomIncrement;

        public void AddChainLink(ChainLink chlLink, GlobalMessageKeyManager gkmKeyManager)
        {
            //check that chain link is valid 
            ValidateChainSource(chlLink, gkmKeyManager);

            //add chain links to buffer 
            ChainLinks.Enqueue(chlLink);

            //search through buffer to get child link
        }

        public bool ValidateChainSource(ChainLink chlLink, GlobalMessageKeyManager gkmKeyManager)
        {
            //perform black magic 
            return true;
        }

        //build a chain
        public ChainLink BuildChain()
        {
            return null;
        }

        //function to evaluate the best chain link
        public void ScoreChainLinks()
        {

        }

        //function to score chain links on number of links and 
        //diversity of links
        protected void ScoreChainLinkLenght()
        {

        }
        
        //does the chain link hold all the messages recieved in the target time frame
        //does the chain include all messages sent by peer
        protected void ScoreChainLinkMessages()
        {

        }

        //which links have been acknowledged by peers
        //how diverse is the chain acknowledgement
        protected void ScoreChainAchnowledgement()
        {

        }

        //search through chain links and return highest score chain link to use as base for next link
        protected ChainLink GetBestChainToExtend()
        {
            return null;
        }

        //get chain link for time
        protected int ChainlinkCycleIndexForTime(DateTime dtmTargetTime, TimeSpan tspTimePerChain, DateTime dtmSystemStartTime)
        {
            //get time elapsed
            TimeSpan tspElapsedTime = dtmTargetTime - dtmSystemStartTime;

            return (int)(tspElapsedTime.Ticks / tspTimePerChain.Ticks);
        }

        // returns the channel that will create the chain link for a given cycle index
        protected int ChainLinkCreator(int iChainLinkCycleIndex, int iChannelCount)
        {
            long lIncrementScaledIndex = ((long)iChainLinkCycleIndex) * ((long)m_iFullCycleRandomIncrement;

            return (int)(lIncrementScaledIndex % m_iChannelCount);
        }

        // the time of the most recent message that can be included in 
        // a chain link of cycle x
        protected DateTime ChainLinkEndTime(int iChainLinkCycleIndex, TimeSpan tspTimePerChain, DateTime dtmSystemStartTime)
        {
            return dtmSystemStartTime + TimeSpan.FromTicks(tspTimePerChain.Ticks * iChainLinkCycleIndex);
        }
    }
}