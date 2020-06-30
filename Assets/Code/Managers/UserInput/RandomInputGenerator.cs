using Sim;
using System;
using UnityEngine;

namespace GameManagers
{
    public class RandomInputGenerator : MonoBehaviour, IInputApplyer
    {
        public float m_fChanceOfDirectionChange = 1f;

        public float m_fChanceOfSpecial = 4f;

        public float m_fCanceSpecialIsMissile = 0.5f;

        public float m_fMaxMissileHoldTime = 3f;

        protected DateTime m_dtmTimeOfLastUpdate = DateTime.MinValue;

        public void ApplyInputs(LocalPeerInputManager lpiTargetLocalPeerInputManager)
        {
            if (m_dtmTimeOfLastUpdate == DateTime.MinValue)
            {
                m_dtmTimeOfLastUpdate = DateTime.UtcNow;
            }

            float fDeltaTime = (float)(DateTime.UtcNow - m_dtmTimeOfLastUpdate).TotalSeconds;

            m_dtmTimeOfLastUpdate = DateTime.UtcNow;

            //check for direction change 
            if (UnityEngine.Random.Range(0.0f, 1.0f) < m_fChanceOfDirectionChange * fDeltaTime)
            {
                int iMoveType = UnityEngine.Random.Range(0, 4);

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