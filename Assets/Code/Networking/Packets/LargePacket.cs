using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Utility;

namespace Networking
{
    public class LargePacket : DataPacket
    {
        public static int TypeID { get; set; } = int.MinValue;

        public override int GetTypeID
        {
            get
            {
                return TypeID;
            }
        }

        //the additional size of a packet to send an array of x bytes
        //this is used to work out the max size of the payload while still 
        //fitting under the mtu
        public int HeaderSize
        {
            get
            {
                int iSize = ByteStream.DataSize(m_bIsLastPacketInSequence);
                iSize += sizeof(int); //the size of the byte array
                iSize += TypeHeaderSize; //the type def for the entire packet
                return iSize;
            }
        }

        public override int PacketPayloadSize
        {
            get
            {
                return NetworkingByteStream.DataSize(this);
            }
        }

        public string m_strNameOfParentPacket;

        public byte m_bIsLastPacketInSequence;

        public List<byte> m_bPacketSegment = new List<byte>();

        public override void DecodePacket(ReadByteStream rbsByteStream)
        {
            NetworkingByteStream.Serialize(rbsByteStream, this);
        }

        public override void EncodePacket(WriteByteStream wbsByteStream)
        {
            NetworkingByteStream.Serialize(wbsByteStream, this);
        }

        public override string ToString()
        {
            return base.ToString() + $": {m_strNameOfParentPacket} ";
        }
    }

    public partial class NetworkingByteStream
    {
        public static void Serialize(ReadByteStream rbsByteStream, LargePacket Input)
        {
            ByteStream.Serialize(rbsByteStream, ref Input.m_bIsLastPacketInSequence);
            ByteStream.Serialize(rbsByteStream, ref Input.m_bPacketSegment);
        }

        public static void Serialize(WriteByteStream wbsByteStream, LargePacket Input)
        {
            ByteStream.Serialize(wbsByteStream, ref Input.m_bIsLastPacketInSequence);
            ByteStream.Serialize(wbsByteStream, ref Input.m_bPacketSegment);
        }

        public static int DataSize(LargePacket Input)
        {
            int iSize = ByteStream.DataSize(Input.m_bIsLastPacketInSequence);
            iSize += ByteStream.DataSize(Input.m_bPacketSegment);

            return iSize;
        }
    }
}
