﻿using FixedPointy;
using Networking;
using Sim;
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
    public struct ActiveGameManagerSceneTesterGlobalMessageChannel
    {
        [SerializeField]
        public long m_lActivePeerID;

        [SerializeField]
        public GlobalMessageChannelState.State m_staState;
        
        [SerializeField]
        public int m_iVotes;
    }

    [Serializable]
    public struct ActiveGameManagerSceneTesterConnectionPeer
    {
        [SerializeField]
        public long m_lPeerId;
    }

    [Serializable]
    public struct ActiveGameManagerSceneTesterSimState
    {
        [SerializeField]
        public bool m_bDrawDebugData;

        [SerializeField]
        public long[] m_lPeersAssignedToSlot;

        public float[] m_fShipHealth;

        public float[] m_fShipSpawnCountdown;

        public float[] m_fShipPosX;
        public float[] m_fShipPosY;
        public float[] m_fShipVelocityX;
        public float[] m_fShipVelocityY;
        
    }

    public class ActiveGameManagerSceneTester : MonoBehaviour
    {
        public bool m_bTestLocally = true;

        public bool m_bUseWebRTCTransmitter = false;

        public string m_strUniqueDeviceID;

        public WebInterface m_wbiWebInterface;

        public IPeerTransmitterFactory m_ptfTransmitterFactory;

        public ActiveGameManager m_agmActiveGameManager;

        public bool m_bAlive = true;

        public ConstData m_cdaConstSimData;

        [SerializeField]
        public SimProcessorSettingsInterface m_sdiSettingsDataInderface;

        [SerializeField]
        public long m_lPeerID;
        
        [SerializeField]
        public NetworkGlobalMessengerProcessor.State m_staGlobalMessagingState;

        [SerializeField]
        public int m_iTotalChainLinks;
        
        [SerializeField]
        public int m_iActiveChainLinks;

        [SerializeField]
        public int m_iBaseChainLinkIndex;

        [SerializeField]
        public int m_iGlobalMessagingChannelIndex;      

        [SerializeField]
        public List<ActiveGameManagerSceneTesterGlobalMessageChannel> m_gmsGlobalMessagingState;

        [SerializeField]
        public List<ActiveGameManagerSceneTesterConnection> m_stcNetworkDebugData;

        [SerializeField]
        public ActiveGameManagerSceneTesterSimState m_sstSimState;


        // Start is called before the first frame update
        void Start()
        {
            if(m_bUseWebRTCTransmitter)
            {
                m_ptfTransmitterFactory = new WebRTCFactory(this);
            }
            else
            {
                m_ptfTransmitterFactory = new FakeWebRTCFactory();
            }
        }

        // Update is called once per frame
        void Update()
        {

        }

        private void OnDestroy()
        {
            //clean up code
            m_agmActiveGameManager?.OnCleanup();

            m_agmActiveGameManager = null;
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

            m_agmActiveGameManager?.OnCleanup();
            
            m_agmActiveGameManager = null;
        }

        protected IEnumerator Test()
        {
            m_bAlive = true;

            yield return null;

            Debug.Log("Starting active game manager Test");

            m_wbiWebInterface = new WebInterface(this);

            m_wbiWebInterface.TestLocally = m_bTestLocally;

            if (m_strUniqueDeviceID == string.Empty)
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

            m_agmActiveGameManager?.OnCleanup();

            m_agmActiveGameManager = new ActiveGameManager(m_sdiSettingsDataInderface.ConvertToSettingsData(),new ConstData(), m_wbiWebInterface, m_ptfTransmitterFactory);

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
            m_lPeerID = m_agmActiveGameManager.m_winWebInterface.UserID;

            NetworkGlobalMessengerProcessor gmpGlobalMessagingProcessor = m_agmActiveGameManager.m_ncnNetworkConnection.GetPacketProcessor<NetworkGlobalMessengerProcessor>();
            TimeNetworkProcessor tnpTimeProcessor = m_agmActiveGameManager.m_ncnNetworkConnection.GetPacketProcessor<TimeNetworkProcessor>();
            TestingSimManager<FrameData,ConstData,SimProcessorSettings> tsmTestSimManager = m_agmActiveGameManager.m_tsmSimManager;

            m_staGlobalMessagingState = gmpGlobalMessagingProcessor.m_staState;

            if (m_staGlobalMessagingState == NetworkGlobalMessengerProcessor.State.Connected ||
                m_staGlobalMessagingState == NetworkGlobalMessengerProcessor.State.Active)
            {
                //the number of links in chain manager
                m_iTotalChainLinks = gmpGlobalMessagingProcessor.m_chmChainManager.ChainLinks.Count;

                //the number of links in the active chain
                m_iActiveChainLinks = (int)(gmpGlobalMessagingProcessor.m_chmChainManager.m_chlBestChainHead.m_iChainLength - gmpGlobalMessagingProcessor.m_chmChainManager.m_chlChainBase.m_iChainLength);

                //get the index of the base link
                m_iBaseChainLinkIndex = (int)gmpGlobalMessagingProcessor.m_chmChainManager.m_chlChainBase.m_iLinkIndex;

                if (gmpGlobalMessagingProcessor.m_gmbMessageBuffer.LatestState.TryGetIndexForPeer(m_agmActiveGameManager.m_ncnNetworkConnection.m_lPeerID, out int iIndex))
                {
                    m_iGlobalMessagingChannelIndex = iIndex;
                }
                else
                {
                    m_iGlobalMessagingChannelIndex = -1;
                }
                               
                m_gmsGlobalMessagingState = new List<ActiveGameManagerSceneTesterGlobalMessageChannel>();

                for (int i = 0; i < gmpGlobalMessagingProcessor.m_gmbMessageBuffer.LatestState.m_gmcMessageChannels.Count; i++)
                {
                    GlobalMessageChannelState gcsChannelState = gmpGlobalMessagingProcessor.m_gmbMessageBuffer.LatestState.m_gmcMessageChannels[i];

                    int iVotes = 0;

                    for(int j = 0; j < gmpGlobalMessagingProcessor.m_gmbMessageBuffer.LatestState.m_gmcMessageChannels.Count; j++)
                    {
                        GlobalMessageChannelState gcsVotingChannel = gmpGlobalMessagingProcessor.m_gmbMessageBuffer.LatestState.m_gmcMessageChannels[j];

                        if (gcsVotingChannel.m_staState == GlobalMessageChannelState.State.Assigned ||
                            gcsVotingChannel.m_staState == GlobalMessageChannelState.State.VoteKick)
                        {
                            //if(gcsVotingChannel.m_chvVotes[i].m_lPeerID == gcsChannelState.m_lChannelPeer)
                            //{
                                if(gcsVotingChannel.m_chvVotes[i].IsActive(tnpTimeProcessor.NetworkTime, GlobalMessagingState.s_tspVoteTimeout) == true)
                                {
                                    iVotes++;
                                }
                            //}
                        }

                    }

                    m_gmsGlobalMessagingState.Add(new ActiveGameManagerSceneTesterGlobalMessageChannel()
                    {
                        m_lActivePeerID = gcsChannelState.m_lChannelPeer,
                        m_staState = gcsChannelState.m_staState,
                        m_iVotes = iVotes
                    });
                }
            }

            m_stcNetworkDebugData = new List<ActiveGameManagerSceneTesterConnection>();

            NetworkLayoutProcessor nlpNetworkLayout = m_agmActiveGameManager.m_ncnNetworkConnection.GetPacketProcessor<NetworkLayoutProcessor>();

            foreach (KeyValuePair<long,ConnectionNetworkLayoutProcessor> cnlConnectionLayout in nlpNetworkLayout.ChildConnectionProcessors)
            {
                ActiveGameManagerSceneTesterConnection stcConnection = new ActiveGameManagerSceneTesterConnection();

                stcConnection.m_lConnectionID = cnlConnectionLayout.Key;

                stcConnection.m_cnsConnectionState = cnlConnectionLayout.Value.ParentConnection.Status;

                stcConnection.m_ptsTransmitterState = cnlConnectionLayout.Value.ParentConnection.m_ptrTransmitter.State;

                if (m_bUseWebRTCTransmitter == false)
                {

                    stcConnection.m_bMakingOffer = (cnlConnectionLayout.Value.ParentConnection.m_ptrTransmitter as FakeWebRTCTransmitter).m_bMakingOffer;

                    stcConnection.m_iNumberOfIceCandidatesRecieved = (cnlConnectionLayout.Value.ParentConnection.m_ptrTransmitter as FakeWebRTCTransmitter).m_iIceCandidatesRecieved;

                }
                else
                {
                    stcConnection.m_bMakingOffer = false;
                    stcConnection.m_iNumberOfIceCandidatesRecieved = 0;
                }
                stcConnection.m_lConnectedPeers = new List<long>();


                foreach (NetworkLayout.ConnectionState cnsConnectionState in cnlConnectionLayout.Value.m_nlaNetworkLayout.m_conConnectionDetails)
                {
                    stcConnection.m_lConnectedPeers.Add(cnsConnectionState.m_lConnectionID);
                }

                m_stcNetworkDebugData.Add(stcConnection);
            }

            if(tsmTestSimManager.m_fdaSimStateBuffer != null && tsmTestSimManager.m_fdaSimStateBuffer.Count > 0)
            {
                FrameData fdaFrameData = tsmTestSimManager.m_fdaSimStateBuffer.PeakEnqueue();
                
                m_sstSimState.m_lPeersAssignedToSlot = fdaFrameData.m_lPeersAssignedToSlot;

                if(fdaFrameData.m_fixShipHealth != null)
                {
                    if(m_sstSimState.m_fShipHealth.Length != fdaFrameData.m_fixShipHealth.Length)
                    {
                        m_sstSimState.m_fShipHealth = new float[fdaFrameData.m_fixShipHealth.Length];
                        m_sstSimState.m_fShipSpawnCountdown = new float[fdaFrameData.m_fixShipHealth.Length];
                        m_sstSimState.m_fShipPosX = new float[fdaFrameData.m_fixShipHealth.Length];
                        m_sstSimState.m_fShipPosY = new float[fdaFrameData.m_fixShipHealth.Length];
                        m_sstSimState.m_fShipVelocityX = new float[fdaFrameData.m_fixShipHealth.Length];
                        m_sstSimState.m_fShipVelocityY = new float[fdaFrameData.m_fixShipHealth.Length];
                    }

                    for(int i = 0; i < fdaFrameData.m_fixShipHealth.Length; i++)
                    {
                        m_sstSimState.m_fShipHealth[i] = (float)fdaFrameData.m_fixShipHealth[i];
                        m_sstSimState.m_fShipSpawnCountdown[i] = (float)fdaFrameData.m_fixTimeUntilRespawn[i];
                        m_sstSimState.m_fShipPosX[i] = (float)fdaFrameData.m_fixShipPosX[i];
                        m_sstSimState.m_fShipPosY[i] = (float)fdaFrameData.m_fixShipPosY[i];
                        m_sstSimState.m_fShipVelocityX[i] = (float)fdaFrameData.m_fixShipVelocityX[i];
                        m_sstSimState.m_fShipVelocityY[i] = (float)fdaFrameData.m_fixShipVelocityY[i];
                    }
                }

            }
        }

        protected void OnDrawGizmosSelected()
        {
           if(m_sstSimState.m_bDrawDebugData && m_sstSimState.m_fShipHealth != null)
            {
                for(int i = 0; i < m_sstSimState.m_fShipHealth.Length; i++)
                {
                    // Draw a random colour sphere for each player
                    Gizmos.color = new Color((((m_sstSimState.m_lPeersAssignedToSlot[i] % 256) + 256) % 256) / 256.0f,
                        ((((m_sstSimState.m_lPeersAssignedToSlot[i] >> 2)  % 256) + 256) % 256) / 256.0f,
                        ((((m_sstSimState.m_lPeersAssignedToSlot[i] >> 4) % 256) + 256) % 256) / 256.0f, 1);


                    Gizmos.DrawSphere(new Vector3(m_sstSimState.m_fShipPosX[i],0, m_sstSimState.m_fShipPosY[i]), 1);
                    Gizmos.DrawLine(new Vector3(m_sstSimState.m_fShipPosX[i], 0, m_sstSimState.m_fShipPosY[i]), 
                        new Vector3(m_sstSimState.m_fShipPosX[i], 0, m_sstSimState.m_fShipPosY[i]) +
                        (new Vector3(m_sstSimState.m_fShipVelocityX[i], 0, m_sstSimState.m_fShipVelocityY[i]) * 2));
                }
            }
        }
    }
}