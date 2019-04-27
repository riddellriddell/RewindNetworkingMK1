using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Networking
{

    public abstract class TickStampedPacket : DataPacket
    {
        public static int MaxTicksBetweenTickStampedPackets
        {
            get
            {
                return byte.MaxValue;
            }
        }

        //the number of ticks between this and the previouse packet
        public int Offset
        {
            get
            {
                return m_bOffset;
            }
        }
        protected byte m_bOffset;

        //this value is not sent across the network but infered from the data sent
        public int m_iTick;

        public TickStampedPacket(int iTargetTick)
        {
            m_iTick = iTargetTick;
        }

        public TickStampedPacket(byte bTickOffset)
        {
            m_bOffset = bTickOffset;
        }

        public TickStampedPacket()
        {
            m_iTick = 0;
            m_bOffset = 0;
        }

        public void SetOffset(int iCurrentTick)
        {
            m_bOffset = (byte)(m_iTick - iCurrentTick);
        }


        public override void DecodePacket(PacketWrapper pkwPacketWrapper)
        {
            ByteStream.Serialize(pkwPacketWrapper.ReadStream, ref m_bOffset);
        }

        public override void EncodePacket(PacketWrapper pkwPacketWrapper)
        {
            ByteStream.Serialize(pkwPacketWrapper.WriteStream, ref m_bOffset);
        }
    }

    // this packet resets the connection Tick To Zero and it used to start a game 
    public class ResetTickCountPacket : DataPacket
    {
        public static int TypeID
        {
            get
            {
                return 0;
            }
        }

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
                return 1;
            }
        }

        public override void DecodePacket(PacketWrapper pkwPacketWrapper)
        {
        }

        public override void EncodePacket(PacketWrapper pkwPacketWrapper)
        {
        }
    }

    //this is just used to keep the connection alive / add enough padding for the next input 
    //this packet is sent if 255 ticks / TickStamp.MaxTicksBetweenPackets have passed since the last packet and their have been no updates 
    public class PingPacket : DataPacket
    {
        public static int TypeID
        {
            get
            {
                return 1;
            }
        }

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
                return 1;
            }
        }

        public override void DecodePacket(PacketWrapper pkwPacketWrapper)
        {
        }

        public override void EncodePacket(PacketWrapper pkwPacketWrapper)
        {
        }
    }
}
