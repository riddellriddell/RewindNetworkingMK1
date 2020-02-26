using Networking;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace GameManagers
{
    [Serializable]
    public struct ActiveGameManagerSceneTesterConnection
    {
        [SerializeField]
        public long m_lConnectionID;

        [SerializeField]
        public Connection.ConnectionStatus m_cnsConnectionState;

        [SerializeField]
        public PeerTransmitterState m_ptsTransmitterState;

        [SerializeField]
        public bool m_bMakingOffer;

        [SerializeField]
        public int m_iNumberOfIceCandidatesRecieved;

        [SerializeField]
        public List<long> m_lConnectedPeers;
    }

    [Serializable]
    public struct ActiveGameManagerSceneTesterConnectionPeer
    {
        [SerializeField]
        public long m_lPeerId;
    }

    public class ActiveGameManagerSceneTester : MonoBehaviour
    {
        public string m_strUniqueDeviceID;

        public WebInterface m_wbiWebInterface;

        public ActiveGameManager m_agmActiveGameManager;

        public bool m_bAlive = true;

        [SerializeField]
        public long m_lPeerID;

        [SerializeField]
        public List<ActiveGameManagerSceneTesterConnection> m_stcNetworkDebugData;

        // Start is called before the first frame update
        void Start()
        {
            
        }

        // Update is called once per frame
        void Update()
        {

        }

        private void OnEnable()
        {
            OnConnect();
        }

        private void OnDisable()
        {
            Disconnect();
        }

        public void NewPeerID()
        {
            m_strUniqueDeviceID = $"User:{Random.Range(int.MinValue, int.MaxValue)}";
        }

        public void OnConnect()
        {
            StartCoroutine(Test());
        }

        public void Disconnect()
        {
            m_bAlive = false;

            m_wbiWebInterface = null;

            m_agmActiveGameManager = null;
        }

        protected IEnumerator Test()
        {
            m_bAlive = true;

            yield return null;

            Debug.Log("Starting active game manager Test");

            m_wbiWebInterface = new WebInterface();

            if(m_strUniqueDeviceID == string.Empty)
            {
                NewPeerID();
            }

            m_wbiWebInterface.GetPlayerID(m_strUniqueDeviceID);
                        

            while (m_bAlive && m_wbiWebInterface.PlayerIDCommunicationStatus.m_cmsStatus != WebInterface.WebAPICommunicationTracker.CommunctionStatus.Succedded )
            {              
                m_wbiWebInterface.UpdateCommunication();

                yield return null;
            }

            if (m_bAlive == false)
            {
                yield break;
            }

            m_agmActiveGameManager = new ActiveGameManager(m_wbiWebInterface);

            while(m_bAlive)
            {              
                m_wbiWebInterface.UpdateCommunication();

                m_agmActiveGameManager.UpdateGame(Time.deltaTime);

                UpdateNetworkDebug();

                yield return null;
            }

            if (m_bAlive == false)
            {
                yield break;
            }

        }

        protected void UpdateNetworkDebug()
        {
            m_lPeerID = m_agmActiveGameManager.m_winWebInterface.PlayerID;

            m_stcNetworkDebugData = new List<ActiveGameManagerSceneTesterConnection>();

            NetworkLayoutProcessor nlpNetworkLayout = m_agmActiveGameManager.m_ncnNetworkConnection.GetPacketProcessor<NetworkLayoutProcessor>();

            foreach (KeyValuePair<long,ConnectionNetworkLayoutProcessor> cnlConnectionLayout in nlpNetworkLayout.ChildConnectionProcessors)
            {
                ActiveGameManagerSceneTesterConnection stcConnection = new ActiveGameManagerSceneTesterConnection();

                stcConnection.m_lConnectionID = cnlConnectionLayout.Key;

                stcConnection.m_cnsConnectionState = cnlConnectionLayout.Value.ParentConnection.Status;

                stcConnection.m_ptsTransmitterState = cnlConnectionLayout.Value.ParentConnection.m_ptrTransmitter.State;

                stcConnection.m_bMakingOffer = (cnlConnectionLayout.Value.ParentConnection.m_ptrTransmitter as FakeWebRTCTransmitter).m_bMakingOffer;

                stcConnection.m_iNumberOfIceCandidatesRecieved = (cnlConnectionLayout.Value.ParentConnection.m_ptrTransmitter as FakeWebRTCTransmitter).m_iIceCandidatesRecieved;

                stcConnection.m_lConnectedPeers = new List<long>();


                foreach (NetworkLayout.ConnectionState cnsConnectionState in cnlConnectionLayout.Value.m_nlaNetworkLayout.m_conConnectionDetails)
                {
                    stcConnection.m_lConnectedPeers.Add(cnsConnectionState.m_lConnectionID);
                }

                m_stcNetworkDebugData.Add(stcConnection);
            }
        }

    }
}