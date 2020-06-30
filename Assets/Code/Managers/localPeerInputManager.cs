using Networking;
using Sim;
using UnityEngine;
using System;
using Utility;
using SharedTypes;
using ProjectSharedTypes;

//this class tracks inputs from the local peer
namespace GameManagers
{
    public class LocalPeerInputManager
    {
        #region UserInputGlobalMessage     
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
        public byte m_bInputState;

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
            UserInputGlobalMessage.TypeID = m_ngpGlobalMessageProcessor.RegisterCustomMessageType<UserInputGlobalMessage>(UserInputGlobalMessage.TypeID);
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
            UserInputGlobalMessage tuiUserInputMessage = m_ngpGlobalMessageProcessor.m_cifGlobalMessageFactory.CreateType<UserInputGlobalMessage>(UserInputGlobalMessage.TypeID);

            //set values
            tuiUserInputMessage.m_bInputState = m_bInputState;

            //get lock on out message stack

            m_ndbNetworkingDataBridge.m_gmbOutMessageBuffer.Add(tuiUserInputMessage);

            //release lock on out message stack

            m_dtmTimeOfLastInputMessageCreation = DateTime.UtcNow;

            m_bDirtyInputState = false;
        }

        //get inputs 
        #region Callbacks

        public void OnLeftPressed()
        {
            m_bInputState = SimInputManager.SetTurnLeft(m_bInputState, true);
            m_bDirtyInputState = true;
        }

        public void OnLeftReleased()
        {
            m_bInputState = SimInputManager.SetTurnLeft(m_bInputState, false);
            m_bDirtyInputState = true;
        }

        public void OnRightPressed()
        {
            m_bInputState = SimInputManager.SetTurnRight(m_bInputState, true);
            m_bDirtyInputState = true;
        }

        public void OnRightReleased()
        {
            m_bInputState = SimInputManager.SetTurnRight(m_bInputState, false);
            m_bDirtyInputState = true;
        }

        public void OnSpecialTap()
        {
            m_bInputState = SimInputManager.SetDropDisruptorEvent(m_bInputState, true);

            //trigger message strait away as this is an event
            CreateUserInputMessage();

        }

        public void OnSpecialHold()
        {
            m_bInputState = SimInputManager.SetChargeMissile(m_bInputState, true);
            m_bDirtyInputState = true;
        }

        public void OnSpecialRelease()
        {
            m_bInputState = SimInputManager.SetChargeMissile(m_bInputState, false);
            m_bDirtyInputState = true;
        }
        #endregion
    }
}