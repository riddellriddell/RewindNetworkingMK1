
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Generated Code do not edit!!!!
namespace Sim
{
	public partial class FrameDataInterpolator
	{	

		public InterpolatedFrameDataGen CalculateErrorOffsets(InterpolatedFrameDataGen ifdOldFrameData, InterpolatedFrameDataGen ifdNewFrameData)
		{
			//check input
			if(ifdOldFrameData == null)
			{
				return ifdNewFrameData;
			}
			
			if(ifdNewFrameData == null)
			{
				return ifdOldFrameData;
			}

			if(m_ecsErrorCorrectionSettings.m_bEnableInterpolation == false)
			{
				return ifdNewFrameData;
			}

			//loop throuhg all variables and calculate the difference 
			for(int i = 0 ; i < ifdOldFrameData.m_sPlayerHealths.Count; i++)
			{
				ifdOldFrameData.m_sPlayerHealthsErrorOffset[i] += (System.Single) (ifdNewFrameData.m_sPlayerHealths[i] - ifdOldFrameData.m_sPlayerHealths[i]);
			}

			for(int i = 0 ; i < ifdOldFrameData.m_v2iPosition.Count; i++)
			{
				ifdOldFrameData.m_v2iPositionErrorOffset[i] += (UnityEngine.Vector2) (ifdNewFrameData.m_v2iPosition[i] - ifdOldFrameData.m_v2iPosition[i]);
			}

			return ifdOldFrameData;

		}

		public InterpolatedFrameDataGen CalculateOffsetInterpolationData(InterpolatedFrameDataGen ifdFrameData)
		{
					//loop throuhg all variables and calculate the difference 
			for(int i = 0 ; i < ifdFrameData.m_sPlayerHealths.Count; i++)
			{
				ifdFrameData.m_sPlayerHealthsErrorAdjusted[i] = ifdFrameData.m_sPlayerHealths[i] -  (System.Int32) (ifdFrameData.m_sPlayerHealthsErrorOffset[i]);
			}

			for(int i = 0 ; i < ifdFrameData.m_v2iPosition.Count; i++)
			{
				ifdFrameData.m_v2iPositionErrorAdjusted[i] = ifdFrameData.m_v2iPosition[i] -  (UnityEngine.Vector2) (ifdFrameData.m_v2iPositionErrorOffset[i]);
			}

			return ifdFrameData;
		}

		public  InterpolatedFrameDataGen ReduceOffsets(InterpolatedFrameDataGen ifdFrameData, float fDeltaTime, InterpolationErrorCorrectionSettings ecsErrorCorrectionSetting)
		{
			for(int i = 0 ; i < ifdFrameData.m_sPlayerHealths.Count; i++)
			{
				ifdFrameData.m_sPlayerHealthsErrorOffset[i] *= CalculateErrorScalingAmount(ifdFrameData.m_sPlayerHealthsErrorOffset[i],fDeltaTime,ecsErrorCorrectionSetting.m_sPlayerHealthsErrorCorrectionSetting );
			}

			for(int i = 0 ; i < ifdFrameData.m_v2iPosition.Count; i++)
			{
				ifdFrameData.m_v2iPositionErrorOffset[i] *= CalculateErrorScalingAmount(ifdFrameData.m_v2iPositionErrorOffset[i].magnitude,fDeltaTime,ecsErrorCorrectionSetting.m_v2iPositionErrorCorrectionSetting );
			}

			return ifdFrameData;
		}

		public InterpolatedFrameDataGen CreateInterpolatedFrameData(GameSimulation gsmSim, float fBaseTime, List<float> fTargetInterpTimeOffsetForPlayer = null,InterpolatedFrameDataGen ifdOutput = null)
        {
            //check input data
            if(ifdOutput == null || ifdOutput.PlayerCount != gsmSim.m_frmDenseFrameQueue[gsmSim.m_iDenseQueueHead].PlayerCount)
            {
                ifdOutput = new InterpolatedFrameDataGen(gsmSim.m_frmDenseFrameQueue[gsmSim.m_iDenseQueueHead].PlayerCount);
            }

			//interpolate all the non list frame data (time remaining, and other world level variables 

			//get the last tick for target time 
			int iInterpTick = GetLastTick(gsmSim,fBaseTime);

			//get the interpolation frame variables 
			FrameData[] fdaInterpFramePair = GetInterpolationPair(gsmSim,iInterpTick);

			//get the interpolation percent
			float fInterp = GetInterplationBetweenTicks(gsmSim,fBaseTime);


			//loop throuhg all the interpolation variables

			//loop through all the players 
			for(int i = 0; i < ifdOutput.PlayerCount; i++)
			{
				//get offset from base time 
				float fOffsetTime = fBaseTime;

				//check to see if there is an offset array 
				if(fTargetInterpTimeOffsetForPlayer != null && fTargetInterpTimeOffsetForPlayer.Count > i)
				{
					fOffsetTime += fTargetInterpTimeOffsetForPlayer[i];
				}

				//get frame index 
				int iTargetIndex = GetLastTick(gsmSim,fOffsetTime);

				if(iTargetIndex != iInterpTick)
				{
					iInterpTick = iTargetIndex;

					//get the interpolation frame variables 
					fdaInterpFramePair = GetInterpolationPair(gsmSim,iInterpTick);
				}

				//Interpolate all the per player variables
				ifdOutput.m_sPlayerHealths[i] = (System.Int32)( ((System.Int32)(fdaInterpFramePair[0].m_sPlayerHealths[i]) * (1 - fInterp)) +  ((System.Int32)(fdaInterpFramePair[1].m_sPlayerHealths[i]) * fInterp));
						
				ifdOutput.m_v2iPosition[i] = (UnityEngine.Vector2)( ((UnityEngine.Vector2)(fdaInterpFramePair[0].m_v2iPosition[i]) * (1 - fInterp)) +  ((UnityEngine.Vector2)(fdaInterpFramePair[1].m_v2iPosition[i]) * fInterp));
						
			
				ifdOutput.m_bFaceDirection[i] = (System.Byte) fdaInterpFramePair[0].m_bFaceDirection[i];

			
				ifdOutput.m_bPlayerState[i] = (System.Byte) fdaInterpFramePair[0].m_bPlayerState[i];

			
				ifdOutput.m_sStateEventTick[i] = (System.Single) fdaInterpFramePair[0].m_sStateEventTick[i];

			
				ifdOutput.m_bScore[i] = (System.Int32) fdaInterpFramePair[0].m_bScore[i];


			}
		
            return ifdOutput;
        }
	}
}