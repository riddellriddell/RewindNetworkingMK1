using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PeerEntryUIManager : MonoBehaviour
{
    //the identifier of the peer
    [SerializeField]
    private Text m_txtPeerID;

    [SerializeField]
    private Text m_txtPeerStateHash;

    [SerializeField]
    private Color m_colNotMatchingColour;

    [SerializeField]
    private Color m_colMatchingColour;

    //how long until the request for the state times out
    [SerializeField]
    private Text m_txtTimeUntilSegmentTimeOut;

    [SerializeField]
    private Image m_imgSegmentTimeOutBar;

    public void UpdateUI(SourcePeer sprSourcePeerData)
    {
        m_txtPeerID.text = sprSourcePeerData.m_lPeerID.ToString();

        m_txtPeerStateHash.text = sprSourcePeerData.m_lHashOfPeerState.ToString();

        m_txtPeerStateHash.color = sprSourcePeerData.m_bMatchesRequestHash ? m_colMatchingColour : m_colNotMatchingColour;

        m_txtTimeUntilSegmentTimeOut.text = sprSourcePeerData.m_tspTimeOutOfSegmentRequest.Seconds.ToString();

        float fPercentUntilTimeOut = (float)(sprSourcePeerData.m_tspTimeOutOfSegmentRequest.TotalSeconds / sprSourcePeerData.m_tspSegmentRequestDuration.TotalSeconds);

        m_imgSegmentTimeOutBar.fillAmount = fPercentUntilTimeOut;
    }

}
