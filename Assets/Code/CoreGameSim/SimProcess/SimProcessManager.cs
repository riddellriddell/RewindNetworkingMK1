using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Sim
{
    public class SimProcessManager<TFrameData, TConstData, TSettingsData> where TFrameData: IFrameData
    {
        public bool m_bCheckForDeSync;

        protected  SortedList<int, ISimProcess<TFrameData, TConstData, TSettingsData>> m_spcSimProcesses = new SortedList<int, ISimProcess<TFrameData, TConstData, TSettingsData>>();

        protected SortedList<int, ISimSetupProcesses<TFrameData, TSettingsData>> m_sspSimSetupProcesses = new SortedList<int, ISimSetupProcesses<TFrameData, TSettingsData>>(); 

        public void AddSimProcess(ISimProcess<TFrameData, TConstData, TSettingsData> spcSimProcess)
        {
            m_spcSimProcesses.Add(spcSimProcess.Priotity, spcSimProcess);
        }

        public void AddSimSetupProcess(in ISimSetupProcesses<TFrameData, TSettingsData> sspSetupProcess)
        {
            m_sspSimSetupProcesses.Add(sspSetupProcess.Priority, sspSetupProcess);
        }

        public void ProcessFrameData(uint iTick, in TSettingsData sdaSettingsData, in TConstData cdaConstData, in TFrameData fdaBaseFrameData, in object[] objInputs, ref TFrameData fdaOutFrameData)
        {
            //copy existing frame data into new frame data
            fdaOutFrameData.ResetToState(fdaBaseFrameData);

            for (int i = 0; i < m_spcSimProcesses.Count; i++)
            {

                bool bProcessSuccelssfull = m_spcSimProcesses[i].ProcessFrameData(iTick, sdaSettingsData, cdaConstData, fdaBaseFrameData, objInputs, ref fdaOutFrameData);

                //check if process ran successfully 
                if(bProcessSuccelssfull == false)
                {
                    //throw error
                }

                if(m_bCheckForDeSync)
                {
                    //log desync values 
                }

            }
        }

        public void SetupInitalFrameData(uint iInitalFrameTick, in TSettingsData sdaSettingsData, long lFirstPeer, ref TFrameData fdaFirstFrameData)
        {
            //apply all the setup processes 
            for(int i = 0; i < m_sspSimSetupProcesses.Count; i++)
            {
                bool bSetupSetpResult = m_sspSimSetupProcesses[i].ApplySetupProcess(iInitalFrameTick, sdaSettingsData, lFirstPeer, ref fdaFirstFrameData);

                if(bSetupSetpResult == false)
                {
                    //throw error for bad setup
                }
            }
        }
    }
}