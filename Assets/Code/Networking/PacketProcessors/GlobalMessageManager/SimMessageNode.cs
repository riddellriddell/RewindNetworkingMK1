using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Networking
{
    public interface ISimMessageNode
    {
        IPeerMessageNode PeerMessageNode { get;}

        //who has acknowledged this node
        HashSet<long> Acknowledgements { get; }

        //has this node been acknowledged by all peers
        bool IsVerified { get; }

        //has this packet passed validation
        bool IsValid { get; }

        //the next node in the list
        ISimMessageNode NextNode { get; }

        //the previouse node in the list
        ISimMessageNode PreviouseNode { get; }

        #region LinkList

        void AttachTo(ISimMessageNode smnNode);

        void AttachNextNode(ISimMessageNode smnNextNode);

        void AttachPreviouseNode(ISimMessageNode smnPreviouseNode);

        void DetachNode();

        #endregion

        //based on the packets before this one is this packet valid? should it be added to the input stack
        bool Validate(IPeerMessageNode pmnPreviousNode, long lSourcePeer, DateTime dtmNetworkTimeRecieved);

    }

    //node type to indicate the end of a channel node 
    public interface ITerminationNode : ISimMessageNode
    {
        //the reason this user connection was terminated 
        TerminationReason Reason { get; }
    }

    public enum TerminationReason
    {
        EmptySlot,
        Hacking,
        TimeOut,
        VoteKick
    }

    public enum InputState
    {
        NotInUse,
        InUse,
        Blocked
    }

}
