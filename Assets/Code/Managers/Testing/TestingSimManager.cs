using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Networking;
using System;

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

    public static long s_lSimTickLenght = TimeSpan.TicksPerSecond / 20;  

    public NetworkingDataBridge m_ndbNetworkingDataBridge;

    public ConstIndexRandomAccessQueue<SimState> m_sstSimStateBuffer;

    //for each of the "threads" processing the body states what index are they currently up too
    public ConstIndexRandomAccessQueue<uint> m_iActiveBodyProcessingTicks;

    public List<Tuple<DateTime, long>> m_tupNewDataRequests = new List<Tuple<DateTime, long>>();

    //the sim tick the latest state is based off
    public uint m_iSimHeadBaseTick;

    //how many inputs occured during the head state tick, this is used to check if new inputs have been createad and a new head state needs to be calculated 
    public int m_iNumberOfInputsInHeadSateCalculation;

    //true if the background state has been updated for head base
    public bool m_bNewDataForHeadBase;

    //to stop race conditions this is the tick the new background state was updated to
    public long m_lNewDataForHeadBaseTick;
    
    public void CheckForNewDataRequests()
    {
        if(m_ndbNetworkingDataBridge.GetNewRequestsForSimData(ref m_tupNewDataRequests))
        {
            //check if sim data for the requests exist
            for(int i = 0; i < m_tupNewDataRequests.Count; i++)
            {
                //get sim tick
                uint iSimTick = ConvertDateTimeToTick(m_tupNewDataRequests[i].Item1);

                //check if exists in sim state buffer
                if(m_sstSimStateBuffer.IsValidIndex(iSimTick))
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

        return (uint) ((lTickSinceStartOfYear + s_lSimTickLenght -1) / s_lSimTickLenght);
    }

    public DateTime ConvertSimTickToDateTime(DateTime dtmCurrnetTime, uint iTick)
    {
        //get number of ticks since start of year 
        DateTime startOfYear = new DateTime(dtmCurrnetTime.Year, 0, 0);

        return startOfYear + TimeSpan.FromTicks(iTick * s_lSimTickLenght);
    }

    public void UpdateHeadState()
    {
        //check if head should be recalculated
        if(ShouldRecalculateHead())
        {
            m_bNewDataForHeadBase = false;

            //run head recalculation
            SimState sstBaseState = m_sstSimStateBuffer[m_iSimHeadBaseTick];

            SimState sstNewHeadState = new SimState();

            DateTime dtmCurrentTime = m_ndbNetworkingDataBridge.m_dtmNetworkTime;

            DateTime dtmFrom = ConvertSimTickToDateTime(dtmCurrentTime, m_iSimHeadBaseTick);

            DateTime dtmTo = ConvertSimTickToDateTime(dtmCurrentTime, m_iSimHeadBaseTick + 1);

            object[] objMessages = m_ndbNetworkingDataBridge.GetMessagesFromData(dtmFrom, dtmTo);

            m_ndbNetworkingDataBridge.UpdateProcessedMessageTime(dtmFrom, dtmTo);

            m_iNumberOfInputsInHeadSateCalculation = objMessages.Length;

            CalculateTickResults(sstBaseState,ref sstNewHeadState, ref objMessages);

            m_sstSimStateBuffer[m_iSimHeadBaseTick + 1] = sstNewHeadState;
        }

        //check if new head should be calculated 
        if(ShouldCalculateNewHeadState())
        {
            //get tick for time 
            uint iHeadTick = ConvertDateTimeToTick(m_ndbNetworkingDataBridge.m_dtmNetworkTime);

            for(uint i = m_sstSimStateBuffer.HeadIndex; i < iHeadTick; i++)
            {        
                //get the state to be calculating from
                SimState sstBaseState = m_sstSimStateBuffer[i];

                SimState sstNewState = new SimState();

                //get the start and end time the new state calculation covers 
                DateTime dtmCurrentTime = m_ndbNetworkingDataBridge.m_dtmNetworkTime;

                DateTime dtmFrom = ConvertSimTickToDateTime(dtmCurrentTime, i);

                DateTime dtmTo = ConvertSimTickToDateTime(dtmCurrentTime, i + 1);

                //get the messages that occure during that state calculation 
                object[] objMessages = m_ndbNetworkingDataBridge.GetMessagesFromData(dtmFrom, dtmTo);

                if(i + 1 == iHeadTick)
                {
                    m_iNumberOfInputsInHeadSateCalculation = objMessages.Length;
                }

                m_ndbNetworkingDataBridge.UpdateProcessedMessageTime(dtmFrom, dtmTo);

                CalculateTickResults(sstBaseState, ref sstNewState, ref objMessages);

                m_sstSimStateBuffer.Enqueue(sstNewState);
            }

            m_iSimHeadBaseTick = iHeadTick - 1;
        }
    }

    public void CalculateTickResults(in SimState sstBaseState, ref SimState sstNewState, ref object[] inputs)
    {

        if (sstBaseState.m_lSimValue1 == 0)
        {
            sstNewState.m_lSimValue2 = sstBaseState.m_lSimValue2 + 1;
        }

        sstNewState.m_lSimValue1 = (sstBaseState.m_lSimValue1 + 1) % sstBaseState.m_lSimValue2;

    }
    
    public bool ShouldRecalculateHead()
    {
        //check if new base state has been calculated
        if (m_bNewDataForHeadBase)
        {
            return true;
        }

        //check if new inputs have been created for head tick
        //get the sorting value for the current tick
        SortingValue svaSortingValue = new SortingValue((ulong)ConvertSimTickToDateTime(m_ndbNetworkingDataBridge.m_dtmNetworkTime, m_iSimHeadBaseTick).Ticks - 1, ulong.MaxValue);

        int iNumberOfMessagesInHeadTickTimeSpan= 0;

        if(m_ndbNetworkingDataBridge.m_squMessageQueue.TryGetFirstIndexGreaterThan(svaSortingValue, out int iIndex))
        {
            //caculate the number of messages that occure during the head tick
            iNumberOfMessagesInHeadTickTimeSpan = m_ndbNetworkingDataBridge.m_squMessageQueue.Count - iIndex;
        }

        //check if there are new messages that have been added to the buffer and not processed by the head tick
        if(iNumberOfMessagesInHeadTickTimeSpan != m_iNumberOfInputsInHeadSateCalculation)
        {
            return true;
        }

        return false;
    }

    public bool ShouldCalculateNewHeadState()
    {
        uint iTickAtCurrentTime = ConvertDateTimeToTick(m_ndbNetworkingDataBridge.m_dtmNetworkTime);

        if(iTickAtCurrentTime > m_iSimHeadBaseTick + 1)
        {
            return true;
        }

        return false;
    }
    
    public void UpdateBodyState()
    {

    }

    //check if a new input needs to be processed 
    public bool ShouldReprocessBody()
    {
        DateTime dtmTimeOfLastProcessedInput = new DateTime((long)(m_ndbNetworkingDataBridge.m_svaSimProcessedMessagesUpTo.m_lSortValueA - 1));

        //check if an input falls within the body
        if(m_iActiveBodyProcessingTicks.Count == 0 || dtmTimeOfLastProcessedInput <= ConvertSimTickToDateTime(dtmTimeOfLastProcessedInput,m_ActiveBodyProcessingTicks.PeakEnqueue()))
        {
            return true;
        }

        return false;
    }

    public void ReprocessBodyStates(DateTime dtmProcessingStartTime)
    {
        //get start tick to process from 
        uint iProcessingStartTick = ConvertDateTimeToTick(dtmProcessingStartTime);
        uint iProcessingBaseTick = iProcessingStartTick - 1;


        //get new processing index 
        m_iActiveBodyProcessingTicks
    }

}

