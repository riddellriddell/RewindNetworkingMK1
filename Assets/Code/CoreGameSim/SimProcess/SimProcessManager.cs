using SharedTypes;
using Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Sim
{
    public class SimProcessManager<TFrameData, TConstData, TSettingsData> where TFrameData: IFrameData
    {
        public bool m_bCheckForDeSync = false;

        public bool m_bHashFrameData = false;

        protected  SortedList<int, ISimProcess<TFrameData, TConstData, TSettingsData>> m_spcSimProcesses = new SortedList<int, ISimProcess<TFrameData, TConstData, TSettingsData>>();

        protected SortedList<int, ISimSetupProcesses<TFrameData, TSettingsData>> m_sspSimSetupProcesses = new SortedList<int, ISimSetupProcesses<TFrameData, TSettingsData>>(); 

        public void AddSimProcess(ISimProcess<TFrameData, TConstData, TSettingsData> spcSimProcess)
        {
            m_spcSimProcesses.Add(spcSimProcess.Priority, spcSimProcess);
        }

        public void AddSimSetupProcess(in ISimSetupProcesses<TFrameData, TSettingsData> sspSetupProcess)
        {
            m_sspSimSetupProcesses.Add(sspSetupProcess.Priority, sspSetupProcess);
        }

        public void ProcessFrameData(uint iTick, in TSettingsData sdaSettingsData, in TConstData cdaConstData, in TFrameData fdaBaseFrameData, in IInput[] objInputs, ref TFrameData fdaOutFrameData)
        {
            //copy existing frame data into new frame data
            fdaOutFrameData.ResetToState(fdaBaseFrameData);

            byte[] bStartHash = new byte[8];
            byte[] bOldStartHashShort = new byte[4];
            byte[] bOldStartHash = new byte[8];

            if (m_bHashFrameData || m_bCheckForDeSync)
            {
                //check that both states are the same
                fdaBaseFrameData.GetHash(bOldStartHash);
                fdaBaseFrameData.GetHash(bOldStartHashShort);
                fdaOutFrameData.GetHash(bStartHash);


                if (!HashTools.CompareHashes(bStartHash, bOldStartHash))
                {
                    //throw error
                    Debug.LogError($"Error game state copy did not work correctly");
                }
            }




            //hash inputs 
            IPeerInputFrameData ifdOutFrameDataInput = (IPeerInputFrameData)fdaOutFrameData;

            if (m_bCheckForDeSync)
            {
                for(int i = 0; i < objInputs.Length; i++)
                {
                    byte[] bSingleInputHash = objInputs[i].GetHash();

                    byte[] bExistingInputHash = ifdOutFrameDataInput.InputHash;

                    HashTools.MergeHashes(ref bExistingInputHash, bSingleInputHash);

                    ifdOutFrameDataInput.InputHash = bExistingInputHash;
                }
            }

           

            for (int i = 0; i < m_spcSimProcesses.Count; i++)
            {
                bool bProcessSuccelssfull = m_spcSimProcesses.Values[i].ProcessFrameData(iTick, sdaSettingsData, cdaConstData, fdaBaseFrameData, objInputs, ref fdaOutFrameData);

                //check if process ran successfully 
                if(bProcessSuccelssfull == false)
                {
                    //throw error
                    Debug.LogError($"Error happened applying sim process {m_spcSimProcesses.Values[i].ProcessName}");
                }

                if(m_bCheckForDeSync)
                {
                    byte[] bHash = new byte[4];

                    fdaOutFrameData.GetHash(bHash);

                    //log desync values 
                    DataHashValidation.LogDataHash(bOldStartHashShort, (byte)i, iTick, bHash, m_spcSimProcesses.Values[i].ProcessName);
                }

            }
        }

        public void SetupInitalFrameData(uint iInitalFrameTick, in TSettingsData sdaSettingsData, long lFirstPeer, ref TFrameData fdaFirstFrameData)
        {
            //apply all the setup processes 
            for(int i = 0; i < m_sspSimSetupProcesses.Count; i++)
            {
                bool bSetupSetpResult = m_sspSimSetupProcesses.Values[i].ApplySetupProcess(iInitalFrameTick, sdaSettingsData, lFirstPeer, ref fdaFirstFrameData);

                if(bSetupSetpResult == false)
                {
                    //throw error for bad setup
                }
            }
        }
    }
}