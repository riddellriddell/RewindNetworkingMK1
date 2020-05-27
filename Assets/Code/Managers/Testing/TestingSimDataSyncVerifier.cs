using Networking;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;

public class TestingSimDataSyncVerifier : BaseDataSyncVerifier<uint,int,TestingSimManager.SimState>
{
   public static void VerifyData(uint iTick,ref TestingSimManager.SimState sstData, int iID)
   {
        int iSize = TestingSimManager.SimState.SizeOfSimState(sstData);

        WriteByteStream wbsNetworking = new WriteByteStream(iSize);

        TestingSimManager.SimState.EncodeSimState(wbsNetworking, ref sstData);

        long lHash = 0;

        using (MD5 md5 = MD5.Create())
        {
            lHash = BitConverter.ToInt64(md5.ComputeHash(wbsNetworking.GetData()), 0);
        }

        RegisterData(lHash, sstData, iTick, iID);

        CleanUpOldEntries(iTick - (uint)(TestingSimManager.s_TicksPerSecond * 2));
   }
}
