using System;
using System.Collections.Generic;
using UnityEngine;

namespace Networking
{

    public class NetworkConnection
    {
        // Defines a comparer to create a sorted set
        // that is sorted by the file extensions.
        private class PacketProcessorComparer : IComparer<BaseNetworkPacketProcessor>
        {
            public int Compare(BaseNetworkPacketProcessor x, BaseNetworkPacketProcessor y)
            {
                if (x == null && y == null)
                {
                    return 0;
                }

                if (x == null)
                {
                    return -1;
                }

                if (y == null)
                {
                    return 1;
                }

                return x.Priority - y.Priority;
            }
        }

        //is this peer the first peer in the swarm
        public bool m_bIsFirstPeer = false;

        //is this peer connected to a swarm
        public bool m_bIsConnectedToSwarm = false;

        //the tiem this peer connected to the swarm 
        public DateTime m_dtmConnectionTime = DateTime.MinValue;

        //list of all the network connection PacketManagers 
        public SortedSet<BaseNetworkPacketProcessor> NetworkPacketProcessors { get; } = new SortedSet<BaseNetworkPacketProcessor>(new PacketProcessorComparer());

        //all the connections 
        public Dictionary<long, Connection> ConnectionList { get; } = new Dictionary<long, Connection>();

        //the unique id for this player
        public long m_lPeerID;

        public delegate void PacketDataIn(byte bPlayerID, DataPacket pktInput);
        public event PacketDataIn m_evtPacketDataIn;

        //used to create packets 
        public ClassWithIDFactory PacketFactory { get; } = new ClassWithIDFactory();

        public IPeerTransmitterFactory m_ptfPeerTransmitterFactory;

        public NetworkConnection(long lUserID, IPeerTransmitterFactory ptfPeerTransmitterFactory)
        {
            //generate a unique ID
            // m_lPlayerUniqueID = SystemInfo.deviceUniqueIdentifier.GetHashCode();
            //m_lUserUniqueID = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
            m_lPeerID = lUserID;

            m_ptfPeerTransmitterFactory = ptfPeerTransmitterFactory;

        }

        public void OnCleanup()
        {
            foreach(Connection conConnection in ConnectionList.Values)
            {
                conConnection.OnCleanup();
            }
        }

        public void AddPacketProcessor(BaseNetworkPacketProcessor nppProcessor)
        {
            if (NetworkPacketProcessors.Add(nppProcessor) == false)
            {
                Debug.LogError("Packet Processor Failed To Add to connection");
            }

            nppProcessor.OnAddToNetwork(this);
        }

        public T GetPacketProcessor<T>() where T : BaseNetworkPacketProcessor
        {
            foreach (BaseNetworkPacketProcessor processor in NetworkPacketProcessors)
            {
                if (processor is T)
                {
                    return processor as T;
                }
            }

            return default(T);
        }

        public Connection CreateOrResetConnection(DateTime dtmNegotiationStart, long lPeerID)
        {
            if (ConnectionList.TryGetValue(lPeerID, out Connection conConnection))
            {
                ResetConnection(dtmNegotiationStart, lPeerID);

                return conConnection;
            }
            else
            {
                return CreateNewConnection(dtmNegotiationStart, lPeerID);
            }
        }

        public Connection CreateNewConnection(DateTime dtmNegotiationStart, long lPeerID)
        {
            Debug.Log($"Creating connection: {lPeerID}");

            //destroy any existing connection for this user 
            DestroyConnection(lPeerID);

            //create new connection
            Connection conNewConnection = new Connection(dtmNegotiationStart, this, lPeerID, PacketFactory, m_ptfPeerTransmitterFactory);

            //register with network manager
            RegisterConnection(conNewConnection);

            return conNewConnection;
        }

        public void ResetConnection(DateTime dtmResetTime, long lPeerID)
        {
            //try and get the target connection
            if (ConnectionList.TryGetValue(lPeerID, out Connection conConnection))
            {
                conConnection.Reset(dtmResetTime);
            }
        }

        public void DestroyConnection(long lUserID)
        {
            //check if connection already exists for user 
            if (ConnectionList.TryGetValue(lUserID, out Connection conTargetConnection))
            {
                Debug.Log($"Destroying connection: {lUserID}");

                //destroy connection
                conTargetConnection.DisconnectFromPeer();

                //remove from conenciton list
                ConnectionList.Remove(lUserID);

                //update all packet managers
                foreach (BaseNetworkPacketProcessor bppProcessor in NetworkPacketProcessors)
                {
                    //tell packet processor that user has disconnected
                    bppProcessor.OnConnectionDisconnect(conTargetConnection);
                }

                conTargetConnection.OnCleanup();
            }
        }

        public void RegisterConnection(Connection conNewConnection)
        {
            //add connection to connection list
            ConnectionList.Add(conNewConnection.m_lUserUniqueID, conNewConnection);

            //set connection values 
            conNewConnection.m_iMaxBytesToSend = 500;

            conNewConnection.m_tspConnectionTimeOutTime = TimeSpan.FromSeconds(4);

            conNewConnection.m_tspConnectionEstablishTimeOut = TimeSpan.FromSeconds(30);

            conNewConnection.m_tspMaxTimeBetweenMessages = TimeSpan.FromSeconds(0.5f);

            //process new connection 
            ProcessNewConnection(conNewConnection);
        }

        public void UpdateConnectionsAndProcessors()
        {
            //update all processors 
            foreach (BaseNetworkPacketProcessor nppProcessor in NetworkPacketProcessors)
            {
                nppProcessor.Update();
            }

            //update connections with current tick
            foreach (Connection conConnection in ConnectionList.Values)
            {
                conConnection.UpdateConnection();
            }
        }

        //send packet to all connected players 
        public void TransmitPacketToAll(DataPacket pktPacket)
        {
            foreach (Connection conConnection in ConnectionList.Values)
            {
                //process packet for sending 
                SendPacket(conConnection, pktPacket);
            }
        }

        //get the number of conenctions that are functioning correctly 
        public int ActiveConnectionCount()
        {
            int iConnectionCount = 0;

            foreach (Connection conConnection in ConnectionList.Values)
            {
                if (conConnection.Status == Connection.ConnectionStatus.Connected)
                {
                    iConnectionCount++;
                }
            }

            return iConnectionCount;
        }

        public void OnFirstPeerInSwarm()
        {
            foreach (BaseNetworkPacketProcessor nppProcessor in NetworkPacketProcessors)
            {
                m_bIsFirstPeer = true;
                nppProcessor.OnFirstPeerInSwarm();
            }
        }

        public void OnConnectToSwarm()
        {
            if (m_bIsConnectedToSwarm == false)
            {
                m_bIsConnectedToSwarm = true;

                //store the base time of connection
                m_dtmConnectionTime = GetPacketProcessor<TimeNetworkProcessor>().BaseTime;

                foreach (BaseNetworkPacketProcessor nppProcessor in NetworkPacketProcessors)
                {
                    nppProcessor.OnConnectToSwarm();
                }
            }
        }

        public void SendPacket(long lPlayerID, DataPacket pktPacket)
        {
            //get connection for ID
            if (ConnectionList.TryGetValue(lPlayerID, out Connection conConnection))
            {
                SendPacket(conConnection, pktPacket);
            }
        }

        //send a packet out to a specific connection 
        public void SendPacket(Connection conConnection, DataPacket pktPacket)
        {
            //process packet for sending 
            pktPacket = SendingPacketNetworkProcesses(conConnection.m_lUserUniqueID, pktPacket);

            if (pktPacket == null)
            {
                Debug.Log($"Sending of packet{pktPacket.ToString()} blocked by network process");
                return;
            }

            conConnection.QueuePacketToSend(pktPacket);
        }

        public void ProcessRecievedPacket(long lUserID, DataPacket pktPacket)
        {
            ReceivedPacketNetworkProcesses(lUserID, pktPacket);
        }

        protected void ProcessPacket(byte bPlayerConnection, DataPacket pktPacket)
        {
            //check if packet was for networking only
            switch (pktPacket.GetTypeID)
            {
                default:
                    //fire event 
                    m_evtPacketDataIn?.Invoke(bPlayerConnection, pktPacket);
                    break;
            }
        }

        protected DataPacket SendingPacketNetworkProcesses(long lUserID, DataPacket pktPacket)
        {
            foreach (BaseNetworkPacketProcessor nppProcessor in NetworkPacketProcessors)
            {
                pktPacket = nppProcessor.ProcessPacketForSending(lUserID, pktPacket);

                if (pktPacket == null)
                {
                    return null;
                }
            }

            return pktPacket;
        }

        protected DataPacket ReceivedPacketNetworkProcesses(long lUserID, DataPacket pktPacket)
        {
            foreach (BaseNetworkPacketProcessor nppProcessor in NetworkPacketProcessors)
            {
                pktPacket = nppProcessor.ProcessReceivedPacket(lUserID, pktPacket);

                if (pktPacket == null)
                {
                    return null;
                }
            }

            return pktPacket;
        }

        protected void ProcessNewConnection(Connection conConnection)
        {
            //process the new connection 
            foreach (BaseNetworkPacketProcessor nppProcessor in NetworkPacketProcessors)
            {
                nppProcessor.OnNewConnection(conConnection);
            }
        }
    }
}
