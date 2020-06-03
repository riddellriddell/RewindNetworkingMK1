using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FixedPointy;

namespace Sim
{
    public class DataInterpolation
    {

        ////get the last frame number before the game time 
        //public int GetLastTick(GameSimulation gsmSim ,float fGameTime)
        //{
        //    //get frame delta
        //    float fSimTickDelta = (float)gsmSim.m_setGameSettings.TickDelta.FixValue;
        //
        //    //calculate the number of frames that have occured by this time 
        //    int iTotalFramesCalculated = Mathf.FloorToInt(fGameTime / fSimTickDelta);
        //
        //    return iTotalFramesCalculated;
        //}
        //
       ////get interpolation percent between ticks from 0 to 1
        //public float GetInterplationBetweenTicks(GameSimulation gsmSim, float fGameTime)
        //{
        //    //get frame delta
        //    float fSimTickDelta = (float)gsmSim.m_setGameSettings.TickDelta.FixValue;
        //
        //    //calculate interpolation value 
        //    return (fGameTime % fSimTickDelta) / fSimTickDelta;
        //}
        //
        ////returns an array holding 2 frames of data one at or closest to the base tick and one after or the same as the base tick depending on what data is available 
        ////the first entry is the oldest the second the youngest
        //public FrameData[] GetInterpolationPair(GameSimulation gsmSim,int iBaseTick)
        //{
        //    //clamp base to available range 
        //    int iClampedBase = Mathf.Max(iBaseTick, gsmSim.m_iOldestTickInBuffer);
        //    int iClampedInterpolateTo = Mathf.Min(iClampedBase, gsmSim.m_iLatestTick);
        //
        //    FrameData[] fdaOutput = new FrameData[2];
        //
        //    fdaOutput[0] = gsmSim.GetFrameData(iClampedBase);
        //    fdaOutput[1] = gsmSim.GetFrameData(iClampedInterpolateTo);
        //
        //    return fdaOutput;
        //}
        //
        ////Get the interpolated position of an object
        //public Vector2 GetInterpolatedPosition(GameSimulation gsmSim, int iPlayerIndex,float fTime)
        //{            
        //    //calculate the frame time 
        //    int iTick = GetLastTick(gsmSim, fTime);
        //
        //    //calculate the interpolation value
        //    float fInterpolationValue = GetInterplationBetweenTicks(gsmSim, fTime);
        //
        //    //get the 2 frames to interpolate between
        //    FrameData[] fdrInterpolationPair = GetInterpolationPair(gsmSim, iTick);
        //
        //    //return the interpolated value
        //    return Vector2.Lerp((Vector2)fdrInterpolationPair[0].m_v2iPosition[iPlayerIndex], (Vector2)fdrInterpolationPair[1].m_v2iPosition[iPlayerIndex],fInterpolationValue);
        //
        //}
        //
        //public InterpolatedFrameData CreateInterpolatedData(GameSimulation gsmSim, List<float> fTargetTime,InterpolatedFrameData itdExistingInterpolatedDataData = null)
        //{
        //    //check input data
        //    if(itdExistingInterpolatedDataData == null || itdExistingInterpolatedDataData.PlayerCount != gsmSim.m_frmDenseFrameQueue[gsmSim.m_iDenseQueueHead].PlayerCount)
        //    {
        //        itdExistingInterpolatedDataData = new InterpolatedFrameData(gsmSim.m_frmDenseFrameQueue[gsmSim.m_iDenseQueueHead].PlayerCount);
        //    }
        //
        //    return null;
        //}
    }
}
