using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public class ECSPrefabInjector : MonoBehaviour, IConvertGameObjectToEntity
{
    //prefab to spawn and link at this location
    public GameObject m_objPrefab;

    void IConvertGameObjectToEntity.Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        GameObject objPrefabInstance = GameObject.Instantiate(m_objPrefab);

        dstManager.AddComponentObject(entity, objPrefabInstance);
        GameObjectEntity.AddToEntity(dstManager, objPrefabInstance, entity);
    }

    public struct PrefabEntitiyComponentData : IComponentData
    {
        public short iPrefabID;
    }

    public struct PrefabEntitySharedData : ISharedComponentData
    {
        public short iPrefabType;
    }
}
