using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Networking
{
    public interface ISimMessagePayload 
    {

    }

    public struct PeerConnectedMessage : ISimMessagePayload
    {

    }

    public struct PeerDisconnectMessage : ISimMessagePayload
    {

    }

}