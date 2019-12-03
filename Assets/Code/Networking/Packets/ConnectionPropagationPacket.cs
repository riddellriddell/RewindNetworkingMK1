using System;
using System.Text;

namespace Networking
{
    public class ConnectionNegotiationPacket : DataPacket
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
        public Int32 m_iIndex;
        public string m_strConnectionNegotiationMessage;

        public override int PacketPayloadSize
        {
            get
            {
                return ByteStream.DataSize(this);
            }
        }

        public override void DecodePacket(ReadByteStream rbsByteStream)
        {
            ByteStream.Serialize(rbsByteStream,this);
        }

        public override void EncodePacket(WriteByteStream wbsByteStream)
        {
            ByteStream.Serialize(wbsByteStream, this);
        }
    }

    //used to serialize and deserialize packet
    public partial class ByteStream
    {
        public static void Serialize(ReadByteStream rbsByteStream, ConnectionNegotiationPacket Input)
        {
            Serialize(rbsByteStream, ref Input.m_lFrom);
            Serialize(rbsByteStream, ref Input.m_lTo);
            Serialize(rbsByteStream, ref Input.m_iIndex);
            Serialize(rbsByteStream, ref Input.m_strConnectionNegotiationMessage);
        }

        public static void Serialize(WriteByteStream rbsByteStream, ConnectionNegotiationPacket Input)
        {
            Serialize(rbsByteStream, ref Input.m_lFrom);
            Serialize(rbsByteStream, ref Input.m_lTo);
            Serialize(rbsByteStream, ref Input.m_iIndex);
            Serialize(rbsByteStream, ref Input.m_strConnectionNegotiationMessage);
        }

        public static int DataSize(ConnectionNegotiationPacket Input)
        {
            int iSize = DataSize(Input.m_lFrom);
            iSize += DataSize(Input.m_lTo);
            iSize += DataSize(Input.m_iIndex);
            iSize += DataSize(Input.m_strConnectionNegotiationMessage);

            return iSize;
        }
    }
}
