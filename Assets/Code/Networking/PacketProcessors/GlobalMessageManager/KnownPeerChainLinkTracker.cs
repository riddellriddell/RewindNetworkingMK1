using System;
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
                ExistsOnLocalPeer
            }
            
            public SortingValue m_svaLinkTime;
            public long m_lLinkHash;
            public LinkState m_lksLinkState;
            public HashSet<long> m_setPeersWithLink;
            public Dictionary<long,System.DateTime> m_dicRequestedFrom;

        }
        
        public Dictionary<long, LinkTracker> m_dicTrackedLinks = new Dictionary<long, LinkTracker>();

        public SortingValue m_svaOldestValidTime = SortingValue.MinValue;
        
        //remove links older than base
        public List<long> RemoveTrackingOfLinksOlderThan(SortingValue svaOldestValidLink)
        {
            m_svaOldestValidTime = svaOldestValidLink;
            
            List<long> lstLinksToRemove = new List<long>();

            foreach (LinkTracker ltrLinkTracker in m_dicTrackedLinks.Values)
            {
                if (ltrLinkTracker.m_svaLinkTime < svaOldestValidLink)
                {
                    lstLinksToRemove.Add(ltrLinkTracker.m_lLinkHash);
                }
            }

            foreach (long lLinkToRemove in lstLinksToRemove)
            {
                m_dicTrackedLinks.Remove(lLinkToRemove);
            }

            return lstLinksToRemove;
        }
        
        //remove a peer and any chain links only they knew of from the list
        public void RemoveTrackingOfPeer(long lPeerID)
        {
            List<long> lstLinksToRemove = new List<long>();
            
            //loop through all tracked items and remove the peer
            foreach (LinkTracker ltrLink in m_dicTrackedLinks.Values)
            {
                if (ltrLink.m_lksLinkState != LinkTracker.LinkState.ExistsOnLocalPeer && ltrLink.m_setPeersWithLink.Contains(lPeerID))
                {
                    ltrLink.m_setPeersWithLink.Remove(lPeerID);

                    if (ltrLink.m_setPeersWithLink.Count == 0)
                    {
                        lstLinksToRemove.Add(ltrLink.m_lLinkHash);
                    }
                    else if(ltrLink.m_dicRequestedFrom.ContainsKey(lPeerID))
                    {
                        ltrLink.m_dicRequestedFrom.Remove(lPeerID);
                    }
                }
                
            }
            
            //remove all the links that now have no tracked external peer
            foreach (long lLinkHash in lstLinksToRemove)
            {
                m_dicTrackedLinks.Remove(lLinkHash);
            }
        }

        //Set link as received 
        public void SetLinkAsReceived(SortingValue svaLinkTime, long lLinkHash)
        {
            if (m_dicTrackedLinks.ContainsKey(lLinkHash))
            {
                LinkTracker ltrLink = m_dicTrackedLinks[lLinkHash];

                ltrLink.m_lksLinkState = LinkTracker.LinkState.ExistsOnLocalPeer;
                ltrLink.m_setPeersWithLink = null;
                ltrLink.m_dicRequestedFrom = null;
                
                m_dicTrackedLinks[lLinkHash] = ltrLink;
            }
            else
            {
                LinkTracker ltrLink = new LinkTracker();
                
                ltrLink.m_svaLinkTime = svaLinkTime;
                ltrLink.m_lLinkHash = lLinkHash;
                ltrLink.m_setPeersWithLink = null;
                ltrLink.m_dicRequestedFrom = null;
                ltrLink.m_lksLinkState = LinkTracker.LinkState.ExistsOnLocalPeer;
                
                m_dicTrackedLinks.Add(lLinkHash, ltrLink);
            }
        }

        //remove requests that are older than a given date
        public void RemoveRequestsOlderThanDate(DateTime dtmMaxRequestAge)
        {
            
            foreach (LinkTracker ltrLink in m_dicTrackedLinks.Values)
            {
                if (ltrLink.m_lksLinkState == LinkTracker.LinkState.ExistsOnLocalPeer)
                {
                    continue;
                }
                
                List<long> lstLinksToRemove = new List<long>();
                
                foreach (var kvpEntry in ltrLink.m_dicRequestedFrom)
                {
                    if (kvpEntry.Value < dtmMaxRequestAge)
                    {
                        lstLinksToRemove.Add(kvpEntry.Key);
                    }
                }
                foreach (long lPeer in lstLinksToRemove)
                {
                    ltrLink.m_dicRequestedFrom.Remove(lPeer);
                }
            }
        }
        
        
        //get list of all links not on local peer with less than x peers requested from
        public List<long> GetListOfMissingLinksOnLocalPeer(int iMinPeerRequests,DateTime dtmNewestTimeToRequestFor)
        {
            List<long> svaOutList = new List<long>();

            foreach (var ltrKnownLink in m_dicTrackedLinks.Values)
            {
                DateTime dtmTimeOfLink = new DateTime((long)(ltrKnownLink.m_svaLinkTime.m_lSortValueA), DateTimeKind.Utc); 
                
                if (ltrKnownLink.m_lksLinkState == LinkTracker.LinkState.ExistsOnLocalPeer || 
                    ltrKnownLink.m_dicRequestedFrom.Count >= iMinPeerRequests ||
                    dtmTimeOfLink > dtmNewestTimeToRequestFor)
                {
                    continue;
                }
                
                svaOutList.Add(ltrKnownLink.m_lLinkHash);
            }

            return svaOutList;
        }
        
        //add tracked link
        public void AddTrackingOfLink(SortingValue svaNewLink, long linkHash, long lPeerID)
        {
            //check if older than tracked links
            if (svaNewLink < m_svaOldestValidTime)
            {
                //we only want to track links in our expected range (newer than our chain base)
                return;
            }
            
            //check if already in list
            if (m_dicTrackedLinks.TryGetValue(linkHash, out LinkTracker link))
            {
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
                ltrLink.m_lLinkHash = linkHash;
                ltrLink.m_setPeersWithLink = new HashSet<long>();
                ltrLink.m_setPeersWithLink.Add(lPeerID);
                ltrLink.m_dicRequestedFrom = new Dictionary<long, DateTime>();
                ltrLink.m_lksLinkState = LinkTracker.LinkState.ExistsOnExternalPeer;
                
                m_dicTrackedLinks.Add(linkHash, ltrLink);
            }
        }
        
    }

    //list of all the chain links a peer has received 
    // public struct ChainLinksReceivedByPeer
    // {
    //     private HashSet<long> m_hstLinkHashes;
    //
    //     public AddHash(long lhash)
    //     {
    //         m_hstLinkHashes.Add(lhash);
    //     }
    //     
    //     
    // }
}