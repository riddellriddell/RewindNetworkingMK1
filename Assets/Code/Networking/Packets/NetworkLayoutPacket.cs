using System;
using System.Collections.Generic;
using System.Linq;

namespace Networking
{
    public class NetworkLayoutPacket : DataPacket
    {
        public static int TypeID
        {
            get
            {
                return 6;
            }
        }

        public override int GetTypeID
        {
            get
            {
                return TypeID;
            }
        }

        public NetworkLayout m_nlaNetworkLayout;

        public override int PacketPayloadSize
        {
            get
            {
                return ByteStream.DataSize(this);
            }
        }

        public override void DecodePacket(ReadByteStream pkwPacketWrapper)
        {
            ByteStream.Serialize(pkwPacketWrapper, this);
        }

        public override void EncodePacket(WriteByteStream pkwPacketWrapper)
        {
            ByteStream.Serialize(pkwPacketWrapper, this);
        }
    }

    public partial class ByteStream
    {
        public static void Serialize(ReadByteStream rbsByteStream, NetworkLayoutPacket Input)
        {
            ByteStream.Serialize(rbsByteStream, ref Input.m_nlaNetworkLayout);
        }

        public static void Serialize(WriteByteStream wbsByteStream, NetworkLayoutPacket Input)
        {
            ByteStream.Serialize(wbsByteStream, ref Input.m_nlaNetworkLayout);
        }

        public static int DataSize(NetworkLayoutPacket Input)
        {
            return ByteStream.DataSize(ref Input.m_nlaNetworkLayout);
        }
    }
}
