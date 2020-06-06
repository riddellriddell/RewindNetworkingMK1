using Networking;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Sim
{
    public interface IPeerSlotAssignmentFrameData
    {
        long[] PeerSlotAssignment { get; set; }
    }

    public interface IPeerSlotAssignmentSettingsData
    {
        int MaxPlayers { get; }
    }

    public class ProcessPeerSlotAssignment<TFrameData, TConstData, TSettingsData> : ISimSetupProcesses<TFrameData, TSettingsData>, ISimProcess<TFrameData, TConstData, TSettingsData> where TFrameData : IPeerSlotAssignmentFrameData where TSettingsData : IPeerSlotAssignmentSettingsData
    {
        public int Priority
        {
            get
            {
                return 0;
            }
        }

        public string ProcessName
        {
            get
            {
                return "ManagePeerSlotAssignment";
            }
        }

        public bool ApplySetupProcess(uint iTick, in TSettingsData sdaSettingsData, long lFirstPeerID, ref TFrameData fdaFrameData)
        {
            fdaFrameData.PeerSlotAssignment = new long[sdaSettingsData.MaxPlayers];

            fdaFrameData.PeerSlotAssignment[0] = lFirstPeerID;

            for(int i = 1; i < sdaSettingsData.MaxPlayers; i++)
            {
                fdaFrameData.PeerSlotAssignment[i] = long.MinValue;
            }

            return true;
        }

        public bool ProcessFrameData(uint iTick, in TSettingsData staSettingsData, in TConstData cdaConstantData, in TFrameData fdaInFrameData, in object[] objInputs, ref TFrameData fdaOutFrameData)
        {
            for (int i = 0; i < objInputs.Length; i++)
            {
                if (objInputs[i] is NetworkingDataBridge.UserConnecionChange)
                {
                    NetworkingDataBridge.UserConnecionChange uccUserConnectionChange = (NetworkingDataBridge.UserConnecionChange)objInputs[i];

                    //apply all the join messages
                    for (int j = 0; j < uccUserConnectionChange.m_iJoinPeerChannelIndex.Length; j++)
                    {
                        fdaOutFrameData.PeerSlotAssignment[uccUserConnectionChange.m_iJoinPeerChannelIndex[j]] = uccUserConnectionChange.m_lJoinPeerID[j];
                    }

                    //apply all the kick messages
                    for (int j = 0; j < uccUserConnectionChange.m_iKickPeerChannelIndex.Length; j++)
                    {
                        fdaOutFrameData.PeerSlotAssignment[uccUserConnectionChange.m_iKickPeerChannelIndex[j]] = long.MinValue;
                    }
                }
            }

            return true;
        }
    }
}