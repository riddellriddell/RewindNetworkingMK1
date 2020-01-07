using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Networking
{
    public class GlobalMessageSimManager
    {
        //wrapper class to hold extra information about input

        //ordered list of all the recieved messages 
        public GlobalMessageBuffer MessageBuffer { get; } = new GlobalMessageBuffer();

        //ordered list of all the message chains
        //public SortedSet<ChainLink> m_chlMessageChains;

        //get the new confirmed inputs since last time this function was called 
        //and delete them from the local buffer

        //get list of all the valid unconfirmed inputs upto target time

        //get confirmed state at time x

        //add unconfimed input
        public void AddMessage(GlobalMessageBase gmbMessage, long lSource)
        {           
            MessageNode mndNode = new MessageNode(gmbMessage, lSource);

            MessageBuffer.AddMessageToBuffer(mndNode);
        }

        //compute hash from last confirmed chain head to recieve all head
        public void ComputeHashesForMessageBuffer()
        {

        }

        //add chain link

        //build chain link

    }

    //temp class
    public class MessageNode : IPeerMessageNode
    {
        public GlobalMessageBase GlobalMessage { get; protected set; }

        public long SourcePeerID { get; protected set; }

        public DateTime MessageCreationTime { get; protected set; }

        public int TypeSorting { get; protected set; }

        public int PeerMessageIndex { get; protected set; }

        public int GlobalMessageIndex { get; set; }

        public long NodeHash { get; protected set; }

        public MessageNode(GlobalMessageBase gmbSourceMessage, long lSourcePeer)
        {
            SourcePeerID = lSourcePeer;
            GlobalMessage = gmbSourceMessage;
            MessageCreationTime = new DateTime(gmbSourceMessage.m_lTimeOfMessage);
            PeerMessageIndex = gmbSourceMessage.m_sigSignatureData.m_iPeerChainIndex;

            //TODO:: get propper message node hash
            NodeHash = 0;

            //TODO:: should be changed when message types are implementd
            TypeSorting = 0;
        }

        public object Clone()
        {
            throw new NotImplementedException();
        }

        public int CompareTo(IPeerMessageNode pmnCompareTo)
        {
            if (pmnCompareTo == null)
            {
                return 1;
            }

            //sort items from oldest to youngest
            if (MessageCreationTime < pmnCompareTo.MessageCreationTime)
            {
                return -1;
            }

            if (MessageCreationTime > pmnCompareTo.MessageCreationTime)
            {
                return 1;
            }

            //sort items for order
            if (PeerMessageIndex < pmnCompareTo.PeerMessageIndex)
            {
                return -1;
            }

            if (PeerMessageIndex > pmnCompareTo.PeerMessageIndex)
            {
                return 1;
            }

            //sort items by type order
            if (TypeSorting < pmnCompareTo.TypeSorting)
            {
                return -1;
            }

            if (TypeSorting > pmnCompareTo.TypeSorting)
            {
                return 1;
            }

            //sort by user 
            if (SourcePeerID < pmnCompareTo.SourcePeerID)
            {
                return -1;
            }

            if (SourcePeerID > pmnCompareTo.SourcePeerID)
            {
                return 1;
            }

            //if all else is equal sort by hash
            if (NodeHash < pmnCompareTo.NodeHash)
            {
                return -1;
            }
            else if (NodeHash > pmnCompareTo.NodeHash)
            {
                return 1;
            }
            else
            {
                //must be same message 
                return 0;
            }
        }
    }
}
