using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Networking
{
    public class FakeWebRTCFactory : IPeerTransmitterFactory
    {
        public IPeerTransmitter CreatePeerTransmitter()
        {
            return new FakeWebRTCTransmitter();
        }
    }
}
