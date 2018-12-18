using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class HelperFunctions
{
    public static int mod(int x, int m)
    {
        int r = x % m;
        return r < 0 ? r + m : r;
    }
}
