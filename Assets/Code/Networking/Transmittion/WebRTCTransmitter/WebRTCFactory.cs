using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Networking
{
    public class WebRTCFactory : IPeerTransmitterFactory
    {
        public MonoBehaviour m_monCoroutineExecuter;

        public WebRTCFactory(MonoBehaviour monCoroutineExecuter)
        {
            m_monCoroutineExecuter = monCoroutineExecuter;
        }

        public IPeerTransmitter CreatePeerTransmitter()
        {
            return new WebRTCTransmitter(m_monCoroutineExecuter);
        }
    }
}
