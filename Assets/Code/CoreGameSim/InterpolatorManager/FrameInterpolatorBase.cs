using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Sim
{
    //base class that auto generated frame data interpolation classes build off
    //this class holds all the shared funcitons and variables 
    public class FrameDataInterpolatorBase<TErrorCorrectionSettings, TFrameData>
    {
        public TErrorCorrectionSettings m_ecsErrorCorrectionSettings;

        public float CalculateErrorScalingAmount(float fMagnitudeOfError, float fDeltaTime, InterpolationErrorCorrectionSettingsBase.ErrorCorrectionSetting ecsErrorCorrectionSetting)
        {
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
    }
}