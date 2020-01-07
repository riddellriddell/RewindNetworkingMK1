using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Networking
{
    public interface IPeerMessageNode : ICloneable
    {
        //the global message this is derived from
        GlobalMessageBase GlobalMessage { get; }

        //the source peer
        long SourcePeerID { get; }

        //the time this message was created or in the case of a conflict the time of the 
        //last valid message
        DateTime MessageCreationTime { get; }

        //for sorting reasons is this packet a confict packet and what level of conflict is it for
        int TypeSorting { get; }

        //the index in the peer message chain
        int PeerMessageIndex { get; }

        //the index of this peer in the entire chain
        int GlobalMessageIndex { get; set; }

        // the number of times a new node has been added behind the all recieved message head
        // this is used to keep track of which inputs need to be echoed to which peers 
        // when there is a hash conflict 
        // this value is not synchronised with other peers
        int GlobalBranchNumber { get; set; }

        //rolling hash chain for all inputs
        //this is used to detect conflicts/differences in inputs between users 
        //may neet to change format
        //GlobalMessageNodeHash GlobalHash { get; set; }

        //the hash of this node 
        long NodeHash { get; }

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
}
