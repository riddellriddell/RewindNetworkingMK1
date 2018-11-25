using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Sim
{
    public interface ISimProcess
    {
        bool PerformProcess(ConstData conConstantGameData, FrameData frmCurrentFrame, FrameData frmUpdatedFrame, int iTick);
    }
}