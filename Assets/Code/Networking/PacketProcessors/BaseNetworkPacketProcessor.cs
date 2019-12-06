using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/// <summary>
/// this virtual class handles processing a packet for sending and receiving 
/// </summary>
namespace Networking
{
    public abstract class BaseNetworkPacketProcessor
    {
        //defines the order that packet processors process a packet if it is processed by multiple packet processors 
        public abstract int Priority { get; }

        public virtual void Update()
        {

        }

        //this gets called when added to the network
        public virtual void OnAddToNetwork(NetworkConnection ncnNetwork)
        {

        }
      
        //this gets called when a new connection is added
        public virtual void OnNewConnection(Connection conConnection)
        {

        } 
        
        //this gets called when a conneciton is dropped / lost
        public virtual void OnConnectionDisconnect(Connection conConnection)
        {

        }

        public virtual DataPacket ProcessReceivedPacket(long lFromUserID, DataPacket pktInputPacket)
        {
            return pktInputPacket;
        }

        public virtual DataPacket ProcessPacketForSending(long lToUserID, DataPacket pktOutputPacket)
        {
            return pktOutputPacket;
        }

    }
}