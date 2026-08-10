using Sim;
using System;
using UnityEngine;
using Utility;

namespace GameManagers
{
    public class RandomInputGenerator : MonoBehaviour, IInputApplyer
    {
        public float m_fChanceOfDirectionChange = 1f;

        public float m_fChanceOfSpecial = 4f;

        public float m_fCanceSpecialIsMissile = 0.5f;

        public float m_fMaxMissileHoldTime = 3f;
        
        public TimeSourceComponentBase m_tscTimeSource;

        protected DateTime m_dtmTimeOfLastUpdate;
        
        protected DeterministicRandomNumberGenerator m_drgRandomNumberGenerator = new DeterministicRandomNumberGenerator(12345678);

        public void Start()
        {
            m_dtmTimeOfLastUpdate = m_tscTimeSource.UTCNow;
        }

        public void ApplyInputs(LocalPeerInputManager lpiTargetLocalPeerInputManager)
        {
            //check if enabled
            if (enabled == false)
            {
                return;
            }
            
            if (m_dtmTimeOfLastUpdate == DateTime.MinValue)
            {
                m_dtmTimeOfLastUpdate = m_tscTimeSource.UTCNow;
            }

            float fDeltaTime = (float)(m_tscTimeSource.UTCNow - m_dtmTimeOfLastUpdate).TotalSeconds;

            m_dtmTimeOfLastUpdate = m_tscTimeSource.UTCNow;

            //check for direction change 
            if (m_drgRandomNumberGenerator.GetRandomRangeFloat(0.0f, 1.0f) < m_fChanceOfDirectionChange * fDeltaTime)
            {
                int iMoveType = m_drgRandomNumberGenerator.GetRandomRangeInt(0, 4);

                switch (iMoveType)
                {
                    case 0:
                        if (SimInputManager.GetTurnLeft(lpiTargetLocalPeerInputManager.m_bInputState) == true)
                        {
                            lpiTargetLocalPeerInputManager.OnLeftReleased();
                        }

                        if (SimInputManager.GetTurnRight(lpiTargetLocalPeerInputManager.m_bInputState) == true)
                        {
                            lpiTargetLocalPeerInputManager.OnRightReleased();
                        }

                        if (SimInputManager.GetBoost(lpiTargetLocalPeerInputManager.m_bInputState) == true)
                        {
                            lpiTargetLocalPeerInputManager.OnLeftReleased();
                            lpiTargetLocalPeerInputManager.OnRightReleased();
                        }

                        break;

                    case 1:

                        if (SimInputManager.GetBoost(lpiTargetLocalPeerInputManager.m_bInputState) == false)
                        {
                            lpiTargetLocalPeerInputManager.OnRightPressed();
                            lpiTargetLocalPeerInputManager.OnLeftPressed();
                        }

                        break;

                    case 2:

                        if (SimInputManager.GetBoost(lpiTargetLocalPeerInputManager.m_bInputState) == true)
                        {
                            lpiTargetLocalPeerInputManager.OnRightReleased();
                        }

                        if (SimInputManager.GetTurnLeft(lpiTargetLocalPeerInputManager.m_bInputState) == false)
                        {
                            lpiTargetLocalPeerInputManager.OnLeftPressed();
                        }

                        if (SimInputManager.GetTurnRight(lpiTargetLocalPeerInputManager.m_bInputState) == true)
                        {
                            lpiTargetLocalPeerInputManager.OnRightReleased();
                        }

                        break;


                    case 3:

                        if (SimInputManager.GetBoost(lpiTargetLocalPeerInputManager.m_bInputState) == true)
                        {
                            lpiTargetLocalPeerInputManager.OnLeftReleased();
                        }

                        if (SimInputManager.GetTurnLeft(lpiTargetLocalPeerInputManager.m_bInputState) == true)
                        {
                            lpiTargetLocalPeerInputManager.OnLeftReleased();
                        }

                        if (SimInputManager.GetTurnRight(lpiTargetLocalPeerInputManager.m_bInputState) == false)
                        {
                            lpiTargetLocalPeerInputManager.OnRightPressed();
                        }

                        break;
                }
            }
            //TODO: add code to test disruptor and missile firing 
        }
    }
}