using SharedTypes;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Utility;

namespace ProjectSharedTypes
{
    public class UserInputGlobalMessage : GlobalMessageBase, ISimMessagePayload
    {
        public static int TypeID { get; set; } = int.MinValue;

        public override int TypeNumber
        {
            get
            {
                return UserInputGlobalMessage.TypeID;
            }
            set
            {
                TypeID = value;
            }
        }

        public byte m_bInputState;

        public override int DataSize()
        {
            return ByteStream.DataSize(m_bInputState);
        }

        public override void Serialize(ReadByteStream rbsByteStream)
        {
            ByteStream.Serialize(rbsByteStream, ref m_bInputState);
        }

        public override void Serialize(WriteByteStream wbsByteStream)
        {
            ByteStream.Serialize(wbsByteStream, ref m_bInputState);
        }

        public byte[] GetHash()
        {
            byte[] bOut = BitConverter.GetBytes((long)TypeID);

            bOut[0] = (byte)(bOut[0] ^ m_bInputState);

            return bOut;
        }
    }
}
