using System;
using System.Collections.Generic;
using UnityEngine;

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

        private DateTime m_dtmStart;


        public void Start()
        {
            m_dtmStart = DateTime.Now;
        }

        public void Update()
        {
            //get time since start
            TimeSpan tspTimeDifference = DateTime.Now - m_dtmStart;

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