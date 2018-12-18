using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Sim
{
    public class EventTracker<Args> : IEventTracker
    {
        public enum TrackingType
        {
            None,
            OnFirstSimOnly,
            FireOnce,
            Cancelable
        }

        public TrackingType Mode { get; private set; }

        public delegate void CancelableEvent(Args argArguments, EventSubscriber evsCancelCallback);

        public delegate void NonCancelableEvent(Args argArguments);

        public event NonCancelableEvent OnEvent;

        public event CancelableEvent OnCancelableEvent;

        //list of all the event arguments that have happened this frame 
        private List<Args> m_argEventArguments;

        //collection of all the event cancel listeners 
        private List<Dictionary<int, EventSubscriber>> m_evtEventTracking;

        private List<Args> m_argEventArgumentsToCall;
        private List<KeyValuePair<int, EventSubscriber>> m_escEventsToCancel;

        public EventTracker(int iFrameBufferCount, TrackingType trtTrackingMode)
        {
            m_argEventArguments = new List<Args>();
            m_argEventArgumentsToCall = new List<Args>();
            m_escEventsToCancel = new List<KeyValuePair<int, EventSubscriber>>();

            Mode = trtTrackingMode;

            switch (trtTrackingMode)
            {
                case TrackingType.None:
                case TrackingType.OnFirstSimOnly:
                    m_evtEventTracking = null;
                    break;

                case TrackingType.FireOnce:
                case TrackingType.Cancelable:
                    m_evtEventTracking = new List<Dictionary<int, EventSubscriber>>(iFrameBufferCount);

                    for (int i = 0; i < iFrameBufferCount; i++)
                    {
                        m_evtEventTracking.Add(new Dictionary<int, EventSubscriber>());
                    }

                    break;
            }
        }

        public void SetupForFrame()
        {
            m_argEventArguments.Clear();
        }

        public void QueueEventFire(Args args)
        {
            m_argEventArguments.Add(args);
        }

        public void Clear(int iIndex)
        {
            m_argEventArguments.Clear();
        }

        public void ApplyFrameEvents(int iIndex, byte bResimCount, bool bFirstSimOfTick)
        {
            //check event trigger mode 
            switch (Mode)
            {
                case TrackingType.None:
                    for (int i = 0; i < m_argEventArguments.Count; i++)
                    {
                        OnEvent?.Invoke(m_argEventArguments[i]);
                    }

                    break;
                case TrackingType.OnFirstSimOnly:
                    if (bFirstSimOfTick)
                    {
                        for (int i = 0; i < m_argEventArguments.Count; i++)
                        {
                            OnEvent?.Invoke(m_argEventArguments[i]);
                        }
                    }

                    break;
                case TrackingType.FireOnce:
                    //setup 
                    m_argEventArgumentsToCall.Clear();

                    //loop through all events 
                    for (int i = 0; i < m_argEventArguments.Count; i++)
                    {

                        //check if event exists in dictionary
                        if (!m_evtEventTracking[iIndex].ContainsKey(m_argEventArguments[i].GetHashCode()))
                        {
                            m_argEventArgumentsToCall.Add(m_argEventArguments[i]);
                        }

                    }

                    //loop through all the new events
                    for (int i = 0; i < m_argEventArgumentsToCall.Count; i++)
                    {
                        //fire event 
                        OnEvent?.Invoke(m_argEventArgumentsToCall[i]);

                        //add to event tracking 
                        m_evtEventTracking[iIndex].Add(m_argEventArgumentsToCall[i].GetHashCode(), null);
                    }

                    break;
                case TrackingType.Cancelable:
                    //setup 
                    m_argEventArgumentsToCall.Clear();
                    m_escEventsToCancel.Clear();

                    //loop through all events 
                    for (int i = 0; i < m_argEventArguments.Count; i++)
                    {
                        EventSubscriber escEventSub;

                        //check if event exists in dictionary
                        if (m_evtEventTracking[iIndex].TryGetValue(m_argEventArguments[i].GetHashCode(), out escEventSub))
                        {
                            //update the resim count 
                            escEventSub.UpdateRecallCount(bResimCount);
                        }
                        else
                        {
                            m_argEventArgumentsToCall.Add(m_argEventArguments[i]);
                        }

                    }

                    //check for events that need to be canceled 
                    foreach (KeyValuePair<int, EventSubscriber> kvpEntry in m_evtEventTracking[iIndex])
                    {
                        //check if item should be removed 
                        if (kvpEntry.Value.WasCanceled(bResimCount) || kvpEntry.Value.HasSubs() == false)
                        {
                            m_escEventsToCancel.Add(kvpEntry);
                        }

                    }

                    //loop through all items that have been canceled 
                    for (int i = 0; i < m_escEventsToCancel.Count; i++)
                    {
                        //cancel item
                        m_escEventsToCancel[i].Value.InvokeCancle();

                        //remove from tracked list 
                        m_evtEventTracking[iIndex].Remove(m_escEventsToCancel[i].Key);
                    }

                    //loop through all the new events
                    for (int i = 0; i < m_argEventArgumentsToCall.Count; i++)
                    {
                        EventSubscriber evsEventSubscriber = new EventSubscriber(bResimCount);

                        //fire event 
                        OnCancelableEvent?.Invoke(m_argEventArgumentsToCall[i], evsEventSubscriber);

                        //check if it should be tracked 
                        if (evsEventSubscriber.HasSubs())
                        {
                            //add to event tracking 
                            m_evtEventTracking[iIndex].Add(m_argEventArgumentsToCall[i].GetHashCode(), evsEventSubscriber);
                        }

                    }

                    break;
            }

            //clean up 
            m_argEventArguments.Clear();
        }
    }
}
