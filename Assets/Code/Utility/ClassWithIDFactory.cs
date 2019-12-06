using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class ClassWithIDFactory
{   
    public ClassWithIDFactory()
    {
        SetupTypes();
    }

    public void AddType<T>(int iID)
    {
        Type typType = typeof(T);

        while (m_tipTypeIDs.Count <= iID)
        {
            m_tipTypeIDs.Add(null);
        }

        Debug.Assert(m_tipTypeIDs[iID] == null, "Two types have the same indexes");

        m_tipTypeIDs[iID] = typType;
    }

    public T CreateType<T>(int iID)
    {
        return (T)Activator.CreateInstance(m_tipTypeIDs[iID]);
    }

    protected virtual void SetupTypes()
    {

    }

    protected List<Type> m_tipTypeIDs = new List<Type>();
}

