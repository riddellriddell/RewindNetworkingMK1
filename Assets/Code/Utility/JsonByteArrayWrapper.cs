using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Code.Utility
{
    public struct JsonByteArrayWrapper
    {
        public byte[] m_bWrappedArray;

        public JsonByteArrayWrapper(byte[] m_bArray)
        {
            m_bWrappedArray = m_bArray;
        }
    }
}