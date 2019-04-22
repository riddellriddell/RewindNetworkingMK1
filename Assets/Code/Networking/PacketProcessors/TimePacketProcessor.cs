using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Networking
{
    public class TimeNetworkProcessor : NetworkPacketProcessor
    {
        //the time everything is based off
        public DateTime BaseTime
        {
            get
            {
                //for testing and debug
                long lTicks = ((DateTime.UtcNow.Ticks / TimeSpan.TicksPerDay) * TimeSpan.TicksPerDay) + (long)(TimeSpan.TicksPerSecond * Time.timeSinceLevelLoad);

                return new DateTime(lTicks, DateTimeKind.Utc);

                //return DateTime.UtcNow;
            }
        }

        //the synchronised time accross the network
        public DateTime NetworkTime
        {
            get
            {
                return BaseTime + m_dtoCurrentTimeOffset;
            }

        }

        public override int Priority
        {
            get
            {
                return 0;
            }
        }


        //the time offset used when claculating 
        private TimeSpan m_dtoTargetTimeOffset;
        private TimeSpan m_dtoCurrentTimeOffset;
        private TimeSpan m_tspOffsetChangeRate;
        private TimeSpan m_tspTimeLerpSpeed = TimeSpan.FromSeconds(1);
        private TimeSpan m_tspMaxLerpDistance = TimeSpan.FromSeconds(1);
        private TimeSpan m_tspUpdateRate = TimeSpan.FromSeconds(1);
        private NetworkConnection m_ncnNetworkConnection;
        private List<TimeSpan> m_dtoTempTimeOffsets = new List<TimeSpan>();

        //this gets called when a new connection is added
        public override void OnAddToNetwork(NetworkConnection ncnNetwork)
        {
            for(int i = 0; i < ncnNetwork.m_conConnectionList.Count; i++)
            {
                OnNewConnection(ncnNetwork.m_conConnectionList[i]);
            }
        }

        public override void OnNewConnection(Connection conConnection)
        {
            TimeConnectionProcessor tcpPacketProcessor = new TimeConnectionProcessor(this, m_tspUpdateRate);
            conConnection.AddPacketProcessor(tcpPacketProcessor);
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

            // generate an array of all the offsets for all conenctions 
            //loop through all the conenctions 
            for (int i = 0; i < m_ncnNetworkConnection.m_conConnectionList.Count; i++)
            {
                //get time offset 
                Connection conConnection = m_ncnNetworkConnection.m_conConnectionList[i];
                TimeConnectionProcessor tcpTimeProcessor = conConnection.GetPacketProcessor<TimeConnectionProcessor>();

                //add offset to list
                m_dtoTempTimeOffsets.Add(tcpTimeProcessor.Offset);

            }

            //sort list from big to small 
            m_dtoTempTimeOffsets.Sort((x, y) => (int)(x.Ticks - y.Ticks));

            int iNumberOfConnectionToAverage = 3;

            int iOffset = ((iNumberOfConnectionToAverage - 1) / 2);

            //calculate the range to take values from
            int iMin = Math.Max(0, (m_ncnNetworkConnection.m_conConnectionList.Count / 2) - (iOffset + 1));
            //calculate the range to take values from
            int iMax = Math.Max(m_ncnNetworkConnection.m_conConnectionList.Count, (m_ncnNetworkConnection.m_conConnectionList.Count / 2) + (iOffset - 1));

            m_dtoTargetTimeOffset = TimeSpan.Zero;

            //calculate new target offset 
            for (int i = iMin; i < iMax; i++)
            {
                m_dtoTargetTimeOffset += m_dtoTempTimeOffsets[i];
            }

            //calc final target offset
            m_dtoTargetTimeOffset = TimeSpan.FromTicks(m_dtoTargetTimeOffset.Ticks / Math.Max(1, iMax - iMin));
        }

        private void LerpToTargetTime(float fDeltaTime)
        {
            //get direction to lerp
            TimeSpan lerpDirection = m_dtoTargetTimeOffset - m_dtoCurrentTimeOffset;

            long lAbsTimeDif = Math.Abs(lerpDirection.Ticks);

            //check if difference is larget than max lerp time
            if (lAbsTimeDif > m_tspMaxLerpDistance.Ticks)
            {
                m_dtoCurrentTimeOffset = m_dtoTargetTimeOffset;
                return;
            }

            float fTicksToLerp = (m_tspTimeLerpSpeed.Ticks * fDeltaTime);

            if (fTicksToLerp < lAbsTimeDif)
            {
                m_dtoCurrentTimeOffset = m_dtoCurrentTimeOffset + TimeSpan.FromTicks((long)(fTicksToLerp * Math.Sign(lerpDirection.Ticks)));
            }
            else
            {
                m_dtoCurrentTimeOffset = m_dtoTargetTimeOffset;
            }
        }
    }

    public class TimeConnectionProcessor : ConnectionPacketProcessor
    {
        public DateTime Time
        {
            get
            {
                return m_tnpTimeNetworkProcessor.BaseTime + Offset;
            }
        }

        public TimeSpan Offset { get; private set; }

        public TimeSpan RTT { get; private set; } = TimeSpan.FromSeconds(0);

        //the value of the echo sent to check the rtt of the connection
        protected byte m_bEchoSent = 0;

        //the time the echo was sent
        protected DateTime m_dtmTimeOfEchoSend;

        //rate of update
        protected TimeSpan m_fEchoUpdateRate = TimeSpan.FromSeconds(1);

        //time since last update
        protected DateTime m_dtmTimeOfLastUpdate;

        public TimeConnectionProcessor(TimeNetworkProcessor tnpNetworkProcessor, TimeSpan tspUpdateRate) : base()
        {
            m_tnpTimeNetworkProcessor = tnpNetworkProcessor;

            m_fEchoUpdateRate = tspUpdateRate;

            //indicate its time for an update immediatly 
            m_dtmTimeOfLastUpdate = DateTime.MinValue;
        }

        public override void Update(Connection conConnection)
        {
            //check if it is time for another update 
            if (m_tnpTimeNetworkProcessor.BaseTime - m_dtmTimeOfLastUpdate > m_fEchoUpdateRate && m_bEchoSent != byte.MinValue)
            {
                //get echo value 
                m_bEchoSent = (byte)UnityEngine.Random.Range(byte.MinValue + 1, byte.MaxValue);

                //generate echo data packet
                NetTestSendPacket ntpEcho = conConnection.m_cifPacketFactory.CreateType<NetTestSendPacket>(NetTestSendPacket.TypeID);

                //set echo value
                ntpEcho.m_bEcho = m_bEchoSent;

                //reset time of echo send and time since last check
                m_dtmTimeOfEchoSend = m_dtmTimeOfLastUpdate = m_tnpTimeNetworkProcessor.BaseTime;

                //queue echo 
                conConnection.QueuePacketToSend(ntpEcho);
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
                ntpReply.m_lTicks = m_tnpTimeNetworkProcessor.BaseTime.Ticks;

                //schedule a reply packet 
                conConnection.QueuePacketToSend(ntpReply);

                return null;
            }
            if (pktInputPacket is NetTestReplyPacket)
            {
                //get packet 
                NetTestReplyPacket ntpEcho = pktInputPacket as NetTestReplyPacket;

                //check if echo matches 
                if (ntpEcho.m_bEcho != m_bEchoSent || m_bEchoSent == byte.MinValue)
                {
                    //bad echo reply probably error or hack? 
                }
                else
                {
                    //update the time difference
                    RTT = m_tnpTimeNetworkProcessor.BaseTime - m_dtmTimeOfEchoSend;
                    m_dtmTimeOfEchoSend = m_tnpTimeNetworkProcessor.BaseTime;
                    
                    //the time on the other end of this connection when this message was sent
                    DateTime dtmTimeOfReplySend = new DateTime(ntpEcho.m_lTicks, DateTimeKind.Utc);

                    //time adjusted for transmittion time
                    DateTime dtmPredictedTime = dtmTimeOfReplySend + (TimeSpan.FromTicks(RTT.Ticks /2));

                    //the difference relitive to the local time 
                    Offset = m_tnpTimeNetworkProcessor.BaseTime - dtmPredictedTime;

                    //reset echo value
                    m_bEchoSent = 0;

                    //trigger recalculation of network time 
                    m_tnpTimeNetworkProcessor.RecalculateTimeOffset();
                }               

                //consume echo packet as it is no longer needed 
                return null;
            }         
   
            return pktInputPacket;
        }

    private TimeNetworkProcessor m_tnpTimeNetworkProcessor;
}
}
