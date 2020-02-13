using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Networking
{
    //this interface allows the simulation code to decide on which peer to connect and allows the simulation code to request to kick someone 
    public interface IGlobalMessageKickJoinSimVerificationInterface
    {
        //passes in a list of the peers the global message system wants to connect to 
        // and allows the simulation to veto connecting to a peer
        void PeerConnectionCandidates(SortedList<long,long> lConenctCandidates);

        //returns a list of peers the sim wants to kick 
        List<long> GetKickRequests();
    }

    // a simple class that does not 
    public class GlobalMessageKickJoinSimVerificationTestingClass : IGlobalMessageKickJoinSimVerificationInterface
    {
        public List<long> GetKickRequests()
        {
            return new List<long>();
        }

        public void PeerConnectionCandidates(SortedList<long, long> lConenctCandidates)
        {
        }
    }
}
