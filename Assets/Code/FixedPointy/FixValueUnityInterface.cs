using FixedPointy;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct FixValueUnityInterface
{
    [SerializeField]
    public int IntValue;
    [SerializeField]
    public int DecimalOffset;

    private bool m_bCalculated;

    public Fix m_fValue;

    public Fix FixValue
    {
        get
        {
            if (m_bCalculated == false)
            {
                CalculateValue();
            }

            //set the decimal value 
            return m_fValue;
        }
    }

    public void CalculateValue()
    {
        m_bCalculated = true;

        Fix outFixNumber = new Fix(IntValue << Fix.FRACTIONAL_BITS);

        Fix offset = new Fix(DecimalOffset << Fix.FRACTIONAL_BITS);

        m_bCalculated = true;

        if(offset == 0)
        {
            Debug.LogError("0 value set on divider for fix value setter");
        }

        m_fValue = (outFixNumber / offset);
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

            //set the decimal value 
            FixVec2 outFixNumber = new FixVec2(IntValue.x << Fix.FRACTIONAL_BITS, IntValue.y << Fix.FRACTIONAL_BITS);

            Fix offset = new Fix(DecimalOffset << Fix.FRACTIONAL_BITS);

            return m_vecValue = outFixNumber / offset;
        }
    }
}