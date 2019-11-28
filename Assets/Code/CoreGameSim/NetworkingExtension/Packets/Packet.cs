using Networking;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Sim
{
    public class InputPacket : TickStampedPacket
    {
        public static int TypeID
        {
            get
            {
                return 4;
            }
        }

        public byte m_bInput;

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

        public override void DecodePacket(PacketWrapper pkwPacketWrapper)
        {
            base.DecodePacket(pkwPacketWrapper);

            //decode tick offset
            ByteStream.Serialize(pkwPacketWrapper.ReadStream,ref m_bInput);

        }

        public override void EncodePacket(PacketWrapper pkwPacketWrapper)
        {

            base.EncodePacket(pkwPacketWrapper);

            //decode tick offset
            ByteStream.Serialize(pkwPacketWrapper.WriteStream, ref m_bInput);
        }
    }

    public class StartCountDownPacket : DataPacket
    {
        public static int TypeID
        {
            get
            {
                return 5;
            }
        }

        public long m_lGameStartTime;

        public DateTime GameStartTime
        {
            get
            {
                return new DateTime(m_lGameStartTime);
            }
        }

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
                return 5;
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

        public override void DecodePacket(PacketWrapper pkwPacketWrapper)
        {
            //decode game start time 
            ByteStream.Serialize(pkwPacketWrapper.ReadStream, ref m_lGameStartTime);
        }

        public override void EncodePacket(PacketWrapper pkwPacketWrapper)
        {
            //encode game start time 
            ByteStream.Serialize(pkwPacketWrapper.WriteStream, ref m_lGameStartTime);
        }
    }
}