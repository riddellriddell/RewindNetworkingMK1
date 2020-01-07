using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Networking
{
    public class ChainLink
    {
        #region SentValues
        //signature by chain link creator validating link
        //contains hash of chain link, chain cycle index
        public byte[] m_bSignature;

        //the chain cycle this link was built from
        public uint m_iFromLinkCycleIndex;

        //hash of previouse chain index
        public byte[] m_bFromLinkHash;

        //list of all the inputs included in this chain and who the inputs belong to
        public List<GlobalMessageBase> m_gmbMessages;

        #endregion

        #region CalculatedValues
        //the max number of chain nodes that could have been added before this
        public uint m_iCyclesIndex;

        //the lenght of this chain
        public uint m_iChainLength;

        //the state of the message sim at the end of the chain
        public List<GlobalMessageChannelState> m_gcsChannelStates;

        #endregion

        #region LocalValues
        // is this branch accepted by channel as the true branch
        public List<bool> m_bIsChannelBranch;

        #endregion
    }
}
