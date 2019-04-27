using System;
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

        // Defines a comparer to create a sorted set
        // this allows control over what order the packet processing is done 
        private class PacketProcessorComparer : IComparer<ConnectionPacketProcessor>
        {
            public int Compare(ConnectionPacketProcessor x, ConnectionPacketProcessor y)
            {
                if(x == null && y == null)
                {
                    return 0;
                }

                if(x == null)
                {
                    return -1;
                }

                if(y == null)
                {
                    return 1;
                }

                return x.Priority - y.Priority;
            }
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
        public Queue<DataPacket> m_pakReceivedPackets;

        // list of all the packets to send that have not yet acknowledged  
        public RandomAccessQueue<DataPacket> m_PacketsInFlight;
             
        //used to create packets 
        public ClassWithIDFactory m_cifPacketFactory;

        // list of all the packet processors 
        protected SortedSet<ConnectionPacketProcessor> m_cppOrderedPacketProcessorList;

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

        public Connection(byte bConnectionID, ClassWithIDFactory cifPacketFactory)
        {
            m_bConnectionID = bConnectionID;

            m_cifPacketFactory = cifPacketFactory;

            m_pakReceivedPackets = new Queue<DataPacket>();
            m_PacketsInFlight = new RandomAccessQueue<DataPacket>();
            m_cppOrderedPacketProcessorList = new SortedSet<ConnectionPacketProcessor>(new PacketProcessorComparer());

            m_iPacketsQueuedToSendCount = 0;
            m_iLastAckPacketNumberSent = 0;
            m_iTotalPacketsReceived = 0;
        }

        //check if a ping packet is needed to keep the connection alive
        public void UpdateConnection()
        {
            //update all packet processors 
            UpdatePacketProcessors();

            //send packets to target
            SendPackets();
        }
        
        public void ReceivePacket(byte[] bData)
        {
            //convert raw data to packet wrapper 
            PacketWrapper packetWrapper = new PacketWrapper(bData);

            //update the last ack packet sent from this client to the connection target clamped to not be more than the total number of packets sent
            m_iLastAckPacketNumberSent = Mathf.Min(Mathf.Max(m_iLastAckPacketNumberSent, packetWrapper.LastAckPackageFromPerson), m_iPacketsQueuedToSendCount);

            //get the tick of the oldest packet in the packet wrapper 
            int iPacketNumberHead = packetWrapper.StartPacketNumber;

            //decode the remaining packets 
            while (packetWrapper.ReadStream.EndOfStream() == false)
            {
                //decode packet
                DataPacket pktDecodedPacket = DecodePacket(packetWrapper);

                iPacketNumberHead++;

                //add packet to list of packets to be processed 
                QueueReceivedPacket(pktDecodedPacket, iPacketNumberHead);
            }
        }

        public bool QueuePacketToSend(DataPacket pktPacket)
        {
            m_iPacketsQueuedToSendCount++;

            pktPacket = ProccessPacketForSending(pktPacket);

            //check if packet should be sent
            if(pktPacket != null)
            {
                m_PacketsInFlight.Enqueue(pktPacket);

                return true;
            }           

            return false;
        }

        public void AddPacketProcessor(ConnectionPacketProcessor cppProcessor)
        {
            if(m_cppOrderedPacketProcessorList.Add(cppProcessor) == false)
            {
                Debug.LogError("Packet Processor Failed To Add to connection");
            }
        }

        public T GetPacketProcessor<T>() where T : ConnectionPacketProcessor
        {
            foreach(ConnectionPacketProcessor cppProcessor in m_cppOrderedPacketProcessorList)
            {
                if(cppProcessor is T)
                {
                    return cppProcessor as T;
                }
            }

            return null;
        }

        private DataPacket ProccessPacketForSending(DataPacket pktPacket)
        {
            //loop through all the packet processors 
            foreach(ConnectionPacketProcessor cppProcessor in m_cppOrderedPacketProcessorList)
            {
                //process packet 
                pktPacket = cppProcessor.ProcessPacketForSending(this,pktPacket);

                //check if packet is still going to get sent 
                if(pktPacket == null)
                {
                    return null;
                }
            }

            return pktPacket;
        }

        private DataPacket ProccessReceivedPacket(DataPacket pktPacket)
        {
            //loop through all the packet processors 
            foreach (ConnectionPacketProcessor cppProcessor in m_cppOrderedPacketProcessorList)
            {
                //process packet 
                pktPacket = cppProcessor.ProcessReceivedPacket(this,pktPacket);

                //check if packet is still going to get sent 
                if (pktPacket == null)
                {
                    return null;
                }
            }

            return pktPacket;
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
            PacketWrapper pkwPacketWrappepr = new PacketWrapper(m_iTotalPacketsReceived, m_iPacketsQueuedToSendCount - m_PacketsInFlight.Count, m_iMaxBytesToSend);
     
            //add as many packets as possible without hitting the max send data limit
            for (int i = 0; i < m_PacketsInFlight.Count; i++)
            {
                DataPacket pktPacketToSend = m_PacketsInFlight[i];

                if (pkwPacketWrappepr.WriteStream.BytesRemaining - pktPacketToSend.PacketSize > 0)
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

        //takes the binary data in the packet wrapper and converts it to a data packet
        private DataPacket DecodePacket(PacketWrapper packetWrapper)
        {
            //get packet type 
            int iPacketType = DataPacket.GetPacketType(packetWrapper);

            //create the packet class that will be instantiated 
            DataPacket pktOutput = m_cifPacketFactory.CreateType<DataPacket>(iPacketType);
                        
            //decode packet
            pktOutput.DecodePacket(packetWrapper);

            return pktOutput;
        }

        private void QueueReceivedPacket(DataPacket pktPacket, int iPacketNumber)
        {
            //check if packet has already been queued 
            if (iPacketNumber <= m_iTotalPacketsReceived)
            {
                return;
            }

            //update the most recent packet number
            m_iTotalPacketsReceived = iPacketNumber;

            pktPacket = ProccessReceivedPacket(pktPacket);

            //check if this packet should be passed on to be processed by the rest of the game 
            if (pktPacket != null)
            {
                //queue up the packet 
                m_pakReceivedPackets.Enqueue(pktPacket);
            }
        }

        ////process packets that have a tick stamp
        //private void ProcessReceivedTickStampedPackets(Packet pktPacket)
        //{
        //    //check if packet is ping packet 
        //    if (pktPacket is PingPacket)
        //    {
        //        m_iLastTickReceived += TickStampedPacket.MaxTicksBetweenTickStampedPackets;
        //
        //        return;
        //    }
        //
        //    if (pktPacket is TickStampedPacket)
        //    {
        //        TickStampedPacket tspPacket = pktPacket as TickStampedPacket;
        //
        //        //update the tick of the connection target 
        //        m_iLastTickReceived += tspPacket.Offset;
        //
        //        //set the tick of the packet 
        //        tspPacket.m_iTick = m_iLastTickReceived;
        //
        //        return;
        //    }
        //
        //    if(pktPacket is ResetTickCountPacket)
        //    {
        //        m_iLastTickReceived = 0;
        //
        //        return;
        //    }
        //}

       // private void ProcessSendingTickStampedPackets(Packet pktPacket)
       // {
       //     //reset the tick for game start events 
       //     if(pktPacket is ResetTickCountPacket)
       //     {
       //         //reset connection tick
       //         m_iLastPacketTickQueuedToSend = 0;
       //     }
       //
       //     if(pktPacket is PingPacket)
       //     {
       //         m_iLastPacketTickQueuedToSend += TickStampedPacket.MaxTicksBetweenTickStampedPackets;
       //     }
       //
       //     //set the offset for tick stamped packets
       //     if (pktPacket is TickStampedPacket)
       //     {
       //         TickStampedPacket tspPacket = pktPacket as TickStampedPacket;
       //
       //         //check if the target tick between this packet and the next is too big
       //         while (tspPacket.m_iTick - m_iLastPacketTickQueuedToSend > TickStampedPacket.MaxTicksBetweenTickStampedPackets)
       //         {
       //             //queue a ping packet 
       //             QueuePacketToSend(new PingPacket());
       //
       //             //shift the tick forward 
       //             m_iLastPacketTickQueuedToSend += TickStampedPacket.MaxTicksBetweenTickStampedPackets;
       //         }
       //
       //         //set the packets offset from the previouse packet 
       //         tspPacket.SetOffset(m_iLastPacketTickQueuedToSend);
       //
       //         //update the current tick
       //         m_iLastPacketTickQueuedToSend = tspPacket.m_iTick;
       //
       //         return;
       //     }
       // }

        private void UpdatePacketProcessors()
        {
            foreach(ConnectionPacketProcessor cppProcessor in m_cppOrderedPacketProcessorList)
            {
                cppProcessor.Update(this);
            }
        }

    }
}
