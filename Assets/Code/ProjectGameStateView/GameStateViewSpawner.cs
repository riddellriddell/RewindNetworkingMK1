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

        public GameObject m_objLazerFirePrefab;

        public int m_iNumberOfImpactsPerShip;

        public GameObject m_objLazerImpactPrefab;

        public GameObject m_objShipSpawnPrefab;

        public GameObject m_objShipDiePrefab;

        private List<GameObject> m_objLazerFirePool;

        private List<GameObject> m_objLazerImpactPool;

        private int m_ilazerImpactPoolHead;

        private List<GameObject> m_objShipSpawnPool;

        private List<GameObject> m_objShipDiePool;

        private NativeArray<Entity> m_entAsteroids;

        private NativeArray<Entity> m_entShips;

        private NativeArray<Entity> m_entLazers;

        private EntityArchetype m_eatAsteroidArchetype;

        private EntityArchetype m_eatShipArchetype;

        private EntityArchetype m_eatLazerArchetype;

        private EntityManager m_emaEntityManager;

        private GameObject m_objEffectParentObject;

        // Start is called before the first frame update
        void Start()
        {
            m_emaEntityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

            m_objEffectParentObject = new GameObject("EffectsParent");
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

            //make sure the object pools are setup 
            SetupObjectPools(ifdInterpolatedFrameData);

            //position all ships
            UpdateShipStates(ifdInterpolatedFrameData);

            //upate all lazers
            UpdateLazerStates(ifdInterpolatedFrameData);

            //update effect positions
            AllignLazerFireEffectsToShips();
        }

        protected void UpdateLazerStates(InterpolatedFrameDataGen ifdInterpolatedFrameData)
        {
            //position all lazers 
            for (int i = 0; i < ifdInterpolatedFrameData.m_fixLazerLifeRemainingErrorAdjusted.Length; i++)
            {
               

                // check if object has been enabled or disabled
                if (ifdInterpolatedFrameData.m_fixLazerLifeRemaining[i] <= 0)
                {
                    //if (m_emaEntityManager.HasComponent(m_entLazers[i], typeof(Disabled)) == false)
                    //{
                    //    m_emaEntityManager.SetEnabled(m_entLazers[i], false);
                    //
                    //    //spawn deactivation effect
                    //}

                    //temp code until unity fix enabled disabled bug
                    Vector3 vecLazerPos = m_emaEntityManager.GetComponentData<Translation>(m_entLazers[i]).Value;
                    
                    if (vecLazerPos.x != 9000)
                    {
                        Quaternion qtrLazerRot = m_emaEntityManager.GetComponentData<Rotation>(m_entLazers[i]).Value;

                        SpawnImpactEffect(
                            vecLazerPos,
                            qtrLazerRot
                                );


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


                }
                else
                {
                    //if (m_emaEntityManager.HasComponent(m_entLazers[i], typeof(Disabled)) == true)
                    //{
                    //    m_emaEntityManager.SetEnabled(m_entLazers[i], true);
                    //
                    //    //spawn activation effect
                    //}

                    Vector3 vecLazerPos = m_emaEntityManager.GetComponentData<Translation>(m_entLazers[i]).Value;
                    if (vecLazerPos.x == 9000)
                    {
                        int iOwnerOfLazer = i / (m_entLazers.Length / m_entShips.Length);

                        SpawnFireEffect(iOwnerOfLazer);
                    }

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
                }

                //change lazer color for local vs non local players
            }
        }

        protected void UpdateShipStates(InterpolatedFrameDataGen ifdInterpolatedFrameData)
        {
            //position all ships 
            for (int i = 0; i < ifdInterpolatedFrameData.m_fixShipHealth.Length; i++)
            {


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

                    //check if ship has already been hiden
                    if (m_emaEntityManager.GetComponentData<Translation>(m_entShips[i]).Value.x != 9000)
                    {
                        //spawn destroy effect 
                        SpawnShipDieEffect(i);

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

                }
                else
                {
                    //if (m_emaEntityManager.HasComponent(m_entShips[i], typeof(Disabled)) == true)
                    //{
                    //    m_emaEntityManager.SetEnabled(m_entShips[i], true);
                    //
                    //    //spawn activation effect
                    //}

                    if (m_emaEntityManager.GetComponentData<Translation>(m_entShips[i]).Value.x == 9000)
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

                        //spawn respawn effect
                        SpawnShipSpawnEffect(i);

                    }
                    else
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
                    }

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

                int iAsteroidIndex = i % m_objAsteroidPrefab.Count;

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

        protected void SetupObjectPools(InterpolatedFrameDataGen ifdInterpolatedFrameData)
        {
            int iNumberOfShips = ifdInterpolatedFrameData.m_fixShipHealth.Length;

            //skip setup if already done
            if (m_objLazerFirePool != null && m_objLazerFirePool.Count == iNumberOfShips)
            {
                return;
            }

            //clean up
            if (m_objLazerFirePool != null)
            {
                for (int i = 0; i < m_objLazerFirePool.Count; i++)
                {
                    Destroy(m_objLazerFirePool[i]);
                }
            }

            //setup lazer fire effects 
            m_objLazerFirePool = new List<GameObject>(iNumberOfShips);

            for (int i = 0; i < iNumberOfShips; i++)
            {
                GameObject objNewObject = GameObject.Instantiate(m_objLazerFirePrefab, m_objEffectParentObject.transform);
                               
                objNewObject.SetActive(false);

                m_objLazerFirePool.Add(objNewObject);
            }

            //setup lazer impact effects

            //clean up
            if (m_objLazerImpactPool != null)
            {
                for (int i = 0; i < m_objLazerFirePool.Count; i++)
                {
                    Destroy(m_objLazerImpactPool[i]);
                }
            }

            int iNumberOfLazerImpacts = m_iNumberOfImpactsPerShip * iNumberOfShips;

            m_objLazerImpactPool = new List<GameObject>(iNumberOfLazerImpacts);

            for (int i = 0; i < iNumberOfLazerImpacts; i++)
            {
                GameObject objNewObject = GameObject.Instantiate(m_objLazerImpactPrefab, m_objEffectParentObject.transform);

                objNewObject.SetActive(false);

                m_objLazerImpactPool.Add(objNewObject);
            }

            //setup ship spawn effects

            //clean up
            if (m_objShipSpawnPool != null)
            {
                for (int i = 0; i < m_objLazerFirePool.Count; i++)
                {
                    Destroy(m_objShipSpawnPool[i]);
                }
            }

            m_objShipSpawnPool = new List<GameObject>(iNumberOfShips);

            for (int i = 0; i < iNumberOfShips; i++)
            {
                GameObject objNewObject = GameObject.Instantiate(m_objShipSpawnPrefab, m_objEffectParentObject.transform);

                objNewObject.SetActive(false);

                m_objShipSpawnPool.Add(objNewObject);
            }

            //setup ship die effects

            //clean up
            if (m_objShipDiePool != null)
            {
                for (int i = 0; i < m_objLazerFirePool.Count; i++)
                {
                    Destroy(m_objShipDiePool[i]);
                }
            }

            m_objShipDiePool = new List<GameObject>(iNumberOfShips);

            for (int i = 0; i < iNumberOfShips; i++)
            {
                GameObject objNewObject = GameObject.Instantiate(m_objShipDiePrefab, m_objEffectParentObject.transform);

                objNewObject.SetActive(false);

                m_objShipDiePool.Add(objNewObject);
            }
        }

        protected void SpawnFireEffect(int iShipIndex)
        {
            //get fire effect from pool
            GameObject objFireEffect = m_objLazerFirePool[iShipIndex];

            //move to correct position
            objFireEffect.transform.localPosition = m_emaEntityManager.GetComponentData<Translation>(m_entShips[iShipIndex]).Value;
            objFireEffect.transform.localScale = Vector3.one * m_emaEntityManager.GetComponentData<Scale>(m_entShips[iShipIndex]).Value;
            objFireEffect.transform.localRotation = m_emaEntityManager.GetComponentData<Rotation>(m_entShips[iShipIndex]).Value;

            //enable effect
            objFireEffect.SetActive(false);
            objFireEffect.SetActive(true);
        }

        protected void SpawnShipSpawnEffect(int iShipIndex)
        {
            //get fire effect from pool
            GameObject objSpawnEffect = m_objShipSpawnPool[iShipIndex];

            //move to correct position
            objSpawnEffect.transform.localPosition = m_emaEntityManager.GetComponentData<Translation>(m_entShips[iShipIndex]).Value;
            objSpawnEffect.transform.localScale = Vector3.one * m_emaEntityManager.GetComponentData<Scale>(m_entShips[iShipIndex]).Value;
            objSpawnEffect.transform.localRotation = m_emaEntityManager.GetComponentData<Rotation>(m_entShips[iShipIndex]).Value;

            //enable effect
            objSpawnEffect.SetActive(false);
            objSpawnEffect.SetActive(true);
        }

        protected void SpawnShipDieEffect(int iShipIndex)
        {
            //get fire effect from pool
            GameObject objDieEffect = m_objShipDiePool[iShipIndex];

            //move to correct position
            objDieEffect.transform.localPosition = m_emaEntityManager.GetComponentData<Translation>(m_entShips[iShipIndex]).Value;
            objDieEffect.transform.localScale = Vector3.one * m_emaEntityManager.GetComponentData<Scale>(m_entShips[iShipIndex]).Value;
            objDieEffect.transform.localRotation = m_emaEntityManager.GetComponentData<Rotation>(m_entShips[iShipIndex]).Value;

            //enable effect
            objDieEffect.SetActive(false);
            objDieEffect.SetActive(true);
        }

        protected void SpawnImpactEffect(Vector3 vecPos, Quaternion qtrRotation)
        {
            //get fire effect from pool
            GameObject objImpactEffect = m_objLazerImpactPool[m_ilazerImpactPoolHead];

            m_ilazerImpactPoolHead = (++m_ilazerImpactPoolHead) % m_objLazerImpactPool.Count;

            //get direction
            objImpactEffect.transform.localRotation = qtrRotation;

            //move to correct position
            objImpactEffect.transform.localPosition = vecPos;

            //enable effect
            objImpactEffect.SetActive(false);
            objImpactEffect.SetActive(true);
        }

        protected void AllignLazerFireEffectsToShips()
        {
            for (int i = 0; i < m_objLazerFirePool.Count; i++)
            {
                if (m_objLazerFirePool[i].activeInHierarchy)
                {
                    m_objLazerFirePool[i].transform.localPosition = m_emaEntityManager.GetComponentData<Translation>(m_entShips[i]).Value;
                    m_objLazerFirePool[i].transform.localScale = Vector3.one * m_emaEntityManager.GetComponentData<Scale>(m_entShips[i]).Value;
                    m_objLazerFirePool[i].transform.localRotation = m_emaEntityManager.GetComponentData<Rotation>(m_entShips[i]).Value;
                }
            }
        }
    }
}
