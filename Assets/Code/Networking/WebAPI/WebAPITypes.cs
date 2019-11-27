using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// this file contains utility and data struct and classes for the WebAPI interface 

namespace Networking
{
    /// <summary>
    /// the details used to identify this machiene and create a player 
    /// </summary>
    public struct DeviceLoginIdentifier
    {
        //the unique identifier for this device 
        public string m_strLoginCredentials;
    }

    /// <summary>
    /// the data used to connect a player to a player ID
    /// </summary>
    public struct UserIDCredentialsPair
    {
        //the identifier for the account
        public long m_lAccountID;

        public DeviceLoginIdentifier m_dliDeviceLoginIdentifier;
    }

    /// <summary>
    /// the data linked to a single account
    /// </summary>
    public struct UserAccountData
    {
        public long m_lAccountID;

        public int m_iPlayerRank;
    }

    [Serializable]
    public struct UserMessage
    {
        /// <summary>
        /// Message Types
        /// </summary>
        public enum MessageType
        {
            WebRTCOffer,
            WebRTCIceCandidate,
            WebRTCReply,
        }

        public long m_lFromUser;
        public long m_lTimeOfMessage;
        public int m_iMessageType;
        public string m_strMessage;
    }

    /// <summary>
    /// structure for holding all the messages associated with a user
    /// </summary>
    public struct UserMessages
    {
        public long m_lAccountID;

        public UserMessage[] m_umUserMessages; 
    }

    //the active state of the sim 
    [Serializable]
    public struct SimStatus
    {
        public enum State
        {
            Broken,
            Setup,
            Lobby,
            Active,
            Result,
            Closed
        }

        /// <summary>
        /// places in the game that are free for people to fill
        /// </summary>
        public int m_iRemainingSlots;

        public int m_iSimStatus;
    }

    /// <summary>
    /// the connection details to an active user in an active lobby/game
    /// </summary>
    [Serializable]
    public struct Gateway
    {
        public long m_lOwningPlayerId;

        public long m_lTimeOfLastUpdate;

        public SimStatus m_sstSimStatus;

    }

    //---------------------- Communication Types --------------------------------

    [Serializable]
    public struct GetMessageReturn
    {
        public UserMessage[] m_usmUserMessages;
    }


    [Serializable]
    public struct SendMessageCommand
    {
        public long m_lFromID;
        public long m_lToID;
        public int m_iType;
        public string m_strMessage;
    }


    [Serializable]
    public struct SetGatewayCommand
    {
        public long m_lOwningPlayerId;

        public SimStatus m_sstStatus;
    }
}