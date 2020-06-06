using FixedPointy;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Sim
{
    public interface IShipPositions
    {
        Fix[] ShipPositionX { get; set; }
        Fix[] ShipPositionY{ get; set; }
        Fix[] ShipVelocityX { get; set; }
        Fix[] ShipVelocityY { get; set; }
        Fix[] ShipBaseAngle { get; set; }

    }

    public class SetupShipMovementProcess<TFrameData, TSettingsData> : ISimSetupProcesses<TFrameData, TSettingsData> where TFrameData : IShipPositions where TSettingsData : IPeerSlotAssignmentSettingsData
    {
        public int Priority { get; } = -1;

        public string ProcessName { get; } = "Setting up ship Positions";

        public bool ApplySetupProcess(uint iTick, in TSettingsData sdaSettingsData, long lFirstPeerID, ref TFrameData fdaFrameData)
        {
            fdaFrameData.ShipPositionX = new Fix[sdaSettingsData.MaxPlayers];
            fdaFrameData.ShipPositionY = new Fix[sdaSettingsData.MaxPlayers];
            fdaFrameData.ShipVelocityX = new Fix[sdaSettingsData.MaxPlayers];
            fdaFrameData.ShipVelocityY = new Fix[sdaSettingsData.MaxPlayers];
            fdaFrameData.ShipBaseAngle = new Fix[sdaSettingsData.MaxPlayers];

            return true;
        }
    }
}
