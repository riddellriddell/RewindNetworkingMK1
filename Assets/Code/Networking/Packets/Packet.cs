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

        public byte[] m_Payload;

        public PacketWrapper(int lastAck, int iPacketStartFrame, int iMaxBytes)
        {
            m_iLastAckPackageFromPerson = lastAck;

            m_iStartPacketNumber = iPacketStartFrame;

            m_Payload = new byte[iMaxBytes];
        }

        public void AddDataPacket(DataPacket pakPacket)
        {
            pakPacket.EncodePacket(this);
        }

    }

    public abstract partial class DataPacket
    {        

        public static int GetPacketType(PacketWrapper pkwPacketWrapper, int iDataReadHead)
        {
            //get the next packet type
            return pkwPacketWrapper.m_Payload[iDataReadHead];
        }

        public abstract int GetTypeID { get; }

        public abstract int PacketSize { get; }

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
            iDataReadHead += sizeof(byte);

            return iDataReadHead;
        }

        protected int BaseEncodePacket(PacketWrapper pkwPacketWrapper, int iDataWriteHead)
        {
            //encode packet type
            pkwPacketWrapper.m_Payload.Add((byte)GetTypeID);
        }
    }    
}