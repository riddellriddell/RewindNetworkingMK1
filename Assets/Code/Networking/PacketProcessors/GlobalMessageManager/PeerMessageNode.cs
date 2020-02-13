using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;

namespace Networking
{
    public class PeerMessageNode
    {
        public const int c_iSignatureSize = 8;

        public static SortingValue SortingValueForTime(DateTime dtmTime)
        {
            SortingValue svaOutput = SortingValue.MinValue;

            svaOutput.m_lSortValueA = (ulong)dtmTime.Ticks;

            return svaOutput;
        }

        #region SentValues
        //the peer that created this message
        public long m_lPeerID;

        //the signed hash of this message plus additional packed data 
        public Byte[] m_bSignature;

        //the hash of the "Best" Chain Link the peer had when it created this message 
        public long m_lChainLinkHeadHash;

        //the order number of this message from this peer
        public uint m_iPeerMessageIndex;

        //the hash of the previouse message from this peer 
        public long m_lPreviousMessageHash;

        //the time the message was created
        public DateTime m_dtmMessageCreationTime;

        //the message type
        public byte m_bMessageType;

        //the message 
        public GlobalMessageBase m_gmbMessage;

        #endregion

        #region CalculatedValues

        public Byte[] m_bPayload;

        public long m_lMessagePayloadHash;

        //the index of this peer in the entire chain
        //public long m_lGlobalMessageIndex;

        //value used to sort all messages
        public SortingValue m_svaMessageSortingValue;

        //the state of the global messaging system after processing this message
       // public GlobalMessagingState m_gmsState;

        #endregion

        public void BuildPayloadHash()
        {
            //compute hash
            using (MD5 md5Hash = MD5.Create())
            {
                //compute hash and store it
                Byte[] bHash = md5Hash.ComputeHash(m_bPayload);

                //get the first 8 of the 16 bytes of the hash
                m_lMessagePayloadHash = BitConverter.ToInt64(bHash, 0);
            }
        }

        public void BuildPayloadArray()
        {
            //get size
            int iSize = PayloadSize();

            //create large enough buffer
            m_bPayload = new Byte[iSize];

            //create write stream
            WriteByteStream wbsWriteBuffer = new WriteByteStream(m_bPayload);

            //serialize the chain link head hash
            ByteStream.Serialize(wbsWriteBuffer, ref m_lChainLinkHeadHash);

            //serialize the message index
            ByteStream.Serialize(wbsWriteBuffer, ref m_iPeerMessageIndex);

            //serialize the parent message hash
            ByteStream.Serialize(wbsWriteBuffer, ref m_lPreviousMessageHash);

            //serialize the time the message was created
            ByteStream.Serialize(wbsWriteBuffer, ref m_dtmMessageCreationTime);

            //serialize message type
            ByteStream.Serialize(wbsWriteBuffer, ref m_bMessageType);

            //serialize message
            m_gmbMessage.Serialize(wbsWriteBuffer);

        }

        //TODO: make this actually criptographic
        public void SignMessage()
        {
            m_bSignature = BitConverter.GetBytes(m_lMessagePayloadHash);
        }

        public void CalculateSortingValue()
        {
            Byte[] bSortingValue = new byte[SortingValue.c_TotalBytes];

            int iStartIndex = 0;

            //store the message creation time
            Array.Copy(BitConverter.GetBytes(m_dtmMessageCreationTime.Ticks), 0, bSortingValue, iStartIndex, sizeof(Int64));

            iStartIndex += sizeof(Int64);

            //store the link index
            Array.Copy(BitConverter.GetBytes(m_iPeerMessageIndex), bSortingValue, sizeof(UInt32));

            iStartIndex += sizeof(UInt32);

            //store part of the hash
            Array.Copy(BitConverter.GetBytes(m_lMessagePayloadHash), 0, bSortingValue, iStartIndex, sizeof(Int32));

            //should also store part of the peer id here 

            //create sorting value
            m_svaMessageSortingValue = new SortingValue(bSortingValue);
        }

        public void DecodePayloadArray(ClassWithIDFactory cifClassFactory)
        {
            //create write stream
            ReadByteStream rbsReadBuffer = new ReadByteStream(m_bPayload);

            //serialize the chain link head hash
            ByteStream.Serialize(rbsReadBuffer, ref m_lChainLinkHeadHash);

            //serialize the message index
            ByteStream.Serialize(rbsReadBuffer, ref m_iPeerMessageIndex);

            //serialize the parent message hash
            ByteStream.Serialize(rbsReadBuffer, ref m_lPreviousMessageHash);

            //serialize the time the message was created
            ByteStream.Serialize(rbsReadBuffer, ref m_dtmMessageCreationTime);

            //serialize message type
            ByteStream.Serialize(rbsReadBuffer, ref m_bMessageType);

            //create correct payload message type using class factory 
            m_gmbMessage = cifClassFactory.CreateType<GlobalMessageBase>(m_bMessageType);

            //serialize message
            m_gmbMessage.Serialize(rbsReadBuffer);
        }
        
        public int MessageSize()
        {
            int iSize = 0;

            iSize += ByteStream.DataSize(m_lPeerID);
            iSize += c_iSignatureSize;
            iSize += ByteStream.DataSize(m_bPayload.Length);
            iSize += m_bPayload.Length;

            return iSize;
        }

        public void EncodePacket(WriteByteStream wbsByteStream)
        {
            // get the peer id
            ByteStream.Serialize(wbsByteStream, ref m_lPeerID);

            //get write signature 
            ByteStream.Serialize(wbsByteStream, ref m_bSignature, c_iSignatureSize);

            //get the payload size 
            int iPayloadSize = m_bPayload.Length;
            ByteStream.Serialize(wbsByteStream, ref iPayloadSize);

            //in the future a segment of the payload will be included in the signature to be 
            //more data efficient

            //serialize payload
            ByteStream.Serialize(wbsByteStream, ref m_bPayload, iPayloadSize);
        }

        public void DecodePacket(ReadByteStream rbsByteStream)
        {
            // get the peer id
            ByteStream.Serialize(rbsByteStream, ref m_lPeerID);

            //get write signature 
            ByteStream.Serialize(rbsByteStream, ref m_bSignature, c_iSignatureSize);

            //get the payload size 
            int iPayloadSize = 0;
            ByteStream.Serialize(rbsByteStream, ref iPayloadSize);

            //in the future a segment of the payload will be included in the signature to be 
            //more data efficient
            m_bPayload = new Byte[iPayloadSize];

            //Get payload
            ByteStream.Serialize(rbsByteStream, ref m_bPayload, iPayloadSize);
        }
        
        protected int PayloadSize()
        {
            int iSize = 0;

            iSize += ByteStream.DataSize(m_lChainLinkHeadHash);

            //add the size of the order number
            iSize += ByteStream.DataSize(m_iPeerMessageIndex);

            //add previouse message hash size 
            iSize += ByteStream.DataSize(m_lPreviousMessageHash);

            //add message creation time
            iSize += ByteStream.DataSize(m_dtmMessageCreationTime);

            //add message type
            iSize += ByteStream.DataSize(m_bMessageType);

            //add message size
            iSize += m_gmbMessage.DataSize();

            return iSize;
        }

        protected bool VerifySignature()
        {
            long lSigHash = BitConverter.ToInt64(m_bSignature, 0);

            if (lSigHash == m_lMessagePayloadHash)
            {
                return true;
            }
            else
            {
                //signature failed 
                return false;
            }
        }


    }

    public interface ISortedMessage
    {
        //a number used to sort the message based on 
        //the time the message was created (top)
        //the message index
        // the message type
        //the peer that created the message
        //the message hash

        SortingValue SortingValue { get; set; }
    }

    public interface IPeerMessageNode : ICloneable, ISortedMessage, IEncriptedMessageInterface
    {
        //the global message this is derived from
        GlobalMessageBase GlobalMessage { get; }

        //the values in this region are used to cronologically sort all messages into a 
        //linear list
        #region SortingValues

        //the source peer
        long SourcePeerID { get; }

        //the time this message was created or in the case of a conflict the time of the 
        //last valid message
        DateTime MessageCreationTime { get; }

        //for sorting reasons is this packet a confict packet and what level of conflict is it for
        int TypeSorting { get; }

        //the index in the peer message chain
        uint PeerMessageIndex { get; }
        
        //the hash of this node 
        long NodeHash { get; }

        #endregion

        #region CalculatedValues

        //global messaging state after this input is applied 
        GlobalMessagingState State { get; }

        //the index of this peer in the entire chain
        int GlobalMessageIndex { get; set; }

        #endregion

        // the number of times a new node has been added behind the all recieved message head
        // this is used to keep track of which inputs need to be echoed to which peers 
        // when there is a hash conflict 
        // this value is not synchronised with other peers
        int GlobalBranchNumber { get; set; }

        //rolling hash chain for all inputs
        //this is used to detect conflicts/differences in inputs between users 
        //may neet to change format
        //GlobalMessageNodeHash GlobalHash { get; set; }


        //compares to other node and returns < 0 if before in global message chain and > 0 if after and 0 if the same
        int CompareTo(IPeerMessageNode pmnCompareTo);

        //check that data in node was created by who they say it was 
        //bool ValidateNodeSources(GlobalMessageKeyManager gkmKeyManager);
    }

    public interface IPeerChannelVoteMessageNode : IPeerMessageNode
    {
        //the vote per peer
        //first value = action to perform (0 = kick, 1 = join)
        //second value = target peer
        Tuple<byte,long>[] ActionPerPeer { get;}
    }

    public partial class ByteStream
    {
        public static int DataSize(IPeerMessageNode pmnMessage)
        {
            throw new Exception("Specialized version of data size function not found");
        }

        public static void Serialize(WriteByteStream wbsByteStream, ref IPeerMessageNode Input)
        {
            throw new Exception("Specialized version of write serialize function not found");
        }

        public static void Serialize(ReadByteStream rbsByteStream, ref IPeerMessageNode Input)
        {
            throw new Exception("Specialized version of write serialize function not found");
        }
    }
}
