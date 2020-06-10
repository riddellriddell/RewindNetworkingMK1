using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SharedTypes
{
    public interface ISimMessagePayload
    {

    }

    public struct UserConnecionChange
    {
        public long[] m_lJoinPeerID;
        public int[] m_iJoinPeerChannelIndex;

        public long[] m_lKickPeerID;
        public int[] m_iKickPeerChannelIndex;

        public UserConnecionChange(int iKickCount, int iJoinCount)
        {
            m_lJoinPeerID = new long[iJoinCount];
            m_iJoinPeerChannelIndex = new int[iJoinCount];

            m_lKickPeerID = new long[iKickCount];
            m_iKickPeerChannelIndex = new int[iKickCount];
        }

    }

    public struct MessagePayloadWrapper
    {
        public long m_lPeerID;

        public int m_iChannelIndex;

        public ISimMessagePayload m_smpPayload;
    }
}
