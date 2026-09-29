using System;
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

    #region PeerHasLinkNotification  
    public class PeerHasLinkPacket:DataPacket
    {
        public static int TypeID { get; set; } = int.MinValue;

        public override int GetTypeID
        {
            get
            {
                return TypeID;
            }
        }

        public SortingValue m_svaLinkSortValue;
        public long m_lLinkHash;

        public override int PacketPayloadSize
        {
            get
            {
                return NetworkingByteStream.DataSize(this);
            }
        }

        public override void DecodePacket(ReadByteStream rbsByteStream)
        {
            NetworkingByteStream.Serialize(rbsByteStream, this);
        }

        public override void EncodePacket(WriteByteStream wbsByteStream)
        {
            NetworkingByteStream.Serialize(wbsByteStream, this);
        }
    }
    
    public partial class NetworkingByteStream
    {
        public static void Serialize(ReadByteStream rbsByteStream, PeerHasLinkPacket Input)
        {
            ByteStream.Serialize(rbsByteStream, ref Input.m_svaLinkSortValue.m_lSortValueA);
            ByteStream.Serialize(rbsByteStream, ref Input.m_svaLinkSortValue.m_lSortValueB);
            ByteStream.Serialize(rbsByteStream, ref Input.m_lLinkHash);
        }

        public static void Serialize(WriteByteStream rbsByteStream, PeerHasLinkPacket Input)
        {
            ByteStream.Serialize(rbsByteStream, ref Input.m_svaLinkSortValue.m_lSortValueA);
            ByteStream.Serialize(rbsByteStream, ref Input.m_svaLinkSortValue.m_lSortValueB);
            ByteStream.Serialize(rbsByteStream, ref Input.m_lLinkHash);
        }

        public static int DataSize(PeerHasLinkPacket Input)
        {
            int iSize = ByteStream.DataSize(Input.m_svaLinkSortValue.m_lSortValueA);
            iSize += ByteStream.DataSize(Input.m_svaLinkSortValue.m_lSortValueB);
            iSize += ByteStream.DataSize(Input.m_lLinkHash);
            return iSize;
        }
    }
    #endregion
    
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

        public static bool HasBeenAddedToClassFactory
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
    
    //this is used to tell other peers when you started simulating the global chain
    //this is used so other peers won't expect a chain state from you if you don't have it
    public class GlobalChainSimulationStartTimePacket:DataPacket
    {
        public static int TypeID { get; set; } = int.MinValue;

        public static bool HasBeenAddedToClassFactory
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

        public DateTime m_dtmStartOfChainSimulation;

        public NetworkGlobalMessengerProcessor.State m_staGlobalMessagingState;

        public override int PacketPayloadSize
        {
            get
            {
                int size = ByteStream.DataSize(m_dtmStartOfChainSimulation.Ticks);
                size += sizeof(int);

                return size;
            }
        }

        public override void DecodePacket(ReadByteStream rbsByteStream)
        {
            ByteStream.Serialize(rbsByteStream, ref m_dtmStartOfChainSimulation);

            int enumAsInt = 0;
            ByteStream.Serialize(rbsByteStream, ref enumAsInt);
            m_staGlobalMessagingState = (NetworkGlobalMessengerProcessor.State)enumAsInt;
        }

        public override void EncodePacket(WriteByteStream wbsByteStream)
        {
            ByteStream.Serialize(wbsByteStream, ref m_dtmStartOfChainSimulation);
            int enumAsInt = (int)m_staGlobalMessagingState;
            ByteStream.Serialize(wbsByteStream, ref enumAsInt);
        }
    }
}

