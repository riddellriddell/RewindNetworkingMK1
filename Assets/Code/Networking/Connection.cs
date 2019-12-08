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
            Initializing,
            Connected,
            Disconnecting,
            Disconnected
        }

        // Defines a comparer to create a sorted set
        // this allows control over what order the packet processing is done 
        private class PacketProcessorComparer : IComparer<BaseConnectionPacketProcessor>
        {
            public int Compare(BaseConnectionPacketProcessor x, BaseConnectionPacketProcessor y)
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

        //the player id associated with this channel 
        [Obsolete]
        public byte m_bConnectionID;

        //a unique id used to identify a player before game starts
        public long m_lUserUniqueID;

        // the max number of bytes to send at once
        public int m_iMaxBytesToSend;

        // the time this connection was initalised
        public DateTime m_conConnectionSetupStart;

        //the time this connection was established
        public DateTime m_dtmConnectionEstablishTime;

        // the max payload to send (max bytes - packet wrapper header)
        public int MaxPacketBytesToSend
        {
            get
            {
                return m_iMaxBytesToSend - PacketWrapper.HeaderSize;
            }
        }

        // list of all the packets that have been received but not yet processed 
        [Obsolete]
        public Queue<DataPacket> ReceivedPackets { get; } = new Queue<DataPacket>();

        // messages created by the transmittion system used to establish a connection to another peer
        public Queue<string> TransmittionNegotiationMessages { get; } = new Queue<string>();

        // list of all the packets to send that have not yet acknowledged  
        public RandomAccessQueue<DataPacket> PacketsInFlight { get; } = new RandomAccessQueue<DataPacket>();
             
        //used to create packets 
        public ClassWithIDFactory m_cifPacketFactory;

        //the current state of the connection
        public ConnectionStatus Status { get; private set; } = ConnectionStatus.Initializing;

        public  IPeerTransmitter m_ptrTransmitter;

        // list of all the packet processors 
        protected SortedSet<BaseConnectionPacketProcessor> OrderedPacketProcessorList { get; } = new SortedSet<BaseConnectionPacketProcessor>(new PacketProcessorComparer());

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

        //the peer network this connection is being managed by
        protected NetworkConnection m_ncnParentNetworkConneciton;

        [Obsolete]
        public Connection(byte bConnectionID, ClassWithIDFactory cifPacketFactory)
        {
            m_bConnectionID = bConnectionID;

            m_cifPacketFactory = cifPacketFactory;

            ReceivedPackets = new Queue<DataPacket>();
            PacketsInFlight = new RandomAccessQueue<DataPacket>();
            OrderedPacketProcessorList = new SortedSet<BaseConnectionPacketProcessor>(new PacketProcessorComparer());

            m_iPacketsQueuedToSendCount = 0;
            m_iLastAckPacketNumberSent = 0;
            m_iTotalPacketsReceived = 0;
        }

        public Connection(DateTime dtmNegotiationStart, NetworkConnection ncnParetnNetwork, long lUserUniqueID, ClassWithIDFactory cifPacketFactory, IPeerTransmitter ptrPeerTransmitter )
        {
            m_conConnectionSetupStart = dtmNegotiationStart;

            m_dtmConnectionEstablishTime = DateTime.MinValue;
                      
            SetStatus(ConnectionStatus.Initializing);

            m_ncnParentNetworkConneciton = ncnParetnNetwork;

            m_lUserUniqueID = lUserUniqueID;

            m_cifPacketFactory = cifPacketFactory;

            m_ptrTransmitter = ptrPeerTransmitter;

            //listen for negotiation messages
            m_ptrTransmitter.OnNegotiationMessageCreated += OnNegoriationMessageFromTransmitter;

            //listen for data sent over the transmittion system
            m_ptrTransmitter.OnDataReceive += ReceivePacket;

            //listen for establishment of a connection to another peer
            m_ptrTransmitter.OnConnectionEstablished += OnConnectionEstablished;

            m_iPacketsQueuedToSendCount = 0;
            m_iLastAckPacketNumberSent = 0;
            m_iTotalPacketsReceived = 0;
        }

        #region TransmittionHandling

        /// <summary>
        /// get transmittion settings from transmittion component 
        /// </summary>
        public void StartConnectionNegotiation()
        {
            m_ptrTransmitter.StartNegotiation();
        }

        //process network negotiation data
        public void ProcessNetworkNegotiationMessage(string strConnectionData)
        {
            m_ptrTransmitter.ProcessNegotiationMessage(strConnectionData);
        }

        public void DisconnectFromPeer()
        {
            m_ptrTransmitter.Disconnect();
        }
        
        protected void OnNegoriationMessageFromTransmitter(string strMessageJson)
        {
            Debug.Log($"Negotiation message:{strMessageJson} created by transmitter on peer {m_ncnParentNetworkConneciton.m_lUserUniqueID}");
            TransmittionNegotiationMessages.Enqueue(strMessageJson);
        }

        protected void OnConnectionEstablished()
        {
            //check that we are transittioning from correct state
            if (Status == ConnectionStatus.Initializing)
            {
                SetStatus(ConnectionStatus.Connected);
            }

            //store the time connection was established 
            m_dtmConnectionEstablishTime = m_ncnParentNetworkConneciton.GetPacketProcessor<TimeNetworkProcessor>().BaseTime;

            //inform parent that now part of the swarm 
            m_ncnParentNetworkConneciton.m_bIsConnectedToSwarm = true;
        }
        #endregion


        public void OnConnectionStateChange( Connection.ConnectionStatus cstOldState, Connection.ConnectionStatus cstNewState)
        {
            foreach(BaseConnectionPacketProcessor cppProcessor in OrderedPacketProcessorList)
            {
                cppProcessor.OnConnectionStateChange(cstOldState, cstNewState);
            }
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

                //check if packet is in order
                if(IsPacketInOrder(iPacketNumberHead))
                {
                    //update the most recent packet number
                    m_iTotalPacketsReceived = iPacketNumberHead;

                    //add packet to list of packets to be processed 
                    ProcessRecievedPacket(pktDecodedPacket);
                }                
            }
        }

        public bool QueuePacketToSend(DataPacket pktPacket)
        {

            DataPacket pktProcessedPacket = SendingPacketConnectionProcesses(pktPacket);

            //check if packet should be sent
            if(pktProcessedPacket != null)
            {
                PacketsInFlight.Enqueue(pktProcessedPacket);

                m_iPacketsQueuedToSendCount++;

                return true;
            }

            Debug.Log($"Sending of packet{pktPacket.ToString()} blocked by connection process");

            return false;
        }

        public void AddPacketProcessor(BaseConnectionPacketProcessor cppProcessor)
        {
            if(OrderedPacketProcessorList.Add(cppProcessor) == false)
            {
                Debug.LogError("Packet Processor Failed To Add to connection");
            }

            cppProcessor.SetConnection(this);

            cppProcessor.Start();
        }
               
        public T GetPacketProcessor<T>() where T : BaseConnectionPacketProcessor
        {
            foreach(BaseConnectionPacketProcessor cppProcessor in OrderedPacketProcessorList)
            {
                if(cppProcessor is T)
                {
                    return cppProcessor as T;
                }
            }

            return null;
        }

        public void ProcessRecievedPacket(DataPacket pktPacket)
        {
            //use the per connection packet processors to evaluate the packet
            pktPacket = RecievedPacketConnectionProcesses(pktPacket);

            //check if this packet should be passed on to be processed by the rest of the game 
            if (pktPacket != null)
            {
                //queue up the packet (remove in future)
                ReceivedPackets.Enqueue(pktPacket);

                //send packets to network packet manager for further processing
                m_ncnParentNetworkConneciton.ProcessRecievedPacket(m_lUserUniqueID, pktPacket);
            }
        }
        
        /// <summary>
        /// processes packet for sending and returns null if packet should not be sent
        /// </summary>
        /// <param name="pktPacket"></param>
        /// <returns></returns>
        private DataPacket SendingPacketConnectionProcesses(DataPacket pktPacket)
        {
            //loop through all the packet processors 
            foreach(BaseConnectionPacketProcessor cppProcessor in OrderedPacketProcessorList)
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

        /// <summary>
        /// runs the per connection packet processes to modifiy the data
        /// returns null if packet should not be processed more by network conneciton
        /// </summary>
        /// <param name="pktPacket"></param>
        /// <returns></returns>
        private DataPacket RecievedPacketConnectionProcesses(DataPacket pktPacket)
        {
            //loop through all the packet processors 
            foreach (BaseConnectionPacketProcessor cppProcessor in OrderedPacketProcessorList)
            {
                //process packet 
                pktPacket = cppProcessor.ProcessReceivedPacket(this,pktPacket);

                //check if packet is still going to get processed
                if (pktPacket == null)
                {
                    return null;
                }
            }

            return pktPacket;
        }

        private void SendPackets()
        {
            //check if connected and able to send packets
            if(Status != ConnectionStatus.Connected)
            {
                return;
            }

            //check if there is anything to send
            if (PacketsInFlight.Count == 0)
            {
                return;
            }

            //calculate the number of packets that dont need to be sent 
            int iPacketsToDrop = PacketsInFlight.Count - (m_iPacketsQueuedToSendCount - m_iLastAckPacketNumberSent);

            if (iPacketsToDrop >= PacketsInFlight.Count)
            {
                PacketsInFlight.Clear();

                return;
            }

            //dequeue all the old packets that have already been acknowledged
            for (int i = 0; i < iPacketsToDrop; i++)
            {
                PacketsInFlight.Dequeue();
            }

            if (PacketsInFlight.Count == 0)
            {
                return;
            }

            //create packet wrapper 
            PacketWrapper pkwPacketWrappepr = new PacketWrapper(m_iTotalPacketsReceived, m_iPacketsQueuedToSendCount - PacketsInFlight.Count, m_iMaxBytesToSend);

            //TODO: Remove This
            List<string> strPacketTypesInPacket = new List<string>();

            //add as many packets as possible without hitting the max send data limit
            for (int i = 0; i < PacketsInFlight.Count; i++)
            {
                DataPacket pktPacketToSend = PacketsInFlight[i];

                if (pkwPacketWrappepr.WriteStream.BytesRemaining - pktPacketToSend.PacketTotalSize >= 0)
                {
                    pkwPacketWrappepr.AddDataPacket(pktPacketToSend);

                    strPacketTypesInPacket.Add(pktPacketToSend.ToString());
                }
                else
                {
                    string strPacketDataInPacket = "";

                    foreach(string strPacketDetails in strPacketTypesInPacket)
                    {
                        strPacketDataInPacket +=  ", "  + strPacketDetails;
                    }


                    Debug.Log($"Connection from User:{m_ncnParentNetworkConneciton.m_lUserUniqueID} to User:{m_lUserUniqueID} is Saturated with data: {strPacketDataInPacket} , Bytes Remaining in packet : {pkwPacketWrappepr.WriteStream.BytesRemaining} Packet that failed to add is { pktPacketToSend.PacketTotalSize} bytes");

                    break;
                }
            }

            //send packet through transmitter
            m_ptrTransmitter.SentData(pkwPacketWrappepr.WriteStream.GetData());
        }

        //takes the binary data in the packet wrapper and converts it to a data packet
        private DataPacket DecodePacket(PacketWrapper packetWrapper)
        {
            //get packet type 
            int iPacketType = DataPacket.GetPacketType(packetWrapper);

            //create the packet class that will be instantiated 
            DataPacket pktOutput = m_cifPacketFactory.CreateType<DataPacket>(iPacketType);
                        
            //decode packet
            pktOutput.DecodePacket(packetWrapper.ReadStream);

            return pktOutput;
        }

        private bool IsPacketInOrder(int iPacketNumber)
        {
            //check if packet has already been queued 
            if (iPacketNumber <= m_iTotalPacketsReceived)
            {
                return false;
            }

            return true;
        }
        
        private void UpdatePacketProcessors()
        {
            foreach(BaseConnectionPacketProcessor cppProcessor in OrderedPacketProcessorList)
            {
                cppProcessor.Update();
            }
        }

        private void SetStatus(ConnectionStatus cstNewStatus)
        {
            ConnectionStatus cstOldStatus = Status;
            Status = cstNewStatus;

            OnConnectionStateChange(cstOldStatus, Status);
        }

    }
}
