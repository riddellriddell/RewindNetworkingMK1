using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FixedPointy;

namespace Sim
{
    [CreateAssetMenu(fileName = "SimGlobalSettings", menuName = "Simulation/Settings", order = 1)]
    public class GameSettings : ScriptableObject
    {
        public bool RunHashChecks = false;
        
        //serialization format of variables 
        public FixValueUnityInterface TickDelta;

        public FixValueUnityInterface ChararcterSize;

        public FixValueUnityInterface QuickAttackRange;

        public FixValueUnityInterface QuickAttackAOE;

        public FixValueUnityInterface QuickAttackWarmUp;

        public FixValueUnityInterface QuickAttackCoolDown;

        public short QuickAttackDamage;

        public FixValueUnityInterface SlowAttackRange;

        public FixValueUnityInterface SlowAttackAOE;

        public FixValueUnityInterface SlowAttackWarmUp;

        public FixValueUnityInterface SlowAttackCoolDown;

        public short SlowAttackDammage;

        public FixValueUnityInterface BlockingCoolDown;
        
        public FixValueUnityInterface MovementSpeed;

        public FixVec2ValueUnityInterface GameFieldSize;

        public FixValueUnityInterface TargetQueueSize;

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