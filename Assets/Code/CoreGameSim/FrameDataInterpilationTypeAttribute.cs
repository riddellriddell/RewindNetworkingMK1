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
        }
        public Type m_tType = null;
        public InterpolationType m_itpInterpolation;

        public FrameDataInterpilationTypeAttribute(Type tType, InterpolationType itpInterpolation = InterpolationType.Linear)
        {
            m_tType = tType;
            m_itpInterpolation = itpInterpolation;
        }
    }
}