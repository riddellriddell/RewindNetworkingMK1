using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Networking
{
    //this class is a state machine that keeps track of the latency from each peer to each other peer
    //and allows a peer to work out if it is a shorter path from one peer to another
    public class GlobalMessagePeerLatencyState
    {
        public struct PeerLatency
        {
            //the latency from this peer to the target
            public int m_iLatency;

            //the state of the connection (to add if needed)

        }

        //the maximum number of peers in the system
        public int MaxNumberOfPeers { get; private set; }

        //buffer holding all the latecy from each peer to each other peer
        protected PeerLatency[] m_plaPeerLatencyBuffer;

        //access to each peers buffer data
        public ArraySegment<PeerLatency>[] PeerLatencyMap { get; private set; }

        public GlobalMessagePeerLatencyState(int iPeerCount)
        {
            MaxNumberOfPeers = iPeerCount;

            //create buffer
            m_plaPeerLatencyBuffer = new PeerLatency[MaxNumberOfPeers * MaxNumberOfPeers];

            //create array segments for acces
            CreateArraySegments();
        }

        public GlobalMessagePeerLatencyState(PeerLatency[] plaPeerLatency,int iPeerCount)
        {
            //check array size
            Debug.Assert(plaPeerLatency.Length == MaxNumberOfPeers * MaxNumberOfPeers, $"Global message peer latency constructed with mismatching peer count {iPeerCount} and latency buffer size {plaPeerLatency.Length}");
  
            MaxNumberOfPeers = iPeerCount;

            //create buffer
            m_plaPeerLatencyBuffer = plaPeerLatency;

            //create array segments for acces
            CreateArraySegments();
        }

        //get the time for a message to be sent from peer to peer acording to reciever
        public TimeSpan GetLatencyFromPeerToPeer(int iFromPeerChannel, int iToPeerChannel)
        {
            //validate values
            Debug.Assert(iFromPeerChannel < MaxNumberOfPeers, $"FromPeerChannel {iFromPeerChannel} out of bounds max channel is {MaxNumberOfPeers}");
            Debug.Assert(iToPeerChannel < MaxNumberOfPeers, $"ToPeerChannel {iToPeerChannel} out of bounds max channel is {MaxNumberOfPeers}");

            return TimeSpan.FromTicks(PeerLatencyMap[iToPeerChannel].Array[iFromPeerChannel].m_iLatency);

        }

        // if a message was sent at signal send time from source what time would that message get to echo peer 
        // and be relayed to reciever peer
        public DateTime SignalRelayTime(DateTime dtmSignalSendTime,int iSignalSourceChannel, int iEchoChannel,int iReceiverChannel)
        {
            //validate all values
            Debug.Assert(iSignalSourceChannel < MaxNumberOfPeers, $"Signal Source Channel {iSignalSourceChannel} out of bounds bounds max channel is {MaxNumberOfPeers}");
            Debug.Assert(iEchoChannel < MaxNumberOfPeers, $"EchoChannel {iEchoChannel} out of bounds max channel is {MaxNumberOfPeers}");
            Debug.Assert(iReceiverChannel < MaxNumberOfPeers, $"RecieverChannel {iReceiverChannel} out of bounds max channel is {MaxNumberOfPeers}");

            //get latency from sender to echo
            int iToEchoLatency = PeerLatencyMap[iEchoChannel].Array[iSignalSourceChannel].m_iLatency;

            //get latency from echo to reciever
            int iToRecieverLatency = PeerLatencyMap[iReceiverChannel].Array[iEchoChannel].m_iLatency;

            DateTime dtmRecieveTime = dtmSignalSendTime + TimeSpan.FromTicks(iToEchoLatency + iToRecieverLatency);

            return dtmRecieveTime;
        }

        //the if reciever peer recieves a message at time DirectSignalRecieveTime from source peer
        //this function returns the time it should recieve an echo from echo peer
        public DateTime EchoTimeFromPeerForRecievedSignal(DateTime dtmTimeSignalReceived, int iSourceChannel, int iEchoChannel, int iReceiverChannel)
        {
            //convert direct recieve time to est send time
            DateTime dtmEstSendTime = dtmTimeSignalReceived - GetLatencyFromPeerToPeer(iSourceChannel, iReceiverChannel);

            //get the time a message echoed off echo peer would arrive at reciever
            return SignalRelayTime(dtmEstSendTime, iSourceChannel, iEchoChannel, iReceiverChannel);
        }        

        //apply a change to a peers latency to another peer
        public void UpdateLatency(int iRecieverPeerChannel, int iSenderPeerChannel, int iLatency)
        {
            //validate inputs
            Debug.Assert(iRecieverPeerChannel < MaxNumberOfPeers, $"RecieverPeerChannel {iRecieverPeerChannel} out of bounds peer channel count {MaxNumberOfPeers} ");
            Debug.Assert(iSenderPeerChannel < MaxNumberOfPeers, $"SenderPeerChannel {iSenderPeerChannel} out of bounds max peer number{MaxNumberOfPeers}");
            Debug.Assert(iLatency >= 0, $"Latency {iLatency} can not be less than 0");


            //TODO:: not sure if this works with structs
            //apply latency from peer to peer
            PeerLatencyMap[iRecieverPeerChannel].Array[iSenderPeerChannel].m_iLatency = iLatency;
        }

        //create accessors for latency of each peer
        protected void CreateArraySegments()
        {
            //create array segments for access
            PeerLatencyMap = new ArraySegment<PeerLatency>[MaxNumberOfPeers];

            for (int i = 0; i < MaxNumberOfPeers; i++)
            {
                PeerLatencyMap[i] = new ArraySegment<PeerLatency>(m_plaPeerLatencyBuffer, (MaxNumberOfPeers - 1) * i, (MaxNumberOfPeers - 1));
            }
        }

    }
}
