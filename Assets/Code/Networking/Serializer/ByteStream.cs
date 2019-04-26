using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Networking
{
    public partial class ByteStream
    {
        protected byte[] m_bData;
        protected int m_iReadWriteHead;

        public ByteStream(int iBufferSize)
        {
            m_bData = new byte[iBufferSize];
            m_iReadWriteHead = 0;
        }

        public ByteStream(byte[] bData)
        {
            m_bData = bData;
            m_iReadWriteHead = 0;
        }

        public byte[] GetData()
        {
            byte[] bOutData = new byte[m_iReadWriteHead];

            GetData(bOutData);

            return bOutData;
        }

        public void GetData(in byte[] bDataArrayToFill)
        {
            Array.Copy(m_bData, 0, bDataArrayToFill, 0, m_iReadWriteHead);
        }

        public bool EndOfStream()
        {
            if(m_iReadWriteHead < m_bData.Length)
            {
                return false;
            }

            return true;
        }

    }

    public class ReadByteStream : ByteStream
    {
        public ReadByteStream(byte[] bData) : base(bData) { }
        public ReadByteStream(int iBufferSize) : base(iBufferSize) { }

    }

    public class WriteByteStream : ByteStream
    {
        public WriteByteStream(byte[] bData) : base(bData) { }
        public WriteByteStream(int iBufferSize) : base(iBufferSize) { }
    }
}
