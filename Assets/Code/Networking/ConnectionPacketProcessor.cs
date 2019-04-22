using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// this virtual class handles processing a packet for sending and receiving 
/// </summary>
namespace Networking
{
    public abstract class ConnectionPacketProcessor 
    {
        //defines the order that packet processors process a packet if it is processed by multiple packet processors 
        public abstract int Priority { get; }

        public virtual void Update(Connection conConnection)
        {

        }

        public virtual DataPacket ProcessReceivedPacket(Connection conConnection,DataPacket pktInputPacket)
        {
            return pktInputPacket;
        }

        public virtual DataPacket ProcessPacketForSending(Connection conConnection,DataPacket pktOutputPacket)
        {
            return pktOutputPacket;
        }

        public void SetConnection(Connection conConnection)
        {
            m_conConnection = conConnection;
        }

        protected Connection m_conConnection = null;

    }
}