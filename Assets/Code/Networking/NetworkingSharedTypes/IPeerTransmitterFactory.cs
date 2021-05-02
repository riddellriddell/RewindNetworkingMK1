using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Networking
{
    /// <summary>
    /// this interface defines a factorty that can create peer connection classes 
    /// </summary>
    public interface IPeerTransmitterFactory
    {
        IPeerTransmitter CreatePeerTransmitter();
    }
}
