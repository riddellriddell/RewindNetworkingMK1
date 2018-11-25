using FixedPointy;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// this interface defines what function the physics system will be able to perform 
/// </summary>
namespace Sim.Physics
{
    public interface IPhysicsEngine
    {
        bool GetPlayersOverlappingCircle(FixVec2 vecCenter, Fix fRadius, short sBitMask,List<short> sItemsHit);
    }
}