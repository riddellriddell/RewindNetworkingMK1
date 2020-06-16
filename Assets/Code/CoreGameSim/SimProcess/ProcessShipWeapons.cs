using FixedPointy;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Sim
{
    public interface IShipWeaponsSettingsData
    {
        Fix AutofireCone { get; }

        Fix AutoFireConeRangeSqr { get; }

        Fix TimeBetweenShots { get; }
    }

    public interface IShipWeaponFrameData
    {
        byte[] LazerFireIndex { get; set; }

        Fix[] TimeUntilNextFire { get; set; }
    }

    public class ProcessShipWeapons<TFrameData, TConstData, TSettingsData> :
        ISimProcess<TFrameData, TConstData, TSettingsData>,
        ISimSetupProcesses<TFrameData, TSettingsData>
        where TFrameData : IPeerSlotAssignmentFrameData, IShipWeaponFrameData, IShipHealthframeData, IShipPositions, ILazerFrameData, IFrameData, new()
        where TSettingsData : IPeerSlotAssignmentSettingsData, IShipWeaponsSettingsData, IShipCollisionSettingsData, IShipHealthSettingsData, ISimTickRateSettings, ILazerSettingsData
        where TConstData : IAsteroidCollisionConstData
    {
        public int Priority { get; } = 10;

        public string ProcessName { get; } = "RespawnDeadShips";

        public bool ApplySetupProcess(uint iTick, in TSettingsData sdaSettingsData, long lFirstPeerID, ref TFrameData fdaFrameData)
        {
            fdaFrameData.LazerFireIndex = new byte[sdaSettingsData.MaxPlayers];
            fdaFrameData.TimeUntilNextFire = new Fix[sdaSettingsData.MaxPlayers];

            return true;
        }

        public bool ProcessFrameData(uint iTick, in TSettingsData sdaSettingsData, in TConstData cdaConstantData, in TFrameData fdaInFrameData, in object[] objInputs, ref TFrameData fdaOutFrameData)
        {
            
            for( int i = 0; i < fdaOutFrameData.PeerSlotAssignment.Length; i++)
            {
                //check peer assigned to slot
                if(fdaOutFrameData.PeerSlotAssignment[i] != long.MinValue && fdaOutFrameData.ShipHealth[i] > Fix.Zero)
                {
                    //apply weapon cooldown
                    fdaOutFrameData.TimeUntilNextFire[i] = FixMath.Max(0, fdaOutFrameData.TimeUntilNextFire[i] - sdaSettingsData.SecondsPerTick);

                    //check if target in range to hit
                    if(fdaOutFrameData.TimeUntilNextFire[i] <=  Fix.Zero)
                    {
                        bool bShouldFire = false;

                        //loop through all ships excluding current ship and check if they are in range to hit
                        for(int j = 1; j < fdaOutFrameData.ShipPositionX.Length; j++)
                        {
                            int iIndex = (j + i) % fdaOutFrameData.ShipPositionX.Length;
                        
                            //check if target ship is alive
                            if(fdaOutFrameData.ShipHealth[iIndex] > Fix.Zero)
                            {
                                //get dist sqr to target
                                Fix fixDeltaX = fdaOutFrameData.ShipPositionX[iIndex] - fdaOutFrameData.ShipPositionX[i];
                                Fix fixDeltaY = fdaOutFrameData.ShipPositionY[iIndex] - fdaOutFrameData.ShipPositionY[i];

                                Fix fixDistSqr = (fixDeltaX * fixDeltaX) + (fixDeltaY * fixDeltaY);

                                //check if target is in range 
                                if (fixDistSqr < sdaSettingsData.AutoFireConeRangeSqr && fixDistSqr > 0)
                                {
                                    //get direction to target
                                    Fix fixDirectionToTarget = FixMath.Atan2(fixDeltaY, fixDeltaX);

                                    //get difference in direction
                                    Fix fixAngleDifference = (((fixDirectionToTarget - fdaOutFrameData.ShipBaseAngle[i]) + 180) % 360) - 180;

                                    //check if target is in fire cone 
                                    if (FixMath.Abs(fixAngleDifference) < sdaSettingsData.AutofireCone)
                                    {
                                        bShouldFire = true;

                                        //exit fire loop 
                                        break;
                                    }
                                }
                            }   
                        }

                        if (bShouldFire)
                        {
                            //Fire Lazer
                            //increment fire index 
                            fdaOutFrameData.LazerFireIndex[i] = (byte)((fdaOutFrameData.LazerFireIndex[i] + i) % 256);

                            //fire lazer 
                            ProcessLazers<TFrameData, TConstData, TSettingsData>.FireLazer(
                                fdaOutFrameData,
                                sdaSettingsData,
                                i,
                                fdaOutFrameData.LazerFireIndex[i],
                                fdaOutFrameData.ShipPositionX[i],
                                fdaOutFrameData.ShipPositionY[i],
                                fdaOutFrameData.ShipBaseAngle[i]);

                            //reset time untill next shot
                            fdaOutFrameData.TimeUntilNextFire[i] = sdaSettingsData.TimeBetweenShots;
                        }
                    }
                }
            }

            return true;
        }
    }
}
