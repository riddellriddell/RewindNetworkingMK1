using Networking;
using System;
using System.Collections.Generic;
using UnityEngine;
using GameManagers;
using Sim;
using Utility;
using FixedPointy;
using SharedTypes;

public class TestingSimManager<TFrameData, TConstData, TSettingsData>: 
    ISimFrameDataProvider<TFrameData>
    where TFrameData : IFrameData, new()
    where TSettingsData : ISimTickRateSettings
{
    public struct SimState
    {
        public static bool EncodeSimState(WriteByteStream wbsByteStream, ref SimState Input)
        {
            ByteStream.Serialize(wbsByteStream, ref Input.m_iInputVal);
            ByteStream.Serialize(wbsByteStream, ref Input.m_iInputCount);
            ByteStream.Serialize(wbsByteStream, ref Input.m_lPeerAssignedToSlot);

            return true;
        }

        public static bool DecodeSimState(ReadByteStream rbsByteStream, ref SimState Ouput)
        {
            ByteStream.Serialize(rbsByteStream, ref Ouput.m_iInputVal);
            ByteStream.Serialize(rbsByteStream, ref Ouput.m_iInputCount);
            ByteStream.Serialize(rbsByteStream, ref Ouput.m_lPeerAssignedToSlot);

            return true;
        }

        public static int SizeOfSimState(in SimState Input)
        {
            return ByteStream.DataSize(Input.m_iInputVal) + ByteStream.DataSize(Input.m_iInputCount) + ByteStream.DataSize(Input.m_lPeerAssignedToSlot);
        }

        public int[] m_iInputVal;

        public int[] m_iInputCount;

        public long[] m_lPeerAssignedToSlot;

    }

    public class ThreadSaveDataLock
    {
        public bool m_bShouldThreadStayAlive = true;
    }

    public TConstData m_cdaConstantData;

    public TSettingsData m_sdaSettingsData;

    //manages the processes that calculate new sim states 
    public SimProcessManager<TFrameData, TConstData, TSettingsData> m_spmSimProcessManager;

    public FrameDataObjectPool<TFrameData> m_fopFrameDataObjectPool = new FrameDataObjectPool<TFrameData>();

    public NetworkingDataBridge m_ndbNetworkingDataBridge;

    public ConstIndexRandomAccessQueue<TFrameData> m_fdaSimStateBuffer;

    //for each of the "threads" processing the body states what index are they currently up too
    //the object is a thread locking object that if set to null will indicate the thread should kill itself
    public SortedRandomAccessQueueUsingLambda<uint, ThreadSaveDataLock> m_iActiveBodyProcessingTicks;

    public List<Tuple<DateTime, long>> m_tupNewDataRequests = new List<Tuple<DateTime, long>>();

    //the sim tick the latest state is based off
    public uint m_iSimHeadTick = 0;

    //how many inputs occured during the head state tick, this is used to check if new inputs have been createad and a new head state needs to be calculated 
    public int m_iNumberOfInputsInHeadSateCalculation = 0;

    //TODO remobe this value, this is a testing value to check the sim has a stat on frist update
    public bool m_bFirstUpdate = true;
    
    public TestingSimManager(TConstData cdaSimConstantData, TSettingsData sdaSimSettingsData, NetworkingDataBridge ndbNetworkingDataBridge,SimProcessManager<TFrameData, TConstData, TSettingsData> spmSimProcessManager)
    {
        m_cdaConstantData = cdaSimConstantData;
        m_sdaSettingsData = sdaSimSettingsData;
        m_ndbNetworkingDataBridge = ndbNetworkingDataBridge;
        m_spmSimProcessManager = spmSimProcessManager;
    }

    public void InitalizeAsFirstPeer(long lPeerID)
    {
        m_iSimHeadTick = ConvertDateTimeToTick(m_ndbNetworkingDataBridge.GetCurrentSimTime()) - 1;

        m_iNumberOfInputsInHeadSateCalculation = 0;

        DateTime dtmSimStartTime = ConvertSimTickToDateTime(m_ndbNetworkingDataBridge.GetCurrentSimTime(), m_iSimHeadTick);



        TFrameData fdaStartState = m_fopFrameDataObjectPool.GetFrameData();

        //setup the inital state
        SetupInitalSimState(m_iSimHeadTick, m_sdaSettingsData,lPeerID,ref fdaStartState);

        m_fdaSimStateBuffer = new ConstIndexRandomAccessQueue<TFrameData>(m_iSimHeadTick);

        //queue first state
        m_fdaSimStateBuffer.Enqueue(fdaStartState);

        // setup sorted random access queue to sort threads processing the most recent data to the front of the queue ready to be dequeued
        // and new threads processing old data to the start of the queue 
        m_iActiveBodyProcessingTicks = new SortedRandomAccessQueueUsingLambda<uint, ThreadSaveDataLock>((uint iCompareFrom, uint iCompareTo) => iCompareFrom.CompareTo(iCompareTo) * -1);

        // set processed up to time, no messages earlier or equal to this time will be processed
        m_ndbNetworkingDataBridge.SetOldestActiveSimTime(new SortingValue((ulong)(dtmSimStartTime.Ticks), ulong.MaxValue));

        //set processed messages time to the earliest possible time as no processing has been done since
        m_ndbNetworkingDataBridge.m_svaSimProcessedMessagesUpTo = m_ndbNetworkingDataBridge.m_svaOldestActiveSimTime.NextSortValue();

    }

    public void InitalizeAsConnectingPeer()
    {
        m_fdaSimStateBuffer = new ConstIndexRandomAccessQueue<TFrameData>(0);

        // setup sorted random access queue to sort threads processing the most recent data to the front of the queue ready to be dequeued
        // and new threads processing old data to the start of the queue 
        m_iActiveBodyProcessingTicks = new SortedRandomAccessQueueUsingLambda<uint, ThreadSaveDataLock>((uint iCompareFrom, uint iCompareTo) => iCompareFrom.CompareTo(iCompareTo) * -1);

        // set processed up to time, no messages earlier or equal to this time will be processed
        m_ndbNetworkingDataBridge.SetOldestActiveSimTime( new SortingValue(0, ulong.MaxValue));

        //set processed messages time to the earliest possible time as no processing has been done since
        m_ndbNetworkingDataBridge.m_svaSimProcessedMessagesUpTo = m_ndbNetworkingDataBridge.m_svaOldestActiveSimTime.NextSortValue();

        m_iSimHeadTick = 0;

        m_iNumberOfInputsInHeadSateCalculation = 0;
    }

    public void CheckForNewData()
    {
        //get lock on network in sim data values 
        if (m_ndbNetworkingDataBridge.m_bHasSimDataBeenProcessedBySim == false)
        {
            uint iSimDataTick = ConvertDateTimeToTick(m_ndbNetworkingDataBridge.m_dtmSimStateSyncRequestTime);

            //get the tick for the target item
            byte[] bSimData = m_ndbNetworkingDataBridge.m_bSimState;

            ReadByteStream rbsSimData = new ReadByteStream(bSimData);

            TFrameData fdaState = m_fopFrameDataObjectPool.GetFrameData();

            fdaState.Decode(rbsSimData);

            //kll all threads processing data before new sim state 

            //get lock on thread queue

            //get start index of all threads working on data older than new data 
            //because list is sorted in reverse order the compare opperator has to be reversed 
            if (m_iActiveBodyProcessingTicks.TryGetFirstIndexGreaterThan(iSimDataTick + 1, out int iOutdatedThreadStartIndex))
            {
                for (int i = iOutdatedThreadStartIndex; i > -1; i--)
                {
                    //get lock for thread interupt
                    ThreadSaveDataLock sdlSaveDataLock = m_iActiveBodyProcessingTicks.GetValueAtIndex(i);

                    sdlSaveDataLock.m_bShouldThreadStayAlive = false;

                    //unlock thread interupt
                }
            }

            //get lock on sim state buffer

            //set sim state 
            if (m_fdaSimStateBuffer.Count > 0 && iSimDataTick <= m_fdaSimStateBuffer.HeadIndex && iSimDataTick >= m_fdaSimStateBuffer.BaseIndex)
            {
                //if there is already a full sim buffer keep the existing data for a smooth update 

                //return the old state to the pool 
                m_fopFrameDataObjectPool.ReturnFrameData(m_fdaSimStateBuffer[iSimDataTick]);

                m_fdaSimStateBuffer[iSimDataTick] = fdaState;

                if (m_iSimHeadTick == iSimDataTick)
                {
                    m_iNumberOfInputsInHeadSateCalculation = 0;
                }
            }
            else
            {
                //if the existing data is out of data reset the state buffer to only have the target state 
                m_fdaSimStateBuffer.Clear();
                m_fdaSimStateBuffer.SetNewBaseIndex(iSimDataTick);
                m_fdaSimStateBuffer.Enqueue(fdaState);
                m_iSimHeadTick = iSimDataTick;
                m_iNumberOfInputsInHeadSateCalculation = 0;
            }

            DateTime dtmStartOfState = ConvertSimTickToDateTime(m_ndbNetworkingDataBridge.GetCurrentSimTime(), iSimDataTick);

            //sim messages older or equal to this have been processed / are not needed / there are not the resources to compute
            m_ndbNetworkingDataBridge.SetOldestActiveSimTime(new SortingValue((ulong)dtmStartOfState.Ticks, ulong.MaxValue));

            //all messages from this start time need to be reprocessed
            m_ndbNetworkingDataBridge.m_svaSimProcessedMessagesUpTo = m_ndbNetworkingDataBridge.m_svaOldestActiveSimTime.NextSortValue();

            // inform network data buffer that sim data has been fetched 
            m_ndbNetworkingDataBridge.m_bHasSimDataBeenProcessedBySim = true;

            //release lock on sim state buffer
            //release lock on thread buffer
        }

        //unlock network in sim data values 
    }

    public void CheckForNewDataRequests()
    {
        if (m_ndbNetworkingDataBridge.GetNewRequestsForSimData(ref m_tupNewDataRequests))
        {
            //check if sim data for the requests exist
            for (int i = 0; i < m_tupNewDataRequests.Count; i++)
            {
                //get sim tick
                uint iSimTick = ConvertDateTimeToTick(m_tupNewDataRequests[i].Item1);

                //check if exists in sim state buffer
                if (m_fdaSimStateBuffer.IsValidIndex(iSimTick))
                {
                    TFrameData fdaState = m_fdaSimStateBuffer[iSimTick];

                    WriteByteStream wbsSimData = new WriteByteStream(fdaState.GetSize());

                    fdaState.Encode(wbsSimData);

                    m_ndbNetworkingDataBridge.AddDataForPeer(m_tupNewDataRequests[i].Item2, m_tupNewDataRequests[i].Item1, wbsSimData.GetData());
                }
            }
        }
    }

    public uint ConvertDateTimeToTick(DateTime dateTime)
    {
        //get number of ticks since start of year 
        DateTime startOfYear = new DateTime(dateTime.Year, 1, 1);

        long lTickSinceStartOfYear = dateTime.Ticks - startOfYear.Ticks;

        uint iSimTicksSinceStartOfYear = (uint)((lTickSinceStartOfYear + m_sdaSettingsData.SimTickLength - 1) / m_sdaSettingsData.SimTickLength);

        return iSimTicksSinceStartOfYear;
    }

    public DateTime ConvertSimTickToDateTime(DateTime dtmCurrnetTime, uint iTick)
    {
        //get number of ticks since start of year 
        DateTime startOfYear = new DateTime(dtmCurrnetTime.Year, 1, 1);

        return startOfYear + TimeSpan.FromTicks(iTick * m_sdaSettingsData.SimTickLength);
    }

    public void Update()
    {
        CheckForNewData();

        if(m_bFirstUpdate)
        {
            m_bFirstUpdate = false;

            if(m_fdaSimStateBuffer.Count == 0 )
            {
                Debug.LogError("No State on first update");
            }
        }

        //check if a sim state has been setup on peer
        if (m_fdaSimStateBuffer.Count != 0)
        {
            CheckForNewDataRequests();

            UpdateStateProcessors();

            DeleteOutdatedStates();
        }
    }

    public void UpdateStateProcessors()
    {
        //check if a message has changed in the body of the sim buffer that needs updating
        if (ShouldReprocessBody())
        {
            //get tick to reprocess from
            DateTime dtmTimeOfLastProcessedInput = new DateTime((long)m_ndbNetworkingDataBridge.m_svaSimProcessedMessagesUpTo.m_lSortValueA);

            uint iBaseTickToProcessFrom = ConvertDateTimeToTick(dtmTimeOfLastProcessedInput) - 1;

            RunStateUpdate(iBaseTickToProcessFrom);
        }

        //check if head should be recalculated
        if (NewInputsForHead())
        {
            RunStateUpdate(m_iSimHeadTick - 1);
        }

        if (TimeForNewHeadTick())
        {
            RunStateUpdate(m_iSimHeadTick);
        }

    }

    public void SetupInitalSimState(uint iStartTick, in TSettingsData sdaSettingsData, in long lInitalPeer, ref TFrameData fdaInitalFrameData)
    {
        m_spmSimProcessManager.SetupInitalFrameData(iStartTick, sdaSettingsData, lInitalPeer, ref fdaInitalFrameData);
    }

    public SimState SetupInitalStateDepricated(uint iTick, int iMaxPeerCount, long lCreatingPeerID)
    {
        SimState sstSimState = new SimState()
        {
            m_iInputVal = new int[iMaxPeerCount],
            m_iInputCount = new int[iMaxPeerCount],
            m_lPeerAssignedToSlot = new long[iMaxPeerCount],
        };

        sstSimState.m_lPeerAssignedToSlot[0] = lCreatingPeerID;

        return sstSimState;
    }

    public void CalculateTickResult(uint iTick, in TSettingsData sdaSettingsData, TConstData cdaConstantData, in TFrameData sstBaseState, ref object[] inputs, ref TFrameData sstNewState)
    {
        m_spmSimProcessManager.ProcessFrameData(iTick, sdaSettingsData, cdaConstantData, sstBaseState, inputs, ref sstNewState);
    }

    public void CalculateTickResultsDepreciated(in SimState sstBaseState, ref SimState sstNewState, ref object[] inputs)
    {
        sstNewState.m_iInputVal = (int[])sstBaseState.m_iInputVal.Clone();
        sstNewState.m_iInputCount = (int[])sstBaseState.m_iInputCount.Clone();
        sstNewState.m_lPeerAssignedToSlot = (long[])sstBaseState.m_lPeerAssignedToSlot.Clone();


        for (int i = 0; i < inputs.Length; i++)
        {
            if(inputs[i] is MessagePayloadWrapper)
            {
               // Debug.Log("Handling custom user input message");

                MessagePayloadWrapper mpwMessageWrapper = (MessagePayloadWrapper)inputs[i];

                LocalPeerInputManager.TestingUserInput peerInput = mpwMessageWrapper.m_smpPayload as LocalPeerInputManager.TestingUserInput;

                sstNewState.m_iInputCount[mpwMessageWrapper.m_iChannelIndex] = sstBaseState.m_iInputCount[mpwMessageWrapper.m_iChannelIndex] + 1;

                sstNewState.m_iInputVal[mpwMessageWrapper.m_iChannelIndex] = peerInput.m_iInput;

                //sstNewState.m_lSimValue1 = (sstNewState.m_lSimValue1 + (peerInput.m_uipInputState.m_bPayload * peerInput.m_uipInputState.m_bPayload)) % long.MaxValue;

            }
            else if(inputs[i] is UserConnecionChange)
            {
                Debug.Log("Handling user connection change message");
            
                UserConnecionChange uccUserConnectionChange = (UserConnecionChange)inputs[i];

                //apply all the join messages
                for(int j  = 0; j < uccUserConnectionChange.m_iJoinPeerChannelIndex.Length; j++)
                {
                    sstNewState.m_lPeerAssignedToSlot[uccUserConnectionChange.m_iJoinPeerChannelIndex[j]] = uccUserConnectionChange.m_lJoinPeerID[j];
                }
                    
                //apply all the kick messages
                for (int j = 0; j < uccUserConnectionChange.m_iKickPeerChannelIndex.Length; j++)
                {
                    sstNewState.m_iInputCount[uccUserConnectionChange.m_iKickPeerChannelIndex[j]] = 0;
                    sstNewState.m_iInputVal[uccUserConnectionChange.m_iKickPeerChannelIndex[j]] = 0;
                    sstNewState.m_lPeerAssignedToSlot[uccUserConnectionChange.m_iKickPeerChannelIndex[j]] = 0;
                }
            }
        }
    }

    public bool NewInputsForHead()
    {
        //lock head mesage buffer

        //get the indexes for the head message buffer
        m_ndbNetworkingDataBridge.GetIndexesBetweenTimes(ConvertSimTickToDateTime(
            m_ndbNetworkingDataBridge.GetCurrentSimTime(), m_iSimHeadTick - 1),
            ConvertSimTickToDateTime(m_ndbNetworkingDataBridge.GetCurrentSimTime(), m_iSimHeadTick),
            out int iStartMessageIndex,
            out int iEndMessageIndex);

        //unlock head message buffer

        int iNumberOfMessagesInHeadTickTimeSpan = (iEndMessageIndex - iStartMessageIndex) + 1;

        //check if there are new messages that have been added to the buffer and not processed by the head tick
        if (iNumberOfMessagesInHeadTickTimeSpan != m_iNumberOfInputsInHeadSateCalculation)
        {
            return true;
        }

        return false;
    }

    public bool TimeForNewHeadTick()
    {
        uint iTickAtCurrentTime = ConvertDateTimeToTick(m_ndbNetworkingDataBridge.GetCurrentSimTime());

        if (iTickAtCurrentTime > m_iSimHeadTick)
        {
            return true;
        }

        return false;
    }

    //check if a new input needs to be processed 
    public bool ShouldReprocessBody()
    {
        DateTime dtmTimeOfLastProcessedInput = new DateTime((long)(m_ndbNetworkingDataBridge.m_svaSimProcessedMessagesUpTo.m_lSortValueA));

        //get lock on thread buffer

        //check if input during or after head tick and should be handled by head tick update instead 
        if(dtmTimeOfLastProcessedInput >= ConvertSimTickToDateTime(dtmTimeOfLastProcessedInput, m_iSimHeadTick))
        {
            return false;
        }

        //check if an input falls before the thread processing the oldest inputs
        if (m_iActiveBodyProcessingTicks.Count == 0 || dtmTimeOfLastProcessedInput < ConvertSimTickToDateTime(dtmTimeOfLastProcessedInput, m_iActiveBodyProcessingTicks.PeakKeyEnqueue() - 1))
        {
            return true;
        }

        return false;
    }

    //updates all the states after base state upto current tick based off network time;
    public void RunStateUpdate(uint iBaseTick)
    {
        //get lock on threading buffer 

        //try to get lock on start index
        //get the save data for thread lock
        ThreadSaveDataLock tsdSaveDataLock = new ThreadSaveDataLock();

        if (m_iActiveBodyProcessingTicks.TryInsertEnqueue(iBaseTick, tsdSaveDataLock, out int iIndex) == false)
        {
            //thread already proccessing tick wait for thread to finish 
            return;
        }

        //unlock thread tick array

        while (iBaseTick < ConvertDateTimeToTick(m_ndbNetworkingDataBridge.GetCurrentSimTime()))
        {
            //get lock on should save data
            if (tsdSaveDataLock.m_bShouldThreadStayAlive == false)
            {
                break;
            }

            //release lock on thread interupt

            //lock thread tick array

            //get current index  
            if (m_iActiveBodyProcessingTicks.TryGetIndexOf(iBaseTick, out int iThreadTickIndex) == false)
            {
                //thread index should not be delisted an error has occured
                return;
            }

            uint iNextThreadTick = uint.MaxValue;

            if (iThreadTickIndex - 1 > -1)
            {
                iNextThreadTick = m_iActiveBodyProcessingTicks.GetKeyAtIndex(iThreadTickIndex - 1);
            }

            if (iNextThreadTick == iBaseTick + 1)
            {
                //wait for next thread along to finish updating state 

                //release lock on thread buffer
                continue;
            }

            //get lock on state buffer

            //get the base state
            TFrameData fdaBaseState = m_fdaSimStateBuffer[iBaseTick];

            //release lock on state buffer

            //increment the base toate to build from 
            uint iNextTick = iBaseTick + 1;

            //update the current tick index
            m_iActiveBodyProcessingTicks.SetKeyAtIndex(iThreadTickIndex, ref iNextTick);

            //release lock on thead tick tracker

            //create data for next state
            TFrameData fdaNextState = m_fopFrameDataObjectPool.GetFrameData();

            DateTime dtmCurrentTime = m_ndbNetworkingDataBridge.GetCurrentSimTime();

            DateTime dtmFrom = ConvertSimTickToDateTime(dtmCurrentTime, iBaseTick);

            DateTime dtmTo = ConvertSimTickToDateTime(dtmCurrentTime, iNextTick);

            //get lock on inputs array 

            object[] objMessages = m_ndbNetworkingDataBridge.GetMessagesFromData(dtmFrom, dtmTo);

            m_ndbNetworkingDataBridge.UpdateProcessedMessageTime(dtmFrom, dtmTo);

            //unlock sim messages array

            //check if processing head state
            if (iNextTick == ConvertDateTimeToTick(m_ndbNetworkingDataBridge.GetCurrentSimTime()))
            {
                //update number of messages in head tick
                m_iNumberOfInputsInHeadSateCalculation = objMessages.Length;
            }

            //caclulate new sim state at tick
            CalculateTickResult(iNextTick, m_sdaSettingsData, m_cdaConstantData, fdaBaseState, ref objMessages, ref fdaNextState);

            //get lock on thread save data value 
            if (tsdSaveDataLock.m_bShouldThreadStayAlive)
            {
                //get lock on state buffer

                //store result
                if (m_fdaSimStateBuffer.HeadIndex == iNextTick -1)
                {
                    m_fdaSimStateBuffer.Enqueue(fdaNextState);
                }
                else if(iNextTick < m_fdaSimStateBuffer.BaseIndex)
                {
                    //TODO: check if this ever gets hit??
                    //check if updating a tick for a state that is not valid anymore 
                    tsdSaveDataLock.m_bShouldThreadStayAlive = false;
                    continue;
                }
                else
                {
                    m_fdaSimStateBuffer[iNextTick] = fdaNextState;
                }

                //release lock on state buffer

                //get lock on data for peer sim sync

                List<Tuple<DateTime, long>> tupPeerRequestsForTime = m_ndbNetworkingDataBridge.GetRequestsForTimePeriod(dtmFrom, dtmTo);

                //todo clean this up to not produce as much garbage 
                if (tupPeerRequestsForTime.Count > 0)
                {
                    Debug.Log("TestingSimManager:: sending sim data to network data bridge to be synced");

                    WriteByteStream wbsSimData = new WriteByteStream( fdaNextState.GetSize());

                    fdaNextState.Encode(wbsSimData);

                    for (int i = 0; i < tupPeerRequestsForTime.Count; i++)
                    {
                        m_ndbNetworkingDataBridge.AddDataForPeer(tupPeerRequestsForTime[i].Item2, tupPeerRequestsForTime[i].Item1, wbsSimData.GetData());
                    }
                }

                // release lock on peer sim sync

                //set the most recently processed tick
                m_iSimHeadTick = Math.Max(iNextTick, m_iSimHeadTick);
            }

            //release lock on thread interupt

            iBaseTick = iNextTick;
        }

        //at this point this thread should be at the exit of the queue and read to dequeue

        //get lock on threat tick queue and save value

        if (tsdSaveDataLock.m_bShouldThreadStayAlive == true)
        {
            m_iActiveBodyProcessingTicks.Dequeue(out uint iTick, out ThreadSaveDataLock tsdValue);
        }
        else
        {
            //get current index 
            if (m_iActiveBodyProcessingTicks.TryGetIndexOf(iBaseTick, out int iThreadTickIndex))
            {
                //remove thread 
                m_iActiveBodyProcessingTicks.Remove(iThreadTickIndex);

            }
        }

        //unlock thread queue
    }

    public void DeleteOutdatedStates()
    {
        //protect against errors on startup 
        long lValidUpTo = (long)(m_ndbNetworkingDataBridge.m_svaConfirmedMessageTime.m_lSortValueA) - 1;

        if(lValidUpTo < 0)
        {
            return;
        }

        //get the validated time from network data bridge 
        DateTime dtmValidatedUpTo = new DateTime(lValidUpTo);

        uint iIndexOfOldestUnconfirmedState = ConvertDateTimeToTick(dtmValidatedUpTo);

        //protect against errors on startup with initalised min values
        if(iIndexOfOldestUnconfirmedState == 0)
        {
            return;
        }

        uint iOldestConfirmedState = iIndexOfOldestUnconfirmedState - 1;

        // remove all states that are not going to change and are not going to be used again in a state update 
        while (m_fdaSimStateBuffer.Count > 1 && m_fdaSimStateBuffer.BaseIndex < iOldestConfirmedState)
        {
            uint iDequeueTick = m_fdaSimStateBuffer.BaseIndex;

            TFrameData fdaOldState = m_fdaSimStateBuffer.Dequeue();

            TestingSimDataSyncVerifier<TFrameData>.VerifyData(iDequeueTick,ref fdaOldState, 0, (int)m_sdaSettingsData.TicksPerSecond * 2);
        }
    }

    //TODO: Find a way to get this data to the interpolator without coppying the entire game state
    // maybe do read only lock on game state or only clone the needed frame data segment 
    public void GetInterpolationFrameData(DateTime dtmSimTime, ref TFrameData fdaOutFrom, ref TFrameData fdaOutToo, out float fOutLerp)
    {
        //check that frame buffer holds more than one entry 
        if(m_fdaSimStateBuffer.Count == 1)
        {
            //get lock on buffer

            fdaOutFrom.ResetToState(m_fdaSimStateBuffer[0]);
            fdaOutToo.ResetToState(m_fdaSimStateBuffer[0]);

            //release llock on buffer
            fOutLerp = 0;

            return;            
        }

        //convert date time to sim tick
        uint iFromTick = Math.Min( Math.Max(ConvertDateTimeToTick(dtmSimTime), m_fdaSimStateBuffer.BaseIndex), m_fdaSimStateBuffer.HeadIndex -1);
        uint iToTick = iFromTick + 1;

        //get lock on buffer

        fdaOutFrom.ResetToState(m_fdaSimStateBuffer[iFromTick]);
        fdaOutToo.ResetToState(m_fdaSimStateBuffer[iToTick]);

        //release lock on buffer

        //get tick of from data
        DateTime dtmFromDataTime = ConvertSimTickToDateTime(dtmSimTime, iFromTick);

        TimeSpan tspTimeDif = dtmSimTime - dtmFromDataTime;

        fOutLerp = tspTimeDif.Ticks / (float)m_sdaSettingsData.SimTickLength;
    }
}

