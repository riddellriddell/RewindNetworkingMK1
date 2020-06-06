using System.Collections;
using System.Collections.Generic;
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
                m_fixShipTurnRate = m_fixShipTurnRate.Value
                #endregion
            };

            return sdaOut;
        }
    }
}