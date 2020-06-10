using UnityEngine;
using System.Collections;
using System;

namespace Sim
{
    public class FrameDataInterpilationTypeAttribute : Attribute
    {
        public enum InterpolationType
        {
            None,
            Linear,
            Bilinear,
        }
        public Type m_tType = null;
        public InterpolationType m_itpInterpolation;
        public string m_strBilinearDependentVariable;

        public FrameDataInterpilationTypeAttribute(Type tType, InterpolationType itpInterpolation = InterpolationType.Linear)
        {
            m_tType = tType;
            m_itpInterpolation = itpInterpolation;
            m_strBilinearDependentVariable = "";
        }
    }
}