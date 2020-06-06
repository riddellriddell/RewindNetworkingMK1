using FixedPointy;
using Utility;

namespace Sim
{
    public interface IShipMovementSettingsData
    {
        Fix ShipSpeed { get; }

        Fix ShipAcceleration { get; }

        Fix ShipTurnRate { get; }
    }

    public class ProcessShipMovement<TFrameData, TConstData, TSettingsData> :
              ISimProcess<TFrameData, TConstData, TSettingsData>
        where TFrameData : IShipPositions, IPeerSlotAssignmentFrameData, IShipHealthframeData, IPeerInputFrameData, IFrameData, new()
        where TSettingsData : IPeerSlotAssignmentSettingsData, IShipMovementSettingsData
    {
        public int Priority { get; } = 4;

        public string ProcessName { get; } = "Ship Movement";

        public bool ProcessFrameData(uint iTick, in TSettingsData sdaSettingsData, in TConstData cdaConstantData, in TFrameData fdaInFrameData, in object[] objInputs, ref TFrameData fdaOutFrameData)
        {
            for (int i = 0; i < sdaSettingsData.MaxPlayers; i++)
            {
                //check if player is in game 
                if (fdaOutFrameData.PeerSlotAssignment[i] != long.MinValue)
                {
                    //check if ship is dead
                    if (fdaOutFrameData.ShipHealth[i] > Fix.Zero)
                    {
                        //calc rotation angle 
                        Fix fixTurn = Fix.Zero;
                        if (SimInputManager.GetTurnLeft(fdaOutFrameData.PeerInput[i]))
                        {
                            fixTurn = fixTurn + sdaSettingsData.ShipTurnRate * TestingSimManager<TFrameData, TConstData, TSettingsData>.s_fixSecondsPerTick;
                        }

                        if (SimInputManager.GetTurnRight(fdaOutFrameData.PeerInput[i]))
                        {
                            fixTurn = fixTurn - sdaSettingsData.ShipTurnRate * TestingSimManager<TFrameData, TConstData, TSettingsData>.s_fixSecondsPerTick;
                        }

                        //update ship rotation 
                        fdaOutFrameData.ShipBaseAngle[i] = (fdaOutFrameData.ShipBaseAngle[i] + 360 + fixTurn) % 360;

                        //calc existing speed 
                        Fix fixShipSpeed = FixMathArrayVectorHelperFunctions.Magnitude(fdaOutFrameData.ShipVelocityX[i], fdaOutFrameData.ShipVelocityY[i]);

                        //check if existing speed is too slow
                        if (fixShipSpeed < sdaSettingsData.ShipSpeed)
                        {
                            fixShipSpeed = fixShipSpeed + sdaSettingsData.ShipAcceleration * TestingSimManager<TFrameData, TConstData, TSettingsData>.s_fixSecondsPerTick;
                        }

                        //check if ship is too fast
                        if (fixShipSpeed > sdaSettingsData.ShipSpeed)
                        {
                            fixShipSpeed = sdaSettingsData.ShipSpeed;
                        }

                        //set ship new velocity 
                        fdaOutFrameData.ShipVelocityX[i] = FixMath.Cos(fdaOutFrameData.ShipBaseAngle[i]) * fixShipSpeed;
                        fdaOutFrameData.ShipVelocityY[i] = FixMath.Sin(fdaOutFrameData.ShipBaseAngle[i]) * fixShipSpeed;

                        //move ship to new position
                        fdaOutFrameData.ShipPositionX[i] = fdaOutFrameData.ShipPositionX[i] + fdaOutFrameData.ShipVelocityX[i];
                        fdaOutFrameData.ShipPositionY[i] = fdaOutFrameData.ShipPositionY[i] + fdaOutFrameData.ShipVelocityY[i];
                    }
                }
            }

            return true;
        }
    }
}
