using Networking;
using Sim;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TickStampedDataNetworkProcessor : NetworkPacketProcessor
{
    public int m_iTick;

    protected NetworkConnection m_ncnNetwork;

    protected List<TickStampedDataConnectionProcessor> m_tcpTickStampProcessors;

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

    protected void AddProcessorToConnection(Connection conConnection)
    {
        TickStampedDataConnectionProcessor tcpTickStampedProcessor = new TickStampedDataConnectionProcessor(m_iTick);

        m_tcpTickStampProcessors.Add(tcpTickStampedProcessor);

        conConnection.m_cppOrderedPacketProcessorList.Add(tcpTickStampedProcessor);
    }
}

public class TickStampedDataConnectionProcessor : ConnectionPacketProcessor
{
    protected int m_iTick;

    protected int m_iLastPacketTickQueuedToSend;

    public TickStampedDataConnectionProcessor(int iStartTick)
    {
        m_iTick = iStartTick;
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

    public override Packet ProcessPacketForSending(Connection conConnection, Packet pktOutputPacket)
    {
        //check if packet is associated to tick stamped data 
       
    }

    public override Packet ProcessReceivedPacket(Connection conConnection, Packet pktInputPacket)
    {
        
    }
}