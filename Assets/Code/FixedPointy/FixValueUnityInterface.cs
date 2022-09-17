using FixedPointy;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct FixTo3PlacesUnityInterface
{
    [SerializeField]
    public int Int;

    [SerializeField]
    [Range(0,999)]
    public int Dec_To_3_Places;

    private bool m_bCalculated;

    public Fix Value;

    public Fix FixValue
    {
        get
        {
            //if (m_bCalculated == false)
            //{
                CalculateValue();
           // }

            //set the decimal value 
            return Value;
        }
    }

    public void CalculateValue()
    {
        m_bCalculated = true;

        Value = Fix.Mix(Int,Dec_To_3_Places,1000);
    }
}

[System.Serializable]
public struct FixVec2ValueUnityInterface
{
    [SerializeField]
    public Vector2Int IntValue;
    [SerializeField]
    public int DecimalOffset;

    private bool m_bCalculated;

    public FixVec2 m_vecValue;

    public FixVec2 FixValue
    {
        get
        {
            if(m_bCalculated == false)
            {
                CalculateValue();                
            }

            //set the decimal value 
            return m_vecValue;
        }
    }

    public void CalculateValue()
    {
        m_bCalculated = true;

        FixVec2 outFixNumber = new FixVec2(IntValue.x << Fix.FRACTIONAL_BITS, IntValue.y << Fix.FRACTIONAL_BITS);

        Fix offset = new Fix(DecimalOffset << Fix.FRACTIONAL_BITS);

        m_vecValue = (outFixNumber / offset);
    }
}

[System.Serializable]
public struct FixVec3ValueUnityInterface
{
    [SerializeField]
    public Vector3Int IntValue;
    [SerializeField]
    public int DecimalOffset;

    private bool m_bCalculated;

    public FixVec2 m_vecValue;

    public FixVec2 FixValue
    {
        get
        {
            if (m_bCalculated == true)
            {
                return m_vecValue;
            }

            m_bCalculated = true;

            //set the decimal value 
            FixVec2 outFixNumber = new FixVec2(IntValue.x << Fix.FRACTIONAL_BITS, IntValue.y << Fix.FRACTIONAL_BITS);

            Fix offset = new Fix(DecimalOffset << Fix.FRACTIONAL_BITS);

            return m_vecValue = outFixNumber / offset;
        }
    }
}