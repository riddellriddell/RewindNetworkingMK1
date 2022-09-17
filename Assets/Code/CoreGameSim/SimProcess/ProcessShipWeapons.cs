using FixedPointy;

namespace Sim
{
    public interface IShipWeaponsSettingsData
    {
        Fix AutofireCone { get; }

        Fix AutoFireConeRangeSqr { get; }

        Fix TimeBetweenShots { get; }

        Fix ShotsChargeTime { get; }
    }

    public interface IShipWeaponFrameData
    {
        byte[] LazerFireIndex { get; set; }

        Fix[] TimeUntilLaserFire { get; set; }

        Fix[] TimeUntilLaserReset { get; set; }
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
            fdaFrameData.TimeUntilLaserFire = new Fix[sdaSettingsData.MaxPlayers];
            fdaFrameData.TimeUntilLaserReset = new Fix[sdaSettingsData.MaxPlayers];

            return true;
        }

        public bool ProcessFrameData(uint iTick, in TSettingsData sdaSettingsData, in TConstData cdaConstantData, in TFrameData fdaInFrameData, in object[] objInputs, ref TFrameData fdaOutFrameData)
        {

            for (int i = 0; i < fdaOutFrameData.PeerSlotAssignment.Length; i++)
            {
                //check peer assigned to slot
                if (fdaOutFrameData.PeerSlotAssignment[i] != long.MinValue && fdaOutFrameData.ShipHealth[i] > Fix.Zero)
                {
                    //apply weapon charge
                    fdaOutFrameData.TimeUntilLaserFire[i] = fdaOutFrameData.TimeUntilLaserFire[i] - sdaSettingsData.SecondsPerTick;

                    //apply weapon cooldown
                    fdaOutFrameData.TimeUntilLaserReset[i] = fdaOutFrameData.TimeUntilLaserReset[i] - sdaSettingsData.SecondsPerTick;

                    //check if it is time to fire weapon 
                    if (fdaOutFrameData.TimeUntilLaserFire[i] < Fix.Zero && (fdaOutFrameData.TimeUntilLaserFire[i] + sdaSettingsData.SecondsPerTick) >= Fix.Zero)
                    {
                        //Fire Lazer
                        //increment fire index 
                        fdaOutFrameData.LazerFireIndex[i] = (byte)((fdaOutFrameData.LazerFireIndex[i] + 1) % ProcessLazers<TFrameData, TConstData, TSettingsData>.LazersPerPeer(sdaSettingsData));

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
                        fdaOutFrameData.TimeUntilLaserReset[i] = sdaSettingsData.TimeBetweenShots;
                    }


                    //check if target in range to hit
                    if (fdaOutFrameData.TimeUntilLaserReset[i] < Fix.Zero && fdaOutFrameData.TimeUntilLaserFire[i] < Fix.Zero)
                    {
                        bool bShouldFire = false;

                        //loop through all ships excluding current ship and check if they are in range to hit
                        for (int j = 0; j < fdaOutFrameData.ShipPositionX.Length; j++)
                        {
                            //skip detection against self
                            if (j == i)
                            {
                                continue;
                            }

                            //check if target ship is alive
                            if (fdaOutFrameData.ShipHealth[j] > Fix.Zero)
                            {
                                //get dist sqr to target
                                Fix fixDeltaX = fdaOutFrameData.ShipPositionX[j] - fdaOutFrameData.ShipPositionX[i];
                                Fix fixDeltaY = fdaOutFrameData.ShipPositionY[j] - fdaOutFrameData.ShipPositionY[i];

                                Fix fixDistSqr = (fixDeltaX * fixDeltaX) + (fixDeltaY * fixDeltaY);

                                //check if target is in range 
                                if (fixDistSqr < sdaSettingsData.AutoFireConeRangeSqr && fixDistSqr > Fix.Zero)
                                {
                                    //get direction to target
                                    Fix fixDirectionToTarget = FixMath.Atan2(fixDeltaY, fixDeltaX) % 360;

                                    Fix fixShipAngle = fdaOutFrameData.ShipBaseAngle[i] % 360;

                                    //get difference in direction
                                    Fix fixAngleDifference = ((FixMath.Abs(fixDirectionToTarget - fixShipAngle) + 180) % 360) - 180;

                                    //Fix fixAngleDifference = FixMath.Min(FixMath.Abs(fixDirectionToTarget - fdaOutFrameData.ShipBaseAngle[i]), FixMath.Abs(((fixDirectionToTarget + 180) % 360) - ((fdaOutFrameData.ShipBaseAngle[i] + 180) % 360)));

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

                        //if there is a viable target trigger shot attack charge
                        if (bShouldFire)
                        {
                            fdaOutFrameData.TimeUntilLaserFire[i] = sdaSettingsData.ShotsChargeTime;
                        }
                    }
                }
            }
            return true;
        }
    }
}
