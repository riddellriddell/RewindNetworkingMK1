using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Sim
{
    public class EventSubscriber  
    {
        /// <summary>
        /// this gets called if the simulation has to resimulate a frame and the target event does not get called again 
        /// </summary>
        public delegate void OnCancel();

        public event OnCancel OnEventCancel;

        //the resimulation count that this event was created on
        private byte m_bInvocationCount;

        public EventSubscriber(byte recallCount)
        {
            m_bInvocationCount = recallCount;
        }

        public void UpdateRecallCount(byte bRecallCount)
        {
            m_bInvocationCount = bRecallCount;
        }

        public bool WasCanceled(byte bRecallCount)
        {
            return bRecallCount != m_bInvocationCount;
        }

        public void InvokeCancle()
        {
            OnEventCancel?.Invoke();
        }

        //is there anyone still subscribed to this 
        public bool HasSubs()
        {
            if(OnEventCancel == null || OnEventCancel.GetInvocationList().Length == 0)
            {
                return false;
            }

            return true;
        }
    }
}