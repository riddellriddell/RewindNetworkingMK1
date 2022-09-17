using FixedPointy;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Utility
{
    public struct DeterministicRandomNumberGenerator
    {
        private static readonly Fix InverseMaxValue = (Fix.One / new Fix(int.MaxValue));
        private static readonly Fix InverseHalfMaxValue = (Fix.One / new Fix(int.MaxValue / 2 ));

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

        public Fix GetRandomFix0To1()
        {
            int iRngBase = (int)(GetRandomInt());
            Fix fixReturnValue = new Fix(iRngBase) * InverseMaxValue;
            if(fixReturnValue >=1)
            {
                Debug.LogError("Rng should not be over 1");

                fixReturnValue = Fix.One;
            }
            return fixReturnValue;
        }

        public Fix GetRandomFix(Fix fixMinValue, Fix fixMaxValue)
        {
            Fix fixRngScale = fixMaxValue - fixMinValue;

            return fixMinValue + (GetRandomFix0To1() * fixRngScale);
        }

        public FixVec2 GetRandomFix2InUnitCircle()
        {
            Fix fixRandomAngle = GetRandomFix0To1() * 2 * FixMath.PI * Fix.Ratio(572958, 1000);

            Fix fixRandomRadius = GetRandomFix0To1() + GetRandomFix0To1();

            if (fixRandomRadius >= 1)
            {
                fixRandomRadius = 2 - fixRandomRadius;
            }

            //pick random polar coordinate 
            return new FixVec2(fixRandomRadius * FixMath.Cos(fixRandomAngle), fixRandomRadius * FixMath.Sin(fixRandomAngle));
        }
    }

    public struct DeterministicLCRGenerator
    {
        private int _x;
        private int _a;
        private int _c;
        private int _m;

        public DeterministicLCRGenerator(long lSeed)
        {
            ulong unSignedSeed = (ulong)lSeed;
            _x = (int)(unSignedSeed & 0x7fffffff);

            _a = 2147483629;
            _c = 2147483587;
            _m = 2147483647;
        }

        public int GetRandomInt()
        {
            return _x = (_a * _x + _c) % _m;
        }

        public Fix GetRandomFix()
        {
            return new Fix(GetRandomInt());
        }

        public Fix GetRandomFix0To1()
        {
            Fix fixReturnValue = new Fix(GetRandomInt() & Fix.FractionMask);
            if (fixReturnValue >= 1 || fixReturnValue < 0)
            {
                Debug.LogError("Rng should not be over 1");

                fixReturnValue = Fix.One;
            }
            return fixReturnValue;
        }

        public Fix GetRandomFix(Fix fixMinValue, Fix fixMaxValue)
        {
            Fix fixRngScale = fixMaxValue - fixMinValue;

            return fixMinValue + (GetRandomFix0To1() * fixRngScale);
        }

        public FixVec2 GetRandomFix2InUnitCircle()
        {
            Fix fixRandomAngle = GetRandomFix0To1() * 2 * FixMath.PI * Fix.Ratio(572958, 1000);

            Fix fixRandomRadius = GetRandomFix0To1() + GetRandomFix0To1();

            if (fixRandomRadius >= 1)
            {
                fixRandomRadius = 2 - fixRandomRadius;
            }

            //pick random polar coordinate 
            return new FixVec2(fixRandomRadius * FixMath.Cos(fixRandomAngle), fixRandomRadius * FixMath.Sin(fixRandomAngle));
        }
    }
}
