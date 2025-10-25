using SharedTypes;
using System;
using Utility;

namespace Networking
{  
    //this message indicates a peer is voting to kick another peer
    public class VoteMessage : GlobalMessageBase
    {
        public static int TypeID { get; set; } = int.MinValue;

        public override int TypeNumber
        {
            get
            {
                return VoteMessage.TypeID;
            }
            set
            {
                TypeID = value;
            }
        }

        //the vote per peer
        //first value = action to perform (0 = kick, 1 = join)
        //second value = target peer
        //third value = reason
        public Tuple<byte, long, string>[] m_tupActionPerPeer;
        
        //single action vote, this is a parallel system for kicking and joining players
        //the goal is to replace the more complicated and bug prone system above
        public GlobalMessageChannelState.ChannelVote.VoteType m_vtaVoteAction;
        public long m_lPeerID;
        
        public override void Serialize(ReadByteStream rbsByteStream)
        {
            VoteMessage vmsMessage = this;

            NetworkingByteStream.Serialize(rbsByteStream, ref vmsMessage);
        }

        public override void Serialize(WriteByteStream wbsByteStream)
        {
            VoteMessage vmsMessage = this;

            NetworkingByteStream.Serialize(wbsByteStream, ref vmsMessage);
        }

        public override int DataSize()
        {
            return NetworkingByteStream.DataSize(this);
        }
    }

    //serialization for vote message 
    public partial class NetworkingByteStream
    {
        public static int DataSize(VoteMessage Input)
        {
            int iSize = 0;

            iSize += ByteStream.DataSize(Input.m_tupActionPerPeer.Length);

            for (int i = 0; i < Input.m_tupActionPerPeer.Length; i++)
            {
                iSize += NetworkingByteStream.DataSize(Input.m_tupActionPerPeer[i]);
            }
            
            //add the size of the new vote system changes
            iSize += ByteStream.DataSize(Input.m_lPeerID);
            iSize += ByteStream.DataSize((Byte)Input.m_vtaVoteAction);

            return iSize;
        }

        public static int DataSize(Tuple<byte, long, string> Input)
        {
            int iSize = 0;
            iSize += ByteStream.DataSize(Input.Item1);
            iSize += ByteStream.DataSize(Input.Item2);
            iSize += ByteStream.DataSize(Input.Item3);

            return iSize;
        }

        public static void Serialize(WriteByteStream wbsStream, ref VoteMessage Input)
        {
            int iCount = Input.m_tupActionPerPeer.Length;

            ByteStream.Serialize(wbsStream, ref iCount);

            for (int i = 0; i < iCount; i++)
            {
                Tuple<byte, long, string> tupVote = Input.m_tupActionPerPeer[i];

                NetworkingByteStream.Serialize(wbsStream, ref tupVote);
            }
            
            Byte bVoteAction = (Byte)Input.m_vtaVoteAction;
            
            ByteStream.Serialize(wbsStream, ref bVoteAction);
            ByteStream.Serialize(wbsStream, ref Input.m_lPeerID);
        }

        public static void Serialize(ReadByteStream rbsStream, ref VoteMessage Input)
        {
            int iCount = 0;

            ByteStream.Serialize(rbsStream, ref iCount);

            if (Input.m_tupActionPerPeer == null || Input.m_tupActionPerPeer.Length != iCount)
            {
                Input.m_tupActionPerPeer = new Tuple<byte, long, string>[iCount];
            }

            for (int i = 0; i < iCount; i++)
            {
                Tuple<byte, long, string> tupVote = null;

                NetworkingByteStream.Serialize(rbsStream, ref tupVote);

                Input.m_tupActionPerPeer[i] = tupVote;
            }

            Byte bVoteAction = 0;
            
            ByteStream.Serialize(rbsStream, ref bVoteAction);
            
            //serialize the new simple vote system
            Input.m_vtaVoteAction = (GlobalMessageChannelState.ChannelVote.VoteType)bVoteAction;

            ByteStream.Serialize(rbsStream, ref Input.m_lPeerID);
        }

        public static void Serialize(WriteByteStream wbsStream, ref Tuple<byte, long, string> Input)
        {
            byte bItem1 = Input.Item1;
            long lItem2 = Input.Item2;
            string strItem3 = Input.Item3;

            ByteStream.Serialize(wbsStream, ref bItem1);
            ByteStream.Serialize(wbsStream, ref lItem2);
            ByteStream.Serialize(wbsStream, ref strItem3);
        }

        public static void Serialize(ReadByteStream rbsStream, ref Tuple<byte, long, string> Input)
        {
            byte bItem1 = 0;
            long lItem2 = 0;
            string strItem3 = "";

            ByteStream.Serialize(rbsStream, ref bItem1);
            ByteStream.Serialize(rbsStream, ref lItem2);

            Input = new Tuple<byte, long, string>(bItem1, lItem2, strItem3);
        }
    }

}