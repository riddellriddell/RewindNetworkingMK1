using Networking;
using System;
using System.Collections.Generic;

public class TestingSimManager
{
    public struct SimState
    {
        public static bool EncodeSimState(WriteByteStream wbsByteStream, ref SimState Input)
        {
            ByteStream.Serialize(wbsByteStream, ref Input.m_lSimValue1);
            ByteStream.Serialize(wbsByteStream, ref Input.m_lSimValue2);

            return true;
        }

        public static bool DecodeSimState(ReadByteStream rbsByteStream, ref SimState Ouput)
        {
            ByteStream.Serialize(rbsByteStream, ref Ouput.m_lSimValue1);
            ByteStream.Serialize(rbsByteStream, ref Ouput.m_lSimValue2);

            return true;
        }

        public static int SizeOfSimState(in SimState Input)
        {
            return ByteStream.DataSize(Input.m_lSimValue1) + ByteStream.DataSize(Input.m_lSimValue2);
        }

        public long m_lSimValue1;
        public long m_lSimValue2;

    }

    public class ThreadSaveDataLock
    {
        public bool m_bShouldThreadStayAlive = true;
    }

    public static long s_lSimTickLenght = TimeSpan.TicksPerSecond / 20;

    public NetworkingDataBridge m_ndbNetworkingDataBridge;

    public ConstIndexRandomAccessQueue<SimState> m_sstSimStateBuffer;

    //for each of the "threads" processing the body states what index are they currently up too
    //the object is a thread locking object that if set to null will indicate the thread should kill itself
    public SortedRandomAccessQueueUsingLambda<uint, ThreadSaveDataLock> m_iActiveBodyProcessingTicks;

    public List<Tuple<DateTime, long>> m_tupNewDataRequests = new List<Tuple<DateTime, long>>();

    //the sim tick the latest state is based off
    public uint m_iSimHeadTick;

    //how many inputs occured during the head state tick, this is used to check if new inputs have been createad and a new head state needs to be calculated 
    public int m_iNumberOfInputsInHeadSateCalculation;


    public TestingSimManager(NetworkingDataBridge ndbNetworkingDataBridge)
    {
        m_ndbNetworkingDataBridge = ndbNetworkingDataBridge;
    }

    public void InitalizeAsFirstPeer()
    {
        m_iSimHeadTick = ConvertDateTimeToTick(m_ndbNetworkingDataBridge.m_dtmNetworkTime) - 1;

        m_iNumberOfInputsInHeadSateCalculation = 0;

        DateTime dtmSimStartTime = ConvertSimTickToDateTime(m_ndbNetworkingDataBridge.m_dtmNetworkTime, m_iSimHeadTick);

        SimState sstStartState = SetupInitalState();

        m_sstSimStateBuffer = new ConstIndexRandomAccessQueue<SimState>(m_iSimHeadTick);

        //queue first state
        m_sstSimStateBuffer.Enqueue(sstStartState);

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
        m_sstSimStateBuffer = new ConstIndexRandomAccessQueue<SimState>(0);

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

            SimState sstState = new SimState();

            SimState.DecodeSimState(rbsSimData, ref sstState);

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
            if (m_sstSimStateBuffer.Count > 0 && iSimDataTick <= m_sstSimStateBuffer.HeadIndex && iSimDataTick >= m_sstSimStateBuffer.BaseIndex)
            {
                //if there is already a full sim buffer keep the existing data for a smooth update 
                m_sstSimStateBuffer[iSimDataTick] = sstState;

                if (m_iSimHeadTick == iSimDataTick)
                {
                    m_iNumberOfInputsInHeadSateCalculation = 0;
                }
            }
            else
            {
                //if the existing data is out of data reset the state buffer to only have the target state 
                m_sstSimStateBuffer.Clear();
                m_sstSimStateBuffer.SetNewBaseIndex(iSimDataTick);
                m_sstSimStateBuffer.Enqueue(sstState);
                m_iSimHeadTick = iSimDataTick;
                m_iNumberOfInputsInHeadSateCalculation = 0;
            }

            DateTime dtmStartOfState = ConvertSimTickToDateTime(m_ndbNetworkingDataBridge.m_dtmNetworkTime, iSimDataTick);

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
                if (m_sstSimStateBuffer.IsValidIndex(iSimTick))
                {
                    SimState sstState = m_sstSimStateBuffer[iSimTick];

                    WriteByteStream wbsSimData = new WriteByteStream(SimState.SizeOfSimState(sstState));

                    SimState.EncodeSimState(wbsSimData, ref sstState);

                    m_ndbNetworkingDataBridge.AddDataForPeer(m_tupNewDataRequests[i].Item2, m_tupNewDataRequests[i].Item1, wbsSimData.GetData());
                }
            }
        }
    }

    public uint ConvertDateTimeToTick(DateTime dateTime)
    {
        //get number of ticks since start of year 
        DateTime startOfYear = new DateTime(dateTime.Year, 0, 0);

        long lTickSinceStartOfYear = dateTime.Ticks - startOfYear.Ticks;

        return (uint)((lTickSinceStartOfYear + s_lSimTickLenght - 1) / s_lSimTickLenght);
    }

    public DateTime ConvertSimTickToDateTime(DateTime dtmCurrnetTime, uint iTick)
    {
        //get number of ticks since start of year 
        DateTime startOfYear = new DateTime(dtmCurrnetTime.Year, 0, 0);

        return startOfYear + TimeSpan.FromTicks(iTick * s_lSimTickLenght);
    }

    public void Update()
    {
        CheckForNewData();

        //check if a sim state has been setup on peer
        if (m_sstSimStateBuffer.Count != 0)
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

    public SimState SetupInitalState()
    {
        SimState sstSimState = new SimState()
        {
            m_lSimValue1 = 1,
            m_lSimValue2 = 1,
        };

        return sstSimState;
    }

    public void CalculateTickResults(in SimState sstBaseState, ref SimState sstNewState, ref object[] inputs)
    {

        if (sstBaseState.m_lSimValue1 == 0)
        {
            sstNewState.m_lSimValue2 = sstBaseState.m_lSimValue2 + 1;
        }

        sstNewState.m_lSimValue1 = (sstBaseState.m_lSimValue1 + 1) % sstBaseState.m_lSimValue2;

    }

    public bool NewInputsForHead()
    {
        //lock head mesage buffer

        //get the indexes for the head message buffer
        m_ndbNetworkingDataBridge.GetIndexesBetweenTimes(ConvertSimTickToDateTime(
            m_ndbNetworkingDataBridge.m_dtmNetworkTime, m_iSimHeadTick - 1),
            ConvertSimTickToDateTime(m_ndbNetworkingDataBridge.m_dtmNetworkTime, m_iSimHeadTick),
            out int iStartMessageIndex,
            out int iEndMessageIndex);

        //unlock head message buffer

        int iNumberOfMessagesInHeadTickTimeSpan = iEndMessageIndex - iStartMessageIndex;

        //check if there are new messages that have been added to the buffer and not processed by the head tick
        if (iNumberOfMessagesInHeadTickTimeSpan != m_iNumberOfInputsInHeadSateCalculation)
        {
            return true;
        }

        return false;
    }

    public bool TimeForNewHeadTick()
    {
        uint iTickAtCurrentTime = ConvertDateTimeToTick(m_ndbNetworkingDataBridge.m_dtmNetworkTime);

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

        while (iBaseTick < ConvertDateTimeToTick(m_ndbNetworkingDataBridge.m_dtmNetworkTime))
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
            SimState sstBaseState = m_sstSimStateBuffer[iBaseTick];

            //release lock on state buffer

            //increment the base toate to build from 
            uint iNextTick = iBaseTick + 1;

            //update the current tick index
            m_iActiveBodyProcessingTicks.SetKeyAtIndex(iThreadTickIndex, ref iNextTick);

            //release lock on thead tick tracker

            //create data for next state
            SimState sstNextState = new SimState();

            DateTime dtmCurrentTime = m_ndbNetworkingDataBridge.m_dtmNetworkTime;

            DateTime dtmFrom = ConvertSimTickToDateTime(dtmCurrentTime, iBaseTick);

            DateTime dtmTo = ConvertSimTickToDateTime(dtmCurrentTime, iNextTick);

            //get lock on inputs array 

            object[] objMessages = m_ndbNetworkingDataBridge.GetMessagesFromData(dtmFrom, dtmTo);

            m_ndbNetworkingDataBridge.UpdateProcessedMessageTime(dtmFrom, dtmTo);

            //unlock sim messages array

            //check if processing head state
            if (iNextTick == ConvertDateTimeToTick(m_ndbNetworkingDataBridge.m_dtmNetworkTime))
            {
                //update number of messages in head tick
                m_iNumberOfInputsInHeadSateCalculation = objMessages.Length;
            }

            //caclulate new sim state at tick
            CalculateTickResults(sstBaseState, ref sstNextState, ref objMessages);

            //get lock on thread save data value 
            if (tsdSaveDataLock.m_bShouldThreadStayAlive)
            {
                //get lock on state buffer

                //store result
                if (m_sstSimStateBuffer.HeadIndex < iNextTick)
                {
                    m_sstSimStateBuffer.Enqueue(sstNextState);
                }
                else if(iNextTick < m_sstSimStateBuffer.BaseIndex)
                {
                    //TODO: check if this ever gets hit??
                    //check if updating a tick for a state that is not valid anymore 
                    tsdSaveDataLock.m_bShouldThreadStayAlive = false;
                    continue;
                }
                else
                {
                    m_sstSimStateBuffer[iNextTick] = sstNextState;
                }

                //release lock on state buffer

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
        //get the validated time from network data bridge 
        DateTime dtmValidatedUpTo = new DateTime((long)(m_ndbNetworkingDataBridge.m_svaConfirmedMessageTime.m_lSortValueA) -1);

        uint iIndexOfOldestUnconfirmedState = ConvertDateTimeToTick(dtmValidatedUpTo);

        //protect against errors on startup with initalised min values
        if(iIndexOfOldestUnconfirmedState == 0)
        {
            return;
        }

        uint iOldestConfirmedState = iIndexOfOldestUnconfirmedState - 1;

        // remove all states that are not going to change and are not going to be used again in a state update 
        while (m_sstSimStateBuffer.Count > 1 && m_sstSimStateBuffer.BaseIndex < iOldestConfirmedState)
        {
            m_sstSimStateBuffer.Dequeue();
        }
    }

}

