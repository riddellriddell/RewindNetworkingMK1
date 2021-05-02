using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Unity.Html5WebRTC
{
    public class WebRTCDataChannel : IDisposable
    {
        //static variables for buffer size 
        //can recieve a maximum of 50 messages at 1000 bytes each before overflowing
        public static int s_iMessageBufferSize = 1000 * 100;
        public static int s_iMessageIndexBufferSize = 200 + 1;

        [SerializeField]
        protected class ChannelEvents
        {
            [SerializeField]
            public bool bOnOpen = false;

            [SerializeField]
            public bool bOnClose = false;

            [SerializeField]
            public string strSerializedCorrectly = "False";
        }

        protected int m_iDataChannelPtr;

        public Action OnOpen;

        public Action OnClose;

        public Action<byte[]> OnMessage;

        protected bool m_bIsAlive = true;

        protected byte[] m_bDataBuffer;

        protected int[] m_iMessageIndexBuffer;

        protected GCHandle m_gchDataBufferGCHandle;

        protected GCHandle m_gchMessageIndexBufferGCHandle;
    
        public WebRTCDataChannel(int iDataChannelPtr)
        {
            m_iDataChannelPtr = iDataChannelPtr;

            //allocat buffers
            m_bDataBuffer = new byte[s_iMessageBufferSize];
            m_iMessageIndexBuffer = new int[s_iMessageIndexBufferSize];

            //pin buffers 
            m_gchDataBufferGCHandle = GCHandle.Alloc(m_bDataBuffer, GCHandleType.Pinned);
            m_gchMessageIndexBufferGCHandle = GCHandle.Alloc(m_iMessageIndexBuffer, GCHandleType.Pinned);

            m_iMessageIndexBuffer[0] = 1;
            m_bDataBuffer[0] = 1;

            //pass buffers to data channel
            NativeFunctions.DataChannelSetupMessageBuffer(
                m_iDataChannelPtr,
                m_bDataBuffer,
                m_bDataBuffer.Length,
                m_iMessageIndexBuffer,
                m_iMessageIndexBuffer.Length
                );

            m_iMessageIndexBuffer[0] = 0;
            m_bDataBuffer[0] = 0;
        }

        public void Update()
        {
            if(m_bIsAlive == false)
            {
                return;
            }

            //check for events on data channel
            string strEventsJson = NativeFunctions.GetDataChannelEvents(m_iDataChannelPtr);
            
            ChannelEvents cheEvents = JsonUtility.FromJson<ChannelEvents>(strEventsJson);

            if (cheEvents.strSerializedCorrectly != "True")
            {
                Debug.Log("Data Channel Events Failed To Serialize Correctly, Serialize String:" + strEventsJson);
            }

            if (cheEvents.bOnOpen)
            {
                Debug.Log("Data Channel Opened C# Side");
                OnOpen?.Invoke();
            }

            if(cheEvents.bOnClose)
            {
                Debug.Log("Data Channel Opened C# Side");
                OnClose?.Invoke();
            }

            if(m_iMessageIndexBuffer[0] > 0)
            {
                Debug.Log("Handling messages C# side");
            }

            //handled messages
            for (int i = 1; i <= m_iMessageIndexBuffer[0]; i++)
            {
                int iStart = 0;
                if(i > 1)
                {
                    iStart = m_iMessageIndexBuffer[i - 1];
                }
                int iCount = m_iMessageIndexBuffer[i] - iStart;

                byte[] bMessageData = new byte[iCount];

                Array.Copy(m_bDataBuffer, iStart, bMessageData, 0, iCount);

                OnMessage?.Invoke(bMessageData);
            }

            m_iMessageIndexBuffer[0] = 0;
        }

        public void Close()
        {
            NativeFunctions.CloseDataChannel(m_iDataChannelPtr);
        }

        public void Dispose()
        {           
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if(disposing)
            {
                NativeFunctions.CloseDataChannel(m_iDataChannelPtr);

                //clean up external resources
                m_gchDataBufferGCHandle.Free();
                m_gchMessageIndexBufferGCHandle.Free();
                m_bDataBuffer = null;
                m_iMessageIndexBuffer = null;

                //make sure to not cause errors if updated after death
                m_bIsAlive = false;

                NativeFunctions.MapDataDelete(m_iDataChannelPtr);
            }
        }

        public void Send(byte[] bData)
        {
            //pin data 
            GCHandle gchSendDataHandle = GCHandle.Alloc(bData, GCHandleType.Pinned);

            NativeFunctions.SendByteArray(m_iDataChannelPtr, bData,bData.Length);

            gchSendDataHandle.Free();
        }
    }
}
