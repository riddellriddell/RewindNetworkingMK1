using SimDataInterpolation;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GameViewUI
{
    public class AliveUI : MonoBehaviour
    {
        public Text m_txtShipHealth;

        public Text m_txtHealthState;

        public Image m_objTakeDamageWarning;

        public Color m_colHealing;

        public Color m_colWaitingForHeal;

        public Color m_colTakeDamageColour;

        public Color m_colLowHealthWarning;

        public AnimationCurve m_amcHelthTextHealingFlash;

        public AnimationCurve m_amcWaitingForHealFlash;

        public AnimationCurve m_amcTakeDamageCurve;

        public AnimationCurve m_amcLowHealthCurve;
        
        public float m_fLowHealthLevel;

        protected float m_fTimeSinceTakeDamage;

        protected float m_fLastHealth;

        public void OnEnterState()
        {
            gameObject.SetActive(true);
        }

        public void OnUpdate(InterpolatedFrameDataGen ifdFrameData, int iPeerIndex)
        {
            if(iPeerIndex < 0)
            {
                return;
            }

            float fHealth = ifdFrameData.m_fixShipHealth[iPeerIndex];

            m_txtShipHealth.text = fHealth.ToString("N0");
            m_txtHealthState.text = "";
            m_txtHealthState.color = Color.clear;

            //flash waiting for heal
            if (ifdFrameData.m_fixShipHealDelayTimeOutErrorAdjusted[iPeerIndex] > 0)
            {
                //flash waiting for healing 
                float fHealingCycle = Time.timeSinceLevelLoad % m_amcWaitingForHealFlash.keys[m_amcWaitingForHealFlash.keys.Length - 1].time;

                m_txtHealthState.color = new Color(m_colWaitingForHeal.r, m_colWaitingForHeal.g, m_colWaitingForHeal.b, m_amcWaitingForHealFlash.Evaluate(fHealingCycle));

                m_txtHealthState.text = "Offline!";
            }

            //check for damage
            if (fHealth < m_fLastHealth)
            {
                m_fTimeSinceTakeDamage = 0;

                m_txtHealthState.color = m_colTakeDamageColour;
            }
            else if (fHealth > m_fLastHealth)
            {
                //flash Healing
                float fHealingCycle = Time.timeSinceLevelLoad % m_amcHelthTextHealingFlash.keys[m_amcHelthTextHealingFlash.keys.Length - 1].time;

                m_txtHealthState.color = new Color(m_colHealing.r, m_colHealing.g, m_colHealing.b, m_amcHelthTextHealingFlash.Evaluate(fHealingCycle));

                m_txtHealthState.text = "Repairing"; 
            }

            //update screen border 
            m_objTakeDamageWarning.color = Color.clear;

            //flash low health warning 
            if (fHealth < m_fLowHealthLevel)
            {
                float fLowHealthCycle = Time.timeSinceLevelLoad % m_amcLowHealthCurve.keys[m_amcLowHealthCurve.keys.Length - 1].time;

                m_objTakeDamageWarning.color = new Color(m_colLowHealthWarning.r, m_colLowHealthWarning.g, m_colLowHealthWarning.b, m_amcLowHealthCurve.Evaluate(fLowHealthCycle));
            }           

            //flash Damage warning 
            if (m_fTimeSinceTakeDamage < m_amcTakeDamageCurve.keys[m_amcTakeDamageCurve.keys.Length - 1].time)
            {
                m_fTimeSinceTakeDamage += Time.deltaTime;

                m_objTakeDamageWarning.color = new Color( m_colTakeDamageColour.r, m_colTakeDamageColour.g, m_colTakeDamageColour.b, m_amcTakeDamageCurve.Evaluate(m_fTimeSinceTakeDamage));
            }

            m_fLastHealth = fHealth;
        }

        public void OnExitState()
        {
            gameObject.SetActive(true);
        }
    }
}
