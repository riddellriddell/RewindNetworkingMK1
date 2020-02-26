using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace GameManagers
{
    public class MultiManagerSceneTester : MonoBehaviour
    {
        public List<ActiveGameManagerSceneTester> m_amgTesters;

        protected List<DateTime> m_dtmTimeOfActivation;

        public float m_fInitalTesterDelayTime;

        public float m_fMinAliveTime;

        public float m_fMinDeadTime;

        public int m_iMinActivePlayers;
        
        //the minimum time at least one peer has been alive for 
        public float m_fMinSwarmAliveTime;

        public float m_fChanceOfConenct;

        public float m_fChanceOfDisconnect;

        public void Start()
        {
            SetupActivationTimeList();

            EnableIndex(0);
        }

        public void Update()
        {
            int iEnabledPeers = EnabledPeers();

            //check if waiting for inital peer to setup
            if (iEnabledPeers == 1 && 
                m_amgTesters[0].gameObject.activeSelf == true && 
                m_fInitalTesterDelayTime > (DateTime.UtcNow - m_dtmTimeOfActivation[0]).TotalSeconds)
            {
                return;
            }
            

            // check if should add new peer 
            float fChanceToEnablePeer = m_fChanceOfConenct * Time.deltaTime;
            if (fChanceToEnablePeer > Random.Range(0.0f, 1.0f))
            {
                //get index to connect
                if(IndexOfValidPeerToActivate(out int iIndex))
                {
                    EnableIndex(iIndex);
                }
            }

            //check if should disconnect peer
            if(iEnabledPeers > m_iMinActivePlayers)
            {
                float fChanceToDisablePeer = m_fChanceOfDisconnect * Time.deltaTime;
                if (fChanceToDisablePeer > Random.Range(0.0f, 1.0f))
                {
                    //get index to connect
                    if (IndexOfValidPeerToDeactivate(out int iIndex))
                    {
                        DisableIndex(iIndex);
                    }
                }
            }

        }

        protected int EnabledPeers()
        {
            int iEnabledPeerCount = 0;

            for(int i = 0; i < m_amgTesters.Count; i++)
            {
                if (m_amgTesters[i].gameObject.activeSelf == true)
                {
                    iEnabledPeerCount++;
                }
            }

            return iEnabledPeerCount;
        }

        protected void SetupActivationTimeList()
        {
            m_dtmTimeOfActivation = new List<DateTime>(m_amgTesters.Count);

            for(int i = 0; i < m_amgTesters.Count; i++)
            {
                m_dtmTimeOfActivation.Add(DateTime.MinValue);
            }
        }

        protected bool IndexOfValidPeerToDeactivate(out int iValidIndex)
        {
            int iStartIndex = Random.Range(0, m_amgTesters.Count);

            for(int i = 0; i < m_amgTesters.Count; i++)
            {
                int iIndex = (i + iStartIndex) % m_amgTesters.Count;

                //check if index is enabled 
                if(m_amgTesters[iIndex].gameObject.activeSelf == false)
                {
                    continue;
                }

                //check if index is old enough
                if (m_fMinAliveTime > (DateTime.UtcNow - m_dtmTimeOfActivation[iIndex]).TotalSeconds)
                {
                    continue;
                }

                //check if disabling this peer would leave a peer in the swarm old enough
                //to maintain min swarm peer age
                if(OldestPeerInSwarmExcludingIndex(iIndex) < m_fMinSwarmAliveTime)
                {
                    continue;
                }

                iValidIndex = iIndex;

                return true;
            }

            iValidIndex = 0;

            return false;
        }

        protected bool IndexOfValidPeerToActivate(out int iValidIndex)
        {
            int iStartIndex = Random.Range(0, m_amgTesters.Count);

            for (int i = 0; i < m_amgTesters.Count; i++)
            {
                int iIndex = (i + iStartIndex) % m_amgTesters.Count;

                if (m_amgTesters[iIndex].gameObject.activeSelf == true)
                {
                    continue;
                }

                if(m_fMinDeadTime > (DateTime.UtcNow - m_dtmTimeOfActivation[iIndex]).TotalSeconds)
                {
                    continue;
                }

                iValidIndex = iIndex;

                return true;
            }

            iValidIndex = 0;

            return false;
        }

        protected void EnableIndex(int iIndex)
        {
            m_amgTesters[iIndex].gameObject.SetActive(true);

            m_dtmTimeOfActivation[iIndex] = DateTime.UtcNow;
        }

        protected void DisableIndex(int iIndex)
        {
            m_amgTesters[iIndex].gameObject.SetActive(false);

            m_dtmTimeOfActivation[iIndex] = DateTime.UtcNow;
        }

        protected float OldestPeerInSwarmExcludingIndex(int iExcludeIndex)
        {
            float fOldestPeerInSeconds = 0;

            for(int i = 0; i < m_amgTesters.Count; i++)
            {
                if(i == iExcludeIndex)
                {
                    continue;
                }

                //check if active 
                if(m_amgTesters[i].gameObject.activeSelf == false)
                {
                    continue;
                }

                //get age 
                float fAge = (float)(DateTime.UtcNow - m_dtmTimeOfActivation[i]).TotalSeconds;

                fOldestPeerInSeconds = Mathf.Max(fAge, fOldestPeerInSeconds);
            }

            return fOldestPeerInSeconds;
        }
    }
}
