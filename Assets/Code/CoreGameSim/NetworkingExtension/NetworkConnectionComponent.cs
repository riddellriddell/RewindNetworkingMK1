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

            m_ncnNetworkConnection = new NetworkConnection(UnityEngine.Random.Range(int.MinValue,int.MaxValue), new FakeWebRTCFactory());

            m_ncnNetworkConnection.AddPacketProcessor(new TickStampedDataNetworkProcessor());
            m_ncnNetworkConnection.AddPacketProcessor(new TimeNetworkProcessor());

        }
        
        private void Update()
        {
            m_ncnNetworkConnection.UpdateConnectionsAndProcessors();
        }

        public void OnDestroy()
        {
            m_ncnNetworkConnection.OnCleanup();
        }
    }
}
