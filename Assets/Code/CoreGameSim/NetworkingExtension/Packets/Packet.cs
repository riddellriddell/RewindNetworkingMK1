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

        public override int PacketSize
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

        public override int DecodePacket(PacketWrapper pkwPacketWrapper, int iDataReadHead)
        {
            iDataReadHead = base.DecodePacket(pkwPacketWrapper, iDataReadHead);

            //decode tick offset
            m_bInput = (pkwPacketWrapper.m_btsPayload[iDataReadHead] as InputPacket).m_bInput;

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

        public override int PacketSize
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

        public override int DecodePacket(PacketWrapper pkwPacketWrapper, int iDataReadHead)
        {
            iDataReadHead = base.BaseDecodePacket(pkwPacketWrapper, iDataReadHead);

            //decode tick offset
            m_lGameStartTime = (pkwPacketWrapper.m_btsPayload[iDataReadHead] as StartCountDownPacket).m_lGameStartTime;


            iDataReadHead += 1;



            return iDataReadHead;
        }

        public override void EncodePacket(PacketWrapper pkwPacketWrapper)
        {
            //pkwPacketWrapper.add(tobyte(m_lGameStartTime));
        }
    }
}