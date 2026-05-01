using Networking;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Utility;

namespace Networking
{
    public class InternetConnectionSimulator : MonoBehaviour
    {
        public static InternetConnectionSimulator Instance { get; private set; }

        private struct TimeStampedWrapper
        {
            public byte[] m_bData;
            public Action<byte[]> m_actRecieveDataCallback;
            public DateTime m_dtmTimeOfDelivery;
        }

        public bool m_bEnableLag = false;
        public float m_fMinLag = 0.25f;
        public float m_fMaxLag = 1f;
        public bool m_bEnableOutages = false;
        public float m_fMinOutage = 0.25f;
        public float m_fMaxOutage = 8f;
        public float m_fMinTimeBetweenOutages = 3;
        public float m_fMaxTimeBetweenOutages = 9;
        public bool m_bEnablePacketLoss = false;
        public float m_fPacketLoss = 0.3f;

        public TimeSourceComponentBase m_tscTimeSource = null;

        private DateTime m_dtmTimeOfLastUpdate;
        private float m_fTimeSinceLastUpdate;
        
        private float m_fTimeUntillNextOutage;
        private float m_fOutageTimeRemainig;

        private List<TimeStampedWrapper> m_lstDataInFlight;
        
        private DeterministicRandomNumberGenerator m_rng = new DeterministicRandomNumberGenerator(12345678);

        protected SortedList<DateTime, Action> m_actDelayedActions = new SortedList<DateTime, Action>();

        [Obsolete]
        public void SendPacket(PacketWrapper packetToSend, Connection conTarget)
        {
            //check if packet is dropped 
            if (IsPacketDropped())
            {
                return;
            }

            //loop through list of packets in flight to find one not in use 
            for (int i = 0; i < m_lstDataInFlight.Count; i++)
            {
                if (m_lstDataInFlight[i].m_dtmTimeOfDelivery == DateTime.MinValue)
                {
                    m_lstDataInFlight[i] = new TimeStampedWrapper()
                    {
                        m_bData = packetToSend.WriteStream.GetData(),
                        m_actRecieveDataCallback = conTarget.ReceivePacket,
                        m_dtmTimeOfDelivery = CalcuateDeliveryTime()
                    };

                    return;
                }
            }

            //if there is not enough room already then add a new entry to the list 
            m_lstDataInFlight.Add(new TimeStampedWrapper()
            {
                m_bData = packetToSend.WriteStream.GetData(),
                m_actRecieveDataCallback = conTarget.ReceivePacket,
                m_dtmTimeOfDelivery = CalcuateDeliveryTime()
            });
        }

        public void SendPacket(byte[] bData, Action<byte[]> actCallback)
        {
            //check if packet is dropped 
            if (IsPacketDropped())
            {
                return;
            }

            //loop through list of packets in flight to find one not in use 
            for (int i = 0; i < m_lstDataInFlight.Count; i++)
            {
                if (m_lstDataInFlight[i].m_dtmTimeOfDelivery == DateTime.MinValue)
                {
                    m_lstDataInFlight[i] = new TimeStampedWrapper()
                    {
                        m_bData = bData,
                        m_actRecieveDataCallback = actCallback,
                        m_dtmTimeOfDelivery = CalcuateDeliveryTime()
                    };

                    return;
                }
            }

            //if there is not enough room already then add a new entry to the list 
            m_lstDataInFlight.Add(new TimeStampedWrapper()
            {
                m_bData = bData,
                m_actRecieveDataCallback = actCallback,
                m_dtmTimeOfDelivery = CalcuateDeliveryTime()
            });
        }

        // Use this for initialization
        void Start()
        {
            if(Instance == null)
            {
                Instance = this;
            }

            m_lstDataInFlight = new List<TimeStampedWrapper>();

            m_dtmTimeOfLastUpdate = m_tscTimeSource.UTCNow;
        }

        // Update is called once per frame
        void Update()
        {
            m_fTimeSinceLastUpdate =  (float)(m_tscTimeSource.UTCNow - m_dtmTimeOfLastUpdate).TotalSeconds;
            m_dtmTimeOfLastUpdate = m_tscTimeSource.UTCNow;
            
            //update the packet outage 
            UpdatePacketOutages();

            if (m_lstDataInFlight != null)
            {
                //loop through all the packets in flight 
                for (int i = 0; i < m_lstDataInFlight.Count; i++)
                {
                    if (m_lstDataInFlight[i].m_dtmTimeOfDelivery < m_tscTimeSource.UTCNow &&
                        m_lstDataInFlight[i].m_bData != null)
                    {
                        //deliver packet 
                        m_lstDataInFlight[i].m_actRecieveDataCallback?.Invoke(m_lstDataInFlight[i].m_bData);

                        TimeStampedWrapper tswNewWrapper = new TimeStampedWrapper();
                        
                        tswNewWrapper.m_dtmTimeOfDelivery = DateTime.MinValue;
                        
                        m_lstDataInFlight[i] = tswNewWrapper;
                    }
                }
            }
        
        }
        
        private void UpdatePacketOutages()
        {
            if(!m_bEnableOutages)
            {
                return;
            }

            if (m_fOutageTimeRemainig > 0)
            {
                m_fOutageTimeRemainig -= m_fTimeSinceLastUpdate;
            }
            else if (m_fTimeUntillNextOutage > 0)
            {
                m_fTimeUntillNextOutage -= m_fTimeSinceLastUpdate;
            }
            else
            {
                m_fOutageTimeRemainig = m_rng.GetRandomRangeFloat(m_fMinOutage, m_fMaxOutage);
                m_fTimeUntillNextOutage = m_rng.GetRandomRangeFloat(m_fMinTimeBetweenOutages, m_fMaxTimeBetweenOutages);
            }
        }

        private bool IsPacketDropped()
        {
            if (m_fOutageTimeRemainig > 0 && m_bEnableOutages)
            {
                return true;
            }

            if (m_rng.GetRandomRangeFloat(0f, 1f) < m_fPacketLoss && m_bEnablePacketLoss)
            {
                return true;
            }

            return false;
        }

        private DateTime CalcuateDeliveryTime()
        {
            if (m_bEnableLag)
            {
                return m_tscTimeSource.UTCNow + TimeSpan.FromSeconds( m_rng.GetRandomRangeFloat(m_fMinLag, m_fMaxLag));
            }
            else
            {
                return m_tscTimeSource.UTCNow ;
            }
        }
        
        public void RunDelayedActions()
        {
            //get the time
            DateTime dtmNow = m_tscTimeSource.UTCNow;
            
            //loop through the delayed actions and execute them
            while (m_actDelayedActions.Count > 0)
            {
                DateTime dtmActionExecuteTime = GetNextActionTime();

                if (dtmActionExecuteTime >= dtmNow)
                {
                    break;
                }

                Action actActionToExecute = DequeueAction();
                
                actActionToExecute.Invoke();
            }
        }
        protected void QueueAction(DateTime dtmTimeToExecute, Action actDelayedAction)
        {
            m_actDelayedActions.Add(dtmTimeToExecute, actDelayedAction);
        }
        
        protected void QueueAction(TimeSpan tspDelayUntilExecute, Action actDelayedAction)
        {
            DateTime dtmNow = m_tscTimeSource.UTCNow;
            m_actDelayedActions.Add(dtmNow + tspDelayUntilExecute, actDelayedAction);
        }

        public void QueueAction(float fDelayUntilExecute, Action actDelayedAction)
        {
            DateTime dtmNow = m_tscTimeSource.UTCNow;

            DateTime dtmKey = dtmNow + TimeSpan.FromSeconds(fDelayUntilExecute);
            
            
            //check if something already exists, if it does then delay the smallest amount possible
            while (m_actDelayedActions.ContainsKey(dtmKey))
            {
                dtmKey += TimeSpan.FromTicks(1);
            }
            
            m_actDelayedActions.Add(dtmKey, actDelayedAction);
        }
        
        protected DateTime GetNextActionTime()
        {
            if (m_actDelayedActions.Count > 0)
            {
                return m_actDelayedActions.Keys.First();
            }

            return DateTime.MaxValue;
        }

        protected Action DequeueAction()
        {
            
            Action actFirstAction = m_actDelayedActions.FirstOrDefault().Value;
            
            m_actDelayedActions.RemoveAt(0);

            return actFirstAction;
        }
    }
}
