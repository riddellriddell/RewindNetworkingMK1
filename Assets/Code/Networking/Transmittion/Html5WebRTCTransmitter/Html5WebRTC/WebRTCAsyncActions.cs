using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.Html5WebRTC
{
    public abstract class AsyncOperationBase : CustomYieldInstruction
    {
        public string Error { get; protected set; }
        public bool IsError { get; protected set; } = false;
        public bool IsDone { get; protected set; } = false;
        public override bool keepWaiting
        {
            get
            {
                if(NativeFunctions.IsAsyncActionComplete(m_iAsyncPtr) && IsDone == false)
                {
                    IsDone = true;
                
                    GetResultOnCompletion();
                
                    return false;
                }
                else
                {
                  return true;
                }
            }
        }

        protected bool m_bKeepWaiting = true;
        protected int m_iAsyncPtr;

        public AsyncOperationBase(int iAsyncPtr)
        {
            m_iAsyncPtr = iAsyncPtr;
        }

        public abstract void GetResultOnCompletion();


    }
       
    public class RTCSessionDescriptionAsyncOperation : AsyncOperationBase
    {
        [SerializeField]
        public struct RTCSessionDescription
        {
            [SerializeField]
            public bool bIsFinished;
            [SerializeField]
            public bool bIsError;
            [SerializeField]
            public string strDescription;
        }

        public string m_strSessionDescription;

        public RTCSessionDescriptionAsyncOperation(int iAsyncPtr) : base(iAsyncPtr)
        {
        }

        public override void GetResultOnCompletion()
        {
            //get session description details
            string strJson = NativeFunctions.GetAsyncResult(m_iAsyncPtr);

            RTCSessionDescription sdcSessionDescription = JsonUtility.FromJson<RTCSessionDescription>(strJson);

            m_strSessionDescription = sdcSessionDescription.strDescription;

            if (sdcSessionDescription.bIsError)
            {
                IsError = true;
            }
        }
    }

    public class RTCSetSessionDescriptionAsyncOperation : AsyncOperationBase
    {
        [SerializeField]
        public class SetLocalDescriptionResult
        {
            [SerializeField]
            public bool bIsError;
        }

        public RTCSetSessionDescriptionAsyncOperation(int iAsyncPtr) : base(iAsyncPtr)
        {
        }

        public override void GetResultOnCompletion()
        {
            //get session description details
            string strJson = NativeFunctions.GetAsyncResult(m_iAsyncPtr);

            SetLocalDescriptionResult sdrSetDescriptionResult = JsonUtility.FromJson<SetLocalDescriptionResult>(strJson);

            if (sdrSetDescriptionResult.bIsError)
            {
                IsError = true;
            }
        }
    }
}