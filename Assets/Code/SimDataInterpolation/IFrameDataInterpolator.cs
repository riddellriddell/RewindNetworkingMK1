using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SimDataInterpolation
{
    //interface for frame interpolators
    public interface IFrameDataInterpolator<TErrorCorrectionSettings, TFrameData, TInterpolatedFrameData>
    {
        TInterpolatedFrameData CalculateErrorOffsets(TInterpolatedFrameData ifdOldFrameData, TInterpolatedFrameData ifdNewFrameData, TErrorCorrectionSettings ecsErrorCorrectionSetting);

        TInterpolatedFrameData CalculateOffsetInterpolationData(TInterpolatedFrameData ifdFrameData);

        TInterpolatedFrameData ReduceOffsets(TInterpolatedFrameData ifdFrameData, float fDeltaTime, TErrorCorrectionSettings ecsErrorCorrectionSetting);

        void CreateInterpolatedFrameData(in TFrameData fdaFromFrame, in TFrameData fdaToFrame, float fInterpolation, ref TInterpolatedFrameData ifdInterpolatedFrameData);

    }
}