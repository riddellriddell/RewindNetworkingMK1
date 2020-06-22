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
        where TSettingsData : IPeerSlotAssignmentSettingsData, IShipMovementSettingsData, ISimTickRateSettings
        where TConstData : IShipRespawnConstData
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

                        if (IsShipOutOfBounds(fdaOutFrameData.ShipPositionX[i], fdaOutFrameData.ShipPositionY[i], cdaConstantData))
                        {
                            fixTurn = ReturnToGameSpaceTurn(fdaOutFrameData.ShipPositionX[i], fdaOutFrameData.ShipPositionY[i], fdaOutFrameData.ShipBaseAngle[i], sdaSettingsData);
                        }
                        else
                        {
                            if (SimInputManager.GetTurnLeft(fdaOutFrameData.PeerInput[i]))
                            {
                                fixTurn = fixTurn + sdaSettingsData.ShipTurnRate * sdaSettingsData.SecondsPerTick;
                            }

                            if (SimInputManager.GetTurnRight(fdaOutFrameData.PeerInput[i]))
                            {
                                fixTurn = fixTurn - sdaSettingsData.ShipTurnRate * sdaSettingsData.SecondsPerTick;
                            }
                        }

                        //update ship rotation 
                        fdaOutFrameData.ShipBaseAngle[i] = (fdaOutFrameData.ShipBaseAngle[i] + 360 + fixTurn) % 360;

                        //calc existing speed 
                        Fix fixShipSpeed = FixMathArrayVectorHelperFunctions.Magnitude(fdaOutFrameData.ShipVelocityX[i], fdaOutFrameData.ShipVelocityY[i]);

                        //check if existing speed is too slow
                        if (fixShipSpeed < sdaSettingsData.ShipSpeed)
                        {
                            fixShipSpeed = fixShipSpeed + sdaSettingsData.ShipAcceleration * sdaSettingsData.SecondsPerTick;
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
                        fdaOutFrameData.ShipPositionX[i] = fdaOutFrameData.ShipPositionX[i] + (fdaOutFrameData.ShipVelocityX[i] * sdaSettingsData.SecondsPerTick);
                        fdaOutFrameData.ShipPositionY[i] = fdaOutFrameData.ShipPositionY[i] + (fdaOutFrameData.ShipVelocityY[i] * sdaSettingsData.SecondsPerTick);
                    }
                }
            }

            return true;
        }

        public bool IsShipOutOfBounds(Fix fixShipPosX, Fix fixShipPosY, TConstData cdaConstData)
        {
            Fix fixDistSqr = FixMath.Abs((fixShipPosX * fixShipPosX * Fix.Ratio(1, 1000)) + (fixShipPosY * fixShipPosY * Fix.Ratio(1,1000)));

            if(fixDistSqr > (cdaConstData.SpawnRadius * cdaConstData.SpawnRadius * Fix.Ratio(1, 1000)))
            {
                return true;
            }

            return false;
        }

        public Fix ReturnToGameSpaceTurn(Fix fixShipPosX, Fix fixShipPosY, Fix fixShipBaseAngle, TSettingsData sdaSettingsData)
        {
            //calculate angle back to world center 
            Fix fixAngleToCenter = FixMath.Atan2(fixShipPosY, fixShipPosX);

            Fix fixAngleDifference = (((fixShipBaseAngle - fixAngleToCenter) +180) % 360) - 180;

            Fix fixMaxTurnAmount = sdaSettingsData.ShipTurnRate * sdaSettingsData.SecondsPerTick;

            return FixMath.Max(FixMath.Min(fixMaxTurnAmount, fixAngleDifference), -fixMaxTurnAmount);
        }
    }
}
