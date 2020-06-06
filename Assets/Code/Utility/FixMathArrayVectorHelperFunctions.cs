using FixedPointy;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Utility
{
    //utillity class for handling arrays of fix values that represent vectors 
    public static class FixMathArrayVectorHelperFunctions
    {
        internal const int FRACTIONAL_BITS = 16;

        public static Fix Magnitude(in Fix fixX,in Fix fixY)
        {
            ulong N = (ulong)((long)fixX.Raw * (long)fixX.Raw + (long)fixY.Raw * (long)fixY.Raw);

            if (N == 0)
            {
                return 0;
            }

            return new Fix((int)(SqrtULong(N << 2) + 1) >> 1);
        }

        internal static uint SqrtULong(ulong N)
        {

            ulong x = 1L << ((31 + (FRACTIONAL_BITS + 2) + 1) / 2);
            while (true)
            {
                ulong y = (x + N / x) >> 1;
                if (y >= x)
                    return (uint)x;
                x = y;
            }
        }
    }
}