using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BaseDataSyncVerifier<TTimeStamp, TID, TDataType> where TTimeStamp : IComparable
{
    protected static Dictionary<TTimeStamp, Tuple<long, List<TID>, TDataType>> HashForDataAtTimme { get; } = new Dictionary<TTimeStamp, Tuple<long, List<TID>, TDataType>>();

    protected static void RegisterData(long lDataHash, TDataType tdtData, TTimeStamp ttsTimeStamp, TID tidID)
    {

        if (HashForDataAtTimme.TryGetValue(ttsTimeStamp, out Tuple<long, List<TID>, TDataType> tupDataEnrey))
        {
            if (tupDataEnrey.Item1 != lDataHash)
            {
                Debug.LogError($"New data entry hash does not match existing entry for datapoint at timestamp {ttsTimeStamp}");
            }
            else
            {
                tupDataEnrey.Item2.Add(tidID);
            }
        }
        else
        {
            List<TID> tidIDList = new List<TID>();

            tidIDList.Add(tidID);

            Tuple<long, List<TID>, TDataType> tupEntry = new Tuple<long, List<TID>, TDataType>(lDataHash, tidIDList, tdtData);

            HashForDataAtTimme.Add(ttsTimeStamp, tupEntry);
        }
    }

    protected static void CleanUpOldEntries(TTimeStamp ttsTimeOutTime)
    {
        List<TTimeStamp> ttsTimesToRemove = new List<TTimeStamp>();

        foreach(TTimeStamp ttsTime in HashForDataAtTimme.Keys)
        {
            if(ttsTime.CompareTo(ttsTimeOutTime) < 0)
            {
                ttsTimesToRemove.Add(ttsTime);
            }
        }

        for (int i = 0; i < ttsTimesToRemove.Count; i++)
        {
            HashForDataAtTimme.Remove(ttsTimesToRemove[i]);
        }
    }

}
