using FixedPointy;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Utility;

namespace Sim
{
    public class ConstData: IShipRespawnConstData, IAsteroidCollisionConstData
    {
        #region IAsteroidCollisionConstData
        public Fix[] m_fixAsteroidPositionX = //{ };
        {
            Fix.Mix(3,5,10),
            Fix.Mix(-3,0,10),
            Fix.Mix(2,1,10),
            Fix.Mix(-3,0,10),
            Fix.Mix(-4,0,10)
        };

        public Fix[] m_fixAsteroidPositionY = //{ };
        {
            Fix.Mix(2,5,10),
            Fix.Mix(3,8,10),
            Fix.Mix(-3,1,10),
            Fix.Mix(-3,5,10),
            Fix.Mix(-1,0,10)
        };

        public Fix[] m_fixAsteroidSize = //{ };
        {
            Fix.Mix(1,0,10),
            Fix.Mix(2,0,10),
            Fix.Mix(1,5,10),
            Fix.Mix(0,5,10),
            Fix.Mix(0,8,10)
        };

        public Fix[] AsteroidPositionX => m_fixAsteroidPositionX;

        public Fix[] AsteroidPositionY => m_fixAsteroidPositionY;

        public Fix[] AsteroidSize => m_fixAsteroidSize;

        #endregion

        #region IShipRespawnConstData
        //temp value until propper editor can be setup
        public Fix m_fixRespawnRadius = Fix.Mix(10,0,2);
        
        public Fix SpawnRadius => m_fixRespawnRadius;

        #endregion
    
        public ConstData()
        {

        }

        public ConstData(MapGenSettings mgsMapSettings)
        {
            GenerateAsteroidPositioins(mgsMapSettings);

            m_fixRespawnRadius = mgsMapSettings.m_fixMapBoundryRadius.FixValue;
        }

        public void GenerateAsteroidPositioins(MapGenSettings mgsMapSettings)
        {
            //calculate area covered by map
            Fix fixMapArea = FixMath.PI * (mgsMapSettings.m_fixAsteroidSpawnRadius.FixValue * mgsMapSettings.m_fixAsteroidSpawnRadius.FixValue);

            int iAsteroidsToSpawn = (int)FixMath.Ceiling(fixMapArea * mgsMapSettings.m_fixDensityPerSqrUnit.FixValue);

            //setup temp asteroid buffer 
            List<FixVec2> fixAsteroidPos = new List<FixVec2>(iAsteroidsToSpawn);

            List<Fix> fixAsteroidSize = new List<Fix>(iAsteroidsToSpawn);

            //deterministic random value
            DeterministicLCRGenerator rngRandom = new DeterministicLCRGenerator(mgsMapSettings.m_lSeed);

            for (int i = 0; i < iAsteroidsToSpawn; i++)
            {
                for(int j = 0; j < mgsMapSettings.m_iPlacementAttemptsPerAsteroid; j++)
                {
                    FixVec2 fixPosCandidate = rngRandom.GetRandomFix2InUnitCircle() * mgsMapSettings.m_fixAsteroidSpawnRadius.FixValue;

                    Fix fixSize = rngRandom.GetRandomFix(mgsMapSettings.m_fixMinSize.FixValue, mgsMapSettings.m_fixMaxSize.FixValue);

                    bool bIsPosSafe = true;

                    //check if pos is clear
                    for(int k = 0; k < fixAsteroidPos.Count; k++)
                    {
                        Fix fixMinDist = (fixSize + fixAsteroidSize[k] + mgsMapSettings.m_fMinSpacing.FixValue);

                        Fix fixTrueDistSq = (fixAsteroidPos[k] - fixPosCandidate).GetMagnitudeSqr();

                        //check if asteroid is to clost to another asteroid
                        if(fixTrueDistSq < (fixMinDist * fixMinDist))
                        {
                            bIsPosSafe = false;
                            break;
                        }
                    }

                    //pos is not clear find new pos
                    if(!bIsPosSafe)
                    {
                        continue;
                    }

                    fixAsteroidPos.Add(fixPosCandidate);
                    fixAsteroidSize.Add(fixSize);

                    break;
                }
            }


            //fill asteroid buffers 
            m_fixAsteroidPositionX = new Fix[fixAsteroidPos.Count];
            m_fixAsteroidPositionY = new Fix[fixAsteroidPos.Count];
            m_fixAsteroidSize = new Fix[fixAsteroidSize.Count];

            //copy all the values into fixed array buffers 
            for(int i = 0; i < fixAsteroidPos.Count; i++)
            {
                m_fixAsteroidPositionX[i] = fixAsteroidPos[i].X;
                m_fixAsteroidPositionY[i] = fixAsteroidPos[i].Y;
                m_fixAsteroidSize[i] = fixAsteroidSize[i];
            }
        }
    }
}