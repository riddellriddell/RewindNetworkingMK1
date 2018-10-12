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
            Ping,
            Input,
            Connection
        }

        public static PacketType GetPacketType(PacketWrapper pkwPacketWrapper, int iDataReadHead)
        {
            return (PacketType)pkwPacketWrapper.m_Payload[iDataReadHead].m_ptyPacketType;
        }       
       
        public abstract PacketType m_ptyPacketType { get; }
              
    }

    public abstract class TickStampedPacket : Packet
    {
        public static byte GetOffsetFromLastPacket(PacketWrapper pkwPacketWrapper, int iDataReadHead, int iTickHead)
        {
            TickStampedPacket tickPacket = pkwPacketWrapper.m_Payload[iDataReadHead] as TickStampedPacket;

            return tickPacket.GetTickOffset(iTickHead);
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
        private byte m_bOffset;

        //this value is not sent across the network but infered from the data sent
        public int m_iTick;

        public TickStampedPacket(int iTargetTick)
        {
            m_iTick = iTargetTick;
        }

        public byte GetTickOffset(int iCurrentHead)
        {
            return (byte)(m_iTick - iCurrentHead);
        }
    }   

    //this is just used to keep the connection alive / add enough padding for the next input 
    //this packet is sent if 255 ticks have passed since the last packet and their have been no updates 
    public class PingPacket : TickStampedPacket
    {

        public override PacketType m_ptyPacketType
        {
            get
            {
                return PacketType.Ping;
            }
        }

        //decode constructor
        public PingPacket(PacketWrapper pkwPacketWrapper, ref int iDataReadHead, int iPacketTick) : base(iPacketTick)
        {
            PingPacket output = (PingPacket)pkwPacketWrapper.m_Payload[iDataReadHead];

            iDataReadHead += 1;
        }

        //send create constructor
        public PingPacket(int iPacketTick):base(iPacketTick)
        {

        }

    }

    public class InputPacket : TickStampedPacket
    {
        public byte input;

        public override PacketType m_ptyPacketType
        {
            get
            {
                return PacketType.Input;
            }
        }

        public InputPacket(PacketWrapper pkwPacketWrapper, ref int iDataReadHead, int iPacketTick) : base(iPacketTick)
        {
            InputPacket intputDataStream = (InputPacket)pkwPacketWrapper.m_Payload[iDataReadHead];

            input = intputDataStream.input;

            iDataReadHead += 1;
        }

        public InputPacket(byte bInput, int iPacketTick) : base(iPacketTick)
        {
            input = bInput;
        }

        public InputKeyFrame ConvertToKeyFrame()
        {
            return new InputKeyFrame()
            {
                m_iInput = input,
                m_iTick = base.m_iTick
            };
        }
    }
}