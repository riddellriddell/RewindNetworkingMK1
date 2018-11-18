using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

/// <summary>
/// this class is a tool to check that code is deterministic and is producting the same outputs when given the same inputs 
/// </summary>
public class DataHashValidation : MonoBehaviour
{
    public static bool LogDataHash(byte[] bInputHash,int iTick,byte[] tupCalculationResults, string strTagData = "")
    {
        byte[] iTickBytes = BitConverter.GetBytes(iTick);

        byte[] bHashBase = new byte[8];

        for (int i = 0; i < bHashBase.Length; i++)
        {
            if(i < 2)
            {
                bHashBase[i] = iTickBytes[i];
            }
            else if (bInputHash.Length > i -2)
            {
                bHashBase[i] = bInputHash[i - 2];
            }
            else
            {
                bHashBase[i] = 0;
            }
        }

        //convert input to short hash
        long lInputHash = BitConverter.ToInt64(bHashBase,0);

        for (int i = 0; i < bHashBase.Length; i++)
        {
            if (tupCalculationResults.Length > i)
            {
                bHashBase[i] = tupCalculationResults[i];
            }
            else
            {
                bHashBase[i] = 0;
            }
        }

        //convert input to short hash
        long lResultHash = BitConverter.ToInt64(bHashBase, 0);

        Tuple<long, string> tupExistingResults = null;

        //check if item already exists in the dictionary 
        if (s_dicValidation.TryGetValue(lInputHash,out tupExistingResults) )
        {
            //compare results 
            if(tupExistingResults.Item1 != lResultHash)
            {
                Debug.LogError("Hash Did Not Match! Existing results tag data :" + tupExistingResults.Item2.ToString() + " New Data " + strTagData.ToString());

                return false;
            }
        }
        else
        {
            //log the hash in the dictionary 
            s_dicValidation[lInputHash] = new Tuple<long, string>(lResultHash,strTagData);
        }

        return true;
    }

    public static void ClearData()
    {
        s_dicValidation.Clear();
    }

    public static void ClearData(int iTick)
    {
        long lKeyToRemove = 0;

        byte[] bTickBytes = BitConverter.GetBytes(iTick);
        byte[] bKeyBytes;

        bool bFound = false;

        foreach(KeyValuePair<long, Tuple<long, string>> key in s_dicValidation)
        {

            lKeyToRemove = key.Key;
            bKeyBytes = BitConverter.GetBytes(lKeyToRemove);
            
            //check the tick part of the hash code 
            if(bTickBytes[0] == bKeyBytes[0] && bTickBytes[1] == bKeyBytes[1])
            {
                bFound = true;
                break;
            } 
        }

        if(bFound)
        {
            s_dicValidation.Remove(lKeyToRemove);
        }
    }

    protected static Dictionary<long, Tuple<long, string>> s_dicValidation = new Dictionary<long, Tuple<long, string>>();
    	
}
