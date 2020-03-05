using System;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace Networking
{
    public class ChainLink
    {
        #region ConstValues
        public const int c_iSignatureSize = 64;
        #endregion

        #region SentData

        public long m_lPeerID;

        //criptographic signature (not in used)
        public Byte[] m_bSignature;

        public uint m_iLinkIndex;

        public long m_lPreviousLinkHash;

        //list of all the inputs included in this chain and who the inputs belong to
        public List<PeerMessageNode> m_pmnMessages = new List<PeerMessageNode>();

        #endregion

        #region CalculatedValues
        #region CalculatedFromLinkValues
        //the hash of this links payload
        public long m_lLinkPayloadHash;

        //all the data in the SentData region excluding the peer id and signature serialized 
        public Byte[] m_bPayload;

        //value used to sort chain links 
        public SortingValue m_svaChainSortingValue;

        //the global messaging state at the end of processing this message
        public GlobalMessagingState m_gmsState;

        #endregion
        #region CalculatedUsingChanAndMessageBuffers 

        //the parent chain link
        public ChainLink m_chlParentChainLink;

        //check if this link is conencted to base
        public bool m_bIsConnectedToBase;

        //the lenght of this chain
        public uint m_iChainLength;

        //the number of messages in chain
        public ulong m_lChainMessageCount;

        // is this branch accepted by channel as the true branch
        public List<bool> m_bIsChannelBranch;
        #endregion
        #endregion

        public void Init(List<PeerMessageNode> pmnMessagesInLink,long lCreatingPeer,uint iLinkIndex,long lPreviousLinkHash)
        {
            //store key values
            m_pmnMessages = pmnMessagesInLink;
            m_lPeerID = lCreatingPeer;
            m_iLinkIndex = iLinkIndex;
            m_lPreviousLinkHash = lPreviousLinkHash;

            //build payload array 
            BuildPayloadArray();
            //get the hash from payload
            BuildPayloadHash();
            //use hash to sign link
            SignData();
            //use hash to calcuate sorting order
            CalculateSortingValue();
        }

        //TODO: this should be moved into a byte stream Data Size Function
        public int LinkDataSize()
        {
            int iSize = ByteStream.DataSize(m_lPeerID);

            iSize += c_iSignatureSize;

            int iPayloadSize = m_bPayload.Length;

            //in the future a percent of the payload will be added to the signature
            //and removed from the payload size to be more efficient

            iSize += ByteStream.DataSize(iPayloadSize);

            iSize += iPayloadSize;

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

        public void CalculateLocalValuesForRecievedLink(ClassWithIDFactory cifClassFactory)
        {
            VerifySignature();

            DecodePayloadArray(cifClassFactory);

            BuildPayloadHash();

            CalculateSortingValue();
        }

        //converts messages and link details to a single byte array
        public void BuildPayloadArray()
        {
            int iSize = GetPayloadSize();

            m_bPayload = new byte[iSize];

            WriteByteStream wbsByteStream = new WriteByteStream(m_bPayload);

            //serialize link index
            ByteStream.Serialize(wbsByteStream, ref m_iLinkIndex);

            //serialize previouse link hash
            ByteStream.Serialize(wbsByteStream, ref m_lPreviousLinkHash);

            //serialize all messages
            int iCount = m_pmnMessages.Count;
            ByteStream.Serialize(wbsByteStream, ref iCount);

            for (int i = 0; i < iCount; i++)
            {
                 m_pmnMessages[i].EncodePacket(wbsByteStream);
            }
        }
               
        //converts a byte array back to messages and link details
        public void DecodePayloadArray(ClassWithIDFactory cifClassFactory)
        {
            ReadByteStream rbsByteStream = new ReadByteStream(m_bPayload);

            //serialize link index
            ByteStream.Serialize(rbsByteStream, ref m_iLinkIndex);

            //serialize previouse link hash
            ByteStream.Serialize(rbsByteStream, ref m_lPreviousLinkHash);

            //serialize all messages
            //serialize all messages
            int iCount = 0;
            ByteStream.Serialize(rbsByteStream, ref iCount);

            m_pmnMessages.Clear();

            for (int i = 0; i < iCount; i++)
            {
                PeerMessageNode pmnMessage = new PeerMessageNode();

                pmnMessage.DecodePacket(rbsByteStream);
                pmnMessage.DecodePayloadArray(cifClassFactory);

                //calculate the hash for the payload
                pmnMessage.BuildPayloadHash();

                //crate the sorting value for the message 
                pmnMessage.CalculateSortingValue();

                //add to messagees in link
                m_pmnMessages.Add(pmnMessage);
            }
        }
               
        public void CaluclateGlobalMessagingStateAtEndOflink(long lLocalPeerID, GlobalMessagingState gmsStateAtLinkStart)
        {
            if(m_gmsState == null)
            {
                int iMaxChannels = gmsStateAtLinkStart.m_gmcMessageChannels.Count;
                m_gmsState = new GlobalMessagingState(iMaxChannels);
            }

            m_gmsState.ResetToState(gmsStateAtLinkStart);

            for(int i = 0; i < m_pmnMessages.Count; i++)
            {
                m_gmsState.ProcessMessage(lLocalPeerID, m_pmnMessages[i]);
            }

            //check that end state matches expected state
            ChainLinkEndStateVerrifier.RegisterLink(this, lLocalPeerID);
        }

        protected void BuildPayloadHash()
        {
            //compute hash
            using (MD5 md5Hash = MD5.Create())
            {
                //compute hash and store it
                Byte[] bHash = md5Hash.ComputeHash(m_bPayload);

                //get the first 8 of the 16 bytes of the hash
                m_lLinkPayloadHash = BitConverter.ToInt64(bHash, 0);
            }
        }

        protected void SignData()
        {
            m_bSignature = new Byte[c_iSignatureSize];

            //copy in 8 hash bytes
            Array.Copy(BitConverter.GetBytes(m_lLinkPayloadHash), m_bSignature, sizeof(long));
        }

        protected bool VerifySignature()
        {
            long lSigHash = BitConverter.ToInt64(m_bSignature, 0);

            if(lSigHash == m_lLinkPayloadHash)
            {
                return true;
            }
            else
            {
                //signature failed 
                return false;
            }
        }

        protected int GetPayloadSize()
        {
            int iSize = 0;
            iSize += ByteStream.DataSize(m_iLinkIndex);
            iSize += ByteStream.DataSize(m_lPreviousLinkHash);

            iSize += ByteStream.DataSize(m_pmnMessages.Count);
            for (int i = 0; i < m_pmnMessages.Count; i++)
            {
                iSize +=  m_pmnMessages[i].MessageSize();
            }

            return iSize;
        }

        public void CalculateSortingValue()
        {
            //Byte[] bSortingValue = new byte[SortingValue.c_TotalBytes];

            //int iStartIndex = 0;
                                 
            ////store part of the hash
            //Array.Copy(BitConverter.GetBytes(m_lLinkPayloadHash), 0, bSortingValue, iStartIndex, sizeof(Int32));

            //iStartIndex += sizeof(UInt32);

            ////store the peer id
            //Array.Copy(BitConverter.GetBytes(m_lPeerID), 0, bSortingValue, iStartIndex, sizeof(Int64));

            //iStartIndex += sizeof(Int64);

            ////store the link index
            //Array.Copy(BitConverter.GetBytes(m_iLinkIndex), bSortingValue, sizeof(UInt32));


            ulong lPartA = 0;
            ulong lPartB = 0;

            lPartA = m_iLinkIndex;

            lPartA = lPartA << sizeof(UInt32);

            ulong iPartAPeerID =  (ulong)m_lPeerID >> sizeof(UInt32);

            lPartA += iPartAPeerID;

            lPartB = (ulong)m_lPeerID << sizeof(UInt32);

            ulong lPartBHash = (ulong)m_lLinkPayloadHash >> sizeof(UInt32);
            
            m_svaChainSortingValue = new SortingValue(lPartA, lPartB);

        }

    }

    //public class ChainLink : IEncriptedMessageInterface
    //{
    //    public class SignedData
    //    {
    //        public static int s_iSigTotalSize = 64;

    //        public static int s_lPeerIDSize = 8;

    //        public static int s_iLinkIndexSize = 4;

    //        public static int s_iHashSize = 16;

    //        public static int s_iExtraData = s_iSigTotalSize - (s_iLinkIndexSize + (2 * s_iHashSize) + s_lPeerIDSize);

    //        public long m_lPeerID;

    //        public uint m_iLinkIndex;

    //        public byte[] m_bHashOfChainLink = new byte[s_iHashSize];

    //        public byte[] m_bHashOfPreviousLink = new byte[s_iHashSize];

    //        public byte[] m_bExtraLinkData = new byte[s_iExtraData];

    //        public byte[] SerializeSigData()
    //        {
    //            List<byte> bOutput = new List<byte>(sizeof(uint) + m_bHashOfChainLink.Length + m_bHashOfPreviousLink.Length);

    //            byte[] bPeerID = BitConverter.GetBytes(m_lPeerID);
    //            for (int i = 0; i < s_lPeerIDSize; i++)
    //            {
    //                bOutput.Add(bPeerID[i]);
    //            }

    //            byte[] bIndex = BitConverter.GetBytes(m_iLinkIndex);

    //            //serialize the link index
    //            for (int i = 0; i < s_iLinkIndexSize; i++)
    //            {
    //                bOutput.Add(bIndex[i]);
    //            }

    //            //serialize the hash for the chain
    //            for (int i = 0; i < s_iHashSize; i++)
    //            {
    //                bOutput.Add(m_bHashOfChainLink[i]);
    //            }

    //            //serialize the previouse hash
    //            for (int i = 0; i < s_iHashSize; i++)
    //            {
    //                bOutput.Add(m_bHashOfPreviousLink[i]);
    //            }

    //            //serialize the extra data
    //            for (int i = 0; i < s_iExtraData; i++)
    //            {
    //                bOutput.Add(m_bExtraLinkData[i]);
    //            }

    //            return bOutput.ToArray();
    //        }

    //        public void DeserializeSignatureArray(byte[] bSignatureData)
    //        {
    //            int ihead = 0;

    //            m_lPeerID = BitConverter.ToInt64(bSignatureData, ihead);

    //            ihead += s_lPeerIDSize;

    //            m_iLinkIndex = BitConverter.ToUInt32(bSignatureData, ihead);

    //            ihead += s_iLinkIndexSize;

    //            for (int i = 0; i < s_iHashSize; i++)
    //            {
    //                m_bHashOfChainLink[i] = bSignatureData[ihead];
    //                ihead++;
    //            }

    //            for (int i = 0; i < s_iHashSize; i++)
    //            {
    //                m_bHashOfPreviousLink[i] = bSignatureData[ihead];
    //                ihead++;
    //            }

    //            for (int i = 0; i < s_iExtraData; i++)
    //            {
    //                m_bExtraLinkData[i] = bSignatureData[ihead];
    //                ihead++;
    //            }
    //        }

    //        public void SetExtraData(byte[] bExtraData)
    //        {
    //            for (int i = 0; i < s_iExtraData; i++)
    //            {
    //                if (i < bExtraData.Length)
    //                {
    //                    m_bExtraLinkData[i] = bExtraData[i];
    //                }
    //                else
    //                {
    //                    m_bExtraLinkData[i] = 0;
    //                }
    //            }
    //        }

    //        public void ExtractExtraData(ref byte[] bExtraData)
    //        {
    //            for (int i = 0; i < s_iExtraData; i++)
    //            {
    //                if (i < bExtraData.Length)
    //                {
    //                    bExtraData[i] = m_bExtraLinkData[i];
    //                }
    //                else
    //                {
    //                    break;
    //                }
    //            }
    //        }
    //    }

    //    #region SentValues
    //    //signature by chain link creator validating link
    //    //contains hash of chain link, chain cycle index
    //    public byte[] m_bSignature;

    //    //the peer that created this chain link
    //    public long m_lPeerID;

    //    //list of all the inputs included in this chain and who the inputs belong to
    //    public List<IPeerMessageNode> m_pmnMessages;

    //    //the serialised data sent to the peers 
    //    public byte[] m_bLinkMessageData;
    //    #endregion

    //    #region ValueSentOnConnect
    //    //the state of the message sim at the end of the chain link
    //    public GlobalMessagingState m_gmsState;

    //    #region DecriptedValues
    //    //the data used to create the siganture 
    //    public SignedData m_sigSignatureData;

    //    #endregion
    //    #endregion


    //    #region CalculatedValues

    //    //value used to sort chain links 
    //    public GlobalChainLinkSortingValue m_csvChainSortingValue;

    //    //the parent chain link
    //    public ChainLink m_chlParentChainLink;

    //    //check if this link is conencted to base
    //    public bool m_bIsConnectedToBase;

    //    //the lenght of this chain
    //    public uint m_iChainLength;

    //    //the number of messages in chain
    //    public ulong m_lChainMessageCount;

    //    // is this branch accepted by channel as the true branch
    //    public List<bool> m_bIsChannelBranch;

    //    #endregion

    //    public void BuildMessageDataArray()
    //    {
    //        //get the amount of data to serialize
    //        int iSize = ByteStream.DataSize(m_lPeerID);

    //        iSize += ByteStream.DataSize(m_pmnMessages);

    //        WriteByteStream wbsWriteByteStream = new WriteByteStream(iSize);

    //        //serialize the peer id
    //        ByteStream.Serialize(wbsWriteByteStream, ref m_lPeerID);

    //        //serialize all the messages
    //        ByteStream.Serialize(wbsWriteByteStream, ref m_pmnMessages);

    //        //get result
    //        m_bLinkMessageData = wbsWriteByteStream.GetData();

    //        //update signature some of the message data is stored in the sig 
    //        //to reduce the signature overhead
    //        m_sigSignatureData.SetExtraData(m_bLinkMessageData);
    //    }

    //    public void BuildMessageDataHash()
    //    {
    //        //check there is message data to hash
    //        if (m_bLinkMessageData == null || m_bLinkMessageData.Length == 0)
    //        {
    //            //serialize message data and peer id
    //            BuildMessageDataArray();
    //        }

    //        using (MD5 md5Hash = MD5.Create())
    //        {
    //            //compute hash and store it
    //            m_sigSignatureData.m_bHashOfChainLink = md5Hash.ComputeHash(m_bLinkMessageData);
    //        }
    //    }

    //    #region Encription
    //    //check if the peer has the keys required to decode the target item
    //    public bool CanDecode(HashSet<long> lMissingKeys, GlobalMessageKeyManager mkmKeyManager)
    //    {
    //        bool bCanDecode = true;

    //        if (mkmKeyManager.ContainsKey(m_lPeerID) == false)
    //        {
    //            bCanDecode = false;
    //            lMissingKeys.Add(m_lPeerID);
    //        }

    //        for (int i = 0; i < m_pmnMessages.Count; i++)
    //        {
    //            if (mkmKeyManager.ContainsKey(m_pmnMessages[i].SourcePeerID) == false)
    //            {
    //                bCanDecode = false;
    //                lMissingKeys.Add(m_pmnMessages[i].SourcePeerID);
    //            }
    //        }

    //        return bCanDecode;
    //    }

    //    public void Encript(GlobalMessageKeyManager mkmKeyManager)
    //    {
    //        EncriptSignature(mkmKeyManager);
    //    }

    //    public void TryDecript(GlobalMessageKeyManager mkmKeyManager)
    //    {
    //        if (mkmKeyManager.m_plkPublicKeys.TryGetValue(m_lPeerID, out GlobalMessageKeyManager.PublicKey pbkPublicKey))
    //        {
    //            //use public key to decript signature

    //            //loop through all children and decript them
    //            for (int i = 0; i < m_pmnMessages.Count; i++)
    //            {
    //                m_pmnMessages[i].TryDecript(mkmKeyManager);
    //            }
    //        }
    //        else
    //        {
    //            throw new Exception("key manager does not have key to decript message");
    //        }
    //    }

    //    public void EncriptSignature(GlobalMessageKeyManager mkmKeyManager)
    //    {
    //        //check that it is the local peer trying to encipt its own message 
    //        if (mkmKeyManager.m_lLocalPeerID != m_lPeerID)
    //        {
    //            throw new Exception("Peer trying to encript message it did not create");
    //        }

    //        //check if hash has been caluclated 
    //        if (m_sigSignatureData.m_bHashOfChainLink == null || m_sigSignatureData.m_bHashOfChainLink.Length == 0)
    //        {
    //            //serialize class as well as build hash
    //            BuildMessageDataHash();
    //        }

    //        using (RSACryptoServiceProvider rsaRSA = new RSACryptoServiceProvider(GlobalMessageKeyManager.s_iKeySize))
    //        {
    //            rsaRSA.ImportParameters(mkmKeyManager.m_rprKeyData);

    //            m_bSignature = rsaRSA.Encrypt(m_sigSignatureData.SerializeSigData(), false);

    //        }
    //    }

    //    public void DecriptSignature(GlobalMessageKeyManager mkmKeyManager)
    //    {
    //        //decript the signature 
    //        using (RSACryptoServiceProvider rsaRSA = new RSACryptoServiceProvider(GlobalMessageKeyManager.s_iKeySize))
    //        {
    //            //get key for peer 
    //            if (mkmKeyManager.m_plkPublicKeys.TryGetValue(m_lPeerID, out GlobalMessageKeyManager.PublicKey pkyPublicKey))
    //            {
    //                rsaRSA.ImportParameters(pkyPublicKey.m_rprPublickey);
    //            }
    //            else
    //            {
    //                throw new Exception("Tried to decript message without correct key");
    //            }

    //            byte[] sigData = rsaRSA.Decrypt(m_sigSignatureData.SerializeSigData(), false);

    //            m_sigSignatureData = new SignedData();
    //            m_sigSignatureData.DeserializeSignatureArray(sigData);

    //            if(m_sigSignatureData.m_lPeerID != m_lPeerID)
    //            {

    //            }
    //        }

    //        //copy accross the extra data 


    //        //check if hash has been caluclated 
    //        if (m_sigSignatureData.m_bHashOfChainLink == null || m_sigSignatureData.m_bHashOfChainLink.Length == 0)
    //        {
    //            //serialize class as well as build hash
    //            BuildMessageDataHash();
    //        }


    //    }

    //    #endregion
    //}

    public partial class ByteStream
    {
        public static int DataSize(List<IPeerMessageNode> pmnMessages)
        {
            int iSize = 0;

            //add array count 
            iSize += sizeof(Int32);

            for (int i = 0; i < pmnMessages.Count; i++)
            {
                iSize += ByteStream.DataSize(pmnMessages[i]);
            }

            return iSize;
        }

        public static void Serialize(WriteByteStream wbsStream, ref List<IPeerMessageNode> Input)
        {
            int iCount = 0;

            if (Input != null)
            {
                //store item count
                iCount = Input.Count;
            }

            Serialize(wbsStream, ref iCount);

            //serialize each item
            for (int i = 0; i < iCount; i++)
            {
                IPeerMessageNode pmsMessage = Input[i];

                Serialize(wbsStream, ref pmsMessage);
            }
        }

        public static void Serialize(ReadByteStream rbsStream, ref List<IPeerMessageNode> Input)
        {

            //get the number of items
            int iCount = 0;

            Serialize(rbsStream, ref iCount);

            if (Input == null)
            {
                Input = new List<IPeerMessageNode>(iCount);
            }

            //deserialize each item 
            for (int i = 0; i < iCount; i++)
            {
                IPeerMessageNode pmsMessage = null;

                Serialize(rbsStream, ref pmsMessage);

                Input.Add(pmsMessage);
            }
        }
    }
}
