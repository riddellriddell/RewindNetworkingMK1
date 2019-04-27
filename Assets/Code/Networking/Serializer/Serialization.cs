using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Networking
{
    public partial class ByteStream
    {
        //read and write a byte
        public static void Serialize(ReadByteStream rbsStream ,ref Byte bOutput)
        {
            bOutput = rbsStream.m_bData[rbsStream.ReadWriteHead];

            rbsStream.ReadWriteHead++;
        }

        public static void Serialize(WriteByteStream wbsStream,ref Byte bInput)
        {
            wbsStream.m_bData[wbsStream.ReadWriteHead] = bInput;

            wbsStream.ReadWriteHead++;
        }

        // read and write int
        public static void Serialize(ReadByteStream rbsStream, ref Int32 iOutput)
        {
            iOutput = BitConverter.ToInt32(rbsStream.m_bData, rbsStream.ReadWriteHead);
            rbsStream.ReadWriteHead += sizeof(Int32);
        }

        public static void Serialize(WriteByteStream wbsStream,ref Int32 iInput)
        {
            Array.Copy(BitConverter.GetBytes(iInput), 0, wbsStream.m_bData, wbsStream.ReadWriteHead, sizeof(Int32));
            wbsStream.ReadWriteHead += sizeof(Int32);
        }

        // read and write long
        public static void Serialize(ReadByteStream rbsStream, ref Int64 iOutput)
        {
            iOutput = BitConverter.ToInt64(rbsStream.m_bData, rbsStream.ReadWriteHead);
            rbsStream.ReadWriteHead += sizeof(Int64);
        }

        public static void Serialize(WriteByteStream wbsStream,ref Int64 iInput)
        {
            Array.Copy(BitConverter.GetBytes(iInput), 0, wbsStream.m_bData, wbsStream.ReadWriteHead, sizeof(Int64));
            wbsStream.ReadWriteHead += sizeof(Int64);
        }
    }
}
