﻿using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Networking
{
    public class TimeNetworkProcessor : ManagedNetworkPacketProcessor<TimeConnectionProcessor>
    {
        public static float s_fTimeOffset = 0;

        public static DateTime StaticBaseTime
        {
            get
            {
                return DateTime.UtcNow;
            }
        }

        public static DateTime CalculateNetworkTime(in TimeSpan tspCurrentOffset, ref DateTime dtmOldestTime)
        {
            //calcualte network time
            DateTime dtmNetworkTime = StaticBaseTime - tspCurrentOffset;

            //make sure never to run time backwards 
            if (dtmNetworkTime < dtmOldestTime)
            {
                return dtmOldestTime;
            }

            dtmOldestTime = dtmNetworkTime;

            return dtmNetworkTime;
        }

        private NetworkingDataBridge NetworkDataBridge { get; set; }

        //the time everything is based off
        public DateTime BaseTime
        {
            get
            {
                //for testing and debug
                //long lTicks = ((DateTime.UtcNow.Ticks / TimeSpan.TicksPerDay) * TimeSpan.TicksPerDay) + (long)(TimeSpan.TicksPerSecond * Time.timeSinceLevelLoad);
                //
                //return new DateTime(lTicks, DateTimeKind.Utc);

                return DateTime.UtcNow + TimeSpan.FromSeconds(m_fTimeOffset);

                //return StaticBaseTime;
            }
        }

        //the synchronised time accross the network
        public DateTime NetworkTime
        {
            get
            {
                return CalculateNetworkTime(m_tspCurrentTimeOffset, ref m_dtmOldestTime);
            }

        }

        //the time offset
        public TimeSpan TimeOffset
        {
            get
            {
                return m_tspCurrentTimeOffset;
            }
        }

        //the latency to the worst active connection
        public TimeSpan LargetsRTT { get; private set; } = TimeSpan.Zero;

        public TimeSpan AverageRTT { get; private set; } = TimeSpan.Zero;

        public override int Priority
        {
            get
            {
                return 0;
            }
        }

        //the oldest network time calculated 
        private DateTime m_dtmOldestTime = DateTime.MinValue;
        private TimeSpan m_tspTargetTimeOffset;
        private TimeSpan m_tspCurrentTimeOffset;
        private TimeSpan m_tspOffsetChangeRate;
        private TimeSpan m_tspTimeLerpSpeed = TimeSpan.FromSeconds(1);
        private TimeSpan m_tspMaxLerpDistance = TimeSpan.FromSeconds(1);
        private TimeSpan m_tspUpdateRate = TimeSpan.FromSeconds(1);
        private TimeSpan m_tspMaxLatencyUsedInCalculations = TimeSpan.FromSeconds(2);
        private List<TimeSpan> m_dtoTempTimeOffsets = new List<TimeSpan>();

        //TODO: remove this code once testing is done
        private float m_fTimeOffset = (++s_fTimeOffset) * 0.25f;

        protected override TimeConnectionProcessor NewConnectionProcessor()
        {
            return new TimeConnectionProcessor(m_tspUpdateRate);
        }

        public override void Update()
        {
            LerpToTargetTime(Time.deltaTime);
        }

        //gets the average time offset across all connections  while correcting for lag and
        //maliciouse packets 
        public void RecalculateTimeOffset()
        {
            m_dtoTempTimeOffsets.Clear();

            LargetsRTT = TimeSpan.Zero;

            AverageRTT = TimeSpan.Zero;

            // generate an array of all the offsets for all conenctions 
            //loop through all the conenctions 
            foreach (TimeConnectionProcessor tcpConnectionTime in ChildConnectionProcessors.Values)
            {
                if (tcpConnectionTime.ParentConnection.Status == Connection.ConnectionStatus.Connected)
                {
                    //add offset to list
                    m_dtoTempTimeOffsets.Add(tcpConnectionTime.Offset);

                    TimeSpan tspLatency = TimeSpan.FromTicks(Math.Min(m_tspMaxLatencyUsedInCalculations.Ticks, tcpConnectionTime.RTT.Ticks));

                    LargetsRTT = TimeSpan.FromTicks(Math.Max(tspLatency.Ticks, LargetsRTT.Ticks));

                    AverageRTT += tspLatency;

                }
            }

            //add local offset
            m_dtoTempTimeOffsets.Add(TimeSpan.FromSeconds(0));

            //calculate average rtt
            AverageRTT = TimeSpan.FromTicks(AverageRTT.Ticks / m_dtoTempTimeOffsets.Count);

            //sort list from big to small 
            m_dtoTempTimeOffsets.Sort((x, y) => (int)(x.Ticks - y.Ticks));

            int iNumberOfConnectionToAverage = 3;

            int iOffset = ((iNumberOfConnectionToAverage - 1) / 2);

            //calculate the range to take values from
            int iMin = Math.Max(0, (m_dtoTempTimeOffsets.Count / 2) - (iOffset + 1));

            //calculate the range to take values from
            int iMax = Math.Max(m_dtoTempTimeOffsets.Count, (m_dtoTempTimeOffsets.Count / 2) + (iOffset - 1));

            m_tspTargetTimeOffset = TimeSpan.Zero;

            //calculate new target offset 
            for (int i = iMin; i < iMax; i++)
            {
                m_tspTargetTimeOffset += m_dtoTempTimeOffsets[i];
            }

            //calc final target offset
            m_tspTargetTimeOffset = TimeSpan.FromTicks(m_tspTargetTimeOffset.Ticks / Math.Max(1, iMax - iMin));
        }

        private void LerpToTargetTime(float fDeltaTime)
        {
            //get direction to lerp
            TimeSpan lerpDirection = m_tspTargetTimeOffset - m_tspCurrentTimeOffset;

            long lAbsTimeDif = Math.Abs(lerpDirection.Ticks);

            //check if difference is larget than max lerp time
            if (lAbsTimeDif > m_tspMaxLerpDistance.Ticks)
            {
                m_tspCurrentTimeOffset = m_tspTargetTimeOffset;
                return;
            }

            float fTicksToLerp = (m_tspTimeLerpSpeed.Ticks * fDeltaTime);

            if (fTicksToLerp < lAbsTimeDif)
            {
                m_tspCurrentTimeOffset = m_tspCurrentTimeOffset + TimeSpan.FromTicks((long)(fTicksToLerp * Math.Sign(lerpDirection.Ticks)));
            }
            else
            {
                m_tspCurrentTimeOffset = m_tspTargetTimeOffset;
            }

            UpdateNetworkDataBridge();
        }

        private void UpdateNetworkDataBridge()
        {
            //get lock on network data bridge time values
            if (NetworkDataBridge != null)
            {
                NetworkDataBridge.m_tspNetworkTimeOffset = m_tspCurrentTimeOffset;
            }

            //drop lock
        }

        protected override void AddDependentPacketsToPacketFactory(ClassWithIDFactory cifPacketFactory)
        {
            NetTestSendPacket.TypeID = cifPacketFactory.AddType<NetTestSendPacket>(NetTestSendPacket.TypeID);
            NetTestReplyPacket.TypeID = cifPacketFactory.AddType<NetTestReplyPacket>(NetTestReplyPacket.TypeID);
        }
    }

    public class TimeConnectionProcessor : ManagedConnectionPacketProcessor<TimeNetworkProcessor>
    {
        public override int Priority
        {
            get
            {
                return 0;
            }
        }

        public DateTime Time
        {
            get
            {
                return m_tParentPacketProcessor.BaseTime + Offset;
            }
        }

        public TimeSpan Offset { get; private set; } = TimeSpan.Zero;

        public TimeSpan RTT { get; private set; } = TimeSpan.Zero;

        //the value of the echo sent to check the rtt of the connection
        protected byte m_bEchoSent = 0;

        //the time the echo was sent
        protected DateTime m_dtmTimeOfEchoSend;

        //rate of update
        protected TimeSpan m_fEchoUpdateRate = TimeSpan.FromSeconds(1);

        //the maximum rtt of a message before it is discarded 
        protected TimeSpan m_tspMaxRTT = TimeSpan.FromSeconds(2);

        protected TimeSpan m_tspMaxRTTChange = TimeSpan.FromSeconds(0.500);

        //time since last update
        protected DateTime m_dtmTimeOfLastUpdate;

        public TimeConnectionProcessor() : base()
        {
        }

        public TimeConnectionProcessor(TimeSpan tspUpdateRate) : base()
        {
            m_fEchoUpdateRate = tspUpdateRate;

            //indicate its time for an update immediatly 
            m_dtmTimeOfLastUpdate = DateTime.MinValue;
        }

        public override void Update()
        {
            //dont try and send packets if not connected
            if (ParentConnection.Status != Connection.ConnectionStatus.Connected)
            {
                return;
            }

            TimeSpan tspTimeSinceLastUpdate = m_tParentPacketProcessor.BaseTime - m_dtmTimeOfLastUpdate;

            //check if it is time for another update 
            if (tspTimeSinceLastUpdate > m_fEchoUpdateRate && (m_bEchoSent == byte.MinValue || tspTimeSinceLastUpdate > m_tspMaxRTT))
            {
                //get echo value 
                m_bEchoSent = (byte)UnityEngine.Random.Range(byte.MinValue + 1, byte.MaxValue);

                //generate echo data packet
                NetTestSendPacket ntpEcho = ParentConnection.m_cifPacketFactory.CreateType<NetTestSendPacket>(NetTestSendPacket.TypeID);

                //set echo value
                ntpEcho.m_bEcho = m_bEchoSent;

                //reset time of echo send and time since last check
                m_dtmTimeOfEchoSend = m_dtmTimeOfLastUpdate = m_tParentPacketProcessor.BaseTime;

                //queue echo 
                ParentConnection.QueuePacketToSend(ntpEcho);
            }
        }

        public override DataPacket ProcessReceivedPacket(Connection conConnection, DataPacket pktInputPacket)
        {
            //check if packet is network test packet 
            if (pktInputPacket is NetTestSendPacket)
            {
                //get packet 
                NetTestSendPacket ntpEcho = pktInputPacket as NetTestSendPacket;

                //get echo Packet 
                NetTestReplyPacket ntpReply = conConnection.m_cifPacketFactory.CreateType<NetTestReplyPacket>(NetTestReplyPacket.TypeID);

                //set reply values 
                ntpReply.m_bEcho = ntpEcho.m_bEcho;

                //set local time 
                ntpReply.m_lLocalBaseTimeTicks = m_tParentPacketProcessor.BaseTime.Ticks;

                //schedule a reply packet 
                conConnection.QueuePacketToSend(ntpReply);

                return null;
            }
            if (pktInputPacket is NetTestReplyPacket)
            {
                //get packet 
                NetTestReplyPacket ntpEcho = pktInputPacket as NetTestReplyPacket;

                TimeSpan tspTimeSinceTestStart = m_tParentPacketProcessor.BaseTime - m_dtmTimeOfEchoSend;

                long tspChangeInRTT = (RTT.Ticks > 0) ? Math.Abs(RTT.Ticks - tspTimeSinceTestStart.Ticks) : 0;


                //check if echo matches 
                if (ntpEcho.m_bEcho != m_bEchoSent || m_bEchoSent == byte.MinValue || tspTimeSinceTestStart > m_tspMaxRTT || tspChangeInRTT > m_tspMaxRTTChange.Ticks)
                {
                    //bad echo reply probably connection error or hack? 
                }
                else
                {
                    //update the time difference
                    RTT = tspTimeSinceTestStart;

                    //the time on the other end of this connection when this message was sent
                    DateTime dtmTimeOfReplySend = new DateTime(ntpEcho.m_lLocalBaseTimeTicks, DateTimeKind.Utc);

                    //time adjusted for transmittion time
                    DateTime dtmPredictedTime = dtmTimeOfReplySend + (TimeSpan.FromTicks(RTT.Ticks / 2));

                    //reset echo value
                    m_bEchoSent = byte.MinValue;

                    //trigger recalculation of network time 
                    m_tParentPacketProcessor.RecalculateTimeOffset();
                }

                //consume echo packet as it is no longer needed 
                return null;
            }

            return pktInputPacket;
        }

        public override void OnConnectionReset()
        {
            //reset connection values 
            RTT = TimeSpan.Zero;
            Offset = TimeSpan.Zero;
            m_bEchoSent = byte.MinValue;
            m_dtmTimeOfEchoSend = DateTime.MinValue;
            m_dtmTimeOfLastUpdate = DateTime.MinValue;
        }
    }
}
