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

        public override int PacketSize
        {
            get
            {
                return 2;
            }
        }

        public override int DecodePacket(PacketWrapper pkwPacketWrapper, int iDataReadHead)
        {
            iDataReadHead = base.BaseDecodePacket(pkwPacketWrapper, iDataReadHead);

            //decode tick offset
            m_bEcho = pkwPacketWrapper.m_Payload[iDataReadHead++];

            return iDataReadHead;
        }

        public override void EncodePacket(PacketWrapper pkwPacketWrapper)
        {
            BaseEncodePacket(pkwPacketWrapper);
            pkwPacketWrapper.m_Payload.Add(m_bEcho);

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

        public override int PacketSize
        {
            get
            {
                return 2;
            }
        }

        public override int DecodePacket(PacketWrapper pkwPacketWrapper, int iDataReadHead)
        {
            iDataReadHead = base.BaseDecodePacket(pkwPacketWrapper, iDataReadHead);

            //decode tick offset
            m_bEcho = pkwPacketWrapper.m_Payload[iDataReadHead++]  ;
            m_lTicks = BitConverter.ToInt64(pkwPacketWrapper.m_Payload.ToArray(),iDataReadHead) ;

            iDataReadHead += 1;

            return iDataReadHead;
        }

        public override void EncodePacket(PacketWrapper pkwPacketWrapper)
        {
            BaseEncodePacket(pkwPacketWrapper);
        }
    }
}
