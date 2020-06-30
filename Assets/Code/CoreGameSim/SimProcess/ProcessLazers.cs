using FixedPointy;
using UnityEngine;

namespace Sim
{
    public interface ILazerFrameData
    {
        byte[] LazerOwner { get; set; }

        Fix[] LazerLifeRemaining { get; set; }

        Fix[] LazerPositionX { get; set; }

        Fix[] LazerPositionY { get; set; }

        Fix[] LazerVelocityX { get; set; }

        Fix[] LazerVelocityY { get; set; }
    }

    public interface ILazerSettingsData
    {
        Fix LazerLife { get; }

        Fix LazerSpeed { get; }

        Fix LazerDamage { get; }

        Fix LazerSize { get; }
    }


    public class ProcessLazers<TFrameData, TConstData, TSettingsData> :
            ISimProcess<TFrameData, TConstData, TSettingsData>,
            ISimSetupProcesses<TFrameData, TSettingsData>
            where TFrameData : IPeerSlotAssignmentFrameData, ILazerFrameData, IShipHealthframeData, IShipPositions, IFrameData, new()
            where TSettingsData : IPeerSlotAssignmentSettingsData, IShipWeaponsSettingsData, IShipHealthSettingsData, IShipCollisionSettingsData, ISimTickRateSettings, ILazerSettingsData
            where TConstData : IAsteroidCollisionConstData
    {
        public static int LazersPerPeer(TSettingsData sdaSettingsData)
        {
            return Mathf.Max((int)((Fix.One / sdaSettingsData.TimeBetweenShots) * sdaSettingsData.LazerLife), 1);
        }

        public static void FireLazer(
            TFrameData fdaFrameData,
            TSettingsData sdaSettingsData,
            int iPeerIndex,
            int iLazerSubIndex,
            Fix fixFirePosX,
            Fix fixFirePosY,
            Fix fixFireAngle
            )
        {
            int iLazersPerPeer = LazersPerPeer(sdaSettingsData);

            int iLazerIndex = (iLazersPerPeer * iPeerIndex) + iLazerSubIndex;

            fdaFrameData.LazerLifeRemaining[iLazerIndex] = sdaSettingsData.LazerLife;

            fdaFrameData.LazerPositionX[iLazerIndex] = fixFirePosX;
            fdaFrameData.LazerPositionY[iLazerIndex] = fixFirePosY;

            fdaFrameData.LazerVelocityX[iLazerIndex] = FixMath.Cos(fixFireAngle) * sdaSettingsData.LazerSpeed;
            fdaFrameData.LazerVelocityY[iLazerIndex] = FixMath.Sin(fixFireAngle) * sdaSettingsData.LazerSpeed;

        }


        public int Priority => 11;

        public string ProcessName => "Process Lazer Behaviour";

        public bool ApplySetupProcess(uint iTick, in TSettingsData sdaSettingsData, long lFirstPeerID, ref TFrameData fdaFrameData)
        {
            int iTotalLazersNeededNeededPerPlayer = LazersPerPeer(sdaSettingsData);

            fdaFrameData.LazerOwner = new byte[iTotalLazersNeededNeededPerPlayer * sdaSettingsData.MaxPlayers];

            for (int i = 0; i < fdaFrameData.LazerOwner.Length; i++)
            {
                fdaFrameData.LazerOwner[i] = (byte)(i / iTotalLazersNeededNeededPerPlayer);
            }

            fdaFrameData.LazerLifeRemaining = new Fix[fdaFrameData.LazerOwner.Length];

            fdaFrameData.LazerPositionX = new Fix[fdaFrameData.LazerOwner.Length];

            fdaFrameData.LazerPositionY = new Fix[fdaFrameData.LazerOwner.Length];

            fdaFrameData.LazerVelocityX = new Fix[fdaFrameData.LazerOwner.Length];

            fdaFrameData.LazerVelocityY = new Fix[fdaFrameData.LazerOwner.Length];

            return true;
        }

        public bool ProcessFrameData(uint iTick, in TSettingsData sdaSettingsData, in TConstData cdaConstantData, in TFrameData fdaInFrameData, in object[] objInputs, ref TFrameData fdaOutFrameData)
        {
            for (int i = 0; i < fdaOutFrameData.LazerLifeRemaining.Length; i++)
            {
                //check if lazer is alive 
                if (fdaOutFrameData.LazerLifeRemaining[i] > Fix.Zero)
                {
                    //reduce life 
                    fdaOutFrameData.LazerLifeRemaining[i] = fdaOutFrameData.LazerLifeRemaining[i] - sdaSettingsData.SecondsPerTick;
                }
            }

            //move all projectiles along their travel paths
            for (int i = 0; i < fdaOutFrameData.LazerPositionX.Length; i++)
            {
                fdaOutFrameData.LazerPositionX[i] = fdaOutFrameData.LazerPositionX[i] + (fdaOutFrameData.LazerVelocityX[i] * sdaSettingsData.SecondsPerTick);
            }

            for (int i = 0; i < fdaOutFrameData.LazerPositionY.Length; i++)
            {
                fdaOutFrameData.LazerPositionY[i] = fdaOutFrameData.LazerPositionY[i] + (fdaOutFrameData.LazerVelocityY[i] * sdaSettingsData.SecondsPerTick);
            }

            //perform collision detection between lazers and asteroids 
            CollisionDetectionHelper<TFrameData, TSettingsData>.DetectCollision(
                cdaConstantData.AsteroidPositionX,
                cdaConstantData.AsteroidPositionY,
                fdaOutFrameData.LazerPositionX,
                fdaOutFrameData.LazerPositionY,
                cdaConstantData.AsteroidSize,
                sdaSettingsData.LazerSize,
                fdaOutFrameData,
                sdaSettingsData,
                ActiveLazerFilter,
                OnLazerCollideWithAsteroid);

            //perform collision detection between lazers and ships 
            CollisionDetectionHelper<TFrameData, TSettingsData>.DetectCollision(
                fdaOutFrameData.ShipPositionX,
                fdaOutFrameData.ShipPositionY,
                fdaOutFrameData.LazerPositionX,
                fdaOutFrameData.LazerPositionY,
                sdaSettingsData.ShipSize,
                sdaSettingsData.LazerSize,
                fdaOutFrameData,
                sdaSettingsData,
                DeadShipFilter,
                ActiveLazerFilter,
                OnLazerCollideWithShip);

            return true;
        }

        public bool ActiveLazerFilter(TFrameData fdaFrameData, int iLazerIndex)
        {
            if (fdaFrameData.LazerLifeRemaining[iLazerIndex] > Fix.Zero)
            {
                return true;
            }

            return false;
        }

        public bool DeadShipFilter(TFrameData fdaFrameData, int iShipIndex)
        {
            if (fdaFrameData.ShipHealth[iShipIndex] > Fix.Zero)
            {
                return true;
            }

            return false;
        }


        public void OnLazerCollideWithAsteroid(
            TFrameData fdaFrameData,
            TSettingsData sdaSettingsData,
            int iAsteroidIndex,
            int jLazerIndex,
            Fix fixCombinedObjectRadius,
            Fix fixDistSqr,
            Fix fixDeltaX,
            Fix fixDeltaY)
        {
            fdaFrameData.LazerLifeRemaining[jLazerIndex] = Fix.Zero;
        }

        public void OnLazerCollideWithShip(
            TFrameData fdaFrameData,
            TSettingsData sdaSettingsData,
            int iShipIndex,
            int jLazerIndex,
            Fix fixCombinedObjectRadius,
            Fix fixDistSqr,
            Fix fixDeltaX,
            Fix fixDeltaY)
        {
            //check if ship is lazer owner
            if (fdaFrameData.LazerOwner[jLazerIndex] == iShipIndex)
            {
                //dont collide with own bullets
                return;
            }

            //remove lazer
            fdaFrameData.LazerLifeRemaining[jLazerIndex] = Fix.Zero;

            //apply damage
            ProcessShipHealth<TFrameData, TConstData, TSettingsData>.DamageShip(fdaFrameData, sdaSettingsData, iShipIndex, sdaSettingsData.LazerDamage, fdaFrameData.LazerOwner[jLazerIndex]);
        }


    }
}