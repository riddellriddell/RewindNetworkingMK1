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

    public Fix FixValue
    {
        get
        {
            //set the decimal value 
            Fix outFixNumber = new Fix(IntValue << Fix.FRACTIONAL_BITS);

            Fix offset = new Fix(DecimalOffset << Fix.FRACTIONAL_BITS);

            return outFixNumber / offset;
        }
    }
}

[System.Serializable]
public struct FixVec2ValueUnityInterface
{
    [SerializeField]
    public Vector2Int IntValue;
    [SerializeField]
    public int DecimalOffset;

    public FixVec2 FixValue
    {
        get
        {
            //set the decimal value 
            FixVec2 outFixNumber = new FixVec2(IntValue.x << Fix.FRACTIONAL_BITS, IntValue.y << Fix.FRACTIONAL_BITS);

            Fix offset = new Fix(DecimalOffset << Fix.FRACTIONAL_BITS);

            return outFixNumber / offset;
        }
    }
}

[System.Serializable]
public struct FixVec3ValueUnityInterface
{
    [SerializeField]
    public Vector3Int IntValue;
    [SerializeField]
    public int DecimalOffset;

    public FixVec2 FixValue
    {
        get
        {
            //set the decimal value 
            FixVec2 outFixNumber = new FixVec2(IntValue.x << Fix.FRACTIONAL_BITS, IntValue.y << Fix.FRACTIONAL_BITS);

            Fix offset = new Fix(DecimalOffset << Fix.FRACTIONAL_BITS);

            return outFixNumber / offset;
        }
    }
}