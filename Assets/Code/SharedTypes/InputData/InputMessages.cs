using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Utility;

namespace SharedTypes
{
    public interface IInput
    {
        byte[] GetHash();
    }

    public interface ISimMessagePayload : IInput
    {

    }

    public struct UserConnecionChange : IInput
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

        public byte[] GetHash()
        {
            byte[] bHash = new byte[8];

            for(int i = 0; i < m_lJoinPeerID.Length; i++)
            {
                byte[] existing_input_hash = BitConverter.GetBytes(m_lJoinPeerID[i]);

                HashTools.MergeHashes(ref bHash, existing_input_hash);
            }

            for (int i = 0; i < m_iJoinPeerChannelIndex.Length; i++)
            {
                byte[] existing_input_hash = BitConverter.GetBytes(m_iJoinPeerChannelIndex[i]);

                HashTools.MergeHashes(ref bHash, existing_input_hash);
            }

            for (int i = 0; i < m_lKickPeerID.Length; i++)
            {
                byte[] existing_input_hash = BitConverter.GetBytes(m_lKickPeerID[i]);

                HashTools.MergeHashes(ref bHash, existing_input_hash);
            }

            for (int i = 0; i < m_iKickPeerChannelIndex.Length; i++)
            {
                byte[] existing_input_hash = BitConverter.GetBytes(m_iKickPeerChannelIndex[i]);

                HashTools.MergeHashes(ref bHash, existing_input_hash);
            }

            return bHash;
        }
    }

    public struct MessagePayloadWrapper : IInput
    {
        public long m_lPeerID;

        public int m_iChannelIndex;

        public ISimMessagePayload m_smpPayload;

        public byte[] GetHash()
        {
            byte[] bHash = new byte[8];

            HashTools.MergeHashes(ref bHash, BitConverter.GetBytes(m_lPeerID));

            HashTools.MergeHashes(ref bHash, BitConverter.GetBytes(m_iChannelIndex));

            HashTools.MergeHashes(ref bHash, m_smpPayload.GetHash());

            return bHash;
        }
    }
}
