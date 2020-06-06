using FixedPointy;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Utility;

namespace Sim
{
    public interface IShipRespawnFrameData
    {
        Fix[] TimeUntilRespawn { get; set;}
    }

    public interface IShipRespawnConstData
    {
        Fix SpawnRadius { get; }
    }

    public interface IShipRespawnSettingsData
    {
        Fix ShipRespawnTime { get; }
    }

    public class ProcessShipSpawn<TFrameData, TConstData, TSettingsData> : 
        ISimProcess<TFrameData, TConstData, TSettingsData>, 
        ISimSetupProcesses<TFrameData,TSettingsData>  
        where TFrameData: IPeerSlotAssignmentFrameData, IShipRespawnFrameData, IShipHealthframeData, IShipPositions, IFrameData, new()
        where TSettingsData : IPeerSlotAssignmentSettingsData, IShipRespawnSettingsData, IShipHealthSettingsData
        where TConstData : IShipRespawnConstData
    {
        public int Priority { get; } = 2;

        public string ProcessName { get; } = "RespawnDeadShips";

        public bool ApplySetupProcess(uint iTick, in TSettingsData sdaSettingsData, long lFirstPeerID, ref TFrameData fdaFrameData)
        {
            fdaFrameData.TimeUntilRespawn = new Fix[sdaSettingsData.MaxPlayers];

            for(int i = 0; i < sdaSettingsData.MaxPlayers; i++)
            {
                fdaFrameData.TimeUntilRespawn[i] = sdaSettingsData.ShipRespawnTime;
            }

            return true;
        }

        public bool ProcessFrameData(uint iTick, in TSettingsData sdaSettingsData, in TConstData cdaConstantData, in TFrameData fdaInFrameData, in object[] objInputs, ref TFrameData fdaOutFrameData)
        {
            for(int i = 0; i < fdaOutFrameData.ShipHealth.Length; i++)
            {
                //check if player is active in game 
                if(fdaOutFrameData.PeerSlotAssignment[i] == long.MinValue)
                {
                    fdaOutFrameData.TimeUntilRespawn[i] = sdaSettingsData.ShipRespawnTime;
                }
                //check if ship is dead
                else if (fdaOutFrameData.ShipHealth[i] <= Fix.Zero)
                {
                    //count down respawn timer 
                    fdaOutFrameData.TimeUntilRespawn[i] = fdaOutFrameData.TimeUntilRespawn[i] - TestingSimManager<TFrameData, TConstData, TSettingsData>.s_fixSecondsPerTick;

                    //check if the player can respawn 
                    if(fdaOutFrameData.TimeUntilRespawn[i] < Fix.Zero)
                    {
                        //respawn the ship

                        //reset ship health
                        ProcessShipHealth<TFrameData, TConstData, TSettingsData>.OnShipRespawn(fdaOutFrameData, sdaSettingsData, i);

                        //reset ship respawn time
                        fdaOutFrameData.TimeUntilRespawn[i] = sdaSettingsData.ShipRespawnTime;

                        long lSeed = ((long.MinValue + iTick) << 8) + i;

                        lSeed = unchecked( lSeed * lSeed * lSeed);

                        //create deterministic random number generator
                        DeterministicRandomNumberGenerator drgRandomNumberGenerator = new DeterministicRandomNumberGenerator(lSeed);

                        //pick random location on boundry of world 
                        Fix fixRandomDirection = drgRandomNumberGenerator.GetRandomFix(Fix.Zero, (Fix)360);

                        //move ship to spawn location and face the center of the world
                        fdaOutFrameData.ShipPositionX[i] = FixMath.Cos(fixRandomDirection) * cdaConstantData.SpawnRadius;
                        fdaOutFrameData.ShipPositionY[i] = FixMath.Sin(fixRandomDirection) * cdaConstantData.SpawnRadius;
                        fdaOutFrameData.ShipBaseAngle[i] = (fixRandomDirection + (Fix)180) % (Fix)360;
                        fdaOutFrameData.ShipVelocityX[i] = Fix.Zero;
                        fdaOutFrameData.ShipVelocityY[i] = Fix.Zero;
                    }
                }
            }

            return true;
        }
    }
}
