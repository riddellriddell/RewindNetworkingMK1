using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Networking
{
    //public struct GlobalChainLinkSortingValue : IComparer<GlobalChainLinkSortingValue>
    //{
    //    public const int c_iTotalBits = 64;
    //    public const int c_iBitsForCyclesIndex = 32;
        
    //    public const int c_iBitsForHash = 32;
    //    public const ulong c_lBitMaskForHash = ~(ulong.MaxValue << c_iBitsForHash);

    //    public static GlobalChainLinkSortingValue MinValue
    //    {
    //        get
    //        {
    //            return new GlobalChainLinkSortingValue()
    //            {
    //                m_lSortValue = ulong.MinValue
    //            };
    //        }
    //    }

    //    public static GlobalChainLinkSortingValue Maxlue
    //    {
    //        get
    //        {
    //            return new GlobalChainLinkSortingValue()
    //            {
    //                m_lSortValue = ulong.MaxValue
    //            };
    //        }
    //    }

    //    public ulong m_lSortValueA;

    //    public ulong m_lSortValueB;

    //    public GlobalChainLinkSortingValue(ChainLink chlLink)
    //    {
    //        m_lSortValue = chlLink.m_iCyclesIndex << c_iBitsForHash;

    //        ulong lHashValue = 0;

    //        for(int i = 0; i < ((c_iBitsForHash / 8 ) + 1) && i < chlLink.m_sigSignatureData.m_bHashOfChainLink.Length; i++)
    //        {
    //            lHashValue = lHashValue << 8;

    //            lHashValue += chlLink.m_sigSignatureData.m_bHashOfChainLink[i];
    //        }

    //        m_lSortValue = m_lSortValue | (lHashValue & c_lBitMaskForHash);
    //    }

    //    public int Compare(GlobalChainLinkSortingValue x, GlobalChainLinkSortingValue y)
    //    {
    //        if(x.m_lSortValue > y.m_lSortValue)
    //        {
    //            return 1;
    //        }
    //        else if(x.m_lSortValue < y.m_lSortValue)
    //        {
    //            return -1;
    //        }

    //        return 0;
    //    }
    //}
}
