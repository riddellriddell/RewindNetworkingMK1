using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedTypes
{
    public interface ILocalPeerProvider
    {
        // get the local peer, returns long.max if there is no local peer
        long GetLocalPeerID();
    }
}
