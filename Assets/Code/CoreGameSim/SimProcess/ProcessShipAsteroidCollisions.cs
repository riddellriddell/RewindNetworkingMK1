using FixedPointy;
using SharedTypes;

namespace Sim
{
    //TODO: move to a generic folder 
    public interface IAsteroidCollisionConstData
    {
        Fix[] AsteroidPositionX { get; }
        Fix[] AsteroidPositionY { get; }
        Fix[] AsteroidSize { get; }
    }

    public class ProcessShipAsteroidCollisions<TFrameData, TConstData, TSettingsData> :
              ISimProcess<TFrameData, TConstData, TSettingsData>
        where TFrameData : IShipPositions, IPeerSlotAssignmentFrameData, IShipHealthframeData, IPeerInputFrameData, IFrameData, new()
        where TSettingsData : IPeerSlotAssignmentSettingsData, IShipMovementSettingsData, ISimTickRateSettings, IShipCollisionSettingsData, IShipHealthSettingsData
        where TConstData : IShipRespawnConstData, IAsteroidCollisionConstData
    {
        public int Priority => 6;

        public string ProcessName => "Process ship ship collisions";

        public bool ProcessFrameData(uint iTick, in TSettingsData sdaSettingsData, in TConstData cdaConstantData, in TFrameData fdaInFrameData, in IInput[] objInputs, ref TFrameData fdaOutFrameData)
        {
            CollisionDetectionHelper<TFrameData, TSettingsData>.DetectCollision(
                cdaConstantData.AsteroidPositionX, 
                cdaConstantData.AsteroidPositionY, 
                fdaOutFrameData.ShipPositionX, 
                fdaOutFrameData.ShipPositionY, 
                cdaConstantData.AsteroidSize, 
                sdaSettingsData.ShipSize, 
                fdaOutFrameData, 
                sdaSettingsData, 
                FilterDeadShips, 
                ShipShipCollisionResponse);

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

            Fix fixSepperationRatio = fixObjectRadius + Fix.Ratio(1, 100);

            Fix fixSepX = (fixNormalX - fixPosDeltaX) * fixSepperationRatio;
            Fix fixSepY = (fixNormalY - fixPosDeltaY) * fixSepperationRatio;

            //move ship appart from asteroid
            fdaFrameData.ShipPositionX[iObjectB] = fdaFrameData.ShipPositionX[iObjectB] + fixSepX;
            fdaFrameData.ShipPositionY[iObjectB] = fdaFrameData.ShipPositionY[iObjectB] + fixSepY;

            //calculate velocity delta
            Fix fixVelocityDeltaX = - fdaFrameData.ShipVelocityX[iObjectB];
            Fix fixVelocityDeltaY = - fdaFrameData.ShipVelocityY[iObjectB];

            bool bCollisionResolutionNeeded = CollisionDetectionHelper<TFrameData, TSettingsData>.CollisionResolutionVelocities(
                fixNormalX,
                fixNormalY,
                fixVelocityDeltaX,
                fixVelocityDeltaY,
                Fix.Zero,
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
                fdaFrameData.ShipVelocityX[iObjectB] = fdaFrameData.ShipVelocityX[iObjectB] + fixOutImpulseBX;
                fdaFrameData.ShipVelocityY[iObjectB] = fdaFrameData.ShipVelocityY[iObjectB] + fixOutImpulseBY;

                //point ships in correct direction

                if (fdaFrameData.ShipVelocityX[iObjectB] != 0 || fdaFrameData.ShipVelocityY[iObjectB] != 0)
                {
                    //point ship in new travel direction 
                    fdaFrameData.ShipBaseAngle[iObjectB] = FixMath.Atan2(fdaFrameData.ShipVelocityY[iObjectB], fdaFrameData.ShipVelocityX[iObjectB]);
                }

                //deal damage to ships
                ProcessShipHealth<TFrameData, TConstData, TSettingsData>.DamageShip(fdaFrameData,sdaSettingsData, iObjectB, sdaSettingsData.ShipImpactDamage);
            }
        }
    }
}
