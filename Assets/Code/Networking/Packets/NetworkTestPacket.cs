using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
                return ByteStream.DataSize(this);
            }
        }

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
        public static void Serialize(ReadByteStream ByteStream, NetTestSendPacket Input)
        {
            Serialize(ByteStream,ref Input.m_bEcho);
        }

        public static void Serialize(WriteByteStream ByteStream, NetTestSendPacket Input)
        {
            Serialize(ByteStream, ref Input.m_bEcho);
        }

        public static int DataSize(NetTestSendPacket Input)
        {
            return DataSize(Input.m_bEcho);
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
                return ByteStream.DataSize(this);
            }
        }

        public override void DecodePacket(ReadByteStream rbsByteStream)
        {
            //decode tick offset
            ByteStream.Serialize(rbsByteStream, this);
        }

        public override void EncodePacket(WriteByteStream wbsByteStream)
        {
            //encode tick offset
            ByteStream.Serialize(wbsByteStream, this);
        }
    }

    public partial class ByteStream
    {
        public static void Serialize(ReadByteStream ByteStream, NetTestReplyPacket Input)
        {
            Serialize(ByteStream, ref Input.m_lLocalBaseTimeTicks);
            Serialize(ByteStream, ref Input.m_bEcho);
        }

        public static void Serialize(WriteByteStream ByteStream, NetTestReplyPacket Input)
        {
            Serialize(ByteStream, ref Input.m_lLocalBaseTimeTicks);
            Serialize(ByteStream, ref Input.m_bEcho);
        }

        public static int DataSize(NetTestReplyPacket Input)
        {
            int iSize = DataSize(Input.m_lLocalBaseTimeTicks);
            iSize += DataSize(Input.m_bEcho);
            return iSize;
        }
    }
}
