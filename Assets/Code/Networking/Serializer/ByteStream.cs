using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Networking
{
    public partial class ByteStream
    {
        protected byte[] m_bData;
        public int ReadWriteHead { get; protected set; }
        public int BytesRemaining
        {
            get
            {
                return m_bData.Length - ReadWriteHead;
            }
        }
        public int BufferSize
        {
            get
            {
                return m_bData.Length;
            }
        }
        public ByteStream(int iBufferSize)
        {
            m_bData = new byte[iBufferSize];
            ReadWriteHead = 0;
        }

        public ByteStream(byte[] bData)
        {
            m_bData = bData;
            ReadWriteHead = 0;
        }

        public byte[] GetData()
        {
            byte[] bOutData = new byte[ReadWriteHead];

            GetData(bOutData);

            return bOutData;
        }

        public void GetData(in byte[] bDataArrayToFill)
        {
            Array.Copy(m_bData, 0, bDataArrayToFill, 0, ReadWriteHead);
        }

        public bool EndOfStream()
        {
            if(ReadWriteHead < m_bData.Length)
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
