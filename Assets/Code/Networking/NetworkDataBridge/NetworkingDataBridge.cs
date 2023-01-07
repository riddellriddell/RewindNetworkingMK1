using SharedTypes;
using System;
using System.Collections.Generic;

namespace Networking
{
    //this class acts as a bridge for other classes to send and recieve data through the networking layer
    public class NetworkingDataBridge : ISimTimeProvider
    {       
        //queue of all the outbound messages 
        public List<GlobalMessageBase> m_gmbOutMessageBuffer = new List<GlobalMessageBase>();

        //queue of all the inbound messages 
        public SortedRandomAccessQueue<SortingValue, object> m_squInMessageQueue = new SortedRandomAccessQueue<SortingValue, object>();
               
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
       
        //timespan values used to shared time accross all peers  
        public DateTime m_dtmNetworkOldestTime = DateTime.MinValue;

        public TimeSpan m_tspNetworkTimeOffset = TimeSpan.Zero;

        //time that all messages have been confirmed up to 
        public SortingValue m_svaConfirmedMessageTime = SortingValue.MinValue;

        //the oldest time in the sim that inputs are needed 
        public SortingValue m_svaOldestActiveSimTime = SortingValue.MinValue;

        //indicates the sim has processed all the messages up to this message
        public SortingValue m_svaSimProcessedMessagesUpTo = SortingValue.MinValue;

        //no messages this old or older are alowed in the message buffer
        public SortingValue m_svaOldestMessageToStoreInBuffer;

        //returns an array of requests between the start time and the end time including times a the same time as the start and excluding items at the end time 
        public List<Tuple<DateTime, long>> GetRequestsForTimePeriod(DateTime dtmStartTimeExclusive, DateTime dtmEndTimeInclusive)
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

                if (m_tupActiveRequestedDataAtTimeForPeers[iMid].Item1 <= dtmStartTimeExclusive)
                {
                    iSearchWindowMin = iMid + 1;
                }
                else
                {
                    iSearchWindowMax = iMid;
                }
            }

            for (int i = iSearchWindowMin; i < m_tupActiveRequestedDataAtTimeForPeers.Count; i++)
            {
                if (m_tupActiveRequestedDataAtTimeForPeers[i].Item1 <= dtmEndTimeInclusive)
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

        public DateTime GetCurrentSimTime()
        {
            //lock network time values

            return TimeNetworkProcessor.CalculateNetworkTime(m_tspNetworkTimeOffset, ref m_dtmNetworkOldestTime);
            
            //unlock network time values
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

            if( svaTime.CompareTo(m_svaOldestMessageToStoreInBuffer) < 0 )
            {
                m_squInMessageQueue.Clear();
                m_svaSimProcessedMessagesUpTo = m_svaOldestMessageToStoreInBuffer;
            }
            else
            {
                UpdateProcessedTimeOnNewMessageAdded(svaTime);
                m_squInMessageQueue.EnterPurgeInsert(svaTime, mprMessage);
            }            
        }

        public void QueuePlayerChangeMessage(SortingValue svaTime, UserConnecionChange uccConnectionChange)
        {
            if (svaTime.CompareTo(m_svaOldestMessageToStoreInBuffer) < 0)
            {
                m_squInMessageQueue.Clear();
                m_svaSimProcessedMessagesUpTo = m_svaOldestMessageToStoreInBuffer;
            }
            else
            {
                UpdateProcessedTimeOnNewMessageAdded(svaTime);
                m_squInMessageQueue.EnterPurgeInsert(svaTime, uccConnectionChange);
            }
        }

        public void UpdateProcessedTimeOnNewMessageAdded(SortingValue svaNewMessageTime)
        {
            if(svaNewMessageTime.CompareTo(m_svaSimProcessedMessagesUpTo) < 0)
            {
                m_svaSimProcessedMessagesUpTo = svaNewMessageTime;
            }
        }

        public void SetValidatedMesageBaseTime(SortingValue svaNewestConfimedMessageTime)
        {
            m_svaConfirmedMessageTime = svaNewestConfimedMessageTime;

            UpdateMessageTimeOut();
        }

        public void SetOldestActiveSimTime(SortingValue svaOldestActiveSimTime)
        {
            m_svaOldestActiveSimTime = svaOldestActiveSimTime;

            UpdateMessageTimeOut();

        }

        public void SetProcessedMessagesUpToTime(SortingValue svaProcessedMessagesUpTo)
        {
            m_svaSimProcessedMessagesUpTo = svaProcessedMessagesUpTo;

            UpdateMessageTimeOut();
        }
        
        public void UpdateMessageTimeOut()
        {
            SortingValue svaNewOldestMessageToStore = OldestValidMessageTime();

            if (m_svaOldestMessageToStoreInBuffer.CompareTo(svaNewOldestMessageToStore) != 0)
            {
                m_svaOldestMessageToStoreInBuffer = svaNewOldestMessageToStore;

                Clear(m_svaOldestMessageToStoreInBuffer);
            }
        }

        //remove inputs from buffer that will never be used again
        //TODO: maybe add some kind of archieving funcitonality 
        public void Clear(SortingValue svaClearUpTo)
        {
            m_squInMessageQueue.ClearTo(svaClearUpTo);
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

        public void GetIndexesBetweenTimes(DateTime dtmStartTime, DateTime dtmEndTime, out int iStartIndex, out int iEndIndex)
        {
            SortingValue svaStartValue = new SortingValue((ulong)dtmStartTime.Ticks, ulong.MaxValue);
            SortingValue svaEndValue = new SortingValue((ulong)dtmEndTime.Ticks + 1, ulong.MinValue);

            bool bStartIndexFound = m_squInMessageQueue.TryGetFirstIndexGreaterThan(svaStartValue, out iStartIndex);
            bool bEndIndexFound = m_squInMessageQueue.TryGetFirstIndexLessThan(svaEndValue, out iEndIndex);

            if(bStartIndexFound == false || bEndIndexFound == false)
            {
                iStartIndex = 0;
                iEndIndex = -1;
            }
        }

        public object[] GetMessagesFromData(DateTime dtmStartTime, DateTime dtmEndTime)
        {
            GetIndexesBetweenTimes(dtmStartTime, dtmEndTime, out int iStartIndex, out int iEndIndex);

            object[] objMessages = new object[(iEndIndex - iStartIndex) + 1];

            for (int i = 0; i < objMessages.Length; i++)
            {
                objMessages[i] = m_squInMessageQueue.GetValueAtIndex(iStartIndex + i);
            }

            return objMessages;
        }


        public SortingValue[] GetSortValuesFromData(DateTime dtmStartTime, DateTime dtmEndTime)
        {
            GetIndexesBetweenTimes(dtmStartTime, dtmEndTime, out int iStartIndex, out int iEndIndex);

            SortingValue[] svaSortValues = new SortingValue[(iEndIndex - iStartIndex) + 1];

            for (int i = 0; i < svaSortValues.Length; i++)
            {
                svaSortValues[i] = m_squInMessageQueue.GetKeyAtIndex(iStartIndex + i);

                //TODO::Wrap this in #defines or something, this should not run on server
                //loop through sorting values and try and back calculate the target tick
                DateTime dtmTimeOfInput = new DateTime((long)svaSortValues[i].m_lSortValueA);

                if (dtmStartTime.Ticks >= dtmTimeOfInput.Ticks)
                {
                    Debug.LogError("this input should have been in last tick");
                }

                if (dtmEndTime.Ticks < dtmTimeOfInput.Ticks)
                {
                    Debug.LogError("this input should have been in next tick");
                }
            }

            return svaSortValues;
        }

        public void UpdateProcessedMessageTime(DateTime dtmStartTimeExclusive, DateTime dtmEndTimeInclusive)
        {
            SortingValue svaFrom = new SortingValue((ulong)dtmStartTimeExclusive.Ticks, ulong.MaxValue);
            SortingValue svaTo = new SortingValue((ulong)dtmEndTimeInclusive.Ticks + 1, ulong.MinValue);

            //if the time processed up to is less than the start range of the values processed then 
            //dont update the processed up to value as there might be a message in the gap between 
            //what has been processed in the past and this update
            if(m_svaSimProcessedMessagesUpTo.CompareTo(svaFrom) <= 0 )
            {
                return;
            }

            //check that this is actually consuming new inputs
            if(m_svaSimProcessedMessagesUpTo.CompareTo(svaTo) >= 0)
            {
                return;
            }

            m_svaSimProcessedMessagesUpTo = svaTo;
        }

        //calculate the oldest time messages are needed
        public SortingValue OldestValidMessageTime()
        {
            //oldest time is a combination of what time the sim messages have been processed up to
            // the time that messages have been validated up to 
            // the time of ongoing sim state fetches 

            SortingValue svaOldestValidTime = m_svaConfirmedMessageTime;

            if (svaOldestValidTime.CompareTo(m_svaSimProcessedMessagesUpTo) > 0)
            {
                svaOldestValidTime = m_svaSimProcessedMessagesUpTo;
            }

            if (svaOldestValidTime.CompareTo(m_svaOldestActiveSimTime) < 1)
            {
                svaOldestValidTime = m_svaOldestActiveSimTime.NextSortValue();
            }

            //if the sim data sync has not succeded keep all messages from oldest time 
            if (m_sssSimStartStateSyncStatus == SimStateSyncNetworkProcessor.State.GettingStateData || m_sssSimStartStateSyncStatus == SimStateSyncNetworkProcessor.State.SyncFailed )
            {
                svaOldestValidTime = m_svaOldestActiveSimTime.NextSortValue();
            }

            return svaOldestValidTime;
        }

        //find all the messages that are nolonger needed / out of date and remove them
        public void RemoveOutdatedMessages(SortingValue svaOldesValidTime)
        {
            while(m_squInMessageQueue.Count > 0 && m_squInMessageQueue.PeakKeyDequeue().CompareTo(svaOldesValidTime) < 1)
            {
                m_squInMessageQueue.Dequeue(out SortingValue svaKey, out Object oObject);
            }
        }


    }
}
