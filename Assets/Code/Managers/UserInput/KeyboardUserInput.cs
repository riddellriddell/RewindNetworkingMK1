using GameViewUI;
using Sim;
using System;
using UnityEngine;

namespace GameManagers
{

    public interface IInputApplyer
    {
        void ApplyInputs(LocalPeerInputManager lpiTargetLocalPeerInputManager);
    }

    public class KeyboardUserInput : MonoBehaviour, IInputApplyer
    {
        public ActiveGameManager m_agmGameManager;

        //TODO:: this should be changed to model view controler where the UI calls the controler to make changes 
        public MobileInputUI m_mbiMobileInput;

        public void ApplyInputs(LocalPeerInputManager lpiTargetLocalPeerInputManager)
        {

            //TODO::this should be removed, dependency inverted and changed to a MVC system
            if(m_mbiMobileInput != null && m_mbiMobileInput.isActiveAndEnabled)
            {
                if (m_mbiMobileInput.m_bLeftPress)
                {
                    lpiTargetLocalPeerInputManager.OnLeftPressed();
                }

                if (m_mbiMobileInput.m_bLeftRelease)
                {
                    lpiTargetLocalPeerInputManager.OnLeftReleased();
                }

                if (m_mbiMobileInput.m_bRighPress)
                {
                    lpiTargetLocalPeerInputManager.OnRightPressed();
                }

                if (m_mbiMobileInput.m_bRighRelease)
                {
                    lpiTargetLocalPeerInputManager.OnRightReleased();
                }

                m_mbiMobileInput.ResetPressState();
            }

            if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
            {
                lpiTargetLocalPeerInputManager.OnLeftPressed();
            }

            if (Input.GetKeyUp(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
            {
                lpiTargetLocalPeerInputManager.OnLeftReleased();
            }

            if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyUp(KeyCode.LeftArrow))
            {
                lpiTargetLocalPeerInputManager.OnRightPressed();
            }

            if (Input.GetKeyUp(KeyCode.D) || Input.GetKeyUp(KeyCode.LeftArrow))
            {
                lpiTargetLocalPeerInputManager.OnRightReleased();
            }
        }
    }

    
}
