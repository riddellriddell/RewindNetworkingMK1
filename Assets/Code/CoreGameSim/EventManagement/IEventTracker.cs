using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Sim
{
    public interface IEventTracker
    {
        void SetupForFrame();

        void Clear(int iIndex);

        void ApplyFrameEvents(int iIndex, byte bResimCount, bool bFirstSimOfTick);
    }


}
