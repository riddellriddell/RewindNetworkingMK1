using System.Collections.Generic;

public static class HelperFunctions
{
    public static int mod(int x, int m)
    {
        int r = x % m;
        return r < 0 ? r + m : r;
    }

    public static List<int> CoPrimes(int iTarget, int iCoPrimeMax)
    {
        List<int> iCoPrimes = new List<int>();

        for (int i = iCoPrimeMax - 1; i > 0; i--)
        {
            if (GreatestCommonDivisor(iTarget, i) == 1)
            {
                iCoPrimes.Add(iTarget);
            }

        }

        return iCoPrimes;
    }

    public static int GreatestCommonDivisor(int iA, int iB)
    {
        if (iB != 0)
        {
            return GreatestCommonDivisor(iB, iA % iB);
        }
        else
        {
            return iA;
        }
    }

    public static bool AreEqual(byte[] bArrayA, byte[] bArrayB)
    {
        if(bArrayA.Length != bArrayB.Length)
        {
            return false;
        }

        for(int i = 0; i < bArrayA.Length; i++)
        {
            if(bArrayA[i] != bArrayB[i])
            {
                return false;
            }
        }

        return true;
    }

    public static bool AreEqual(List<byte> bArrayA, List<byte> bArrayB)
    {
        if (bArrayA.Count != bArrayB.Count)
        {
            return false;
        }

        for (int i = 0; i < bArrayA.Count; i++)
        {
            if (bArrayA[i] != bArrayB[i])
            {
                return false;
            }
        }

        return true;
    }

}
