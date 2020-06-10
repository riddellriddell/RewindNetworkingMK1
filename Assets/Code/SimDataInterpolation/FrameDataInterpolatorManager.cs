using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using SharedTypes;

namespace SimDataInterpolation
{
    /// <summary>
    /// this class interpolates data from the simulation based off a time from game start and an offset per player 
    /// </summary>
    public partial class FrameDataInterpolatorManager<TFrameData, TErrorCorrectionSettings, TInterpolatedFrameData, TFrameDataInterpolator, TFrameDataProvider, TSimTimeProvider> 
        where TFrameDataInterpolator : IFrameDataInterpolator<TErrorCorrectionSettings, TFrameData, TInterpolatedFrameData>
        where TFrameData : Sim.IFrameData,  new()
        where TFrameDataProvider : ISimFrameDataProvider<TFrameData>
        where TSimTimeProvider : ISimTimeProvider
        where TInterpolatedFrameData : IInterpolatedFrameData<TFrameData>, new()
    {
        //the Frame data from last interpolation calculation 
        public TInterpolatedFrameData m_ifdSmoothedInterpolatedFrameData;

        //the frame data from the most recent Interpolation calculation
        public TInterpolatedFrameData m_ifdUnsmoothedLastFrameData;

        //temp value used to pull data from the sim 
        public TFrameData m_fdaFromFrameData;
        public TFrameData m_fdaToFrameData;

        //when was this date time calculation done
        public DateTime m_dtmTimeOfCurrentInterpCalc;

        //the base tick this was calculated from
        public uint m_iFromTickOfCurrentInterpCalc;

        //the tick the interpolated state was calculating to
        public uint m_iToTickOfCurrentInterpCalc;

        //the class that interpolates the frame data
        public IFrameDataInterpolator<TErrorCorrectionSettings, TFrameData, TInterpolatedFrameData> m_fdiFramDataInterpolator;
        
        //the simulation that this is being based off
        public TFrameDataProvider m_sfpSimFrameDataProvider;

        //TODO sepperate this out so in the future the interpolator can run on ofline games
        //network data bridge used to get the current game time
        //maybe use an interface 
        public TSimTimeProvider m_gtpSimTimeProvider;

        //the settings to use when correcting posittion errors caused by networking
        public TErrorCorrectionSettings m_ecsErrorCorrectionSettings;

        public DateTime m_dtmTimeOfLastUpdate;

        public FrameDataInterpolatorManager(
            TErrorCorrectionSettings ecsErrorCorrectionSettings, 
            TFrameDataProvider sfpSimFrameDataProvider,
            TFrameDataInterpolator fdiFramDataInterpolator,
            TSimTimeProvider stpSimTimeProvider)
        {
            //store settings 
            m_ecsErrorCorrectionSettings = ecsErrorCorrectionSettings;

            //store simulation
            m_sfpSimFrameDataProvider = sfpSimFrameDataProvider;

            m_fdiFramDataInterpolator = fdiFramDataInterpolator;

            m_gtpSimTimeProvider = stpSimTimeProvider;
                       
        }

        public void Initalize()
        {
            m_ifdUnsmoothedLastFrameData = new TInterpolatedFrameData();
            m_ifdSmoothedInterpolatedFrameData = new TInterpolatedFrameData();
            m_fdaToFrameData = new TFrameData();
            m_fdaFromFrameData = new TFrameData();
            m_dtmTimeOfLastUpdate = DateTime.UtcNow;
        }

        public void UpdateInterpolatedData()
        {
            float fDeltaTime = (float)(DateTime.UtcNow - m_dtmTimeOfLastUpdate).TotalSeconds;
            m_dtmTimeOfLastUpdate = DateTime.UtcNow;

            //get network time 
            DateTime dtmSimTime = m_gtpSimTimeProvider.GetCurrentSimTime();

            //get the new frame data at the old frame time
            m_sfpSimFrameDataProvider.GetInterpolationFrameData(m_dtmTimeOfCurrentInterpCalc, ref m_fdaFromFrameData, ref m_fdaToFrameData, out float fLerp);

            //make sure the interpolation frame data matches the new data 
            m_ifdSmoothedInterpolatedFrameData.MatchFormat(m_fdaToFrameData);
            m_ifdUnsmoothedLastFrameData.MatchFormat(m_fdaToFrameData);

            //calculate interpolate frame data at the same time as the last frame
            m_fdiFramDataInterpolator.CreateInterpolatedFrameData(m_fdaFromFrameData, m_fdaToFrameData, fLerp, ref m_ifdUnsmoothedLastFrameData);

            //get the interpolation errors between the old state and the new state and apply them to the new state
            m_fdiFramDataInterpolator.CalculateErrorOffsets(m_ifdSmoothedInterpolatedFrameData, m_ifdUnsmoothedLastFrameData, m_ecsErrorCorrectionSettings);

            //scale the error offsets down
            m_fdiFramDataInterpolator.ReduceOffsets(m_ifdSmoothedInterpolatedFrameData, fDeltaTime, m_ecsErrorCorrectionSettings);

            //get the new frame data for the new time
            m_sfpSimFrameDataProvider.GetInterpolationFrameData(dtmSimTime, ref m_fdaFromFrameData, ref m_fdaToFrameData, out fLerp);

            //calculate the new interpolated data 
            m_fdiFramDataInterpolator.CreateInterpolatedFrameData(m_fdaFromFrameData, m_fdaToFrameData, fLerp, ref m_ifdSmoothedInterpolatedFrameData);

            //apply error offsets
            m_fdiFramDataInterpolator.CalculateOffsetInterpolationData(m_ifdSmoothedInterpolatedFrameData);

            m_dtmTimeOfCurrentInterpCalc = dtmSimTime;
        }
    }
}
