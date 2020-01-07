using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Networking
{
    public abstract class GlobalMessageBase
    {
        //the signature used to verify this message was sent by peer
        //and the order this message was sent
        public struct SignatureData
        {
            public byte[] m_bHashOfMessageData;

            public byte[] m_bHashOfPreviouseData;

            public int m_iPeerChainIndex;
        }

        //proof of what the source peer had seen at the time of message creation
        //the source peer could have seen additional messages but for compression reasons
        //this only includes the hash of the message heads for all the messages at the last time the 
        //peer has recieved messages from all active users
        public struct MessageAcknowledgement
        {
            // the last global message index that all messages were recieved from all users 
            public int m_iLastAllRecievedGlobalIndex;

            // the hash of all the messages at last recieved message time
            public byte[] m_bAllMessageHash;

            //the message chain head that this client is building off
            public int m_iGlobalMessageChainIndex;

            //the hash of the message chain head 
            public byte[] m_bMessageChainHash;
        }

        // the time that this message was sent
        // this is used to put messages in order so they can be verified 
        public long m_lTimeOfMessage;

        //signed signature data that proves who sent the message,
        //the message contents as well as the order the message was sent 
        //relative to other messages 
        public byte[] m_bSignature;

        //the data containd by the signature 
        public SignatureData m_sigSignatureData;

        //the creator of this message
        public long m_lUserID;

        //calcualtes the hash for the message
        public abstract byte[] CalculateHash();

        //calculate the short hash of the message 
        public abstract long CalculateShortHash();

    }
}