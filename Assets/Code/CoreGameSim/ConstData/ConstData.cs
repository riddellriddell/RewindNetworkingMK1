using FixedPointy;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Sim
{
    public class ConstData: IShipRespawnConstData, IAsteroidCollisionConstData
    {
        #region IAsteroidCollisionConstData
        public Fix[] m_fixAsteroidPositionX =
        {
            Fix.Mix(3,5,10),
            Fix.Mix(-3,0,10),
            Fix.Mix(2,1,10),
            Fix.Mix(-3,8,10),
            Fix.Mix(1,0,10)
        };

        public Fix[] m_fixAsteroidPositionY =
        {
            Fix.Mix(2,5,10),
            Fix.Mix(3,8,10),
            Fix.Mix(-3,1,10),
            Fix.Mix(-2,5,10),
            Fix.Mix(1,0,10)
        };

        public Fix[] m_fixAsteroidSize =
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
        public Fix m_fixRespawnRadius = Fix.Mix(3,1,2);
        
        public Fix SpawnRadius => m_fixRespawnRadius;

        #endregion
    }
}