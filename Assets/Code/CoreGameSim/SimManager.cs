using Networking;
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

    public UserInputGenerator m_uigInputGenerator;

    public NetworkConnection m_ntcNetworkConnection;

    public GameSettings m_gstSettings;

    protected ConstData m_conGameData;

    public int m_playerCount;

    protected GameSimulation m_simGameSim;

    protected float m_fTimeSinceLastSim;

    protected int m_iSimTick;

    public float m_fDebugScale;

    protected GameLoopState m_glsGameState;

    protected float m_fCountDownTime;

    protected float m_fNetworkTimeOfCountdownStart;

    // Use this for initialization
    public void Start ()
    {
        if(m_uigInputGenerator == null)
        {
            m_uigInputGenerator = GetComponent<UserInputGenerator>();
        }

        if (m_ntcNetworkConnection == null)
        {
            m_ntcNetworkConnection = GetComponent<NetworkConnection>();
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
	public void Update ()
    {
       

    }

    public void OnDrawGizmos()
    {
        if(m_simGameSim == null)
        {
            return;
        }

        //get the latest frame 
        FrameData frmLatestFrame = m_simGameSim.m_frmDenseFrameQueue[m_simGameSim.m_iDenseQueueHead];

        for (int i = 0; i < frmLatestFrame.PlayerCount; i++)
        {
            Vector3 drawPos = new Vector3((float)frmLatestFrame.m_v2iPosition[i].X * m_fDebugScale, 0, (float)frmLatestFrame.m_v2iPosition[i].Y * m_fDebugScale);

            Gizmos.DrawSphere(drawPos, 1);
        }      
    }

    public void AddInput()
    {
        if(m_uigInputGenerator.HasNewInputs)
        {
            m_uigInputGenerator.UpdateInputState();

            //send input to other connections 
            m_ntcNetworkConnection.TransmitPacketToAll(new InputPacket(m_uigInputGenerator.m_bCurrentInput, m_simGameSim.m_iLatestTick));

            m_simGameSim.AddInput(m_bPlayer, new InputKeyFrame() { m_iInput = m_uigInputGenerator.m_bCurrentInput, m_iTick = m_simGameSim.m_iLatestTick - m_iInputOffset });
        }

    }

    public void RefreshServerInput()
    {
        //send out stored packets 
        m_ntcNetworkConnection.DestributeReceivedPackets();
    }

    public void HandleInputFromNetwork(byte bPlayerID, Packet pktInput)
    {
        if (pktInput is InputPacket)
        {
            InputKeyFrame ikfInput = (pktInput as InputPacket).ConvertToKeyFrame();

            m_simGameSim.AddInput(bPlayerID, ikfInput);
        }
    }

    private void SwitchToCountDownState()
    {
        m_glsGameState = GameLoopState.COUNT_DOWN;

        //sort out who is playing and who has disconected

        //synchronise clocks 


    }

    private void SwitchToActiveState()
    {
        m_glsGameState = GameLoopState.ACTIVE;

        //set start time and tick
        m_fTimeSinceLastSim = 0;
        m_iSimTick = 0;

        //set player count 
        m_playerCount = m_ntcNetworkConnection.ActiveConnectionCount();

        SetupSimulation();
    }

    private void SwitchToEndState()
    {
        m_glsGameState = GameLoopState.END;
    }

    private void UpdateLobyState()
    {

    }

    private void UpdateCountDownState()
    {

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
