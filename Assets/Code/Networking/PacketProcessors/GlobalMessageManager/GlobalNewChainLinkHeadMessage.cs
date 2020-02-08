using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Networking
{
    //this class informs peers that message sender has selected a new "Best" chain link to base future chain links off 
    public class GlobalNewChainLinkHeadMessage : DataPacket
    {
        public static int TypeID { get; } = 15;

        public long m_lNewChainLinkHeadHash;

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
                return ByteStream.DataSize(m_lNewChainLinkHeadHash);
            }
        }

        public override void DecodePacket(ReadByteStream rbsByteStream)
        {
            ByteStream.Serialize(rbsByteStream,ref m_lNewChainLinkHeadHash);
        }

        public override void EncodePacket(WriteByteStream wbsByteStream)
        {
            ByteStream.Serialize(wbsByteStream, ref m_lNewChainLinkHeadHash);
        }
    }

}
