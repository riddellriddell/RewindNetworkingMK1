using FixedPointy;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Sim
{
    public class ConstData: IShipRespawnConstData
    {
        public Fix[] m_fixAsteroidX;
        public Fix[] m_fixAsteroidY;

        public Fix[] m_fixAsteroidSize;

        //temp value until propper editor can be setup
        public Fix m_fixRespawnRadius = (Fix)10;
        
        public Fix SpawnRadius => m_fixRespawnRadius;
    }
}