using System;
using System.Collections.Generic;
using UnityEngine;
using Utility;

namespace Code.Managers.Testing
{
  
    [System.Serializable]
    public struct ClientEnableDelay
    {
        [SerializeField]
        public float m_fDelay;

        [SerializeField]
        public GameObject m_objTarget;
    }
    
    public class TestClientEnabler: MonoBehaviour
    {
        [SerializeField]
        public List<ClientEnableDelay> m_cedTargets = new List<ClientEnableDelay>();

        public TimeSourceComponentBase m_tscTimeSource;
        
        private DateTime m_dtmStart;
        
        


        public void Start()
        {
            m_dtmStart = m_tscTimeSource.UTCNow;
        }

        public void Update()
        {
            //get time since start
            TimeSpan tspTimeDifference = m_tscTimeSource.UTCNow - m_dtmStart;

            double dSeconds = tspTimeDifference.TotalSeconds;
            
            foreach (ClientEnableDelay target in m_cedTargets)
            {
                if (target.m_fDelay < dSeconds)
                {
                    target.m_objTarget.SetActive(true);
                }
            }
        }
    }
}