using System.Collections.Generic;

namespace Networking
{
    //this class is used to track what chain links peers have received vs what the local peer has
    //if a peer receives a chain link and the local peer does not this class is part of the system to request that link 
    //from the other peer
    public class KnownPeerChainLinkTracker
    {
        public struct LinkTracker
        {
            public enum LinkState
            {
                ExistsOnExternalPeer,
                RequestedFromPeer,
                ExistsOnLocalPeer
            }
            
            public SortingValue m_svaLinkTime;
            public LinkState m_lksLinkState;
            public HashSet<long> m_setPeersWithLink;
            public HashSet<long> m_setRequestedFrom;

        }
        
        public Dictionary<SortingValue, LinkTracker> m_dicTrackedLinks = new Dictionary<SortingValue, LinkTracker>();

        public SortingValue m_svaOldestValidTime = SortingValue.MinValue;
        
        //remove links older than base
        void RemoveTrackingOfLinksOlderThan(SortingValue svaOldestValidLink)
        {
            m_svaOldestValidTime = svaOldestValidLink;
            
            List<SortingValue> lstLinksToRemove = new List<SortingValue>();

            foreach (SortingValue svaLinkTime in m_dicTrackedLinks.Keys)
            {
                if (svaLinkTime < svaOldestValidLink)
                {
                    lstLinksToRemove.Add(svaLinkTime);
                }
            }

            foreach (SortingValue svaLinkToRemove in lstLinksToRemove)
            {
                m_dicTrackedLinks.Remove(svaLinkToRemove);
            }
        }
        
        //remove a peer and any chain links only they knew of from the list
        void RemoveTrackingOfPeer(long lPeerID)
        {
            List<SortingValue> lstLinksToRemove = new List<SortingValue>();
            
            //loop through all tracked items and remove the peer
            foreach (LinkTracker ltrLink in m_dicTrackedLinks.Values)
            {
                if (ltrLink.m_lksLinkState != LinkTracker.LinkState.ExistsOnLocalPeer && ltrLink.m_setPeersWithLink.Contains(lPeerID))
                {
                    ltrLink.m_setPeersWithLink.Remove(lPeerID);

                    if (ltrLink.m_setPeersWithLink.Count == 0)
                    {
                        lstLinksToRemove.Add(ltrLink.m_svaLinkTime);
                    }
                }
            }
            
            //remove all the links that now have no tracked external peer
            foreach (SortingValue svaLinkTime in lstLinksToRemove)
            {
                m_dicTrackedLinks.Remove(svaLinkTime);
            }
        }

        //Set link as received 
        void SetLinkAsReceived(SortingValue svaReceivedLink)
        {
            //check if older than tracked links
            if (svaReceivedLink < m_svaOldestValidTime)
            {
                //we only want to track links in our expected range (newer than our chain base)
                return;
            }
            
            if (m_dicTrackedLinks.ContainsKey(svaReceivedLink))
            {
                LinkTracker ltrLink = m_dicTrackedLinks[svaReceivedLink];

                ltrLink.m_lksLinkState = LinkTracker.LinkState.ExistsOnLocalPeer;

                ltrLink.m_setRequestedFrom = null;
                
                m_dicTrackedLinks[svaReceivedLink] = ltrLink;
            }
            else
            {
                LinkTracker ltrLink = new LinkTracker();
                
                ltrLink.m_svaLinkTime = svaReceivedLink;
                ltrLink.m_setPeersWithLink = null;
                ltrLink.m_lksLinkState = LinkTracker.LinkState.ExistsOnLocalPeer;
                
                m_dicTrackedLinks.Add(svaReceivedLink, ltrLink);
            }
        }

        void SetLinkRequestAsFailed(SortingValue svaFailedLink, long lPeerRequestedFromID)
        {
            //check if link time is still valid 
            if (svaFailedLink < m_svaOldestValidTime)
            {
                //link is no longer being tracked so we can ignore the failure
            }
            
            //get the link tracker
            if (m_dicTrackedLinks.ContainsKey(svaFailedLink))
            {
                LinkTracker ltrLink = m_dicTrackedLinks[svaFailedLink];
                
                //check if peer was in list this link was requested from
                if (ltrLink.m_lksLinkState == LinkTracker.LinkState.RequestedFromPeer)
                {
                    if (ltrLink.m_setRequestedFrom.Contains(lPeerRequestedFromID))
                    {
                        ltrLink.m_setRequestedFrom.Remove(lPeerRequestedFromID);

                        if (ltrLink.m_setRequestedFrom.Count == 0)
                        {
                            //no peers left that this link was requested from 
                            ltrLink.m_lksLinkState = LinkTracker.LinkState.ExistsOnLocalPeer;
                        }
                    }
                }
            }
            else
            {
                //should not be here, either the link was removed due to being too old or the link was never received
            }
        }
        
        //get list of all links not on local peer with less than x peers requested from
        List<SortingValue> GetListOfMissingLinksOnLocalPeer(int iMinPeerRequests)
        {
            List<SortingValue> svaOutList = new List<SortingValue>();

            foreach (var ltrKnownLink in m_dicTrackedLinks.Values)
            {
                if (ltrKnownLink.m_lksLinkState == LinkTracker.LinkState.ExistsOnLocalPeer || ltrKnownLink.m_setRequestedFrom.Count <= iMinPeerRequests)
                {
                    continue;
                }
                
                svaOutList.Add(ltrKnownLink.m_svaLinkTime);
            }

            return svaOutList;
        }
        
        //add tracked link
        void AddTrackingOfLink(SortingValue svaNewLink, long lPeerID)
        {
            //check if older than tracked links
            if (svaNewLink < m_svaOldestValidTime)
            {
                //we only want to track links in our expected range (newer than our chain base)
                return;
            }
            
            //check if already in list
            if (m_dicTrackedLinks.ContainsKey(svaNewLink))
            {
                LinkTracker link = m_dicTrackedLinks[svaNewLink];
                
                //check if we have it locally 
                if (link.m_lksLinkState == LinkTracker.LinkState.ExistsOnLocalPeer)
                {
                    //no need to do anything
                }
                else
                {
                    //add to list of peers that have this link
                    link.m_setPeersWithLink.Add(lPeerID);
                }
                
            }
            else
            {
                //create a new link tracker so the local peer can know where to look
                //if it wants to get links of fellow peers
                LinkTracker ltrLink = new LinkTracker();
                
                //set the sorting value for peer
                ltrLink.m_svaLinkTime = svaNewLink;
                ltrLink.m_setPeersWithLink = new HashSet<long>();
                ltrLink.m_setPeersWithLink.Add(lPeerID);
                ltrLink.m_lksLinkState = LinkTracker.LinkState.ExistsOnExternalPeer;
                
                m_dicTrackedLinks.Add(svaNewLink, ltrLink);
            }
        }
        
    }

    //list of all the chain links a peer has received 
    public struct ChainLinksReceivedByPeer
    {
        private HashSet<SortingValue> m_hstLinkSortingValues;
    }
}