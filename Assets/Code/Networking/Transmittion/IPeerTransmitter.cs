﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Networking
{
    public enum PeerTransmitterState
    {
        New,
        Negotiating,
        Connected,
        Disconnected,
        Error
    }

    //this represents a connection to another user 
    public interface IPeerTransmitter
    {
        //status of connection
        PeerTransmitterState State { get; }

        //start process of negotiating network connection 
        //only one client should do this the other client should wait for 
        //negotiation message to start connection process
        void StartNegotiation();

        //try to shut down the connection
        void Disconnect();

        //called when message for connection negotiation is created
        Action<string> OnNegotiationMessageCreated { get; set; }

        //process negotiation message from external sender 
        bool ProcessNegotiationMessage(string strMessage);

        //called when connection has been established
        Action OnConnectionEstablished { get; set; }

        //called when data is received
        Action<byte[]> OnDataReceive { get; set; }

        //send data through internet 
        bool SentData(byte[] data);
    }
}