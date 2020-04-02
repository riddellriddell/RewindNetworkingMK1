using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// this file contains utility and data struct and classes for the WebAPI interface 

namespace Networking
{

    /// <summary>
    /// the data used to connect a player to a player ID
    /// </summary>
    [Serializable]
    public struct UserIDDetails
    {
        //the identifier for the account
        public long m_lUserID;

        public long m_lUserKey;
    }

    /// <summary>
    /// the data linked to a single account
    /// </summary>
    public struct UserAccountData
    {
        public long m_lAccountID;

        public int m_iPlayerRank;
    }

    //todo find a better way to do this
    public enum MessageType
    {
        None, //use to detect defective messages that were not correctly serializd 
        GatewayMessage
    }


    [Serializable]
    public struct UserMessage
    {       
        public long m_lFromUser;
        public long m_dtmTimeOfMessage;
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
        public long m_lUserID;

        public long m_lUserKey;

        public long m_dtmLastActiveTime;

        public SimStatus m_staGameState;

    }

    //---------------------- Communication Types --------------------------------


    [Serializable]
    public struct GetMessageRequest
    {
        public long m_lUserID;
        public long m_lUserKey;
    }

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
        public long m_lUserID;
        public long m_lUserKey;
        public SimStatus m_staGameState;
    }

    [Serializable]
    public struct SearchForGatewayReturn
    {
        public long m_lGateOwnerUserID;
    }
}