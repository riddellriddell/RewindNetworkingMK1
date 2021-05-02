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
            //for testing reasons only use a fake transmitter 
            //return new FakeWebRTCTransmitter();

#if UNITY_EDITOR_WIN
            return new WebRTCTransmitter(m_monCoroutineExecuter);
#elif UNITY_WEBGL
            return new Html5WebRTCTransmitter(m_monCoroutineExecuter);
#else
            return new WebRTCTransmitter(m_monCoroutineExecuter);
#endif
        }
    }
}
