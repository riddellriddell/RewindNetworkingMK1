using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Networking
{
    /// <summary>
    /// this packet anounces that the user sending this packet has an active gateway to the matchmaking serrver
    /// </summary>
    public class GatewayActiveAnouncePacket : DataPacket
    {
        public static int TypeID { get; } = 10;

        public override int GetTypeID
        {
            get
            {
                return TypeID;
            }
        }

        public override int PacketPayloadSize { get; } = 0;

        //packet has no data to encode or decode
        public override void DecodePacket(PacketWrapper pkwPacketWrapper)
        {
        }

        public override void EncodePacket(PacketWrapper pkwPacketWrapper)
        {
        }
    }
}