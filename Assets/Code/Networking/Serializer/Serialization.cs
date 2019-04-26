using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Networking
{
    public partial class ByteStream
    {
        //read and write a byte
        public static void Serialize(ReadByteStream rbsStream ,out Byte bOutput)
        {
            bOutput = rbsStream.m_bData[rbsStream.m_iReadWriteHead];

            rbsStream.m_iReadWriteHead++;
        }

        public static void Serialize(WriteByteStream wbsStream, Byte bInput)
        {
            wbsStream.m_bData[wbsStream.m_iReadWriteHead] = bInput;

            wbsStream.m_iReadWriteHead++;
        }

        // read and write int
        public static void Serialize(ReadByteStream rbsStream, out Int32 iOutput)
        {
            iOutput = BitConverter.ToInt32(rbsStream.m_bData, rbsStream.m_iReadWriteHead);
            rbsStream.m_iReadWriteHead += sizeof(Int32);
        }

        public static void Serialize(WriteByteStream wbsStream,Int32 iInput)
        {
            Array.Copy(BitConverter.GetBytes(iInput), 0, wbsStream.m_bData, wbsStream.m_iReadWriteHead, sizeof(Int32));
            wbsStream.m_iReadWriteHead += sizeof(Int32);
        }

        // read and write long
        public static void Serialize(ReadByteStream rbsStream, out Int64 iOutput)
        {
            iOutput = BitConverter.ToInt32(rbsStream.m_bData, rbsStream.m_iReadWriteHead);
            rbsStream.m_iReadWriteHead += sizeof(Int64);
        }

        public static void Serialize(WriteByteStream wbsStream, Int64 iInput)
        {
            Array.Copy(BitConverter.GetBytes(iInput), 0, wbsStream.m_bData, wbsStream.m_iReadWriteHead, sizeof(Int32));
            wbsStream.m_iReadWriteHead += sizeof(Int64);
        }
    }
}
