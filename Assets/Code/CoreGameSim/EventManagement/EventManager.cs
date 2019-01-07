using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Sim
{
    /// <summary>
    /// this class handels events created by the simulation
    /// </summary>
    public abstract class EventManager 
    {
        public bool RaisingEventsForFrame { get; private set; }

        protected List<IEventTracker> m_evtEvents;

        protected List<byte> m_bFrameReSimCount;

        protected int m_iHeadTick;

        protected bool m_bFistSimOfTick;

        protected int m_iTargetIndex;

        protected byte m_bResimCount;

        protected int m_iBufferTail;

        public EventManager(int iBufferSize )
        {
            SetupFrameResimCount(iBufferSize);

            SetupEventArray();

            SetupEvents();
        }

        public void PrepForFrameEvents(int iTick)
        {
            //check if tick falls within buffers
            if(iTick <= m_iHeadTick - m_bFrameReSimCount.Count)
            {
                //not tracking events this far in the past 
                RaisingEventsForFrame = false;

                return;
            }

            RaisingEventsForFrame = true;

            //update tick head
            if(m_iHeadTick < iTick)
            {
                m_iHeadTick = iTick;
                m_bFistSimOfTick = true;
            }
            else
            {
                m_bFistSimOfTick = false;
            }

            //get index 
            m_iTargetIndex = ConvertFromTickToIndex(iTick);

            //check if the target frame requires clearing
            if(RequiresClear(m_iTargetIndex))
            {
                ClearEventBufferFrame(m_iTargetIndex);
            }
            else
            {
                IncrementFrameResimCount(m_iTargetIndex);
            }

            //setup frame events for the target tick 
            for (int i = 0; i < m_evtEvents.Count; i++)
            {
                m_evtEvents[i].SetupForFrame();
            }
        }
        
        public void SendEventsForFrame()
        {
            //check that we are raising events for this frame
            if(RaisingEventsForFrame == false)
            {
                return;
            }

            //for each of the frame event managers
            for (int i = 0; i < m_evtEvents.Count; i++)
            {
                m_evtEvents[i].ApplyFrameEvents(m_iTargetIndex,m_bResimCount,m_bFistSimOfTick);
            }

        }

        protected abstract void SetupEvents();
       
        protected void RegisterEvent<Arg>(ref EventTracker<Arg> etrTargetToSet, EventTracker<Arg>.TrackingType trtTrackingType)
        {
            etrTargetToSet = new EventTracker<Arg>(m_bFrameReSimCount.Count, trtTrackingType);

            m_evtEvents.Add(etrTargetToSet);
        }

        protected int ConvertFromTickToIndex(int iTick)
        {
            return HelperFunctions.mod(iTick, m_bFrameReSimCount.Count);
            
        }

        protected void SetupFrameResimCount(int iFrameCount)
        {
            //create array to keep track of the number of times frames are recalculated 
            m_bFrameReSimCount = new List<byte>(iFrameCount);

            for(int i = 0; i < iFrameCount; i++)
            {
                m_bFrameReSimCount.Add(0);
            }

            m_iBufferTail = iFrameCount - 1;
        }

        protected void SetupEventArray()
        {
            m_evtEvents = new List<IEventTracker>();
        }

        //increment the number of times a frame has been simulated 
        protected void IncrementFrameResimCount(int iFrameIndex)
        {
            m_bResimCount = m_bFrameReSimCount[iFrameIndex] = (byte)((++m_bFrameReSimCount[iFrameIndex]) % 256);
        }

        //check if this is an old frame that needs clearing 
        protected bool RequiresClear(int iIndex)
        {
            return m_iBufferTail == iIndex;
        }

        protected void ClearEventBufferFrame(int iIndex)
        {
            //increment buffer tail
            m_iBufferTail = ++m_iBufferTail % m_bFrameReSimCount.Count;

            //reset resim count 
            m_bFrameReSimCount[iIndex] = 0;

            //reset all frame event managers 
            for(int i = 0; i < m_evtEvents.Count; i++)
            {
                m_evtEvents[i].Clear(iIndex);
            }
        }

    }
}
