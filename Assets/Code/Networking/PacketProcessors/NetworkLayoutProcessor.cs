using System;
using System.Collections.Generic;
using UnityEngine;
using Utility;

namespace Networking
{
    public struct NetworkLayout
    {
        public struct ConnectionState
        {
            public long m_lConnectionID;
            public DateTime m_dtmTimeOfConnection;

            public ConnectionState(long lID, DateTime dtmTimeOfConnection)
            {
                m_lConnectionID = lID;
                m_dtmTimeOfConnection = dtmTimeOfConnection;
            }

        }

        public List<ConnectionState> m_conConnectionDetails;

        public NetworkLayout(int iConnectionCount)
        {
            m_conConnectionDetails = new List<ConnectionState>(iConnectionCount);
        }

        public void Add(long lConnectionID, DateTime dtmTimeOfConnection)
        {
            if (m_conConnectionDetails == null)
            {
                m_conConnectionDetails = new List<ConnectionState>();
            }

            //check if it has already been added 
            for (int i = 0; i < m_conConnectionDetails.Count; i++)
            {
                if (m_conConnectionDetails[i].m_lConnectionID == lConnectionID)
                {
                    m_conConnectionDetails[i] = new ConnectionState(lConnectionID, dtmTimeOfConnection);
                    return;
                }
            }

            m_conConnectionDetails.Add(new ConnectionState(lConnectionID, dtmTimeOfConnection));
        }

        //returns list of connection id's not in the passed in conTargetConnections list
        public List<long> ConnectionsNotInDictionary(Dictionary<long, Connection> conExistingConnections)
        {
            //check that connections list is setup
            if (m_conConnectionDetails == null)
            {
                return new List<long>(conExistingConnections.Keys);
            }

            List<long> conOutput = new List<long>();

            for (int i = 0; i < m_conConnectionDetails.Count; i++)
            {
                long lConnectionID = m_conConnectionDetails[i].m_lConnectionID;

                //check if not connected to peer or the connection has been disconnected 
                if (conExistingConnections.TryGetValue(lConnectionID, out Connection conConnection) == false ||
                    conConnection.Status == Connection.ConnectionStatus.New ||
                    conConnection.Status == Connection.ConnectionStatus.Disconnected ||
                    conConnection.Status == Connection.ConnectionStatus.Disconnecting)
                {
                    conOutput.Add(m_conConnectionDetails[i].m_lConnectionID);
                }
            }

            return conOutput;
        }

        public bool HasTarget(long lConnectionID)
        {
            if (m_conConnectionDetails == null)
            {
                return false;
            }

            for (int i = 0; i < m_conConnectionDetails.Count; i++)
            {
                if (m_conConnectionDetails[i].m_lConnectionID == lConnectionID)
                {
                    return true;
                }
            }

            return false;
        }
    }

    //serializer for network data layout
    public partial class NetworkingByteStream
    {
        public static void Serialize(ReadByteStream rbsByteStream, ref NetworkLayout Input)
        {
            Int32 iSize = 0;

            ByteStream.Serialize(rbsByteStream, ref iSize);

            Input.m_conConnectionDetails = new List<NetworkLayout.ConnectionState>(iSize);

            for (int i = 0; i < iSize; i++)
            {
                NetworkLayout.ConnectionState cstState = new NetworkLayout.ConnectionState();
                Serialize(rbsByteStream, ref cstState);

                Input.m_conConnectionDetails.Add(cstState);
            }
        }

        public static void Serialize(WriteByteStream wbsByteStream, ref NetworkLayout Input)
        {
            Int32 iSize = Input.m_conConnectionDetails.Count;

            ByteStream.Serialize(wbsByteStream, ref iSize);

            for (int i = 0; i < iSize; i++)
            {
                NetworkLayout.ConnectionState cstState = Input.m_conConnectionDetails[i];
                Serialize(wbsByteStream, ref cstState);
            }
        }

        public static void Serialize(ReadByteStream rbsByteStream, ref NetworkLayout.ConnectionState Input)
        {
            ByteStream.Serialize(rbsByteStream, ref Input.m_lConnectionID);
            ByteStream.Serialize(rbsByteStream, ref Input.m_dtmTimeOfConnection);
        }

        public static void Serialize(WriteByteStream wbsByteStream, ref NetworkLayout.ConnectionState Input)
        {
            ByteStream.Serialize(wbsByteStream, ref Input.m_lConnectionID);
            ByteStream.Serialize(wbsByteStream, ref Input.m_dtmTimeOfConnection);
        }

        public static int DataSize(ref NetworkLayout Input)
        {
            int iSize = ByteStream.DataSize(Input.m_conConnectionDetails.Count);

            for (int i = 0; i < Input.m_conConnectionDetails.Count; i++)
            {
                iSize += DataSize(Input.m_conConnectionDetails[i]);
            }

            return iSize;
        }

        public static int DataSize(NetworkLayout.ConnectionState Input)
        {
            int iSize = ByteStream.DataSize(Input.m_lConnectionID);

            iSize += ByteStream.DataSize(Input.m_dtmTimeOfConnection);

            return iSize;
        }
    }

    //this packet processor tracks the layout of the network
    public class NetworkLayoutProcessor : ManagedNetworkPacketProcessor<ConnectionNetworkLayoutProcessor>
    {
        public delegate void ConnectionLayoutChange(ConnectionNetworkLayoutProcessor clpLayoutProcessor);
        public event ConnectionLayoutChange m_evtPeerConnectionLayoutChange;

        public bool m_bHasRecievedGatewayDataFromPeers = false;

        public bool m_bShouldUpdatePeers = false;

        public HashSet<long> MissingConnections { get; } = new HashSet<long>();

        public override int Priority
        {
            get
            {
                return 2;
            }
        }

        public override void Update()
        {
            if (m_bShouldUpdatePeers)
            {
                Debug.Log("sending layout to connected peers");

                SendNetworkLayoutToPeers();
                m_bShouldUpdatePeers = false;
            }
        }

        //when a connected peer changes their network layout
        public void OnPeerNetworkLayoutChange(ConnectionNetworkLayoutProcessor clpConnectionNetworkLayoutProcessor)
        {
            //indicate that network layout data has been recieved from peers 
            m_bHasRecievedGatewayDataFromPeers = true;

            //check if peer has new connections that are missing in local 
            CheckForMissingConnections();

            m_evtPeerConnectionLayoutChange.Invoke(clpConnectionNetworkLayoutProcessor);
        }

        //then the local peer gains or looses connection to a peer
        public void OnPeerDisconnect()
        {
            //tell peers next update the change in network layout
            m_bShouldUpdatePeers = true;

            //update the missing connections list
            CheckForMissingConnections();
        }

        //called when a peer losses connection
        public void OnPeerConnection(long lPeerID)
        {
            //tell peers next update the change in network layout
            m_bShouldUpdatePeers = true;

            //update the list of missing connection
            RemoveMissingConnectionID(lPeerID);
        }

        //find connected peers with link to target
        public List<long> PeersWithConnection(long lConnection)
        {
            List<long> lOutput = new List<long>();

            foreach (KeyValuePair<long, ConnectionNetworkLayoutProcessor> kvpPair in ChildConnectionProcessors)
            {
                if (kvpPair.Value.ParentConnection.Status == Connection.ConnectionStatus.Connected &&
                    kvpPair.Value.m_nlaNetworkLayout.HasTarget(lConnection))
                {
                    lOutput.Add(kvpPair.Key);
                }
            }

            return lOutput;
        }

        //check that all peers have sent their connection layout data
        public bool HasRecievedNetworkLayoutDataFromAllConnectedPeers()
        {
            foreach (ConnectionNetworkLayoutProcessor clpLayout in ChildConnectionProcessors.Values)
            {
                if (clpLayout.ParentConnection.Status == Connection.ConnectionStatus.Connected &&
                    clpLayout.m_bHasRecievedNetworkLayoutData == false)
                {
                    return false;
                }
            }

            return true;
        }

        protected void SendNetworkLayoutToPeers()
        {
            //generate network layout packet and send it to all connections
            NetworkLayoutPacket nlpNetworkLayoutPacket = ParentNetworkConnection.PacketFactory.CreateType<NetworkLayoutPacket>(NetworkLayoutPacket.TypeID);
            nlpNetworkLayoutPacket.m_nlaNetworkLayout = GenterateNetworkLayout();

            //send packet out to all connections updating them on what users this computer is conencted to 
            ParentNetworkConnection.TransmitPacketToAll(nlpNetworkLayoutPacket);
        }

        protected NetworkLayout GenterateNetworkLayout()
        {
            NetworkLayout networkLayout = new NetworkLayout(ChildConnectionProcessors.Count);

            foreach (ConnectionNetworkLayoutProcessor clpLayout in ChildConnectionProcessors.Values)
            {
                //check if fully connected
                if (clpLayout.ParentConnection.Status == Connection.ConnectionStatus.Connected)
                {
                    Debug.Log($"adding connection: {clpLayout.ParentConnection.m_lUserUniqueID} to network layout");
                    DateTime dtmTimeOfConnection = clpLayout.NetworkTimeOfConnection;

                    networkLayout.Add(clpLayout.ParentConnection.m_lUserUniqueID, dtmTimeOfConnection);
                }
            }

            return networkLayout;
        }

        protected void CheckForMissingConnections()
        {
            MissingConnections.Clear();

            foreach (ConnectionNetworkLayoutProcessor clpConnectionLayout in ChildConnectionProcessors.Values)
            {
                if (clpConnectionLayout.ParentConnection.Status == Connection.ConnectionStatus.Connected)
                {
                    //check if peer has missing connections 
                    CheckPeerForMissingConnection(clpConnectionLayout);
                }
            }
        }

        protected void CheckPeerForMissingConnection(ConnectionNetworkLayoutProcessor clpConnectionLayout)
        {
            List<long> lMissingConnections = clpConnectionLayout.m_nlaNetworkLayout.ConnectionsNotInDictionary(ParentNetworkConnection.ConnectionList);

            for (int i = 0; i < lMissingConnections.Count; i++)
            {
                //check if missing connection is to local peer 
                if (lMissingConnections[i] == ParentNetworkConnection.m_lPeerID)
                {
                    continue;
                }

                TryAddMissingConnectionID(lMissingConnections[i]);
            }
        }

        protected void TryAddMissingConnectionID(long lConnectionID)
        {
            if (MissingConnections.Contains(lConnectionID))
            {
                return;
            }

            MissingConnections.Add(lConnectionID);
        }

        protected void RemoveMissingConnectionID(long lConnectionID)
        {
            MissingConnections.Remove(lConnectionID);
        }

        protected override void AddDependentPacketsToPacketFactory(ClassWithIDFactory cifPacketFactory)
        {
            cifPacketFactory.AddType<NetworkLayoutPacket>(NetworkLayoutPacket.TypeID);
        }
    }

    public class ConnectionNetworkLayoutProcessor : ManagedConnectionPacketProcessor<NetworkLayoutProcessor>
    {
        public override int Priority
        {
            get
            {
                return 2;
            }
        }

        public DateTime NetworkTimeOfConnection
        {
            get
            {
                return TimeNetworkProcessor.ConvertFromBaseToNetworkTime(
                            m_dtmBaseTimeOfConnection,
                            m_tnpNetworkTime.TimeOffset);
            }
        }

        //the time this connection was made 
        public DateTime m_dtmBaseTimeOfConnection;

        //the connection layout at the other end of this connection
        public NetworkLayout m_nlaNetworkLayout;

        //has this peer sent its network layout data 
        public bool m_bHasRecievedNetworkLayoutData = false;

        protected TimeNetworkProcessor m_tnpNetworkTime;

        public ConnectionNetworkLayoutProcessor()
        {
            m_nlaNetworkLayout = new NetworkLayout(0);
        }

        public override void Start()
        {
            base.Start();

            m_tnpNetworkTime = m_tParentPacketProcessor.ParentNetworkConnection.GetPacketProcessor<TimeNetworkProcessor>();

            //store the network time that this 
            m_dtmBaseTimeOfConnection = m_tnpNetworkTime.BaseTime;

        }

        //update the targets Packet layout
        public override DataPacket ProcessReceivedPacket(Connection conConnection, DataPacket pktInputPacket)
        {
            if (pktInputPacket is NetworkLayoutPacket)
            {
                Debug.Log("Recieved network layout packet");

                m_nlaNetworkLayout = (pktInputPacket as NetworkLayoutPacket).m_nlaNetworkLayout;

                m_bHasRecievedNetworkLayoutData = true;

                m_tParentPacketProcessor.OnPeerNetworkLayoutChange(this);

                return null;
            }

            return pktInputPacket;
        }

        public override void OnConnectionReset()
        {
            //reset time of connection 
            m_dtmBaseTimeOfConnection = m_tnpNetworkTime.BaseTime;

            m_bHasRecievedNetworkLayoutData = false;

            m_nlaNetworkLayout = new NetworkLayout(0);

            base.OnConnectionReset();
        }

        public override void OnConnectionStateChange(Connection.ConnectionStatus cstOldState, Connection.ConnectionStatus cstNewState)
        {
            if (cstNewState == Connection.ConnectionStatus.Connected)
            {
                m_tParentPacketProcessor.OnPeerConnection(ParentConnection.m_lUserUniqueID);
            }

            if (cstNewState == Connection.ConnectionStatus.Disconnected || cstNewState == Connection.ConnectionStatus.Disconnecting)
            {
                m_tParentPacketProcessor.OnPeerDisconnect();
            }
        }
    }
}
