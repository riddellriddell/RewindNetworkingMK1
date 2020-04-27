namespace Networking
{
    //this class creates a queue of valid messages 
    public class GlobalSimMessageBuffer
    {
        public struct UserConnecionChange
        {
            public long[] m_lJoinPeerID;
            public int[] m_iJoinPeerChannelIndex;

            public long[] m_lKickPeerID;
            public int[] m_iKickPeerChannelIndex;

            public UserConnecionChange(int iKickCount, int iJoinCount)
            {
                m_lJoinPeerID = new long[iJoinCount];
                m_iJoinPeerChannelIndex = new int[iJoinCount];

                m_lKickPeerID = new long[iKickCount];
                m_iKickPeerChannelIndex = new int[iKickCount];
            }

        }

        public struct MessagePayloadWrapper
        {
            public long m_lPeerID;

            public int m_iChannelIndex;

            public ISimMessagePayload m_smpPayload;

        }

        //all messages before this are guaranteed not to change (kinda, if they do the local peer is desynched)
        public SortingValue m_svaSafeMessageTime;

        //the most recent time messages were recieved from all peers 
        public SortingValue m_svaAllRecievedTime;

        //queue of all the messages 
        public SortedRandomAccessQueue<SortingValue, object> m_squMessageQueue;

        //queue this message discarding all the messages in the buffer that were older than this message
        public void QueueSimMessage(SortingValue svaTime, long lPeerID, int iChannelIndex, ISimMessagePayload smpMessage)
        {
            MessagePayloadWrapper mprMessage = new MessagePayloadWrapper()
            {
                m_lPeerID = lPeerID,
                m_iChannelIndex = iChannelIndex,
                m_smpPayload = smpMessage
            };

            m_squMessageQueue.EnterPurgeInsert(svaTime, mprMessage);
        }

        public void QueuePlayerChangeMessage(SortingValue svaEventTimt, UserConnecionChange uccConnectionChange)
        {
            m_squMessageQueue.EnterPurgeInsert(svaEventTimt, uccConnectionChange);
        }

        public void Clear(SortingValue svaClearUpTo)
        {
            m_squMessageQueue.ClearTo(svaClearUpTo);
        }

    }
}
