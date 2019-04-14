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
    public abstract class NetworkPacketProcessor
    {
        //defines the order that packet processors process a packet if it is processed by multiple packet processors 
        public virtual int Priority { get; }

        public virtual void Update()
        {

        }

        //this gets called when a new connection is added
        public virtual void OnAddToNetwork(NetworkConnection ncnNetwork)
        {

        }
        
        public virtual void OnNewConnection(Connection conConnection)
        {

        }               

        public virtual DataPacket ProcessReceivedPacket(  DataPacket pktInputPacket)
        {
            return pktInputPacket;
        }

        public virtual DataPacket ProcessPacketForSending(  DataPacket pktOutputPacket)
        {
            return pktOutputPacket;
        }

    }
}