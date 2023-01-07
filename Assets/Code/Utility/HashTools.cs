using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Utility
{
    public static class HashTools
    {
        public static bool CompareHashes(byte[] bHashA, byte[] bHashB)
        {
            if(bHashA.Length != bHashB.Length)
            {
                return false;
            }

            for(int i =0; i < bHashA.Length; i++)
            {
                if(bHashA[i] != bHashB[i])
                {
                    return false;
                }
            }

            return true;
        }
        public static void MergeHashes(ref byte[] bMergeTo, byte[] bMergeFrom)
        {
            byte[] bTemp = new byte[bMergeTo.Length];

            for (int i = 0; i < bMergeFrom.Length; i++)
            {
                byte bTo = bMergeTo[i % bMergeTo.Length];

                bTo += 1;

                bTo = WrapBitShift(bTo, 1);

                bTo = (byte)(bTo ^ bMergeFrom[i]);

                bMergeTo[i % bMergeTo.Length] = bTo;
            }
        }

        static byte WrapBitShift(byte bInput, int iShift)
        {
            if(iShift > 0)
            {
                int iPartA = bInput << iShift;
                int iPartB = bInput >> (8 - iShift);

                bInput = (byte)(iPartA | iPartB);
            }

            if (iShift < 0)
            {
                int iPartA = bInput >> iShift;
                int iPartB = bInput << (8 + iShift);

                bInput = (byte)(iPartA | iPartB);
            }

            return bInput;
        }

        
    }
}
