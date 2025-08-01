using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using static Unity.Html5WebRTC.RTCSessionDescriptionAsyncOperation;

namespace Unity.Html5WebRTC
{
    public class WebRTCConnection 
    {
        public delegate void DelegateOnIceCandidate(RTCIceCandidate candidate);

        public delegate void DelegateOnDataChannel(WebRTCDataChannel dataChannel);

        public DelegateOnIceCandidate OnIceCandidate { get; set; }
        
        public DelegateOnDataChannel OnDataChannel { get; set; }
        
        public bool m_bLocalDescriptionSet = false;
        
        public bool m_bRemoteDescriptionSet = false;

        [SerializeField]
        protected class ConnecitonEvents
        {
            [SerializeField]
            public bool bOnIceCandidate = false;

            [SerializeField]
            public int iDataChannelPtr = -1;
        }

        [SerializeField]
        protected class IceCandidates
        {
            [SerializeField]
            public string[] strCandidates;
        }

        //pointer to the connection in javascript memory
        protected int m_iConnectionPtr;
               
        public WebRTCConnection(RTCConnectionConfig ccfConnectionConfig)
        {
            Debug.Log("Calling native function to setup new connection");
            m_iConnectionPtr = NativeFunctions.NewConnection(JsonUtility.ToJson(ccfConnectionConfig));
        }

        public void Update()
        {
            IntPtr ptrConnectionEventBuffer = NativeFunctions.GetConnectionEvents(m_iConnectionPtr);
            string strConnectionEventsJson = Marshal.PtrToStringUTF8(ptrConnectionEventBuffer);
            //free data buffer 
            NativeFunctions.FreePtr(ptrConnectionEventBuffer);

            //get updated events
            ConnecitonEvents cevEvents = JsonUtility.FromJson<ConnecitonEvents>(strConnectionEventsJson);

            //check for ice candidate event but only if local state has been set
            if (m_bLocalDescriptionSet && cevEvents.bOnIceCandidate)
            {
                IntPtr ptrIceCandidateBuffer = NativeFunctions.GetConnectionIceCandidateEvents(m_iConnectionPtr);
                string strIceCandidatesJson = Marshal.PtrToStringUTF8(ptrIceCandidateBuffer);
                //free data buffer 
                NativeFunctions.FreePtr(ptrIceCandidateBuffer);

                //get ice candidates and fire event
                IceCandidates icdIceCandidates = JsonUtility.FromJson<IceCandidates>(strIceCandidatesJson);

                for (int i = 0; i < icdIceCandidates.strCandidates.Length; i++)
                {
                    RTCIceCandidate iceCandidate = JsonUtility.FromJson<RTCIceCandidate>(icdIceCandidates.strCandidates[i]);

                    //log ice candidate
                    Debug.Log("Connection On ice candidate");

                    OnIceCandidate?.Invoke(iceCandidate);
                }
            }

            if(cevEvents.iDataChannelPtr != -1)
            {
                WebRTCDataChannel dchDataChannel = new WebRTCDataChannel(cevEvents.iDataChannelPtr);

                OnDataChannel?.Invoke(dchDataChannel);
            }
        }
        
        public RTCSessionDescriptionAsyncOperation CreateOffer()
        {
           int iAsyncPtr = NativeFunctions.CreateOffer(m_iConnectionPtr);

            return new RTCSessionDescriptionAsyncOperation(iAsyncPtr);
        }

        public RTCSessionDescriptionAsyncOperation CreateAnswer()
        {
            int iAsyncPtr = NativeFunctions.CreateAnswer(m_iConnectionPtr);

            return new RTCSessionDescriptionAsyncOperation(iAsyncPtr);
        }

        public WebRTCDataChannel CreateDataChannel(string strLabel, bool bIsReliable)
        {
            int iDataChannelPtr = NativeFunctions.CreateDataChannel(m_iConnectionPtr, strLabel, bIsReliable);

            return new WebRTCDataChannel(iDataChannelPtr);
        }
        
        public RTCSetSessionDescriptionAsyncOperation SetLocalDescription(string strSessionDescriptionJson)
        {
            int iAsyncPtr = NativeFunctions.SetLocalDescription(m_iConnectionPtr, strSessionDescriptionJson);

            return new RTCSetSessionDescriptionAsyncOperation(iAsyncPtr);
        }

        public RTCSetSessionDescriptionAsyncOperation SetRemoteDescription(string strSessionDescriptionJson)
        {
            int iAsyncPtr = NativeFunctions.SetRemoteDescription(m_iConnectionPtr, strSessionDescriptionJson);

            return new RTCSetSessionDescriptionAsyncOperation(iAsyncPtr);
        }

        public void AddIceCandidate(RTCIceCandidate icdIceCandidate)
        {
            string strToJson = JsonUtility.ToJson(icdIceCandidate);
            
            Debug.Log( $"Adding wrapped ice candidate: {strToJson}");
            
            NativeFunctions.AddIceCandidate(m_iConnectionPtr, strToJson);
        }

        public void Close()
        {
            NativeFunctions.CloseConnection(m_iConnectionPtr);
        }

        public void Dispose()
        {
            NativeFunctions.DisposeConnection(m_iConnectionPtr);
        }
    }
}
