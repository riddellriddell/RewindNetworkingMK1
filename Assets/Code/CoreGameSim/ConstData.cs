using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConstData
{
    public List<byte> m_bPlayerCharacters;
      
    public int PlayerCount
    {
        get
        {
            return m_bPlayerCharacters.Count;
        }
    }

    public ConstData(List<byte> bPlayerCharacters)
    {
        m_bPlayerCharacters = bPlayerCharacters;
    }
}