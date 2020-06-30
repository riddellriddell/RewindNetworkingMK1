using FixedPointy;

namespace Sim
{
    public interface IShipCollisionSettingsData
    {
        Fix ShipRestitution { get; }
        Fix ShipFriction { get; }
        Fix ShipSize { get; }
        Fix ShipInverseMass { get; }
        Fix ShipImpactDamage { get; }
    }

    public class ProcessShipShipCollisions<TFrameData, TConstData, TSettingsData> :
              ISimProcess<TFrameData, TConstData, TSettingsData>
        where TFrameData : IShipPositions, IPeerSlotAssignmentFrameData, IShipHealthframeData, IPeerInputFrameData, IFrameData, new()
        where TSettingsData : IPeerSlotAssignmentSettingsData, IShipMovementSettingsData, ISimTickRateSettings, IShipCollisionSettingsData, IShipHealthSettingsData
        where TConstData : IShipRespawnConstData
    {
        public int Priority => 5;

        public string ProcessName => "Process ship ship collisions";

        public bool ProcessFrameData(uint iTick, in TSettingsData sdaSettingsData, in TConstData cdaConstantData, in TFrameData fdaInFrameData, in object[] objInputs, ref TFrameData fdaOutFrameData)
        {
            CollisionDetectionHelper<TFrameData, TSettingsData>.DetectCollision(fdaOutFrameData.ShipPositionX, fdaOutFrameData.ShipPositionY, sdaSettingsData.ShipSize, fdaOutFrameData, sdaSettingsData, FilterDeadShips, ShipShipCollisionResponse);

            return true;
        }

        public bool FilterDeadShips(TFrameData fdaFrameData, int iIndex)
        {
            if (fdaFrameData.ShipHealth[iIndex] > Fix.Zero)
            {
                return true;
            }

            return false;
        }

        public void ShipShipCollisionResponse(TFrameData fdaFrameData, TSettingsData sdaSettingsData, int iObjectA, int iObjectB, Fix fixObjectRadius, Fix fixPosDeltaSqr, Fix fixPosDeltaX, Fix fixPosDeltaY)
        {
            // get untangle dist 
            CollisionDetectionHelper<TFrameData, TSettingsData>.CalcCollisionNormal(fixPosDeltaSqr, fixPosDeltaX, fixPosDeltaY, out Fix fixNormalX, out Fix fixNormalY);

            Fix fixSepperationRatio = (Fix.Ratio(1, 2) * fixObjectRadius) + Fix.Ratio(1, 100);

            Fix fixSepX = (fixNormalX - fixPosDeltaX) * fixSepperationRatio;
            Fix fixSepY = (fixNormalY - fixPosDeltaY) * fixSepperationRatio;

            //move ships appart
            fdaFrameData.ShipPositionX[iObjectA] = fdaFrameData.ShipPositionX[iObjectA] - fixSepX;
            fdaFrameData.ShipPositionY[iObjectA] = fdaFrameData.ShipPositionY[iObjectA] - fixSepY;

            fdaFrameData.ShipPositionX[iObjectB] = fdaFrameData.ShipPositionX[iObjectB] + fixSepX;
            fdaFrameData.ShipPositionY[iObjectB] = fdaFrameData.ShipPositionY[iObjectB] + fixSepY;

            //calculate velocity delta
            Fix fixVelocityDeltaX = fdaFrameData.ShipVelocityX[iObjectA] - fdaFrameData.ShipVelocityX[iObjectB];
            Fix fixVelocityDeltaY = fdaFrameData.ShipVelocityY[iObjectA] - fdaFrameData.ShipVelocityY[iObjectB];

            bool bCollisionResolutionNeeded = CollisionDetectionHelper<TFrameData, TSettingsData>.CollisionResolutionVelocities(
                fixNormalX,
                fixNormalY,
                fixVelocityDeltaX,
                fixVelocityDeltaY,
                sdaSettingsData.ShipInverseMass,
                sdaSettingsData.ShipInverseMass,
                sdaSettingsData.ShipRestitution,
                sdaSettingsData.ShipFriction,
                out Fix fixOutImpulseAX,
                out Fix fixOutImpulseAY,
                out Fix fixOutImpulseBX,
                out Fix fixOutImpulseBY);

            // if collision has already been handled and the objects are headed away from each other dont 
            // apply impulse
            if (bCollisionResolutionNeeded)
            {
                fdaFrameData.ShipVelocityX[iObjectA] = fdaFrameData.ShipVelocityX[iObjectA] - fixOutImpulseAX;
                fdaFrameData.ShipVelocityY[iObjectA] = fdaFrameData.ShipVelocityY[iObjectA] - fixOutImpulseAY;

                fdaFrameData.ShipVelocityX[iObjectB] = fdaFrameData.ShipVelocityX[iObjectB] + fixOutImpulseBX;
                fdaFrameData.ShipVelocityY[iObjectB] = fdaFrameData.ShipVelocityY[iObjectB] + fixOutImpulseBY;

                //point ships in correct direction

                //check for null travel vectors 
                if (fdaFrameData.ShipVelocityX[iObjectA] != 0 || fdaFrameData.ShipVelocityY[iObjectA] != 0)
                {
                    //point ship in new travel direction 
                    fdaFrameData.ShipBaseAngle[iObjectA] = FixMath.Atan2(fdaFrameData.ShipVelocityY[iObjectA], fdaFrameData.ShipVelocityX[iObjectA]);
                }

                if (fdaFrameData.ShipVelocityX[iObjectB] != 0 || fdaFrameData.ShipVelocityY[iObjectB] != 0)
                {
                    //point ship in new travel direction 
                    fdaFrameData.ShipBaseAngle[iObjectB] = FixMath.Atan2(fdaFrameData.ShipVelocityY[iObjectB], fdaFrameData.ShipVelocityX[iObjectB]);
                }

                //deal damage to both ships
                ProcessShipHealth<TFrameData, TConstData, TSettingsData>.DamageShip(fdaFrameData, sdaSettingsData, iObjectA, sdaSettingsData.ShipImpactDamage, (byte)iObjectB);
                ProcessShipHealth<TFrameData, TConstData, TSettingsData>.DamageShip(fdaFrameData, sdaSettingsData, iObjectB, sdaSettingsData.ShipImpactDamage, (byte)iObjectA);
            }
        }
    }
}
