using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Networking
{
    //this class is a framework for setting up a network of N connected peers for testing packet processors
    public class BaseNetworkTestSetup : MonoBehaviour
    {
        public int m_iNumberOfPeersToCreate;

        public List<NetworkConnection> m_ncnPeerNetworks;

        public bool m_bAllPeersConnected = false;

        // Start is called before the first frame update
        void Start()
        {
            Debug.Log("Starting network setup");

            CreateNetworkPeers();
        }

        // Update is called once per frame
        void Update()
        {
            for(int i = 0; i < m_ncnPeerNetworks.Count; i++)
            {
                m_ncnPeerNetworks[i].UpdateConnectionsAndProcessors();
            }

            UpdateConnectionMessages();

            if(m_bAllPeersConnected)
            {
                OnConnectedNetworkUpdate();
            }
        }

        public void CreateNetworkPeers()
        {
            DateTime dtmProcessStart = DateTime.UtcNow;

            m_ncnPeerNetworks = new List<NetworkConnection>();

           

            //creeate all peers
            for (int i = 0; i < m_iNumberOfPeersToCreate; i++)
            {
                long lPeerID = GenerateUserID(i);

                IPeerTransmitterFactory m_ptfTransmitterFactory = CreateTransmitterFactory();

                NetworkConnection ncnNewNetworkConnection = new NetworkConnection(lPeerID,m_ptfTransmitterFactory);

                SetupPeerPacketProcessors(ncnNewNetworkConnection);

                m_ncnPeerNetworks.Add(ncnNewNetworkConnection);
            }

            OnPreConnectionCreate();

            //start connecting peers 
            for (int i = 0; i < m_ncnPeerNetworks.Count; i++)
            {
                for(int j = i + 1; j < m_ncnPeerNetworks.Count; j++)
                {
                    NetworkConnection ncnConnectingFrom = m_ncnPeerNetworks[i];
                    NetworkConnection ncnConnectingTo = m_ncnPeerNetworks[j];

                    Connection conFrom = ncnConnectingFrom.CreateNewConnection(dtmProcessStart, ncnConnectingTo.m_lPeerID);
                    Connection conTo = ncnConnectingTo.CreateNewConnection(dtmProcessStart, ncnConnectingFrom.m_lPeerID);

                    conFrom.StartConnectionNegotiation();

                }
            }
        }

        //transfer all connection negotiation messages 
        public void UpdateConnectionMessages()
        {
            bool bAllPeersConnected = true;

            for(int i = 0; i < m_ncnPeerNetworks.Count; i++)
            {
                NetworkConnection ncnFromPeer = m_ncnPeerNetworks[i];


                foreach (Connection conFromConnecion in ncnFromPeer.ConnectionList.Values)
                {
                    NetworkConnection ncnToPeer = null;

                    for(int j = 0; j < m_ncnPeerNetworks.Count; j++)
                    {
                        if(m_ncnPeerNetworks[j].m_lPeerID == conFromConnecion.m_lUserUniqueID)
                        {
                            ncnToPeer = m_ncnPeerNetworks[j];

                            break;
                        }
                    }

                    Connection conToConnection = ncnToPeer.ConnectionList[ncnFromPeer.m_lPeerID];

                    while (conFromConnecion.TransmittionNegotiationMessages.Count > 0)
                    {
                        conToConnection.ProcessNetworkNegotiationMessage(conFromConnecion.TransmittionNegotiationMessages.Dequeue());
                    }


                    if (m_bAllPeersConnected == false && bAllPeersConnected == true)
                    {
                        if (conFromConnecion.Status != Connection.ConnectionStatus.Connected)
                        {
                            bAllPeersConnected = false;
                        }
                    }
                }
            }

            if(m_bAllPeersConnected == false && bAllPeersConnected == true)
            {
                m_bAllPeersConnected = bAllPeersConnected;

                OnAllPeersConnected();
            }
        }
         

        public virtual long GenerateUserID(int iIndex)
        {
            Debug.Log($"Creating Peer wih id:{iIndex}");
            return iIndex;
        }

        public virtual IPeerTransmitterFactory CreateTransmitterFactory()
        {
            Debug.Log("Creating TransmitterFactory");
            return new FakeWebRTCFactory();
        }

        public virtual void SetupPeerPacketProcessors(NetworkConnection ncnPeerNetwork)
        {
            Debug.Log("Setting up peer network processors");
            ncnPeerNetwork.AddPacketProcessor(new TimeNetworkProcessor());
            ncnPeerNetwork.AddPacketProcessor(new NetworkLargePacketTransferManager());
        }

        public virtual void OnPreConnectionCreate()
        {
            Debug.Log("Pre connection create");

        }

        public virtual void OnAllPeersConnected()
        {
            Debug.Log("All connections established");
        }

        public virtual void OnConnectedNetworkUpdate()
        {

        }
    }
}
