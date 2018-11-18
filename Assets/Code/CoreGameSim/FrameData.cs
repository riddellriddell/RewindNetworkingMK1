using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FixedPointy;
using System.Security.Cryptography;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

public class FrameData
{
    public enum State : byte
    {
        Standing,
        Moving,
        FastAttack,
        SlowAttack,
        Blocking,
        Stunned,
        Dead
    }

    [FlagsAttribute]
    public enum Direction : byte
    {
        None = 0,
        Up = 1,
        Down = 2,
        Left = 4,
        Right = 8
    }



    public int PlayerCount
    {
        get
        {
            return m_sPlayerHealths.Count;
        }
    }

    //the frame this data represents 
    public int m_iTickNumber;

    //list of all the player healths
    public List<short> m_sPlayerHealths;

    //list of all the player positions 
    public List<FixVec2> m_v2iPosition;

    //list of all the player directions 
    public List<byte> m_bFaceDirection;

    //list of all the player states
    public List<byte> m_bPlayerState;

    //list of all action cooldowns
    public List<short> m_sCoolDown;

    //list of all the player scores 
    public List<byte> m_bScore;

    public FrameData(int iPlayerNumber)
    {
        m_iTickNumber = 0;

        //initalise all list lengths 
        m_sPlayerHealths = new List<short>(iPlayerNumber);
        m_v2iPosition = new List<FixVec2>(iPlayerNumber);
        m_bFaceDirection = new List<byte>(iPlayerNumber);
        m_bPlayerState = new List<byte>(iPlayerNumber);
        m_sCoolDown = new List<short>(iPlayerNumber);
        m_bScore = new List<byte>(iPlayerNumber);

        //initalise values 
        for (int i = 0; i < iPlayerNumber; i++)
        {
            m_sPlayerHealths.Add(0);
            m_v2iPosition.Add(FixVec2.Zero);
            m_bFaceDirection.Add(0);
            m_bPlayerState.Add(0);
            m_sCoolDown.Add(0);
            m_bScore.Add(0);
        }

    }

    public FrameData(FrameData source)
    {
        m_iTickNumber = source.m_iTickNumber;

        //coppy values from source 
        m_sPlayerHealths = new List<short>(source.m_sPlayerHealths);
        m_v2iPosition = new List<FixVec2>(source.m_v2iPosition);
        m_bFaceDirection = new List<byte>(source.m_bFaceDirection);
        m_bPlayerState = new List<byte>(source.m_bPlayerState);
        m_sCoolDown = new List<short>(source.m_sCoolDown);
        m_bScore = new List<byte>(source.m_bScore);
    }

    //generate a hash of all the values 
    public void GetHashCode(byte[] bOutput)
    {
        //size of memory stream needed 
        int iMemoryStreamSize = sizeof(int);
        iMemoryStreamSize += sizeof(short) * m_sPlayerHealths.Count;
        iMemoryStreamSize += (sizeof(int) * 2) * m_v2iPosition.Count;
        iMemoryStreamSize += m_bFaceDirection.Count;
        iMemoryStreamSize += m_bPlayerState.Count;
        iMemoryStreamSize += sizeof(short) * m_sCoolDown.Count;
        iMemoryStreamSize += m_bScore.Count;

        MemoryStream mstMemoryStream = new MemoryStream(iMemoryStreamSize);
        BinaryFormatter bfmBinaryFormatter = new BinaryFormatter();


        bfmBinaryFormatter.Serialize(mstMemoryStream, m_iTickNumber);

        for (int i = 0; i < m_sPlayerHealths.Count; i++)
        {
            bfmBinaryFormatter.Serialize(mstMemoryStream, m_sPlayerHealths[i]);
        }

        for (int i = 0; i < m_v2iPosition.Count; i++)
        {
            bfmBinaryFormatter.Serialize(mstMemoryStream, m_v2iPosition[i].X.Raw);
            bfmBinaryFormatter.Serialize(mstMemoryStream, m_v2iPosition[i].Y.Raw);
        }

        for (int i = 0; i < m_bFaceDirection.Count; i++)
        {
            bfmBinaryFormatter.Serialize(mstMemoryStream, m_bFaceDirection[i]);
        }

        for (int i = 0; i < m_bPlayerState.Count; i++)
        {
            bfmBinaryFormatter.Serialize(mstMemoryStream, m_bPlayerState[i]);
        }

        for (int i = 0; i < m_sCoolDown.Count; i++)
        {
            bfmBinaryFormatter.Serialize(mstMemoryStream, m_sCoolDown[i]);
        }

        for (int i = 0; i < m_bScore.Count; i++)
        {
            bfmBinaryFormatter.Serialize(mstMemoryStream, m_bScore[i]);
        }

        MD5 md5 = MD5.Create();

        byte[] bHash = md5.ComputeHash(mstMemoryStream);

        for (int i = 0; i < bOutput.Length; i++)
        {
            bOutput[i] = bHash[i % bHash.Length];
        }
    }

}
