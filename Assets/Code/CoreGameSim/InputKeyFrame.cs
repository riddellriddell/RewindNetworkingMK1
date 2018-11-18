using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct InputKeyFrame
{
    [FlagsAttribute]
    public enum Input : byte
    {
        None = 0,
        Up = 1,
        Down = 2,
        Left = 4,
        Right = 8,
        FastAttack = 16,
        SlowAttack = 32,
        Block = 64

    }
    
    public int m_iTick;
    public byte m_iInput;    

    public int AddToByteArray(byte[] bByteArray, int iOffsetInArray)
    {
        byte[] bIntBytes = BitConverter.GetBytes(m_iTick);

        for(int i = 0; i < bIntBytes.Length; i++)
        {
            bByteArray[iOffsetInArray++] = bIntBytes[i];
        }

        bByteArray[iOffsetInArray++] = m_iInput;
        
        return iOffsetInArray;
    }
}
