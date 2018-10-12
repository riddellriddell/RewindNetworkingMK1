using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ISimProcess
{
    bool PerformProcess(ConstData conConstantGameData, FrameData frmCurrentFrame, FrameData frmUpdatedFrame,int iTick);
}
