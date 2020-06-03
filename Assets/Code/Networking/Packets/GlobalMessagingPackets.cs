using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Utility;

namespace Networking
{
    public class GlobalMessagePacket : DataPacket
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

        //the 
        public PeerMessageNode m_pmnMessage;

        public override int PacketPayloadSize
        {
            get
            {
                return m_pmnMessage.MessageSize();
            }
        }

        public override void DecodePacket(ReadByteStream rbsByteStream)
        {
            if(m_pmnMessage == null)
            {
                m_pmnMessage = new PeerMessageNode();
            }

            m_pmnMessage.DecodePacket(rbsByteStream);
        }

        public override void EncodePacket(WriteByteStream wbsByteStream)
        {
            m_pmnMessage.EncodePacket(wbsByteStream);
        }
    }

    public class GlobalLinkRequest : DataPacket
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

        //the hash of the link the peer is requesting 
        public long m_lRequestedLinkHash;

        public override int PacketPayloadSize
        {
            get
            {
                return ByteStream.DataSize(m_lRequestedLinkHash);
            }
        }

        public override void DecodePacket(ReadByteStream rbsByteStream)
        {
            ByteStream.Serialize(rbsByteStream, ref m_lRequestedLinkHash);
        }

        public override void EncodePacket(WriteByteStream wbsByteStream)
        {
            ByteStream.Serialize(wbsByteStream, ref m_lRequestedLinkHash); ;
        }
    }

    public class GlobalChainLinkPacket : DataPacket
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

        //chain link
        public ChainLink m_chlLink;

        public override int PacketPayloadSize
        {
            get
            {
                return m_chlLink.LinkDataSize();
            }
        }

        public override void DecodePacket(ReadByteStream rbsByteStream)
        {
            //make sure chain link exists to decode the data 
            if(m_chlLink == null)
            {
                m_chlLink = new ChainLink();
            }

            m_chlLink.DecodePacket(rbsByteStream);
        }

        public override void EncodePacket(WriteByteStream wbsByteStream)
        {
            m_chlLink.EncodePacket(wbsByteStream);
        }
    }

    public class GlobalChainStatePacket:DataPacket
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

        public GlobalMessageStartStateCandidate m_sscStartStateCandidate;

        public override int PacketPayloadSize
        {
            get
            {
                return NetworkingByteStream.DataSize(m_sscStartStateCandidate);
            }
        }

        public override void DecodePacket(ReadByteStream rbsByteStream)
        {
            NetworkingByteStream.Serialize(rbsByteStream, ref m_sscStartStateCandidate);
        }

        public override void EncodePacket(WriteByteStream wbsByteStream)
        {
            NetworkingByteStream.Serialize(wbsByteStream, ref m_sscStartStateCandidate);
        }
    }
}
