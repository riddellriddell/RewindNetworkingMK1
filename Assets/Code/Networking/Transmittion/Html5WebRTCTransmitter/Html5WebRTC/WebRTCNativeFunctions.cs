using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Unity.Html5WebRTC
{
    public static class NativeFunctions
    {
        //TODO: remove if works
        [DllImport("__Internal")]
        public static extern void SetupDataChannel(int iDummyVar);

        [DllImport("__Internal")]
        public static extern void Initialize();
        
        [DllImport("__Internal")]
        public static extern int NewConnection(string strIceUrl);

        [DllImport("__Internal")]
        public static extern string GetConnectionEvents(int iConnectionPtr);

        [DllImport("__Internal")]
        public static extern string GetConnectionIceCandidateEvents(int iConnectionPtr);

        [DllImport("__Internal")]
        public static extern void CloseConnection(int iConnectionPtr);

        [DllImport("__Internal")]
        public static extern void DisposeConnection(int iConnectionPtr);
        
        [DllImport("__Internal")]
        public static extern void Dispose();
 
        [DllImport("__Internal")]
        public static extern int CreateOffer(int iChannelPtr);

        [DllImport("__Internal")]
        public static extern int CreateAnswer(int iChannelPtr);
        
        [DllImport("__Internal")]
        public static extern bool IsAsyncActionComplete(int iAsyncPtr);
        
        [DllImport("__Internal")]
        public static extern string GetAsyncResult(int iAsyncPtr);

        [DllImport("__Internal")]
        public static extern void MapDataDelete(int iMapDataPtr);

        [DllImport("__Internal")]
        public static extern int SetLocalDescription(int iConnectionPtr, string strSessionDescriptionJson);

        [DllImport("__Internal")]
        public static extern int SetRemoteDescription(int iConnectionPtr, string strSessionDescriptionJson);

        [DllImport("__Internal")]
        public static extern void AddIceCandidate(int iConnectionPtr, string strIceCandidateJson);

        //------------------------------------ Data Channel ----------------------------------------------

        [DllImport("__Internal")]
        public static extern int CreateDataChannel(int iChannelPtr, string strLabel, bool bIsReliable);


        [DllImport("__Internal")]
        public static extern void DataChannelSetupMessageBuffer(
            int iDataChannelPtr,
            byte[] bMessageBuffer,
            int iMessageBufferLength,
            int[] iIndexBuffer,
            int iIndexBufferLenght
            );

        [DllImport("__Internal")]
        public static extern string GetDataChannelEvents(int iDataChannelPtr);

        [DllImport("__Internal")]
        public static extern void SendByteArray(int iDataChannelPtr, byte[] bMessage, int iLength);

        [DllImport("__Internal")]
        public static extern void CloseDataChannel(int iDataChannelPtr);

        [DllImport("__Internal")]
        public static extern void DisposeDataChannel(int iDataChannelPtr);
    }
}
