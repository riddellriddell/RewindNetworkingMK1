using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Networking
{
    public class SimStateSyncUnitTest : BaseNetworkTestSetup
    {

        public int m_iSourceDataSize;

        public long m_lSourceDataAHash;

        public byte[] m_bSourceDataA;

        public DateTime m_dtmSourceDataTime;
        
        public override void SetupPeerPacketProcessors(NetworkConnection ncnPeerNetwork)
        {
            base.SetupPeerPacketProcessors(ncnPeerNetwork);

            ncnPeerNetwork.AddPacketProcessor(new SimStateSyncNetworkProcessor());
        }

        //set peer sim state source data
        public override void OnAllPeersConnected()
        {
            base.OnAllPeersConnected();

            RequestSimState();
        }

        public override void OnConnectedNetworkUpdate()
        {
            CheckForSimStateRequest();

            CheckIfRecievedData();
        }

        public void RequestSimState()
        {
            m_bSourceDataA = new byte[m_iSourceDataSize];

            //fill source 
            for (int i = 0; i < m_iSourceDataSize; i++)
            {
                m_bSourceDataA[i] = (byte)Random.Range(0, 255);
            }

            using (MD5 md5Hasher = MD5.Create())
            {
                m_lSourceDataAHash = BitConverter.ToInt64(md5Hasher.ComputeHash(m_bSourceDataA), 0);
            }

            m_dtmSourceDataTime = DateTime.UtcNow;

            List<long> lAuthorativePeers = new List<long>();
            for(int i = 1; i < m_ncnPeerNetworks.Count; i++)
            {
                lAuthorativePeers.Add(m_ncnPeerNetworks[i].m_lPeerID);
            }

            SimStateSyncNetworkProcessor ssnSimStateSync = m_ncnPeerNetworks[0].GetPacketProcessor<SimStateSyncNetworkProcessor>();

            ssnSimStateSync.RequestSimData(m_dtmSourceDataTime,lAuthorativePeers);
        }

        public void CheckForSimStateRequest()
        {
            for(int i = 0; i < m_ncnPeerNetworks.Count; i++)
            {
                SimStateSyncNetworkProcessor ssnSimStateSync = m_ncnPeerNetworks[i].GetPacketProcessor<SimStateSyncNetworkProcessor>();

                List<Tuple<DateTime,long>> tupRequestedSimStates =  ssnSimStateSync.GetRequestedTimeOfSimStates();

                if(tupRequestedSimStates.Count > 0)
                {
                    Debug.Log($"{tupRequestedSimStates.Count} requests for sim data recieved");
                }

                for(int j = 0; j < tupRequestedSimStates.Count; j++)
                {
                    if(tupRequestedSimStates[j].Item1 != m_dtmSourceDataTime)
                    {
                        Debug.Log("Bad sim time request");

                        continue;
                    }

                    Debug.Log($"Setting sim data to data from{m_dtmSourceDataTime} for peer {tupRequestedSimStates[j].Item2}");

                    ssnSimStateSync.SetSimDataForPeer(tupRequestedSimStates[j].Item2, m_bSourceDataA);
                }
            }
        }

        public void CheckIfRecievedData()
        {
            SimStateSyncNetworkProcessor ssnStateSync = m_ncnPeerNetworks[0].GetPacketProcessor<SimStateSyncNetworkProcessor>();

            if (ssnStateSync.m_bIsFullStateSynced == true)
            {
                long lsyncedDataHash = 0;

                using (MD5 md5Hasher = MD5.Create())
                {
                    lsyncedDataHash = BitConverter.ToInt64(md5Hasher.ComputeHash(ssnStateSync.m_bSimState), 0);
                }

                if(m_lSourceDataAHash == lsyncedDataHash)
                {
                    Debug.Log("Sate synced successfully");
                }
                else
                {
                    Debug.Log("Sync state not synchronised successfully");
                }
            }
        }
    }
}
