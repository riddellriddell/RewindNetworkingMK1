using Networking;
using Sim;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;
using Utility;

public class TestingSimDataSyncVerifier<TFrameData> : BaseDataSyncVerifier<uint,int,TFrameData> where TFrameData : IFrameData
{
   public static void VerifyData(uint iTick,ref TFrameData fdaData, int iID,int iTrackingWindowSize)
   {
        int iSize =  fdaData.GetSize();

        WriteByteStream wbsNetworking = new WriteByteStream(iSize);

        fdaData.Encode(wbsNetworking);

        long lHash = 0;

        using (MD5 md5 = MD5.Create())
        {
            lHash = BitConverter.ToInt64(md5.ComputeHash(wbsNetworking.GetData()), 0);
        }

        RegisterData(lHash, fdaData, iTick, iID);

        CleanUpOldEntries(iTick - (uint)(iTrackingWindowSize));
   }
}
