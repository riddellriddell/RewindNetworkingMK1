using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Networking
{
    public class LargePacket : DataPacket
    {
        public static int TypeID
        {
            get
            {
                return 0;
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

        public override int GetTypeID
        {
            get
            {
                return TypeID;
            }
        }

        public override int PacketPayloadSize
        {
            get
            {
                return ByteStream.DataSize(this);
            }
        }

        public byte m_bIsLastPacketInSequence;

        public List<byte> m_bPacketSegment = new List<byte>();

        public override void DecodePacket(ReadByteStream rbsByteStream)
        {
            ByteStream.Serialize(rbsByteStream, this);
        }

        public override void EncodePacket(WriteByteStream wbsByteStream)
        {
            ByteStream.Serialize(wbsByteStream, this);
        }
    }

    public partial class ByteStream
    {
        public static void Serialize(ReadByteStream rbsByteStream, LargePacket Input)
        {
            Serialize(rbsByteStream, ref Input.m_bIsLastPacketInSequence);
            Serialize(rbsByteStream, ref Input.m_bPacketSegment);
        }

        public static void Serialize(WriteByteStream wbsByteStream, LargePacket Input)
        {
            Serialize(wbsByteStream, ref Input.m_bIsLastPacketInSequence);
            Serialize(wbsByteStream, ref Input.m_bPacketSegment);
        }

        public static int DataSize(LargePacket Input)
        {
            int iSize = DataSize(Input.m_bIsLastPacketInSequence);
            iSize += DataSize(Input.m_bPacketSegment);

            return iSize;
        }
    }
}
