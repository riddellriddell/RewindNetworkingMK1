using Networking;
using SharedTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Code.Managers.Testing
{
    //the point of this class is to track all the inputs created and check if peers are correctly applying them to the correct tick
    class TestInputUsage
    {
        class InputTracker
        {
            //input hash to check if 2 inputs are being stored at the same sort value in the buffer
            public long m_lHashOfInput;

            public long m_lCreatorPeerId;

            public bool m_bIsConnectionChangeMessage;

            public int m_iNumberOfPeersThatHaveUsedThisInput;

            /// <summary>
            /// tracker for which peers used this input and what tick the input was applied to
            /// </summary>
            public List<Tuple<uint, List<long>>> m_lstTickInputConsumedAndPeersWhoUsedit;
        }

        //input at index
        private static SortedRandomAccessQueue<SortingValue, InputTracker> m_srqInputTracker = new SortedRandomAccessQueue<SortingValue, InputTracker>();

        //the first tick processed by an agent
        private static Dictionary<long, uint> m_dicFirstTickByAgent = new Dictionary<long, uint>();

        //the oldest tick that has not been finalized
        private static Dictionary<long,uint> m_dicOldestUnvalidatedTickByAgent = new Dictionary<long, uint>();

        /// <summary>
        /// when a peer consumes an input they should call this and it will mark that 
        /// </summary>
        /// <param name="svaTimeOfInput"></param>
        /// <param name="lHashOfInput"></param>
        /// <param name="iTickInputUsed"></param>
        /// <param name="lPeerUsingInput"></param>
        /// <param name="lPeerCreatingInput"></param>
        public static void RegisterInputUsage(SortingValue svaTimeOfInput, long lHashOfInput, uint iTickInputUsed, long lPeerUsingInput, long lPeerWhoCreatedInput, bool bIsConnectionChangeMessage)
        {
            //check if input has already been registered
            if (m_srqInputTracker.TryGetIndexOf(svaTimeOfInput, out int iIndex))
            {
                InputTracker iptInputTracker = m_srqInputTracker.GetValueAtIndex(iIndex);

                //check that the input hash matches 
                if (iptInputTracker.m_lHashOfInput != lHashOfInput)
                {
                    Debug.LogError("Input hash does not match existing entry for this value");
                }

                bool bIsPeerRegisteredAtTick = false;
                bool bHasPeerDoubleRegistered = false;
                //check if there is disagreement on the tick the input belongs too
                for (int i = 0; i < iptInputTracker.m_lstTickInputConsumedAndPeersWhoUsedit.Count; i++)
                {
                    Tuple<uint, List<long>> tupPeersUsingInputAtTick = iptInputTracker.m_lstTickInputConsumedAndPeersWhoUsedit[i];

                    //check if this entry is for the correct tick
                    if (tupPeersUsingInputAtTick.Item1 != iTickInputUsed)
                    {
                        if (tupPeersUsingInputAtTick.Item2.Contains(lPeerUsingInput))
                        {
                            bHasPeerDoubleRegistered = true;
                        }
                    }
                    else
                    {
                        //check if already registered
                        if (tupPeersUsingInputAtTick.Item2.Contains(lPeerUsingInput))
                        {
                            //input is already reigstered no need to add it again
                            bIsPeerRegisteredAtTick = true;
                        }
                        else
                        {
                            //this input has already been registerd for this tick but this is the first time this peer is using it
                            bIsPeerRegisteredAtTick = true;
                            iptInputTracker.m_lstTickInputConsumedAndPeersWhoUsedit[i].Item2.Add(lPeerUsingInput);
                        }
                    }
                }

                if(bHasPeerDoubleRegistered)
                {
                    Debug.LogError("Peer input logged at different tick for same input");
                }

                //this is the first time 
                if (bIsPeerRegisteredAtTick == false)
                {
                    if (iptInputTracker.m_lstTickInputConsumedAndPeersWhoUsedit.Count > 0)
                    {
                        Debug.LogError("Disagreement between peers on when input should be used");
                    }

                    List<long> lstPeerList = new List<long>();
                    lstPeerList.Add(lPeerUsingInput);
                    Tuple<uint, List<long>> tupNewEntryForPeer = new Tuple<uint, List<long>>(iTickInputUsed, lstPeerList);
                    iptInputTracker.m_lstTickInputConsumedAndPeersWhoUsedit.Add(tupNewEntryForPeer);
                    iptInputTracker.m_iNumberOfPeersThatHaveUsedThisInput += 1;

                }
            }
            else
            {
                //this is the first time we are seeing this input so we need to create a new input tracker
                InputTracker iptNewInputTracker = new InputTracker();
                iptNewInputTracker.m_lHashOfInput = lHashOfInput;
                iptNewInputTracker.m_lCreatorPeerId = lPeerWhoCreatedInput;
                iptNewInputTracker.m_bIsConnectionChangeMessage = bIsConnectionChangeMessage;
                iptNewInputTracker.m_iNumberOfPeersThatHaveUsedThisInput = 1;
                iptNewInputTracker.m_lstTickInputConsumedAndPeersWhoUsedit = new List<Tuple<uint, List<long>>>();

                //add first peer entry
                List<long> lstPeerList = new List<long>();
                lstPeerList.Add(lPeerUsingInput);
                Tuple<uint, List<long>> tupNewEntryForPeer = new Tuple<uint, List<long>>(iTickInputUsed, lstPeerList);
                iptNewInputTracker.m_lstTickInputConsumedAndPeersWhoUsedit.Add(tupNewEntryForPeer);

                //insert the tick and throw an error if there is a collision
                if (m_srqInputTracker.TryInsertEnqueue(svaTimeOfInput, iptNewInputTracker, out int existingItem) == false)
                {
                    Debug.LogError("should not be here");
                }

            }
        }

        public static void OnStateFinalized(uint iTickOfStateFinalized, long lPeerFinalizingState, ITickTimeTranslator  tttTickTimeTranslator)
        {
            //get the oldest unvalidated state for the agent
            if(m_dicOldestUnvalidatedTickByAgent.TryGetValue(lPeerFinalizingState,out uint iBaseTick) )
            {
                DateTime dtmFinalizedTimeOfTick = tttTickTimeTranslator.ConvertSimTickToDateTime(iTickOfStateFinalized);
                DateTime dtmBaseTimeOfTick = tttTickTimeTranslator.ConvertSimTickToDateTime(iBaseTick);

                //convery tick to sorting value 
                SortingValue svaFinalizedSortValue = new SortingValue((ulong)(dtmFinalizedTimeOfTick.Ticks) + 1, ulong.MinValue);
                SortingValue svaBaseSortValue = new SortingValue((ulong)(dtmBaseTimeOfTick.Ticks), ulong.MaxValue);

                int iBaseIndex = 0;
                int iFinalizedIndex = 0;

                if(!m_srqInputTracker.TryGetFirstIndexLessThan(svaFinalizedSortValue, out iFinalizedIndex))
                {
                    //there are no inputs before this date to finalize
                    return;
                }

                if (!m_srqInputTracker.TryGetFirstIndexGreaterThan(svaBaseSortValue, out iBaseIndex))
                {
                    //there are no new inputs after this date to finalize
                    return;
                }

                if(iBaseIndex > iFinalizedIndex)
                {
                    //there are no inputs between these dates to finalize 
                    return;
                }

                int iNumOfInputsToFinalize = (iFinalizedIndex - iBaseIndex) + 1;

                for (int i = 0; i < iNumOfInputsToFinalize; i++)
                {
                    //get the registration for the input at the target tick
                    InputTracker iptInputTracker = m_srqInputTracker.GetValueAtIndex(iBaseIndex + i);

                    bool bHasPeerConsumedInput = false;

                    //loop through all the inputs and make sure the target peer has used it
                    for(int jTickOfInput = iptInputTracker.m_lstTickInputConsumedAndPeersWhoUsedit.Count -1; jTickOfInput >= 0; jTickOfInput--)
                    {
                        List<long> lstPeersAtTick = iptInputTracker.m_lstTickInputConsumedAndPeersWhoUsedit[jTickOfInput].Item2;

                        if(lstPeersAtTick.Contains(lPeerFinalizingState))
                        {
                            if(bHasPeerConsumedInput == true)
                            {
                                Debug.LogError($"Peer: {lPeerFinalizingState} has consumed the input at 2 different ticks");
                            }

                            bHasPeerConsumedInput = true;

                            lstPeersAtTick.Remove(lPeerFinalizingState);

                            if(lstPeersAtTick.Count == 0)
                            {
                                iptInputTracker.m_lstTickInputConsumedAndPeersWhoUsedit.RemoveAt(jTickOfInput);
                            }
                        }
                    }

                    if(bHasPeerConsumedInput == false)
                    {
                        uint iTick = tttTickTimeTranslator.ConvertDateTimeToTick( new DateTime((long)m_srqInputTracker.GetKeyAtIndex(iBaseIndex + i).m_lSortValueA));

                        Debug.LogError( $"Peer: {lPeerFinalizingState} has not consumed an input at tick {iTick} when finalizing state for tick {iTickOfStateFinalized}, " +
                                        $"the peers first tick was {m_dicFirstTickByAgent[lPeerFinalizingState]} the creator of the input was {iptInputTracker.m_lCreatorPeerId}" +
                                        $"the message was a connection message:{iptInputTracker.m_bIsConnectionChangeMessage} " +
                                        $" There have been :{iptInputTracker.m_iNumberOfPeersThatHaveUsedThisInput} peers that have used this input");
                    }
                }

                //remove any input trackers that have been fully finalized 
                for(int i = 0; i < m_srqInputTracker.Count; i++)
                {
                    if(m_srqInputTracker.PeakValueDequeue().m_lstTickInputConsumedAndPeersWhoUsedit.Count == 0)
                    {
                        m_srqInputTracker.Dequeue(out SortingValue svaKey, out InputTracker iptValue);
                    }
                    else
                    {
                        break;
                    }
                }

                //update the ticks finalized by peer
                m_dicOldestUnvalidatedTickByAgent[lPeerFinalizingState] = iTickOfStateFinalized;

            }
            else
            {
                Debug.LogError($"Peer: {lPeerFinalizingState} has not registered a start tick");
            }
        }

        public static void SetFirstInstance(uint iFirstTickForPeer, long lPeer)
        {
            m_dicFirstTickByAgent[lPeer] = iFirstTickForPeer;
            m_dicOldestUnvalidatedTickByAgent[lPeer] = iFirstTickForPeer;
        }

    }
}
