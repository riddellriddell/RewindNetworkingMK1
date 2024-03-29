﻿using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;

namespace Networking
{
    public class SimStateSyncNetworkProcessor : ManagedNetworkPacketProcessor<SimStateSyncConnectionProcessor>
    {
        //the maximum size a single segment of the sim state will be
        public int MaxSegmentSize { get; private set; }

        //if all active peers agree on a state there should be no chance for the state to be changed by another input
        //so we can end the state fetch early but just in case we want to not finalize the state before this time
        public TimeSpan MinStateDeliveryTime { get; private set; }

        //how long from deciding on a state hash to fetch to when all data for that state has been delivered
        public TimeSpan StateRequestTimeOut { get; private set; }

        //how long each peer has to return a state segment assigned to them, this is to stop one peer not returning a segment for ages
        //and holding up
        public TimeSpan SegmentRequestTimeOut { get; private set; }

        public float MaxFailedRequestPercent { get; private set; }

        public enum State
        {
            None,
            GettingStateData,
            StateSynced,
            SyncFailed
        }

        public override int Priority { get; } = 15;

        public State m_staState = State.None;

        //has the sim state byte array been fully filled? note this does not mean the state is the final / reliable state
        public bool m_bIsFullStateSynced = false;

        //has the hash of the sim state changes on one of the peers 
        public bool m_bIsConnectedPeerStateHashDirty = false;

        //as a peer finished / failed / become available to suply a segment of data 
        public bool m_bIsStateSegmentAsignmentDirty = false;

        public bool m_bIsRequestedOutDataDirty = false;

        //all the peers that are active in the global messaging system
        public List<long> m_lAuthorativePeers;

        //the hash of the sim state that the majority of peers in the global messaging system
        //believe is the state of the sim when local peer joined the messaging system
        public long m_lAgreedSimHash;

        //the number of peers with matching sim hashes 
        public int m_iPeersWithSimHash;

        //hash of each of the segments of the chain 
        public long[] m_lSimDataSegmentsHash;

        //the number of bytes in the sim state
        public uint m_iSimStateSize;

        //sim state payload
        public byte[] m_bSimState;

        //the indexes of all the data segments in the sim state that are yet to be recieved 
        public HashSet<ushort> m_sPendingSegments;

        //the indexes of all the segments yet to be assigned to a peer
        public HashSet<ushort> m_sUnAssignedSegments;

        //the time of the state requested by the local peer
        public DateTime m_dtmTimeOfAgreedState;

        //the time by which this request needs to be filled on not filled 
        public DateTime m_dtmRequestTimeOut;

        //if all peers agree on the state hash then this time is set to MinStateDeliveryTime in the future
        //and if all peers still agree at that time then the state is considerd delivered 
        public DateTime m_dtmEarlyStateDeliveryTime;

        //when will the next request for in data time out
        public DateTime m_dtmTimeOfNextInDataTimeOut;

        //when will the next request for outbound data time out
        public DateTime m_dtmTimeOfNextOutDataTimeOut;

        //a sorted list of all the sim data times requested
        public List<Tuple<DateTime, long>> m_lRequestedSimDataTimes;

        //network time tracker
        public TimeNetworkProcessor m_tnpNetworkTime;

        //the global messaging processor
        //this is used to check if the local player is clasified as an authorative peer
        public NetworkGlobalMessengerProcessor m_gmpGlobalMessageProcessor;

        //data structure for transfering information between the networking layer and the sim managment components
        public NetworkingDataBridge m_ndbNetworkDataBridge;

        public SimStateSyncNetworkProcessor(NetworkingDataBridge ndbNetworkDataBridge): base()
        {
            m_ndbNetworkDataBridge = ndbNetworkDataBridge;
        }

        public override void ApplyNetworkSettings(NetworkConnectionSettings ncsSettings)
        {
            base.ApplyNetworkSettings(ncsSettings);

            MaxSegmentSize = ncsSettings.m_iMaxSegmentSize;
            StateRequestTimeOut = TimeSpan.FromSeconds(ncsSettings.m_fStateRequestTimeOut);
            MinStateDeliveryTime = TimeSpan.FromSeconds(ncsSettings.m_fMinStateDeliveryTime);
            SegmentRequestTimeOut = TimeSpan.FromSeconds(ncsSettings.m_fSegmentRequestTimeOut);
            MaxFailedRequestPercent = ncsSettings.m_fMaxFailedRequestPercent;
        }

        public override void Update()
        {
            //-------- Out Data Management -----------------

            //update requests for sim data
            UpdateOutDataTimeOut();

            //check if any requests for data have timed out or if new requests have been added 
            if (m_bIsRequestedOutDataDirty)
            {
                m_bIsRequestedOutDataDirty = false;

                //tell the sim manager the timestamps for the requested data and what peers want that data
                m_ndbNetworkDataBridge.m_tupActiveRequestedDataAtTimeForPeers = GetRequestedTimeOfSimStates();
            }

            //check if any new states have been added to the data bridge 
            //this happens when state data exists for a request at a time
            if (m_ndbNetworkDataBridge.m_tupDataAtTimeForPeers.Count > 0)
            {
                Debug.Log("SimStateSync:: Festching Data For Peers From Network Data Bridge");

                //check if there are any active requests 
                foreach (Tuple<DateTime, long, byte[]> tupDataAtTime in m_ndbNetworkDataBridge.m_tupDataAtTimeForPeers.Values)
                {
                    //set data for peer
                    SetSimDataForPeer(tupDataAtTime.Item2, tupDataAtTime.Item1, tupDataAtTime.Item3);
                }

                m_ndbNetworkDataBridge.m_tupDataAtTimeForPeers.Clear();
            }

            //-------------- In Data Management --------------------------

            //check if peer is syncing state 
            if (m_staState != State.GettingStateData)
            {
                return;
            }

            //check the percent of peers that agree on the state at a tick
            CheckForEarlyStateSync();

            if (m_staState != State.GettingStateData)
            {
                return;
            }

            //check if state sync has timed out 
            UpdateTimeOutState();

            if (m_staState != State.GettingStateData)
            {
                return;
            }

            //TODO:: 5/11/22 remove this if not in use. at the moment we are not tracking if a peer does not have a state 
            //check if not enough peers dont have state for time
            //UpdatePeerState();

            //if (m_staState != State.GettingStateData)
            //{
            //    return;
            //}

            //update peer hash states 
            if (m_bIsConnectedPeerStateHashDirty)
            {
                m_bIsConnectedPeerStateHashDirty = false;

                //check if the most common sim data hash has changed 
                if (CheckForMostLikelyHashChange())
                {
                    //clear out existing data and restart sourcing sim data segments 
                    ResetSimDataOnHashChange();

                    //flag that new segment download requests need to be sent out
                    m_bIsStateSegmentAsignmentDirty = true;
                }
            }

            //check if any requests to peers have timed out
            UpdateSegmentTimeOut();

            //check if any peers have delivered their assigned segments and 
            //can be assigned new segments to send 
            if (m_bIsStateSegmentAsignmentDirty)
            {
                AssignSegmentsToAvailablePeers();

                //mark segment assignemnt as nolonger dirty 
                m_bIsStateSegmentAsignmentDirty = false;
            }

        }

        protected override void AddDependentPacketsToPacketFactory(ClassWithIDFactory cifPacketFactory)
        {
            //add all the data packet classes this processor relies on to the main class factory 
            SimStateSyncRequestPacket.TypeID = ParentNetworkConnection.PacketFactory.AddType<SimStateSyncRequestPacket>(SimStateSyncRequestPacket.TypeID);
            SimStateSyncHashMapPacket.TypeID = ParentNetworkConnection.PacketFactory.AddType<SimStateSyncHashMapPacket>(SimStateSyncHashMapPacket.TypeID);
            SimSegmentSyncRequestPacket.TypeID = ParentNetworkConnection.PacketFactory.AddType<SimSegmentSyncRequestPacket>(SimSegmentSyncRequestPacket.TypeID);
            SimSegmentSyncDataPacket.TypeID = ParentNetworkConnection.PacketFactory.AddType<SimSegmentSyncDataPacket>(SimSegmentSyncDataPacket.TypeID);
        }

        public override void OnAddToNetwork(NetworkConnection ncnNetwork)
        {
            base.OnAddToNetwork(ncnNetwork);

            m_tnpNetworkTime = ParentNetworkConnection.GetPacketProcessor<TimeNetworkProcessor>();

            //set start valuse 
            m_dtmRequestTimeOut = DateTime.MinValue;

            //set when current system will time out 
            m_dtmTimeOfNextInDataTimeOut = DateTime.MaxValue;
        }

        public override DataPacket ProcessReceivedPacket(long lFromUserID, DataPacket pktInputPacket)
        {
            if (pktInputPacket is SimSegmentSyncDataPacket)
            {
                SimSegmentSyncDataPacket ssdSegmentData = pktInputPacket as SimSegmentSyncDataPacket;

                //DO SOME BASIC VALIDATION
                if(m_dtmTimeOfAgreedState.Ticks != ssdSegmentData.m_lTickOfGameState)
                {
                    Debug.LogError($"The state sent by peer{lFromUserID} is for tick {ssdSegmentData.m_lTickOfGameState} but the request is for time {m_dtmTimeOfAgreedState.Ticks}");
                }

                OnRecieveSegment(ref ssdSegmentData.m_bSegmentData, ssdSegmentData.m_lSegmentHash, lFromUserID);

                return null;
            }

            return pktInputPacket;
        }

        //checks to see if state sync time has run out and if it has if state was sucesfully synced 
        public void UpdateTimeOutState()
        {
            if (m_tnpNetworkTime.BaseTime > m_dtmRequestTimeOut)
            {
                //clean up peers
                foreach (SimStateSyncConnectionProcessor sscSyncConnection in ChildConnectionProcessors.Values)
                {
                    sscSyncConnection.CleanUpRecievingState();
                }

                //check if full state was downloaded 
                if (m_sPendingSegments.Count > 0)
                {
                    m_staState = State.SyncFailed;
                    m_ndbNetworkDataBridge.m_sssSimStartStateSyncStatus = m_staState;

                    Debug.LogError("Sync failed, did not download all segments before time ran out");

                    return;
                }

                int iMaxNumberOfFailedRequests = Mathf.CeilToInt(m_lAuthorativePeers.Count * MaxFailedRequestPercent);

                //check if enough peers aggreed upon state
                if (m_iPeersWithSimHash < m_lAuthorativePeers.Count - iMaxNumberOfFailedRequests)
                {
                    m_staState = State.SyncFailed;
                    m_ndbNetworkDataBridge.m_sssSimStartStateSyncStatus = m_staState;

                    Debug.LogError("Sync failed, final synchronizations was not stable, not enough peers egreed with final state");

                    return;
                }

                m_staState = State.StateSynced;
                m_ndbNetworkDataBridge.m_sssSimStartStateSyncStatus = m_staState;
            }
        }
        
        //check if the state has been recieved from all peers and they all agree on the state so there is no need to wait any longer
        public void CheckForEarlyStateSync()
        {
            //how many peers should have the same state hash to early complete connection 
            int number_of_required_matches = m_lAuthorativePeers.Count;

            //check if local peer counts as an authorative peer 
           if(m_lAuthorativePeers.Contains(ParentNetworkConnection.m_lPeerID))
           {
                //don't count the local peer for the number of peers that need to have the same state
                number_of_required_matches -= 1;
           }

            //check if full state was downloaded 
            if (m_sPendingSegments.Count == 0 && m_iPeersWithSimHash >= number_of_required_matches)
            {
                //wait for a fixed period of time in case a peer changes their mind about the state;
                if (m_dtmEarlyStateDeliveryTime > m_tnpNetworkTime.BaseTime)
                {
                    //clean up peers
                    foreach (SimStateSyncConnectionProcessor sscSyncConnection in ChildConnectionProcessors.Values)
                    {
                        sscSyncConnection.CleanUpRecievingState();
                    }

                    Debug.Log("State synced early");

                    //show that state is synced 
                    m_staState = State.StateSynced;
                    m_ndbNetworkDataBridge.m_sssSimStartStateSyncStatus = m_staState;
                }
            }
            else
            {
                //reset time since all peers have agreed 
                m_dtmEarlyStateDeliveryTime = m_tnpNetworkTime.BaseTime + MinStateDeliveryTime;
            }
        }

        //check peers to see if peers have state available 
        //TODO:: this currently does not do anything because the ony sync states used are not requested and recieved 
        //public void UpdatePeerState()
        //{
        //    int iFailedRequests = 0;
        //
        //    for (int i = 0; i < m_lAuthorativePeers.Count; i++)
        //    {
        //        if (ChildConnectionProcessors.TryGetValue(m_lAuthorativePeers[i], out SimStateSyncConnectionProcessor sscStateSync))
        //        {
        //            if (sscStateSync.m_istInState == SimStateSyncConnectionProcessor.InState.NotAvailable)
        //            {
        //                iFailedRequests++;
        //            }
        //        }
        //    }
        //
        //    int iMaxNumberOfFailedRequests = Mathf.CeilToInt(m_lAuthorativePeers.Count * MaxFailedRequestPercent);
        //
        //    if (iFailedRequests >= iMaxNumberOfFailedRequests)
        //    {
        //        Debug.LogError("Not enough peers have state available, sync failed" );
        //        m_staState = State.SyncFailed;
        //    }
        //}

        //check if connection is on the list of authorative peers
        public bool IsPeerInAuthorativeList(Connection conConnection)
        {
            return m_lAuthorativePeers.Contains(conConnection.m_lUserUniqueID);
        }

        //resets the local hash state and fires request to peers for new state segment hash list
        public void RequestSimData(DateTime dtmTargetTime, List<long> lAuthorativePeers)
        {
            if (m_lAuthorativePeers == null)
            {
                m_lAuthorativePeers = new List<long>();
            }
            else
            {
                m_lAuthorativePeers.Clear();
            }

            m_lAuthorativePeers.AddRange(lAuthorativePeers);

            //set time of state 
            m_dtmTimeOfAgreedState = dtmTargetTime;

            //set time of request timeout
            m_dtmRequestTimeOut = m_tnpNetworkTime.BaseTime + StateRequestTimeOut;

            m_staState = State.GettingStateData;

            m_lAgreedSimHash = 0;

            m_sPendingSegments = new HashSet<ushort>();

            m_sUnAssignedSegments = new HashSet<ushort>();

            m_iPeersWithSimHash = 0;

            m_iSimStateSize = 0;

            m_bIsConnectedPeerStateHashDirty = false;

            //send request to peers for data 
            for (int i = 0; i < m_lAuthorativePeers.Count; i++)
            {
                if (ChildConnectionProcessors.TryGetValue(m_lAuthorativePeers[i], out SimStateSyncConnectionProcessor sscConnection))
                {
                    if (sscConnection.ParentConnection.Status == Connection.ConnectionStatus.Connected)
                    {
                        sscConnection.RequestSimState();
                    }
                }
            }

            //tell network data bride data syncing has started 
            m_ndbNetworkDataBridge.m_sssSimStartStateSyncStatus = m_staState;
            m_ndbNetworkDataBridge.m_bIsThereDataOnBridgeForSimToInitWith = false;
            m_ndbNetworkDataBridge.m_dtmSimStateSyncRequestTime = m_dtmTimeOfAgreedState;

        }

        //public void Calculate the most common hash for game state and return true if hash has changed 
        public bool CheckForMostLikelyHashChange()
        {
            List<Tuple<long, SimStateSyncConnectionProcessor>> lHashOptions = new List<Tuple<long, SimStateSyncConnectionProcessor>>(m_lAuthorativePeers.Count);

            //loop through all the peers and find the ones that have sent a hash map
            //add the hashes to an array
            for (int i = 0; i < m_lAuthorativePeers.Count; i++)
            {
                if (ChildConnectionProcessors.TryGetValue(m_lAuthorativePeers[i], out SimStateSyncConnectionProcessor sscStateSync))
                {
                    if (sscStateSync.m_istInState == SimStateSyncConnectionProcessor.InState.Recieved)
                    {
                        lHashOptions.Add(new Tuple<long, SimStateSyncConnectionProcessor>(sscStateSync.m_lInTotalStateHash, sscStateSync));
                    }
                }
            }

            lHashOptions.Sort((a, b) => (int)(a.Item1 - b.Item1));

            long lCommonStateHash = 0;
            int iCommonStateCount = 0;
            SimStateSyncConnectionProcessor sscPeerWithCommonState = null;

            long lCurrentStateHash = 0;
            int iCurrentStateCount = 0;

            for (int i = 0; i < lHashOptions.Count; i++)
            {
                if (lHashOptions[i].Item1 != lCurrentStateHash)
                {
                    //check if there is no chance for this state to be the common state 
                    if (iCommonStateCount > lHashOptions.Count - i)
                    {
                        break;
                    }

                    lCurrentStateHash = lHashOptions[i].Item1;

                    iCurrentStateCount = 1;
                }
                else
                {
                    iCurrentStateCount++;
                }

                if (iCurrentStateCount > iCommonStateCount)
                {
                    sscPeerWithCommonState = lHashOptions[i].Item2;
                    lCommonStateHash = lCurrentStateHash;
                    iCommonStateCount = iCurrentStateCount;
                }
            }

            //check if most common hash has changed 
            if (m_lAgreedSimHash == lCommonStateHash)
            {
                //active hash has not changed
                Debug.Log("Common hash has not changed");
                return false;
            }

            //coppy accross the new hash state 
            m_lAgreedSimHash = lCommonStateHash;
            m_iPeersWithSimHash = iCommonStateCount;

            //coppy across segment hash array 
            if (m_lSimDataSegmentsHash == null || m_lSimDataSegmentsHash.Length != sscPeerWithCommonState.m_lInSimDataSegmentsHash.Length)
            {
                m_lSimDataSegmentsHash = new long[sscPeerWithCommonState.m_lInSimDataSegmentsHash.Length];
            }

            sscPeerWithCommonState.m_lInSimDataSegmentsHash.CopyTo(m_lSimDataSegmentsHash, 0);

            m_iSimStateSize = sscPeerWithCommonState.m_iTotalByteCount;

            return true;
        }

        //sets up data structures for recieving a new state 
        public void ResetSimDataOnHashChange()
        {
            //create and fill missig segment indexes map
            if (m_sPendingSegments == null)
            {
                m_sPendingSegments = new HashSet<ushort>();
            }
            else
            {
                m_sPendingSegments.Clear();
            }

            if (m_sUnAssignedSegments == null)
            {
                m_sUnAssignedSegments = new HashSet<ushort>();
            }
            else
            {
                m_sUnAssignedSegments.Clear();
            }

            if (m_lSimDataSegmentsHash == null)
            {
                m_lSimDataSegmentsHash = new long[0];
            }

            for (ushort i = 0; i < m_lSimDataSegmentsHash.Length; i++)
            {
                m_sPendingSegments.Add(i);
                m_sUnAssignedSegments.Add(i);
            }

            //setup array to recieve the data 
            if (m_bSimState == null || m_bSimState.Length != m_iSimStateSize)
            {
                m_bSimState = new byte[m_iSimStateSize];
            }

            m_bIsFullStateSynced = false;
        }

        //reset all the requests for segments on target sim data hash change 
        public void ResetConnectionRequestsOnHashChange()
        {
            foreach (SimStateSyncConnectionProcessor sscSegmentSyncConnection in ChildConnectionProcessors.Values)
            {
                sscSegmentSyncConnection.m_sRequestedSegment = ushort.MaxValue;
            }
        }

        //when data is recieved this funciton works out what segment it belongs to and 
        //copies the data into the state byte array
        public void OnRecieveSegment(ref byte[] bData, long lHashOfData, long lPeer)
        {
            //check if state not syncing 
            if (m_staState != State.GettingStateData)
            {
                return;
            }

            //check segment size 
            if (bData.Length > MaxSegmentSize)
            {
                //bad segment size 
                Debug.LogError($"Sim state segement too big? corrupted or hack attempt from peer{lPeer}?");

                //TODO: Throw an error of some kind and mayb trigger a kick action
                return;
            }

            long lSegmentHash = 0;

            using (MD5 md5Hash = MD5.Create())
            {
                //generate hash
                byte[] bHash = md5Hash.ComputeHash(bData);

                //convert hash to long for convienience 
                lSegmentHash = BitConverter.ToInt64(bHash, 0);
            }

            ushort sDataSegment = ushort.MaxValue;

            //search for corresponding segment 
            for (ushort i = 0; i < m_lSimDataSegmentsHash.Length; i++)
            {
                if (m_lSimDataSegmentsHash[i] == (lSegmentHash ^ i))
                {
                    sDataSegment = i;
                    break;
                }
            }

            //check if a segment was found 
            if (sDataSegment == ushort.MaxValue)
            {
                //is the hash of the message a match to anything
                int iHashIndex = -1;

                //check if hash matcher anything
                for (ushort i = 0; i < m_lSimDataSegmentsHash.Length; i++)
                {
                    if (m_lSimDataSegmentsHash[i] == (lHashOfData))
                    {
                        iHashIndex = i;
                        break;
                    }
                }

                //data does not belong to current sim state 
                Debug.LogError($"Sim state segement from peer{lPeer} with hash {lSegmentHash} does not belong to target sim state but the message hash of {lHashOfData} can be found in the hash map at index {iHashIndex}");

                return;
            }

            //check if segment has already been recieved
            if (m_sPendingSegments.Contains(sDataSegment) == false)
            {
                // data segment already recieved
                Debug.LogError($"Sim state segement from peer{lPeer} already recieved");

                return;
            }

            //indicate that segment has been recieved 
            m_sPendingSegments.Remove(sDataSegment);

            //indicate that it should not be assigned to a peer to send to local peer
            m_sUnAssignedSegments.Remove(sDataSegment);

            //copy accross data 
            bData.CopyTo(m_bSimState, sDataSegment * MaxSegmentSize);

            //update assigned segment
            if (ChildConnectionProcessors.TryGetValue(lPeer, out SimStateSyncConnectionProcessor sscConnectionSync))
            {
                //clear assigned segments 
                sscConnectionSync.OnSegmentRequestFilled(sDataSegment);
            }

            //check if all data segments have been recieved 
            if (m_sPendingSegments.Count == 0)
            {
                //log all states recieved
                Debug.Log($"All stim state segements recieved for state at time:{m_dtmTimeOfAgreedState}");

                m_bIsFullStateSynced = true;

                //update the data in the sim segment buffer 
                m_ndbNetworkDataBridge.UpdateSimStateAtTime(m_dtmTimeOfAgreedState, m_bSimState);

                //TODO: fire some kind of event telling sim to get new state 
            }

            //indicate segment assignemnt needs to be updated
            m_bIsStateSegmentAsignmentDirty = true;
        }

        public void ReturnRequestToPool(ushort sSegmentRequest)
        {
            //check if segment has already been delivered 
            if (m_sPendingSegments.Contains(sSegmentRequest))
            {
                m_sUnAssignedSegments.Add(sSegmentRequest);
            }
        }

        public void UpdateSegmentAssignment()
        {
            //check if any segment requests have not been filled 
            UpdateSegmentTimeOut();

        }

        // if there are any segments yet to be recieved this function assignes the segment to 
        // a free peer that is authorative and does not currently have a segment assigned
        public void AssignSegmentsToAvailablePeers()
        {
            //check if there are segments left to assign
            if (m_sUnAssignedSegments.Count > 0)
            {
                foreach (SimStateSyncConnectionProcessor sscStateSyncConnection in ChildConnectionProcessors.Values)
                {
                    //check if peer has data and is not already assigned a segment 
                    if (sscStateSyncConnection.ParentConnection.Status == Connection.ConnectionStatus.Connected &&
                        sscStateSyncConnection.m_istInState == SimStateSyncConnectionProcessor.InState.Recieved &&
                        sscStateSyncConnection.m_lInTotalStateHash == m_lAgreedSimHash &&
                        sscStateSyncConnection.m_sRequestedSegment == ushort.MaxValue)
                    {
                        //get next segment 
                        HashSet<ushort>.Enumerator enmEnumerator = m_sUnAssignedSegments.GetEnumerator();

                        enmEnumerator.MoveNext();

                        ushort sSegmentIndex = enmEnumerator.Current;

                        //remove from unassigned pool
                        m_sUnAssignedSegments.Remove(sSegmentIndex);

                        //the time when this request will time out
                        DateTime dtmRequestTimeOut = m_tnpNetworkTime.BaseTime + SegmentRequestTimeOut;

                        //update the next request to time out 
                        m_dtmTimeOfNextInDataTimeOut = new DateTime(Math.Min(m_dtmTimeOfNextInDataTimeOut.Ticks, dtmRequestTimeOut.Ticks));

                        //apply that to peer 
                        sscStateSyncConnection.OnAssignSegment(sSegmentIndex, dtmRequestTimeOut);

                        //check if all segments have been assigned 
                        if (m_sUnAssignedSegments.Count == 0)
                        {
                            break;
                        }
                    }
                }
            }
        }

        public void UpdateSegmentTimeOut()
        {
            //get current time
            DateTime dtmBaseTime = m_tnpNetworkTime.BaseTime;

            //check for any requests that have timed out 
            if (dtmBaseTime > m_dtmTimeOfNextInDataTimeOut)
            {
                m_dtmTimeOfNextInDataTimeOut = DateTime.MaxValue;

                foreach (SimStateSyncConnectionProcessor sscStateSyncConnections in ChildConnectionProcessors.Values)
                {
                    //update the timeout for any channels 
                    sscStateSyncConnections.UpdateInDataRequestTimeout(ref m_dtmTimeOfNextInDataTimeOut, dtmBaseTime);
                }

                m_bIsStateSegmentAsignmentDirty = true;
            }
        }

        public void UpdateOutDataTimeOut()
        {
            //get current time
            DateTime dtmNetworkTime = m_tnpNetworkTime.NetworkTime;

            //check for any requests that have timed out 
            if (dtmNetworkTime > m_dtmTimeOfNextInDataTimeOut)
            {
                m_dtmTimeOfNextInDataTimeOut = DateTime.MaxValue;

                foreach (SimStateSyncConnectionProcessor sscStateSyncConnections in ChildConnectionProcessors.Values)
                {
                    //update the timeout for any channels 
                    sscStateSyncConnections.UpdateOutDataRequestTimeOut(ref m_dtmTimeOfNextInDataTimeOut, dtmNetworkTime);
                }

                m_bIsRequestedOutDataDirty = true;
            }
        }

        //get a list of all the times for all the requestd sim states and the peers that requested them
        public List<Tuple<DateTime, long>> GetRequestedTimeOfSimStates()
        {
            List<Tuple<DateTime, long>> dtmSimTimes = new List<Tuple<DateTime, long>>();

            foreach (SimStateSyncConnectionProcessor sscConnection in ChildConnectionProcessors.Values)
            {
                if (sscConnection.m_ostOutState == SimStateSyncConnectionProcessor.OutState.Pending || sscConnection.m_ostOutState == SimStateSyncConnectionProcessor.OutState.Active)
                {
                    dtmSimTimes.Add(new Tuple<DateTime, long>(sscConnection.m_dtmTimeOfOutSimState, sscConnection.ParentConnection.m_lUserUniqueID));
                }
            }

            dtmSimTimes.Sort((x, y) => (x.Item1.CompareTo(y.Item1)));

            return dtmSimTimes;
        }

        public void SetSimDataForPeer(long lPeerID, DateTime dtmTargetTime, byte[] bData)
        {
            if (ChildConnectionProcessors.TryGetValue(lPeerID, out SimStateSyncConnectionProcessor sscConnection))
            {
                if (sscConnection.m_dtmTimeOfOutSimState == dtmTargetTime && sscConnection.m_ostOutState != SimStateSyncConnectionProcessor.OutState.NotRequested)
                {
                    sscConnection.OnSimDataChange(bData);
                }
            }
        }

        public void OnNewRequestForSimDataAtTime( DateTime dtmTimeOfRequest, long lNewRequestFromPeerID)
        {         
            DateTime dtmTimeOutTime = dtmTimeOfRequest + this.StateRequestTimeOut;

            //update the time of the next state time out
            if (m_dtmTimeOfNextOutDataTimeOut > dtmTimeOutTime)
            {
                m_dtmTimeOfNextOutDataTimeOut = dtmTimeOutTime;
            }

             m_bIsRequestedOutDataDirty = true;

            Debug.Log("SimStateSync:: adding new request for sim data to network data bridge");

            //add the time of the data request to the data bridge so the sim manager knows when to copy out sim state 
            m_ndbNetworkDataBridge.m_tupNewRequestedDataAtTimeForPeers.Add(new Tuple<DateTime, long>(dtmTimeOfRequest, lNewRequestFromPeerID));
        }
    }

    public class SimStateSyncConnectionProcessor : ManagedConnectionPacketProcessor<SimStateSyncNetworkProcessor>
    {
        public enum OutState
        {
            NotRequested, //no request has beeen made yet
            Pending, //there is a request for a state but that state has not been generated yet
            Active //a request has been made and this peer has the data
        }

        public enum InState
        {
            NotRequested,
            
            //TODO:: remvoe these if they are never used at the moment the instate is initalized to not requested and 
            //only set to received once a state has arrived 
            //Pending,
            //NotAvailable,
            
            Recieved
        }

        public override int Priority { get; } = 15;

        #region SendingState

        public OutState m_ostOutState;

        //the state of the sim when the peer requested sim state hash 
        public byte[] m_bSimDataAtPeerRequest;

        //hash of each of the segments of the chain 
        public long[] m_lOutSimDataSegmentsHash;

        public DateTime m_dtmTimeOfOutSimState;

        #endregion

        #region RecievingState 

        //the state of the data sync
        public InState m_istInState;

        //the time the segment request was made 
        public DateTime m_dtmRequestTimeOut;

        //the hash of the entire state sent by peer 
        //this value is calculated locally based of data segment hash
        public long m_lInTotalStateHash;

        //hash of each of the segments of the chain 
        public long[] m_lInSimDataSegmentsHash;

        //the size of the sim state that the peer has 
        public uint m_iTotalByteCount;

        //the segment the local peer has requsted the remote peer send
        public ushort m_sRequestedSegment;

        #endregion

        public override void Start()
        {
            base.Start();

            #region SendingState

            m_ostOutState = OutState.NotRequested;

            //the state of the sim when the peer requested sim state hash 
            m_bSimDataAtPeerRequest = new byte[0];

            //hash of each of the segments of the chain 
            m_lOutSimDataSegmentsHash = new long[0];

            m_dtmTimeOfOutSimState = DateTime.MinValue;

            #endregion

            #region RecievingState 

            //the state of the data sync
            m_istInState = InState.NotRequested;

            //the time the segment request was made 
            m_dtmRequestTimeOut = DateTime.MinValue;

            //the hash of the entire state sent by peer 
            //this value is calculated locally based of data segment hash
            m_lInTotalStateHash = 0;

            //hash of each of the segments of the chain 
            m_lInSimDataSegmentsHash = new long[0];

            //the size of the sim state that the peer has 
            m_iTotalByteCount = 0;

            //the segment the local peer has requsted the remote peer send
            m_sRequestedSegment = ushort.MaxValue;

            #endregion
        }
        
        public override void OnConnectionReset()
        {
            #region SendingState

            m_ostOutState = OutState.NotRequested;

            //the state of the sim when the peer requested sim state hash 
            m_bSimDataAtPeerRequest = new byte[0];

            //hash of each of the segments of the chain 
            m_lOutSimDataSegmentsHash = new long[0];

            m_dtmTimeOfOutSimState = DateTime.MinValue;

            #endregion

            #region RecievingState 

            //the state of the data sync
            m_istInState = InState.NotRequested;

            //the time the segment request was made 
            m_dtmRequestTimeOut = DateTime.MinValue;

            //the hash of the entire state sent by peer 
            //this value is calculated locally based of data segment hash
            m_lInTotalStateHash = 0;

            //hash of each of the segments of the chain 
            m_lInSimDataSegmentsHash = new long[0];

            //the size of the sim state that the peer has 
            m_iTotalByteCount = 0;

            //the segment the local peer has requsted the remote peer send
            m_sRequestedSegment = ushort.MaxValue;
            #endregion
        }

        public override void OnConnectionStateChange(Connection.ConnectionStatus cstOldState, Connection.ConnectionStatus cstNewState)
        {
            switch (cstNewState)
            {
                case Connection.ConnectionStatus.Connected:
                    //check if peer needs to send sim state data 
                    HandleNewConnectionWhenRecieving();
                    break;
                case Connection.ConnectionStatus.Disconnected:

                    //clear any requests for game states
                    m_dtmTimeOfOutSimState = DateTime.MinValue;

                    m_ostOutState = OutState.NotRequested;

                    m_bSimDataAtPeerRequest = new byte[0];

                    break;
            }
        }

        public override DataPacket ProcessReceivedPacket(Connection conConnection, DataPacket pktInputPacket)
        {
            if (pktInputPacket is SimStateSyncRequestPacket)
            {
                SimStateSyncRequestPacket srsRequestState = pktInputPacket as SimStateSyncRequestPacket;

                OnRequestStateForTime(srsRequestState.m_dtmTimeOfSimData);

                return null;
            }
            else if (pktInputPacket is SimStateSyncHashMapPacket)
            {
                SimStateSyncHashMapPacket shmHashMap = pktInputPacket as SimStateSyncHashMapPacket;

                OnRecieveSimDataSegmentHash(ref shmHashMap.m_lSegmentHashes, shmHashMap.m_iBytes);

                return null;
            }
            else if (pktInputPacket is SimSegmentSyncRequestPacket)
            {
                SimSegmentSyncRequestPacket ssrSegmentRequest = pktInputPacket as SimSegmentSyncRequestPacket;

                OnRequestDataSegment(ssrSegmentRequest.m_lSegmentHash);

                return null;
            }

            return pktInputPacket;
        }

        #region SendingState
        public void OnRequestStateForTime(DateTime dtmTime)
        {
            Debug.Log("SimStateSyncProcessor:: Data Request recieved for time");

            m_ostOutState = OutState.Pending;
            m_dtmTimeOfOutSimState = dtmTime;
                       
            //add to new list of requests 
            m_tParentPacketProcessor.OnNewRequestForSimDataAtTime(dtmTime, ParentConnection.m_lUserUniqueID);

        }

        public void OnSimDataChange(byte[] bSimDataAtPeerRequest)
        {
            Debug.Log("SimStateSyncProcessor:: Data for request found sending hash map");

            //check if connection active
            if (ParentConnection.Status != Connection.ConnectionStatus.Connected)
            {
                return;
            }

            m_bSimDataAtPeerRequest = bSimDataAtPeerRequest;

            //rebuild segment data hash 
            BuildSegmentDataHashArray();

            //change state to active 
            m_ostOutState = OutState.Active;

            //send / resend hash array to target peer
            SimStateSyncHashMapPacket shmStateHashMap = ParentConnection.m_cifPacketFactory.CreateType<SimStateSyncHashMapPacket>(SimStateSyncHashMapPacket.TypeID);

            shmStateHashMap.m_lSegmentHashes = m_lOutSimDataSegmentsHash;
            shmStateHashMap.m_iBytes = (uint)m_bSimDataAtPeerRequest.Length;

            //send hash map of game state at time
            ParentConnection.QueuePacketToSend(shmStateHashMap);
        }

        public void OnRequestDataSegment(long lDataHash)
        {
            //check if data set 
            if (m_ostOutState == OutState.NotRequested)
            {
                //send not available reply? 
                Debug.LogError("Sim sync mode not requested no sim state has been stored and no hashmap calculated");

                return;
            }

            for (int i = 0; i < m_lOutSimDataSegmentsHash.Length; i++)
            {
                if (m_lOutSimDataSegmentsHash[i] == lDataHash)
                {
                    //calculate start byte index for segment
                    int iStart = m_tParentPacketProcessor.MaxSegmentSize * i;
                    int iCount = Math.Min(m_tParentPacketProcessor.MaxSegmentSize, m_bSimDataAtPeerRequest.Length - iStart);

                    //send data segment to peer 
                    SimSegmentSyncDataPacket ssdSegmentData = ParentConnection.m_cifPacketFactory.CreateType<SimSegmentSyncDataPacket>(SimSegmentSyncDataPacket.TypeID);

                    //add hash of data 
                    ssdSegmentData.m_lSegmentHash = lDataHash;

                    //add segment tick
                    ssdSegmentData.m_lTickOfGameState = m_dtmTimeOfOutSimState.Ticks;

                    //create destination array
                    ssdSegmentData.m_bSegmentData = new byte[iCount];

                    //fill segment data array
                    Array.Copy(m_bSimDataAtPeerRequest, iStart, ssdSegmentData.m_bSegmentData, 0, iCount);

                    //TODO::remove this or wrap it in a define 
                    //check that the data segment actually hashes to the correct value
                    using (MD5 md5Hash = MD5.Create())
                    {
                                              //where to start generating the hash from
                        int iOffset = i * m_tParentPacketProcessor.MaxSegmentSize;
                        
                        //bytes to use in hash generation
                        int iDataToSendCount = Math.Min(m_bSimDataAtPeerRequest.Length - iOffset, m_tParentPacketProcessor.MaxSegmentSize);

                        //generate hash
                        byte[] bHash = md5Hash.ComputeHash(m_bSimDataAtPeerRequest, iOffset, iDataToSendCount);

                        //convert hash to long for convienience 
                        long lHashOfDataToSend = BitConverter.ToInt64(bHash, 0) ^ i;

                        if(lHashOfDataToSend != lDataHash)
                        {
                            Debug.LogError($"The Hash map for peer is incorrect and the data has a hash of {lHashOfDataToSend} at time {m_dtmTimeOfOutSimState} which does not match the target hash of {lDataHash} that the peer {ParentConnection.m_lUserUniqueID} requested");
                        }
          
                    }

                    //send data tp peer
                    ParentConnection.QueuePacketToSend(ssdSegmentData);

                    return;
                }
            }

            //data segment not found send not available reply
        }

        //this creates a hash for each 300 byte segment of the sim data 
        public void BuildSegmentDataHashArray()
        {
            //calculate the numer of segments needed
            int iSegmentCount = (m_bSimDataAtPeerRequest.Length + m_tParentPacketProcessor.MaxSegmentSize - 1) / m_tParentPacketProcessor.MaxSegmentSize;

            //check segment count array is correct size 
            if (m_lOutSimDataSegmentsHash == null || iSegmentCount != m_lOutSimDataSegmentsHash.Length)
            {
                m_lOutSimDataSegmentsHash = new long[iSegmentCount];
            }

            using (MD5 md5Hash = MD5.Create())
            {
                for (int i = 0; i < iSegmentCount; i++)
                {
                    //where to start generating the hash from
                    int iOffset = i * m_tParentPacketProcessor.MaxSegmentSize;
                    //bytes to use in hash generation
                    int iCount = Math.Min(m_bSimDataAtPeerRequest.Length - iOffset, m_tParentPacketProcessor.MaxSegmentSize);

                    //generate hash
                    byte[] bHash = md5Hash.ComputeHash(m_bSimDataAtPeerRequest, iOffset, iCount);

                    //convert hash to long for convienience 
                    m_lOutSimDataSegmentsHash[i] = BitConverter.ToInt64(bHash, 0) ^ i;
                }
            }

        }

        //check if this connection has an out bound state request
        //check if the request has timed out and if it hasn't 
        //update the time of next request time out
        public void UpdateOutDataRequestTimeOut(ref DateTime dtmTimeOfNextRequestTimeOut, DateTime dtmCurrentTime)
        {
            if (m_ostOutState == OutState.NotRequested)
            {
                return;
            }

            DateTime dtmTimeOutTime = m_dtmTimeOfOutSimState + m_tParentPacketProcessor.StateRequestTimeOut;
            
            //check if request for game state has timed out
            if (dtmTimeOutTime < dtmCurrentTime)
            {
                //reset all outbound values
                m_ostOutState = OutState.NotRequested;

                m_bSimDataAtPeerRequest = new byte[0];

                m_lOutSimDataSegmentsHash = new long[0];

                m_dtmTimeOfOutSimState = DateTime.MinValue;

                return;
            }
            
            if (dtmTimeOutTime < dtmTimeOfNextRequestTimeOut)
            {
                dtmTimeOfNextRequestTimeOut = dtmTimeOutTime;
            }

            return;
        }
        #endregion

        #region RecievingState

        public void HandleNewConnectionWhenRecieving()
        {
            //check if currently requesting sim state
            if (m_tParentPacketProcessor.m_staState == SimStateSyncNetworkProcessor.State.GettingStateData)
            {
                //check if peer is on list of authorative peers 
                if (m_tParentPacketProcessor.IsPeerInAuthorativeList(ParentConnection))
                {
                    //send request for state 
                    RequestSimState();
                }
            }
        }

        public void RequestSimState()
        {
            //get time of state from parent network packet processor 
            DateTime dtmSimStateTime = m_tParentPacketProcessor.m_dtmTimeOfAgreedState;

            //send request to peer

            //generate echo data packet
            SimStateSyncRequestPacket srsStateRequest = ParentConnection.m_cifPacketFactory.CreateType<SimStateSyncRequestPacket>(SimStateSyncRequestPacket.TypeID);

            //set request time
            srsStateRequest.m_dtmTimeOfSimData = dtmSimStateTime;

            //queue request 
            ParentConnection.QueuePacketToSend(srsStateRequest);
        }

        public void OnRecieveSimDataSegmentHash(ref long[] lSimHash, uint uByteCount)
        {
            //check if still syncing 
            if (m_tParentPacketProcessor.m_staState != SimStateSyncNetworkProcessor.State.GettingStateData)
            {
                Debug.LogWarning("Recieved state hash after sync state left getting data state");
                return;
            }

            if (m_lInSimDataSegmentsHash == null || m_lInSimDataSegmentsHash.Length != lSimHash.Length)
            {
                Debug.Log($"initalizing hash map array to lenght {lSimHash.Length}");
                m_lInSimDataSegmentsHash = new long[lSimHash.Length];
            }

            lSimHash.CopyTo(m_lInSimDataSegmentsHash, 0);

            m_iTotalByteCount = uByteCount;

            //merge all the hashes togeather in a sort of criptographicalaly secure way
            m_lInTotalStateHash = m_iTotalByteCount;

            for (int i = 0; i < lSimHash.Length; i++)
            {
                unchecked
                {
                    m_lInTotalStateHash = m_lInTotalStateHash + m_lInTotalStateHash + m_lInTotalStateHash + lSimHash[i];
                }
            }

            m_istInState = InState.Recieved;

            //clear any assigned segments 
            //this is in a function just to make it easier to debug
            if (m_sRequestedSegment != ushort.MaxValue)
            {
                m_sRequestedSegment = ushort.MaxValue;
            }

            //flag parent state to update best state 
            m_tParentPacketProcessor.m_bIsConnectedPeerStateHashDirty = true;
        }

        //requests data from a peer to be delivered by a certain time
        public void OnAssignSegment(ushort sSegmentIndex, DateTime dtmTimeOutTime)
        {
            if (m_lInSimDataSegmentsHash.Length <= sSegmentIndex)
            {
                //requested out of bounds segment 
                Debug.LogError($"Segment request for index:{sSegmentIndex} is for data that does not exist in the hash map");
                return;
            }

            m_sRequestedSegment = sSegmentIndex;
            m_dtmRequestTimeOut = dtmTimeOutTime;

            //send request to peer 
            SimSegmentSyncRequestPacket ssrSegmentRequest = ParentConnection.m_cifPacketFactory.CreateType<SimSegmentSyncRequestPacket>(SimSegmentSyncRequestPacket.TypeID);

            ssrSegmentRequest.m_lSegmentHash = m_lInSimDataSegmentsHash[sSegmentIndex];

            //queue request 
            ParentConnection.QueuePacketToSend(ssrSegmentRequest);
        }

        public void OnSegmentRequestFilled(ushort sSegmentIndex)
        {
            if (m_sRequestedSegment == sSegmentIndex)
            {
                m_sRequestedSegment = ushort.MaxValue;
            }
        }

        //check if the request has timed out and if it has returns true
        public void UpdateInDataRequestTimeout(ref DateTime dtmNextTimeOut, DateTime dtmTime)
        {
            //check if assigned a segment
            if (m_sRequestedSegment == ushort.MaxValue)
            {
                //no segment assigned
                return;
            }

            //check if timed out
            if (m_dtmRequestTimeOut < dtmTime)
            {
                //return request to the to be assigned segment pool
                m_tParentPacketProcessor.ReturnRequestToPool(m_sRequestedSegment);

                //reset assigned segment
                m_sRequestedSegment = ushort.MaxValue;

                return;
            }
            else
            {
                if (dtmNextTimeOut > m_dtmRequestTimeOut)
                {
                    dtmNextTimeOut = m_dtmRequestTimeOut;
                }

                return;
            }
        }

        public void CleanUpRecievingState()
        {
            //the state of the data sync
            m_istInState = InState.NotRequested;

            //the time the segment request was made 
            m_dtmRequestTimeOut = DateTime.MinValue;

            //the hash of the entire state sent by peer 
            //this value is calculated locally based of data segment hash
            m_lInTotalStateHash = 0;

            //hash of each of the segments of the chain 
            m_lInSimDataSegmentsHash = null;

            //the size of the sim state that the peer has 
            m_iTotalByteCount = 0;

            //the segment the local peer has requsted the remote peer send
            m_sRequestedSegment = ushort.MaxValue;
        }

        #endregion
    }
}


