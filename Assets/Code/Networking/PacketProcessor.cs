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
        public virtual int Priority { get; }

        //constructor
        public virtual ConnectionPacketProcessor();

        public virtual Packet ProcessReceivedPacket(Packet pktInputPacket)
        {
            return pktInputPacket;
        }

        public virtual Packet ProcessPaketForSending(Packet pktOutputPacket)
        {
            return pktOutputPacket;
        }

    }
}