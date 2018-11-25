using FixedPointy;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Sim.Physics
{
    public enum PrimitiveType
    {
        AABB,
        CIRCLE
    }
    public interface ICollisionPrimitive
    {
        /// <summary>
        /// the shape of the enum to use in collision 
        /// </summary>
        PrimitiveType Shape { get; set;}

        /// <summary>
        /// collition bit flage 
        /// first bit is if this item is enabled 
        /// </summary>
        short Flags { get; set;}

        /// <summary>
        /// the center of the collider
        /// </summary>
        FixVec2 CenterPoint { get; set;}

        /// <summary>
        /// the max distance squared from that center point to the edge of this item 
        /// this is used in the broad phase to speed up collision detection 
        /// </summary>
        Fix Extents { get; set; }
    }

    public interface IBoxPrimitive : ICollisionPrimitive
    {
        /// <summary>
        /// the distance from the center to the edge of the box on both axis 
        /// </summary>
        FixVec2 HalfXYSize { get; set; }
    } 
}
