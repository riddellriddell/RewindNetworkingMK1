using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// this virtual class handles processing a packet for sending and receiving 
/// </summary>
namespace Networking
{
    public abstract class BaseConnectionPacketProcessor 
    {
        //defines the order that packet processors process a packet if it is processed by multiple packet processors 
        public abstract int Priority { get; }
        
        protected Connection m_conConnection = null;

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

        //called when a connection disconnects
        public virtual void OnConnectionDisconnect()
        {

        }

        public void SetConnection(Connection conConnection)
        {
            m_conConnection = conConnection;
        }


    }
}