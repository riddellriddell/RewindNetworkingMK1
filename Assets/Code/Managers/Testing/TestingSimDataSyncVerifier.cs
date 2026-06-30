using Networking;
using SharedTypes;
using Sim;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;
using Utility;

public class TestingSimDataSyncVerifier<TFrameData> where TFrameData : IFrameData
{
    public enum RegistrationType
    {
        SimFinalState,
        FirstStateOnDataBridge
    }
    
    //this is the final hash of the data at a tick for a peer, it is recorded when a peer 
    //deletes states off the sim state buffer and represents what a peer has decided the world
    //is at a given tick 
    public static BaseDataSyncVerifier<uint, Tuple<long,RegistrationType>, TFrameData> s_dsvFinalDataStateForPeer = new BaseDataSyncVerifier<uint, Tuple<long,RegistrationType>, TFrameData>();

    public static void VerifyData(uint iTick, ref TFrameData fdaData, long lPeerID, RegistrationType rgtTypeOfReg, int iTrackingWindowSize)
    {
        int iSize =  fdaData.GetSize();

        WriteByteStream wbsNetworking = new WriteByteStream(iSize);

        fdaData.Encode(wbsNetworking);

        long lHash = 0;

        //using (MD5 md5 = MD5.Create())
        //{
        //    lHash = BitConverter.ToInt64(md5.ComputeHash(wbsNetworking.GetData()), 0);
        //}

        byte[] bInputHash = ((IPeerInputFrameData)fdaData).InputHash;

        if(bInputHash.Length < 8)
        {
            Debug.LogError("input hash wrong size");
        }

        lHash = BitConverter.ToInt64(bInputHash);

        Tuple<long, RegistrationType> tuple = new Tuple<long, RegistrationType>(lPeerID, rgtTypeOfReg); 

        //store the hash for the entire state for checking later
        BaseDataSyncVerifier< uint, Tuple<long,RegistrationType>, TFrameData>.Result rltRegisterResult = s_dsvFinalDataStateForPeer.RegisterData(lHash, fdaData, iTick, tuple);

        if (rltRegisterResult.m_bSuccess == false)
        {
            string strExistingIDs = "";
        
            for(int i = 0; i < rltRegisterResult.m_lstConflictingEntries.Count; i++)
            {
                strExistingIDs += $"[{rltRegisterResult.m_lstConflictingEntries[i].Item1}|{rltRegisterResult.m_lstConflictingEntries[i].Item2}], ";
            }
        
            Debug.LogError($"New data entry hash: {lHash} does not match existing entry for datapoint at timestamp {iTick} the new data was added by peer {lPeerID} and is of rego type {rgtTypeOfReg} and there are {rltRegisterResult.m_lstConflictingEntries.Count} existing hashes with the following info: {strExistingIDs}");
        }

        s_dsvFinalDataStateForPeer.CleanUpOldEntries(iTick - (uint)(iTrackingWindowSize));
    }
}
