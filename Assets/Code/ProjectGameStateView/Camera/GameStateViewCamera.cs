using SimDataInterpolation;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GameStateView
{

    public class GameStateViewCamera : MonoBehaviour
    {
        public float m_fLeadAmount;

        public float m_fLeadMinSpeed;

        public float m_fLeadMaxSpeed;

        public float m_fCameraAcceleration;

        public float m_fCameraFriction;

        public float m_fMaxCameraDistanceFromTarget;

        public AnimationCurve m_amcShakeFrequencyAtForce;

        public AnimationCurve m_amcShakeProfile;
        
        public float m_fShakeProcession;

        public float m_fShakeFoceForDamage;

        public float m_fShakeForceMultiplyer;

        public float m_fShakeRestitution;

        public float m_fShakeOnHealthChangeGreaterThan;

        protected float m_fLastHealth;
        
        protected float m_fShakeForce;

        protected float m_fProgressThroughShakeX;
        protected float m_fProgressThroughShakeY;
        protected Vector3 m_vecSmoothedCameraPos;
        protected Vector3 m_vecCameraVelocity;

        public void OnViewUpdate(InterpolatedFrameDataGen ifdFrameData, long lLocalPeerID )
        {
            int iPeerIndex = int.MinValue;

            //get local peer index
            for(int i = 0; i < ifdFrameData.m_lPeersAssignedToSlot.Length; i++)
            {
                if(ifdFrameData.m_lPeersAssignedToSlot[i] == lLocalPeerID)
                {
                    iPeerIndex = i;

                    break;
                }
            }

            if(iPeerIndex == int.MinValue)
            {
                return;
            }

            //check for damage
            if(m_fLastHealth > (ifdFrameData.m_fixShipHealth[iPeerIndex] + m_fShakeOnHealthChangeGreaterThan))
            {
                AddShakeForce(m_fShakeFoceForDamage);
            }

            m_fLastHealth = ifdFrameData.m_fixShipHealth[iPeerIndex];

            if (ifdFrameData.m_fixShipHealth[iPeerIndex] > 0)
            {
                //get lead pos
                Vector3 vecTargetCameraPos = CalcCameraLeadPos(
                    ifdFrameData.m_fixShipPosXErrorAdjusted[iPeerIndex],
                    ifdFrameData.m_fixShipPosYErrorAdjusted[iPeerIndex],
                    ifdFrameData.m_fixShipVelocityXErrorAdjusted[iPeerIndex],
                    ifdFrameData.m_fixShipVelocityYErrorAdjusted[iPeerIndex]);

                //add smoothing 
                m_vecSmoothedCameraPos = CalcSmoothedCameraPos(
                    vecTargetCameraPos,
                    m_vecSmoothedCameraPos,
                    ref m_vecCameraVelocity);
            }
            else
            {
                //continue smoothing 
                m_vecSmoothedCameraPos = CalcSmoothedCameraPos(
                    m_vecSmoothedCameraPos,
                    m_vecSmoothedCameraPos,
                    ref m_vecCameraVelocity);
            }

            //add any shake force 
            Vector3 vecShakenCamerPos = m_vecSmoothedCameraPos + CalcShakeOffset();

            //update camera shake values 
            UpdateCameraShake();

            //apply effect to camera pos
            gameObject.transform.position = vecShakenCamerPos;
        }

        public void AddShakeForce(float fExraShakeForceAmount)
        {
            m_fShakeForce += fExraShakeForceAmount;
        }
    
        public Vector3 CalcCameraLeadPos(float fTargetX, float fTargetY, float fTargetVelX, float fTargetVelY)
        {
            Vector3 vecCurrentPos = new Vector3(fTargetX, 0, fTargetY);

            Vector3 vecLeadDirection = new Vector3(fTargetVelX, 0, fTargetVelY);

            float fLerpAmount = (Mathf.Clamp(vecLeadDirection.magnitude, m_fLeadMinSpeed, m_fLeadMaxSpeed) - m_fLeadMinSpeed) / (m_fLeadMaxSpeed - m_fLeadMinSpeed);

            Vector3 vecLeadPos = vecCurrentPos + (vecLeadDirection.normalized * (fLerpAmount * m_fLeadAmount));

            return vecLeadPos;
        }

        public Vector3 CalcSmoothedCameraPos(Vector3 vecTargetCameraPos,Vector3 vecCurrentCameraPos,ref Vector3 vecCameraVelocity)
        {
            //get velocity dif
            Vector3 vecPredictedCameraPos = vecCurrentCameraPos + (vecCameraVelocity * Time.deltaTime);
            Vector3 vecPredictionError = vecTargetCameraPos - vecPredictedCameraPos;

            //get force to push camera
            Vector3 vecCameraAcceleration = vecPredictionError.normalized * m_fCameraAcceleration * Time.deltaTime;

            //acceleerate camera towards target
            vecCameraVelocity += vecCameraAcceleration;

            //apply friction on camera 
            vecCameraVelocity = vecCameraVelocity * (1 - (m_fCameraFriction * Time.deltaTime));

            //calc new camera position
            Vector3 vecNewCameraPos = vecCurrentCameraPos + (vecCameraVelocity * Time.deltaTime);

            //snap camera if distance it too great
            Vector3 vecCameraDistance = vecTargetCameraPos - vecNewCameraPos;

            float fDistanceFromTarget = vecCameraDistance.magnitude;

            if (fDistanceFromTarget > m_fMaxCameraDistanceFromTarget)
            {
                float fExcessDistance = fDistanceFromTarget - m_fMaxCameraDistanceFromTarget;

                vecNewCameraPos = vecNewCameraPos + (vecCameraDistance.normalized * fExcessDistance);
            }

            return vecNewCameraPos;
        }

        public void UpdateCameraShake ()
        {
            //move through the shake plus add a little drift between the 2 so the shake is never in the same direction
            float fShakeProgress = m_amcShakeFrequencyAtForce.Evaluate(m_fShakeForce) * Time.deltaTime;

            m_fProgressThroughShakeX = (m_fProgressThroughShakeX + fShakeProgress) % 1;
            m_fProgressThroughShakeY = (m_fProgressThroughShakeX + fShakeProgress + (m_fShakeProcession * Time.deltaTime)) % 1;

            //reduce shake amount 
            m_fShakeForce -= m_fShakeForce * m_fShakeRestitution * Time.deltaTime;

        }

        public Vector3 CalcShakeOffset()
        {
            Vector3 vecOffset = new Vector3(
            m_amcShakeProfile.Evaluate(m_fProgressThroughShakeX) * m_fShakeForce * m_fShakeForceMultiplyer,
            0,
            m_amcShakeProfile.Evaluate(m_fProgressThroughShakeY) * m_fShakeForce * m_fShakeForceMultiplyer);

            return vecOffset;
        }

    }
}
