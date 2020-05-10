using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Networking
{
    public class SimStateSyncUnitTest : BaseNetworkTestSetup
    {

        public int m_iSourceDataASize;

        public long m_lSourceDataAHash;

        public byte[] m_bSourceDataA;

        public int m_iSourceDataBSize;

        public long m_lSourceDataBHash;

        public byte[] m_bSourceDataB;

        public long[] m_lSetDataHash;

        public float m_fChaceOfWrongData = 0.8f;

        public float m_fChanceOfCorrectingMistake = 0.05f;

        public DateTime m_dtmSourceDataTime;

        public override void SetupPeerPacketProcessors(NetworkConnection ncnPeerNetwork)
        {
            base.SetupPeerPacketProcessors(ncnPeerNetwork);

            NetworkingDataBridge ndbNetworkingDataBridge = new NetworkingDataBridge();

            ncnPeerNetwork.AddPacketProcessor(new SimStateSyncNetworkProcessor(ndbNetworkingDataBridge));
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
            GenerateSourceData(m_iSourceDataASize, out m_bSourceDataA, out m_lSourceDataAHash);

            GenerateSourceData(m_iSourceDataBSize, out m_bSourceDataB, out m_lSourceDataBHash);

            m_dtmSourceDataTime = DateTime.UtcNow;

            List<long> lAuthorativePeers = new List<long>();
            for (int i = 1; i < m_ncnPeerNetworks.Count; i++)
            {
                lAuthorativePeers.Add(m_ncnPeerNetworks[i].m_lPeerID);
            }

            m_lSetDataHash = new long[m_ncnPeerNetworks.Count];

            for (int i = 0; i < m_lSetDataHash.Length; i++)
            {
                m_lSetDataHash[i] = long.MinValue;
            }

            SimStateSyncNetworkProcessor ssnSimStateSync = m_ncnPeerNetworks[0].GetPacketProcessor<SimStateSyncNetworkProcessor>();

            ssnSimStateSync.RequestSimData(m_dtmSourceDataTime, lAuthorativePeers);
        }

        public void GenerateSourceData(int iSize, out byte[] bData, out long lHash)
        {
            bData = new byte[iSize];

            //fill source 
            for (int i = 0; i < iSize; i++)
            {
                bData[i] = (byte)Random.Range(0, 255);
            }

            using (MD5 md5Hasher = MD5.Create())
            {
                lHash = BitConverter.ToInt64(md5Hasher.ComputeHash(bData), 0);
            }
        }

        public void CheckForSimStateRequest()
        {
            for (int i = 0; i < m_ncnPeerNetworks.Count; i++)
            {


                SimStateSyncNetworkProcessor ssnSimStateSync = m_ncnPeerNetworks[i].GetPacketProcessor<SimStateSyncNetworkProcessor>();

                List<Tuple<DateTime, long>> tupRequestedSimStates = ssnSimStateSync.GetRequestedTimeOfSimStates();

                if (tupRequestedSimStates.Count == 0)
                {
                    continue;
                }

                if (m_lSetDataHash[i] == long.MinValue)
                {
                    Debug.Log($"{tupRequestedSimStates.Count} requests for sim data recieved");

                    bool bSetCorrectData = Random.Range(0.0f, 1.0f) > m_fChaceOfWrongData;

                    m_lSetDataHash[i] = bSetCorrectData ? m_lSourceDataAHash : m_lSourceDataBHash;

                    for (int j = 0; j < tupRequestedSimStates.Count; j++)
                    {
                        if (tupRequestedSimStates[j].Item1 != m_dtmSourceDataTime)
                        {
                            Debug.Log("Bad sim time request");

                            continue;
                        }

                        Debug.Log($"Setting sim data to data from{m_dtmSourceDataTime} for peer {tupRequestedSimStates[j].Item2}");

                        ssnSimStateSync.SetSimDataForPeer(tupRequestedSimStates[j].Item2, tupRequestedSimStates[j].Item1, bSetCorrectData ? m_bSourceDataA : m_bSourceDataB);
                    }

                }
                else if (m_lSetDataHash[i] != m_lSourceDataAHash)
                {
                    //check if should switch to correct data 
                    bool bSwitchToCorrectData = Random.Range(0.0f, 1.0f) < (m_fChanceOfCorrectingMistake * Time.deltaTime);

                    if (bSwitchToCorrectData)
                    {
                        m_lSetDataHash[i] = m_lSourceDataAHash;

                        for (int j = 0; j < tupRequestedSimStates.Count; j++)
                        {
                            if (tupRequestedSimStates[j].Item1 != m_dtmSourceDataTime)
                            {
                                Debug.Log("Bad sim time request");

                                continue;
                            }

                            Debug.Log($"Setting sim data to data from{m_dtmSourceDataTime} for peer {tupRequestedSimStates[j].Item2}");

                            ssnSimStateSync.SetSimDataForPeer(tupRequestedSimStates[j].Item2, tupRequestedSimStates[j].Item1, m_bSourceDataA);
                        }
                    }
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

                if (m_lSourceDataAHash == lsyncedDataHash)
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
