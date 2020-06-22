using Sim;
using SimDataInterpolation;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace GameStateView
{
    public class GameStateViewSpawner : MonoBehaviour
    {
        public int m_iAsteroidRenderLayer;

        public List<GameObject> m_objAsteroidPrefab;

        public GameObject m_objShipPrefab;

        public GameObject m_objLazerPrefab;

        private NativeArray<Entity> m_entAsteroids;

        private NativeArray<Entity> m_entShips;

        private NativeArray<Entity> m_entLazers;

        private EntityArchetype m_eatAsteroidArchetype;

        private EntityArchetype m_eatShipArchetype;

        private EntityArchetype m_eatLazerArchetype;

        private EntityManager m_emaEntityManager;

        // Start is called before the first frame update
        void Start()
        {
            m_emaEntityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

           


        }

        protected void OnDestroy()
        {
            if (m_entAsteroids != null && m_entAsteroids.IsCreated)
            {
                m_entAsteroids.Dispose();
            }

            if (m_entShips != null && m_entShips.IsCreated)
            {
                m_entShips.Dispose();
            }

            if (m_entLazers != null && m_entLazers.IsCreated)
            {
                m_entLazers.Dispose();
            }
        }

        public void SetupConstDataViewEntities(ConstData cdaConstData)
        {
            SetAsteroidValues(cdaConstData);
        }

        public void UpdateView(InterpolatedFrameDataGen ifdInterpolatedFrameData, SimProcessorSettings sdaSettingsData)
        {

            //make sure view data matches frame data format
            MatchInterpolatedDataFormat(ifdInterpolatedFrameData, sdaSettingsData);

            //position all ships
            UpdateShipStates(ifdInterpolatedFrameData);

            //upate all lazers
            UpdateLazerStates(ifdInterpolatedFrameData);
        }

        protected void UpdateLazerStates(InterpolatedFrameDataGen ifdInterpolatedFrameData)
        {
            //position all lazers 
            for (int i = 0; i < ifdInterpolatedFrameData.m_fixLazerLifeRemainingErrorAdjusted.Length; i++)
            {
                

                //move to location
                m_emaEntityManager.SetComponentData(
                    m_entLazers[i],
                    new Translation()
                    {
                        Value = new float3(
                        ifdInterpolatedFrameData.m_fixLazerPositionXErrorAdjusted[i],
                        0,
                        ifdInterpolatedFrameData.m_fixLazerPositionYErrorAdjusted[i])
                    }
                    );

                float fRotation = Mathf.Atan2(ifdInterpolatedFrameData.m_fixLazerVelocityXErrorAdjusted[i], ifdInterpolatedFrameData.m_fixLazerVelocityYErrorAdjusted[i]);

                //rotate to angle 
                m_emaEntityManager.SetComponentData(m_entLazers[i], new Rotation() { Value = quaternion.Euler(0, fRotation, 0) });

                // check if object has been enabled or disabled
                if (ifdInterpolatedFrameData.m_fixLazerLifeRemainingErrorAdjusted[i] <= 0)
                {
                    //if (m_emaEntityManager.HasComponent(m_entLazers[i], typeof(Disabled)) == false)
                    //{
                    //    m_emaEntityManager.SetEnabled(m_entLazers[i], false);
                    //
                    //    //spawn deactivation effect
                    //}

                    //temp code until unity fix enabled disabled bug
                    m_emaEntityManager.SetComponentData(
                        m_entLazers[i],
                        new Translation()
                        {
                            Value = new float3(
                            9000,
                            9000,
                            9000)
                        }
                        );
                }
                else
                {
                    //if (m_emaEntityManager.HasComponent(m_entLazers[i], typeof(Disabled)) == true)
                    //{
                    //    m_emaEntityManager.SetEnabled(m_entLazers[i], true);
                    //
                    //    //spawn activation effect
                    //}
                }

                //change lazer color for local vs non local players
            }
        }

        protected void UpdateShipStates(InterpolatedFrameDataGen ifdInterpolatedFrameData)
        {
            //position all ships 
            for (int i = 0; i < ifdInterpolatedFrameData.m_fixShipHealth.Length; i++)
            {
                //move to location
                m_emaEntityManager.SetComponentData(
                    m_entShips[i],
                    new Translation()
                    {
                        Value = new float3(
                        ifdInterpolatedFrameData.m_fixShipPosXErrorAdjusted[i],
                        0,
                        ifdInterpolatedFrameData.m_fixShipPosYErrorAdjusted[i])
                    }
                    );

                float fRotation = Mathf.Atan2(ifdInterpolatedFrameData.m_fixShipVelocityXErrorAdjusted[i], ifdInterpolatedFrameData.m_fixShipVelocityYErrorAdjusted[i]);

                //rotate to angle 
                m_emaEntityManager.SetComponentData(m_entShips[i], new Rotation() { Value = quaternion.Euler(0, fRotation, 0) });

                // check if object has been enabled or disabled
                if (ifdInterpolatedFrameData.m_fixShipHealth[i] <= 0)
                {
                    //if (m_emaEntityManager.HasComponent(m_entShips[i], typeof(Disabled)) == false)
                    //{
                    //    m_emaEntityManager.SetEnabled(m_entShips[i], false);
                    //
                    //    //spawn deactivation effect
                    //}

                    //temp code until unity fix enable and disable bugs
                    
                    //move to location ourside world
                    m_emaEntityManager.SetComponentData(
                        m_entShips[i],
                        new Translation()
                        {
                            Value = new float3(
                            9000,
                            9000,
                            9000)
                        }
                        );

                }
                else
                {
                    //if (m_emaEntityManager.HasComponent(m_entShips[i], typeof(Disabled)) == true)
                    //{
                    //    m_emaEntityManager.SetEnabled(m_entShips[i], true);
                    //
                    //    //spawn activation effect
                    //}
                }

            }
        }

        protected Entity SetupPrefab(GameObject objSourcePrefab)
        {
            return GameObjectConversionUtility.ConvertGameObjectHierarchy(objSourcePrefab, new GameObjectConversionSettings()
            {
                DestinationWorld = World.DefaultGameObjectInjectionWorld
            });
        }

        protected Entity SpawnPrefabAtLocation(Entity entEntity, float3 fl3Location, float fl3Scale, quaternion qutRotation)
        {
            Entity entNewEnt = m_emaEntityManager.Instantiate(entEntity);

            m_emaEntityManager.SetComponentData(entNewEnt, new Translation() { Value = fl3Location });
            m_emaEntityManager.AddComponentData(entNewEnt, new Scale() { Value = fl3Scale });
            m_emaEntityManager.SetComponentData(entNewEnt, new Rotation() { Value = qutRotation });

            return entNewEnt;
        }

        protected void SetAsteroidValues(ConstData cdaConstData)
        {
            if (m_entAsteroids != null && m_entAsteroids.IsCreated)
            {
                m_entAsteroids.Dispose();
            }

            NativeArray<Entity> entAsteroidPrefab = new NativeArray<Entity>(m_objAsteroidPrefab.Count, Allocator.Temp);

            //setup asteroids
            for (int i = 0; i < m_objAsteroidPrefab.Count; i++)
            {
                entAsteroidPrefab[i] = SetupPrefab(m_objAsteroidPrefab[i]);
            }

            m_entAsteroids = new NativeArray<Entity>(cdaConstData.m_fixAsteroidSize.Length, Allocator.Persistent);

            for (int i = 0; i < cdaConstData.m_fixAsteroidSize.Length; i++)
            {
                //set the position plus an offset and rotation value
                float3 fl3Pos = new float3((float)cdaConstData.AsteroidPositionX[i], 0, (float)cdaConstData.AsteroidPositionY[i]);

                //set the scale value
                float fScale = (float)cdaConstData.m_fixAsteroidSize[i];

                Random rngRandom = new Random((uint)cdaConstData.AsteroidSize[0].Raw);

                quaternion qtrRotation = quaternion.Euler(rngRandom.NextFloat3(0, 360));

                int iAsteroidIndex = rngRandom.NextInt(0, m_objAsteroidPrefab.Count - 1);

                //spawn object and add it to entity array 
                m_entAsteroids[i] = SpawnPrefabAtLocation(entAsteroidPrefab[iAsteroidIndex], fl3Pos, fScale, qtrRotation);
            }

            //clean up asteroids
            m_emaEntityManager.DestroyEntity(entAsteroidPrefab);

            entAsteroidPrefab.Dispose();


        }

        protected void MatchInterpolatedDataFormat(InterpolatedFrameDataGen ifdInterpolatedFrameData, SimProcessorSettings sdaSettings)
        {
            if (m_entShips == null || m_entShips.Length != ifdInterpolatedFrameData.m_fixShipHealth.Length)
            {
                //setup ship prefab
                Entity entShipPrefab = SetupPrefab(m_objShipPrefab);


                if (m_entShips != null && m_entShips.IsCreated)
                {
                    m_entShips.Dispose();
                }

                m_entShips = m_emaEntityManager.Instantiate(entShipPrefab, ifdInterpolatedFrameData.m_fixShipHealth.Length, Allocator.Persistent);

                for (int i = 0; i < m_entShips.Length; i++)
                {
                    m_emaEntityManager.AddComponentData(m_entShips[i], new Scale() { Value = (float)sdaSettings.m_fixShipSize });
                    //m_emaEntityManager.SetEnabled(m_entShips[i], false);
                }

                m_emaEntityManager.DestroyEntity(entShipPrefab);
            }



            if (m_entLazers == null || m_entLazers.Length != ifdInterpolatedFrameData.m_fixLazerLifeRemaining.Length)
            {
                //setup ship prefab
                Entity entLazerPrefab = SetupPrefab(m_objLazerPrefab);

                if (m_entLazers != null && m_entLazers.IsCreated)
                {
                    m_entLazers.Dispose();
                }

                m_entLazers = m_emaEntityManager.Instantiate(entLazerPrefab, ifdInterpolatedFrameData.m_fixLazerLifeRemaining.Length, Allocator.Persistent);

                for (int i = 0; i < m_entLazers.Length; i++)
                {
                    m_emaEntityManager.AddComponentData(m_entLazers[i], new Scale() { Value = (float)sdaSettings.LazerSize });
                    //m_emaEntityManager.SetEnabled(m_entLazers[i], false);
                }

                m_emaEntityManager.DestroyEntity(entLazerPrefab);
            }
        }
    }
}
