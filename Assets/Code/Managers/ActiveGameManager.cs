using Networking;
using Sim;
using System;
using System.Collections;
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
        protected WebInterface m_winWebInterface = null;

        //the game sim
        protected SimManager m_smgSimManager = null;
        
        //the peer to peer network
        protected NetworkConnection m_ncnNetworkConnection;

        //the amount of time to wait to get gateway before timing out and starting again
        protected float m_fGettingGatewayTimeout = 10f;

        //the amount of time to wait before timing out a connection through a gateway
        protected float m_fGatewayConnectionTimeout = 10f;
        protected DateTime m_dtmConnectThroughGateStart;

        //the timeout time for getting sim state from cluster
        protected float m_fGettingSimStateTimeOut = 20f;
        protected DateTime m_dtmGettingSimStateStart;


        public ActiveGameManager(WebInterface winWebInterface)
        {
            m_winWebInterface = winWebInterface;
        }
        
        public void UpdateGame(float fDeltaTime)
        {

            //update current state 
            switch (State)
            {
                case ActiveGameState.GettingGateway:

                    //wait for web interface to either fail or succeed 

                    break;

                case ActiveGameState.ConnectingThroughGateway:

                    //get connection request from networkconnection
                    //send request through gate and listen to response
                    //apply response to network connection
                    //mark network connection as connected to cluster
                    //exit state when connection through network connection estabished 

                    break;

                case ActiveGameState.GettingSimStateFromCluster:

                    //request sim state from cluster
                    //exit when sim state pulled from cluster

                    break;

                case ActiveGameState.SetUpNewSim:
                    //no existing game found so setting up new sim internally 
                    //return when sim created using start settings 

                    break;

                case ActiveGameState.RunningStandardGame:

                    //listen for any disconnect or stop commands from sim 

                    //send any sim game logs to server

                    //check if gateway needs to be setup

                    //if gateway is running pass any messages for gateway manager through to network connector

                    //when sim ends ent

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

        protected void EnterGettingGateway()
        {
            //set state to getting gateway
            State = ActiveGameState.GettingGateway;

            //request gateway from webinterface
            if(m_winWebInterface.SearchForGateway() == false)
            {
                //error has occured searching for gateway exit active game state
                State = ActiveGameState.Error;
                return;
            }
        }

        protected void UpdateGettingGateway()
        {
            //check if gateway found
            if(m_winWebInterface.ExternalGatewayCommunicationStatus.m_cmsStatus == WebInterface.WebAPICommunicationTracker.CommunctionStatus.Succedded)
            {
                //transition to connecting through gateway 
                EnterConnectingThroughGateway();

                return;
            }
            else if(m_winWebInterface.ExternalGatewayCommunicationStatus.m_cmsStatus == WebInterface.WebAPICommunicationTracker.CommunctionStatus.Failed &&
                m_winWebInterface.ExternalGatewayCommunicationStatus.ShouldRestart() == false)
            {
                EnterSetUpNewSim();
                return;
            }
        }

        protected void EnterConnectingThroughGateway()
        {
            State = ActiveGameState.ConnectingThroughGateway;
            m_dtmConnectThroughGateStart = DateTime.UtcNow;

            //tell p2p network to start a new connection for gateway
            long lConnectionID = m_winWebInterface.ExternalGateway.Value.m_lOwningPlayerId;
        }

        protected void UpdateConnectingThroughGateway()
        {
            //check if gateway has any new messages for network

            //check if network has any new messages to send through gateway 

            //check if network has established connection
            bool bHasEstablishedConnection = true;
            if (bHasEstablishedConnection)
            {
                //start getting sim state from cluster
                EnterGettingSimStateFromCluster();
                return;
            }

            //check for timeout 
            TimeSpan tspTimeSinceConnectionStart = DateTime.UtcNow - m_dtmConnectThroughGateStart;

            if(tspTimeSinceConnectionStart.TotalSeconds > m_fGatewayConnectionTimeout)
            {
                //connection attempt timed out restarting active game connection process
                Reset();
                return;
            }
        }

        protected void EnterGettingSimStateFromCluster()
        {
            State = ActiveGameState.GettingSimStateFromCluster;
            m_dtmGettingSimStateStart = DateTime.UtcNow;
        }

        protected void UpdateGettingSimStateFromCluster()
        {
            //check if sim state has been fetched from cluster
            bool bHasFetchedSimState = true;
            if(bHasFetchedSimState)
            {
                //set sim state using fetched state 


                //transition to running normal game

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
            State = ActiveGameState.SetUpNewSim;

            //use passed in target sim settings to setup inital sim

            // sim manager setup sim
        }

        protected void UpdateSetUpNewSim()
        {
            //wait for sim setup to finish

            bool bIsSimSetup = true;

            if(bIsSimSetup)
            {
                //enter run game state 
                EnterRunningGame();
                return;
            }
        }

        protected void EnterRunningGame()
        {
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

            //update Web Interface(may be external? )

            //------------ check for game state changes ------

            //check if user has been disconnected from game

            //check if game has ended 

            //enter game end state 

            //----------- check for networking system changes 

            //check if Gateway is needed

            //get sim state and use it to set gateway

            //get any inputs from web and apply them to p2p network

            //get any inputs from p2p network that need to be sent through network
        }
        
        protected void EnterEndGameState()
        {
            State = ActiveGameState.GameEnded;

            //deactivate networking 

            //deactivate gateway
        }

        protected void UpdateGameEndState()
        {
            //wait for user to exit back to main menu
        }
    }
}
