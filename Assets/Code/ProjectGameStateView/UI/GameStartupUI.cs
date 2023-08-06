using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity;
using UnityEngine.UI;
using System;
using Networking;

public class GameStartupUI : MonoBehaviour
{
    [SerializeField]
    protected Text m_txtGameStataOut;

    [SerializeField]
    protected GettingGameStateView m_ggsGettingGameStateView;

    public void UpdateGameState(string strGameState)
    {       
        m_txtGameStataOut.text = strGameState + "/n" + m_txtGameStataOut.text;
    }

    //this class is the glue that extracts the data needed from the networking layer
    //and injects it into the ui layer which does not know about the networking or game code
    public void UpdateGetGameState(NetworkConnection nwcNetworkConnection)
    {
        //check if setup at all
        if(m_ggsGettingGameStateView == null)
        {
            //early out if visuals not connected
            return;
        }

        //get the state sync code 
        SimStateSyncNetworkProcessor sssSimStateSync = nwcNetworkConnection.GetPacketProcessor<SimStateSyncNetworkProcessor>();

        //get the syncronised time value
        TimeNetworkProcessor tnpNetworkTime = nwcNetworkConnection.GetPacketProcessor<TimeNetworkProcessor>();

        //update when all attempts will time out 

        //update when the current request will time out
        m_ggsGettingGameStateView.m_stoCurrentGetStateAttemptTimeOutData.m_tspTimeOutTime = sssSimStateSync.StateRequestTimeOut;
        m_ggsGettingGameStateView.m_stoCurrentGetStateAttemptTimeOutData.m_dtmGetStateStartTime = sssSimStateSync.m_dtmRequestTimeOut;

        //set the hash of the intended game state 
        m_ggsGettingGameStateView.m_lGameStateHash = sssSimStateSync.m_lAgreedSimHash;

        //list all the peers that requested 
        List<SourcePeer> lstPeerStates = new List<SourcePeer>();

        //loop throuhg all authorative peers 
        for(int i = 0; i < sssSimStateSync.m_lAuthorativePeers.Count; i++)
        {
            SourcePeer sprPeerData = new SourcePeer();

            if(sssSimStateSync.ChildConnectionProcessors.TryGetValue(sssSimStateSync.m_lAuthorativePeers[i], out SimStateSyncConnectionProcessor sscSyncConnection))
            {
                //get the connection manager for the state sync for the peer 
                sprPeerData.m_lPeerID = sssSimStateSync.m_lAuthorativePeers[i];

                //update the hash for the peer at the other end 
                sprPeerData.m_lHashOfPeerState = sscSyncConnection.m_lInTotalStateHash;

                //get if the peer has matching hash
                sprPeerData.m_bMatchesRequestHash = sscSyncConnection.m_lInTotalStateHash == sssSimStateSync.m_lAgreedSimHash;

                //the segment index requested from the peer
                sprPeerData.m_iIndexOfRequestedSegment = sscSyncConnection.m_sRequestedSegment;

                //update when the state request for this peer times out 
                sprPeerData.m_tspTimeOutOfSegmentRequest = sscSyncConnection.m_dtmRequestTimeOut - tnpNetworkTime.NetworkTime;

                //the max time a segment request can take
                sprPeerData.m_tspSegmentRequestDuration = sssSimStateSync.SegmentRequestTimeOut;

                lstPeerStates.Add(sprPeerData);

            }
        }

        m_ggsGettingGameStateView.UpdatePeerStates(lstPeerStates);
    }

    public void OnEnterState()
    {
        gameObject?.SetActive(true);
    }

    public void OnExitState()
    {
        gameObject?.SetActive(false);
    }
}
