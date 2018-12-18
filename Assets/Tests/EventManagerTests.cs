using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;
using Sim;
using System;

public class EventManagerTests
{
    public class TestEventManager : EventManager
    {
        public EventTracker<int> m_evtTestEventNone;

        public EventTracker<int> m_evtTestEventFirst;

        public EventTracker<int> m_evtTestEventOnce;

        public EventTracker<int> m_evtTestEventCancel;

        public TestEventManager(int bufferSize) : base(bufferSize) { }
        

        protected override void SetupEvents()
        {
            RegisterEvent(ref m_evtTestEventNone, EventTracker<int>.TrackingType.None);
            RegisterEvent(ref m_evtTestEventFirst, EventTracker<int>.TrackingType.OnFirstSimOnly);
            RegisterEvent(ref m_evtTestEventOnce, EventTracker<int>.TrackingType.FireOnce);
            RegisterEvent(ref m_evtTestEventCancel, EventTracker<int>.TrackingType.Cancelable);
        }
    }

    public TestEventManager m_evmEventManager;

    public bool m_bEventCalled;

    public bool m_bEventCanceled;

    [Test]
    public void EventManagerTestsSimplePasses()
    {
        // Use the Assert class to test conditions.
    }
        
    /// <summary>
    /// test if the target function is called if an event is registered with the event manager
    /// </summary>
    [Test]
    public void BasicCheckIfEventIsCalled()
    {
        //setup event manager
        SetupEventManager(60);

        //register for event
        m_evmEventManager.m_evtTestEventNone.OnEvent += OnEventCall;

        //prepare for call
        m_bEventCalled = false;

        //clear event manager 
        m_evmEventManager.PrepForFrameEvents(0);

        //queue event 
        m_evmEventManager.m_evtTestEventNone.QueueEventFire(0);

        //fire events
        m_evmEventManager.SendEventsForFrame();

        //clean up event listening 
        m_evmEventManager.m_evtTestEventNone.OnEvent -= OnEventCall;

        //check if event was called
        Debug.Assert(m_bEventCalled == true,"None Mode Event was not called");

        
        //register for event
        m_evmEventManager.m_evtTestEventFirst.OnEvent += OnEventCall;

        //prepare for call
        m_bEventCalled = false;

        //clear event manager 
        m_evmEventManager.PrepForFrameEvents(1);

        //queue event 
        m_evmEventManager.m_evtTestEventFirst.QueueEventFire(0);

        //fire events
        m_evmEventManager.SendEventsForFrame();

        //clean up event listening 
        m_evmEventManager.m_evtTestEventFirst.OnEvent -= OnEventCall;

        //check if event was called
        Debug.Assert(m_bEventCalled == true, "First Mode Event was not called");


        //register for event
        m_evmEventManager.m_evtTestEventOnce.OnEvent += OnEventCall;

        //prepare for call
        m_bEventCalled = false;

        //clear event manager 
        m_evmEventManager.PrepForFrameEvents(2);

        //queue event 
        m_evmEventManager.m_evtTestEventOnce.QueueEventFire(0);

        //fire events
        m_evmEventManager.SendEventsForFrame();

        //clean up event listening 
        m_evmEventManager.m_evtTestEventOnce.OnEvent -= OnEventCall;

        //check if event was called
        Debug.Assert(m_bEventCalled == true, "Once Mode Event was not called");

        //register for event
        m_evmEventManager.m_evtTestEventCancel.OnCancelableEvent += OnEventCallCancelable;

        //prepare for call
        m_bEventCalled = false;

        //clear event manager 
        m_evmEventManager.PrepForFrameEvents(3);

        //queue event 
        m_evmEventManager.m_evtTestEventCancel.QueueEventFire(0);

        //fire events
        m_evmEventManager.SendEventsForFrame();

        //clean up event listening 
        m_evmEventManager.m_evtTestEventCancel.OnEvent -= OnEventCall;

        //check if event was called
        Debug.Assert(m_bEventCalled == true, "Cancel Mode Event was not called");
    }

    /// <summary>
    /// check if the event is only fired once if on first sim mode 
    /// </summary>
    [Test]
    public void CheckIfEventFiredOnFirstSim()
    {
        //setup event manager
        SetupEventManager(60);

        //register for event
        m_evmEventManager.m_evtTestEventFirst.OnEvent += OnEventCall;

        //prepare for call
        m_bEventCalled = false;

        //clear event manager 
        m_evmEventManager.PrepForFrameEvents(1);

        //queue event 
        m_evmEventManager.m_evtTestEventFirst.QueueEventFire(0);

        //fire events
        m_evmEventManager.SendEventsForFrame();
        
        //check if event was called
        Debug.Assert(m_bEventCalled == true, "First Mode Event was not called");

        //prepare for call
        m_bEventCalled = false;

        //clear event manager 
        m_evmEventManager.PrepForFrameEvents(1);

        //queue event 
        m_evmEventManager.m_evtTestEventFirst.QueueEventFire(0);

        //fire events
        m_evmEventManager.SendEventsForFrame();
        
        //check if event was called
        Debug.Assert(m_bEventCalled == false, "First Mode Event was called when it shouldn't");

        //prepare for call
        m_bEventCalled = false;

        //clear event manager 
        m_evmEventManager.PrepForFrameEvents(2);

        //queue event 
        m_evmEventManager.m_evtTestEventFirst.QueueEventFire(0);

        //fire events
        m_evmEventManager.SendEventsForFrame();

        //check if event was called
        Debug.Assert(m_bEventCalled == true, "First Mode Event was not called");

    }

    /// <summary>
    /// check if event only gets fired once on fire once mode 
    /// </summary>
    [Test]
    public void CheckIfEventFiredOnce()
    {
        //setup event manager
        SetupEventManager(60);

        //register for event
        m_evmEventManager.m_evtTestEventOnce.OnEvent += OnEventCall;

        //prepare for call
        m_bEventCalled = false;

        //clear event manager 
        m_evmEventManager.PrepForFrameEvents(1);

        //queue event 
        m_evmEventManager.m_evtTestEventOnce.QueueEventFire(0);

        //fire events
        m_evmEventManager.SendEventsForFrame();

        //check if event was called
        Debug.Assert(m_bEventCalled == true, "Once Mode Event was not called");

        //prepare for call
        m_bEventCalled = false;

        //clear event manager 
        m_evmEventManager.PrepForFrameEvents(1);

        //queue event 
        m_evmEventManager.m_evtTestEventOnce.QueueEventFire(0);

        //fire events
        m_evmEventManager.SendEventsForFrame();

        //check if event was called
        Debug.Assert(m_bEventCalled == false, "Once Mode Event was called when it shouldn't");
    }

    /// <summary>
    /// check if the event gets correctly canceled 
    /// </summary>
    [Test]
    public void CheckIfCanceled()
    {
        //setup event manager
        SetupEventManager(60);

        //register for event
        m_evmEventManager.m_evtTestEventCancel.OnCancelableEvent += OnEventCallCancelable;

        //prepare for call
        m_bEventCalled = false;
        m_bEventCanceled = false;

        //clear event manager 
        m_evmEventManager.PrepForFrameEvents(1);

        //queue event 
        m_evmEventManager.m_evtTestEventCancel.QueueEventFire(0);

        //fire events
        m_evmEventManager.SendEventsForFrame();

        //check if event was called
        Debug.Assert(m_bEventCalled == true, "Cancel Mode Event was not called");

        //clear event manager 
        m_evmEventManager.PrepForFrameEvents(1);

        //fire events
        m_evmEventManager.SendEventsForFrame();

        //check if event was called
        Debug.Assert(m_bEventCanceled == true, "Cancel Mode did not correctly cancel event");
    }
    protected void SetupEventManager(int iFrameBufferCount)
    {
        m_evmEventManager = new TestEventManager(iFrameBufferCount);

        Debug.Assert(m_evmEventManager != null, "Faild to setup");

    }

    protected void OnEventCall(int testValue)
    {
        m_bEventCalled = true;
    }

    protected void OnEventCallCancelable(int testValue,EventSubscriber evsEventSubscription)
    {
        m_bEventCalled = true;
        evsEventSubscription.OnEventCancel += OnEventCancel;
    }

    protected void OnEventCancel()
    {
        m_bEventCanceled = true;
    }
        
}
