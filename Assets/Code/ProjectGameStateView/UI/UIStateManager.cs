using SimDataInterpolation;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GameViewUI
{
    public class UIStateManager : MonoBehaviour
    {
        public enum State
        {
            Default,
            Startup,
            Dead,
            Alive,

        }

        public State m_staState = State.Default;

        public GameStartupUI m_gsuGameStartupUi;

        public DeadUI m_duiDeadUI;

        public AliveUI m_auiAliveUI;

        public int m_iActivePlayerIndex;

        public void SetStateSetup()
        {
            ChangeState(State.Startup);
        }

        public int GetPeerSlotAssignment(InterpolatedFrameDataGen ifdFrameData, long lLocalPeerID)
        {
            for(int i = 0; i < ifdFrameData.m_lPeersAssignedToSlot.Length; i++)
            {
                if(ifdFrameData.m_lPeersAssignedToSlot[i] == lLocalPeerID)
                {
                    return i;
                }
            }

            return int.MinValue;
        }

        public void ChangeState(State staNewState)
        {
            //block changes to same state 
            if(staNewState == m_staState)
            {
                return;
            }

            switch(m_staState)
            {
                case State.Startup:

                    OnExitStartup();

                    break;

                case State.Dead:

                    OnExitDead();

                    break;

                case State.Alive:

                    OnExitAlive();

                    break;
            }

            switch(staNewState)
            {
                case State.Startup:

                    OnEnterStartup();

                    break;

                case State.Dead:

                    OnEnterDead();

                    break;

                case State.Alive:

                    OnEnterAlive();

                    break;
            }

            m_staState = staNewState;
        }

        public void UpdateGameView(InterpolatedFrameDataGen ifdFrameData, long lPlayerID)
        {
            int iPeerIndex = GetPeerSlotAssignment(ifdFrameData, lPlayerID);

            if (iPeerIndex > -1 && ifdFrameData.m_fixShipHealth[iPeerIndex] > 0)
            {
                ChangeState(State.Alive);
            }
            else
            {
                ChangeState(State.Dead);
            }

            if (m_staState == State.Dead)
            {
                m_duiDeadUI.OnUpdate(ifdFrameData, iPeerIndex);
            }
            else if(m_staState == State.Alive)
            {
                m_auiAliveUI.OnUpdate(ifdFrameData, iPeerIndex);
            }
        }

        public void LogStartupEvent(string strEvent)
        {
            if(m_staState == State.Startup)
            {
                m_gsuGameStartupUi.UpdateGameState(strEvent);
            }
        }

        public void OnEnterStartup()
        {
            //show startup ui
            m_gsuGameStartupUi.OnEnterState();
        }

        public void OnExitStartup()
        {
            //hide startup ui   
            m_gsuGameStartupUi.OnExitState();
        }

        public void OnEnterDead()
        {
            m_duiDeadUI.OnEnterState();

        }

        public void OnExitDead()
        {
            m_duiDeadUI.OnExitState();
        }

        public void OnEnterAlive()
        {
            m_auiAliveUI.OnEnterState();
        }

        public void OnExitAlive()
        {
            m_auiAliveUI.OnExitState();
        }
    }
}
