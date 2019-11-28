using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Networking
{
    /// <summary>
    /// this class keeps track of the number of gateways connectint the cluster to the matchmaking
    /// server and if no gateway exists decides if it wants to become a gateway 
    /// </summary>
    public class NetworkGatewayManager : ManagedNetworkPacketProcessor<ConnectionGatewayManager>
    {
        /// <summary>
        /// how many secconds between anouncing you currently have a gateway running
        /// </summary>
        public static TimeSpan GatewayAnounceRate
        {
            get
            {
                return TimeSpan.FromSeconds(5);
            }
        }

        /// <summary>
        /// if another gateway anounce is not recieved in this time a users gateway is considered closed
        /// </summary>
        public static TimeSpan GatewayTimeout
        {
            get
            {
                return TimeSpan.FromSeconds(8);
            }
        }

        /// <summary>
        /// turns off gateway managers attempts to setup new gateways / communicate gateway setup with other peers in cluster
        /// </summary>
        public bool m_bEnabled;

        /// <summary>
        /// based on the current peer network should this peer have a gateway to the matchmaker open
        /// </summary>
        public bool NeedsOpenGateway { get; private set; }

        //defines the order that packet processors process packets
        public override int Priority { get; } = 10;

        public override void Update()
        {
            //check if enabled and is not currently an active gate 
            if(m_bEnabled && NeedsOpenGateway == false)
            {
                //is there an active gate in the cluster
                bool bActiveGate = false;

                //check if there is a client that has an open gateway
                foreach(ConnectionGatewayManager cgmConnection in ChildConnectionProcessors.Values)
                {
                    if(cgmConnection.HasActiveGateway)
                    {
                        bActiveGate = true;
                        break;
                    }
                }

                //if there is no active gate
                if (bActiveGate == false)
                {
                    bool bShouldOpenGate = true;

                    //check if this user is highest user ID and should open gate 
                    foreach (long userID in ChildConnectionProcessors.Keys)
                    {
                        if(userID > ParentNetworkConnection.m_lPlayerUniqueID)
                        {
                            bShouldOpenGate = false;

                            break;
                        }
                    }

                    if(bShouldOpenGate)
                    {
                        //open gate 
                        NeedsOpenGateway = true;
                    }
                }
            }

            base.Update();
        }

        //process gateway packets
        public override DataPacket ProcessReceivedPacket(DataPacket pktInputPacket)
        {
            return base.ProcessReceivedPacket(pktInputPacket);
        }
    }

    public class ConnectionGatewayManager : ManagedConnectionPacketProcessor<NetworkGatewayManager>
    {
        public bool HasActiveGateway
        {
            get
            {
                if(DateTime.UtcNow - TimeOfLastGatewayNotification < NetworkGatewayManager.GatewayTimeout )
                {
                    return true;
                }

                return false;
            }
        }

        public DateTime TimeOfLastGatewayNotification { get; private set; } = DateTime.MinValue;

        public DateTime TimeOfFistGatewatActivation { get; private set; } = DateTime.MinValue;

        public override int Priority { get; } = 10;

        protected DateTime m_dtmTimeOfLastOpenGateNotification = DateTime.MinValue;

        public override void Update(Connection conConnection)
        {
            //check if user has active gateway
            if (m_tParentPacketProcessor.m_bEnabled && m_tParentPacketProcessor.NeedsOpenGateway)
            {
                //check if gateway has timed out
                if (DateTime.UtcNow - m_dtmTimeOfLastOpenGateNotification > NetworkGatewayManager.GatewayAnounceRate)
                {
                    //create packet to send
                    GatewayActiveAnouncePacket gapAnouncePacket = conConnection.m_cifPacketFactory.CreateType<GatewayActiveAnouncePacket>(GatewayActiveAnouncePacket.TypeID);

                    //make new announcement 
                    conConnection.QueuePacketToSend(gapAnouncePacket);

                    //update the last time a gateway announce was sent 
                    m_dtmTimeOfLastOpenGateNotification = DateTime.UtcNow;
                }
            }

            base.Update(conConnection);
        }

        public override DataPacket ProcessReceivedPacket(Connection conConnection, DataPacket pktInputPacket)
        {
            //check if packet is gateway announcement
            if(pktInputPacket.GetTypeID == GatewayActiveAnouncePacket.TypeID)
            {
                //check if time of gateway activation

                //update the last time seeing a notification for a gateway
                TimeOfLastGatewayNotification = DateTime.UtcNow;

                return null;
            }

            return base.ProcessReceivedPacket(conConnection, pktInputPacket);
        }
    }
}
