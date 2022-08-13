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

        //a unique id used to identify a player before game starts
        public long m_lUserUniqueID;

        // the max number of bytes to send at once
        public int m_iMaxBytesToSend;

        //how many un acknowledged packets should be sent before waiting for an acknowledgement
        public int m_iMaxPackestInFlight;

        // the time this connection was initalised
        public DateTime m_dtmConnectionSetupStart;

        //the time this connection was established
        public DateTime m_dtmConnectionEstablishTime;

        //the last time a message was recieved
        public DateTime m_dtmTimeOfLastActivity;

        //the last time a message was sent 
        public DateTime m_dtmTimeOfLastMessageSent;

        public bool m_bSendConnectionMessage;

        // the max time a connected connection can go without recieving 
        //a message before it is considered disconnected
        public TimeSpan m_tspConnectionTimeOutTime;

        //the amount of time to wait for a connection to be established 
        public TimeSpan m_tspConnectionEstablishTimeOut;

        //the maximum amount of time to wait between sending messages 
        public TimeSpan m_tspMaxTimeBetweenMessages;

        // the max payload to send (max bytes - packet wrapper header)
        public int MaxPacketBytesToSend
        {
            get
            {
                return m_iMaxBytesToSend - PacketWrapper.HeaderSize;
            }
        }

        // messages created by the transmittion system used to establish a connection to another peer
        public Queue<string> TransmittionNegotiationMessages { get; } = new Queue<string>();

        // list of all the packets to send that have not yet acknowledged  
        public RandomAccessQueue<DataPacket> PacketsInFlight { get; } = new RandomAccessQueue<DataPacket>();
             
        //used to create packets 
        public ClassWithIDFactory m_cifPacketFactory;

        //the current state of the connection
        public ConnectionStatus Status { get; private set; } = ConnectionStatus.New;

        //when the connection is created or a reconnection is triggered this is used to
        //create a new peer transmitter
        public IPeerTransmitterFactory m_ptfTransmitterFactory;

        //the transmitter used to send data through the internet
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

        //does this peer need to send an acknowledgement of packets
        //recieved back to sender 
        protected bool m_bNeedToSendAckPacket;

        //the peer network this connection is being managed by
        protected NetworkConnection m_ncnParentNetworkConneciton;

        public Connection(DateTime dtmNegotiationStart, NetworkConnection ncnParetnNetwork, long lUserUniqueID, ClassWithIDFactory cifPacketFactory, IPeerTransmitterFactory ptfPeerFactory )
        {
            m_dtmConnectionSetupStart = dtmNegotiationStart;

            m_dtmTimeOfLastActivity = DateTime.UtcNow; 

            m_dtmConnectionEstablishTime = DateTime.MinValue;

            m_dtmTimeOfLastMessageSent = DateTime.UtcNow;

            m_ncnParentNetworkConneciton = ncnParetnNetwork;

            m_lUserUniqueID = lUserUniqueID;

            m_cifPacketFactory = cifPacketFactory;

            m_ptfTransmitterFactory = ptfPeerFactory;

            m_ptrTransmitter = m_ptfTransmitterFactory.CreatePeerTransmitter();

            //listen for negotiation messages
            m_ptrTransmitter.OnNegotiationMessageCreated += OnNegoriationMessageFromTransmitter;

            //listen for data sent over the transmittion system
            m_ptrTransmitter.OnDataReceive += ReceivePacket;

            //listen for establishment of a connection to another peer
            m_ptrTransmitter.OnConnectionEstablished += OnConnectionEstablished;

            //listen for connection being disconnected 
            m_ptrTransmitter.OnConnectionLost += OnConnectionLost;

            m_iPacketsQueuedToSendCount = 0;
            m_iLastAckPacketNumberSent = 0;
            m_iTotalPacketsReceived = 0;
            m_bNeedToSendAckPacket = false;

            SetStatus(ConnectionStatus.Initializing);
        }

        public void Reset(DateTime dtmResetTime)
        {
            //reset conneciton start time
            m_dtmConnectionSetupStart = dtmResetTime;

            //reset the last time of activity
            m_dtmTimeOfLastActivity = DateTime.UtcNow;
                       
            //reset connection establish time
            m_dtmConnectionEstablishTime = DateTime.MinValue;

            //reset the last time a message was sent
            m_dtmTimeOfLastMessageSent = DateTime.UtcNow;

            //remove any stored date from previouse connection
            //stored in packet processors 
            OnConnectionReset();

            //clean up existing transmitter
            m_ptrTransmitter?.OnCleanup();

            //reset the peer transmitter
            m_ptrTransmitter = m_ptfTransmitterFactory.CreatePeerTransmitter();

            //subscribe to new negotiation messages from new transmitter
            m_ptrTransmitter.OnNegotiationMessageCreated += OnNegoriationMessageFromTransmitter;

            //listen for data sent over the transmittion system
            m_ptrTransmitter.OnDataReceive += ReceivePacket;

            //listen for establishment of a connection to another peer
            m_ptrTransmitter.OnConnectionEstablished += OnConnectionEstablished;

            //listen for connection being disconnected 
            m_ptrTransmitter.OnConnectionLost += OnConnectionLost;

            //reset sent packet tracking values
            m_iPacketsQueuedToSendCount = 0;
            m_iLastAckPacketNumberSent = 0;
            m_iTotalPacketsReceived = 0;
            m_bNeedToSendAckPacket = false;

            //reset transmission negotiation messages
            TransmittionNegotiationMessages.Clear();

            //clear any packets in flight
            PacketsInFlight.Clear();

            SetStatus(ConnectionStatus.Initializing);
        }

        public void OnCleanup()
        {
            DisconnectFromPeer();

            m_ptrTransmitter?.OnCleanup();
            m_ptrTransmitter = null;
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
            m_dtmTimeOfLastActivity = DateTime.UtcNow;
            m_ptrTransmitter.ProcessNegotiationMessage(strConnectionData);
        }

        public void UpdateConnectionState()
        {

        }

        public void DisconnectFromPeer()
        {
            //make sure not already disconnecting
            if(Status == ConnectionStatus.Disconnecting || Status == ConnectionStatus.Disconnected)
            {
                return;
            }

            SetStatus(ConnectionStatus.Disconnecting);

            m_ptrTransmitter.Disconnect();           
        }

        protected void OnNegoriationMessageFromTransmitter(string strMessageJson)
        {
            Debug.Log($"Negotiation message:{strMessageJson} created by transmitter on peer {m_ncnParentNetworkConneciton.m_lPeerID}");
            TransmittionNegotiationMessages.Enqueue(strMessageJson);
        }
        #endregion

        protected void OnConnectionEstablished()
        {      
            //check that we are transittioning from correct state
            if (Status == ConnectionStatus.Initializing)
            {
                SetStatus(ConnectionStatus.Connected);
            }

            m_dtmTimeOfLastActivity = DateTime.UtcNow;

            m_bSendConnectionMessage = true;

            //store the time connection was established 
            m_dtmConnectionEstablishTime = m_ncnParentNetworkConneciton.GetPacketProcessor<TimeNetworkProcessor>().BaseTime;

            //inform parent that now part of the swarm 
            m_ncnParentNetworkConneciton.OnConnectToSwarm();

            //send a dummy message 

        }
        
        protected void OnConnectionLost()
        {
            SetStatus(ConnectionStatus.Disconnected);
        }

        public void OnConnectionReset()
        {
            foreach (BaseConnectionPacketProcessor cppProcessor in OrderedPacketProcessorList)
            {
                cppProcessor.OnConnectionReset();
            }
        }

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
            //check if this connection is still valid 
            CheckForDisconnect();

            //update all packet processors 
            UpdatePacketProcessors();

            //send packets to target
            SendPackets();
        }

        public void CheckForDisconnect()
        {
            if (Status == ConnectionStatus.New || Status == ConnectionStatus.Disconnecting || Status == ConnectionStatus.Disconnected)
            {
                return;
            }

            TimeSpan tspTimeSinceLastMessage = DateTime.UtcNow - m_dtmTimeOfLastActivity;

            //allow extra time for connection messages to get through
            if (Status == ConnectionStatus.Initializing)
            {
                if (tspTimeSinceLastMessage > m_tspConnectionEstablishTimeOut)
                {
                    DisconnectFromPeer();
                }
            }
            else
            {
                if (tspTimeSinceLastMessage > m_tspConnectionTimeOutTime)
                {
                    DisconnectFromPeer();
                }
            }
        }
        
        public void ReceivePacket(byte[] bData)
        {
            //update the time since last message 
            m_dtmTimeOfLastActivity = DateTime.UtcNow;

            //convert raw data to packet wrapper 
            PacketWrapper packetWrapper = new PacketWrapper(bData);

            //get the tick of the oldest packet in the packet wrapper 
            int iPacketNumberHead = packetWrapper.StartPacketNumber;

            //update the last ack packet sent from this client to the connection target clamped to not be more than the total number of packets sent
            m_iLastAckPacketNumberSent = Mathf.Min(Mathf.Max(m_iLastAckPacketNumberSent, packetWrapper.LastAckPackageFromPerson), m_iPacketsQueuedToSendCount);

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

                    //indicate that an acknowledgement needs to be sent to packet sender
                    m_bNeedToSendAckPacket = true;
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

        #region PacketProcessors 
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
                //send packets to network packet manager for further processing
                m_ncnParentNetworkConneciton.ProcessRecievedPacket(m_lUserUniqueID, pktPacket);
            }
        }
        
        //count the ammount of data that needs to be sent but has not yet 
        public int BytesQueued()
        {
            int iBytesQueued = 0;

            for(int i = 0; i < PacketsInFlight.Count; i++)
            {
                iBytesQueued += PacketsInFlight[i].PacketTotalSize;
            }

            return iBytesQueued;
        }

        //is there more data queued than can be sent in a single packet
        public bool IsChannelOverCapacity()
        {
            return BytesQueued() > MaxPacketBytesToSend;
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

        #endregion

        private void SendPackets()
        {
            //check if connected and able to send packets
            if(Status != ConnectionStatus.Connected)
            {
                return;
            }

            bool bForceSendMessage = false;

            //send a message if jsut connected 
            bForceSendMessage = m_bSendConnectionMessage;

            //check if max time between packet sends has been reached
            if (m_tspMaxTimeBetweenMessages < DateTime.UtcNow - m_dtmTimeOfLastMessageSent)
            {
                bForceSendMessage = true;
            }

            //check if there is anything to send or an acknowledgement needs to be sent
            if (PacketsInFlight.Count == 0 && m_bNeedToSendAckPacket == false && bForceSendMessage == false)
            {
                return;
            }

            //calculate the number of packets that dont need to be sent 
            int iPacketsToDrop = PacketsInFlight.Count - (m_iPacketsQueuedToSendCount - m_iLastAckPacketNumberSent);

            if (iPacketsToDrop >= PacketsInFlight.Count)
            {
                PacketsInFlight.Clear();

            }
            else
            {
                //dequeue all the old packets that have already been acknowledged
                for (int i = 0; i < iPacketsToDrop; i++)
                {
                    PacketsInFlight.Dequeue();
                }
            }

            //check if there is still a need to send a packet
            if (PacketsInFlight.Count == 0 && m_bNeedToSendAckPacket == false && bForceSendMessage == false)
            {
                return;
            }

            int iPacketStart = (m_iPacketsQueuedToSendCount - PacketsInFlight.Count);

            //create packet wrapper 
            PacketWrapper pkwPacketWrappepr = new PacketWrapper(m_iTotalPacketsReceived, iPacketStart, m_iMaxBytesToSend);

            //TODO: Remove This
            List<string> strPacketTypesInPacket = new List<string>();

            int iPacketsSent = 0;

            //add as many packets as possible without hitting the max send data limit
            for (int i = 0; i < PacketsInFlight.Count ; i++)
            {
                DataPacket pktPacketToSend = PacketsInFlight[i];

                if (pkwPacketWrappepr.WriteStream.BytesRemaining - pktPacketToSend.PacketTotalSize >= 0)
                {
                    pkwPacketWrappepr.AddDataPacket(pktPacketToSend);

                    strPacketTypesInPacket.Add(pktPacketToSend.ToString());
                }
                else if (iPacketsSent < (m_iMaxPackestInFlight - 1) && i < PacketsInFlight.Count - 1)
                {
                    //send packet through transmitter
                    m_ptrTransmitter.SentData(pkwPacketWrappepr.WriteStream.GetData());

                    //setup new packet
                    pkwPacketWrappepr = new PacketWrapper(m_iTotalPacketsReceived, iPacketStart + i, m_iMaxBytesToSend);

                    i--;

                }
                else if(iPacketsSent == (m_iMaxPackestInFlight - 1))
                { 
                    string strPacketDataInPacket = "";

                    foreach (string strPacketDetails in strPacketTypesInPacket)
                    {
                        strPacketDataInPacket += ", " + strPacketDetails;
                    }

                    Debug.Log($"Connection from User:{m_ncnParentNetworkConneciton.m_lPeerID} to User:{m_lUserUniqueID} is Saturated with data: {strPacketDataInPacket} , Bytes Remaining in packet : {pkwPacketWrappepr.WriteStream.BytesRemaining} Packet that failed to add is { pktPacketToSend.PacketTotalSize} bytes");

                    break;
                }
            }

            //send packet through transmitter
            m_ptrTransmitter.SentData(pkwPacketWrappepr.WriteStream.GetData());


            //dont need to send first connection message any more
            m_bSendConnectionMessage = false;

            //stop sending acks for packets
            m_bNeedToSendAckPacket = false;

            //update the time of last packet sent
            m_dtmTimeOfLastMessageSent = DateTime.UtcNow;

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
            if (iPacketNumber != (m_iTotalPacketsReceived + 1))
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
