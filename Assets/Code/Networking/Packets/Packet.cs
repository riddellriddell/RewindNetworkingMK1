using System;
using Utility;

namespace Networking
{
    public class PacketWrapper
    {
        //the number of packets that are used as header / not payload data 
        public static int HeaderSize
        {
            get
            {
                int iSize = sizeof(Int32); //last acknowledged packet 
                iSize += sizeof(Int32); //paket start index / number;

                return iSize;
            }
        }

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

        public PacketWrapper(byte[] bData)
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

            //encode packet payload
            pakPacket.EncodePacket(this.WriteStream);
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

        //the additional type data needed to encode this packet outside of the payload data
        public static int TypeHeaderSize
        {
            get
            {
                return 1;
            }
        }


        //the total size of the packet including the type header byte
        public int PacketTotalSize
        {
            get
            {
                return PacketPayloadSize + TypeHeaderSize;
            }
        }

        public abstract int PacketPayloadSize { get; }

        //not to be serialized
        //the source of the packt
        //public long Source { get; }

        public abstract void DecodePacket(ReadByteStream rbsByteStream);

        public abstract void EncodePacket(WriteByteStream wbsByteStream);

    }
}