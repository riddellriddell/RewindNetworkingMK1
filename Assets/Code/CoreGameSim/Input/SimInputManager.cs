using System;

namespace Sim
{
    //the job of this class is to consume input change messages from the input buffer and merge them into a single input struct that can be acted upon by the sim
    public class SimInputManager
    {
        #region InputAccess
        public const byte c_bButtonInputMask = 1 | 2 | 4;

        public const byte c_bEventInputMask = 8;

        public static bool GetTurnLeft(byte bInput)
        {
            if (GetBoost(bInput) == false)
            {
                return (bInput & 1) != 0;
            }

            return false;
        }

        public static byte SetTurnLeft(byte bInput, bool bValue)
        {
            if (bValue)
            {
                bInput = (byte)(bInput | 1);
            }
            else
            {
                bInput = (byte)(bInput & ~1);
            }

            return bInput;
        }

        public static bool GetTurnRight(byte bInput)
        {
            if (GetBoost(bInput) == false)
            {
                return (bInput & 2) != 0;
            }

            return false;
        }
        public static byte SetTurnRight(byte bInput, bool bValue)
        {
            if (bValue)
            {
                bInput = (byte)(bInput | 2);
            }
            else
            {
                bInput = (byte)(bInput & ~2);
            }

            return bInput;
        }

        public static byte SetBoost(byte bInput, bool bValue)
        {
            if (bValue)
            {
                bInput = (byte)(bInput | (1 & 2));
            }
            else
            {
                bInput = (byte)(bInput & ~(1 & 2));
            }

            return bInput;

        }

        public static bool GetBoost(byte bInput)
        {
            return ((bInput & 1) != 0) && ((bInput & 2) != 0);
        }

        public static bool GetChargeMissile(byte bInput)
        {
            return ((bInput & 4) != 0);
        }

        public static byte SetChargeMissile(byte binput, bool bValue)
        {
            if (bValue)
            {
                binput = (byte)(binput | 4);
            }
            else
            {
                binput = (byte)(binput & ~4);
            }

            return binput;
        }

        public static bool GetDropDisruptorEvent(byte bInput)
        {
            return ((bInput & 8) != 0);
        }

        public static byte SetDropDisruptorEvent(byte binput, bool bValue)
        {
            if (bValue)
            {
                binput = (byte)(binput | 8);
            }
            else
            {
                binput = (byte)(binput & ~8);
            }

            return binput;
        }


        public static byte ProcessInput(byte bInput, byte bMessageChange)
        {
            //set button inputs
            bInput = (byte)((bInput & ~c_bButtonInputMask) + (bMessageChange & c_bButtonInputMask));

            //set event inputs
            bInput = (byte)(bInput | (bMessageChange & c_bEventInputMask));

            return bInput;
        }

        public static byte ClearEvents(byte bInput)
        {
            return (byte)(bInput & ~c_bEventInputMask);
        }

        public static byte DefaultInput()
        {
            return 0;
        }

        #endregion

        #region FutureCodeForMoreAdvancedInputSchemes 
        //a button that is either on or off 
        //if tapped it registeres as pressed for one tick even if it is released before the end of the tick
        // if released it will not count as preseed again until the start of the next tick 
        public struct TickSafeBinaryButton
        {
            [Flags]
            public enum State : byte
            {
                None = 0,
                BaseStateTrue = 1,
                PresentedStateTrue = 2,
                StartOfTickTrue = 4,
            }

            public State m_staState;

            //absolute state
            public bool BaseState
            {
                get
                {
                    return (m_staState & State.BaseStateTrue) != State.None;
                }

                private set
                {
                    if (value)
                    {
                        m_staState = m_staState | State.BaseStateTrue;
                    }
                    else
                    {
                        m_staState = m_staState & ~State.BaseStateTrue;
                    }
                }
            }

            //presented state 
            public bool PresentedState
            {
                get
                {
                    return (m_staState & State.PresentedStateTrue) != State.None;
                }

                private set
                {
                    if (value)
                    {
                        m_staState = m_staState | State.PresentedStateTrue;
                    }
                    else
                    {
                        m_staState = m_staState & ~State.PresentedStateTrue;
                    }
                }
            }

            //has been pressed this tick
            private bool StartOfTick
            {
                get
                {
                    return (m_staState & State.StartOfTickTrue) != State.None;
                }

                set
                {
                    if (value)
                    {
                        m_staState = m_staState | State.StartOfTickTrue;
                    }
                    else
                    {
                        m_staState = m_staState & ~State.StartOfTickTrue;
                    }
                }
            }

            public void OnNewTick()
            {
                StartOfTick = BaseState;
                PresentedState = BaseState;
            }

            public void OnNewInput(bool bBaseState)
            {
                BaseState = bBaseState;

                if (BaseState != StartOfTick)
                {
                    PresentedState = !StartOfTick;
                }
            }
        }

        //this button acts like a binary button but 
        public struct TimedPressButton
        {

        }

        //inputs are either -1, 0 or 1 but are scaled by when in a tick they are applied 
        //if a change from 0 to 1 is recieved half way through a tick the input is treated as 0.5 
        public struct TemporalBinaryLinearAxis
        {

        }
        #endregion

        public struct UserInput
        {
            public const byte c_bButtonInputMask = 1 & 2 & 4;

            public const byte c_bEventInputMask = 8;

            public byte m_bPayload;

            public bool TurnLeft
            {
                get
                {
                    if (Boost == false)
                    {
                        return (m_bPayload & 1) != 0;
                    }

                    return false;
                }

                set
                {
                    if (value)
                    {
                        m_bPayload = (byte)(m_bPayload | 1);
                    }
                    else
                    {
                        m_bPayload = (byte)(m_bPayload & ~1);
                    }
                }
            }

            public bool TurnRight
            {
                get
                {
                    if (Boost == false)
                    {
                        return (m_bPayload & 2) != 0;
                    }

                    return false;
                }

                set
                {
                    if (value)
                    {
                        m_bPayload = (byte)(m_bPayload | 2);
                    }
                    else
                    {
                        m_bPayload = (byte)(m_bPayload & ~2);
                    }
                }
            }

            public bool Boost
            {
                get
                {
                    return ((m_bPayload & 1) != 0) && ((m_bPayload & 2) != 0);
                }

                set
                {
                    if (value)
                    {
                        m_bPayload = (byte)(m_bPayload | (1 & 2));
                    }
                    else
                    {
                        m_bPayload = (byte)(m_bPayload & ~(1 & 2));
                    }
                }
            }

            public bool ChargeingMissile
            {
                get
                {
                    return ((m_bPayload & 4) != 0);
                }

                set
                {
                    if (value)
                    {
                        m_bPayload = (byte)(m_bPayload | 4);
                    }
                    else
                    {
                        m_bPayload = (byte)(m_bPayload & ~4);
                    }
                }

            }

            public bool DropDisruptorEvent
            {
                get
                {
                    return ((m_bPayload & 8) != 0);
                }

                set
                {
                    if (value)
                    {
                        m_bPayload = (byte)(m_bPayload | 8);
                    }
                    else
                    {
                        m_bPayload = (byte)(m_bPayload & ~8);
                    }
                }
            }
                       
            public void ProcessInput(byte bMessageChange)
            {
                //set button inputs
                m_bPayload = (byte)((m_bPayload & ~c_bButtonInputMask) + (bMessageChange & c_bButtonInputMask));

                //set event inputs
                m_bPayload = (byte)(m_bPayload | (bMessageChange & c_bEventInputMask));
            }

            public void ClearEvents()
            {
                m_bPayload = (byte)(m_bPayload & ~c_bEventInputMask);
            }

            public void ResetInputs()
            {
                m_bPayload = 0;
            }

        }

        //the inputs for all the players
        public UserInput[] m_uipUserInputs;

        public SimInputManager(int iMaxPlayers)
        {
            m_uipUserInputs = new UserInput[iMaxPlayers];
        }

        public void ProcessInput(int iPlayerIndex, byte bInput)
        {
            //process user input
            m_uipUserInputs[iPlayerIndex].ProcessInput(bInput);
        }

        public void OnNewTick()
        {
            //clean up any events from the previouse tick 
            for (int i = 0; i < m_uipUserInputs.Length; i++)
            {
                m_uipUserInputs[i].ClearEvents();
            }
        }
    }
}
