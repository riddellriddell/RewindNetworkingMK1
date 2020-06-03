using FixedPointy;
using UnityEngine;

namespace Sim
{
    public struct SettingsData
    {
        #region GLobalSettings
        public int m_iMaxPlayers;

        #endregion

        #region ShipStats 
        public Fix m_fixShipSize;

        public int m_iShipHealth;

        public Fix m_fixShipHealRate;

        public Fix m_fixHealDelayTime;

        public Fix m_fixShipSpawnImmunityTime;

        #endregion

        #region ShipMovement 

        public Fix m_fixShipSpeed;

        public Fix m_fixShipBoostSpeed;

        public Fix m_fixShipAcceleration;

        public Fix m_fixShipBoostAcceleration;

        public Fix m_fixShipDeceleration;

        public Fix m_fixShipStunDeceleration;

        public Fix m_fixShipTurnRate;

        public Fix m_fixShipStunSpinRate;

        public Fix m_fixShipRecoverTurnRate;

        public Fix m_fixShipStunTime;

        public Fix m_fixShipRecoverTime;

        #endregion

        #region MainGun

        public Fix m_fixShipAimExtraTurn;

        public Fix m_fixShipAimExtraInterpolate;

        public Fix m_fixShipAutofireRange;

        public Fix m_fixShipAutoFireCone;

        public Fix m_fixShipFireRate;

        #endregion

        #region LaserProjectile

        public Fix m_fixLaserSize;

        public Fix m_fixLaserSpeed;

        public int m_iLaserDamage;

        #endregion

    }

    [CreateAssetMenu(fileName = "SimSettings", menuName = "Simulation/Settings", order = 1)]
    public class SettingsDataInterface : ScriptableObject
    {
        #region GLobalSettings
        public int m_iMaxPlayers;
        #endregion

        #region ShipStats 
        public FixValueUnityInterface m_fixShipSize;

        public int m_iShipHealth;

        public FixValueUnityInterface m_fixShipHealRate;

        public FixValueUnityInterface m_fixHealDelayTime;

        public FixValueUnityInterface m_fixShipSpawnImmunityTime;

        #endregion

        #region ShipMovement 

        public FixValueUnityInterface m_fixShipSpeed;

        public FixValueUnityInterface m_fixShipBoostSpeed;

        public FixValueUnityInterface m_fixShipAcceleration;

        public FixValueUnityInterface m_fixShipBoostAcceleration;

        public FixValueUnityInterface m_fixShipDeceleration;

        public FixValueUnityInterface m_fixShipStunDeceleration;

        public FixValueUnityInterface m_fixShipTurnRate;

        public FixValueUnityInterface m_fixShipStunSpinRate;

        public FixValueUnityInterface m_fixShipRecoverTurnRate;

        public FixValueUnityInterface m_fixShipStunTime;

        public FixValueUnityInterface m_fixShipRecoverTime;

        #endregion

        #region MainGun

        public FixValueUnityInterface m_fixShipAimExtraTurn;

        public FixValueUnityInterface m_fixShipAimExtraInterpolate;

        public FixValueUnityInterface m_fixShipAutofireRange;

        public FixValueUnityInterface m_fixShipAutoFireCone;

        public FixValueUnityInterface m_fixShipFireRate;

        #endregion

        #region LaserProjectile

        public FixValueUnityInterface m_fixLaserSize;

        public FixValueUnityInterface m_fixLaserSpeed;

        public int m_iLaserDamage;

        #endregion

        //convert scriptable object to settings data struct 
        public SettingsData ConvertToSettingsData()
        {
            #region ShipStats 
            m_fixShipSize.CalculateValue();

            m_fixShipHealRate.CalculateValue();

            m_fixHealDelayTime.CalculateValue();

            m_fixShipSpawnImmunityTime.CalculateValue();

            #endregion

            #region ShipMovement 

            m_fixShipSpeed.CalculateValue();

            m_fixShipBoostSpeed.CalculateValue();

            m_fixShipAcceleration.CalculateValue();

            m_fixShipBoostAcceleration.CalculateValue();

            m_fixShipDeceleration.CalculateValue();

            m_fixShipStunDeceleration.CalculateValue();

            m_fixShipTurnRate.CalculateValue();

            m_fixShipStunSpinRate.CalculateValue();

            m_fixShipRecoverTurnRate.CalculateValue();

            m_fixShipStunTime.CalculateValue();

            m_fixShipRecoverTime.CalculateValue();

            #endregion

            #region MainGun

            m_fixShipAimExtraTurn.CalculateValue();

            m_fixShipAimExtraInterpolate.CalculateValue();

            m_fixShipAutofireRange.CalculateValue();

            m_fixShipAutoFireCone.CalculateValue();

            m_fixShipFireRate.CalculateValue();

            #endregion

            #region LaserProjectile

            m_fixLaserSize.CalculateValue();

            m_fixLaserSpeed.CalculateValue();


            #endregion


            SettingsData sdaOut = new SettingsData()
            {
                #region GLobalSettings
                m_iMaxPlayers = m_iMaxPlayers,
                #endregion

                #region ShipStats 
                m_fixShipSize = m_fixShipSize.FixValue,

                m_iShipHealth = m_iShipHealth,

                m_fixShipHealRate = m_fixShipHealRate.FixValue,

                m_fixHealDelayTime = m_fixHealDelayTime.FixValue,


                m_fixShipSpawnImmunityTime = m_fixShipSpawnImmunityTime.FixValue,

                #endregion

                #region ShipMovement 

                m_fixShipSpeed = m_fixShipSpeed.FixValue,

                m_fixShipBoostSpeed = m_fixShipBoostSpeed.FixValue,

                m_fixShipAcceleration = m_fixShipAcceleration.FixValue,

                m_fixShipBoostAcceleration = m_fixShipBoostAcceleration.FixValue,

                m_fixShipDeceleration = m_fixShipDeceleration.FixValue,

                m_fixShipStunDeceleration = m_fixShipStunDeceleration.FixValue,

                m_fixShipTurnRate = m_fixShipTurnRate.FixValue,

                m_fixShipStunSpinRate = m_fixShipStunSpinRate.FixValue,

                m_fixShipRecoverTurnRate = m_fixShipRecoverTurnRate.FixValue,

                m_fixShipStunTime = m_fixShipStunTime.FixValue,

                m_fixShipRecoverTime = m_fixShipRecoverTime.FixValue,

                #endregion

                #region MainGun

                m_fixShipAimExtraTurn = m_fixShipAimExtraTurn.FixValue,

                m_fixShipAimExtraInterpolate = m_fixShipAimExtraInterpolate.FixValue,

                m_fixShipAutofireRange = m_fixShipAutofireRange.FixValue,

                m_fixShipAutoFireCone = m_fixShipAutoFireCone.FixValue,

                m_fixShipFireRate = m_fixShipFireRate.FixValue,

                #endregion

                #region LaserProjectile

                m_fixLaserSize = m_fixLaserSize.FixValue,

                m_fixLaserSpeed = m_fixLaserSpeed.FixValue,

                m_iLaserDamage = m_iLaserDamage

                #endregion

            };

            return sdaOut;
        }
    }



    public class GameSettingsInterfaceDepricated : ScriptableObject
    {
        public bool RunHashChecks = false;

        public bool Invincibility = true;

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