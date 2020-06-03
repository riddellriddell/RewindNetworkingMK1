
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Generated Code do not edit!!!!
namespace Sim
{
	public class FrameDataInterpolator : FrameDataInterpolatorBase<InterpolationErrorCorrectionSettings, FrameData>
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
			for(int i = 0 ; i < ifdOldFrameData.m_bShipHealth.Length; i++)
			{
				ifdOldFrameData.m_bShipHealthErrorOffset[i] += (System.Single) (ifdNewFrameData.m_bShipHealth[i] - ifdOldFrameData.m_bShipHealth[i]);
			}

			for(int i = 0 ; i < ifdOldFrameData.m_fixShipHealDelayTimeOut.Length; i++)
			{
				ifdOldFrameData.m_fixShipHealDelayTimeOutErrorOffset[i] += (System.Single) (ifdNewFrameData.m_fixShipHealDelayTimeOut[i] - ifdOldFrameData.m_fixShipHealDelayTimeOut[i]);
			}

			for(int i = 0 ; i < ifdOldFrameData.m_fixShipPosX.Length; i++)
			{
				ifdOldFrameData.m_fixShipPosXErrorOffset[i] += (System.Single) (ifdNewFrameData.m_fixShipPosX[i] - ifdOldFrameData.m_fixShipPosX[i]);
			}

			for(int i = 0 ; i < ifdOldFrameData.m_fixShipPosY.Length; i++)
			{
				ifdOldFrameData.m_fixShipPosYErrorOffset[i] += (System.Single) (ifdNewFrameData.m_fixShipPosY[i] - ifdOldFrameData.m_fixShipPosY[i]);
			}

			for(int i = 0 ; i < ifdOldFrameData.m_fixShipVelocityX.Length; i++)
			{
				ifdOldFrameData.m_fixShipVelocityXErrorOffset[i] += (System.Single) (ifdNewFrameData.m_fixShipVelocityX[i] - ifdOldFrameData.m_fixShipVelocityX[i]);
			}

			for(int i = 0 ; i < ifdOldFrameData.m_fixShipVelocityY.Length; i++)
			{
				ifdOldFrameData.m_fixShipVelocityYErrorOffset[i] += (System.Single) (ifdNewFrameData.m_fixShipVelocityY[i] - ifdOldFrameData.m_fixShipVelocityY[i]);
			}

			for(int i = 0 ; i < ifdOldFrameData.m_fixShipBaseAngle.Length; i++)
			{
				ifdOldFrameData.m_fixShipBaseAngleErrorOffset[i] += (System.Single) (ifdNewFrameData.m_fixShipBaseAngle[i] - ifdOldFrameData.m_fixShipBaseAngle[i]);
			}

			return ifdOldFrameData;

		}

		public InterpolatedFrameDataGen CalculateOffsetInterpolationData(InterpolatedFrameDataGen ifdFrameData)
		{
					//loop throuhg all variables and calculate the difference 
			for(int i = 0 ; i < ifdFrameData.m_bShipHealth.Length; i++)
			{
				ifdFrameData.m_bShipHealthErrorAdjusted[i] = (System.Byte)(ifdFrameData.m_bShipHealth[i] - ifdFrameData.m_bShipHealthErrorOffset[i]);
			}

			for(int i = 0 ; i < ifdFrameData.m_fixShipHealDelayTimeOut.Length; i++)
			{
				ifdFrameData.m_fixShipHealDelayTimeOutErrorAdjusted[i] = (System.Single)(ifdFrameData.m_fixShipHealDelayTimeOut[i] - ifdFrameData.m_fixShipHealDelayTimeOutErrorOffset[i]);
			}

			for(int i = 0 ; i < ifdFrameData.m_fixShipPosX.Length; i++)
			{
				ifdFrameData.m_fixShipPosXErrorAdjusted[i] = (System.Single)(ifdFrameData.m_fixShipPosX[i] - ifdFrameData.m_fixShipPosXErrorOffset[i]);
			}

			for(int i = 0 ; i < ifdFrameData.m_fixShipPosY.Length; i++)
			{
				ifdFrameData.m_fixShipPosYErrorAdjusted[i] = (System.Single)(ifdFrameData.m_fixShipPosY[i] - ifdFrameData.m_fixShipPosYErrorOffset[i]);
			}

			for(int i = 0 ; i < ifdFrameData.m_fixShipVelocityX.Length; i++)
			{
				ifdFrameData.m_fixShipVelocityXErrorAdjusted[i] = (System.Single)(ifdFrameData.m_fixShipVelocityX[i] - ifdFrameData.m_fixShipVelocityXErrorOffset[i]);
			}

			for(int i = 0 ; i < ifdFrameData.m_fixShipVelocityY.Length; i++)
			{
				ifdFrameData.m_fixShipVelocityYErrorAdjusted[i] = (System.Single)(ifdFrameData.m_fixShipVelocityY[i] - ifdFrameData.m_fixShipVelocityYErrorOffset[i]);
			}

			for(int i = 0 ; i < ifdFrameData.m_fixShipBaseAngle.Length; i++)
			{
				ifdFrameData.m_fixShipBaseAngleErrorAdjusted[i] = (System.Single)(ifdFrameData.m_fixShipBaseAngle[i] - ifdFrameData.m_fixShipBaseAngleErrorOffset[i]);
			}

			return ifdFrameData;
		}

		public  InterpolatedFrameDataGen ReduceOffsets(InterpolatedFrameDataGen ifdFrameData, float fDeltaTime, InterpolationErrorCorrectionSettings ecsErrorCorrectionSetting)
		{
			for(int i = 0 ; i < ifdFrameData.m_bShipHealth.Length; i++)
			{
				ifdFrameData.m_bShipHealthErrorOffset[i] *= CalculateErrorScalingAmount(ifdFrameData.m_bShipHealthErrorOffset[i],fDeltaTime,ecsErrorCorrectionSetting.m_bShipHealthErrorCorrectionSetting );
			}

			for(int i = 0 ; i < ifdFrameData.m_fixShipHealDelayTimeOut.Length; i++)
			{
				ifdFrameData.m_fixShipHealDelayTimeOutErrorOffset[i] *= CalculateErrorScalingAmount(ifdFrameData.m_fixShipHealDelayTimeOutErrorOffset[i],fDeltaTime,ecsErrorCorrectionSetting.m_fixShipHealDelayTimeOutErrorCorrectionSetting );
			}

			for(int i = 0 ; i < ifdFrameData.m_fixShipPosX.Length; i++)
			{
				ifdFrameData.m_fixShipPosXErrorOffset[i] *= CalculateErrorScalingAmount(ifdFrameData.m_fixShipPosXErrorOffset[i],fDeltaTime,ecsErrorCorrectionSetting.m_fixShipPosXErrorCorrectionSetting );
			}

			for(int i = 0 ; i < ifdFrameData.m_fixShipPosY.Length; i++)
			{
				ifdFrameData.m_fixShipPosYErrorOffset[i] *= CalculateErrorScalingAmount(ifdFrameData.m_fixShipPosYErrorOffset[i],fDeltaTime,ecsErrorCorrectionSetting.m_fixShipPosYErrorCorrectionSetting );
			}

			for(int i = 0 ; i < ifdFrameData.m_fixShipVelocityX.Length; i++)
			{
				ifdFrameData.m_fixShipVelocityXErrorOffset[i] *= CalculateErrorScalingAmount(ifdFrameData.m_fixShipVelocityXErrorOffset[i],fDeltaTime,ecsErrorCorrectionSetting.m_fixShipVelocityXErrorCorrectionSetting );
			}

			for(int i = 0 ; i < ifdFrameData.m_fixShipVelocityY.Length; i++)
			{
				ifdFrameData.m_fixShipVelocityYErrorOffset[i] *= CalculateErrorScalingAmount(ifdFrameData.m_fixShipVelocityYErrorOffset[i],fDeltaTime,ecsErrorCorrectionSetting.m_fixShipVelocityYErrorCorrectionSetting );
			}

			for(int i = 0 ; i < ifdFrameData.m_fixShipBaseAngle.Length; i++)
			{
				ifdFrameData.m_fixShipBaseAngleErrorOffset[i] *= CalculateErrorScalingAmount(ifdFrameData.m_fixShipBaseAngleErrorOffset[i],fDeltaTime,ecsErrorCorrectionSetting.m_fixShipBaseAngleErrorCorrectionSetting );
			}

			return ifdFrameData;
		}

		public void CreateInterpolatedFrameData(in FrameData fdaFromFrame,in FrameData fdaToFrame,float fInterpolation, ref InterpolatedFrameDataGen ifdInterpolatedFrameData)
        {
			//loop throuhg all the non time offset interpolation variables
			//that are not in arrays 
			for(int i = 0 ; i < ifdInterpolatedFrameData.m_lPeersAssignedToSlot.Length ; i++)
			{

					ifdInterpolatedFrameData.m_lPeersAssignedToSlot[i] = (System.Single) fdaToFrame.m_lPeersAssignedToSlot[i];

			}
			for(int i = 0 ; i < ifdInterpolatedFrameData.m_bInput.Length ; i++)
			{

					ifdInterpolatedFrameData.m_bInput[i] = (System.Int32) fdaToFrame.m_bInput[i];

			}
			for(int i = 0 ; i < ifdInterpolatedFrameData.m_bShipHealth.Length ; i++)
			{

					ifdInterpolatedFrameData.m_bShipHealth[i] = (System.Byte)( ((System.Byte)(fdaFromFrame.m_bShipHealth[i]) * (1 - fInterpolation)) +  ((System.Byte)(fdaToFrame.m_bShipHealth[i]) * fInterpolation));
						
			}
			for(int i = 0 ; i < ifdInterpolatedFrameData.m_fixShipHealDelayTimeOut.Length ; i++)
			{

					ifdInterpolatedFrameData.m_fixShipHealDelayTimeOut[i] = (System.Single)( ((System.Single)(fdaFromFrame.m_fixShipHealDelayTimeOut[i]) * (1 - fInterpolation)) +  ((System.Single)(fdaToFrame.m_fixShipHealDelayTimeOut[i]) * fInterpolation));
						
			}
			for(int i = 0 ; i < ifdInterpolatedFrameData.m_bShipLastDamagedBy.Length ; i++)
			{

					ifdInterpolatedFrameData.m_bShipLastDamagedBy[i] = (System.Byte) fdaToFrame.m_bShipLastDamagedBy[i];

			}
			for(int i = 0 ; i < ifdInterpolatedFrameData.m_fixShipPosX.Length ; i++)
			{

					ifdInterpolatedFrameData.m_fixShipPosX[i] = (System.Single)( ((System.Single)(fdaFromFrame.m_fixShipPosX[i]) * (1 - fInterpolation)) +  ((System.Single)(fdaToFrame.m_fixShipPosX[i]) * fInterpolation));
						
			}
			for(int i = 0 ; i < ifdInterpolatedFrameData.m_fixShipPosY.Length ; i++)
			{

					ifdInterpolatedFrameData.m_fixShipPosY[i] = (System.Single)( ((System.Single)(fdaFromFrame.m_fixShipPosY[i]) * (1 - fInterpolation)) +  ((System.Single)(fdaToFrame.m_fixShipPosY[i]) * fInterpolation));
						
			}
			for(int i = 0 ; i < ifdInterpolatedFrameData.m_fixShipVelocityX.Length ; i++)
			{

					ifdInterpolatedFrameData.m_fixShipVelocityX[i] = (System.Single)( ((System.Single)(fdaFromFrame.m_fixShipVelocityX[i]) * (1 - fInterpolation)) +  ((System.Single)(fdaToFrame.m_fixShipVelocityX[i]) * fInterpolation));
						
			}
			for(int i = 0 ; i < ifdInterpolatedFrameData.m_fixShipVelocityY.Length ; i++)
			{

					ifdInterpolatedFrameData.m_fixShipVelocityY[i] = (System.Single)( ((System.Single)(fdaFromFrame.m_fixShipVelocityY[i]) * (1 - fInterpolation)) +  ((System.Single)(fdaToFrame.m_fixShipVelocityY[i]) * fInterpolation));
						
			}
			for(int i = 0 ; i < ifdInterpolatedFrameData.m_fixShipBaseAngle.Length ; i++)
			{

					ifdInterpolatedFrameData.m_fixShipBaseAngle[i] = (System.Single)( ((System.Single)(fdaFromFrame.m_fixShipBaseAngle[i]) * (1 - fInterpolation)) +  ((System.Single)(fdaToFrame.m_fixShipBaseAngle[i]) * fInterpolation));
						
			}
        }
	}
}