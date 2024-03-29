﻿using GameStateView;
using GameViewUI;
using Networking;
using Sim;
using SimDataInterpolation;
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// the point of this class is to manager the setting up and running the peer to peer network and game sim given the inital inputs
/// </summary>
namespace GameManagers
{
    public class ActiveGameManager
    {
        public enum ActiveGameState
        {

            //------------------- Match browser if using match brows option ---

            //------------------- Entry point if auto playing -----------------
            GettingGateway,

            //------------------- If Existing Gateway was found ---------------
            ConnectingThroughGateway,
            GettingSimStateFromCluster,

            //------------------- If no gateway was found ---------------------
            SetUpNewSim,

            //------------------- Run standard game --------------------------- 
            RunningStandardGame,

            //------------------- Game Over -----------------------------------
            GameEnded,

            Error,

        }

        //the state of the game
        public ActiveGameState State { get; private set; } = ActiveGameState.GettingGateway;

        public SimProcessManager<FrameData, ConstData, SimProcessorSettings> m_spmSimProcessManager;

        //the sim manager that handles getting data from the data bridge and passing it to the sim to be processed 
        public TestingSimManager<FrameData, ConstData,SimProcessorSettings> m_tsmSimManager;

        //data structure for passing all data from networking to sim 
        public NetworkingDataBridge m_ndbDataBridge;

        //the web interface 
        public WebInterface m_winWebInterface = null;

        public SimProcessorSettings m_sdaSimSettingsData;

        public InterpolationErrorCorrectionSettingsGen m_ecsInterpolationErrorCorrectionSettings;

        public NetworkConnectionSettings m_ncsNetworkConnectionSettings;

        public ConstData m_cdaConstData;

        //the peer to peer network
        public NetworkConnection m_ncnNetworkConnection;
               
        //the interface between the p2p network and the web API
        public NetworkGatewayManager m_ngmGatewayManager;

        public TimeNetworkProcessor m_tnpTimeManager;

        public NetworkConnectionPropagatorProcessor m_ncpConnectionPropegator;

        public NetworkGlobalMessengerProcessor m_ngpGlobalMessagingProcessor;

        public SimStateSyncNetworkProcessor m_sssStateSyncProcessor;

        public DateTime m_dtmSimStateGetStart;

        public LocalPeerInputManager m_lpiLocalPeerInputManager;

        public RandomInputGenerator m_pitLocalPeerInputTester;


        public FrameDataInterpolatorManager<
            FrameData,
            InterpolationErrorCorrectionSettingsGen,
            InterpolatedFrameDataGen,
            FrameDataInterpolator,
            TestingSimManager<FrameData, ConstData, SimProcessorSettings>,
            NetworkingDataBridge>
            m_fimFrameDataInterpolationManager;

        public IGameStateView m_gsvGameStateView;

        public UIStateManager m_usmUIStateManager;

        public GameStateViewCamera m_gvcGameCamera;

        public IPeerTransmitterFactory m_ptfTransmitterFactory;

        public int m_iDefaultMaxGameSize = 6;

        protected DateTime m_dtmConnectThroughGateStart;

        protected DateTime m_dtmGettingSimStateStart;

        //dictionary of all games this client has attempted to join but failed
        protected Dictionary<long, int> m_dicConnectionAttempts;

        public ActiveGameManager(
            SimProcessorSettings sdaSimSettingsData, 
            InterpolationErrorCorrectionSettingsGen ecsInterpolationErrorCorrectionSettings, 
            NetworkConnectionSettings ncsNetworkConnectionSettings,
            ConstData cdaConstantSimData, 
            WebInterface winWebInterface, 
            IPeerTransmitterFactory ptfTransmitterFactory,
            IGameStateView gsvGameStateViewSpawner,
            UIStateManager usmUIManager,
            GameStateViewCamera gvcGameViewCamera)
        {
            m_ptfTransmitterFactory = ptfTransmitterFactory;
            m_winWebInterface = winWebInterface;
            m_sdaSimSettingsData = sdaSimSettingsData;
            m_ecsInterpolationErrorCorrectionSettings = ecsInterpolationErrorCorrectionSettings;
            m_ncsNetworkConnectionSettings = ncsNetworkConnectionSettings;
            m_cdaConstData = cdaConstantSimData;
            m_gsvGameStateView = gsvGameStateViewSpawner;
            m_usmUIStateManager = usmUIManager;
            m_gvcGameCamera = gvcGameViewCamera;

            m_dicConnectionAttempts = new Dictionary<long, int>();

            //start the connection process
            EnterGettingGateway();
        }

        public void OnCleanup()
        {
            //clean up existing connections
            m_ncnNetworkConnection?.OnCleanup();
        }

        public void UpdateGame(float fDeltaTime)
        {

            //update current state 
            switch (State)
            {
                case ActiveGameState.GettingGateway:

                    //wait for web interface to either fail or succeed 
                    UpdateGettingGateway();

                    break;

                case ActiveGameState.ConnectingThroughGateway:

                    //get connection request from networkconnection
                    //send request through gate and listen to response
                    //apply response to network connection
                    //mark network connection as connected to cluster
                    //exit state when connection through network connection estabished 
                    UpdateConnectingThroughGateway();

                    break;

                case ActiveGameState.GettingSimStateFromCluster:

                    //request sim state from cluster
                    //exit when sim state pulled from cluster
                    UpdateGettingSimStateFromCluster();

                    break;

                case ActiveGameState.SetUpNewSim:
                    //no existing game found so setting up new sim internally 
                    //return when sim created using start settings 
                    UpdateSetUpNewSim();

                    break;

                case ActiveGameState.RunningStandardGame:

                    //listen for any disconnect or stop commands from sim 
                    //send any sim game logs to server
                    //check if gateway needs to be setup
                    //if gateway is running pass any messages for gateway manager through to network connector
                    UpdateRunningGame();

                    //when sim ends ent


                    break;
                case ActiveGameState.GameEnded:

                    UpdateGameEndState();

                    break;
            }


        }

        protected void Reset()
        {
            //reset sim 
            // m_smgSimManager = new SimManager();

            //reset networking 
            //m_ncnNetworkConnection = new NetworkConnection();

            //start getting gateway
            EnterGettingGateway();

        }

        #region GameStates

        protected void EnterGettingGateway()
        {
            Debug.Log($"Enter {ActiveGameState.GettingGateway.ToString()} state");
            m_usmUIStateManager?.SetStateSetup();
            m_usmUIStateManager?.LogStartupEvent("Searching For Game");

            //set state to getting gateway
            State = ActiveGameState.GettingGateway;

            GetGatewayRequest gwrGatewayRequest = new GetGatewayRequest()
            {
                m_lFlags = m_ncsNetworkConnectionSettings.m_lFlags,
                m_lGameType = m_ncsNetworkConnectionSettings.m_lGameType
            };

            //request gateway from webinterface
            if (m_winWebInterface.SearchForGatewayList(gwrGatewayRequest) == false)
            {
                Debug.Log($"User:{m_winWebInterface.UserID} Encountered error when searching for gateway");

                //error has occured searching for gateway exit active game state
                State = ActiveGameState.Error;
                return;
            }

            //setup network to connect through gateway 
            SetupNetworking();
        }

        protected void UpdateGettingGateway()
        {
            //check if gateway found
            if (m_winWebInterface.SearchForGatewayListStatus.m_cmsStatus == WebInterface.WebAPICommunicationTracker.CommunctionStatus.Succedded)
            {

                long lBestGatewayID = 0;
                int iBestGateNumberOfConenctionAttempts = int.MaxValue;

                //evaluate gateways to pick the best one, this is done to avoid repeatedly trying to connect to a bad game
                for(int i = 0; i < m_winWebInterface.ExternalGatewayList.Length; i++)
                {
                    long lGateId = m_winWebInterface.ExternalGatewayList[i].m_lGateOwnerUserID;

                    int iConnectionAttempts = 0;

                    if(m_dicConnectionAttempts.TryGetValue(lGateId, out iConnectionAttempts))
                    {
                        if(iConnectionAttempts < iBestGateNumberOfConenctionAttempts)
                        {
                            lBestGatewayID = lGateId;
                            iBestGateNumberOfConenctionAttempts = iConnectionAttempts;
                        }
                    }
                    else
                    {
                        //if there has been no connection attempts then this is a good gate to connect through
                        lBestGatewayID = lGateId;
                        iBestGateNumberOfConenctionAttempts = 0;
                        break;
                    }
                }

                //check if it would be better to start own match at all servers are broken
                if (iBestGateNumberOfConenctionAttempts >= m_ncsNetworkConnectionSettings.m_iMaxFailedConnectionsForValidGate)
                {
                    Debug.Log($"User:{m_winWebInterface.UserID} has already tried connecting to all available gates and failed: {iBestGateNumberOfConenctionAttempts} times, starting new game");
                    EnterSetUpNewSim();
                }
                else
                {

                    //update the connection attempt dictionary
                    m_dicConnectionAttempts[lBestGatewayID] = iBestGateNumberOfConenctionAttempts + 1;

                    //transition to connecting through gateway 
                    EnterConnectingThroughGateway(lBestGatewayID);
                }

                return;
            }
            else if (m_winWebInterface.SearchForGatewayListStatus.m_cmsStatus == WebInterface.WebAPICommunicationTracker.CommunctionStatus.Failed &&
               (m_winWebInterface.SearchForGatewayListStatus.ShouldRestart() == false || m_winWebInterface.NoGatewayExistsOnServer))
            {
                if(m_winWebInterface.SearchForGatewayListStatus.ShouldRestart() == false)
                {
                    Debug.Log($"User:{m_winWebInterface.UserID} could not get gateway");
                }

                EnterSetUpNewSim();
                return;
            }
        }

        protected void EnterConnectingThroughGateway(long lConnectionID)
        {
            Debug.Log($"Enter {ActiveGameState.ConnectingThroughGateway.ToString()} state");

            m_usmUIStateManager?.LogStartupEvent("Game found connecting to lead peer");

            State = ActiveGameState.ConnectingThroughGateway;
            m_dtmConnectThroughGateStart = DateTime.UtcNow;

            //tell p2p network to start a new connection through gateway

            Debug.Assert(lConnectionID != 0, "Gateway ID recieved from server appears to be invalid");


            //setup the peer to peer network to match the settings of the taget network
            m_ngpGlobalMessagingProcessor.Initalize(m_sdaSimSettingsData.MaxPlayers);

            //tell the connection propegator who to try to connect to
            m_ncpConnectionPropegator.StartRequest(lConnectionID);

            //start setting up the visuals 
            m_gsvGameStateView?.SetupConstDataViewEntities(m_cdaConstData);
        }

        protected void UpdateConnectingThroughGateway()
        {
            //check if gateway has any new messages for network
            //check if network has any new messages to send through gateway 
            HandleGateway();

            //update networking processes 
            m_ncnNetworkConnection.UpdateConnectionsAndProcessors();

            //check if network has established connection
            if (m_ncnNetworkConnection.m_bIsConnectedToSwarm)
            {
                //start getting sim state from cluster
                EnterGettingSimStateFromCluster();
                return;
            }

            //check for timeout 
            TimeSpan tspTimeSinceConnectionStart = DateTime.UtcNow - m_dtmConnectThroughGateStart;

            //check if connection failed 

            if (tspTimeSinceConnectionStart.TotalSeconds > m_ncsNetworkConnectionSettings.m_fGatewayConnectionTimeout || m_ncnNetworkConnection.HaveAllConnectionsFailed())
            {
                if (tspTimeSinceConnectionStart.TotalSeconds > m_ncsNetworkConnectionSettings.m_fGatewayConnectionTimeout)
                {
                    Debug.Log("Connection through gateway attempt timed out");
                }

                if (m_ncnNetworkConnection.HaveAllConnectionsFailed())
                {
                    Debug.Log("Connection through gateway failed");
                }

                m_usmUIStateManager?.LogStartupEvent("Failed to connect to lead peer restarting connection process");

                //connection attempt timed out restarting active game connection process
                Reset();
                return;
            }
        }

        protected void EnterGettingSimStateFromCluster()
        {
            Debug.Log($"Enter {ActiveGameState.GettingSimStateFromCluster.ToString()} state");

            m_usmUIStateManager?.LogStartupEvent("Getting game state from peers");

            State = ActiveGameState.GettingSimStateFromCluster;
            m_dtmGettingSimStateStart = DateTime.UtcNow;

            m_tsmSimManager.InitalizeAsConnectingPeer();
        }

        protected void UpdateGettingSimStateFromCluster()
        {
            //update networking 
            m_ncnNetworkConnection.UpdateConnectionsAndProcessors();

            //update gateway management 
            HandleGateway();

            //update UI
            m_usmUIStateManager?.UpdateNeworkView(m_ncnNetworkConnection);


            //check if sim state has been fetched from cluster
            bool bHasFetchedSimState = true;

            //check if the user is connected to the swarm and has been voted in to a player position
            if (m_ngpGlobalMessagingProcessor.m_staState != NetworkGlobalMessengerProcessor.State.Active)
            {
                bHasFetchedSimState = false;
            }

            //TODO: in the future start game once first full state is retrieved from server not once state sync time out has come to an end 
            //check if sim state has been fetched 
            if (m_sssStateSyncProcessor.m_bIsFullStateSynced == false && m_sssStateSyncProcessor.m_staState != SimStateSyncNetworkProcessor.State.GettingStateData )
            {
                bHasFetchedSimState = false;

                //check if connected to global messaging system
                if (m_ngpGlobalMessagingProcessor.m_staState == NetworkGlobalMessengerProcessor.State.Connected || m_ngpGlobalMessagingProcessor.m_staState == NetworkGlobalMessengerProcessor.State.Active)
                {
                    Debug.Log("Fetching Sim State From peers");
                    m_usmUIStateManager?.LogStartupEvent("Getting world state from peers");

                    GetSimDataFromPeers();
                }
            }

            //check that state was synced successfully 
            if (m_sssStateSyncProcessor.m_bIsFullStateSynced == true && m_sssStateSyncProcessor.m_staState == SimStateSyncNetworkProcessor.State.SyncFailed)
            {
                Debug.LogError("something went wrong, some how we have a full sate but ther was an error with the sync");
                bHasFetchedSimState = false;
            }

            //this gets set to false when there is data on the bridge ready for the sim to process
            if (m_ndbDataBridge.m_bIsThereDataOnBridgeForSimToInitWith == false)
            {
                bHasFetchedSimState = false;
            }
                       

            if (bHasFetchedSimState)
            {
                //set sim state using fetched state 
                //transition to running normal game
                EnterRunningGame();

                return;
            }

            //check if getting sim state has timed out 
            TimeSpan tspTimeSinceGetSimStateStarted = DateTime.UtcNow - m_dtmGettingSimStateStart;

            if (tspTimeSinceGetSimStateStarted.TotalSeconds > m_ncsNetworkConnectionSettings.m_fGettingSimStateTimeOut)
            {
                Debug.Log($"Getting sim state timed out before game state was fetched, Global Messaging status:{m_ngpGlobalMessagingProcessor.m_staState}, Sim Data Fetch Status:{m_ngpGlobalMessagingProcessor.m_staState}, has a full state bben synced:{m_sssStateSyncProcessor.m_bIsFullStateSynced}");

                m_usmUIStateManager?.LogStartupEvent("Unable to get game state from peers, restarting connection process");

                //restart the connection process
                Reset();
                return;
            }
        }

        protected void EnterSetUpNewSim()
        {
            Debug.Log($"Enter {ActiveGameState.SetUpNewSim.ToString()} state");

            State = ActiveGameState.SetUpNewSim;

            //setup the peer to peer network settings 
            m_ngpGlobalMessagingProcessor.Initalize(m_sdaSimSettingsData.MaxPlayers);

            //use passed in target sim settings to setup inital sim

            // sim manager setup sim
            m_tsmSimManager.InitalizeAsFirstPeer(m_ncnNetworkConnection.m_lPeerID);

            //start setting up the visuals if visual system is attached
            m_gsvGameStateView?.SetupConstDataViewEntities(m_cdaConstData);

        }

        protected void UpdateSetUpNewSim()
        {
            Debug.Log("Update Setup State");

            //wait for sim setup to finish

            bool bIsSimSetup = true;

            if (bIsSimSetup)
            {
                Debug.Log("Indicate First in swarm");

                //tell network layer global messaging system that it is the first peer in the 
                m_ncnNetworkConnection.OnFirstPeerInSwarm();

                Debug.Log("Run on connect to swarm");
                //activate network layer to start looking for new connections
                m_ncnNetworkConnection.OnConnectToSwarm();

                //enter run game state 
                EnterRunningGame();
                return;
            }
        }

        protected void EnterRunningGame()
        {
            Debug.Log($"Enter {ActiveGameState.RunningStandardGame.ToString()} state");

            m_usmUIStateManager?.LogStartupEvent("Starting Game");

            State = ActiveGameState.RunningStandardGame;

            //initalize frame data interpolator 
            m_fimFrameDataInterpolationManager.Initalize();
           
        }

        protected void UpdateRunningGame()
        {

            // ------------ check for errors -----------------
            //check for error in networking

            //check for error in sim

            // ------------ Check For Failed Sim State Get ---

            if(m_ndbDataBridge.m_sssSimStartStateSyncStatus == SimStateSyncNetworkProcessor.State.SyncFailed)
            {
                if((DateTime.UtcNow - m_dtmGettingSimStateStart).TotalSeconds > m_ncsNetworkConnectionSettings.m_fGettingSimStateTimeOut)
                {
                    Debug.Log("Error getting sim state, get sim state timed out, forced to reset get game process");

                    m_usmUIStateManager?.LogStartupEvent("World state recieved from peers was not valid and not able to get a replacement valid world state from peers, restarting connection process");

                    //getting sim state ultamatly failed, need to reset get process
                    Reset();
                    return;

                }
                else
                {
                    //try to get the sim state agin 
                    GetSimDataFromPeers();

                }
            }

            //------------- update inputs --------------------

            //get inputs from sim and apply them to the network global message manager

            //get inputs from local peer and apply them to networking 
            m_lpiLocalPeerInputManager.Update();

            //------------ Update Systems --------------------

            //update simulation
            m_tsmSimManager.Update();

            //update interpolated data 
            m_fimFrameDataInterpolationManager.UpdateInterpolatedData();

            //update networking 
            m_ncnNetworkConnection.UpdateConnectionsAndProcessors();

            //update Web Interface(may be external? )

            //update visuals 
            m_gsvGameStateView?.UpdateView(m_fimFrameDataInterpolationManager.m_ifdSmoothedInterpolatedFrameData, m_sdaSimSettingsData);

            //update UI
            m_usmUIStateManager?.UpdateGameView(m_fimFrameDataInterpolationManager.m_ifdSmoothedInterpolatedFrameData, m_ncnNetworkConnection.m_lPeerID);

            //update camera 
            m_gvcGameCamera?.OnViewUpdate(m_fimFrameDataInterpolationManager.m_ifdSmoothedInterpolatedFrameData, m_ncnNetworkConnection.m_lPeerID);

            //------------ check for game state changes ------

            //check if user has been disconnected from game

            //check if game has ended 

            //enter game end state 

            //----------- check for networking system changes 

            //update gateway management 
            HandleGateway();


        }

        protected void EnterEndGameState()
        {
            Debug.Log($"Enter {ActiveGameState.GameEnded.ToString()} state");

            State = ActiveGameState.GameEnded;

            //deactivate networking 

            //deactivate gateway
        }

        protected void UpdateGameEndState()
        {
            //wait for user to exit back to main menu
        }

        #endregion

        protected void GetSimDataFromPeers()
        {
            //check if already attempting to get sim state
            if (m_sssStateSyncProcessor.m_staState != SimStateSyncNetworkProcessor.State.GettingStateData)
            {
                //schedule new request for data

                //get current time 
                DateTime dtmCurrentTime = m_tnpTimeManager.NetworkTime;

                //get latency to worst connection 
                TimeSpan tspWorstLatency = m_tnpTimeManager.LargetsRTT.TotalSeconds < m_ncsNetworkConnectionSettings.m_fStartSimStateMaxLagCompensation ?
                    m_tnpTimeManager.LargetsRTT : TimeSpan.FromSeconds(m_ncsNetworkConnectionSettings.m_fStartSimStateMaxLagCompensation);

                //how far in the future should we try and get a sim state
                TimeSpan tspStateLeadTime = tspWorstLatency + TimeSpan.FromSeconds(m_ncsNetworkConnectionSettings.m_fStartSimStateRequestLeadTime);

                //calculate a time that the request will reach all peers before the peer has discarded the data 
                DateTime dtmSimStateRequestTime = dtmCurrentTime + tspStateLeadTime;

                //get list of trusted peers
                List<long> tupActivePeerList = m_ngpGlobalMessagingProcessor.m_chmChainManager.m_chlBestChainHead.m_gmsState.GetActivePeerIDs();

                //send request to peers
                m_sssStateSyncProcessor.RequestSimData(dtmSimStateRequestTime, tupActivePeerList);

            }
        }

        /// <summary>
        /// gets messages from web api and passes them to the peer to peer networking layer
        /// </summary>
        protected void HandleGateway()
        {
            //check if Gateway is needed
            if (m_ngmGatewayManager.NeedsOpenGateway)
            {
                //get sim state and use it to set gateway
                GatewayState smsStatus = new GatewayState()
                {
                    m_iRemainingSlots = 10,
                };

                m_winWebInterface.SetGateway(smsStatus);

            }
            else
            {
                //make sure gateway is disabled
                m_winWebInterface.CloseGateway();
            }

            //if still in the connection phase of an active gateway
            if (m_ncnNetworkConnection.m_bIsConnectedToSwarm == false || m_ngmGatewayManager.NeedsOpenGateway)
            {
                //make sure getting messages from server is enabled
                if (m_winWebInterface.IsGettingMessagesFromServer() == false)
                {
                    m_winWebInterface.StartGettingMessages();
                }

                //get any inputs from web and apply them to p2p network
                while (m_winWebInterface.MessagesFromServer.Count > 0)
                {
                    UserMessage usmMessage = m_winWebInterface.MessagesFromServer.Dequeue();

                    Debug.Log($"recieved message{usmMessage.m_strMessage} from: {usmMessage.m_lFromUser}");

                    m_ngmGatewayManager.ProcessMessageFromGateway(usmMessage);
                }

                //get any inputs from p2p network that need to be sent through network
                while (m_ngmGatewayManager.MessagesToSend.Count > 0)
                {
                    SendMessageCommand smcMessage = m_ngmGatewayManager.MessagesToSend.Dequeue();

                    Debug.Log($"sending message {smcMessage.m_strMessage} from: {smcMessage.m_lFromID} to:{smcMessage.m_lToID}");

                    m_winWebInterface.SendMessage(smcMessage);
                }
            }
            else
            {
                //check if getting messages from server when you dont need to be
                if (m_winWebInterface.IsGettingMessagesFromServer())
                {
                    Debug.Log($"User:{m_winWebInterface.UserID} stopping getting messages from server");
                    m_winWebInterface.StopGettingMessages();
                }
            }
        }

        protected void SetupNetworking()
        {
            //clean up existing network
            m_ncnNetworkConnection?.OnCleanup();

            //create network
            m_ncnNetworkConnection = new NetworkConnection(m_winWebInterface.UserID, m_ptfTransmitterFactory, m_ncsNetworkConnectionSettings);

            //create network data bridge
            m_ndbDataBridge = new NetworkingDataBridge();

            //set the local peer id this is for debug only 
            m_ndbDataBridge.m_lLocalPeerID = m_winWebInterface.UserID;


            //add network processors
            m_tnpTimeManager = new TimeNetworkProcessor(m_ndbDataBridge);
            m_ncnNetworkConnection.AddPacketProcessor(m_tnpTimeManager);

            m_ncnNetworkConnection.AddPacketProcessor(new NetworkLargePacketTransferManager());
            m_ncnNetworkConnection.AddPacketProcessor(new NetworkLayoutProcessor());

            m_ngmGatewayManager = new NetworkGatewayManager();
            m_ncnNetworkConnection.AddPacketProcessor(m_ngmGatewayManager);

            m_ncpConnectionPropegator = new NetworkConnectionPropagatorProcessor();
            m_ncnNetworkConnection.AddPacketProcessor(m_ncpConnectionPropegator);


            //manager for all the processes that calculate new game stated
            m_spmSimProcessManager = new SimProcessManager<FrameData, ConstData, SimProcessorSettings>();

            //add code to setup ship movement values
            SetupShipMovementProcess<FrameData, SimProcessorSettings> smpShipMovemntProcessor = new SetupShipMovementProcess<FrameData, SimProcessorSettings>();

            m_spmSimProcessManager.AddSimSetupProcess(smpShipMovemntProcessor);

            //add code to process peer slot asignment 
            ProcessPeerSlotAssignment<FrameData, ConstData, SimProcessorSettings> psaPeerSlotAsignment = new ProcessPeerSlotAssignment<FrameData, ConstData, SimProcessorSettings>();

            m_spmSimProcessManager.AddSimSetupProcess(psaPeerSlotAsignment);
            m_spmSimProcessManager.AddSimProcess(psaPeerSlotAsignment);

            //add code to process user inputs
            ProcessPeerInputs<FrameData, ConstData, SimProcessorSettings> ppiPeerInputProcessor = new ProcessPeerInputs<FrameData, ConstData, SimProcessorSettings>();

            m_spmSimProcessManager.AddSimSetupProcess(ppiPeerInputProcessor);
            m_spmSimProcessManager.AddSimProcess(ppiPeerInputProcessor);

            //add code to spawn ships
            ProcessShipSpawn<FrameData, ConstData, SimProcessorSettings> pssShipSpawnProcess = new ProcessShipSpawn<FrameData, ConstData, SimProcessorSettings>();

            m_spmSimProcessManager.AddSimSetupProcess(pssShipSpawnProcess);
            m_spmSimProcessManager.AddSimProcess(pssShipSpawnProcess);

            //add ship health
            ProcessShipHealth<FrameData, ConstData, SimProcessorSettings> pshShipHealthProcessor = new ProcessShipHealth<FrameData, ConstData, SimProcessorSettings>();

            m_spmSimProcessManager.AddSimSetupProcess(pshShipHealthProcessor);
            m_spmSimProcessManager.AddSimProcess(pshShipHealthProcessor);

            //add ship movement 
            ProcessShipMovement<FrameData, ConstData, SimProcessorSettings> psmShipMovementProcessor = new ProcessShipMovement<FrameData, ConstData, SimProcessorSettings>();

            m_spmSimProcessManager.AddSimProcess(psmShipMovementProcessor);

            //add ship to ship collision 
            ProcessShipShipCollisions<FrameData, ConstData, SimProcessorSettings> sscShipShipCollisionProcessor = new ProcessShipShipCollisions<FrameData, ConstData, SimProcessorSettings>();

            m_spmSimProcessManager.AddSimProcess(sscShipShipCollisionProcessor);

            //add ship asteroid collisions 
            ProcessShipAsteroidCollisions<FrameData, ConstData, SimProcessorSettings> sacShipAsteroidCollisionProcessor = new ProcessShipAsteroidCollisions<FrameData, ConstData, SimProcessorSettings>();

            m_spmSimProcessManager.AddSimProcess(sacShipAsteroidCollisionProcessor);

            //add ship weapons processor
            ProcessShipWeapons<FrameData, ConstData, SimProcessorSettings> pcwShipWeaponsProcessor = new ProcessShipWeapons<FrameData, ConstData, SimProcessorSettings>();

            m_spmSimProcessManager.AddSimSetupProcess(pcwShipWeaponsProcessor);
            m_spmSimProcessManager.AddSimProcess(pcwShipWeaponsProcessor);

            //add lazer processor  
            ProcessLazers<FrameData, ConstData, SimProcessorSettings> plzLazerProcessor = new ProcessLazers<FrameData, ConstData, SimProcessorSettings>();

            m_spmSimProcessManager.AddSimSetupProcess(plzLazerProcessor);
            m_spmSimProcessManager.AddSimProcess(plzLazerProcessor);


            //m_spmSimProcessManager.AddSimSetupProcess();

            m_tsmSimManager = new TestingSimManager<FrameData,ConstData,SimProcessorSettings>(m_cdaConstData, m_sdaSimSettingsData, m_ndbDataBridge, m_spmSimProcessManager);

            m_ngpGlobalMessagingProcessor = new NetworkGlobalMessengerProcessor(m_ndbDataBridge);
            m_ncnNetworkConnection.AddPacketProcessor(m_ngpGlobalMessagingProcessor);

            m_sssStateSyncProcessor = new SimStateSyncNetworkProcessor(m_ndbDataBridge);
            m_ncnNetworkConnection.AddPacketProcessor(m_sssStateSyncProcessor);

            m_lpiLocalPeerInputManager = new LocalPeerInputManager(m_ndbDataBridge, m_ngpGlobalMessagingProcessor);

            m_fimFrameDataInterpolationManager = new FrameDataInterpolatorManager<
                FrameData,
                InterpolationErrorCorrectionSettingsGen,
                InterpolatedFrameDataGen,
                FrameDataInterpolator,
                TestingSimManager<FrameData, ConstData, SimProcessorSettings>,
                NetworkingDataBridge>
                (
                m_ecsInterpolationErrorCorrectionSettings,
                m_tsmSimManager,
                new FrameDataInterpolator(),
                m_ndbDataBridge
                );
        }
    }
}
