

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SimDataInterpolation;
using FixedPointy;
using Sim;

//Generated Code do not edit!!!!
namespace SimDataInterpolation
{
	public class FrameDataInterpolator : IFrameDataInterpolator<InterpolationErrorCorrectionSettingsGen,FrameData,InterpolatedFrameDataGen>
	{
        public float CalculateErrorScalingAmount(float fError, float fDeltaTime, InterpolationErrorCorrectionSettingsBase.ErrorCorrectionSetting ecsErrorCorrectionSetting)
        {
			float fMagnitudeOfError = Mathf.Abs(fError);

            //check if there is no error
            if (fMagnitudeOfError == 0)
            {
                return 0;
            }

            //check if error bigger than snap distance 
            if (fMagnitudeOfError > ecsErrorCorrectionSetting.m_fSnapDistance)
            {
                return 0;
            }

            //check if the error is within the min error adjustment amount
            if (fMagnitudeOfError < ecsErrorCorrectionSetting.m_fMinInterpDistance)
            {
                return 1;
            }

            //scale down error magnitude
            float fReductionAmount = fMagnitudeOfError - (fMagnitudeOfError * (1 - ecsErrorCorrectionSetting.m_fQuadraticInterpRate));

            //apply linear clamping
            fReductionAmount = Mathf.Clamp(fReductionAmount, ecsErrorCorrectionSetting.m_fMinLinearInterpSpeed, ecsErrorCorrectionSetting.m_fMaxLinearInterpSpeed);

            //convert to scale multiplier
            float fScalePercent = Mathf.Clamp01(1 - ((fReductionAmount / fMagnitudeOfError) * fDeltaTime));

            return fScalePercent;
        }	

		public InterpolatedFrameDataGen CalculateErrorOffsets(InterpolatedFrameDataGen ifdOldFrameData, InterpolatedFrameDataGen ifdNewFrameData, InterpolationErrorCorrectionSettingsGen ecsErrorCorrectionSetting)
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

			if(ecsErrorCorrectionSetting.m_bEnableInterpolation == false)
			{
				return ifdNewFrameData;
			}

			//loop throuhg all variables and calculate the difference 
			for(int i = 0 ; i < ifdOldFrameData.m_fixTimeUntilRespawn.Length; i++)
			{
				ifdOldFrameData.m_fixTimeUntilRespawnErrorOffset[i] += (System.Single) (ifdNewFrameData.m_fixTimeUntilRespawn[i] - ifdOldFrameData.m_fixTimeUntilRespawn[i]);
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

			for(int i = 0 ; i < ifdOldFrameData.m_fixTimeUntilLaserFire.Length; i++)
			{
				ifdOldFrameData.m_fixTimeUntilLaserFireErrorOffset[i] += (System.Single) (ifdNewFrameData.m_fixTimeUntilLaserFire[i] - ifdOldFrameData.m_fixTimeUntilLaserFire[i]);
			}

			for(int i = 0 ; i < ifdOldFrameData.m_fixTimeUntilNextFire.Length; i++)
			{
				ifdOldFrameData.m_fixTimeUntilNextFireErrorOffset[i] += (System.Single) (ifdNewFrameData.m_fixTimeUntilNextFire[i] - ifdOldFrameData.m_fixTimeUntilNextFire[i]);
			}

			for(int i = 0 ; i < ifdOldFrameData.m_fixLazerLifeRemaining.Length; i++)
			{
				ifdOldFrameData.m_fixLazerLifeRemainingErrorOffset[i] += (System.Single) (ifdNewFrameData.m_fixLazerLifeRemaining[i] - ifdOldFrameData.m_fixLazerLifeRemaining[i]);
			}

			for(int i = 0 ; i < ifdOldFrameData.m_fixLazerPositionX.Length; i++)
			{
				ifdOldFrameData.m_fixLazerPositionXErrorOffset[i] += (System.Single) (ifdNewFrameData.m_fixLazerPositionX[i] - ifdOldFrameData.m_fixLazerPositionX[i]);
			}

			for(int i = 0 ; i < ifdOldFrameData.m_fixLazerPositionY.Length; i++)
			{
				ifdOldFrameData.m_fixLazerPositionYErrorOffset[i] += (System.Single) (ifdNewFrameData.m_fixLazerPositionY[i] - ifdOldFrameData.m_fixLazerPositionY[i]);
			}

			for(int i = 0 ; i < ifdOldFrameData.m_fixLazerVelocityX.Length; i++)
			{
				ifdOldFrameData.m_fixLazerVelocityXErrorOffset[i] += (System.Single) (ifdNewFrameData.m_fixLazerVelocityX[i] - ifdOldFrameData.m_fixLazerVelocityX[i]);
			}

			for(int i = 0 ; i < ifdOldFrameData.m_fixLazerVelocityY.Length; i++)
			{
				ifdOldFrameData.m_fixLazerVelocityYErrorOffset[i] += (System.Single) (ifdNewFrameData.m_fixLazerVelocityY[i] - ifdOldFrameData.m_fixLazerVelocityY[i]);
			}

			return ifdOldFrameData;

		}

		public InterpolatedFrameDataGen CalculateOffsetInterpolationData(InterpolatedFrameDataGen ifdFrameData)
		{
					//loop throuhg all variables and calculate the difference 
			for(int i = 0 ; i < ifdFrameData.m_fixTimeUntilRespawn.Length; i++)
			{
				ifdFrameData.m_fixTimeUntilRespawnErrorAdjusted[i] = (System.Single)(ifdFrameData.m_fixTimeUntilRespawn[i] - ifdFrameData.m_fixTimeUntilRespawnErrorOffset[i]);
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

			for(int i = 0 ; i < ifdFrameData.m_fixTimeUntilLaserFire.Length; i++)
			{
				ifdFrameData.m_fixTimeUntilLaserFireErrorAdjusted[i] = (System.Single)(ifdFrameData.m_fixTimeUntilLaserFire[i] - ifdFrameData.m_fixTimeUntilLaserFireErrorOffset[i]);
			}

			for(int i = 0 ; i < ifdFrameData.m_fixTimeUntilNextFire.Length; i++)
			{
				ifdFrameData.m_fixTimeUntilNextFireErrorAdjusted[i] = (System.Single)(ifdFrameData.m_fixTimeUntilNextFire[i] - ifdFrameData.m_fixTimeUntilNextFireErrorOffset[i]);
			}

			for(int i = 0 ; i < ifdFrameData.m_fixLazerLifeRemaining.Length; i++)
			{
				ifdFrameData.m_fixLazerLifeRemainingErrorAdjusted[i] = (System.Single)(ifdFrameData.m_fixLazerLifeRemaining[i] - ifdFrameData.m_fixLazerLifeRemainingErrorOffset[i]);
			}

			for(int i = 0 ; i < ifdFrameData.m_fixLazerPositionX.Length; i++)
			{
				ifdFrameData.m_fixLazerPositionXErrorAdjusted[i] = (System.Single)(ifdFrameData.m_fixLazerPositionX[i] - ifdFrameData.m_fixLazerPositionXErrorOffset[i]);
			}

			for(int i = 0 ; i < ifdFrameData.m_fixLazerPositionY.Length; i++)
			{
				ifdFrameData.m_fixLazerPositionYErrorAdjusted[i] = (System.Single)(ifdFrameData.m_fixLazerPositionY[i] - ifdFrameData.m_fixLazerPositionYErrorOffset[i]);
			}

			for(int i = 0 ; i < ifdFrameData.m_fixLazerVelocityX.Length; i++)
			{
				ifdFrameData.m_fixLazerVelocityXErrorAdjusted[i] = (System.Single)(ifdFrameData.m_fixLazerVelocityX[i] - ifdFrameData.m_fixLazerVelocityXErrorOffset[i]);
			}

			for(int i = 0 ; i < ifdFrameData.m_fixLazerVelocityY.Length; i++)
			{
				ifdFrameData.m_fixLazerVelocityYErrorAdjusted[i] = (System.Single)(ifdFrameData.m_fixLazerVelocityY[i] - ifdFrameData.m_fixLazerVelocityYErrorOffset[i]);
			}

			return ifdFrameData;
		}

		public  InterpolatedFrameDataGen ReduceOffsets(InterpolatedFrameDataGen ifdFrameData, float fDeltaTime, InterpolationErrorCorrectionSettingsGen ecsErrorCorrectionSetting)
		{
			for(int i = 0 ; i < ifdFrameData.m_fixTimeUntilRespawn.Length; i++)
			{
				ifdFrameData.m_fixTimeUntilRespawnErrorOffset[i] *= CalculateErrorScalingAmount(ifdFrameData.m_fixTimeUntilRespawnErrorOffset[i],fDeltaTime,ecsErrorCorrectionSetting.m_fixTimeUntilRespawnErrorCorrectionSetting );
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

			for(int i = 0 ; i < ifdFrameData.m_fixTimeUntilLaserFire.Length; i++)
			{
				ifdFrameData.m_fixTimeUntilLaserFireErrorOffset[i] *= CalculateErrorScalingAmount(ifdFrameData.m_fixTimeUntilLaserFireErrorOffset[i],fDeltaTime,ecsErrorCorrectionSetting.m_fixTimeUntilLaserFireErrorCorrectionSetting );
			}

			for(int i = 0 ; i < ifdFrameData.m_fixTimeUntilNextFire.Length; i++)
			{
				ifdFrameData.m_fixTimeUntilNextFireErrorOffset[i] *= CalculateErrorScalingAmount(ifdFrameData.m_fixTimeUntilNextFireErrorOffset[i],fDeltaTime,ecsErrorCorrectionSetting.m_fixTimeUntilNextFireErrorCorrectionSetting );
			}

			for(int i = 0 ; i < ifdFrameData.m_fixLazerLifeRemaining.Length; i++)
			{
				ifdFrameData.m_fixLazerLifeRemainingErrorOffset[i] *= CalculateErrorScalingAmount(ifdFrameData.m_fixLazerLifeRemainingErrorOffset[i],fDeltaTime,ecsErrorCorrectionSetting.m_fixLazerLifeRemainingErrorCorrectionSetting );
			}

			for(int i = 0 ; i < ifdFrameData.m_fixLazerPositionX.Length; i++)
			{
				ifdFrameData.m_fixLazerPositionXErrorOffset[i] *= CalculateErrorScalingAmount(ifdFrameData.m_fixLazerPositionXErrorOffset[i],fDeltaTime,ecsErrorCorrectionSetting.m_fixLazerPositionXErrorCorrectionSetting );
			}

			for(int i = 0 ; i < ifdFrameData.m_fixLazerPositionY.Length; i++)
			{
				ifdFrameData.m_fixLazerPositionYErrorOffset[i] *= CalculateErrorScalingAmount(ifdFrameData.m_fixLazerPositionYErrorOffset[i],fDeltaTime,ecsErrorCorrectionSetting.m_fixLazerPositionYErrorCorrectionSetting );
			}

			for(int i = 0 ; i < ifdFrameData.m_fixLazerVelocityX.Length; i++)
			{
				ifdFrameData.m_fixLazerVelocityXErrorOffset[i] *= CalculateErrorScalingAmount(ifdFrameData.m_fixLazerVelocityXErrorOffset[i],fDeltaTime,ecsErrorCorrectionSetting.m_fixLazerVelocityXErrorCorrectionSetting );
			}

			for(int i = 0 ; i < ifdFrameData.m_fixLazerVelocityY.Length; i++)
			{
				ifdFrameData.m_fixLazerVelocityYErrorOffset[i] *= CalculateErrorScalingAmount(ifdFrameData.m_fixLazerVelocityYErrorOffset[i],fDeltaTime,ecsErrorCorrectionSetting.m_fixLazerVelocityYErrorCorrectionSetting );
			}

			return ifdFrameData;
		}

		public void CreateInterpolatedFrameData(in FrameData fdaFromFrame,in FrameData fdaToFrame,float fInterpolation, ref InterpolatedFrameDataGen ifdInterpolatedFrameData)
        {
			//loop throuhg all the non time offset interpolation variables
			//that are not in arrays 
			for(int i = 0 ; i < ifdInterpolatedFrameData.m_lPeersAssignedToSlot.Length ; i++)
			{

					ifdInterpolatedFrameData.m_lPeersAssignedToSlot[i] = (System.Int64) fdaToFrame.m_lPeersAssignedToSlot[i];

			}
			for(int i = 0 ; i < ifdInterpolatedFrameData.m_bInput.Length ; i++)
			{

					ifdInterpolatedFrameData.m_bInput[i] = (System.Int32) fdaToFrame.m_bInput[i];

			}
			for(int i = 0 ; i < ifdInterpolatedFrameData.m_iInputHash.Length ; i++)
			{

					ifdInterpolatedFrameData.m_iInputHash[i] = (System.Int32) fdaToFrame.m_iInputHash[i];

			}
			for(int i = 0 ; i < ifdInterpolatedFrameData.m_fixShipHealth.Length ; i++)
			{

					ifdInterpolatedFrameData.m_fixShipHealth[i] = (System.Single) fdaToFrame.m_fixShipHealth[i];

			}
			for(int i = 0 ; i < ifdInterpolatedFrameData.m_fixShipHealDelayTimeOut.Length ; i++)
			{

					ifdInterpolatedFrameData.m_fixShipHealDelayTimeOut[i] = (System.Single) fdaToFrame.m_fixShipHealDelayTimeOut[i];

			}
			for(int i = 0 ; i < ifdInterpolatedFrameData.m_bShipLastDamagedBy.Length ; i++)
			{

					ifdInterpolatedFrameData.m_bShipLastDamagedBy[i] = (System.Byte) fdaToFrame.m_bShipLastDamagedBy[i];

			}
			for(int i = 0 ; i < ifdInterpolatedFrameData.m_fixTimeUntilRespawn.Length ; i++)
			{

				if((fdaFromFrame.m_fixShipHealth[i] <= Fix.Zero) == true)
				{
					ifdInterpolatedFrameData.m_fixTimeUntilRespawn[i] = (System.Single) fdaToFrame.m_fixTimeUntilRespawn[i];
				}
				else
				{
					ifdInterpolatedFrameData.m_fixTimeUntilRespawn[i] = (System.Single)( ((System.Single)(fdaFromFrame.m_fixTimeUntilRespawn[i]) * (1 - fInterpolation)) +  ((System.Single)(fdaToFrame.m_fixTimeUntilRespawn[i]) * fInterpolation));
						
				}
			}
			for(int i = 0 ; i < ifdInterpolatedFrameData.m_fixShipPosX.Length ; i++)
			{

				if((fdaFromFrame.m_fixShipHealth[i] <= Fix.Zero) == true)
				{
					ifdInterpolatedFrameData.m_fixShipPosX[i] = (System.Single) fdaToFrame.m_fixShipPosX[i];
				}
				else
				{
					ifdInterpolatedFrameData.m_fixShipPosX[i] = (System.Single)( ((System.Single)(fdaFromFrame.m_fixShipPosX[i]) * (1 - fInterpolation)) +  ((System.Single)(fdaToFrame.m_fixShipPosX[i]) * fInterpolation));
						
				}
			}
			for(int i = 0 ; i < ifdInterpolatedFrameData.m_fixShipPosY.Length ; i++)
			{

				if((fdaFromFrame.m_fixShipHealth[i] <= Fix.Zero) == true)
				{
					ifdInterpolatedFrameData.m_fixShipPosY[i] = (System.Single) fdaToFrame.m_fixShipPosY[i];
				}
				else
				{
					ifdInterpolatedFrameData.m_fixShipPosY[i] = (System.Single)( ((System.Single)(fdaFromFrame.m_fixShipPosY[i]) * (1 - fInterpolation)) +  ((System.Single)(fdaToFrame.m_fixShipPosY[i]) * fInterpolation));
						
				}
			}
			for(int i = 0 ; i < ifdInterpolatedFrameData.m_fixShipVelocityX.Length ; i++)
			{

				if((fdaFromFrame.m_fixShipHealth[i] <= Fix.Zero) == true)
				{
					ifdInterpolatedFrameData.m_fixShipVelocityX[i] = (System.Single) fdaToFrame.m_fixShipVelocityX[i];
				}
				else
				{
					ifdInterpolatedFrameData.m_fixShipVelocityX[i] = (System.Single)( ((System.Single)(fdaFromFrame.m_fixShipVelocityX[i]) * (1 - fInterpolation)) +  ((System.Single)(fdaToFrame.m_fixShipVelocityX[i]) * fInterpolation));
						
				}
			}
			for(int i = 0 ; i < ifdInterpolatedFrameData.m_fixShipVelocityY.Length ; i++)
			{

				if((fdaFromFrame.m_fixShipHealth[i] <= Fix.Zero) == true)
				{
					ifdInterpolatedFrameData.m_fixShipVelocityY[i] = (System.Single) fdaToFrame.m_fixShipVelocityY[i];
				}
				else
				{
					ifdInterpolatedFrameData.m_fixShipVelocityY[i] = (System.Single)( ((System.Single)(fdaFromFrame.m_fixShipVelocityY[i]) * (1 - fInterpolation)) +  ((System.Single)(fdaToFrame.m_fixShipVelocityY[i]) * fInterpolation));
						
				}
			}
			for(int i = 0 ; i < ifdInterpolatedFrameData.m_fixShipBaseAngle.Length ; i++)
			{
				if((fdaFromFrame.m_fixShipHealth[i] <= Fix.Zero) == true)
				{
					ifdInterpolatedFrameData.m_fixShipBaseAngle[i] = (System.Single) fdaToFrame.m_fixShipBaseAngle[i];
				}
				else
				{					
				System.Single m_fixShipBaseAngleDifference = (System.Single)(fdaToFrame.m_fixShipBaseAngle[i]) - (System.Single)(fdaFromFrame.m_fixShipBaseAngle[i]);

				if(Mathf.Abs(m_fixShipBaseAngleDifference) > 180)
				{
					m_fixShipBaseAngleDifference = m_fixShipBaseAngleDifference - ((System.Single)(360) * Mathf.Sign(m_fixShipBaseAngleDifference));
				}

				ifdInterpolatedFrameData.m_fixShipBaseAngle[i] = (((System.Single)(((System.Single) fdaFromFrame.m_fixShipBaseAngle[i]) + (m_fixShipBaseAngleDifference * fInterpolation)) + (System.Single)360)) % (System.Single)360;

				}
			}
			for(int i = 0 ; i < ifdInterpolatedFrameData.m_bLazerFireIndex.Length ; i++)
			{

					ifdInterpolatedFrameData.m_bLazerFireIndex[i] = (System.Byte) fdaToFrame.m_bLazerFireIndex[i];

			}
			for(int i = 0 ; i < ifdInterpolatedFrameData.m_fixTimeUntilLaserFire.Length ; i++)
			{

				if((fdaFromFrame.m_fixShipHealth[i] <= Fix.Zero) == true)
				{
					ifdInterpolatedFrameData.m_fixTimeUntilLaserFire[i] = (System.Single) fdaToFrame.m_fixTimeUntilLaserFire[i];
				}
				else
				{
					ifdInterpolatedFrameData.m_fixTimeUntilLaserFire[i] = (System.Single)( ((System.Single)(fdaFromFrame.m_fixTimeUntilLaserFire[i]) * (1 - fInterpolation)) +  ((System.Single)(fdaToFrame.m_fixTimeUntilLaserFire[i]) * fInterpolation));
						
				}
			}
			for(int i = 0 ; i < ifdInterpolatedFrameData.m_fixTimeUntilNextFire.Length ; i++)
			{

				if((fdaFromFrame.m_fixShipHealth[i] <= Fix.Zero) == true)
				{
					ifdInterpolatedFrameData.m_fixTimeUntilNextFire[i] = (System.Single) fdaToFrame.m_fixTimeUntilNextFire[i];
				}
				else
				{
					ifdInterpolatedFrameData.m_fixTimeUntilNextFire[i] = (System.Single)( ((System.Single)(fdaFromFrame.m_fixTimeUntilNextFire[i]) * (1 - fInterpolation)) +  ((System.Single)(fdaToFrame.m_fixTimeUntilNextFire[i]) * fInterpolation));
						
				}
			}
			for(int i = 0 ; i < ifdInterpolatedFrameData.m_bLazerOwner.Length ; i++)
			{

					ifdInterpolatedFrameData.m_bLazerOwner[i] = (System.Byte) fdaToFrame.m_bLazerOwner[i];

			}
			for(int i = 0 ; i < ifdInterpolatedFrameData.m_fixLazerLifeRemaining.Length ; i++)
			{

				if((fdaFromFrame.m_fixLazerLifeRemaining[i] <= fdaToFrame.m_fixLazerLifeRemaining[i]) == true)
				{
					ifdInterpolatedFrameData.m_fixLazerLifeRemaining[i] = (System.Single) fdaToFrame.m_fixLazerLifeRemaining[i];
				}
				else
				{
					ifdInterpolatedFrameData.m_fixLazerLifeRemaining[i] = (System.Single)( ((System.Single)(fdaFromFrame.m_fixLazerLifeRemaining[i]) * (1 - fInterpolation)) +  ((System.Single)(fdaToFrame.m_fixLazerLifeRemaining[i]) * fInterpolation));
						
				}
			}
			for(int i = 0 ; i < ifdInterpolatedFrameData.m_fixLazerPositionX.Length ; i++)
			{

				if((fdaFromFrame.m_fixLazerLifeRemaining[i] <= fdaToFrame.m_fixLazerLifeRemaining[i]) == true)
				{
					ifdInterpolatedFrameData.m_fixLazerPositionX[i] = (System.Single) fdaToFrame.m_fixLazerPositionX[i];
				}
				else
				{
					ifdInterpolatedFrameData.m_fixLazerPositionX[i] = (System.Single)( ((System.Single)(fdaFromFrame.m_fixLazerPositionX[i]) * (1 - fInterpolation)) +  ((System.Single)(fdaToFrame.m_fixLazerPositionX[i]) * fInterpolation));
						
				}
			}
			for(int i = 0 ; i < ifdInterpolatedFrameData.m_fixLazerPositionY.Length ; i++)
			{

				if((fdaFromFrame.m_fixLazerLifeRemaining[i] <= fdaToFrame.m_fixLazerLifeRemaining[i]) == true)
				{
					ifdInterpolatedFrameData.m_fixLazerPositionY[i] = (System.Single) fdaToFrame.m_fixLazerPositionY[i];
				}
				else
				{
					ifdInterpolatedFrameData.m_fixLazerPositionY[i] = (System.Single)( ((System.Single)(fdaFromFrame.m_fixLazerPositionY[i]) * (1 - fInterpolation)) +  ((System.Single)(fdaToFrame.m_fixLazerPositionY[i]) * fInterpolation));
						
				}
			}
			for(int i = 0 ; i < ifdInterpolatedFrameData.m_fixLazerVelocityX.Length ; i++)
			{

				if((fdaFromFrame.m_fixLazerLifeRemaining[i] <= fdaToFrame.m_fixLazerLifeRemaining[i]) == true)
				{
					ifdInterpolatedFrameData.m_fixLazerVelocityX[i] = (System.Single) fdaToFrame.m_fixLazerVelocityX[i];
				}
				else
				{
					ifdInterpolatedFrameData.m_fixLazerVelocityX[i] = (System.Single)( ((System.Single)(fdaFromFrame.m_fixLazerVelocityX[i]) * (1 - fInterpolation)) +  ((System.Single)(fdaToFrame.m_fixLazerVelocityX[i]) * fInterpolation));
						
				}
			}
			for(int i = 0 ; i < ifdInterpolatedFrameData.m_fixLazerVelocityY.Length ; i++)
			{

				if((fdaFromFrame.m_fixLazerLifeRemaining[i] <= fdaToFrame.m_fixLazerLifeRemaining[i]) == true)
				{
					ifdInterpolatedFrameData.m_fixLazerVelocityY[i] = (System.Single) fdaToFrame.m_fixLazerVelocityY[i];
				}
				else
				{
					ifdInterpolatedFrameData.m_fixLazerVelocityY[i] = (System.Single)( ((System.Single)(fdaFromFrame.m_fixLazerVelocityY[i]) * (1 - fInterpolation)) +  ((System.Single)(fdaToFrame.m_fixLazerVelocityY[i]) * fInterpolation));
						
				}
			}
        }
	}
}