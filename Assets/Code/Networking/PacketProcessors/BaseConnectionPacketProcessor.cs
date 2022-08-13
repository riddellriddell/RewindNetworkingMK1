using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// this virtual class handles processing a packet for sending and receiving 
/// this class is attached to a connection and is used to prepare messages for sending and recieving
/// </summary>
namespace Networking
{
    public abstract class BaseConnectionPacketProcessor 
    {
        //defines the order that packet processors process a packet if it is processed by multiple packet processors 
        public abstract int Priority { get; }
        
        public Connection ParentConnection { get; private set; } = null;

        //this gets called after the connection packet processor has been added to the 
        //conneciton and all of its setup values have been set
        public virtual void Start()
        {

        }

        public virtual void Update()
        {

        }

        // when target peer reconnects to local peer requiring
        // all synced data to be flushed from connection to maintain determinism accross the connection
        public virtual void OnConnectionReset()
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

        public virtual void OnConnectionStateChange(Connection.ConnectionStatus cstOldState, Connection.ConnectionStatus cstNewState)
        {

        }

        public void SetConnection(Connection conConnection)
        {
            ParentConnection = conConnection;
        }


    }
}