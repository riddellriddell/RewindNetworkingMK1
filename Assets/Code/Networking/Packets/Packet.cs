using System;

namespace Networking
{

    public class PacketWrapper
    {
        public enum WriteMode
        {
            Read,
            Write
        }

        //last ack packet recieved from target 
        public int LastAckPackageFromPerson
        {
            get
            {
                return m_iLastAckPackageFromPerson;
            }
        }
        public int StartPacketNumber
        {
            get
            {
                return m_iStartPacketNumber;
            }
        }

        protected int m_iLastAckPackageFromPerson;
        protected int m_iStartPacketNumber;

        public WriteMode Mode
        {
            get; private set;
        }

        public WriteByteStream WriteStream
        {
            get
            {
                if (Mode == WriteMode.Read)
                {
                    return null;
                }
                return m_btsPayload as WriteByteStream;
            }
        }

        public ReadByteStream ReadStream
        {
            get
            {
                if (Mode == WriteMode.Write)
                {
                    return null;
                }
                return m_btsPayload as ReadByteStream;
            }
        }

        protected ByteStream m_btsPayload;

        public PacketWrapper(int lastAck, int iPacketStartNumber, int iMaxBytes)
        {
            Mode = WriteMode.Write;

            m_iLastAckPackageFromPerson = lastAck;

            m_iStartPacketNumber = iPacketStartNumber;

            m_btsPayload = new WriteByteStream(iMaxBytes);

            ByteStream.Serialize(WriteStream, ref m_iLastAckPackageFromPerson);
            ByteStream.Serialize(WriteStream, ref m_iStartPacketNumber);

        }

        public PacketWrapper(Byte[] bData)
        {
            Mode = WriteMode.Read;

            m_btsPayload = new ReadByteStream(bData);

            ByteStream.Serialize(ReadStream, ref m_iLastAckPackageFromPerson);
            ByteStream.Serialize(ReadStream, ref m_iStartPacketNumber);
        }

        public void AddDataPacket(DataPacket pakPacket)
        {
            byte bID = (byte)pakPacket.GetTypeID;
            //encode packet type
            ByteStream.Serialize(WriteStream, ref bID);

            pakPacket.EncodePacket(this);
        }

    }

    public abstract partial class DataPacket
    {

        public static int GetPacketType(PacketWrapper pkwPacketWrapper)
        {
            byte btype = 0;

            //get the next packet type
            ByteStream.Serialize(pkwPacketWrapper.ReadStream, ref btype);

            return btype;
        }

        public abstract int GetTypeID { get; }

        public abstract int PacketSize { get; }

        public abstract void DecodePacket(PacketWrapper pkwPacketWrapper);

        public abstract void EncodePacket(PacketWrapper pkwPacketWrapper);

        //applys an offset to the data read for the byte used for packet type
        public virtual int ApplyDataReadOffset(int iDataReadHead)
        {
            return iDataReadHead;
        }
    }
}