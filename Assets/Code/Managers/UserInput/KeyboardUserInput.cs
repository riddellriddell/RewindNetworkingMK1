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

        public void ApplyInputs(LocalPeerInputManager lpiTargetLocalPeerInputManager)
        {
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
