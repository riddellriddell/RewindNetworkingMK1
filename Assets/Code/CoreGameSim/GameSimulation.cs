using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using FixedPointy;
using System;
using System.Security.Cryptography;

namespace Sim
{
    public class GameSimulation
    {
        public int m_iDebugNetConnectionID;

        public GameSettings m_setGameSettings;

        public ConstData m_conConstantGameData;

        public bool m_bEnableDebugHashChecks = true;

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
            int iQueueLength = (int)FixMath.Ceiling(m_setGameSettings.TargetQueueSize.m_fValue / m_setGameSettings.TickDelta.m_fValue);

            //fill queue
            m_frmDenseFrameQueue = new List<FrameData>(iQueueLength);

            for (int i = 0; i < iQueueLength; i++)
            {
                m_frmDenseFrameQueue.Add(new FrameData(m_conConstantGameData.PlayerCount));
            }

            m_bInputsForTick = new List<byte>(m_conConstantGameData.PlayerCount);
            m_ipbPlayerInputs = new List<InputBuffer>(m_conConstantGameData.PlayerCount);

            for (int i = 0; i < m_conConstantGameData.PlayerCount; i++)
            {
                m_bInputsForTick.Add(0);
                m_ipbPlayerInputs.Add(new InputBuffer(iQueueLength));

            }

            //setup inital frame
            SetupInitalFrameData(m_frmDenseFrameQueue[GetFrameDataIndex(m_iLastResolvedTick)], m_setGameSettings);
        }

        public void SetupInitalFrameData(FrameData frmFirstFrame, GameSettings setGameSettings)
        {
            //set the inital health of all players
            for(int i = 0; i < frmFirstFrame.m_sPlayerHealths.Count; i++)
            {
                frmFirstFrame.m_sPlayerHealths[i] = setGameSettings.PlayerHealth;
            }

            //set the inital state for all players
            for(int i = 0; i < frmFirstFrame.m_bPlayerState.Count; i++)
            {
                frmFirstFrame.m_bPlayerState[i] = (byte)FrameData.State.Standing;
            }

        }

        public void AddInput(byte bPlayer, InputKeyFrame ikfInput)
        {
            //get target input buffer
            InputBuffer ipbBuffer = m_ipbPlayerInputs[bPlayer];

            //add input to buffer
            ipbBuffer.AddKeyframeToEndOfInputBuffer(ikfInput);

            m_iLastResolvedTick = Mathf.Min(m_iLastResolvedTick, ikfInput.m_iTick);
        }

        public void AddInput(byte bPlayer, InputKeyFrame[] input)
        {
            //get target input buffer
            InputBuffer ipbBuffer = m_ipbPlayerInputs[bPlayer];

            //add the inputs and check if any ticks need to be recalculated 
            m_iLastResolvedTick = Mathf.Min(ipbBuffer.AddKeyFrames(input), m_iLastResolvedTick);

        }

        public FrameData GetFrameData(int iTick)
        {
            return m_frmDenseFrameQueue[GetFrameDataIndex(iTick)];
        }

        public int GetFrameDataIndex(int iTick)
        {
            //get last resolved tick frame
            int iOffsetFromLatest = m_iLatestTick - iTick;

            //check if lookup is overrunning the dense queue 
            if (iOffsetFromLatest >= m_frmDenseFrameQueue.Count - 1)
            {
                Debug.LogError("Frame Request Over flowing buffer by " + (iOffsetFromLatest - m_frmDenseFrameQueue.Count) + " buffer size is " + m_frmDenseFrameQueue.Count);
            }

            //get the lookup index in the buffer 
            return HelperFunctions.mod((m_iDenseQueueHead - iOffsetFromLatest), m_frmDenseFrameQueue.Count);
        }

        public void UpdateSimulation(int iTargetTick)
        {
            //check if last resolved tick is older than latest tick
            if (iTargetTick <= m_iLastResolvedTick)
            {
                //up to date 
                return;
            }

            int iFamesToCalculate = iTargetTick - m_iLastResolvedTick;

            //get last resolved tick frame
            int iStartFrameOffset = m_iLatestTick - m_iLastResolvedTick;

            //get spot to look up in buffer 
            int iStartIndex = GetFrameDataIndex(m_iLastResolvedTick);

            //get the frame data 
            FrameData frmFrameToSimulate = m_frmDenseFrameQueue[iStartIndex];

            int iOutputIndex = iStartIndex;
            FrameData frmFrameOutput;

            for (int i = 0; i < iFamesToCalculate; i++)
            {
                //get output frame thats 1 ahead of the current frame / start index 
                iOutputIndex = GetFrameDataIndex(m_iLastResolvedTick + 1);

                //get target frame output 
                frmFrameOutput = m_frmDenseFrameQueue[iOutputIndex];

                //simulate frame
                SimulateFrame(m_setGameSettings, m_conConstantGameData, m_ipbPlayerInputs, m_bInputsForTick, frmFrameToSimulate, frmFrameOutput);

                //shift simulation forwards 
                frmFrameToSimulate = frmFrameOutput;

                //set head of simulation
                m_iDenseQueueHead = iOutputIndex;
                m_iLastResolvedTick = frmFrameToSimulate.m_iTickNumber;
            }
        }

        //simulate a single frame 
        public bool SimulateFrame(GameSettings setSettings, ConstData conConstantGameData, List<InputBuffer> ipbPlayerInputs, List<byte> bInputsForTick, FrameData frmFrameToSimulate, FrameData frmSimulatedFrame)
        {
            //check if running hash debugs
            if (m_bEnableDebugHashChecks)
            {
                //clear frame data 
                frmSimulatedFrame.ResetData();
            }

            //was simulation successfull
            bool bSuccess = false;

            //blit across key frame data 
            TransferHealthValues(frmFrameToSimulate, frmSimulatedFrame);

            TransferStateValues(frmFrameToSimulate, frmSimulatedFrame);

            UpdateStateTick(frmFrameToSimulate, frmSimulatedFrame);

            //do hash checks 
            if (m_bEnableDebugHashChecks)
            {
                if (DataHashValidation.LogDataHash(GetHashForInputs(frmFrameToSimulate.m_iTickNumber),0, frmFrameToSimulate.m_iTickNumber, GetHashForFrame(frmFrameToSimulate.m_iTickNumber + 1), "Initial Value Copy") == false)
                {
                    Debug.LogError("update base values Failed Hash Check");
                }
            }


            //get inputs for frame
            GetInputsForFrame(ipbPlayerInputs, bInputsForTick, frmFrameToSimulate.m_iTickNumber);

            //check for death

            //update cool downs

            //update all state changes  

            //perform all moves
            bSuccess = PerformMoveProcess(setSettings, conConstantGameData, bInputsForTick, frmFrameToSimulate, frmSimulatedFrame);

            //do hash checks 
            if (m_bEnableDebugHashChecks)
            {
                if (DataHashValidation.LogDataHash(GetHashForInputs(frmFrameToSimulate.m_iTickNumber),1, frmFrameToSimulate.m_iTickNumber, GetHashForFrame(frmFrameToSimulate.m_iTickNumber + 1), "") == false)
                {
                    Debug.LogError("Move Command Failed Hash Check");
                }
            }

            //perform collision resolution 
            PerformCollisionDetection(setSettings, frmFrameToSimulate, frmSimulatedFrame);

            //do hash checks 
            if (m_bEnableDebugHashChecks)
            {
                if (DataHashValidation.LogDataHash(GetHashForInputs(frmFrameToSimulate.m_iTickNumber), 2, frmFrameToSimulate.m_iTickNumber, GetHashForFrame(frmFrameToSimulate.m_iTickNumber + 1), "") == false)
                {
                    Debug.LogError("Collision Resolution Failed Hash Check");
                }
            }

            //perform all attacks 
            PerformAttackActions(setSettings, conConstantGameData, bInputsForTick, frmFrameToSimulate, frmSimulatedFrame);

            //do hash checks 
            if (m_bEnableDebugHashChecks)
            {
                if (DataHashValidation.LogDataHash(GetHashForInputs(frmFrameToSimulate.m_iTickNumber),3, frmFrameToSimulate.m_iTickNumber, GetHashForFrame(frmFrameToSimulate.m_iTickNumber + 1), "") == false)
                {
                    Debug.LogError("Attack calculation Failed Hash Check");
                }
            }

            //check for death
            CheckForDeath(setSettings, frmSimulatedFrame);

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
            for (int i = 0; i < ipbPlayerInputBuffer.Count; i++)
            {
                int iIndex = 0;

                ipbPlayerInputBuffer[i].TryGetIndexOfInputForTick(iTick, out iIndex);

                bOutInputs[i] = ipbPlayerInputBuffer[i].m_ikfInputBuffer[iIndex].m_iInput;
            }
        }

        public bool PerformMoveProcess(GameSettings setSettings, ConstData conConstantGameData, List<byte> bInputsForTick, FrameData frmCurrentFrame, FrameData frmUpdatedFrame)
        {
            //get the tick delta
            Fix fTick = setSettings.TickDelta.m_fValue;

            //get refferences to the arrays that we will be processing
            List<byte> inPlayerStates = frmCurrentFrame.m_bPlayerState;
            List<byte> outPlayerStates = frmUpdatedFrame.m_bPlayerState;

            List<FixVec2> inPlayerPos = frmCurrentFrame.m_v2iPosition;
            List<FixVec2> outPlayerPos = frmUpdatedFrame.m_v2iPosition;

            List<byte> inMoveDirection = frmCurrentFrame.m_bFaceDirection;
            List<byte> outMoveDirection = frmUpdatedFrame.m_bFaceDirection;

            List<short> sCollisions = new List<short>();


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
                        v2iMoveDirection = new FixVec2(v2iMoveDirection.X, -1);
                        outMoveDirection[i] = (byte)FrameData.Direction.Down;
                    }

                    if ((bInputsForTick[i] & (byte)InputKeyFrame.Input.Left) > 0)
                    {
                        v2iMoveDirection = new FixVec2(-1, v2iMoveDirection.Y);
                        outMoveDirection[i] = (byte)(outMoveDirection[i] | (byte)FrameData.Direction.Left);
                    }
                    else if ((bInputsForTick[i] & (byte)InputKeyFrame.Input.Right) > 0)
                    {
                        v2iMoveDirection = new FixVec2(1, v2iMoveDirection.Y);
                        outMoveDirection[i] = (byte)(outMoveDirection[i] | (byte)FrameData.Direction.Right);
                    }

                    if (v2iMoveDirection.GetMagnitude() == 0)
                    {
                        //if there is no movment command 
                        outMoveDirection[i] = inMoveDirection[i];
                        outPlayerStates[i] = (byte)FrameData.State.Standing;
                        outPlayerPos[i] = inPlayerPos[i];

                    }
                    else
                    {
                        //apply movement speed
                        v2iMoveDirection = v2iMoveDirection * (setSettings.MovementSpeed.m_fValue * fTick);

                        //apply movement to position
                        outPlayerPos[i] = inPlayerPos[i] + v2iMoveDirection;

                        //set move state 
                        outPlayerStates[i] = (byte)FrameData.State.Moving;
                    }
                }
                else
                {
                    outPlayerPos[i] = inPlayerPos[i];
                    outMoveDirection[i] = inMoveDirection[i];
                }

            }

            return true;
        }

        public bool PerformCollisionDetection(GameSettings setSettings, FrameData frmCurrentFrame, FrameData frmUpdatedFrame)
        {
            List<FixVec2> outPlayerPos = frmUpdatedFrame.m_v2iPosition;

            List<FixVec2> inPlayerPos = frmCurrentFrame.m_v2iPosition;

            List<short> sCollisions = new List<short>();

            //loop through all the players
            for (int i = 0; i < outPlayerPos.Count; i++)
            {              

                //check for collisions with other players 
                if (GetPlayersOverlappingCircle(setSettings, frmCurrentFrame, outPlayerPos[i], setSettings.ChararcterSize.m_fValue, i, sCollisions))
                {
                    FixVec2 vecOutPos = FixVec2.Zero;

                    Fix fixAverageScale = Fix.One / sCollisions.Count;

                    Fix fixCollisionExitDistance = setSettings.ChararcterSize.m_fValue * 2;

                    //loop through all collisions and attempt to resolve them 
                    for (int j = 0; j < sCollisions.Count; j++)
                    {
                        //look up hit player position 
                        FixVec2 vecHit = inPlayerPos[sCollisions[j]];

                        //get direction to player
                        FixVec2 vecExitDir = outPlayerPos[i] - vecHit;

                        //calcualte collision exit pos
                        vecOutPos += (vecHit + vecExitDir.Normalize() * fixCollisionExitDistance) * fixAverageScale;
                    }

                    outPlayerPos[i] = vecOutPos;

                    //remove any collisions detected 
                    sCollisions.Clear();
                }
            }

            return true;
        }

        public bool TransferHealthValues(FrameData frmCurrentFrame, FrameData frmUpdatedFrame)
        {
            for (int i = 0; i < frmCurrentFrame.m_sPlayerHealths.Count; i++)
            {
                //blit across health
                frmUpdatedFrame.m_sPlayerHealths[i] = frmCurrentFrame.m_sPlayerHealths[i];
            }

            return true;
        }

        public bool TransferStateValues(FrameData frmCurrentFrame, FrameData frmUpdatedFrame)
        {
            for (int i = 0; i < frmCurrentFrame.m_bPlayerState.Count; i++)
            {
                //blit across player state
                frmUpdatedFrame.m_bPlayerState[i] = frmCurrentFrame.m_bPlayerState[i];
            }

            return true;
        }

        public bool UpdateStateTick(FrameData frmCurrentFrame, FrameData frmUpdatedFrame)
        {
            for (int i = 0; i < frmCurrentFrame.m_bPlayerState.Count; i++)
            {
                //update tick for state
                frmUpdatedFrame.m_sStateEventTick[i] = (short)(frmCurrentFrame.m_sStateEventTick[i] + 1);
            }

            return true;
        }

        public bool PerformAttackActions(GameSettings setSettings, ConstData conConstantGameData, List<byte> bInputsForTick, FrameData frmCurrentFrame, FrameData frmUpdatedFrame)
        {
            //get refferences to the arrays that we will be processing
            List<byte> inPlayerStates = frmCurrentFrame.m_bPlayerState;
            List<byte> outPlayerStates = frmUpdatedFrame.m_bPlayerState;

            List<FixVec2> inPlayerPos = frmCurrentFrame.m_v2iPosition;
            List<FixVec2> outPlayerPos = frmUpdatedFrame.m_v2iPosition;

            List<byte> inMoveDirection = frmCurrentFrame.m_bFaceDirection;
            List<byte> outMoveDirection = frmUpdatedFrame.m_bFaceDirection;

            List<short> sCollisions = new List<short>();

            Fix fixFrameDelta = setSettings.TickDelta.m_fValue;

            Fix fixQuickAttackWarmUp = setSettings.QuickAttackWarmUp.m_fValue;

            Fix fixQuickAttackCoolDown = setSettings.SlowAttackCoolDown.m_fValue;

            Fix fixQuickAttackRange = setSettings.QuickAttackRange.m_fValue;

            Fix fixQuickAttackAOE = setSettings.QuickAttackAOE.m_fValue;

            short sQuickAttackDamage = setSettings.QuickAttackDamage;

            Fix fixSlowAttackWarmUp = setSettings.SlowAttackWarmUp.m_fValue;

            Fix fixSlowAttackCoolDown = setSettings.SlowAttackWarmUp.m_fValue;

            Fix fixSlowAttackRange = setSettings.SlowAttackRange.m_fValue;

            Fix fixSlowAttackAOE = setSettings.SlowAttackAOE.m_fValue;

            short sSlowAttackDamage = setSettings.SlowAttackDammage;

            byte bFastAttackState = (byte)FrameData.State.FastAttack;

            byte bSlowAttackState = (byte)FrameData.State.SlowAttack;

            byte bBlockingState = (byte)FrameData.State.Blocking;

            byte bStandingState = (byte)FrameData.State.Standing;

            byte bMovingState = (byte)FrameData.State.Moving;

            byte bQuickAttackInput = (byte)InputKeyFrame.Input.QuickAttack;

            byte bSlowAttackInput = (byte)InputKeyFrame.Input.SlowAttack;

            int iStateTick = frmCurrentFrame.m_iTickNumber;

            //loop through all the players 
            for (int i = 0; i < frmCurrentFrame.PlayerCount; i++)
            {
                //get player state 
                byte bPlayerState = frmUpdatedFrame.m_bPlayerState[i];

                //check if player is in a state that can attack
                if (bPlayerState == bStandingState || bPlayerState == bMovingState)
                {
                    //check if player wants to attack
                    if ((bInputsForTick[i] & bQuickAttackInput) > 0)
                    {
                        frmUpdatedFrame.m_bPlayerState[i] = bFastAttackState;

                        frmUpdatedFrame.m_sStateEventTick[i] = 0;
                    }
                    else if ((bInputsForTick[i] & bSlowAttackInput) > 0)
                    {
                        frmUpdatedFrame.m_bPlayerState[i] = bSlowAttackState;

                        frmUpdatedFrame.m_sStateEventTick[i] = 0;
                    }
                }

                //check if the player is attacking
                if (bPlayerState == bFastAttackState || bPlayerState == bSlowAttackState)
                {
                    //get the time spent in this state 
                    Fix fixStateTime = frmUpdatedFrame.m_sStateEventTick[i] * fixFrameDelta;

                    //get warm up time 
                    Fix fixWarmUpTime = (bPlayerState == bSlowAttackState) ? fixSlowAttackWarmUp : fixQuickAttackWarmUp;

                    //check if player is performing attack this tick
                    if (fixStateTime > fixWarmUpTime && (fixStateTime - fixFrameDelta) < fixWarmUpTime)
                    {
                        Fix fixAttackAOE = (bPlayerState == bSlowAttackState) ? fixSlowAttackAOE : fixQuickAttackRange;

                        //get attack range 
                        Fix fAttackRange = (bPlayerState == bSlowAttackState) ? fixSlowAttackRange : fixQuickAttackRange;

                        //calculate location of attack
                        FixVec2 vecAttackLocation = frmCurrentFrame.m_v2iPosition[i] + (ConvertDirectionToNormal(frmCurrentFrame.m_bFaceDirection[i]) * fAttackRange);

                        //clear out existing collision data 
                        sCollisions.Clear();

                        //check if the player hit anything 
                        if (GetPlayersOverlappingCircle(setSettings, frmCurrentFrame, vecAttackLocation, fixAttackAOE, i, sCollisions))
                        {
                            //loop through all targets hit
                            for (int j = 0; j < sCollisions.Count; j++)
                            {
                                int iCollisionIndex = sCollisions[j];

                                //get direction from attacker to player 
                                FixVec2 vecReverseAttackDirection = frmCurrentFrame.m_v2iPosition[i] - frmCurrentFrame.m_v2iPosition[iCollisionIndex];

                                byte bDirectionOfAttack = ConvertNormalToDirection(vecReverseAttackDirection.Normalize());

                                //check if attack was blocked 
                                int iAttackAlignment = CompareDirections(bDirectionOfAttack, frmCurrentFrame.m_bFaceDirection[iCollisionIndex]);

                                short sDamage = (bPlayerState == bSlowAttackState) ? sSlowAttackDamage : sQuickAttackDamage;

                                //check if target was blocking 
                                if (bPlayerState == bFastAttackState && frmCurrentFrame.m_bPlayerState[iCollisionIndex] == bBlockingState)
                                {
                                    //check if stabbed in the back
                                    if (iAttackAlignment == 4)
                                    {
                                        frmUpdatedFrame.m_sPlayerHealths[iCollisionIndex] = 0;
                                    }
                                    else if (iAttackAlignment > 1) //check if blocked by shield 
                                    {
                                        //apply damage 
                                        frmUpdatedFrame.m_sPlayerHealths[iCollisionIndex] = (short)(frmUpdatedFrame.m_sPlayerHealths[iCollisionIndex] - sDamage);
                                    }
                                }
                                else
                                {
                                    //check if stabbed in the back
                                    if (iAttackAlignment == 4)
                                    {
                                        frmUpdatedFrame.m_sPlayerHealths[iCollisionIndex] = 0;
                                    }
                                    else
                                    {
                                        //apply damage 
                                        frmUpdatedFrame.m_sPlayerHealths[iCollisionIndex] = (short)(frmUpdatedFrame.m_sPlayerHealths[iCollisionIndex] - sDamage);
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        Fix fixCoolDownTime = (bPlayerState == bSlowAttackState) ? fixSlowAttackCoolDown : fixQuickAttackCoolDown;

                        //check if the player has finished attacking 
                        if (fixStateTime > fixWarmUpTime + fixCoolDownTime)
                        {
                            //switch to standing state 
                            frmUpdatedFrame.m_bPlayerState[i] = bStandingState;
                        }
                    }

                }
            }

            return true;
        }

        public bool CheckForDeath(GameSettings setSettings, FrameData frmUpdatedFrame)
        {
            if(setSettings.Invincibility)
            {
                return true;
            }

            byte bDeadState = (byte)FrameData.State.Dead;

            for(int i = 0; i < frmUpdatedFrame.m_sPlayerHealths.Count; i++)
            {
                if(frmUpdatedFrame.m_sPlayerHealths[i] <= 0)
                {
                    frmUpdatedFrame.m_bPlayerState[i] = bDeadState;
                }
            }

            return true;
        }

        //returnd how Similar 2 directions are with 0 being the same and 4 being pollar oposites 
        public int CompareDirections(byte bDirection1, byte bDirection2)
        {
            //if defender is not facing a direction or the attack is directionless its a direct hit
            if (bDirection1 == 0 || bDirection2 == 0)
            {
                return 4;
            }

            //check if directions are the same 
            if (bDirection1 == bDirection2)
            {
                return 0;
            }

            FixVec2 vecDirection1 = ConvertDirectionToNormal(bDirection1);
            FixVec2 vecDirection2 = ConvertDirectionToNormal(bDirection2);

            Fix fixAllignment = vecDirection1.Dot(vecDirection2);

            if (fixAllignment > Fix.Epsilon)
            {
                return 1;
            }

            if (fixAllignment > Fix.Epsilon)
            {
                return 2;
            }

            if (fixAllignment > ((-Fix.Epsilon) - 1))
            {
                return 3;
            }

            return 4;
        }

        public FixVec2 ConvertDirectionToNormal(byte dirDirection)
        {
            //check for null direction
            if (dirDirection == 0)
            {
                return new FixVec2(0, 1);
            }

            //check if up
            if ((dirDirection & (byte)FrameData.Direction.Left) > 0)
            {
                //check if left or right 
                if ((dirDirection & (byte)FrameData.Direction.Up) > 0)
                {
                    return new FixVec2(-1, 1).Normalize();
                }
                else if ((dirDirection & (byte)FrameData.Direction.Down) > 0)
                {
                    return new FixVec2(-1, -1).Normalize();
                }
                else
                {
                    return new FixVec2(-1, 0);
                }
            }
            else if ((dirDirection & (byte)FrameData.Direction.Right) > 0) //check if down 
            {
                //check if left or right 
                if ((dirDirection & (byte)FrameData.Direction.Up) > 0)
                {
                    return new FixVec2(1, 1).Normalize();
                }
                else if ((dirDirection & (byte)FrameData.Direction.Down) > 0)
                {
                    return new FixVec2(1, -1).Normalize();
                }
                else
                {
                    return new FixVec2(1, 0);
                }
            }
            else
            {
                //check if left or right 
                if ((dirDirection & (byte)FrameData.Direction.Up) > 0)
                {
                    return new FixVec2(0, 1);
                }
                else
                {
                    return new FixVec2(0, -1);
                }
            }
        }

        public byte ConvertNormalToDirection(FixVec2 vecNormal)
        {

            //get 22 degree sine value 
            Fix fixPSin22 = FixMath.Sign(22);
            Fix fixNSin22 = -fixPSin22;

            byte bDirection = 0;

            //check if left
            if (vecNormal.X < fixNSin22)
            {
                bDirection = (byte)FrameData.Direction.Left;
            }
            else if (vecNormal.X > fixNSin22) //check if right 
            {
                bDirection = (byte)FrameData.Direction.Right;
            }

            //check if up
            if (vecNormal.Y > fixNSin22)
            {
                bDirection = (byte)(bDirection | (byte)FrameData.Direction.Left);
            }
            else if (vecNormal.Y < fixNSin22) //check if down 
            {
                bDirection = (byte)(bDirection | (byte)FrameData.Direction.Right);
            }

            return bDirection;
        }

        public byte[] GetHashForInputs(int iTick)
        {
            //create small array to hold temp results
            byte[] bIndivitualHash = new byte[8];

            //create byte array for all hash results
            byte[] bHashResults = new byte[bIndivitualHash.Length * m_ipbPlayerInputs.Count];

            //loop through all input buffers 
            for (int i = 0; i < m_ipbPlayerInputs.Count; i++)
            {
                //get input hash
                m_ipbPlayerInputs[i].GetHashCodeForInputs(bIndivitualHash, 0, iTick);

                //add to main list 
                for (int j = 0; j < bIndivitualHash.Length; j++)
                {
                    bHashResults[(i * bIndivitualHash.Length) + j] = bIndivitualHash[j];
                }
            }

            MD5 md5 = MD5.Create();

            //generate the hash code 
            return md5.ComputeHash(bHashResults);
        }

        public byte[] GetHashForFrame(int iTick)
        {
            byte[] bHash = new byte[8];

            GetFrameData(iTick).GetHashCode(bHash);

            return bHash;
        }

        public bool GetPlayersOverlappingCircle(GameSettings setSettings, FrameData frmCurrentFrame, FixVec2 vecCenter, Fix fSize, int iPlayer, List<short> sPlayersHit)
        {
            //calculate the max distance from center to player
            Fix fHitDistanceSqr = (fSize + setSettings.ChararcterSize.m_fValue);

            fHitDistanceSqr = fHitDistanceSqr * fHitDistanceSqr;

            List<FixVec2> vecPlayerCenterPoints = frmCurrentFrame.m_v2iPosition;

            List<byte> bPlayerState = frmCurrentFrame.m_bPlayerState;

            byte bDeadState = (byte)FrameData.State.Dead;

            bool bHit = false;

            //loop through all the players
            for (short i = 0; i < vecPlayerCenterPoints.Count; i++)
            {
                if (i != iPlayer && bPlayerState[i] != bDeadState)
                {
                    //get difference between points
                    Fix fDistanceSqr = (vecPlayerCenterPoints[i] - vecCenter).GetMagnitudeSqr();

                    //check for center point overlap
                    if (fDistanceSqr < fHitDistanceSqr)
                    {
                        //add to hit list 
                        sPlayersHit.Add(i);

                        bHit = true;
                    }
                }
            }

            return bHit;
        }
    }
}