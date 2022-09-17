using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Networking
{
    [CreateAssetMenu(fileName = "NetworkSettings", menuName = "Network/Settings", order = 1)]
    public class NetworkConnectionSettings : ScriptableObject
    {
        //----- Network settings -----------------
        [SerializeField]
        public int m_iMaxBytesToSend = 1000;
        
        [SerializeField]
        public int m_iMaxPackestInFlight = 100;

        [SerializeField]
        public float m_fConnectionTimeOutTime = 20.0f;

        [SerializeField]
        public float m_fConnectionEstablishTimeOut = 60.0f;

        [SerializeField]
        public float m_fMaxTimeBetweenMessages =  0.5f;

        //-------- Time constrol settings -------------

        [SerializeField]
        public float m_fTimeLerpSpeed = 0.2f;

        [SerializeField]
        public float m_fMaxLerpDistance =  10;

        [SerializeField]
        public float m_fUpdateRate =  1;

        [SerializeField]
        public float m_fMaxLatencyUsedInCalculations = 2;

        //-------- Sim State Sync Processor -----------------

        [SerializeField]
        public int m_iMaxSegmentSize = 300;

        [SerializeField]
        public float m_fStateRequestTimeOut = 10f;

        [SerializeField]
        public float m_fSegmentRequestTimeOut = 2f;

        [SerializeField]
        public float m_fMaxFailedRequestPercent = 0.5f;

        //--------------- Network Gateway Manager -----------
        [SerializeField]
        public float m_fGatewayAnounceRate = 3f;

        [SerializeField]
        public float m_fGatewayTimeout = 10;

        //----------------- Network Large Packet Transfer Manager --------------------------

        [SerializeField]
        public int s_iStartBufferSize = 2000;

        //------------- Connection Propegator processor --------------------------------
        
        [SerializeField]
        public float m_fForceConnectionTime = 20;

        //-------------- Chain Manager ------------------------
        //the percent of active peers that need to acknowledge a link to make it 
        //a valid base link candidate

        [SerializeField]
        public float m_fPercentOfAcknowledgementsToRebase = 0.5f;

        [SerializeField]
        public uint m_iMinCycleAge = 10;

        [SerializeField]
        public uint m_iMaxCycleAge = 100;

        [SerializeField]
        public int m_iMinChainLenght = 10;

        [SerializeField]
        public float m_fTimeBetweenLinks = 1.0f;

        //--------------- Global Messaging State ----------------------

        //--------------- NetworkGlobalMessengerProcessor -----------------
        [SerializeField]
        public float m_fVoteTimeout = 2f;

        [SerializeField]
        public float m_fChannelTimeOutTime = 3.0f;

        [SerializeField]
        public float m_fStateCollectionTimeOutTime = 20.0f;

        [SerializeField]
        public float m_fMinPercentOfStartStatesFromPeers = 0.75f;

        [SerializeField]
        public float m_fJoinVoteGracePeriod = 30.0f;

        [SerializeField]
        public float m_fOldConnectionFilterPadding = 5f;
    }
}
