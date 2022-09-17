using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.Html5WebRTC
{
    [Serializable]
    public struct RTCIceCandidate
    {
        [SerializeField]
        public string candidate;
        [SerializeField]
        public string sdpMid;
        [SerializeField]
        public int sdpMLineIndex;
    }

    [Serializable]
    public struct RTCConnectionConfig
    {
        [SerializeField]
        public RTCIceServer[] iceServers;
    }

    [Serializable]
    public struct RTCIceServer
    {
        [SerializeField]
        public string urls;
        [SerializeField]
        public string username;
        [SerializeField]
        public string credential;
    }

    public class WebRTC
    {
        public static void Initialize()
        {
            //setup webrtc
            NativeFunctions.Initialize();
        }

        public static void Dispose()
        {

            NativeFunctions.Dispose();
        }
    }
}
