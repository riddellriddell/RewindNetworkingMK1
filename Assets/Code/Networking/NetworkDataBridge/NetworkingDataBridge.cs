using SharedTypes;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Networking
{
    //this class acts as a bridge for other classes to send and receive data through the networking layer
    public class NetworkingDataBridge : ISimTimeProvider, ILocalPeerProvider
    {       
        //queue of all the outbound messages 
        public List<GlobalMessageBase> m_gmbOutMessageBuffer = new List<GlobalMessageBase>();

        //queue of all the inbound messages 
        public SortedRandomAccessQueue<SortingValue, IInput> m_squInMessageQueue = new SortedRandomAccessQueue<SortingValue, IInput>();
               
        //the time of the synchronization sync 
        public DateTime m_dtmSimStateSyncRequestTime = DateTime.MaxValue;

        //the state of the sim synchronization 
        public SimStateSyncNetworkProcessor.State m_sssSimStartStateSyncStatus = SimStateSyncNetworkProcessor.State.None;

        //the synchonised state of the sim at startup
        public byte[] m_bSimState = new byte[0];

        //has the data from the bridge been coppied into the sim for use
        public bool m_bIsThereDataOnBridgeForSimToInitWith = false;

        //what data the peers on the network are requesting  
        public List<Tuple<DateTime, long>> m_tupActiveRequestedDataAtTimeForPeers = new List<Tuple<DateTime, long>>();

        //new requests from peers
        //this get cleared out once user
        public List<Tuple<DateTime, long>> m_tupNewRequestedDataAtTimeForPeers = new List<Tuple<DateTime, long>>();

        //data for requests from peers
        public Dictionary<long,Tuple<DateTime, long, byte[]>> m_tupDataAtTimeForPeers = new Dictionary<long, Tuple<DateTime, long, byte[]>>();
       
        //timespan values used to shared time across all peers  
        public DateTime m_dtmNetworkOldestTime = DateTime.MinValue;

        public TimeSpan m_tspNetworkTimeOffset = TimeSpan.Zero;

        //time that all messages have been confirmed up to 
        public SortingValue m_svaConfirmedMessageTime = SortingValue.MinValue;

        //the oldest time in the sim that inputs are needed 
        public SortingValue m_svaOldestActiveSimTime = SortingValue.MinValue;

        //indicates the sim has processed all the messages up to this message
        public SortingValue m_svaSimProcessedMessagesUpToAndIncluding = SortingValue.MinValue;
        
        //the old sim processed up to time, this is used when re queuing messages so the same sequence of inputs does not 
        //get re simulated multiple times
        public SortingValue m_svaOldSimProcessedMessagesUpToAndIncluding = SortingValue.MinValue;

        //the most recent message added to the buffer
        //this is used to track changes to the buffer and catch messages that were deleted off the front of the buffer
        public SortingValue m_svaValidatedUpTo = SortingValue.MinValue;

        //no messages this old or older are allowed in the message buffer
        public SortingValue m_svaOldestMessageToStoreInBuffer;

        //network aware time source to pull synced time from
        public TimeNetworkProcessor m_tnpTimeNetworkProcessor;

        
        //get the number of messages on the bridge
        public int Count
        {
            get { return m_squInMessageQueue.Count; }
        }

        
        //TODO::Wrap this in a #define
        //DO Not Use In Simulation code
        //this is only for use when debugging
        public long m_lLocalPeerID;

        //returns an array of requests between the start time and the end time including times at the same time as the start and excluding items at the end time 
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

            return m_tnpTimeNetworkProcessor.CalculateNetworkTime(m_tspNetworkTimeOffset, ref m_dtmNetworkOldestTime);
            
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

        //queue this message discarding all the messages in the buffer that come after it
        public void QueueSimMessage(SortingValue svaTime, long lPeerID, int iChannelIndex, ISimMessagePayload smpMessage)
        {
            MessagePayloadWrapper mprMessage = new MessagePayloadWrapper()
            {
                m_lPeerID = lPeerID,
                m_iChannelIndex = iChannelIndex,
                m_smpPayload = smpMessage
            };

            QueueSimMessage(svaTime, mprMessage);
        }


        public void QueueSimMessage(SortingValue svaTime, in IInput inpInput)
        {
            //check if a player is changing before the start of the message queue
            if (svaTime < m_svaOldestMessageToStoreInBuffer)
            {
                m_squInMessageQueue.Clear();
                m_svaSimProcessedMessagesUpToAndIncluding = m_svaOldestMessageToStoreInBuffer;
            }
            else
            {
                UpdateProcessedTimeOnNewMessageAdded(svaTime);
                m_squInMessageQueue.EnterPurgeInsert(svaTime, inpInput);
                
                //update the validated up to time
                m_svaValidatedUpTo = svaTime;
            }
            
            //check if queuing up messages before start state
            if ((svaTime <= m_svaOldestActiveSimTime))
            {
                //convert from sort value to time
                DateTime dtmMessageTime = new DateTime((long)svaTime.m_lSortValueA , DateTimeKind.Utc);
                DateTime dtmOldestStateTime = new DateTime((long)m_svaOldestActiveSimTime.m_lSortValueA , DateTimeKind.Utc);
                
                TimeSpan tspDifference = dtmMessageTime - dtmOldestStateTime;
                
                //throw error as we are adding messages without a base state to process from
                Debug.LogError( $"NetworkdDataBridge.QueueSimMessage : Queuing up message on peer {m_lLocalPeerID} at time {dtmMessageTime.ToString("mm:ss.fff")} " +
                                $"before the oldest simulation state at time {dtmOldestStateTime.ToString("mm:ss.fff" )}, the message is before the state by {tspDifference.ToString()}" +
                                $"we will not be able to process this message");
            }
        }

        public void QueueSimMessageDepricated(SortingValue svaTime, in IInput inpInput)
        {
            
            //check if queuing up messages before start state
            if ((svaTime <= m_svaOldestActiveSimTime))
            {
                //throw error as we are adding messages without a base state to process from
                Debug.LogError( $"Queuing up message before the oldest simulation state, " +
                                $"we will not be able to process this message");
            }
            
            //check if a player is changing before the start of the message queue
            if (svaTime < m_svaOldestMessageToStoreInBuffer)
            {
                m_squInMessageQueue.Clear();
                m_svaSimProcessedMessagesUpToAndIncluding = m_svaOldestMessageToStoreInBuffer;
                
                //queue this message
                m_squInMessageQueue.EnterPurgeInsert( svaTime,inpInput);

                return;
            }

            
            //If this message is behind the "sim processed up to" then messages might have already been simulated
            if (svaTime <= m_svaSimProcessedMessagesUpToAndIncluding)
            {
                //store the sim processed up to value so we can later check if a message has been simulated
                m_svaOldSimProcessedMessagesUpToAndIncluding = m_svaSimProcessedMessagesUpToAndIncluding;

                m_svaSimProcessedMessagesUpToAndIncluding = svaTime.LastSortValue();
                
                //reset the most recent message time
                m_svaValidatedUpTo = svaTime;
            }

            bool bAlreadySimmulated = false;

            //check if this message is inside the simulated message window
            if (svaTime <= m_svaOldSimProcessedMessagesUpToAndIncluding )
            {

                //check if the previous message was processed by the sim
                //this checks if the message was already in the sim in which case the simulation would have simulated it
                //it also checks if there was a message between this and the last confirmed processed message, in which case 
                //this message would be flagged as not correctly processed 
                if (m_squInMessageQueue.TryGetIndexOf(svaTime, out int iNewMessageIndex) && iNewMessageIndex != 0 )
                {
                    SortingValue svaPreviousInputTime = m_squInMessageQueue.GetKeyAtIndex(iNewMessageIndex -1 );
                
                    // if this message exists and the previous message was processed and we are not past the old processed 
                    // up to time then this message would have also been processed so there is no need to nuke the message chain
                    if (svaPreviousInputTime <=  m_svaSimProcessedMessagesUpToAndIncluding)
                    {
                        m_svaSimProcessedMessagesUpToAndIncluding = svaTime;

                        bAlreadySimmulated = true;
                    }
                }
                
                //update the time of the most recent message
                //update the newest message time
                //this is assuming messages are added chronologically 
                //if we are outside of the simulated window then this value is not getting reset 
                //and this check is invalid and will give false positives when re evaluating messages
                //past the simulated up to time
                if (svaTime < m_svaValidatedUpTo)
                {
                    //throw error as we are adding messages without a base state to process from
                    Debug.LogError( "this code assumes we are adding messages chronologically " +
                                    "if we are hitting this then either messages are coming in out of order " + 
                                    "or we have not correctly reset for another batch of messages");

                    return; 
                }
            }

            //update the most recent message time
            m_svaValidatedUpTo = svaTime;
                
            //if the message already exists then we don't need to do anything
            if (!bAlreadySimmulated)
            {
                // remove all messages between this one and the sim processed up to time
                // this is to also remove any messages that might be between the simulated up to time
                // and this message
                m_squInMessageQueue.ClearFrom(m_svaSimProcessedMessagesUpToAndIncluding );
                    
                //queue this message
                m_squInMessageQueue.EnterPurgeInsert( svaTime,inpInput);
            }

        }
        
        //update the oldest message that is yet to be processed by the sim
        public void UpdateProcessedTimeOnNewMessageAdded(SortingValue svaNewMessageTime)
        {
            //check that the new time is less than the old processed up to time but is later than the state sync
            if((svaNewMessageTime < m_svaSimProcessedMessagesUpToAndIncluding) && (svaNewMessageTime > m_svaOldestActiveSimTime))
            {
                m_svaSimProcessedMessagesUpToAndIncluding = svaNewMessageTime;
            }
            else if( (svaNewMessageTime <= m_svaOldestActiveSimTime))
            {
                //throw error as we are adding messages without a base state to process from
                Debug.LogError( $"Peer {GetLocalPeerID()} is Queuing up message at sort value {svaNewMessageTime} " +
                                $"before the oldest simulation state {m_svaOldestActiveSimTime} , " +
                                $"we will not be able to process this message");
            }

            //This code has been moved to the new queue message function
            //if it is not revived make sure to remove it 
            
            // if( (svaNewMessageTime <= m_svaOldestActiveSimTime))
            // {
            //     //throw error as we are adding messages without a base state to process from
            //     Debug.LogError( $"Peer {GetLocalPeerID()} is Queuing up message at sort value {svaNewMessageTime} " +
            //                     $"before the oldest simulation state {m_svaOldestActiveSimTime} , " +
            //                     $"we will not be able to process this message");
            //
            //     return;
            // }
            //
            // //update the newest message time
            // //this is assuming messages are added chronologically 
            // if (svaNewMessageTime < m_svaLastMessageAddedToBuffer)
            // {
            //     //throw error as we are adding messages without a base state to process from
            //     Debug.LogError( "this code assumes we are adding messages chronologically " +
            //                     "if we are hitting this then either messages are coming in out of order " + 
            //                     "or we have not correctly reset for another batch of messages");
            //
            //     return;
            // }
            //
            // m_svaLastMessageAddedToBuffer = svaNewMessageTime;
            //
            // //check if we are past the head time
            // if (m_svaOldSimProcessedMessagesUpToAndIncluding < svaNewMessageTime)
            // {
            //     return;
            // }
            //
            // //check if this message is before the processed up to value, if it is then we can reset the processed up to head
            // if (svaNewMessageTime < m_svaSimProcessedMessagesUpToAndIncluding)
            // {
            //     m_svaOldSimProcessedMessagesUpToAndIncluding = m_svaSimProcessedMessagesUpToAndIncluding;
            //
            //     //get the time before this
            //     m_svaSimProcessedMessagesUpToAndIncluding = svaNewMessageTime.LastSortValue();
            // }
            //
            // //check if the previous message was processed by the sim
            // //this checks if the message was already in the sim in which case the simulation would have simulated it
            // //it also checks if there was a message between this and the last confirmed processed message, in which case 
            // //this message would be flagged as not correctly processed 
            // if (m_squInMessageQueue.TryGetIndexOf(svaNewMessageTime, out int iNewMessageIndex) && iNewMessageIndex != 0 )
            // {
            //     SortingValue svaPreviousInputTime = m_squInMessageQueue.GetKeyAtIndex(iNewMessageIndex -1 );
            //     
            //     // if this message exists and the previous message was processed and we are not past the old processed 
            //     // up to time then this message would have also been processed so there is no need to nuke the message chain
            //     if (svaPreviousInputTime <=  m_svaSimProcessedMessagesUpToAndIncluding)
            //     {
            //         m_svaSimProcessedMessagesUpToAndIncluding = svaNewMessageTime;
            //
            //         return;
            //     }
            // }
        }

        public void ResetValidationPointToTime(SortingValue svaResetTime)
        {
            if (svaResetTime <= m_svaSimProcessedMessagesUpToAndIncluding)
            {
                //If this message is behind the "sim processed up to" then messages might have already been simulated
                //store the sim processed up to value so we can later check if a message has been simulated
                m_svaOldSimProcessedMessagesUpToAndIncluding = m_svaSimProcessedMessagesUpToAndIncluding;

                m_svaSimProcessedMessagesUpToAndIncluding = svaResetTime;

                //reset the most recent message time
                m_svaValidatedUpTo = svaResetTime;
            }
        }
        
        public void RemoveUnvalidatedMessages()
        {
            m_squInMessageQueue.ClearFrom(m_svaValidatedUpTo);
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
            m_svaSimProcessedMessagesUpToAndIncluding = svaProcessedMessagesUpTo;

            UpdateMessageTimeOut();
        }
        
        public void UpdateMessageTimeOut()
        {
            SortingValue svaNewOldestMessageToStore = OldestValidMessageTime();

            if (m_svaOldestMessageToStoreInBuffer.CompareTo(svaNewOldestMessageToStore) != 0)
            {
                m_svaOldestMessageToStoreInBuffer = svaNewOldestMessageToStore;

                ClearTo(m_svaOldestMessageToStoreInBuffer);
            }
        }

        //remove inputs from buffer that will never be used again
        //TODO: maybe add some kind of archieving funcitonality 
        public void ClearTo(SortingValue svaClearUpTo)
        {
            m_squInMessageQueue.ClearTo(svaClearUpTo);
        }

        public void Clear()
        {
            m_squInMessageQueue.Clear();
        }

        public void UpdateSimStateAtTime(DateTime dtmTime, byte[] bSimData)
        {
            if(m_bSimState == null || m_bSimState.Length != bSimData.Length)
            {
                m_bSimState = new byte[bSimData.Length];
            }

            bSimData.CopyTo(m_bSimState, 0);

            m_bIsThereDataOnBridgeForSimToInitWith = true;
        }

        public void GetIndexesBetweenTimes(DateTime dtmStartTimeExclusive, DateTime dtmEndTimeInclusive, out int iStartIndex, out int iEndIndex)
        {
            SortingValue svaStartValue = new SortingValue((ulong)dtmStartTimeExclusive.Ticks, ulong.MaxValue);
            SortingValue svaEndValue = new SortingValue((ulong)dtmEndTimeInclusive.Ticks + 1, ulong.MinValue);

            bool bStartIndexFound = m_squInMessageQueue.TryGetFirstIndexGreaterThan(svaStartValue, out iStartIndex);
            bool bEndIndexFound = m_squInMessageQueue.TryGetFirstIndexLessThan(svaEndValue, out iEndIndex);

            if(bStartIndexFound == false || bEndIndexFound == false)
            {
                iStartIndex = 0;
                iEndIndex = -1;
            }
        }

        public IInput[] GetMessagesFromData(DateTime dtmStartTime, DateTime dtmEndTime)
        {
            GetIndexesBetweenTimes(dtmStartTime, dtmEndTime, out int iStartIndex, out int iEndIndex);

            IInput[] objMessages = new IInput[(iEndIndex - iStartIndex) + 1];

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
            //TODO::I think this might be wrong but I don't have time to check
            SortingValue svaFrom = new SortingValue((ulong)dtmStartTimeExclusive.Ticks, ulong.MaxValue);
            SortingValue svaTo = new SortingValue((ulong)dtmEndTimeInclusive.Ticks + 1, ulong.MinValue);

            //if the time processed up to is less than the start range of the values processed then 
            //don't update the processed up to value as there might be a message in the gap between 
            //what has been processed in the past and this update
            if(m_svaSimProcessedMessagesUpToAndIncluding < svaFrom)
            {
                return;
            }

            //check that this is actually consuming new inputs
            if(m_svaSimProcessedMessagesUpToAndIncluding > svaTo)
            {
                return;
            }

            m_svaSimProcessedMessagesUpToAndIncluding = svaTo;
        }

        //calculate the oldest time messages are needed
        public SortingValue OldestValidMessageTime()
        {
            //oldest time is a combination of what time the sim messages have been processed up to
            // the time that messages have been validated up to 
            // the time of ongoing sim state fetches 

            SortingValue svaOldestValidTime = m_svaConfirmedMessageTime;

            if (svaOldestValidTime > m_svaSimProcessedMessagesUpToAndIncluding)
            {
                svaOldestValidTime = m_svaSimProcessedMessagesUpToAndIncluding;
            }

            if (svaOldestValidTime <= m_svaOldestActiveSimTime)
            {
                svaOldestValidTime = m_svaOldestActiveSimTime.NextSortValue();
            }

            //if the sim data sync has not succeeded keep all messages from oldest time 
            if (m_sssSimStartStateSyncStatus == SimStateSyncNetworkProcessor.State.GettingStateData || m_sssSimStartStateSyncStatus == SimStateSyncNetworkProcessor.State.SyncFailed )
            {

                svaOldestValidTime =  m_svaOldestActiveSimTime.NextSortValue();

                // make sure we don't throw away inputs after our state request time
                SortingValue svaStateRequestTime = new SortingValue((ulong)m_dtmSimStateSyncRequestTime.Ticks, ulong.MinValue);
                
                if (svaOldestValidTime > svaStateRequestTime)
                {
                    svaOldestValidTime = svaStateRequestTime;
                }
            }

            return svaOldestValidTime;
        }

        //find all the messages that are nolonger needed / out of date and remove them
        public void RemoveOutdatedMessages(SortingValue svaOldesValidTime)
        {
            while(m_squInMessageQueue.Count > 0 && m_squInMessageQueue.PeakKeyDequeue().CompareTo(svaOldesValidTime) < 1)
            {
                m_squInMessageQueue.Dequeue(out SortingValue svaKey, out IInput oObject);
            }
        }

        public long GetLocalPeerID()
        {
            return m_lLocalPeerID;
        }
    }
}
