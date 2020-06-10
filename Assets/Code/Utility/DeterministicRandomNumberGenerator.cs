using FixedPointy;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Utility
{
    public struct DeterministicRandomNumberGenerator
    {
        uint m_w;
        uint m_z;

        public DeterministicRandomNumberGenerator(long lSeed)
        {
            lSeed = unchecked(lSeed * lSeed * lSeed * lSeed);

            byte[] seedBits = BitConverter.GetBytes(lSeed);

            m_w = BitConverter.ToUInt32(seedBits, 0);
            m_z = BitConverter.ToUInt32(seedBits, 4);
        }

        public DeterministicRandomNumberGenerator(ulong lSeed)
        {
            m_w = (uint)(lSeed >> 32);
            m_z = (uint)((lSeed << 32) >> 32);
        }

        public uint GetRandomInt()
        {
            m_z = 36969 * (m_z & 65535) + (m_z >> 16);
            m_w = 18000 * (m_w & 65535) + (m_w >> 16);
            return (m_z << 16) + m_w;  /* 32-bit result */
        }

        public Fix GetRandomFix()
        {
            uint iRngBase = GetRandomInt();
            int iRngRaw = (int)(int.MinValue + iRngBase);

            return new Fix(iRngRaw);
        }

        public Fix GetRandomFix(Fix fixMinValue, Fix fixMaxValue)
        {
            Fix fixRngScale = fixMaxValue - fixMinValue;

            return fixMinValue + (GetRandomFix() * fixRngScale);
        }
    }
}
