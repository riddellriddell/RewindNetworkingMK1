using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Networking;
using System;

namespace Sim
{
    /// <summary>
    /// this class interpolates data from the simulation based off a time from game start and an offset per player 
    /// </summary>
    public partial class FrameDataInterpolatorManager<TFrameData,TConstData,TSettingsData> where TFrameData : Sim.IFrameData,  new()
    {
        //has a new input caused an error in prediction 
        public bool m_bCalculatePredictionError;
 
        public List<float> m_fTargetInterpTimeOffsetForPlayer;

        //the time of the last interpolated data calculation
        public float m_fTimeOfLastInterpolatedDataCalculation;

        //the current frame data 
        public InterpolatedFrameDataGen m_ifdInterpolatedFrameData;

        //the simulation that this is being based off
        public TestingSimManager<TFrameData, TConstData, TSettingsData> m_tsmSimManager;

        //TODO sepperate this out so in the future the interpolator can run on ofline games
        //network data bridge used to get the current game time
        public NetworkingDataBridge m_ndbNetworkingDataBridge;

        //the settings to use when correcting posittion errors caused by networking
        public InterpolationErrorCorrectionSettings m_ecsErrorCorrectionSettings;

        public FrameDataInterpolatorManager( InterpolationErrorCorrectionSettings ecsErrorCorrectionSettings, TestingSimManager<TFrameData, TConstData, TSettingsData> tsmSimManager)
        {
            //store settings 
            m_ecsErrorCorrectionSettings = ecsErrorCorrectionSettings;

            //store simulation
            m_tsmSimManager = tsmSimManager;
          
        }

        public void Initalize(int iMaxPlayers)
        {
            //setup time offset list
            m_fTargetInterpTimeOffsetForPlayer = new List<float>();
            for (int i = 0; i < iMaxPlayers; i++)
            {
                m_fTargetInterpTimeOffsetForPlayer.Add(0f);
            }

            //setup inital frame data state 
            //m_ifdInterpolatedFrameData = CreateInterpolatedFrameData(, 0);
        }

         ////get the last frame number before the game time 
         //public int GetLastTick(GameSimulation gsmSim, float fGameTime)
         //{
         //    //get frame delta
         //    float fSimTickDelta = (float)gsmSim.m_setGameSettings.TickDelta.FixValue;
         //
         //    //calculate the number of frames that have occured by this time 
         //    int iTotalFramesCalculated = Mathf.FloorToInt(fGameTime / fSimTickDelta);
         //
         //    return iTotalFramesCalculated;
         //}

        //get interpolation percent between ticks from 0 to 1
        public void GetTickInterpolationData(TestingSimManager<TFrameData, TConstData, TSettingsData> tsmSimManager, DateTime dtmGameTime, out uint iFromTick,out uint iToTick, out float fInterpValue)
        {
            //get current game time 
            iToTick = tsmSimManager.ConvertDateTimeToTick(dtmGameTime);

            iFromTick = (iToTick != 0) ? iToTick - 1 : 0;

            TimeSpan tspTickDelta = dtmGameTime - tsmSimManager.ConvertSimTickToDateTime(dtmGameTime, iFromTick);

            fInterpValue = (float)tspTickDelta.Ticks / (float)TestingSimManager<TFrameData, TConstData, TSettingsData>.s_lSimTickLenght;
        }

        //returns an array holding 2 frames of data one at or closest to the base tick and one after or the same as the base tick depending on what data is available 
        //the first entry is the oldest the second the youngest
        //public FrameData[] GetInterpolationPair(GameSimulation gsmSim, int iBaseTick)
        //{
        //    //clamp base to available range 
        //    int iClampedBase = Mathf.Max(Mathf.Min(iBaseTick, gsmSim.m_iLatestTick), gsmSim.m_iOldestTickInBuffer);
        //    int iClampedInterpolateTo = Mathf.Min(iClampedBase + 1, gsmSim.m_iLatestTick);
        //
        //    FrameData[] fdaOutput = new FrameData[2];
        //
        //    fdaOutput[0] = gsmSim.GetFrameData(iClampedBase);
        //    fdaOutput[1] = gsmSim.GetFrameData(iClampedInterpolateTo);
        //
        //    return fdaOutput;
        //}



        public void UpdateInterpolatedDataForTime(float fBaseTime, float fDeltaTime)
        {
            //check if errors need to be recalculated 
            //if (m_bCalculatePredictionError)
            //{
            //    //recalculate new interpolation data for the same time as the last interpolated data calculation 
            //    InterpolatedFrameDataGen ifdNewInterpData = CreateInterpolatedFrameData(m_gsmSourceSimulation, m_fTimeOfLastInterpolatedDataCalculation, m_fTargetInterpTimeOffsetForPlayer);
            //
            //    //compare interpolation data for the same frame to calculate error 
            //    m_ifdInterpolatedFrameData = CalculateErrorOffsets(m_ifdInterpolatedFrameData, ifdNewInterpData);
            //
            //    //indicate that offsets have been calculated 
            //    m_bCalculatePredictionError = false;
            //}
            //
            ////calculate latest interpolated position
            //m_ifdInterpolatedFrameData = CreateInterpolatedFrameData(m_gsmSourceSimulation, fBaseTime, m_fTargetInterpTimeOffsetForPlayer, m_ifdInterpolatedFrameData);
            //
            ////scale error hiding
            //m_ifdInterpolatedFrameData = ReduceOffsets(m_ifdInterpolatedFrameData, fDeltaTime, m_ecsErrorCorrectionSettings);
            //
            ////calculate latest interpolated position with offsets applied 
            //m_ifdInterpolatedFrameData = CalculateOffsetInterpolationData(m_ifdInterpolatedFrameData);
            //
            ////set the time of the last interpolation calculation
            //m_fTimeOfLastInterpolatedDataCalculation = fBaseTime;
        }
    }
}
