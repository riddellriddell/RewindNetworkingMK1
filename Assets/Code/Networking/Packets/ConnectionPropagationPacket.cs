using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Networking
{
    public class ConnectionRequestPacket : DataPacket
    {
        public static int TypeID
        {
            get
            {
                return 7;
            }
        }

        public override int GetTypeID
        {
            get
            {
               return TypeID;
            }
        }

        public Int64 m_lFrom;
        public Int64 m_lTo;
        public List<Byte> m_bConnectionRequestDetails;

        public override int PacketPayloadSize
        {
            get
            {
                int iPacketSize = sizeof(Int64) * 2;
                iPacketSize += sizeof(Int32);
                iPacketSize += sizeof(Byte) * m_bConnectionRequestDetails.Count;

                return iPacketSize;
            }
        }

        public override void DecodePacket(PacketWrapper pkwPacketWrapper)
        {
            ByteStream.Serialize(pkwPacketWrapper.ReadStream, ref m_lFrom);
            ByteStream.Serialize(pkwPacketWrapper.ReadStream, ref m_lTo);
            ByteStream.Serialize(pkwPacketWrapper.ReadStream, ref m_bConnectionRequestDetails);
        }

        public override void EncodePacket(PacketWrapper pkwPacketWrapper)
        {
            ByteStream.Serialize(pkwPacketWrapper.ReadStream, ref m_lFrom);
            ByteStream.Serialize(pkwPacketWrapper.ReadStream, ref m_lTo);
            ByteStream.Serialize(pkwPacketWrapper.ReadStream, ref m_bConnectionRequestDetails);
        }
    }

    public class ConnectionReplyPacket : DataPacket
    {
        public static int TypeID
        {
            get
            {
                return 8;
            }
        }

        public override int GetTypeID
        {
            get
            {
                return TypeID;
            }
        }

        public Int64 m_lFrom;
        public Int64 m_lTo;
        public List<Byte> m_bConnectionReplyDetails;

        public override int PacketPayloadSize
        {
            get
            {
                int iPacketSize = sizeof(Int64) * 2;
                iPacketSize += sizeof(Int32);
                iPacketSize += sizeof(Byte) * m_bConnectionReplyDetails.Count;

                return iPacketSize;
            }
        }

        public override void DecodePacket(PacketWrapper pkwPacketWrapper)
        {
            ByteStream.Serialize(pkwPacketWrapper.ReadStream, ref m_lFrom);
            ByteStream.Serialize(pkwPacketWrapper.ReadStream, ref m_lTo);
            ByteStream.Serialize(pkwPacketWrapper.ReadStream, ref m_bConnectionReplyDetails);
        }

        public override void EncodePacket(PacketWrapper pkwPacketWrapper)
        {
            ByteStream.Serialize(pkwPacketWrapper.ReadStream, ref m_lFrom);
            ByteStream.Serialize(pkwPacketWrapper.ReadStream, ref m_lTo);
            ByteStream.Serialize(pkwPacketWrapper.ReadStream, ref m_bConnectionReplyDetails);
        }
    }
}
