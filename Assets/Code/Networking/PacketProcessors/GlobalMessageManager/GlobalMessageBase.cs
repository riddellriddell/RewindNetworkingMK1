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
        public Tuple<byte, long>[] m_tupActionPerPeer;

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

            return iSize;
        }

        public static int DataSize(Tuple<byte, long> Input)
        {
            int iSize = 0;
            iSize += ByteStream.DataSize(Input.Item1);
            iSize += ByteStream.DataSize(Input.Item2);

            return iSize;
        }

        public static void Serialize(WriteByteStream wbsStream, ref VoteMessage Input)
        {
            int iCount = Input.m_tupActionPerPeer.Length;

            ByteStream.Serialize(wbsStream, ref iCount);

            for (int i = 0; i < iCount; i++)
            {
                Tuple<byte, long> tupVote = Input.m_tupActionPerPeer[i];

                NetworkingByteStream.Serialize(wbsStream, ref tupVote);
            }
        }

        public static void Serialize(ReadByteStream rbsStream, ref VoteMessage Input)
        {
            int iCount = 0;

            ByteStream.Serialize(rbsStream, ref iCount);

            if (Input.m_tupActionPerPeer == null || Input.m_tupActionPerPeer.Length != iCount)
            {
                Input.m_tupActionPerPeer = new Tuple<byte, long>[iCount];
            }

            for (int i = 0; i < iCount; i++)
            {
                Tuple<byte, long> tupVote = null;

                NetworkingByteStream.Serialize(rbsStream, ref tupVote);

                Input.m_tupActionPerPeer[i] = tupVote;
            }
        }

        public static void Serialize(WriteByteStream wbsStream, ref Tuple<byte, long> Input)
        {
            byte bItem1 = Input.Item1;
            long lItem2 = Input.Item2;

            ByteStream.Serialize(wbsStream, ref bItem1);
            ByteStream.Serialize(wbsStream, ref lItem2);
        }

        public static void Serialize(ReadByteStream rbsStream, ref Tuple<byte, long> Input)
        {
            byte bItem1 = 0;
            long lItem2 = 0;

            ByteStream.Serialize(rbsStream, ref bItem1);
            ByteStream.Serialize(rbsStream, ref lItem2);

            Input = new Tuple<byte, long>(bItem1, lItem2);
        }
    }

}