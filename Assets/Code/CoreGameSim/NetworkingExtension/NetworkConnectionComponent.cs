using Networking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Sim
{
    public class NetworkConnectionComponent : MonoBehaviour
    {
        public InternetConnectionSimulator m_icwConnectionSimulation;

        public NetworkConnection m_ncnNetworkConnection = null;

        public void Init()
        {
            if(m_ncnNetworkConnection != null)
            {
                return;
            }

            m_ncnNetworkConnection = new NetworkConnection(new GamePacketFactory(), m_icwConnectionSimulation);

        }
        
        private void Update()
        {
            m_ncnNetworkConnection.UpdateConnectionsAndProcessors();
        }
    }
}
