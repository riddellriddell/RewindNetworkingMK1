using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Networking
{
    //tell peer to send sim state for time 
    public class SimStateSyncRequestPacket : DataPacket
    {
        public static int TypeID { get; set; } = int.MinValue;

        public static bool HasBeedAddedToClassFactory
        {
            get
            {
                return TypeID != int.MinValue;
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
                return ByteStream.DataSize(m_dtmTimeOfSimData);
            }
        }

        public DateTime m_dtmTimeOfSimData;

        public override void DecodePacket(ReadByteStream rbsByteStream)
        {
            ByteStream.Serialize(rbsByteStream, ref m_dtmTimeOfSimData);
        }

        public override void EncodePacket(WriteByteStream wbsByteStream)
        {
            ByteStream.Serialize(wbsByteStream, ref m_dtmTimeOfSimData);
        }
    }

    //tell peer to send sim state for time 
    public class SimStateSyncHashMapPacket : DataPacket
    {
        public static int TypeID { get; set; } = int.MinValue;

        public static bool HasBeedAddedToClassFactory
        {
            get
            {
                return TypeID != int.MinValue;
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
                return ByteStream.DataSize(m_lSegmentHashes) + ByteStream.DataSize(m_iBytes);
            }
        }

        public long[] m_lSegmentHashes;

        public uint m_iBytes;

        public override void DecodePacket(ReadByteStream rbsByteStream)
        {
            ByteStream.Serialize(rbsByteStream, ref m_lSegmentHashes);
            ByteStream.Serialize(rbsByteStream, ref m_iBytes);
        }

        public override void EncodePacket(WriteByteStream wbsByteStream)
        {
            ByteStream.Serialize(wbsByteStream, ref m_lSegmentHashes);
            ByteStream.Serialize(wbsByteStream, ref m_iBytes);
        }
    }

    //tell peer to send sim state for time 
    public class SimSegmentSyncRequestPacket : DataPacket
    {
        public static int TypeID { get; set; } = int.MinValue;

        public static bool HasBeedAddedToClassFactory
        {
            get
            {
                return TypeID != int.MinValue;
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
                return ByteStream.DataSize(m_lSegmentHash);
            }
        }

        public long m_lSegmentHash;

        public override void DecodePacket(ReadByteStream rbsByteStream)
        {
            ByteStream.Serialize(rbsByteStream, ref m_lSegmentHash);
        }

        public override void EncodePacket(WriteByteStream wbsByteStream)
        {
            ByteStream.Serialize(wbsByteStream, ref m_lSegmentHash);
        }
    }

    public class SimSegmentSyncDataPacket : DataPacket
    {
        public static int TypeID { get; set; } = int.MinValue;

        public static bool HasBeedAddedToClassFactory
        {
            get
            {
                return TypeID != int.MinValue;
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
                return ByteStream.DataSize(m_bSegmentData);
            }
        }

        public byte[] m_bSegmentData;

        public override void DecodePacket(ReadByteStream rbsByteStream)
        {
            ByteStream.Serialize(rbsByteStream, ref m_bSegmentData);
        }

        public override void EncodePacket(WriteByteStream wbsByteStream)
        {
            ByteStream.Serialize(wbsByteStream, ref m_bSegmentData);
        }
    }
}