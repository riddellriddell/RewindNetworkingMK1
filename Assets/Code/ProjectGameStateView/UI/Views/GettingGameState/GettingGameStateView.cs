using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public struct GetStateTimeOut
{
    public DateTime m_dtmGetStateStartTime;
    public TimeSpan m_tspTimeOutTime;
}

public struct SourcePeer
{
    public long m_lPeerID;
    public long m_lHashOfPeerState;
    public bool m_bMatchesRequestHash;
    public long m_iIndexOfRequestedSegment;
    public TimeSpan m_tspTimeOutOfSegmentRequest;
    public TimeSpan m_tspSegmentRequestDuration;

    public long m_strPeerState;
}

public class GettingGameStateView : MonoBehaviour
{
    //the boundry in the UI to draw in

    //how long until the all attempt to get the state times out 
    public GetStateTimeOut m_stoAllGetStateTimeOutData;

    //how long until the current atempt to get the state times out 
    public GetStateTimeOut m_stoCurrentGetStateAttemptTimeOutData;

    public long m_lGameStateHash;

    private List<SourcePeer> m_lstSourcePeerData;

    //get state time out text
    [SerializeField]
    private Text m_txtStageOfGettingState;

    //get state time out text
    [SerializeField]
    private Text m_txtGlobalGetStateTimeOutTime;

    //get state time out bar
    [SerializeField]
    private Image m_imgGlobalGetStateTimeOutBar;

    //the hash of the target state 
    [SerializeField]
    private Text m_txtStateHashValue;

    //----------- The state of all the segments of the game state -----------
    //[SerializeField]
    //private GameObject m_objSegmentPrototype;

   // [SerializeField]
   // private GameObject m_objParentOfSegmentStates;


    //----------- The settings used for getting sim state -------------------

    //shows what world time we are trying to get the state of relative to current time
    [SerializeField]
    private Text m_txtTimeDiferenceToRequestedState;

    //per peer entry 

    //the parent object to put all the peers under
    [SerializeField]
    private GameObject m_objPeerListParent;

    [SerializeField]
    //the prototype object for the peer list
    private PeerEntryUIManager m_pemPeerUIEntryPrototype;

    //list of all the peers this peer is trying to download the game state from
    private List<PeerEntryUIManager> m_lstSourcePeerEntryList = new List<PeerEntryUIManager>();

    //shows what world time we are trying to get the state of relative to current time
    [SerializeField]
    private Text m_txtGetStateAttemptTimeOutTime;


    //get state time out bar
    [SerializeField]
    private Image m_imgGetStateAttemptTimeOutBar;


    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    //updates the UI with how much time until the get state times out and the conenction resets 
    public void UpdateAllGetStateTimeOut(DateTime dtmNow)
    {
        TimeSpan tspTimeSinceStart = dtmNow - m_stoAllGetStateTimeOutData.m_dtmGetStateStartTime;

        double timeOutTime = m_stoAllGetStateTimeOutData.m_tspTimeOutTime.TotalMilliseconds > 0 ? m_stoAllGetStateTimeOutData.m_tspTimeOutTime.TotalMilliseconds : 1.0f;

        float fTimeOutPercent = (float)tspTimeSinceStart.TotalMilliseconds / (float)timeOutTime;

        m_txtGlobalGetStateTimeOutTime.text = $"{(int)tspTimeSinceStart.TotalSeconds} seconds until time out";

        m_imgGlobalGetStateTimeOutBar.fillAmount = fTimeOutPercent;
    }

    public void UpdateGetStateAttemptTimeOut(DateTime dtmNow)
    {
        TimeSpan tspTimeSinceStart = dtmNow - m_stoCurrentGetStateAttemptTimeOutData.m_dtmGetStateStartTime;

        double timeOutTime = m_stoCurrentGetStateAttemptTimeOutData.m_tspTimeOutTime.TotalMilliseconds > 0 ? m_stoCurrentGetStateAttemptTimeOutData.m_tspTimeOutTime.TotalMilliseconds : 1.0f;

        float fTimeOutPercent = (float)tspTimeSinceStart.TotalMilliseconds / (float)timeOutTime;

        m_txtGlobalGetStateTimeOutTime.text = $"{(int)tspTimeSinceStart.TotalSeconds} seconds until time out";

        m_imgGlobalGetStateTimeOutBar.fillAmount = fTimeOutPercent;
    }

    public void UpdatePeerStates(List<SourcePeer> lstNewPeerStates)
    {
        //check if there are enough times
        for(int i = m_lstSourcePeerEntryList.Count; i < lstNewPeerStates.Count; i++)
        {
            //create new items to match the numbers 
            PeerEntryUIManager pemPeerManager = GameObject.Instantiate<PeerEntryUIManager>(m_pemPeerUIEntryPrototype, m_objPeerListParent.transform);
            m_lstSourcePeerEntryList.Add(pemPeerManager);
        }

        //hide excess item
        for(int i = 0; i < m_lstSourcePeerEntryList.Count; i++)
        {
            bool bIsNeeded = i < lstNewPeerStates.Count;

            m_lstSourcePeerEntryList[i].gameObject.SetActive(bIsNeeded);
        }

        //update UI items
        for(int i = 0; i < lstNewPeerStates.Count; i++)
        {
            m_lstSourcePeerEntryList[i].UpdateUI(lstNewPeerStates[i]);
        }
    }
}
