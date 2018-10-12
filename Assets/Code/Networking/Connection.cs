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

        // the packet number of the last packet sent
        protected int m_iLastPacketNumberQueuedToSend;

        // the tick of the last tick stamped packet sent
        protected int m_iLastPacketTickQueuedToSend;

        // the number of the last acknowledged packet received by connection target 
        protected int m_iLastAckPacketNumberSent;

        // the most recent packet received 
        protected int m_iTotalPacketsReceived;

        //the tick of the last packet recieved
        protected int m_iLastTickReceived;

        public Connection(byte ConnectionID)
        {
            m_pakReceivedPackets = new Queue<Packet>();
            m_PacketsInFlight = new RandomAccessQueue<Packet>();

            m_iLastPacketNumberQueuedToSend = 0;
            m_iLastAckPacketNumberSent = 0;
            m_iTotalPacketsReceived = 0;
        }

        //check if a ping packet is needed to keep the connection alive
        public void UpdateConnection(int iTick)
        {
            //check if a ping packet is needed
            if (iTick - m_iLastPacketNumberQueuedToSend >= byte.MaxValue)
            {
                //add a ping packet to send list to maintain connection
                QueuePacketToSend(new PingPacket(iTick));
            }

            //send packets to target
            SendPackets();
        }

        public void QueuePacketToSend(Packet packet)
        {
            m_iLastPacketNumberQueuedToSend++;

            m_PacketsInFlight.Enqueue(packet);
        }

        public void ReceivePacket(PacketWrapper packetWrapper)
        {
            //update the last ack packet sent from this client to the connection target clamped to not be more than the total number of packets sent
            m_iLastAckPacketNumberSent = Mathf.Min( Mathf.Max(m_iLastAckPacketNumberSent, packetWrapper.m_iLastAckPackageFromPerson),m_iLastPacketNumberQueuedToSend);

            //get the tick of the oldest packet in the packet wrapper 
            int iPacketNumberHead = packetWrapper.m_iStartPacketNumber;

            //the packet read head 
            int iPacketReadHead = 0;

            //the length of the data 
            int iPacketReadTail = packetWrapper.m_Payload.Count;

            //decode the remaining packets 
            while (iPacketReadHead < iPacketReadTail)
            {
                iPacketNumberHead++;

                //decode packet
                Packet pktDecodedPacket = DecodePacket(packetWrapper, ref iPacketReadHead);

                //add packet to list of packets to be processed 
                QueuePacket(pktDecodedPacket, iPacketNumberHead);
            }

        }

        private void SendPackets()
        {
            //check if there is anything to send
            if(m_PacketsInFlight.Count == 0)
            {
                return;
            }

            //calculate the number of packets that dont need to be sent 
            int iPacketsToDrop = m_PacketsInFlight.Count - (m_iLastPacketNumberQueuedToSend - m_iLastAckPacketNumberSent);

            if(iPacketsToDrop >= m_PacketsInFlight.Count)
            {
                m_PacketsInFlight.Clear();

                return;
            }

            //dequeue all the old packets that have already been acknowledged
            for(int i = 0; i < iPacketsToDrop; i++)
            {
                m_PacketsInFlight.Dequeue();
            }

            if (m_PacketsInFlight.Count == 0)
            {
                return;
            }

            //create packet wrapper 
            PacketWrapper pkwPacketWrappepr = new PacketWrapper(m_iTotalPacketsReceived, m_PacketsInFlight.PeakDequeue().m_iPacketNumber, m_PacketsInFlight.Count);

            //add as many packets as possible without hitting the max send data limit
            for (int i = 0; i < m_PacketsInFlight.Count; i++ )
            {
                pkwPacketWrappepr.AddDataPacket(m_PacketsInFlight[i]);
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


            //decode Packet 
            switch (ptyPacketType)
            {
                case Packet.PacketType.Ping:
                    return new PingPacket(packetWrapper, ref iReadHead );

                case Packet.PacketType.Input:
                    return new InputPacket(packetWrapper, ref iReadHead );
            }

            return null;
        }

        private void QueuePacket(Packet pktPacket, int iPacketNumber)
        {
            //check if packet has already been queued 
            if (iPacketNumber <= m_iTotalPacketsReceived)
            {
                return;
            }

            //update the most recent tick
            m_iTotalPacketsReceived = iPacketNumber;
            
            //do some processing (should clean up)
            if(pktPacket is TickStampedPacket)
            {
                //process the tick offset
                ProcessTickStampedPackets(pktPacket as TickStampedPacket);
            }
                       
            //queue up the packet 
            m_pakReceivedPackets.Enqueue(pktPacket);
        }

        //process packets that have a tick stamp
        private void ProcessTickStampedPackets(TickStampedPacket tspPacket)
        {
            //update the tick of the connection target 
            m_iLastTickReceived += tspPacket.Offset;

            //set the tick of the packet 
            tspPacket.m_iTick = m_iLastTickReceived;
        }
    }
}
