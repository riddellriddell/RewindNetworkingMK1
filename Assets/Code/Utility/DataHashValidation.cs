using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using UnityEngine;
using Utility;

/// <summary>
/// this class is a tool to check that code is deterministic and is producting the same outputs when given the same inputs 
/// </summary>
public class DataHashValidation : MonoBehaviour
{
    public static bool LogDataHash(byte[] bInputDataHash,int bExecutionPoint , uint iTick,  byte[] bOutputDataHash, string strTagData = "")
    {
        List<byte> lstInputDataHash = new List<byte>();
        lstInputDataHash.AddRange(BitConverter.GetBytes(iTick));
        lstInputDataHash.AddRange(BitConverter.GetBytes(bExecutionPoint));
        lstInputDataHash.AddRange(bInputDataHash);

        MD5 md5 = MD5.Create();

        byte[] bInputHash = md5.ComputeHash(lstInputDataHash.ToArray());

        //convert input to short hash
        long lInputHash = BitConverter.ToInt64(bInputHash);

        List<byte> lstOutDataHash = new List<byte>(bOutputDataHash);

        while(lstOutDataHash.Count < 8)
        {
            lstOutDataHash.Add(0);
        }

        //convert input to short hash
        long lOutputHash = BitConverter.ToInt64(lstOutDataHash.ToArray());

        Tuple<long,uint, string> tupOutputHashDetailsForInput = null;

        //check if item already exists in the dictionary 
        if (s_dicValidation.TryGetValue(lInputHash,out tupOutputHashDetailsForInput) )
        {
            //check for tick collision
            if(tupOutputHashDetailsForInput.Item2 != iTick)
            {
                Debug.LogError($"collision for tick {iTick}, existing has at target is for tick:{tupOutputHashDetailsForInput.Item2}");

            }
            else if (tupOutputHashDetailsForInput.Item1 != lOutputHash) //compare results 
            {
                Debug.LogError($"Hash Did Not Match at tick {iTick} and execution point {bExecutionPoint}! Existing tag for hash: {tupOutputHashDetailsForInput.Item3.ToString()}New tag for hash:{strTagData.ToString()}");

                return false;
            }
        }
        else
        {
            //log the hash in the dictionary 
            s_dicValidation[lInputHash] = new Tuple<long, uint, string>(lOutputHash,iTick,strTagData);
            
            //add to list of inputs for tick to make it easier to remove later
            s_sltSortedListOfKeysForTicks.Add(iTick, lInputHash);
        }

        return true;
    }

    public static void ClearData()
    {
        s_dicValidation.Clear();
    }

    public static void ClearData(uint iTick)
    {
        long lKeyToRemove = 0;

        byte[] bTickBytes = BitConverter.GetBytes(iTick);
        byte[] bKeyBytes;

        bool bFound = false;

        foreach(KeyValuePair<long, Tuple<long, uint, string>> key in s_dicValidation)
        {
            
            //check the tick part of the hash code 
            if(key.Value.Item2 == iTick)
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

    public static void ClearDataBefore(uint iTick)
    {
        //List<long> lKeyToRemove = new List<long>();
        //
        //foreach (KeyValuePair<long, Tuple<long, uint, string>> key in s_dicValidation)
        //{
        //    //check the tick part of the hash code 
        //    if (key.Value.Item2 < iTick)
        //    {
        //        lKeyToRemove.Add(key.Key);
        //    }
        //}
        //
        //for(int i = 0; i < lKeyToRemove.Count; i++)
        //{ 
        //    s_dicValidation.Remove(lKeyToRemove[i]);
        //}

        int iValuesToRemove = 0;

        foreach (KeyValuePair<uint,long> kvpData in s_sltSortedListOfKeysForTicks)
        {
            if(kvpData.Key >= iTick)
            {
                break;
            }

            s_dicValidation.Remove(kvpData.Value);

            iValuesToRemove++;
        }

        for(int i = 0; i < iValuesToRemove; i++)
        {
            s_sltSortedListOfKeysForTicks.RemoveAt(0);
        }
    }


    /// <summary>
    /// Comparer for comparing two keys, handling equality as beeing greater
    /// Use this Comparer e.g. with SortedLists or SortedDictionaries, that don't allow duplicate keys
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    public class DuplicateKeyComparer<TKey>
                    :
                 IComparer<TKey> where TKey : IComparable
    {
        #region IComparer<TKey> Members

        public int Compare(TKey x, TKey y)
        {
            int result = x.CompareTo(y);

            if (result == 0)
                return 1; // Handle equality as being greater. Note: this will break Remove(key) or
            else          // IndexOfKey(key) since the comparer never returns 0 to signal key equality
                return result;
        }

        #endregion
    }

    protected static Dictionary<long, Tuple<long,uint, string>> s_dicValidation = new Dictionary<long, Tuple<long,uint, string>>();

    protected static SortedList<uint, long> s_sltSortedListOfKeysForTicks = new SortedList<uint, long>(new DuplicateKeyComparer<uint>());
    	
}
