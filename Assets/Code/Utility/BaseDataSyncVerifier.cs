using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BaseDataSyncVerifier<TTimeStamp, TID, TDataType> where TTimeStamp : IComparable
{
    public Dictionary<TTimeStamp, Tuple<long, List<TID>, TDataType>> HashForDataAtTimme { get; } = new Dictionary<TTimeStamp, Tuple<long, List<TID>, TDataType>>();

    public struct Result
    {
        public Result(bool bDidSucceed, List<TID> lstConflicts)
        {
            m_bSuccess = bDidSucceed;
            m_lstConflictingEntries = lstConflicts;
        }

        public bool m_bSuccess;
        public List<TID> m_lstConflictingEntries;
    }
    
    public Result RegisterData(long lDataHash, TDataType tdtData, TTimeStamp ttsTimeStamp, TID tidID)
    {

        if (HashForDataAtTimme.TryGetValue(ttsTimeStamp, out Tuple<long, List<TID>, TDataType> tupDataEnrey))
        {
            if (tupDataEnrey.Item1 != lDataHash)
            {
                Result rstConflictResult = new Result(false, tupDataEnrey.Item2);
                
                return rstConflictResult;

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
        
        return new Result(true,null);
    }

    public void CleanUpOldEntries(TTimeStamp ttsTimeOutTime)
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
