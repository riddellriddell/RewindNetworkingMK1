using Networking;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

    public GameSettings m_gstSettings;

    protected ConstData m_conGameData;

    public int m_playerCount;

    protected GameSimulation m_simGameSim;

    protected float m_fTimeSinceLastSim;

    protected int m_iSimTick;

    public float m_fDebugScale;

    public bool m_bShowDebug;

    public Color m_colDebugCollour;

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
        FrameData frmLatestFrame = m_simGameSim.m_frmDenseFrameQueue[m_simGameSim.m_iDenseQueueHead];

        for (int i = 0; i < frmLatestFrame.PlayerCount; i++)
        {
            Vector3 vecDrawPos = new Vector3((float)frmLatestFrame.m_v2iPosition[i].X * m_fDebugScale, 0, (float)frmLatestFrame.m_v2iPosition[i].Y * m_fDebugScale);

            Color colDefaultColor = Gizmos.color;

            Gizmos.color = m_colDebugCollour;

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

                m_simGameSim.AddInput(m_ntcNetworkConnection.m_bPlayerID, new InputKeyFrame() { m_iInput = m_uigInputGenerator.m_bCurrentInput, m_iTick = m_simGameSim.m_iLatestTick });
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

            m_simGameSim.AddInput(bPlayerID, ikfInput);
        }

        if (pktPacket is StartCountDownPacket)
        {
            StartCountDownPacket scdStartPacket = pktPacket as StartCountDownPacket;

            //get the game start time
            if (m_dtmTimeToStartGame == null || m_dtmTimeToStartGame.Ticks > scdStartPacket.m_lGameStartTime)
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

        if (DateTime.UtcNow > m_dtmTimeToStartGame)
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
        while ((m_fTimeSinceLastSim) > (float)m_simGameSim.m_setGameSettings.m_fixTickDelta)
        {
            //add debug inputs 
            AddInput();

            m_fTimeSinceLastSim -= ((float)m_simGameSim.m_setGameSettings.m_fixTickDelta);

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
        m_gstSettings.Deserialize();

        //create list of all players
        List<byte> bPlayerCharacters = new List<byte>(m_playerCount);

        //fill player character list
        for (int i = 0; i < m_playerCount; i++)
        {
            bPlayerCharacters.Add(0);
        }

        m_conGameData = new ConstData(bPlayerCharacters);

        m_simGameSim = new GameSimulation(m_gstSettings, m_conGameData);
    }
}
