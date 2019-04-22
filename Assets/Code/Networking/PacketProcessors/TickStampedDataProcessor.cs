using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// this class adds a tick stamped data processor to all connections
/// </summary>
namespace Networking
{
    public class TickStampedDataNetworkProcessor : NetworkPacketProcessor
    {
        public override int Priority
        {
            get
            {
                return 1;
            }
        }

        protected int m_iTick;

        protected NetworkConnection m_ncnNetwork;

        protected List<TickStampedDataConnectionProcessor> m_tcpTickStampProcessors;

        public void UpdateTick(int iTick)
        {
            m_iTick = iTick;

            for (int i = 0; i < m_tcpTickStampProcessors.Count; i++)
            {
                m_tcpTickStampProcessors[i].SetTick(m_iTick);
            }
        }

        public override void OnAddToNetwork(NetworkConnection ncnNetwork)
        {
            //store network
            m_ncnNetwork = ncnNetwork;

            m_tcpTickStampProcessors = new List<TickStampedDataConnectionProcessor>();

            //add tick processors to all connections 
            for (int i = 0; i < m_ncnNetwork.m_conConnectionList.Count; i++)
            {
                if (m_ncnNetwork.m_conConnectionList[i] != null)
                {
                    AddProcessorToConnection(m_ncnNetwork.m_conConnectionList[i]);
                }
            }

        }

        public override void OnNewConnection(Connection conConnection)
        {
            AddProcessorToConnection(conConnection);
        }

        protected void AddProcessorToConnection(Connection conConnection)
        {
            TickStampedDataConnectionProcessor tcpTickStampedProcessor = new TickStampedDataConnectionProcessor(m_iTick);

            m_tcpTickStampProcessors.Add(tcpTickStampedProcessor);

            conConnection.AddPacketProcessor(tcpTickStampedProcessor);
        }
    }

    /// <summary>
    /// this keeps track of the current tick of the connection 
    /// </summary>
    public class TickStampedDataConnectionProcessor : ConnectionPacketProcessor
    {
        protected int m_iTick;

        protected int m_iLastPacketTickQueuedToSend;

        protected int m_iLastPacketTickReceived;

        public TickStampedDataConnectionProcessor(int iStartTick)
        {
            m_iTick = iStartTick;
        }

        public void SetTick(int iTick)
        {
            m_iTick = iTick;
        }

        public override void Update(Connection conConnection)
        {
            //check if a ping packet is needed
            if (m_iTick - m_iLastPacketTickQueuedToSend >= TickStampedPacket.MaxTicksBetweenTickStampedPackets)
            {
                //add a ping packet to send list to maintain connection
                conConnection.QueuePacketToSend(new PingPacket());
            }
        }

        public override DataPacket ProcessPacketForSending(Connection conConnection, DataPacket pktOutputPacket)
        {
            //reset the tick for game start events 
            if (pktOutputPacket is ResetTickCountPacket)
            {
                //reset connection tick
                m_iLastPacketTickQueuedToSend = 0;
            }

            if (pktOutputPacket is PingPacket)
            {
                m_iLastPacketTickQueuedToSend += TickStampedPacket.MaxTicksBetweenTickStampedPackets;
            }

            //set the offset for tick stamped packets
            if (pktOutputPacket is TickStampedPacket)
            {
                TickStampedPacket tspPacket = pktOutputPacket as TickStampedPacket;

                //check if the target tick between this packet and the next is too big
                while (tspPacket.m_iTick - m_iLastPacketTickQueuedToSend > TickStampedPacket.MaxTicksBetweenTickStampedPackets)
                {
                    //queue a ping packet 
                    conConnection.QueuePacketToSend(new PingPacket());
                }

                //set the packets offset from the previouse packet 
                tspPacket.SetOffset(m_iLastPacketTickQueuedToSend);

                //update the current tick
                m_iLastPacketTickQueuedToSend = tspPacket.m_iTick;

                return tspPacket;
            }

            return pktOutputPacket;
        }

        public override DataPacket ProcessReceivedPacket(Connection conConnection, DataPacket pktInputPacket)
        {
            //check if packet is ping packet 
            if (pktInputPacket is PingPacket)
            {
                m_iLastPacketTickReceived += TickStampedPacket.MaxTicksBetweenTickStampedPackets;

                //consume ping packet as it is no longer needed 
                return null;
            }

            //check if it is a game tick reset packet 
            if (pktInputPacket is ResetTickCountPacket)
            {
                m_iLastPacketTickReceived = 0;

                return null;
            }

            //calculate the packet tick based off its offset 
            if (pktInputPacket is TickStampedPacket)
            {
                TickStampedPacket tspPacket = pktInputPacket as TickStampedPacket;

                //update the tick of the connection target 
                m_iLastPacketTickReceived += tspPacket.Offset;

                //set the tick of the packet 
                tspPacket.m_iTick = m_iLastPacketTickReceived;

                return tspPacket;
            }

            return pktInputPacket;
        }
    }
}