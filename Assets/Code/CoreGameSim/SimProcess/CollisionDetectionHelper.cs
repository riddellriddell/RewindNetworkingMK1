using FixedPointy;
using System;

namespace Sim
{
    public static class CollisionDetectionHelper<TFrameData, TSettingsData>
    {
        //Simple collision detection 
        // when fncFilter returns false item is not checked for collision
        // action variables , index of collision item a, index of collision item b, Radius of objects, dist squre root , pos delta x, pos delta y 
        public static void DetectCollision(Fix[] fixObjectX, Fix[] fixObjectY, Fix fixObjectRadius, TFrameData fdaFrameData, TSettingsData sdaSettingsData, Func<TFrameData, int, bool> fncFilter, Action<TFrameData, TSettingsData, int, int, Fix, Fix, Fix, Fix> actCollisionResolution)
        {
            Fix fixDoubleObjectRadiusInverse = Fix.One / (fixObjectRadius + fixObjectRadius);

            for (int i = 0; i < fixObjectX.Length; i++)
            {
                if (fncFilter(fdaFrameData, i))
                {
                    for (int j = i + 1; j < fixObjectX.Length; j++)
                    {
                        if (fncFilter(fdaFrameData, j))
                        {
                            Fix fixDeltaX = (fixObjectX[j] - fixObjectX[i]) * fixDoubleObjectRadiusInverse;
                            Fix fixDeltaY = (fixObjectY[j] - fixObjectY[i]) * fixDoubleObjectRadiusInverse;
                            Fix fixDistSqr = (fixDeltaX * fixDeltaX) + (fixDeltaY * fixDeltaY);

                            if (fixDistSqr < Fix.One && fixDistSqr > Fix.Zero)
                            {
                                // perform action on colission detection
                                actCollisionResolution(fdaFrameData, sdaSettingsData, i, j, fixObjectRadius, fixDistSqr, fixDeltaX, fixDeltaY);
                            }
                        }
                    }
                }
            }
        }

        public static void DetectCollision(Fix[] fixObjectAX, Fix[] fixObjectAY, Fix[] fixObjectBX, Fix[] fixObjectBY, Fix fixObjectARadius, Fix fixObjectBRadius, TFrameData fdaFrameData, TSettingsData sdaSettingsData, Func<TFrameData, int, bool> fncFilter, Action<TFrameData, TSettingsData, int, int, Fix, Fix, Fix, Fix> actCollisionResolution)
        {
            Fix fixCombinedObjectRadius = fixObjectARadius + fixObjectBRadius;
            Fix fixDoubleObjectRadiusInverse = Fix.One / fixCombinedObjectRadius;

            for (int i = 0; i < fixObjectAX.Length; i++)
            {
                if (fncFilter(fdaFrameData, i))
                {
                    for (int j = 0; j < fixObjectBX.Length; j++)
                    {
                        if (fncFilter(fdaFrameData, j))
                        {
                            Fix fixDeltaX = (fixObjectBX[j] - fixObjectAX[i]) * fixDoubleObjectRadiusInverse;
                            Fix fixDeltaY = (fixObjectBY[j] - fixObjectAY[i]) * fixDoubleObjectRadiusInverse;
                            Fix fixDistSqr = (fixDeltaX * fixDeltaX) + (fixDeltaY * fixDeltaY);

                            if (fixDistSqr < Fix.One && fixDistSqr > Fix.Zero)
                            {
                                // perform action on colission detection
                                actCollisionResolution(fdaFrameData, sdaSettingsData, i, j, fixCombinedObjectRadius, fixDistSqr, fixDeltaX, fixDeltaY);
                            }
                        }
                    }
                }
            }
        }

        public static void DetectCollision(Fix[] fixObjectAX, Fix[] fixObjectAY, Fix[] fixObjectBX, Fix[] fixObjectBY, Fix[] fixObjectARadius, Fix fixObjectBRadius, TFrameData fdaFrameData, TSettingsData sdaSettingsData, Func<TFrameData, int, bool> fncFilter, Action<TFrameData, TSettingsData, int, int, Fix, Fix, Fix, Fix> actCollisionResolution)
        {
            for (int i = 0; i < fixObjectAX.Length; i++)
            {
                Fix fixCombinedObjectRadius = fixObjectARadius[i] + fixObjectBRadius;
                Fix fixDoubleObjectRadiusInverse = Fix.One / fixCombinedObjectRadius;

                for (int j = 0; j < fixObjectBX.Length; j++)
                {
                    if (fncFilter(fdaFrameData, j))
                    {
                        Fix fixDeltaX = (fixObjectBX[j] - fixObjectAX[i]) * fixDoubleObjectRadiusInverse;
                        Fix fixDeltaY = (fixObjectBY[j] - fixObjectAY[i]) * fixDoubleObjectRadiusInverse;
                        Fix fixDistSqr = (fixDeltaX * fixDeltaX) + (fixDeltaY * fixDeltaY);

                        if (fixDistSqr < Fix.One && fixDistSqr > Fix.Zero)
                        {
                            // perform action on colission detection
                            actCollisionResolution(fdaFrameData, sdaSettingsData, i, j, fixCombinedObjectRadius, fixDistSqr, fixDeltaX, fixDeltaY);
                        }
                    }
                }

            }
        }

        public static void CalcCollisionNormal(Fix fixDistSqrt, Fix fixDeltaX, Fix fixDeltaY, out Fix fixNormalX, out Fix fixNormalY)
        {
            //work out how far appart they are 
            Fix fixDist = FixMath.Sqrt(fixDistSqrt);
            //get normalization value
            Fix fixNormalization = Fix.One / fixDist;

            // get the distance needed to sepperate the objects
            fixNormalX = fixDeltaX * fixNormalization;
            fixNormalY = fixDeltaY * fixNormalization;
        }

        public static bool CollisionResolutionVelocities(
            Fix fixNormalX,
            Fix fixNormalY,
            Fix fixVelocityDeltaX,
            Fix fixVelocityDeltaY,
            Fix fixInverseMassA,
            Fix fixInverseMassB,
            Fix fixRestitution,
            Fix fixFriction,
            out Fix fixOutImpulseAX,
            out Fix fixOutImpulseAY,
            out Fix fixOutImpulseBX,
           out Fix fixOutImpulseBY
           )
        {
            //get velocity along normal
            Fix fixNormalVelocity = (fixVelocityDeltaX * fixNormalX) + (fixVelocityDeltaY * fixNormalY);

            if (fixNormalVelocity < Fix.Zero)
            {
                fixOutImpulseAX = Fix.Zero;
                fixOutImpulseAY = Fix.Zero;
                fixOutImpulseBX = Fix.Zero;
                fixOutImpulseBY = Fix.Zero;

                return false;
            }

            //apply restitution 
            fixNormalVelocity = fixNormalVelocity * fixRestitution;

            //get velocity along tangent 
            Fix fixTangentVelocity = (fixVelocityDeltaX * fixNormalY) + (fixVelocityDeltaY * -fixNormalX);

            //apply friction 
            fixTangentVelocity = fixTangentVelocity * fixFriction;

            //get impuse normal

            //assign impulse based on mass difference
            Fix fixBounceImpulse = fixNormalVelocity / (fixInverseMassA + fixInverseMassB);
            Fix fixFrictionImpulse = Fix.Zero; //fixTangentVelocity / (fixInverseMassA + fixInverseMassB);

            fixOutImpulseAX = ((fixNormalX * fixBounceImpulse) + (fixNormalY * fixFrictionImpulse)) * fixInverseMassA;
            fixOutImpulseAY = ((fixNormalY * fixBounceImpulse) + (-fixNormalX * fixFrictionImpulse)) * fixInverseMassA;

            fixOutImpulseBX = ((fixNormalX * fixBounceImpulse) + (fixNormalY * fixFrictionImpulse)) * fixInverseMassB;
            fixOutImpulseBY = ((fixNormalY * fixBounceImpulse) + (-fixNormalX * fixFrictionImpulse)) * fixInverseMassB;

            return true;

        }
    }
}