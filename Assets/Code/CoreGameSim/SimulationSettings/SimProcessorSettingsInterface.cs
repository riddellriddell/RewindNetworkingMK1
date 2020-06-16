using FixedPointy;
using System;
using UnityEngine;

namespace Sim
{
    [CreateAssetMenu(fileName = "SimSettings", menuName = "Simulation/Settings", order = 1)]
    public class SimProcessorSettingsInterface : ScriptableObject
    {
        #region IPeerSlotAssignmentSettingsData
        [SerializeField]
        public int m_iMaxPlayers;
        #endregion

        #region IShipRespawnSettingsData
        [SerializeField]
        public FixTo3PlacesUnityInterface m_fixShipRespawnTime;
        #endregion

        #region IShipHealthSettingsData
        [SerializeField]
        public FixTo3PlacesUnityInterface m_iShipMaxHealth;
        [SerializeField]
        public FixTo3PlacesUnityInterface m_fixShipHealRate;
        [SerializeField]
        public FixTo3PlacesUnityInterface m_fixHealDelayTime;
        #endregion

        #region IShipMovementSettingsData
        [SerializeField]
        public FixTo3PlacesUnityInterface m_fixShipSpeed;
        [SerializeField]
        public FixTo3PlacesUnityInterface m_fixShipAcceleration;
        [SerializeField]
        public FixTo3PlacesUnityInterface m_fixShipTurnRate;
        #endregion

        #region ISimTickRateSettings
        [SerializeField]
        public int m_iTicksPerSecond;
        #endregion

        #region IShipCollisionSettingsData

        public FixTo3PlacesUnityInterface m_fixShipRestitution;

        public FixTo3PlacesUnityInterface m_fixShipFriction;

        public FixTo3PlacesUnityInterface m_fixShipSize;

        public FixTo3PlacesUnityInterface m_fixShipMass;

        public FixTo3PlacesUnityInterface m_fixShipImpactDamage;

        #endregion

        #region IShipWeaponsSettingsData
        public FixTo3PlacesUnityInterface m_fixAutofireCone;

        public FixTo3PlacesUnityInterface m_fixAutoFireConeRange;

        public FixTo3PlacesUnityInterface m_fixTimeBetweenShots;
        #endregion

        #region ILazerSettingsData
        public FixTo3PlacesUnityInterface m_fixLazerLife;

        public FixTo3PlacesUnityInterface m_fixLazerSpeed;

        public FixTo3PlacesUnityInterface m_fixLazerDamage;

        public FixTo3PlacesUnityInterface m_fixLazerSize;
        #endregion

        //convert scriptable object to settings data struct 
        public SimProcessorSettings ConvertToSettingsData()
        {
            #region IPeerSlotAssignmentSettingsData
            #endregion

            #region IShipRespawnSettingsData
            m_fixShipRespawnTime.CalculateValue();
            #endregion

            #region IShipHealthSettingsData
            m_iShipMaxHealth.CalculateValue();
            m_fixShipHealRate.CalculateValue();
            m_fixHealDelayTime.CalculateValue();
            #endregion

            #region IShipMovementSettingsData
            m_fixShipSpeed.CalculateValue();
            m_fixShipAcceleration.CalculateValue();
            m_fixShipTurnRate.CalculateValue();
            #endregion

            #region IShipCollisionSettingsData

            m_fixShipRestitution.CalculateValue();

            m_fixShipFriction.CalculateValue();

            m_fixShipSize.CalculateValue();

            m_fixShipMass.CalculateValue();

            m_fixShipImpactDamage.CalculateValue();
            #endregion

            #region IShipWeaponsSettingsData
            m_fixAutofireCone.CalculateValue();

            m_fixAutoFireConeRange.CalculateValue();

            m_fixTimeBetweenShots.CalculateValue();
            #endregion

            #region ILazerSettingsData
            m_fixLazerLife.CalculateValue();

            m_fixLazerSpeed.CalculateValue();

            m_fixLazerDamage.CalculateValue();

            m_fixLazerSize.CalculateValue();
            #endregion

            SimProcessorSettings sdaOut = new SimProcessorSettings()
            {
                #region IPeerSlotAssignmentSettingsData
                m_iMaxPlayers = m_iMaxPlayers,
                #endregion

                #region IShipRespawnSettingsData
                m_fixShipRespawnTime = m_fixShipRespawnTime.Value,
                #endregion

                #region IShipHealthSettingsData
                m_iShipMaxHealth = m_iShipMaxHealth.Value,
                m_fixShipHealRate = m_fixShipHealRate.Value,
                m_fixHealDelayTime = m_fixHealDelayTime.Value,
                #endregion

                #region IShipMovementSettingsData
                m_fixShipSpeed = m_fixShipSpeed.Value,
                m_fixShipAcceleration = m_fixShipAcceleration.Value,
                m_fixShipTurnRate = m_fixShipTurnRate.Value,
                #endregion

                #region ISimSecondsPerTick
                m_iTicksPerSecond = m_iTicksPerSecond,
                m_lSimTickLength = TimeSpan.TicksPerSecond / m_iTicksPerSecond,
                m_fixSecondsPerTick = Fix.Ratio(1, m_iTicksPerSecond),
                #endregion

                #region IShipCollisionSettingsData

                m_fixShipRestitution = m_fixShipRestitution.Value,

                m_fixShipFriction = m_fixShipFriction.Value,

                m_fixShipSize = m_fixShipSize.Value,

                m_fixShipInverseMass = Fix.One / m_fixShipMass.Value,

                m_fixShipImpactDamage = m_fixShipImpactDamage.Value,
                #endregion

                #region IShipWeaponsSettingsData
                m_fixAutofireCone = m_fixAutofireCone.Value,

                m_fixAutoFireConeRangeSqr = m_fixAutoFireConeRange.Value * m_fixAutoFireConeRange.Value,

                m_fixTimeBetweenShots = m_fixTimeBetweenShots.Value,
                #endregion

                #region ILazerSettingsData
                m_fixLazerLife = m_fixLazerLife.Value,

                m_fixLazerSpeed = m_fixLazerSpeed.Value,

                m_fixLazerDamage = m_fixLazerDamage.Value,

                m_fixLazerSize = m_fixLazerSpeed.Value,
                #endregion
            };

            return sdaOut;
        }
    }
}