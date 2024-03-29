﻿using Networking;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Networking
{
    public class InternetConnectionSimulator : MonoBehaviour
    {
        public static InternetConnectionSimulator Instance { get; private set; }

        private struct TimeStampedWrapper
        {
            public byte[] m_bData;
            public Action<byte[]> m_actRecieveDataCallback;
            public float m_fTimeOfDelivery;
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

        private float m_fTimeUntillNextOutage;
        private float m_fOutageTimeRemainig;

        private List<TimeStampedWrapper> m_lstDataInFlight;

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
                if (m_lstDataInFlight[i].m_fTimeOfDelivery == 0)
                {
                    m_lstDataInFlight[i] = new TimeStampedWrapper()
                    {
                        m_bData = packetToSend.WriteStream.GetData(),
                        m_actRecieveDataCallback = conTarget.ReceivePacket,
                        m_fTimeOfDelivery = CalcuateDeliveryTime()
                    };

                    return;
                }
            }

            //if there is not enough room already then add a new entry to the list 
            m_lstDataInFlight.Add(new TimeStampedWrapper()
            {
                m_bData = packetToSend.WriteStream.GetData(),
                m_actRecieveDataCallback = conTarget.ReceivePacket,
                m_fTimeOfDelivery = CalcuateDeliveryTime()
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
                if (m_lstDataInFlight[i].m_fTimeOfDelivery == 0)
                {
                    m_lstDataInFlight[i] = new TimeStampedWrapper()
                    {
                        m_bData = bData,
                        m_actRecieveDataCallback = actCallback,
                        m_fTimeOfDelivery = CalcuateDeliveryTime()
                    };

                    return;
                }
            }

            //if there is not enough room already then add a new entry to the list 
            m_lstDataInFlight.Add(new TimeStampedWrapper()
            {
                m_bData = bData,
                m_actRecieveDataCallback = actCallback,
                m_fTimeOfDelivery = CalcuateDeliveryTime()
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
        }

        // Update is called once per frame
        void Update()
        {
            //update the packet outage 
            UpdatePacketOutages();

            //loop through all the packets in flight 
            for (int i = 0; i < m_lstDataInFlight.Count; i++)
            {
                if (m_lstDataInFlight[i].m_fTimeOfDelivery < Time.timeSinceLevelLoad && m_lstDataInFlight[i].m_bData != null)
                {
                    //deliver packet 
                    m_lstDataInFlight[i].m_actRecieveDataCallback?.Invoke(m_lstDataInFlight[i].m_bData);

                    m_lstDataInFlight[i] = new TimeStampedWrapper();
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
                m_fOutageTimeRemainig -= Time.deltaTime;
            }
            else if (m_fTimeUntillNextOutage > 0)
            {
                m_fTimeUntillNextOutage -= Time.deltaTime;
            }
            else
            {
                m_fOutageTimeRemainig = Random.Range(m_fMinOutage, m_fMaxOutage);
                m_fTimeUntillNextOutage = Random.Range(m_fMinTimeBetweenOutages, m_fMaxTimeBetweenOutages);
            }
        }

        private bool IsPacketDropped()
        {
            if (m_fOutageTimeRemainig > 0 && m_bEnableOutages)
            {
                return true;
            }

            if (Random.Range(0f, 1f) < m_fPacketLoss && m_bEnablePacketLoss)
            {
                return true;
            }

            return false;
        }

        private float CalcuateDeliveryTime()
        {
            if (m_bEnableLag)
            {
                return Time.timeSinceLevelLoad + Random.Range(m_fMinLag, m_fMaxLag);
            }
            else
            {
                return Time.timeSinceLevelLoad;
            }
        }
    }
}
