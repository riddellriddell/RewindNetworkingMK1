using FixedPointy;
using UnityEngine;


namespace Sim
{
    [CreateAssetMenu(fileName = "MapSettings", menuName = "Sim/MapSettings", order = 1)]
    public class MapGenSettings : ScriptableObject
    {
        [SerializeField]
        public long m_lSeed;


        [SerializeField]
        public int m_iPlacementAttemptsPerAsteroid;

        //how many asteroids to spawn per square unit of space
        [SerializeField]
        public FixTo3PlacesUnityInterface m_fixDensityPerSqrUnit;

        //the area to fill
        [SerializeField]
        public FixTo3PlacesUnityInterface m_fixAsteroidSpawnRadius;

        [SerializeField]
        public FixTo3PlacesUnityInterface m_fixMapBoundryRadius;

        //the closest distance 2 asteroids can be togeather
        [SerializeField]
        public FixTo3PlacesUnityInterface m_fMinSpacing;


        [SerializeField]
        public FixTo3PlacesUnityInterface m_fixMinSize;

        [SerializeField]
        public FixTo3PlacesUnityInterface m_fixMaxSize;

    }
}
