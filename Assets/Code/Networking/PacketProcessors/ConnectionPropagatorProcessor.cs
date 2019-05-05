using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Networking
{
    public class ConnectionPropagatorProcessor : NetworkPacketProcessor
    {

        public struct ConnectionRequestProgress
        {
            public long m_lConnectionID;
            public DateTime m_dtmTimeOfMessageSend;
        }

        public override int Priority
        {
            get
            {
                return 7;
            }
        }

        private List<long> m_lMissingConnectionIDs = new List<long>();

        public ConnectionPropagatorProcessor(NetworkLayoutProcessor nlpNetworkLayoutProcessor)
        {
            nlpNetworkLayoutProcessor.m_evtPeerConnectionLayoutChange += OnPeerNetworkLayoutChange;
        }

        public void OnPeerNetworkLayoutChange(ConnectionNetworkLayoutProcessor clpNetworkLayoutProcessor)
        {
            m_lMissingConnectionIDs.Clear();

            List<NetworkLayoutProcessor.NetworkLayout.Connection> conConnections = clpNetworkLayoutProcessor.m_nlaNetworkLayout.m_conConnectionDetails;

            //loop through all the peers connections and see if any are missign from current connections
            for (int i = 0; i < conConnections.Count; i++)
            {
                //check if connection exists 
                if()
            }

        }
    }
}
