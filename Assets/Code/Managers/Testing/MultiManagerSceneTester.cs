using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace GameManagers
{
    public class MultiManagerSceneTester : MonoBehaviour
    {
        public enum TestMode
        {
            RANDOM_CONNECT_AND_DISCONENCT,
            FIXED_HOST_RANDOM_CONNECT_AND_DISCONNECT,
            TIMED_ADDITION
        }

        public List<ActiveGameManagerSceneTester> m_amgTesters;

        protected List<DateTime> m_dtmTimeOfActivation;

        //leaves on peer active for this time to setup iniaial game without adding or removing people
        public float m_fInitalTesterDelayTime;

        //the min time a peer should be alive before killing the connection
        public float m_fMinAliveTime = 5;

        //the min time a connection should be dead before reviving it
        public float m_fMinDeadTime = 1;

        //the target min number of players to be active at once
        public int m_iMinActivePlayers = 2;
        
        //the minimum time at least one peer has been alive for 
        public float m_fMinSwarmAliveTime = 5;

        public TestMode m_tsmTestMode = TestMode.RANDOM_CONNECT_AND_DISCONENCT;

        //the random chance of a peer connection
        public float m_fChanceOfConenct = 0.05f; // default to once every 20 seconds

        //the random chance of a peer disconnecting
        public float m_fChanceOfDisconnect = 0.05f; // default to once every 20 seconds

        public float m_fTimeBetweenAdditions = 5.0f;

        public float m_fTimeSinceLastConnect = 0;

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
            
            switch(m_tsmTestMode)
            {
                case TestMode.RANDOM_CONNECT_AND_DISCONENCT:
                {
                    // check if should add new peer 
                    float fChanceToEnablePeer = m_fChanceOfConenct * Time.deltaTime;
                    if (fChanceToEnablePeer > Random.Range(0.0f, 1.0f))
                    {
                        //get index to connect
                        if (IndexOfValidPeerToActivate(out int iIndex))
                        {
                            EnableIndex(iIndex);
                        }
                    }

                    //check if should disconnect peer
                    if (iEnabledPeers > m_iMinActivePlayers)
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
                    break;
                }
                case TestMode.FIXED_HOST_RANDOM_CONNECT_AND_DISCONNECT:
                    {

                        // check if should add new peer 
                        float fChanceToEnablePeer = m_fChanceOfConenct * Time.deltaTime;
                        if (fChanceToEnablePeer > Random.Range(0.0f, 1.0f))
                        {
                            //get index to connect
                            if (IndexOfValidPeerToActivate(out int iIndex , 0))
                            {
                                EnableIndex(iIndex);
                            }
                        }

                        //check if should disconnect peer
                        if (iEnabledPeers > m_iMinActivePlayers)
                        {
                            float fChanceToDisablePeer = m_fChanceOfDisconnect * Time.deltaTime;
                            if (fChanceToDisablePeer > Random.Range(0.0f, 1.0f))
                            {
                                //get index to connect
                                if (IndexOfValidPeerToDeactivate(out int iIndex, 0))
                                {
                                    DisableIndex(iIndex);
                                }
                            }
                        }
                        break;
                    }
                case TestMode.TIMED_ADDITION:
                    {
                        m_fTimeSinceLastConnect += Time.deltaTime;

                        if (m_fTimeSinceLastConnect > m_fTimeBetweenAdditions)
                        {
                            //get index to connect
                            if (IndexOfValidPeerToActivate(out int iIndex))
                            {
                                m_fTimeSinceLastConnect = 0;
                                EnableIndex(iIndex);
                            }
                        }
                        break;
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

        protected bool IndexOfValidPeerToDeactivate(out int iValidIndex, int iExcludeIndex = -1)
        {
            int iStartIndex = Random.Range(0, m_amgTesters.Count);

            for(int i = 0; i < m_amgTesters.Count; i++)
            {
                int iIndex = (i + iStartIndex) % m_amgTesters.Count;

                //check if index should be excludeed
                if(iIndex == iExcludeIndex)
                {
                    continue;
                }

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

        protected bool IndexOfValidPeerToActivate(out int iValidIndex, int iExcludeIndex = -1)
        {
            int iStartIndex = Random.Range(0, m_amgTesters.Count);

            for (int i = 0; i < m_amgTesters.Count; i++)
            {
                int iIndex = (i + iStartIndex) % m_amgTesters.Count;

                //check if index should be excludeed
                if (iIndex == iExcludeIndex)
                {
                    continue;
                }

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
