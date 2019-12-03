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

        //the link through the internet to other clients
        public InternetConnectionSimulator m_icwConnectionSimulation;

        //list of all the network connection PacketManagers 
        public SortedSet<BaseNetworkPacketProcessor> NetworkPacketProcessors { get; } = new SortedSet<BaseNetworkPacketProcessor>(new PacketProcessorComparer());

        //all the connections 
        public Dictionary<long, Connection> ConnectionList { get; } = new Dictionary<long, Connection>();

        //the local id of the player
        [Obsolete]
        public byte m_bPlayerID;

        //the unique id for this player
        public int m_lUserUniqueID;

        public delegate void PacketDataIn(byte bPlayerID, DataPacket pktInput);
        public event PacketDataIn m_evtPacketDataIn;

        //used to create packets 
        public ClassWithIDFactory m_cifPacketFactory;

        public IPeerTransmitterFactory m_ptfPeerTransmitterFactory;

        public NetworkConnection(ClassWithIDFactory cifPacketFactory, InternetConnectionSimulator igaInternetGateway)
        {
            //generate a unique ID
            // m_lPlayerUniqueID = SystemInfo.deviceUniqueIdentifier.GetHashCode();
            m_lUserUniqueID = UnityEngine.Random.Range(int.MinValue, int.MaxValue);

            m_cifPacketFactory = cifPacketFactory;
            m_icwConnectionSimulation = igaInternetGateway;

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

        //when connection is first made default to the connection Tick 
        [Obsolete]
        public void MakeFirstConnection(int startTick)
        {

        }

        public Connection CreateNewConnection(long lUserUniqueID)
        {
            //check if connection already exists for user 
            if (ConnectionList.TryGetValue(lUserUniqueID, out Connection conTargetConnection))
            {
                //destroy connection
                conTargetConnection.DisconnectFromPeer();

                //remove from conenciton list
                ConnectionList.Remove(lUserUniqueID);
            }

            //create new peer connection
            IPeerTransmitter ptrPeerTransmitter = m_ptfPeerTransmitterFactory.CreatePeerTransmitter();

            //create new connection
            Connection conNewConnection = new Connection(this, lUserUniqueID, m_cifPacketFactory, ptrPeerTransmitter);

            //register with network manager
            RegisterConnection(conNewConnection);

            return conNewConnection;
        }

        public void RegisterConnection(Connection conNewConnection)
        {
            //add connection to connection list
            ConnectionList.Add(conNewConnection.m_lUserUniqueID, conNewConnection);

            //set connection values 
            conNewConnection.m_iMaxBytesToSend = 500;

            //process new connection 
            ProcessNewConnection(conNewConnection);
        }

        public void MakeTestingConnection(NetworkConnection nwcConnectionTarget)
        {
            this.m_bPlayerID = 0;
            nwcConnectionTarget.m_bPlayerID = 1;

            //create new connection 
            Connection m_conLocalConnection = new Connection(nwcConnectionTarget.m_bPlayerID, m_cifPacketFactory);
            m_conLocalConnection.m_icsConnectionSim = m_icwConnectionSimulation;

            Connection m_conTargetConnection = new Connection(m_bPlayerID, nwcConnectionTarget.m_cifPacketFactory);
            m_conTargetConnection.m_icsConnectionSim = m_icwConnectionSimulation;

            m_conLocalConnection.m_conConnectionTarget = m_conTargetConnection;
            m_conTargetConnection.m_conConnectionTarget = m_conLocalConnection;

            RegisterConnection(m_conLocalConnection);
            nwcConnectionTarget.RegisterConnection(m_conTargetConnection);



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
                SendPackage(conConnection.m_lUserUniqueID, pktPacket);
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

        //get the synchronised network time 
        public float NetworkTime()
        {
            return Time.timeSinceLevelLoad;
        }

        //send a packet out to a specific connection 
        public void SendPackage(long lPlayerID, DataPacket pktPacket)
        {
            //get connection for ID
            if (ConnectionList.TryGetValue(lPlayerID, out Connection conConnection))
            {
                //process packet for sending 
                pktPacket = SendingPacketNetworkProcesses(lPlayerID, pktPacket);

                if (pktPacket == null)
                {
                    return;
                }

                conConnection.QueuePacketToSend(pktPacket);
            }
        }

        [Obsolete]
        public void DestributeReceivedPackets()
        {
            //loop through all the connections 
            foreach(Connection conConnection in ConnectionList.Values)
            {
                while (conConnection.ReceivedPackets.Count > 0)
                {
                    DataPacket pktPacket = conConnection.ReceivedPackets.Dequeue();

                    ProcessPacket(conConnection.m_bConnectionID, pktPacket);
                }
            }
        }

        [Obsolete]
        public Connection MakeConnectionOffer()
        {
            Connection conOffer = new Connection(m_bPlayerID, m_cifPacketFactory);

            return conOffer;
        }

        [Obsolete]
        public Connection MakeReply(Connection conConnectionOffer)
        {
            RegisterConnection(conConnectionOffer);

            Connection conOffer = new Connection(m_bPlayerID, m_cifPacketFactory);

            return conOffer;
        }

        [Obsolete]
        public void RecieveConnectionReply(Connection conConnectionReply)
        {
            RegisterConnection(conConnectionReply);
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
