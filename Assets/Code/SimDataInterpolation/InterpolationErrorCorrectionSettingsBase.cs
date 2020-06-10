using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InterpolationErrorCorrectionSettingsBase : ScriptableObject
{
    //struct that represents how errors in the data due to networking are resolved 
    [System.Serializable]
    public struct ErrorCorrectionSetting
    {
        [Range(0, 20)]
        [SerializeField]
        public float m_fSnapDistance;

        [Range(0, 20)]
        [SerializeField]
        public float m_fMinInterpDistance;

        [Range(0, 20)]
        [SerializeField]
        public float m_fMinLinearInterpSpeed;

        [Range(0, 20)]
        [SerializeField]
        public float m_fMaxLinearInterpSpeed;

        [Range(0, 10)]
        [SerializeField]
        public float m_fQuadraticInterpRate;

    }

    public bool m_bEnableInterpolation = false;
}
