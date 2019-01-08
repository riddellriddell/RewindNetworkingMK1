using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Networking
{
    public class Connection
    {
        public enum ConnectionStatus
        {
            New,
            Connected,
            Disconnected
        }

        //values used for testing
        public InternetConnectionSimulator m_icsConnectionSim;
        public Connection m_conConnectionTarget;

        //the player id associated with this channel 
        public byte m_bConnectionID;

        //a unique id used to identify a player before game starts
        public long m_lUniqueID;

        // the max number of packets to sent at once 
        public int m_iMaxBytesToSend;

        // list of all the packets that have been received but not yet processed 
        public Queue<Packet> m_pakReceivedPackets;

        // list of all the packets to send that have not yet acknowledged  
        public RandomAccessQueue<Packet> m_PacketsInFlight;

        // list of all the packet processors 
        protected SortedList<ConnectionPacketProcessor> m_cppOrderedPacketProcessorList;

        // the packet number of the last packet sent
        protected int m_iPacketsQueuedToSendCount;

        // the tick of the last tick stamped packet sent
        protected int m_iLastPacketTickQueuedToSend;

        // the number of the last acknowledged packet received by connection target 
        protected int m_iLastAckPacketNumberSent;

        // the most recent packet received 
        protected int m_iTotalPacketsReceived;

        //the tick of the last packet recieved
        protected int m_iLastTickReceived;

        public Connection(byte bConnectionID)
        {
            m_bConnectionID = bConnectionID;

            m_pakReceivedPackets = new Queue<Packet>();
            m_PacketsInFlight = new RandomAccessQueue<Packet>();
            m_cppOrderedPacketProcessorList = new List<ConnectionPacketProcessor>();

            m_iPacketsQueuedToSendCount = 0;
            m_iLastAckPacketNumberSent = 0;
            m_iTotalPacketsReceived = 0;
        }

        //check if a ping packet is needed to keep the connection alive
        public void UpdateConnection(int iTick)
        {
            //check if a ping packet is needed
            if (iTick - m_iLastPacketTickQueuedToSend >= TickStampedPacket.MaxTicksBetweenTickStampedPackets)
            {
                //add a ping packet to send list to maintain connection
                QueuePacketToSend(new PingPacket());
            }

            //send packets to target
            SendPackets();
        }
        
        public void ReceivePacket(PacketWrapper packetWrapper)
        {
            //update the last ack packet sent from this client to the connection target clamped to not be more than the total number of packets sent
            m_iLastAckPacketNumberSent = Mathf.Min(Mathf.Max(m_iLastAckPacketNumberSent, packetWrapper.m_iLastAckPackageFromPerson), m_iPacketsQueuedToSendCount);

            //get the tick of the oldest packet in the packet wrapper 
            int iPacketNumberHead = packetWrapper.m_iStartPacketNumber;

            //the packet read head 
            int iPacketReadHead = 0;

            //the length of the data 
            int iPacketReadTail = packetWrapper.m_Payload.Count;

            //decode the remaining packets 
            while (iPacketReadHead < iPacketReadTail)
            {
                //decode packet
                Packet pktDecodedPacket = DecodePacket(packetWrapper, ref iPacketReadHead);

                iPacketNumberHead++;

                //add packet to list of packets to be processed 
                QueueReceivedPacket(pktDecodedPacket, iPacketNumberHead);
            }
        }

        public void QueuePacketToSend(Packet packet)
        {
            m_iPacketsQueuedToSendCount++;

            ProcessSendingTickStampedPackets(packet);

            m_PacketsInFlight.Enqueue(packet);
        }



        private void SendPackets()
        {
            //check if there is anything to send
            if (m_PacketsInFlight.Count == 0)
            {
                return;
            }

            //calculate the number of packets that dont need to be sent 
            int iPacketsToDrop = m_PacketsInFlight.Count - (m_iPacketsQueuedToSendCount - m_iLastAckPacketNumberSent);

            if (iPacketsToDrop >= m_PacketsInFlight.Count)
            {
                m_PacketsInFlight.Clear();

                return;
            }

            //dequeue all the old packets that have already been acknowledged
            for (int i = 0; i < iPacketsToDrop; i++)
            {
                m_PacketsInFlight.Dequeue();
            }

            if (m_PacketsInFlight.Count == 0)
            {
                return;
            }

            //create packet wrapper 
            PacketWrapper pkwPacketWrappepr = new PacketWrapper(m_iTotalPacketsReceived, m_iPacketsQueuedToSendCount - m_PacketsInFlight.Count, m_PacketsInFlight.Count);

            int iBytesRemining = m_iMaxBytesToSend;

            //add as many packets as possible without hitting the max send data limit
            for (int i = 0; i < m_PacketsInFlight.Count; i++)
            {
                Packet pktPacketToSend = m_PacketsInFlight[i];

                int iPacketSize = pktPacketToSend.PacketSize;

                iBytesRemining -= iPacketSize;

                if (iBytesRemining > 0)
                {

                    pkwPacketWrappepr.AddDataPacket(pktPacketToSend);
                }
                else
                {
                    break;
                }
            }

            //send packet through coms 
            if (m_icsConnectionSim != null)
            {
                m_icsConnectionSim.SendPacket(pkwPacketWrappepr, m_conConnectionTarget);
            }
        }

        private Packet DecodePacket(PacketWrapper packetWrapper, ref int iReadHead)
        {

            //get packet type 
            Packet.PacketType ptyPacketType = Packet.GetPacketType(packetWrapper, iReadHead);

            Packet pktOutput = null;

            //create Packet 
            switch (ptyPacketType)
            {
                case Packet.PacketType.Ping:
                    pktOutput = new PingPacket();
                    break;
                case Packet.PacketType.Input:
                    pktOutput = new InputPacket();
                    break;
                case Packet.PacketType.ResetTickCount:
                    pktOutput = new ResetTickCountPacket();
                    break;
                case Packet.PacketType.StartCountdown:
                    pktOutput = new StartCountDownPacket();
                    break;

            }

            //decode packet
            iReadHead = pktOutput.DecodePacket(packetWrapper, iReadHead);

            return pktOutput;
        }

        private void QueueReceivedPacket(Packet pktPacket, int iPacketNumber)
        {
            //check if packet has already been queued 
            if (iPacketNumber <= m_iTotalPacketsReceived)
            {
                return;
            }

            //update the most recent packet number
            m_iTotalPacketsReceived = iPacketNumber;

            //process the tick offset
            ProcessReceivedTickStampedPackets(pktPacket);

            //check if this packet should be passed on to be processed by the rest of the game 
            if (ShouldPacketBePassedOn(pktPacket))
            {
                //queue up the packet 
                m_pakReceivedPackets.Enqueue(pktPacket);
            }
        }

        //process packets that have a tick stamp
        private void ProcessReceivedTickStampedPackets(Packet pktPacket)
        {
            //check if packet is ping packet 
            if (pktPacket is PingPacket)
            {
                m_iLastTickReceived += TickStampedPacket.MaxTicksBetweenTickStampedPackets;

                return;
            }

            if (pktPacket is TickStampedPacket)
            {
                TickStampedPacket tspPacket = pktPacket as TickStampedPacket;

                //update the tick of the connection target 
                m_iLastTickReceived += tspPacket.Offset;

                //set the tick of the packet 
                tspPacket.m_iTick = m_iLastTickReceived;

                return;
            }

            if(pktPacket is ResetTickCountPacket)
            {
                m_iLastTickReceived = 0;

                return;
            }
        }

        private void ProcessSendingTickStampedPackets(Packet pktPacket)
        {
            //reset the tick for game start events 
            if(pktPacket is ResetTickCountPacket)
            {
                //reset connection tick
                m_iLastPacketTickQueuedToSend = 0;
            }

            if(pktPacket is PingPacket)
            {
                m_iLastPacketTickQueuedToSend += TickStampedPacket.MaxTicksBetweenTickStampedPackets;
            }

            //set the offset for tick stamped packets
            if (pktPacket is TickStampedPacket)
            {
                TickStampedPacket tspPacket = pktPacket as TickStampedPacket;

                //check if the target tick between this packet and the next is too big
                while (tspPacket.m_iTick - m_iLastPacketTickQueuedToSend > TickStampedPacket.MaxTicksBetweenTickStampedPackets)
                {
                    //queue a ping packet 
                    QueuePacketToSend(new PingPacket());

                    //shift the tick forward 
                    m_iLastPacketTickQueuedToSend += TickStampedPacket.MaxTicksBetweenTickStampedPackets;
                }

                //set the packets offset from the previouse packet 
                tspPacket.SetOffset(m_iLastPacketTickQueuedToSend);

                //update the current tick
                m_iLastPacketTickQueuedToSend = tspPacket.m_iTick;

                return;
            }
        }

        //checks if this packet is just a connection maintanance packet 
        //or should be passed on to the rest of the game for processing
        private bool ShouldPacketBePassedOn(Packet pktPacket)
        {            
            return true;
        }

    }
}
