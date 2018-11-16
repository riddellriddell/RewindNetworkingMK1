using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Networking
{

    public class PacketWrapper
    {
        //last ack packet recieved from target 
        public int m_iLastAckPackageFromPerson;

        public int m_iStartPacketNumber;

        public List<Packet> m_Payload;

        public PacketWrapper(int lastAck, int iPacketStartFrame, int iPacketCount)
        {
            m_iLastAckPackageFromPerson = lastAck;

            m_iStartPacketNumber = iPacketStartFrame;

            m_Payload = new List<Packet>(iPacketCount);
        }

        public void AddDataPacket(Packet pakPacket)
        {
            m_Payload.Add(pakPacket);
        }

    }

    public abstract class Packet
    {
        public enum PacketType : byte
        {
            StartCountdown,
            ResetTickCount,
            Ping,
            Input
        }

        public static PacketType GetPacketType(PacketWrapper pkwPacketWrapper, int iDataReadHead)
        {
            return (PacketType)pkwPacketWrapper.m_Payload[iDataReadHead].m_ptyPacketType;
        }

        public abstract PacketType m_ptyPacketType { get; }

        public abstract int DecodePacket(PacketWrapper pkwPacketWrapper, int iDataReadHead);

        public abstract void EncodePacket(PacketWrapper pkwPacketWrapper);

        //applys an offset to the data read for the byte used for packet type
        public virtual int ApplyDataReadOffset(int iDataReadHead)
        {
            return iDataReadHead;
        }

        protected int BaseDecodePacket(PacketWrapper pkwPacketWrapper, int iDataReadHead)
        {
            //add read offset for packet type
            //iDataReadHead += sizeof(PacketType);

            return iDataReadHead;
        }

        protected void BaseEncodePacket(PacketWrapper pkwPacketWrapper)
        {
            //encode packet type
            //pkwPacketWrapper.AddDataPacket((byte)m_ptyPacketType);
        }

    }

    public abstract class TickStampedPacket : Packet
    {
        public static int MaxTicksBetweenTickStampedPackets
        {
            get
            {
                return byte.MaxValue;
            }
        }

        public static int ApplyPacketHeaderOffset(int iDataReadHead)
        {
            return iDataReadHead + 0;
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

        public override int ApplyDataReadOffset(int iDataReadHead)
        {
            //add a byte for the offset
            //iDataReadHead += 1;

            return base.ApplyDataReadOffset(iDataReadHead);
        }

        public override int DecodePacket(PacketWrapper pkwPacketWrapper, int iDataReadHead)
        {
            iDataReadHead = BaseDecodePacket(pkwPacketWrapper, iDataReadHead);

            //decode tick offset
            m_bOffset = (pkwPacketWrapper.m_Payload[iDataReadHead] as TickStampedPacket).m_bOffset;

            //offset data read point for offset byte
            //iDataReadHead += sizeof(byte);

            return iDataReadHead;
        }

        public override void EncodePacket(PacketWrapper pkwPacketWrapper)
        {
            BaseEncodePacket(pkwPacketWrapper);

            //pkwPacketWrapper.add(m_bOffset);
        }
    }

    // this packet resets the connection Tick To Zero and it used to start a game 
    public class ResetTickCountPacket : Packet
    {
        public override PacketType m_ptyPacketType
        {
            get
            {
                return PacketType.ResetTickCount;
            }
        }

        public override int DecodePacket(PacketWrapper pkwPacketWrapper, int iDataReadHead)
        {
            return BaseDecodePacket(pkwPacketWrapper, iDataReadHead) + 1;
        }

        public override void EncodePacket(PacketWrapper pkwPacketWrapper)
        {
            BaseEncodePacket(pkwPacketWrapper);
        }
    }

    //this is just used to keep the connection alive / add enough padding for the next input 
    //this packet is sent if 255 ticks / TickStamp.MaxTicksBetweenPackets have passed since the last packet and their have been no updates 
    public class PingPacket : Packet
    {

        public override PacketType m_ptyPacketType
        {
            get
            {
                return PacketType.Ping;
            }
        }

        public override int DecodePacket(PacketWrapper pkwPacketWrapper, int iDataReadHead)
        {
            return BaseDecodePacket(pkwPacketWrapper, iDataReadHead) + 1;
        }

        public override void EncodePacket(PacketWrapper pkwPacketWrapper)
        {
            BaseEncodePacket(pkwPacketWrapper);
        }

    }

    public class InputPacket : TickStampedPacket
    {
        public byte m_bInput;

        public override PacketType m_ptyPacketType
        {
            get
            {
                return PacketType.Input;
            }
        }

        public InputPacket() : base(0)
        {
            m_bInput = 0;
        }

        public InputPacket(byte bInput, int iPacketTick) : base(iPacketTick)
        {
            m_bInput = bInput;
        }

        public InputKeyFrame ConvertToKeyFrame()
        {
            return new InputKeyFrame()
            {
                m_iInput = m_bInput,
                m_iTick = base.m_iTick
            };
        }

        public override int DecodePacket(PacketWrapper pkwPacketWrapper, int iDataReadHead)
        {
            iDataReadHead =  base.DecodePacket(pkwPacketWrapper, iDataReadHead);

            //decode tick offset
            m_bInput = (pkwPacketWrapper.m_Payload[iDataReadHead] as InputPacket).m_bInput;

            //move the read head
            //iDataReadHead += sizeof(byte);

            iDataReadHead++;

            return iDataReadHead;
        }

        public override void EncodePacket(PacketWrapper pkwPacketWrapper)
        {
            BaseEncodePacket(pkwPacketWrapper);

            //add input byte
            //pkwPacketWrapper.add(m_bInput)
        }
    }

    public class StartCountDownPacket : Packet
    {

        public long m_lGameStartTime;

        public DateTime GameStartTime
        {
            get
            {
                return new DateTime(m_lGameStartTime);
            }
        }

        public override PacketType m_ptyPacketType
        {
            get
            {
                return PacketType.StartCountdown;
            }
        }

        public StartCountDownPacket()
        {
            m_lGameStartTime = DateTime.UtcNow.Ticks;
        }

        public StartCountDownPacket(long lTime)
        {
            m_lGameStartTime = lTime;
        }


        public override int DecodePacket(PacketWrapper pkwPacketWrapper, int iDataReadHead)
        {
            iDataReadHead = base.BaseDecodePacket(pkwPacketWrapper, iDataReadHead);

            //decode tick offset
            m_lGameStartTime = (pkwPacketWrapper.m_Payload[iDataReadHead] as StartCountDownPacket).m_lGameStartTime;


            iDataReadHead += 1;



            return iDataReadHead;
        }

        public override void EncodePacket(PacketWrapper pkwPacketWrapper)
        {
            //pkwPacketWrapper.add(tobyte(m_lGameStartTime));
        }
    }
}