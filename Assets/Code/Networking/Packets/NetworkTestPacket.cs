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
        public static int TypeID
        {
            get
            {
                return 2;
            }
        }

        //the value to echo back 
        public byte m_bEcho;

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
                return 2;
            }
        }

        public override void DecodePacket(ReadByteStream rbsByteStream)
        {
            ByteStream.Serialize(rbsByteStream, ref m_bEcho);
        }

        public override void EncodePacket(WriteByteStream wbsByteStream)
        {
            ByteStream.Serialize(wbsByteStream,ref m_bEcho);
        }
    }

    public class NetTestReplyPacket : DataPacket
    {
        public static int TypeID
        {
            get
            {
                return 3;
            }
        }

        //the time on this computer 
        public long m_lTicks;

        //the value to echo back 
        public byte m_bEcho;

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
                return 2;
            }
        }

        public override void DecodePacket(ReadByteStream rbsByteStream)
        {

            //decode tick offset
            ByteStream.Serialize(rbsByteStream, ref m_bEcho);
            ByteStream.Serialize(rbsByteStream, ref m_lTicks);

        }

        public override void EncodePacket(WriteByteStream wbsByteStream)
        {
            //encode tick offset
            ByteStream.Serialize(wbsByteStream,ref m_bEcho);
            ByteStream.Serialize(wbsByteStream,ref m_lTicks);
        }
    }
}
