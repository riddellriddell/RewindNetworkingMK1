using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utility;

namespace Networking
{
    /// <summary>
    /// this class is used to test the RTT time of the network 
    /// </summary>
    public class NetTestSendPacket : DataPacket
    {
        public static int TypeID { get; set; } = int.MinValue;

        public override int GetTypeID
        {
            get
            {
                return TypeID;
            }
        }
        
        //the value to echo back 
        public byte m_bEcho;

        public override int PacketPayloadSize
        {
            get
            {
                return NetworkingByteStream.DataSize(this);
            }
        }

        public override void DecodePacket(ReadByteStream rbsByteStream)
        {
            NetworkingByteStream.Serialize(rbsByteStream, this);
        }

        public override void EncodePacket(WriteByteStream wbsByteStream)
        {
            NetworkingByteStream.Serialize(wbsByteStream, this);
        }
    }

    public partial class NetworkingByteStream
    {
        public static void Serialize(ReadByteStream rbsByteStream, NetTestSendPacket Input)
        {
            ByteStream.Serialize(rbsByteStream, ref Input.m_bEcho);
        }

        public static void Serialize(WriteByteStream wbsByteStream, NetTestSendPacket Input)
        {
            ByteStream.Serialize(wbsByteStream, ref Input.m_bEcho);
        }

        public static int DataSize(NetTestSendPacket Input)
        {
            return ByteStream.DataSize(Input.m_bEcho);
        }
    }

    public class NetTestReplyPacket : DataPacket
    {
        public static int TypeID { get; set; } = int.MinValue;

        public override int GetTypeID
        {
            get
            {
                return TypeID;
            }
        }


        //the time on this computer 
        public long m_lLocalBaseTimeTicks;

        //the value to echo back 
        public byte m_bEcho;

        public override int PacketPayloadSize
        {
            get
            {
                return NetworkingByteStream.DataSize(this);
            }
        }

        public override void DecodePacket(ReadByteStream rbsByteStream)
        {
            //decode tick offset
            NetworkingByteStream.Serialize(rbsByteStream, this);
        }

        public override void EncodePacket(WriteByteStream wbsByteStream)
        {
            //encode tick offset
            NetworkingByteStream.Serialize(wbsByteStream, this);
        }
    }

    public partial class NetworkingByteStream
    {
        public static void Serialize(ReadByteStream rbsByteStream, NetTestReplyPacket Input)
        {
            ByteStream.Serialize(rbsByteStream, ref Input.m_lLocalBaseTimeTicks);
            ByteStream.Serialize(rbsByteStream, ref Input.m_bEcho);
        }

        public static void Serialize(WriteByteStream wbsByteStream, NetTestReplyPacket Input)
        {
            ByteStream.Serialize(wbsByteStream, ref Input.m_lLocalBaseTimeTicks);
            ByteStream.Serialize(wbsByteStream, ref Input.m_bEcho);
        }

        public static int DataSize(NetTestReplyPacket Input)
        {
            int iSize = ByteStream.DataSize(Input.m_lLocalBaseTimeTicks);
            iSize += ByteStream.DataSize(Input.m_bEcho);
            return iSize;
        }
    }
}
