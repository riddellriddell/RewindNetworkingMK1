using System;
using System.Collections.Generic;
using System.Linq;
using Utility;

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
                return NetworkingByteStream.DataSize(this);
            }
        }

        public override void DecodePacket(ReadByteStream pkwPacketWrapper)
        {
            NetworkingByteStream.Serialize(pkwPacketWrapper, this);
        }

        public override void EncodePacket(WriteByteStream pkwPacketWrapper)
        {
            NetworkingByteStream.Serialize(pkwPacketWrapper, this);
        }
    }

    public partial class NetworkingByteStream
    {
        public static void Serialize(ReadByteStream rbsByteStream, NetworkLayoutPacket Input)
        {
            NetworkingByteStream.Serialize(rbsByteStream, ref Input.m_nlaNetworkLayout);
        }

        public static void Serialize(WriteByteStream wbsByteStream, NetworkLayoutPacket Input)
        {
            NetworkingByteStream.Serialize(wbsByteStream, ref Input.m_nlaNetworkLayout);
        }

        public static int DataSize(NetworkLayoutPacket Input)
        {
            return NetworkingByteStream.DataSize(ref Input.m_nlaNetworkLayout);
        }
    }
}
