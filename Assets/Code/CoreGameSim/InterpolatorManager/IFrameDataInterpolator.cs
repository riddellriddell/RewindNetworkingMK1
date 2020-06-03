using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IFrameDataInterpolator<TInterpolatedFrameData>
{
    TInterpolatedFrameData InterpolatedFrameData();
}
