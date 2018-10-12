using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using FixedPointy;

public class GameSimulation
{
    public GameSettings m_setGameSettings;

    public ConstData m_conConstantGameData;

    //the most recent tick
    public int m_iLatestTick
    {
        get
        {

            return m_frmDenseFrameQueue[m_iDenseQueueHead].m_iTickNumber;
        }
    }

    //the last tick that has not been invalidated by new inputs 
    public int m_iLastResolvedTick;
       
    //the head of the queue 
    public int m_iDenseQueueHead;

    //the queue of frames
    public List<FrameData> m_frmDenseFrameQueue;
      
    //a history of the game in snapshots every x seconds 
    //public List<FrameData> m_fmrKeyFrameHistory;

    //a buffer of all the inputs for all the players 
    public List<InputBuffer> m_ipbPlayerInputs;

    //a list of all the inputs for the current tick
    protected List<byte> m_bInputsForTick;
 
    public GameSimulation(GameSettings setSttings, ConstData conConstantData)
    {
        m_setGameSettings = setSttings;
        m_conConstantGameData = conConstantData;

        //calculate queue length
        int iQueueLength =(int)FixMath.Ceiling( m_setGameSettings.m_fixTargetQueueLength / m_setGameSettings.m_fixTickDelta);

        //fill queue
        m_frmDenseFrameQueue = new List<FrameData>(iQueueLength);

        for(int i = 0; i < iQueueLength; i++)
        {
            m_frmDenseFrameQueue.Add( new FrameData(m_conConstantGameData.PlayerCount));
        }

        m_bInputsForTick = new List<byte>(m_conConstantGameData.PlayerCount);
        m_ipbPlayerInputs = new List<InputBuffer>(m_conConstantGameData.PlayerCount);

        for(int i = 0; i < m_conConstantGameData.PlayerCount; i++)
        {
            m_bInputsForTick.Add(0);
            m_ipbPlayerInputs.Add(new InputBuffer(iQueueLength));
            
        }
               
    }

    public void AddInput(byte bPlayer,InputKeyFrame ikfInput)
    {
        //get target input buffer
        InputBuffer ipbBuffer = m_ipbPlayerInputs[bPlayer];

        //add input to buffer
        ipbBuffer.AddKeyframeToEndOfInputBuffer(ikfInput);

        m_iLastResolvedTick = Mathf.Min(m_iLastResolvedTick, ikfInput.m_iTick);
    }

    public void AddInput(byte bPlayer,InputKeyFrame[] input)
    {
        //get target input buffer
        InputBuffer ipbBuffer = m_ipbPlayerInputs[bPlayer];

        //add the inputs and check if any ticks need to be recalculated 
        m_iLastResolvedTick = Mathf.Min( ipbBuffer.AddKeyFrames(input), m_iLastResolvedTick);

    }

    public void UpdateSimulation(int iTargetTick)
    {
        //check if last resolved tick is older than latest tick
        if(iTargetTick <= m_iLastResolvedTick)
        {
            //up to date 
            return;
        }

        int iFamesToCalculate = iTargetTick - m_iLastResolvedTick;

        //get last resolved tick frame
        int iStartFrameOffset = m_frmDenseFrameQueue[m_iDenseQueueHead].m_iTickNumber - m_iLastResolvedTick;

        //get spot to look up in buffer 
        int iStartIndex = HelperFunctions.mod((m_iDenseQueueHead - iStartFrameOffset) , m_frmDenseFrameQueue.Count);

        //get the frame data 
        FrameData frmFrameToSimulate = m_frmDenseFrameQueue[iStartIndex];

        int iOutputIndex = iStartIndex;
        FrameData frmFrameOutput;

        for(int i = 0; i < iFamesToCalculate; i++)
        {
            //get output frame 
            iOutputIndex = HelperFunctions.mod((iStartIndex + i) + 1 , m_frmDenseFrameQueue.Count);
            frmFrameOutput = m_frmDenseFrameQueue[iOutputIndex];

            //simulate frame
            SimulateFrame(m_setGameSettings, m_conConstantGameData, m_ipbPlayerInputs, m_bInputsForTick, frmFrameToSimulate, frmFrameOutput);
            
            //shift simulation forwards 
            frmFrameToSimulate = frmFrameOutput;          

        }

        //set head of simulation
        m_iDenseQueueHead = iOutputIndex;

        m_iLastResolvedTick = iTargetTick;

    }

    //simulate a single frame 
    public bool SimulateFrame(GameSettings setSettings, ConstData conConstantGameData, List<InputBuffer> ipbPlayerInputs, List<byte> bInputsForTick, FrameData frmFrameToSimulate, FrameData frmSimulatedFrame)
    {
        //was simulation successfull
        bool bSuccess = false;

        //get inputs for frame
        GetInputsForFrame(ipbPlayerInputs, bInputsForTick, frmFrameToSimulate.m_iTickNumber);

         //check for death

         //update cool downs

         //update all state changes  

         //perform all moves
         bSuccess = PerformMoveProcess(setSettings, conConstantGameData, bInputsForTick, frmFrameToSimulate, frmSimulatedFrame, setSettings.m_fixTickDelta);

        //perform all attacks 

        //increment frame tick
        frmSimulatedFrame.m_iTickNumber = frmFrameToSimulate.m_iTickNumber + 1;

        return true;
    }

    public void InitaliseDenseQueue(int tickDelta, int bufferTimeSpan, byte playerCount)
    {
        m_iDenseQueueHead = 0;

        int numberOfFrames = bufferTimeSpan / tickDelta;

        m_frmDenseFrameQueue = new List<FrameData>(numberOfFrames);

        for (int i = 0; i < m_frmDenseFrameQueue.Count; i++)
        {
            m_frmDenseFrameQueue[i] = new FrameData(playerCount);
        }
    }

    public void GetInputsForFrame(List<InputBuffer> ipbPlayerInputBuffer, List<byte> bOutInputs, int iTick)
    {
        for(int i = 0; i < ipbPlayerInputBuffer.Count; i++)
        {
            int iIndex = 0;

            ipbPlayerInputBuffer[i].TryGetIndexOfInputForTick(iTick, out iIndex);

            bOutInputs[i] = ipbPlayerInputBuffer[i].m_ikfInputBuffer[iIndex].m_iInput;
        }
    }

    public bool PerformMoveProcess(GameSettings setSettings,ConstData conConstantGameData, List<byte> bInputsForTick, FrameData frmCurrentFrame, FrameData frmUpdatedFrame, Fix iTick)
    {
        //get refferences to the arrays that we will be processing
        List<byte> inPlayerStates = frmCurrentFrame.m_bPlayerState;
        List<byte> outPlayerStates = frmUpdatedFrame.m_bPlayerState;

        List<FixVec2> inPlayerPos = frmCurrentFrame.m_v2iPosition;
        List<FixVec2> outPlayerPos = frmUpdatedFrame.m_v2iPosition;

        List<byte> inMoveDirection = frmCurrentFrame.m_bFaceDirection;
        List<byte> outMoveDirection = frmUpdatedFrame.m_bFaceDirection;
           

        //loop through all the players 
        for (int i = 0; i < frmCurrentFrame.PlayerCount; i++)
        {
            //check if player is in a state to move
            if (inPlayerStates[i] == (byte)FrameData.State.Standing || inPlayerStates[i] == (byte)FrameData.State.Moving)
            {
                //get movement direction from input
                FixVec2 v2iMoveDirection = FixVec2.Zero;
                outMoveDirection[i] = (byte)FrameData.Direction.None;

                if ((bInputsForTick[i] & (byte)InputKeyFrame.Input.Up) > 0)
                {
                    v2iMoveDirection = new FixVec2(v2iMoveDirection.X, 1);
                    outMoveDirection[i] = (byte)FrameData.Direction.None; ;
                }
                else if ((bInputsForTick[i] & (byte)InputKeyFrame.Input.Down) > 0)
                {
                    v2iMoveDirection = new FixVec2(v2iMoveDirection.X, - 1);
                    outMoveDirection[i] = (byte)FrameData.Direction.Down;
                }

                if ((bInputsForTick[i] & (byte)InputKeyFrame.Input.Left) > 0)
                {
                    v2iMoveDirection = new FixVec2(-1, v2iMoveDirection.Y);
                    outMoveDirection[i] = (byte)(outMoveDirection[i] | (byte)FrameData.Direction.Left);
                }
                else if ((bInputsForTick[i] & (byte)InputKeyFrame.Input.Right) > 0)
                {
                    v2iMoveDirection = new FixVec2( 1, v2iMoveDirection.Y);
                    outMoveDirection[i] = (byte)(outMoveDirection[i] | (byte)FrameData.Direction.Right);
                }
                
                if (v2iMoveDirection.GetMagnitude() == 0)
                {
                    //if there is no movment command 
                    outMoveDirection[i] = inMoveDirection[i];
                    outPlayerStates[i] = (byte)FrameData.State.Standing;
                    outPlayerPos[i] = inPlayerPos[i];

                    return true;
                }

                //apply movement speed
                v2iMoveDirection = v2iMoveDirection * (setSettings.m_fixMoveSpeed * iTick);

                //apply movement to position
                outPlayerPos[i] = inPlayerPos[i] + v2iMoveDirection;

                //set move state 
                outPlayerStates[i] = (byte)FrameData.State.Moving;
            }
        }

        return true;
    }
}
