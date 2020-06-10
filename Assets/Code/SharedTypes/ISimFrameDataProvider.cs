using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SharedTypes
{
    public interface ISimFrameDataProvider<TFrameData>
    {
        // simple version for prototype
        void GetInterpolationFrameData(DateTime dtmSimTime, ref TFrameData fdaOutFrom, ref TFrameData fdaOutToo, out float fOutLerp);

        //full version for multi time offset data display
        //bool GetInterpolationFrameData(
        //    DateTime dtmSimTime, 
        //    ValueTuple<byte, byte>[] bSortdOffsetsForChannel, 
        //    ref FrameData[] fdaOutFrame, 
        //    ref DateTime dtmFrameTime[], 
        //    ref byte[] bFromIndexForChannel, 
        //    ref float[] fLerpPercentPerChannel);
    }
}
