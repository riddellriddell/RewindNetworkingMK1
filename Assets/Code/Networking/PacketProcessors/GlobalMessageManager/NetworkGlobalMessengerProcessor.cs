using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;

namespace Networking
{
    public class NetworkGlobalMessengerProcessor : ManagedNetworkPacketProcessor<ConnectionGlobalMessengerProcessor>
    {
        public class GlobalMessagePeer
        {

        }

        public class GlobalMessageWrapper
        {
            //the user this message belongs to
            public long m_lMessageSource;

            //list of all the users that have seen this message 
            public HashSet<long> m_lSeenBy;

            public GlobalMessageBase m_gmbMessage;
        }

        public class PeerMessageChain
        {
            //chain of all the messages yet to be fully validated
            public RandomAccessQueue<GlobalMessageWrapper> m_gmwNonValidatedMessageChain;

            //the most recent message that has been fully validated and is the base for the message chain
            protected GlobalMessageWrapper m_gmwChainBase;

            //messages that have been fully validated and are awaiting processing 
            public Queue<GlobalMessageWrapper> m_gmwValidatedMessages;

            //gets the send time (according to the sender) of the last message recieved
            public DateTime GetMessageHeadTime()
            {
                DateTime dtmSendTime = DateTime.Now;

                return dtmSendTime;
            }

            //returns the time of the last message that has been seen by all peers
            public DateTime GetTimeOfFullValidation()
            {
                return new DateTime(m_gmwChainBase.m_gmbMessage.m_lTimeOfMessage);
            }

            public bool TryAddNewMessage(GlobalMessageBase gmbMessage)
            {
                //check message number

                //check message date

                //check message hash

                //check message sig

                //add to wrapper and add to end of chain

                return true;
            }
        }

        public override int Priority
        {
            get
            {
                return 5;
            }
        }

        public Dictionary<long, PeerMessageChain> m_pmcPeerMessageChain;

        protected TimeNetworkProcessor m_tnpNetworkTime;

        
    }

    public class ConnectionGlobalMessengerProcessor : ManagedConnectionPacketProcessor<NetworkGlobalMessengerProcessor>
    {
        public override int Priority
        {
            get
            {
                return 5;
            }
        }
    }
}
