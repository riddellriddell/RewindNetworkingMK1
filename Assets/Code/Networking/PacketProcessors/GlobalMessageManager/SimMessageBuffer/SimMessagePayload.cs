using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Networking
{
    public interface ISimMessagePayload 
    {
        //the peer this message came from
        long lPeer { get; }
    }

    public struct PeerConnectedMessage : ISimMessagePayload
    {
        public long lPeer { get; set; }
    }

    public struct PeerDisconnectMessage : ISimMessagePayload
    {
        public long lPeer { get; set; }
    }

}