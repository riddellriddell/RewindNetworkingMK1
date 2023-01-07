
using ProjectSharedTypes;
using SharedTypes;
using System;
using Utility;

namespace Sim
{
    public interface IPeerInputFrameData
    {
        byte[] PeerInput { get; set; }

        byte[] InputHash { get; set; }
    }

    public class ProcessPeerInputs<TFrameData, TConstData, TSettingsData> :
            ISimProcess<TFrameData, TConstData, TSettingsData>,
            ISimSetupProcesses<TFrameData, TSettingsData>
        where TFrameData : IPeerInputFrameData
        where TSettingsData : IPeerSlotAssignmentSettingsData
    {


        // Start is called before the first frame update
        public int Priority { get; } =  1;

        public string ProcessName { get; } = "User Input Process";

        public bool ApplySetupProcess(uint iTick, in TSettingsData sdaSettingsData, long lFirstPeerID, ref TFrameData fdaFrameData)
        {
            fdaFrameData.PeerInput = new byte[sdaSettingsData.MaxPlayers];
            fdaFrameData.InputHash = new byte[8];

            for (int i = 0; i < fdaFrameData.PeerInput.Length; i++)
            {
                fdaFrameData.PeerInput[i] = 0;
            }

            for (int i = 0; i < fdaFrameData.InputHash.Length; i++)
            {
                fdaFrameData.InputHash[i] = 0;
            }

            return true;
        }

        public bool ProcessFrameData(uint iTick, in TSettingsData sdaSettingsData, in TConstData cdaConstantData, in TFrameData fdaInFrameData, in IInput[] objInputs, ref TFrameData fdaOutFrameData)
        {
            //reset all input events for new tick
            for(int i = 0; i < sdaSettingsData.MaxPlayers; i++)
            {
                fdaOutFrameData.PeerInput[i] = SimInputManager.ClearEvents(fdaOutFrameData.PeerInput[i]);
            }

            for (int i = 0; i < objInputs.Length; i++)
            {
                //check if message is a disconnect message 
                //TODO shift this class into a shared typed folder or something
                if(objInputs[i] is MessagePayloadWrapper)
                {
                    MessagePayloadWrapper mpwPayloadWrapper = (MessagePayloadWrapper)objInputs[i];
                    UserInputGlobalMessage uimInputMessage = (UserInputGlobalMessage)mpwPayloadWrapper.m_smpPayload;

                    fdaOutFrameData.PeerInput[mpwPayloadWrapper.m_iChannelIndex] = SimInputManager.ProcessInput(fdaOutFrameData.PeerInput[mpwPayloadWrapper.m_iChannelIndex], uimInputMessage.m_bInputState);

                }
                else if (objInputs[i] is UserConnecionChange)
                {
                    UserConnecionChange uccUserConnectionChange = (UserConnecionChange)objInputs[i];

                    //clear all inputs for peers that have left
                    for(int j = 0; j < uccUserConnectionChange.m_iKickPeerChannelIndex.Length; j++)
                    {
                        fdaOutFrameData.PeerInput[uccUserConnectionChange.m_iKickPeerChannelIndex[j]] = SimInputManager.DefaultInput();
                    }
                }
                
            }

            //get existing hash
            byte[] bTickBytes = BitConverter.GetBytes((long)iTick);

            byte[] bExistingHash = fdaOutFrameData.InputHash;

            HashTools.MergeHashes(ref bExistingHash, bTickBytes);

            //combine all the hashes
            for (int i = 0; i < objInputs.Length; i++)
            {
                IInput iptInput = objInputs[i];
                HashTools.MergeHashes(ref bExistingHash, iptInput.GetHash());
            }

            fdaOutFrameData.InputHash = bExistingHash;

            return true;
        }
    }
}
