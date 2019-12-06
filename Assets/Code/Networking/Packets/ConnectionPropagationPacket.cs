using System;
using System.Text;

namespace Networking
{

    #region ConnectionNegotiationBasePacket
    public class ConnectionNegotiationBasePacket:DataPacket
    {
        public static int TypeID
        {
            get
            {
                return 7;
            }
        }

        public override int GetTypeID
        {
            get
            {
                return TypeID;
            }
        }

        public Int64 m_lFrom;
        public Int64 m_lTo;
        public Int32 m_iIndex;
        public DateTime m_dtmNegotiationStart;

        public override int PacketPayloadSize
        {
            get
            {
                return ByteStream.DataSize(this);
            }
        }

        public override void DecodePacket(ReadByteStream rbsByteStream)
        {
            ByteStream.Serialize(rbsByteStream, this);
        }

        public override void EncodePacket(WriteByteStream wbsByteStream)
        {
            ByteStream.Serialize(wbsByteStream, this);
        }
    }

    //used to serialize and deserialize packet
    public partial class ByteStream
    {
        public static void Serialize(ReadByteStream rbsByteStream, ConnectionNegotiationBasePacket Input)
        {
            Serialize(rbsByteStream, ref Input.m_lFrom);
            Serialize(rbsByteStream, ref Input.m_lTo);
            Serialize(rbsByteStream, ref Input.m_iIndex);
            Serialize(rbsByteStream, ref Input.m_dtmNegotiationStart);
        }

        public static void Serialize(WriteByteStream rbsByteStream, ConnectionNegotiationBasePacket Input)
        {
            Serialize(rbsByteStream, ref Input.m_lFrom);
            Serialize(rbsByteStream, ref Input.m_lTo);
            Serialize(rbsByteStream, ref Input.m_iIndex);
            Serialize(rbsByteStream, ref Input.m_dtmNegotiationStart);
        }

        public static int DataSize(ConnectionNegotiationBasePacket Input)
        {
            int iSize = DataSize(Input.m_lFrom);
            iSize += DataSize(Input.m_lTo);
            iSize += DataSize(Input.m_iIndex);
            iSize += DataSize(Input.m_dtmNegotiationStart);
            return iSize;
        }
    }
    #endregion

    #region ConnectionNegotiationMessagePacket
    public class ConnectionNegotiationMessagePacket : ConnectionNegotiationBasePacket
    {
        public static new int TypeID
        {
            get
            {
                return 8;
            }
        }

        public override int GetTypeID
        {
            get
            {
                return TypeID;
            }
        }

        public string m_strConnectionNegotiationMessage;

        public override int PacketPayloadSize
        {
            get
            {
                return ByteStream.DataSize(this);
            }
        }

        public override void DecodePacket(ReadByteStream rbsByteStream)
        {
            ByteStream.Serialize(rbsByteStream, this);
        }

        public override void EncodePacket(WriteByteStream wbsByteStream)
        {
            ByteStream.Serialize(wbsByteStream, this);
        }
    }

    //used to serialize and deserialize packet
    public partial class ByteStream
    {
        public static void Serialize(ReadByteStream rbsByteStream, ConnectionNegotiationMessagePacket Input)
        {
            Serialize(rbsByteStream, (ConnectionNegotiationBasePacket)Input);
            Serialize(rbsByteStream, ref Input.m_strConnectionNegotiationMessage);
        }

        public static void Serialize(WriteByteStream rbsByteStream, ConnectionNegotiationMessagePacket Input)
        {
            Serialize(rbsByteStream, (ConnectionNegotiationBasePacket)Input);
            Serialize(rbsByteStream, ref Input.m_strConnectionNegotiationMessage);
        }

        public static int DataSize(ConnectionNegotiationMessagePacket Input)
        {
            int iSize = DataSize((ConnectionNegotiationBasePacket)Input);
            iSize += DataSize(Input.m_strConnectionNegotiationMessage);

            return iSize;
        }
    }

    #endregion

    //#region ConnectionNegotiationOpenPacket
    ///// <summary>
    ///// this packet tells a peer to open or replace a connection 
    ///// </summary>
    //public class ConnectionNegotiationOpenPacket : ConnectionNegotiationBasePacket
    //{
    //    public static new int TypeID { get; } = 3;
           

    //    public override int GetTypeID
    //    {
    //        get
    //        {
    //            return TypeID;
    //        }
    //    }

    //    //in the future add signature public key or other user validation data here

    //    public override int PacketPayloadSize
    //    {
    //        get
    //        {
    //            return ByteStream.DataSize(this);
    //        }
    //    }

    //    public override void DecodePacket(ReadByteStream rbsByteStream)
    //    {
    //        ByteStream.Serialize(rbsByteStream, this);
    //    }

    //    public override void EncodePacket(WriteByteStream wbsByteStream)
    //    {
    //        ByteStream.Serialize(wbsByteStream, this);
    //    }
    //}

    ////serialization of open connection packet
    //public partial class ByteStream
    //{
    //    public static void Serialize(ReadByteStream ByteStream,ConnectionNegotiationOpenPacket Input)
    //    {
    //        Serialize(ByteStream, (ConnectionNegotiationBasePacket)Input);
    //    }

    //    public static void Serialize(WriteByteStream ByteStream,ConnectionNegotiationOpenPacket Input)
    //    {
    //        Serialize(ByteStream, (ConnectionNegotiationBasePacket)Input);
    //    }

    //    public static int DataSize(ConnectionNegotiationOpenPacket Input)
    //    {
    //        int iSize = DataSize((ConnectionNegotiationBasePacket)Input);
    //        return iSize;
    //    }
    //}

    //#endregion

}
