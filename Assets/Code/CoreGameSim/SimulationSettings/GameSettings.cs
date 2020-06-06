using FixedPointy;
using UnityEngine;

namespace Sim
{
    public class GameSettingsInterfaceDepricated : ScriptableObject
    {
        public bool RunHashChecks = false;

        public bool Invincibility = true;

        //serialization format of variables 
        public FixTo3PlacesUnityInterface TickDelta;

        public FixTo3PlacesUnityInterface ChararcterSize;

        public FixTo3PlacesUnityInterface QuickAttackRange;

        public FixTo3PlacesUnityInterface QuickAttackAOE;

        public FixTo3PlacesUnityInterface QuickAttackWarmUp;

        public FixTo3PlacesUnityInterface QuickAttackCoolDown;

        public short QuickAttackDamage;

        public FixTo3PlacesUnityInterface SlowAttackRange;

        public FixTo3PlacesUnityInterface SlowAttackAOE;

        public FixTo3PlacesUnityInterface SlowAttackWarmUp;

        public FixTo3PlacesUnityInterface SlowAttackCoolDown;

        public short SlowAttackDammage;

        public FixTo3PlacesUnityInterface BlockingCoolDown;

        public FixTo3PlacesUnityInterface MovementSpeed;

        public FixVec2ValueUnityInterface GameFieldSize;

        public FixTo3PlacesUnityInterface TargetQueueSize;

        public short PlayerHealth = 100;

        public void Deserialize()
        {
            Debug.Log("Awake");

            //serialization format of variables 
            TickDelta.CalculateValue();
            ChararcterSize.CalculateValue();

            QuickAttackRange.CalculateValue();
            QuickAttackAOE.CalculateValue();
            QuickAttackWarmUp.CalculateValue();
            QuickAttackCoolDown.CalculateValue();

            SlowAttackRange.CalculateValue();
            SlowAttackAOE.CalculateValue();
            SlowAttackWarmUp.CalculateValue();
            SlowAttackCoolDown.CalculateValue();

            MovementSpeed.CalculateValue();
            GameFieldSize.CalculateValue();
            TargetQueueSize.CalculateValue();
        }
    }
}