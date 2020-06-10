using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace SimDataInterpolation
{
    public interface IInterpolatedFrameData<TFrameData>
    {
        void MatchFormat(TFrameData fdaFrameData);
    }
}