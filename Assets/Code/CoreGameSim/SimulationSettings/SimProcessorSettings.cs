using FixedPointy;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Sim
{
    public struct SimProcessorSettings : IPeerSlotAssignmentSettingsData, IShipRespawnSettingsData, IShipHealthSettingsData, IShipMovementSettingsData
    {
        #region IPeerSlotAssignmentSettingsData
        public int m_iMaxPlayers;
        public int MaxPlayers => m_iMaxPlayers;
        #endregion

        #region IShipRespawnSettingsData
        public Fix m_fixShipRespawnTime;

        public Fix ShipRespawnTime => m_fixShipRespawnTime;
        #endregion

        #region IShipHealthSettingsData
        public Fix m_iShipMaxHealth;

        public Fix m_fixShipHealRate;

        public Fix m_fixHealDelayTime;

        public Fix ShipMaxHealth => m_iShipMaxHealth;

        public Fix ShipHealDelayTime => m_fixHealDelayTime;

        public Fix ShipHealRate => m_fixShipHealRate;
        #endregion

        #region IShipMovementSettingsData
        public Fix m_fixShipSpeed;

        public Fix m_fixShipAcceleration;

        public Fix m_fixShipTurnRate;

        public Fix ShipSpeed { get => m_fixShipSpeed; }

        public Fix ShipAcceleration { get => m_fixShipAcceleration; }

        public Fix ShipTurnRate { get => m_fixShipTurnRate; }
        #endregion

        //public Fix m_fixShipBoostSpeed;
        //
        //
        //
        //public Fix m_fixShipBoostAcceleration;
        //
        //public Fix m_fixShipDeceleration;
        //
        //public Fix m_fixShipStunDeceleration;
        //
        //
        //
        //public Fix m_fixShipStunSpinRate;
        //
        //public Fix m_fixShipRecoverTurnRate;
        //
        //public Fix m_fixShipStunTime;
        //
        //public Fix m_fixShipRecoverTime;
        //
        //
        //
        //#region MainGun
        //
        //public Fix m_fixShipAimExtraTurn;
        //
        //public Fix m_fixShipAimExtraInterpolate;
        //
        //public Fix m_fixShipAutofireRange;
        //
        //public Fix m_fixShipAutoFireCone;
        //
        //public Fix m_fixShipFireRate;
        //
        //#endregion
        //
        //#region LaserProjectile
        //
        //public Fix m_fixLaserSize;
        //
        //public Fix m_fixLaserSpeed;
        //
        //public int m_iLaserDamage;
        //
        //
        //#endregion

    }
}