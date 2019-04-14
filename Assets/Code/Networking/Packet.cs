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

        public List<DataPacket> m_Payload;

        public PacketWrapper(int lastAck, int iPacketStartFrame, int iPacketCount)
        {
            m_iLastAckPackageFromPerson = lastAck;

            m_iStartPacketNumber = iPacketStartFrame;

            m_Payload = new List<DataPacket>(iPacketCount);
        }

        public void AddDataPacket(DataPacket pakPacket)
        {
            m_Payload.Add(pakPacket);
        }

    }

    public abstract partial class DataPacket
    {        

        public static int GetPacketType(PacketWrapper pkwPacketWrapper, int iDataReadHead)
        {
            return 1;
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
            //iDataReadHead += sizeof(PacketType);

            return iDataReadHead;
        }

        protected void BaseEncodePacket(PacketWrapper pkwPacketWrapper)
        {
            //encode packet type
            //pkwPacketWrapper.AddDataPacket((byte)m_ptyPacketType);
        }
    }    
}