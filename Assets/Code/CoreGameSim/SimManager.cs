﻿using Networking;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Sim
{
    public class SimManager : MonoBehaviour
    {
        public enum GameLoopState
        {
            LOBBY,
            COUNT_DOWN,
            ACTIVE,
            END
        }

        public float m_fCountDownTime;

        public UserInputGenerator m_uigInputGenerator;

        public NetworkConnection m_ntcNetworkConnection;

        public NetworkConnection m_ntcConnectionTarget;

        public GameSettings m_setSettings;

        //the settings to use when correcting posittion errors caused by networking
        public InterpolationErrorCorrectionSettings m_ecsErrorCorrectionSettings;

        //the class that manages the interpolation of the game state held in the sim manager
        public FrameDataInterpolator m_fdiFrameDataInterpolator = null;

        public int m_playerCount;

        protected ConstData m_conGameData;       

        protected GameSimulation m_simGameSim;

        protected float m_fTimeSinceLastSim;

        protected int m_iSimTick;

        protected float TotalGameTime
        {
            get
            {
                return (m_iSimTick * (float)m_setSettings.TickDelta.FixValue) + m_fTimeSinceLastSim;
            }
        }
        
        #region Debug
        public float m_fDebugScale;

        public bool m_bShowDebug;

        public Color m_colSimDebugTint;

        public Color m_colMovingState = Color.green;

        public Color m_colStandingState = Color.cyan;

        public Color m_colQuickAttack = Color.yellow;

        public Color m_colSlowAttack = Color.red;

        public Color m_colBlocking = Color.blue;

        public Color m_colDead = Color.white;
        #endregion

        protected GameLoopState m_glsGameState;

        protected float m_fNetworkTimeOfCountdownStart;

        protected DateTime m_dtmTimeToStartGame;

        // Use this for initialization
        public void Start()
        {
            if (m_uigInputGenerator == null)
            {
                m_uigInputGenerator = GetComponent<UserInputGenerator>();
            }

            if (m_ntcNetworkConnection == null)
            {
                m_ntcNetworkConnection = GetComponent<NetworkConnection>();
            }

            if (m_ntcConnectionTarget != null)
            {
                m_ntcNetworkConnection.MakeTestingConnection(m_ntcConnectionTarget);
            }

            m_ntcNetworkConnection.m_evtPacketDataIn += HandleInputFromNetwork;

            m_glsGameState = GameLoopState.LOBBY;
        }

        public void Destroy()
        {
            if (m_ntcNetworkConnection != null)
            {
                m_ntcNetworkConnection.m_evtPacketDataIn -= HandleInputFromNetwork;
            }
        }

        // Update is called once per frame
        public void Update()
        {
            //destribute inputs
            m_ntcNetworkConnection.DestributeReceivedPackets();

            switch (m_glsGameState)
            {
                case GameLoopState.LOBBY:

                    UpdateLobyState();

                    break;

                case GameLoopState.COUNT_DOWN:

                    UpdateCountDownState();

                    break;

                case GameLoopState.ACTIVE:

                    UpdateActiveState();

                    break;

                case GameLoopState.END:

                    UpdateEndState();

                    break;
            }
        }

        public void OnDrawGizmos()
        {
            if (m_simGameSim == null || m_bShowDebug == false)
            {
                return;
            }

            //get the latest frame 
            //FrameData frmLatestFrame = m_simGameSim.m_frmDenseFrameQueue[m_simGameSim.m_iDenseQueueHead];

            //update the interpolated data 
            m_fdiFrameDataInterpolator.UpdateInterpolatedDataForTime(TotalGameTime - (float)m_setSettings.TickDelta.FixValue, Time.deltaTime);

            InterpolatedFrameDataGen frmLatestFrame = m_fdiFrameDataInterpolator.m_ifdInterpolatedFrameData;


            for (int i = 0; i < frmLatestFrame.PlayerCount; i++)
            {
                Vector3 vecDrawPos = new Vector3((float)frmLatestFrame.m_v2iPositionErrorAdjusted[i].x * m_fDebugScale, 0, (float)frmLatestFrame.m_v2iPositionErrorAdjusted[i].y * m_fDebugScale);

                Color colDefaultColor = Gizmos.color;

                Color colPlayerColour = Color.white;

                switch((FrameData.State)frmLatestFrame.m_bPlayerState[i])
                {
                    case FrameData.State.Dead:
                        colPlayerColour = m_colDead;
                        break;
                    case FrameData.State.FastAttack:
                        colPlayerColour = m_colQuickAttack;
                            break;
                    case FrameData.State.SlowAttack:
                        colPlayerColour = m_colSlowAttack;
                        break;
                    case FrameData.State.Moving:
                        colPlayerColour = m_colMovingState;
                        break;
                    case FrameData.State.Standing:
                        colPlayerColour = m_colStandingState;
                        break;
                    case FrameData.State.Blocking:
                        colPlayerColour = m_colBlocking;
                        break;
                }

                colPlayerColour *= m_colSimDebugTint;
                
                Gizmos.color = colPlayerColour;

                Gizmos.DrawSphere(vecDrawPos, 1);

                Gizmos.color = colDefaultColor;

            }
        }

        public void AddInput()
        {
            if (m_uigInputGenerator == null)
            {
                return;
            }

            //check if in active game state 
            if (m_glsGameState == GameLoopState.ACTIVE)
            {
                if (m_uigInputGenerator.HasNewInputs)
                {
                    m_uigInputGenerator.UpdateInputState();

                    //send input to other connections 
                    m_ntcNetworkConnection.TransmitPacketToAll(new InputPacket(m_uigInputGenerator.m_bCurrentInput, m_simGameSim.m_iLatestTick));

                    AddInputToSim(m_ntcNetworkConnection.m_bPlayerID, new InputKeyFrame() { m_iInput = m_uigInputGenerator.m_bCurrentInput, m_iTick = m_simGameSim.m_iLatestTick });
                }
            }

        }

        public void RefreshServerInput()
        {
            //send out stored packets 
            m_ntcNetworkConnection.DestributeReceivedPackets();
        }

        public void HandleInputFromNetwork(byte bPlayerID, Packet pktPacket)
        {
            if (pktPacket is InputPacket)
            {
                InputKeyFrame ikfInput = (pktPacket as InputPacket).ConvertToKeyFrame();

                AddInputToSim(bPlayerID, ikfInput);
            }

            if (pktPacket is StartCountDownPacket)
            {
                StartCountDownPacket scdStartPacket = pktPacket as StartCountDownPacket;

                //get the game start time
                if (m_glsGameState == GameLoopState.LOBBY || m_dtmTimeToStartGame == null || m_dtmTimeToStartGame.Ticks > scdStartPacket.m_lGameStartTime)
                {
                    //set the game start time to the closest time
                    m_dtmTimeToStartGame = scdStartPacket.GameStartTime;
                }

                //check if in the lobby state
                if (m_glsGameState == GameLoopState.LOBBY)
                {
                    //switch to the count down state 
                    SwitchToCountDownState();
                }
            }
        }

        private void SwitchToCountDownState()
        {
            m_glsGameState = GameLoopState.COUNT_DOWN;

            //sort out who is playing and who has disconected

            //synchronise clocks 

            //set game start time 

        }

        private void SwitchToActiveState()
        {
            m_glsGameState = GameLoopState.ACTIVE;

            //set start time and tick
            m_fTimeSinceLastSim = 0;
            m_iSimTick = 0;

            //set player count 
            m_playerCount = m_ntcNetworkConnection.ActiveConnectionCount() + 1;

            SetupSimulation();

            //reset all ticks
            m_ntcNetworkConnection.TransmitPacketToAll(new ResetTickCountPacket());
        }

        private void SwitchToEndState()
        {
            m_glsGameState = GameLoopState.END;
        }

        private void UpdateLobyState()
        {
            m_ntcNetworkConnection.UpdateConnections(0);

            //check if the player wants to start the game 
            if (m_uigInputGenerator?.m_bStartGame ?? false)
            {
                //calculate game start time 
                m_dtmTimeToStartGame = DateTime.UtcNow + TimeSpan.FromSeconds(m_fCountDownTime);

                //send out message to other players to start game
                m_ntcNetworkConnection.TransmitPacketToAll(new StartCountDownPacket(m_dtmTimeToStartGame.Ticks));

                //change game to countdown
                SwitchToCountDownState();

                return;
            }
        }

        private void UpdateCountDownState()
        {

            if (DateTime.UtcNow.Ticks > m_dtmTimeToStartGame.Ticks)
            {
                SwitchToActiveState();
            }

            m_ntcNetworkConnection.UpdateConnections(0);
        }

        private void UpdateActiveState()
        {
            //update time since last frame
            m_fTimeSinceLastSim += Time.deltaTime;

            //check if it is time to update the simulaiton 
            while ((m_fTimeSinceLastSim) > (float)m_simGameSim.m_setGameSettings.TickDelta.m_fValue)
            {
                //add debug inputs 
                AddInput();

                m_fTimeSinceLastSim -= ((float)m_simGameSim.m_setGameSettings.TickDelta.m_fValue);

                m_iSimTick++;

                //update the networking 
                m_ntcNetworkConnection.UpdateConnections(m_iSimTick);

                //do one frame of simulation
                m_simGameSim.UpdateSimulation(m_iSimTick);

            }
        }

        private void UpdateEndState()
        {
            m_ntcNetworkConnection.UpdateConnections(0);
        }

        private void SetupSimulation()
        {
            //deserialize key data 
            m_setSettings.Deserialize();

            //create list of all players
            List<byte> bPlayerCharacters = new List<byte>(m_playerCount);

            //fill player character list
            for (int i = 0; i < m_playerCount; i++)
            {
                bPlayerCharacters.Add(0);
            }

            m_conGameData = new ConstData(bPlayerCharacters);

            m_simGameSim = new GameSimulation(m_setSettings, m_conGameData);

            m_simGameSim.m_iDebugNetConnectionID = m_ntcNetworkConnection.m_bPlayerID;

            m_simGameSim.m_bEnableDebugHashChecks = m_setSettings.RunHashChecks;

            //setup data interpolator 
            m_fdiFrameDataInterpolator = new FrameDataInterpolator(m_simGameSim,m_ecsErrorCorrectionSettings);
        }

        private void AddInputToSim(byte bPlayerID, InputKeyFrame ikfInput)
        {
            //add input into the sim
            m_simGameSim.AddInput(bPlayerID, ikfInput);

            //check to see if the input might cause a simulation recalculation 
            if(m_simGameSim.m_iLastResolvedTick != m_simGameSim.m_iLatestTick)
            {
                m_fdiFrameDataInterpolator.m_bCalculatePredictionError = true;
            }
        }
    }
}
