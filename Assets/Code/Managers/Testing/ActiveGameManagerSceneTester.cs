using GameStateView;
using GameViewUI;
using Networking;
using Sim;
using SimDataInterpolation;
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
        public float[] m_fShipRotation;
        public float[] m_fShipWeaponChargeUp;
        public float[] m_fShipWeaponCoolDown;

        public float[] m_fLazerPosX;
        public float[] m_fLazerPosY;
        public float[] m_fLazerLife;
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
        public IGameStateView m_gsvGameStateView;

        [SerializeField]
        public UIStateManager m_usmGameUIStateManager;

        [SerializeField]
        public GameStateViewCamera m_gvcGameViewCamera;

        [SerializeField]
        public SimProcessorSettingsInterface m_sdiSettingsDataInderface;

        [SerializeField]
        public MapGenSettings m_mgsMapSettingsDataInderface;

        [SerializeField]
        public InterpolationErrorCorrectionSettingsGen m_ecsErrorCorrectionSettings;

        [SerializeField]
        public NetworkConnectionSettings m_ncsNetworkConnectionSettings;

        [SerializeField]
        public long m_lPeerID;

        [SerializeField]
        public NetworkGlobalMessengerProcessor.State m_staGlobalMessagingState;

        [SerializeField]
        public int m_iChainStartCandidates;

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

        public IInputApplyer m_iapInputApplyer;

        // Start is called before the first frame update
        void Start()
        {
            if (m_bUseWebRTCTransmitter)
            {
                m_ptfTransmitterFactory = new WebRTCFactory(this);
            }
            else
            {
                m_ptfTransmitterFactory = new FakeWebRTCFactory();
            }

            if(m_gsvGameStateView == null)
            {
                m_gsvGameStateView = GetComponent<IGameStateView>();
            }

            if(m_iapInputApplyer == null)
            {
                m_iapInputApplyer = GetComponent<IInputApplyer>();
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

            Debug.Log($"Get player id for divice_id {m_strUniqueDeviceID}");

            m_wbiWebInterface.GetPlayerID(m_strUniqueDeviceID);


            while (m_bAlive && m_wbiWebInterface.PlayerIDCommunicationStatus.m_cmsStatus != WebInterface.WebAPICommunicationTracker.CommunctionStatus.Succedded)
            {
                m_wbiWebInterface.UpdateCommunication();

                yield return null;
            }

            if (m_bAlive == false)
            {
                yield break;
            }

            Debug.Log($"player id:{m_wbiWebInterface.UserID}, Player Key:{m_wbiWebInterface.UserKey} fetched for divice_id {m_strUniqueDeviceID}");

            m_agmActiveGameManager?.OnCleanup();

            m_cdaConstSimData = new ConstData(m_mgsMapSettingsDataInderface);

            //setup game
            m_agmActiveGameManager = new ActiveGameManager(
                m_sdiSettingsDataInderface.ConvertToSettingsData(), 
                m_ecsErrorCorrectionSettings,
                m_ncsNetworkConnectionSettings,
                m_cdaConstSimData, 
                m_wbiWebInterface, 
                m_ptfTransmitterFactory,
                m_gsvGameStateView,
                m_usmGameUIStateManager,
                m_gvcGameViewCamera);

            while (m_bAlive)
            {
                m_wbiWebInterface.UpdateCommunication();

                m_iapInputApplyer?.ApplyInputs(m_agmActiveGameManager.m_lpiLocalPeerInputManager);

                m_agmActiveGameManager.UpdateGame(Time.deltaTime);

#if UNITY_EDITOR
                UpdateNetworkDebug();
#endif

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
            TestingSimManager<FrameData, ConstData, SimProcessorSettings> tsmTestSimManager = m_agmActiveGameManager.m_tsmSimManager;

            m_staGlobalMessagingState = gmpGlobalMessagingProcessor.m_staState;

            if(m_staGlobalMessagingState == NetworkGlobalMessengerProcessor.State.ConnectAsAdditionalPeer)
            {
                //get the nuber of start candidates 
                m_iChainStartCandidates = gmpGlobalMessagingProcessor.m_chmChainManager.StartStateCandidates.Count;

                //the number of links in chain manager
                m_iTotalChainLinks = gmpGlobalMessagingProcessor.m_chmChainManager.ChainLinks.Count;

            }
            else
            {
                m_iChainStartCandidates = 0;
            }

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

                //loop through all the channels
                for (int i = 0; i < gmpGlobalMessagingProcessor.m_gmbMessageBuffer.LatestState.m_gmcMessageChannels.Count; i++)
                {
                    GlobalMessageChannelState gcsChannelState = gmpGlobalMessagingProcessor.m_gmbMessageBuffer.LatestState.m_gmcMessageChannels[i];

                    int iVotes = 0;

                    //for a given chanel check what its votes are on all the other channels
                    for (int j = 0; j < gmpGlobalMessagingProcessor.m_gmbMessageBuffer.LatestState.m_gmcMessageChannels.Count; j++)
                    {
                        GlobalMessageChannelState gcsVotingChannel = gmpGlobalMessagingProcessor.m_gmbMessageBuffer.LatestState.m_gmcMessageChannels[j];

                        if (gcsVotingChannel.m_staState == GlobalMessageChannelState.State.Assigned ||
                            gcsVotingChannel.m_staState == GlobalMessageChannelState.State.VoteKick)
                        {
                            //if the vote is for the channel
                            //TODO:: not sure this if is needed, this will limit votes from other chaneels to only the votes that effect this channel?
                            if(gcsVotingChannel.m_chvVotes[i].m_lPeerID == gcsChannelState.m_lChannelPeer)
                            {
                                if (gcsVotingChannel.m_chvVotes[i].IsActive(tnpTimeProcessor.NetworkTime, gmpGlobalMessagingProcessor.m_chmChainManager.VoteTimeout) == true)
                                {
                                    //
                                    iVotes++;
                                }
                            }
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

            foreach (KeyValuePair<long, ConnectionNetworkLayoutProcessor> cnlConnectionLayout in nlpNetworkLayout.ChildConnectionProcessors)
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

            if (m_agmActiveGameManager.m_fimFrameDataInterpolationManager.m_ifdUnsmoothedLastFrameData != null)
            {
                InterpolatedFrameDataGen ifdInterpolatedFrameData = m_agmActiveGameManager.m_fimFrameDataInterpolationManager.m_ifdSmoothedInterpolatedFrameData;

                if (ifdInterpolatedFrameData.m_fixShipHealthErrorOffset != null)
                {
                    if (m_sstSimState.m_fShipHealth.Length != ifdInterpolatedFrameData.m_fixShipHealthErrorAdjusted.Length)
                    {
                        m_sstSimState.m_lPeersAssignedToSlot = new long[ifdInterpolatedFrameData.m_lPeersAssignedToSlot.Length];
                        m_sstSimState.m_fShipHealth = new float[ifdInterpolatedFrameData.m_fixShipHealthErrorAdjusted.Length];
                        m_sstSimState.m_fShipSpawnCountdown = new float[ifdInterpolatedFrameData.m_fixShipHealthErrorAdjusted.Length];
                        m_sstSimState.m_fShipPosX = new float[ifdInterpolatedFrameData.m_fixShipHealthErrorAdjusted.Length];
                        m_sstSimState.m_fShipPosY = new float[ifdInterpolatedFrameData.m_fixShipHealthErrorAdjusted.Length];
                        m_sstSimState.m_fShipVelocityX = new float[ifdInterpolatedFrameData.m_fixShipHealthErrorAdjusted.Length];
                        m_sstSimState.m_fShipVelocityY = new float[ifdInterpolatedFrameData.m_fixShipHealthErrorAdjusted.Length];
                        m_sstSimState.m_fShipRotation = new float[ifdInterpolatedFrameData.m_fixShipBaseAngleErrorAdjusted.Length];
                        m_sstSimState.m_fShipWeaponChargeUp = new float[ifdInterpolatedFrameData.m_fixTimeUntilLaserFireErrorAdjusted.Length];
                        m_sstSimState.m_fShipWeaponCoolDown = new float[ifdInterpolatedFrameData.m_fixTimeUntilNextFireErrorAdjusted.Length];
                    }

                    for (int i = 0; i < ifdInterpolatedFrameData.m_fixShipHealthErrorOffset.Length; i++)
                    {
                        m_sstSimState.m_lPeersAssignedToSlot[i] = ifdInterpolatedFrameData.m_lPeersAssignedToSlot[i];
                        m_sstSimState.m_fShipHealth[i] = ifdInterpolatedFrameData.m_fixShipHealth[i];
                        m_sstSimState.m_fShipSpawnCountdown[i] = ifdInterpolatedFrameData.m_fixTimeUntilRespawnErrorAdjusted[i];
                        m_sstSimState.m_fShipPosX[i] = ifdInterpolatedFrameData.m_fixShipPosXErrorAdjusted[i];
                        m_sstSimState.m_fShipPosY[i] = ifdInterpolatedFrameData.m_fixShipPosYErrorAdjusted[i];
                        m_sstSimState.m_fShipVelocityX[i] = ifdInterpolatedFrameData.m_fixShipVelocityXErrorAdjusted[i];
                        m_sstSimState.m_fShipVelocityY[i] = ifdInterpolatedFrameData.m_fixShipVelocityYErrorAdjusted[i];
                        m_sstSimState.m_fShipRotation[i] = Mathf.Deg2Rad * ifdInterpolatedFrameData.m_fixShipBaseAngle[i];
                        m_sstSimState.m_fShipWeaponChargeUp[i] = ifdInterpolatedFrameData.m_fixTimeUntilLaserFireErrorAdjusted[i];
                        m_sstSimState.m_fShipWeaponCoolDown[i] = ifdInterpolatedFrameData.m_fixTimeUntilNextFireErrorAdjusted[i];
                    }

                    if (m_sstSimState.m_fLazerLife.Length != ifdInterpolatedFrameData.m_fixLazerLifeRemaining.Length)
                    {
                        
                        m_sstSimState.m_fLazerLife = new float[ifdInterpolatedFrameData.m_fixLazerLifeRemaining.Length];
                        m_sstSimState.m_fLazerPosX = new float[ifdInterpolatedFrameData.m_fixLazerLifeRemaining.Length];
                        m_sstSimState.m_fLazerPosY = new float[ifdInterpolatedFrameData.m_fixLazerLifeRemaining.Length];
                    }                                                                                                 

                    for (int i = 0; i < ifdInterpolatedFrameData.m_fixLazerLifeRemaining.Length; i++)
                    {
                        m_sstSimState.m_fLazerLife[i] = ifdInterpolatedFrameData.m_fixLazerLifeRemaining[i];
                        m_sstSimState.m_fLazerPosX[i] = ifdInterpolatedFrameData.m_fixLazerPositionX[i];
                        m_sstSimState.m_fLazerPosY[i] = ifdInterpolatedFrameData.m_fixLazerPositionY[i];
                    }
                }

            }
        }

        protected void OnDrawGizmosSelected()
        {
            if (m_sstSimState.m_bDrawDebugData)
            {
                BaseDrawGismos(1);
            }
        }

        protected void OnDrawGizmos()
        {
            if (m_sstSimState.m_bDrawDebugData)
            {
                BaseDrawGismos(0.25f);
            }
        }

        protected void BaseDrawGismos(float fAlpha)
        {
            if (m_sstSimState.m_bDrawDebugData && m_sstSimState.m_fShipHealth != null)
            {
                for (int i = 0; i < m_sstSimState.m_fShipHealth.Length; i++)
                {
                    if (m_sstSimState.m_fShipHealth[i] > 0)
                    {

                        // Draw a random colour sphere for each player
                        Gizmos.color = new Color((((m_sstSimState.m_lPeersAssignedToSlot[i] % 256) + 256) % 256) / 256.0f,
                            ((((m_sstSimState.m_lPeersAssignedToSlot[i] >> 2) % 256) + 256) % 256) / 256.0f,
                            ((((m_sstSimState.m_lPeersAssignedToSlot[i] >> 4) % 256) + 256) % 256) / 256.0f, fAlpha);


                        Gizmos.DrawSphere(new Vector3(m_sstSimState.m_fShipPosX[i], 0, m_sstSimState.m_fShipPosY[i]), (float)m_sdiSettingsDataInderface.m_fixShipSize.Value);
                        Gizmos.DrawLine(new Vector3(m_sstSimState.m_fShipPosX[i], 0, m_sstSimState.m_fShipPosY[i]),
                            new Vector3(m_sstSimState.m_fShipPosX[i], 0, m_sstSimState.m_fShipPosY[i]) +
                            (new Vector3(Mathf.Cos(m_sstSimState.m_fShipRotation[i]), 0, Mathf.Sin(m_sstSimState.m_fShipRotation[i])) * 4));
                    }
                }

                for(int i = 0; i < m_sstSimState.m_fLazerLife.Length; i++)
                {
                    if(m_sstSimState.m_fLazerLife[i] > 0)
                    {
                        Gizmos.color = new Color(0, 0, 1, fAlpha);

                        Gizmos.DrawSphere(new Vector3(m_sstSimState.m_fLazerPosX[i], 0, m_sstSimState.m_fLazerPosY[i]), (float)m_sdiSettingsDataInderface.m_fixLazerSize.Value);
                    }
                }
            }

            if (m_cdaConstSimData != null && m_cdaConstSimData.m_fixAsteroidPositionX != null)
            {
                for (int i = 0; i < m_cdaConstSimData.m_fixAsteroidPositionX.Length; i++)
                {
                    // Draw a random colour sphere for each player
                    Gizmos.color = new Color(1, 0.5f, 0, 1);

                    Gizmos.DrawSphere(new Vector3((float)m_cdaConstSimData.m_fixAsteroidPositionX[i], 0, (float)m_cdaConstSimData.m_fixAsteroidPositionY[i]), (float)m_cdaConstSimData.m_fixAsteroidSize[i]);
                }
            }    
        }
    }
}