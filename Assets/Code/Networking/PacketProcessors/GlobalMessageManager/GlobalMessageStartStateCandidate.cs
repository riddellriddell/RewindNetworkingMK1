using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;
using Utility;

namespace Networking
{
    //this class is a wrapper class used for deciding the start state of the global messaging system 
    public class GlobalMessageStartStateCandidate
    {
        public static long GenerateHash(GlobalMessagingState gsmState,long lChainLinkHash)
        {
            //get size of all data
            int iSize = NetworkingByteStream.DataSize(gsmState);
            iSize += ByteStream.DataSize(lChainLinkHash);

            //craete buffer
            WriteByteStream wbsByteStream = new WriteByteStream(iSize);


            //serialize data 
            NetworkingByteStream.Serialize(wbsByteStream, ref gsmState);
            ByteStream.Serialize(wbsByteStream, ref lChainLinkHash);

            //compute hash
            using (MD5 md5Hash = MD5.Create())
            {
                //compute hash and store it
                Byte[] bHash = md5Hash.ComputeHash(wbsByteStream.GetData());

                //get the first 8 of the 16 bytes of the hash
                return BitConverter.ToInt64(bHash, 0);
            }

        }

        #region SentValues

        //a state that could possibly be used as a start state for the chain
        public GlobalMessagingState m_gmsStateCandidate;

        //the hash of the next link built off this state
        public long m_lNextLinkHash;

        #endregion

        #region CalculatedValus

        //the peer that proposed this start state
        public long m_lStartStateCreatorPeerID;

        //the hash of this state 
        public long m_lHashOfStateCandidate;

        //a refference to the next link
        public ChainLink m_chlNextLink;

        //is there a state on the same chain that is older
        public bool m_bIsOldestStateOnChain = true;

        //the score of this start state
        //(currently just the number of peers on chains starting from this state)
        public int m_iStartStateScore = 1;

        #endregion

        public void Init(GlobalMessagingState gmsState,long lNextChainLinkHash)
        {
            m_gmsStateCandidate = gmsState;
            m_lNextLinkHash = lNextChainLinkHash;

            m_lHashOfStateCandidate = GenerateHash(gmsState, lNextChainLinkHash);
        }
    }

    public partial class NetworkingByteStream
    {
        public static int DataSize(GlobalMessageStartStateCandidate Input)
        {
            //get size of all data
            int iSize = NetworkingByteStream.DataSize(Input.m_gmsStateCandidate);
            iSize += ByteStream.DataSize(Input.m_lNextLinkHash);

            return iSize;
        }

        public static void Serialize(ReadByteStream rbsByteStream, ref GlobalMessageStartStateCandidate Input)
        {
            if(Input == null)
            {
                Input = new GlobalMessageStartStateCandidate();
            }

            //serialize data 
            NetworkingByteStream.Serialize(rbsByteStream, ref Input.m_gmsStateCandidate);
            ByteStream.Serialize(rbsByteStream, ref Input.m_lNextLinkHash);

        }

        public static void Serialize(WriteByteStream wbsByteStream, ref GlobalMessageStartStateCandidate Input)
        {
            //serialize data 
            NetworkingByteStream.Serialize(wbsByteStream, ref Input.m_gmsStateCandidate);
            ByteStream.Serialize(wbsByteStream, ref Input.m_lNextLinkHash);

        }
    }
}
