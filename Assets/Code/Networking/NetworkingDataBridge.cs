using System;
using System.Collections.Generic;

namespace Networking
{
    //this class acts as a bridge for other classes to send and recieve data through the networking layer
    public class NetworkingDataBridge
    {
        public struct UserConnecionChange
        {
            public long[] m_lJoinPeerID;
            public int[] m_iJoinPeerChannelIndex;

            public long[] m_lKickPeerID;
            public int[] m_iKickPeerChannelIndex;

            public UserConnecionChange(int iKickCount, int iJoinCount)
            {
                m_lJoinPeerID = new long[iJoinCount];
                m_iJoinPeerChannelIndex = new int[iJoinCount];

                m_lKickPeerID = new long[iKickCount];
                m_iKickPeerChannelIndex = new int[iKickCount];
            }

        }

        public struct MessagePayloadWrapper
        {
            public long m_lPeerID;

            public int m_iChannelIndex;

            public ISimMessagePayload m_smpPayload;

        }

        //queue of all the messages 
        public SortedRandomAccessQueue<SortingValue, object> m_squMessageQueue = new SortedRandomAccessQueue<SortingValue, object>();
               
        //the time of the synchronization sync 
        public DateTime m_dtmSimStateSyncRequestTime = DateTime.MaxValue ;

        //the state of the sim synchronization 
        public SimStateSyncNetworkProcessor.State m_sssSimStartStateSyncStatus = SimStateSyncNetworkProcessor.State.None;

        //the synchonised state of the sim at startup
        public byte[] m_bSimState = new byte[0];

        //has the data from the bridge been coppied into the sim for use
        public bool m_bHasSimDataBeenProcessedBySim = true;

        //what data the peers on the network are requesting  
        public List<Tuple<DateTime, long>> m_tupActiveRequestedDataAtTimeForPeers = new List<Tuple<DateTime, long>>();

        //new requests from peers
        //this get cleared out once user
        public List<Tuple<DateTime, long>> m_tupNewRequestedDataAtTimeForPeers = new List<Tuple<DateTime, long>>();

        //data for requests from peers
        public Dictionary<long,Tuple<DateTime, long, byte[]>> m_tupDataAtTimeForPeers = new Dictionary<long, Tuple<DateTime, long, byte[]>>();
       
        //the shared time accross all peers  
        public DateTime m_dtmNetworkTime = DateTime.UtcNow;

        //time that all messages have been confirmed up to 
        public SortingValue m_svaConfirmedMessageTime = SortingValue.MinValue;

        //the oldest time in the sim that inputs are needed 
        public SortingValue m_svaOldestActiveSimTime = SortingValue.MinValue;

        //indicates the sim has processed all the messages up to this message
        public SortingValue m_svaSimProcessedMessagesUpTo = SortingValue.MinValue;

        //returns an array of requests between the start time and the end time including times a the same time as the start and excluding items at the end time 
        public List<Tuple<DateTime, long>> GetRequestsForTimePeriod(DateTime dtmStartTimeInclusive, DateTime dtmEndTimeExclusive)
        {
            List<Tuple<DateTime, long>> outArray = new List<Tuple<DateTime, long>>(0);

            //check if there are any values in the array
            if (m_tupActiveRequestedDataAtTimeForPeers.Count == 0)
            {
                return outArray;
            }

            //perform binary sarch on remaining values 

            int iSearchWindowMin = 0;
            int iSearchWindowMax = m_tupActiveRequestedDataAtTimeForPeers.Count;

            while (iSearchWindowMin < iSearchWindowMax)
            {
                int iMid = (iSearchWindowMin + iSearchWindowMax) / 2;

                if (m_tupActiveRequestedDataAtTimeForPeers[iMid].Item1 < dtmStartTimeInclusive)
                {
                    iSearchWindowMin = iMid + 1;
                }
                else
                {
                    iSearchWindowMax = iMid;
                }
            }

            // all items in list are before start window
            if (iSearchWindowMin == m_tupActiveRequestedDataAtTimeForPeers.Count)
            {
                return outArray;
            }

            for (int i = iSearchWindowMin; i < m_tupActiveRequestedDataAtTimeForPeers.Count; i++)
            {
                if (m_tupActiveRequestedDataAtTimeForPeers[i].Item1 < dtmEndTimeExclusive)
                {
                    outArray.Add(m_tupActiveRequestedDataAtTimeForPeers[i]);
                }
                else
                {
                    break;
                }
            }

            return outArray;
        }
             
        public bool GetNewRequestsForSimData(ref List<Tuple<DateTime,long>> tupNewItems)
        {
            //check if there are any new requests for data 
            if (m_tupNewRequestedDataAtTimeForPeers.Count == 0)
            {
                return false;
            }

            tupNewItems.AddRange(m_tupNewRequestedDataAtTimeForPeers);

            m_tupNewRequestedDataAtTimeForPeers.Clear();

            return true;
        }

        //add data for peer at time
        public void AddDataForPeer(long lPeerID, DateTime dtmTime, byte[] bData)
        {
            m_tupDataAtTimeForPeers[lPeerID] = new Tuple<DateTime, long, byte[]>(dtmTime, lPeerID, bData);
        }

        //queue this message discarding all the messages in the buffer that were older than this message
        public void QueueSimMessage(SortingValue svaTime, long lPeerID, int iChannelIndex, ISimMessagePayload smpMessage)
        {
            MessagePayloadWrapper mprMessage = new MessagePayloadWrapper()
            {
                m_lPeerID = lPeerID,
                m_iChannelIndex = iChannelIndex,
                m_smpPayload = smpMessage
            };

            m_squMessageQueue.EnterPurgeInsert(svaTime, mprMessage);
        }

        public void QueuePlayerChangeMessage(SortingValue svaEventTimt, UserConnecionChange uccConnectionChange)
        {
            m_squMessageQueue.EnterPurgeInsert(svaEventTimt, uccConnectionChange);
        }

        public void Clear(SortingValue svaClearUpTo)
        {
            m_squMessageQueue.ClearTo(svaClearUpTo);
        }

        public void UpdateSimStateAtTime(DateTime dtmTime, byte[] bSimData)
        {
            if(m_bSimState == null || m_bSimState.Length != bSimData.Length)
            {
                m_bSimState = new byte[bSimData.Length];
            }

            bSimData.CopyTo(m_bSimState, 0);

            m_bHasSimDataBeenProcessedBySim = false;
        }

        public object[] GetMessagesFromData(DateTime dtmStartTime, DateTime dtmEndTime)
        {
            SortingValue svaStartValue = new SortingValue((ulong)dtmStartTime.Ticks - 1, ulong.MaxValue);
            SortingValue svaEndValue = new SortingValue((ulong)dtmEndTime.Ticks, ulong.MaxValue);

            m_squMessageQueue.TryGetFirstIndexGreaterThan(svaStartValue, out int iStartIndex);
            m_squMessageQueue.TryGetFirstIndexLessThan(svaEndValue, out int iEndIndex);

            object[] objMessages = new object[(iEndIndex - iStartIndex) + 1];

            for (int i = iStartIndex; i <= iEndIndex; i++)
            {
                objMessages[i] = m_squMessageQueue.GetValueAtIndex(iStartIndex + i);
            }

            return objMessages;
        }

        public void UpdateProcessedMessageTime(DateTime dtmStartTimeInclusive, DateTime dtmEndTimeExclusive)
        {

            SortingValue svaFrom = new SortingValue((ulong)dtmStartTimeInclusive.Ticks, ulong.MinValue);
            SortingValue svaTo = new SortingValue((ulong)dtmEndTimeExclusive.Ticks - 1, ulong.MaxValue);

            if(m_svaSimProcessedMessagesUpTo.CompareTo(svaFrom) < 0 )
            {
                return;
            }

            if(m_svaSimProcessedMessagesUpTo.CompareTo(svaTo) > 0)
            {
                return;
            }

            m_svaSimProcessedMessagesUpTo = svaTo;
        }

    }
}
