using SimDataInterpolation;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

namespace GameViewUI
{
    public class DeadUI : MonoBehaviour
    {
        public CanvasGroup m_cgvFadeOutGroup;

        //respawn countdown
        public Text m_txtCountDown;

        public AnimationCurve m_amcFaidIn;

        public float m_fFadeOutRate;

        protected bool m_bFadingOut;

        public void Update()
        {
            // fade out pannel on state change 
            if(m_bFadingOut)
            {
                m_txtCountDown.text = "Now!";

                m_cgvFadeOutGroup.alpha = m_cgvFadeOutGroup.alpha - (m_fFadeOutRate * Time.deltaTime);

                if(m_cgvFadeOutGroup.alpha <= 0)
                {
                    this.gameObject.SetActive(false);
                }
            }
        }

        public void OnUpdate(InterpolatedFrameDataGen ifdFrameData, int iLocalPeerIndex)
        {
            if(iLocalPeerIndex < 0)
            {
                m_txtCountDown.text = "Waiting For Spawn";

                return;
            }

            m_txtCountDown.text = ifdFrameData.m_fixTimeUntilRespawn[iLocalPeerIndex].ToString("N1");

            m_cgvFadeOutGroup.alpha = m_amcFaidIn.Evaluate(ifdFrameData.m_fixTimeUntilRespawnErrorAdjusted[iLocalPeerIndex]);
        }

        public void OnEnterState()
        {
            gameObject.SetActive(true);

            m_bFadingOut = false;

            m_cgvFadeOutGroup.alpha = 0;

            m_txtCountDown.text = "";
        }

        public void OnExitState()
        {
            m_bFadingOut = true;
        }
    }
}