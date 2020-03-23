﻿using Networking;
using Sim;
using System;
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

        //the web interface 
        public WebInterface m_winWebInterface = null;

        //the game sim
        public SimManager m_smgSimManager = null;

        //the peer to peer network
        public NetworkConnection m_ncnNetworkConnection;

        //the interface between the p2p network and the web API
        public NetworkGatewayManager m_ngmGatewayManager;

        public NetworkConnectionPropagatorProcessor m_ncpConnectionPropegator;

        //the amount of time to wait to get gateway before timing out and starting again
        protected float m_fGettingGatewayTimeout = 20f;

        //the amount of time to wait before timing out a connection through a gateway
        protected float m_fGatewayConnectionTimeout = 20f;
        protected DateTime m_dtmConnectThroughGateStart;

        //the timeout time for getting sim state from cluster
        protected float m_fGettingSimStateTimeOut = 20f;
        protected DateTime m_dtmGettingSimStateStart;


        public ActiveGameManager(WebInterface winWebInterface)
        {
            m_winWebInterface = winWebInterface;

            //start the connection process
            EnterGettingGateway();
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

            //set state to getting gateway
            State = ActiveGameState.GettingGateway;

            //request gateway from webinterface
            if (m_winWebInterface.SearchForGateway() == false)
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
            if (m_winWebInterface.ExternalGatewayCommunicationStatus.m_cmsStatus == WebInterface.WebAPICommunicationTracker.CommunctionStatus.Succedded)
            {
                //transition to connecting through gateway 
                EnterConnectingThroughGateway();

                return;
            }
            else if (m_winWebInterface.ExternalGatewayCommunicationStatus.m_cmsStatus == WebInterface.WebAPICommunicationTracker.CommunctionStatus.Failed &&
               ( m_winWebInterface.ExternalGatewayCommunicationStatus.ShouldRestart() == false || m_winWebInterface.NoGatewayExistsOnServer))
            {
                EnterSetUpNewSim();
                return;
            }
        }

        protected void EnterConnectingThroughGateway()
        {
            Debug.Log($"Enter {ActiveGameState.ConnectingThroughGateway.ToString()} state");

            State = ActiveGameState.ConnectingThroughGateway;
            m_dtmConnectThroughGateStart = DateTime.UtcNow;

            //tell p2p network to start a new connection through gateway

            //get the gateway peer
            long lConnectionID = m_winWebInterface.ExternalGateway.Value.m_lGateOwnerUserID;

            //tell the connection propegator who to try to connect to
            m_ncpConnectionPropegator.StartRequest(lConnectionID);
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

            if (tspTimeSinceConnectionStart.TotalSeconds > m_fGatewayConnectionTimeout)
            {

                Debug.Log("Connection attempt timed out");

                //connection attempt timed out restarting active game connection process
                Reset();
                return;
            }
        }

        protected void EnterGettingSimStateFromCluster()
        {
            Debug.Log($"Enter {ActiveGameState.GettingSimStateFromCluster.ToString()} state");

            State = ActiveGameState.GettingSimStateFromCluster;
            m_dtmGettingSimStateStart = DateTime.UtcNow;
        }

        protected void UpdateGettingSimStateFromCluster()
        {
            //update networking 
            m_ncnNetworkConnection.UpdateConnectionsAndProcessors();

            //update gateway management 
            HandleGateway();


            //check if sim state has been fetched from cluster
            bool bHasFetchedSimState = true;
            if (bHasFetchedSimState)
            {
                //set sim state using fetched state 


                //transition to running normal game
                EnterRunningGame();

                return;
            }

            //check if getting sim state has timed out 
            TimeSpan tspTimeSinceGetSimStateStarted = DateTime.UtcNow - m_dtmGettingSimStateStart;

            if (tspTimeSinceGetSimStateStarted.TotalSeconds > m_fGettingSimStateTimeOut)
            {
                //restart the connection process
                Reset();
                return;
            }
        }

        protected void EnterSetUpNewSim()
        {
            Debug.Log($"Enter {ActiveGameState.SetUpNewSim.ToString()} state");

            State = ActiveGameState.SetUpNewSim;

            //use passed in target sim settings to setup inital sim

            // sim manager setup sim

        }

        protected void UpdateSetUpNewSim()
        {
            //wait for sim setup to finish

            bool bIsSimSetup = true;

            if (bIsSimSetup)
            {
                //tell network layer global messaging system that it is the first peer in the 
                m_ncnNetworkConnection.OnFirstPeerInSwarm();

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

            State = ActiveGameState.RunningStandardGame;

            //change setting on network to running standard game
        }

        protected void UpdateRunningGame()
        {

            // ------------ check for errors -----------------
            //check for error in networking

            //check for error in sim

            //------------- update inputs --------------------

            //get inputs from sim and apply them to the network global message manager

            //get external inputs from global message manager and apply them to the sim

            //------------ Update Systems --------------------

            //update simulation

            //update networking 
            m_ncnNetworkConnection.UpdateConnectionsAndProcessors();

            //update Web Interface(may be external? )

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

        /// <summary>
        /// gets messages from web api and passes them to the peer to peer networking layer
        /// </summary>
        protected void HandleGateway()
        {
            //check if Gateway is needed
            if (m_ngmGatewayManager.NeedsOpenGateway)
            {
                //get sim state and use it to set gateway
                SimStatus smsStatus = new SimStatus()
                {
                    m_iRemainingSlots = 10,
                    m_iSimStatus = (int)SimStatus.State.Lobby
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
                if(m_winWebInterface.IsGettingMessagesFromServer() == false)
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
            //create network
            m_ncnNetworkConnection = new NetworkConnection(m_winWebInterface.UserID, new FakeWebRTCFactory());

            //add network processors
            m_ncnNetworkConnection.AddPacketProcessor(new TimeNetworkProcessor());
            m_ncnNetworkConnection.AddPacketProcessor(new NetworkdLargePacketTransferManager());
            m_ncnNetworkConnection.AddPacketProcessor(new NetworkLayoutProcessor());

            m_ngmGatewayManager = new NetworkGatewayManager();
            m_ncnNetworkConnection.AddPacketProcessor(m_ngmGatewayManager);

            m_ncpConnectionPropegator = new NetworkConnectionPropagatorProcessor();
            m_ncnNetworkConnection.AddPacketProcessor(m_ncpConnectionPropegator);

            m_ncnNetworkConnection.AddPacketProcessor(new NetworkGlobalMessengerProcessor());

        }
    }
}
