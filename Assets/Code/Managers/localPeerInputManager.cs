using Networking;
using Sim;
using UnityEngine;
using System;
using Utility;

//this class tracks inputs from the local peer
namespace GameManagers
{
    public class LocalPeerInputManager
    {
        #region UserInputGlobalMessage
        public class UserInputGlobalMessage : GlobalMessageBase, ISimMessagePayload
        {
            public static int TypeID { get; set; } = int.MinValue;

            public override int TypeNumber
            {
                get
                {
                    return UserInputGlobalMessage.TypeID;
                }
                set
                {
                    TypeID = value;
                }
            }

            public SimInputManager.UserInput m_uipInputState;

            public override int DataSize()
            {
                return ByteStream.DataSize(m_uipInputState.m_bPayload);
            }

            public override void Serialize(ReadByteStream rbsByteStream)
            {
                ByteStream.Serialize(rbsByteStream, ref m_uipInputState.m_bPayload);
            }

            public override void Serialize(WriteByteStream wbsByteStream)
            {
                ByteStream.Serialize(wbsByteStream, ref m_uipInputState.m_bPayload);
            }
        }

        public class TestingUserInput : GlobalMessageBase, ISimMessagePayload
        {
            public static int TypeID { get; set; } = int.MinValue;

            public override int TypeNumber
            {
                get
                {
                    return TestingUserInput.TypeID;
                }
                set
                {
                    TypeID = value;
                }
            }

            public int m_iInput;

            public override int DataSize()
            {
                return ByteStream.DataSize(m_iInput);
            }

            public override void Serialize(ReadByteStream rbsByteStream)
            {
                ByteStream.Serialize(rbsByteStream, ref m_iInput);
            }

            public override void Serialize(WriteByteStream wbsByteStream)
            {
                ByteStream.Serialize(wbsByteStream, ref m_iInput);
            }
        }
        #endregion

        //the minimum time between new message creation to avoid message spam
        public static TimeSpan s_tspMinTimeBetweenMessages = TimeSpan.FromSeconds(1f / 60f);

        //the time the last input message was created 
        public DateTime m_dtmTimeOfLastInputMessageCreation;

        //the local state of the input 
        public SimInputManager.UserInput m_uipInputState;

        public int m_iNumberOfInputsCreated = 0;
        
        //has the inputs changes since the last message to peers
        public bool m_bDirtyInputState;

        public NetworkingDataBridge m_ndbNetworkingDataBridge;

        public NetworkGlobalMessengerProcessor m_ngpGlobalMessageProcessor;

        public LocalPeerInputManager(NetworkingDataBridge ndbNetworkingDataBridge, NetworkGlobalMessengerProcessor ngpGlobalMessageProcessor)
        {
            m_ndbNetworkingDataBridge = ndbNetworkingDataBridge;
            m_ngpGlobalMessageProcessor = ngpGlobalMessageProcessor;

            //register user input message type
            TestingUserInput.TypeID = m_ngpGlobalMessageProcessor.RegisterCustomMessageType<TestingUserInput>(TestingUserInput.TypeID);
        }

        public void Update()
        {
            if (m_bDirtyInputState)
            {
                //if (DateTime.UtcNow - m_dtmTimeOfLastInputMessageCreation > s_tspMinTimeBetweenMessages)
                //{
                    CreateUserInputMessage();
                //}
            }
        }

        public void CreateUserInputMessage()
        {
            //create user input message
            TestingUserInput tuiUserInputMessage = m_ngpGlobalMessageProcessor.m_cifGlobalMessageFactory.CreateType<TestingUserInput>(TestingUserInput.TypeID);

            //set values
            tuiUserInputMessage.m_iInput = m_iNumberOfInputsCreated;

            //get lock on out message stack

            m_ndbNetworkingDataBridge.m_gmbOutMessageBuffer.Add(tuiUserInputMessage);

            //release lock on out message stack

            m_dtmTimeOfLastInputMessageCreation = DateTime.UtcNow;

            m_bDirtyInputState = false;
        }

        //get inputs 
        #region Callbacks
        public void IncrementNumberOfMessages()
        {
            m_iNumberOfInputsCreated++;

            CreateUserInputMessage();
        }

        public void OnLeftPressed()
        {
            m_uipInputState.TurnLeft = true;
            m_bDirtyInputState = true;
        }

        public void OnLeftReleased()
        {
            m_uipInputState.TurnLeft = false;
            m_bDirtyInputState = true;
        }

        public void OnRightPressed()
        {
            m_uipInputState.TurnRight = true;
            m_bDirtyInputState = true;
        }

        public void OnRightReleased()
        {
            m_uipInputState.TurnRight = false;
            m_bDirtyInputState = true;
        }

        public void OnSpecialTap()
        {
            m_uipInputState.DropDisruptorEvent = true;

            //trigger message strait away as this is an event
            CreateUserInputMessage();

        }

        public void OnSpecialHold()
        {
            m_uipInputState.ChargeingMissile = true;
            m_bDirtyInputState = true;
        }

        public void OnSpecialRelease()
        {
            m_uipInputState.ChargeingMissile = false;
            m_bDirtyInputState = true;
        }
        #endregion
    }

    public class LoclaPeerInputManagerTester
    {
        public LocalPeerInputManager m_lpiLocalPeerInputManager;

        public float m_fChanceOfDirectionChange = 0.5f;

        public float m_fChanceOfSpecial = 4f;

        public float m_fCanceSpecialIsMissile = 0.5f;

        public float m_fMaxMissileHoldTime = 3f;

        protected DateTime m_dtmTimeOfLastUpdate = DateTime.MinValue;

        public LoclaPeerInputManagerTester(LocalPeerInputManager lpiTargetLocalPeerInputManager)
        {
            m_lpiLocalPeerInputManager = lpiTargetLocalPeerInputManager;
        }

        public void Update()
        {
            if(m_dtmTimeOfLastUpdate == DateTime.MinValue)
            {
                m_dtmTimeOfLastUpdate = DateTime.UtcNow;
            }

            while(DateTime.UtcNow - m_dtmTimeOfLastUpdate > TimeSpan.FromSeconds(1f/3f))
            {
                m_dtmTimeOfLastUpdate += TimeSpan.FromSeconds(1f / 3f);

                m_lpiLocalPeerInputManager.IncrementNumberOfMessages();
            }





            //float fDeltaTime = (float)(DateTime.UtcNow - m_dtmTimeOfLastUpdate).TotalSeconds;
            //
            //m_dtmTimeOfLastUpdate = DateTime.UtcNow;
            //
            ////check for direction change 
            //if (UnityEngine.Random.Range(0.0f, 1.0f ) < m_fChanceOfDirectionChange * fDeltaTime)
            //{
            //    int iMoveType = UnityEngine.Random.Range(0, 4);
            //
            //    switch(iMoveType)
            //    {
            //        case 0:
            //            if(m_lpiLocalPeerInputManager.m_uipInputState.TurnLeft == true)
            //            {
            //                m_lpiLocalPeerInputManager.OnLeftReleased();
            //            }
            //
            //            if (m_lpiLocalPeerInputManager.m_uipInputState.TurnRight == true)
            //            {
            //                m_lpiLocalPeerInputManager.OnRightReleased();
            //            }
            //
            //            if (m_lpiLocalPeerInputManager.m_uipInputState.Boost == true)
            //            {
            //                m_lpiLocalPeerInputManager.OnLeftReleased();
            //                m_lpiLocalPeerInputManager.OnRightReleased();
            //            }
            //
            //            break;
            //
            //        case 1:
            //
            //            if (m_lpiLocalPeerInputManager.m_uipInputState.Boost == false)
            //            {
            //                m_lpiLocalPeerInputManager.OnRightPressed();
            //                m_lpiLocalPeerInputManager.OnLeftPressed();
            //            }
            //
            //            break;
            //
            //        case 2:
            //
            //            if (m_lpiLocalPeerInputManager.m_uipInputState.Boost == true)
            //            {
            //                m_lpiLocalPeerInputManager.OnRightReleased();
            //            }
            //
            //            if (m_lpiLocalPeerInputManager.m_uipInputState.TurnLeft == false)
            //            {
            //                m_lpiLocalPeerInputManager.OnLeftPressed();
            //            }
            //
            //            if (m_lpiLocalPeerInputManager.m_uipInputState.TurnRight == true)
            //            {
            //                m_lpiLocalPeerInputManager.OnRightReleased();
            //            }
            //
            //            break;
            //
            //
            //        case 3:
            //
            //            if (m_lpiLocalPeerInputManager.m_uipInputState.Boost == true)
            //            {
            //                m_lpiLocalPeerInputManager.OnLeftReleased();
            //            }
            //
            //            if (m_lpiLocalPeerInputManager.m_uipInputState.TurnLeft == true)
            //            {
            //                m_lpiLocalPeerInputManager.OnLeftReleased();
            //            }
            //
            //            if (m_lpiLocalPeerInputManager.m_uipInputState.TurnRight == false)
            //            {
            //                m_lpiLocalPeerInputManager.OnRightPressed();
            //            }
            //
            //            break;
            //    }                
            //}
            //

            //TODO: add code to test disruptor and missile firing 

        }
    }
    
}