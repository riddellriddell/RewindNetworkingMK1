using Sim;
using SimDataInterpolation;
using System.Collections.Generic;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace GameStateView
{
    public class GameStateViewSpawnerMK2 : MonoBehaviour, IGameStateView
    {
        //------- Prefabs ---------

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

        private List<GameObject> m_objAsteroids;

        private List<GameObject> m_objShips;

        private List<GameObject> m_objLazers;

        private GameObject m_objEffectParentObject;

        private GameObject m_objAsteroidParentObject;

        private GameObject m_objShipParentObject;

        private GameObject m_objLazerParentObject;

        // Start is called before the first frame update
        void Start()
        {
            m_objEffectParentObject = new GameObject("EffectsParent");
            m_objAsteroidParentObject = new GameObject("AsteroidParent");
            m_objShipParentObject = new GameObject("ShipParent");
            m_objLazerParentObject = new GameObject("LazerParent");
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

                    //temp code until unity fix enabled disabled bug
                    Vector3 vecLazerPos = m_objLazers[i].transform.position;

                    if (vecLazerPos.x != 9000)
                    {
                        Quaternion qtrLazerRot = m_objLazers[i].transform.rotation;

                        SpawnImpactEffect(
                            vecLazerPos,
                            qtrLazerRot
                                );


                        m_objLazers[i].transform.position = new Vector3(9000, 9000, 9000);
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

                    Vector3 vecLazerPos = m_objLazers[i].transform.position;
                    if (vecLazerPos.x == 9000)
                    {
                        int iOwnerOfLazer = i / (m_objLazers.Count / m_objShips.Count);

                        SpawnFireEffect(iOwnerOfLazer);
                    }

                    //move to location
                    m_objLazers[i].transform.position = new Vector3(
                            ifdInterpolatedFrameData.m_fixLazerPositionXErrorAdjusted[i],
                            0,
                            ifdInterpolatedFrameData.m_fixLazerPositionYErrorAdjusted[i]);


                    float fRotation = Mathf.Atan2(ifdInterpolatedFrameData.m_fixLazerVelocityXErrorAdjusted[i], ifdInterpolatedFrameData.m_fixLazerVelocityYErrorAdjusted[i]);

                    //rotate to angle 
                    m_objLazers[i].transform.rotation = Quaternion.Euler(0, fRotation * Mathf.Rad2Deg, 0);
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
                    if (m_objShips[i].transform.position.x != 9000)
                    {
                        //spawn destroy effect 
                        SpawnShipDieEffect(i);

                        //move to location ourside world
                        m_objShips[i].transform.position = new Vector3(9000, 9000, 9000);

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

                    if ( m_objShips[i].transform.position.x == 9000)
                    {
                        //move to location
                        m_objShips[i].transform.position = new Vector3(
                            ifdInterpolatedFrameData.m_fixShipPosXErrorAdjusted[i],
                            0,
                            ifdInterpolatedFrameData.m_fixShipPosYErrorAdjusted[i]);

                        //float fRotation = Mathf.Atan2(ifdInterpolatedFrameData.m_fixShipVelocityXErrorAdjusted[i], ifdInterpolatedFrameData.m_fixShipVelocityYErrorAdjusted[i]);

                        float fRotation = (-ifdInterpolatedFrameData.m_fixShipBaseAngleErrorAdjusted[i] + 90);

                        //rotate to angle 
                        m_objShips[i].transform.rotation = Quaternion.Euler(0, fRotation, 0);

                        //spawn respawn effect
                        SpawnShipSpawnEffect(i);

                    }
                    else
                    {
                        //move to location
                        m_objShips[i].transform.position = new Vector3(
                            ifdInterpolatedFrameData.m_fixShipPosXErrorAdjusted[i],
                            0,
                            ifdInterpolatedFrameData.m_fixShipPosYErrorAdjusted[i]);

                        float fRotation = (-ifdInterpolatedFrameData.m_fixShipBaseAngleErrorAdjusted[i] + 90);

                        m_objShips[i].transform.rotation = Quaternion.Euler(0, fRotation, 0);
                    }

                }

            }
        }

        protected void SetAsteroidValues(ConstData cdaConstData)
        {
            m_objAsteroids = new List<GameObject>();

            Debug.Log("setup asteroids");
            for (int i = 0; i < cdaConstData.m_fixAsteroidSize.Length; i++)
            {
                //set the position plus an offset and rotation value
                Vector3 vecPos = new Vector3((float)cdaConstData.AsteroidPositionX[i], 0, (float)cdaConstData.AsteroidPositionY[i]);

                //set the scale value
                float fScale = (float)cdaConstData.m_fixAsteroidSize[i];

                Random rngRandom = new Random((uint)cdaConstData.AsteroidSize[0].Raw);

                Quaternion qtrRotation = Quaternion.Euler(rngRandom.NextFloat3(0, 360));

                int iAsteroidIndex = i % m_objAsteroidPrefab.Count;


                Debug.Log("spawn at location");
                //spawn object and add it to entity array 
                Transform trnNewAsteroid = Instantiate(m_objAsteroidPrefab[iAsteroidIndex], m_objAsteroidParentObject.transform).transform;

                trnNewAsteroid.position = vecPos;
                trnNewAsteroid.localScale = Vector3.one * fScale;
                trnNewAsteroid.rotation = qtrRotation;
                m_objAsteroids.Add(trnNewAsteroid.gameObject);
            }
        }

        protected void MatchInterpolatedDataFormat(InterpolatedFrameDataGen ifdInterpolatedFrameData, SimProcessorSettings sdaSettings)
        {
            if (m_objShips == null || m_objShips.Count != ifdInterpolatedFrameData.m_fixShipHealth.Length)
            {

                m_objShips = new List<GameObject>();

                for (int i = 0; i < ifdInterpolatedFrameData.m_fixShipHealth.Length; i++)
                {
                    Transform trnShip = Instantiate(m_objShipPrefab).transform;

                    trnShip.localScale = Vector3.one * (float)sdaSettings.m_fixShipSize;

                    m_objShips.Add(trnShip.gameObject);
                }
            }

            if (m_objLazers == null || m_objLazers.Count != ifdInterpolatedFrameData.m_fixLazerLifeRemaining.Length)
            {
                //setup ship prefab
                m_objLazers = new List<GameObject>();

                for (int i = 0; i < ifdInterpolatedFrameData.m_fixLazerLifeRemaining.Length; i++)
                {
                    Transform trnLazer = Instantiate(m_objLazerPrefab).transform;

                    trnLazer.localScale = Vector3.one * (float)sdaSettings.LazerSize;

                    m_objLazers.Add(trnLazer.gameObject);
                }
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
            objFireEffect.transform.localPosition = m_objShips[iShipIndex].transform.position;
            objFireEffect.transform.localScale = m_objShips[iShipIndex].transform.localScale;
            objFireEffect.transform.localRotation = m_objShips[iShipIndex].transform.localRotation;

            //enable effect
            objFireEffect.SetActive(false);
            objFireEffect.SetActive(true);
        }

        protected void SpawnShipSpawnEffect(int iShipIndex)
        {
            //get fire effect from pool
            GameObject objSpawnEffect = m_objShipSpawnPool[iShipIndex];

            //move to correct position
            objSpawnEffect.transform.localPosition = m_objShips[iShipIndex].transform.position;
            objSpawnEffect.transform.localScale = m_objShips[iShipIndex].transform.localScale;
            objSpawnEffect.transform.localRotation = m_objShips[iShipIndex].transform.localRotation;

            //enable effect
            objSpawnEffect.SetActive(false);
            objSpawnEffect.SetActive(true);
        }

        protected void SpawnShipDieEffect(int iShipIndex)
        {
            //get fire effect from pool
            GameObject objDieEffect = m_objShipDiePool[iShipIndex];

            //move to correct position
            objDieEffect.transform.localPosition = m_objShips[iShipIndex].transform.position;
            objDieEffect.transform.localScale = m_objShips[iShipIndex].transform.localScale;
            objDieEffect.transform.localRotation = m_objShips[iShipIndex].transform.localRotation;

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
                    m_objLazerFirePool[i].transform.localPosition = m_objShips[i].transform.position;
                    m_objLazerFirePool[i].transform.localScale = m_objShips[i].transform.localScale;
                    m_objLazerFirePool[i].transform.localRotation = m_objShips[i].transform.localRotation;
                }
            }
        }
    }
}
