using System;
using System.Collections.Generic;
using UnityEngine;

public class ClassWithIDFactory
{
    public ClassWithIDFactory()
    {
        SetupTypes();
    }

    //adds new type and returns index
    public int AddType<T>()
    {
        Type typType = typeof(T);

        m_tipTypeIDs.Add(typType);

        return m_tipTypeIDs.Count - 1;
    }

    public int AddType<T>(int iID)
    {
        if(iID == int.MinValue)
        {
            return AddType<T>();
        }

        Type typType = typeof(T);

        while (m_tipTypeIDs.Count <= iID)
        {
            m_tipTypeIDs.Add(null);
        }

        if(m_tipTypeIDs[iID] != null && m_tipTypeIDs[iID].Equals(typType))
        {
            return iID;
        }

       // Debug.Assert(m_tipTypeIDs[iID] == null, $"Two types {m_tipTypeIDs[iID].ToString()}, {typType.ToString()} have the same index {iID}");

        m_tipTypeIDs[iID] = typType;

        return iID;
    }

    public T CreateType<T>(int iID)
    {
        if (iID < 0 || iID >= m_tipTypeIDs.Count)
        {
            throw new Exception($"ID {iID} does not exists in factory");
        }

        return (T)Activator.CreateInstance(m_tipTypeIDs[iID]);
    }

    protected virtual void SetupTypes()
    {

    }

    protected List<Type> m_tipTypeIDs = new List<Type>();
}

