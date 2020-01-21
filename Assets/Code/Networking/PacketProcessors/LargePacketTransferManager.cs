using System;
using System.Collections.Generic;
using UnityEngine;

namespace Networking
{
    /// <summary>
    /// if a packet is too large to fit inside the mtu this packet processor breaks it up and reassebles it at the other end
    /// </summary>
    public class NetworkdLargePacketTransferManager : ManagedNetworkPacketProcessor<ConnectionLargePacketTransferManager>
    {
        public static int s_iStartBufferSize = 2000;

        public override int Priority { get; } = 12;

        public byte[] m_bSharedBuffer;

        public override void OnAddToNetwork(NetworkConnection ncnNetwork)
        {
            m_bSharedBuffer = new byte[s_iStartBufferSize];
            base.OnAddToNetwork(ncnNetwork);
        }

        protected override void AddDependentPacketsToPacketFactory(ClassWithIDFactory cifPacketFactory)
        {
            cifPacketFactory.AddType<LargePacket>(LargePacket.TypeID);
        }

    }

    public class ConnectionLargePacketTransferManager : ManagedConnectionPacketProcessor<NetworkdLargePacketTransferManager>
    {
        public override int Priority { get; } = 12;
        protected List<LargePacket> LargePacketSections { get; } = new List<LargePacket>();

        public override DataPacket ProcessPacketForSending(Connection conConnection, DataPacket pktOutputPacket)
        {
            //check is packet  larger than the mtu and will need splitting 
            if ((pktOutputPacket is LargePacket) == false && pktOutputPacket.PacketTotalSize >= conConnection.MaxPacketBytesToSend)
            {
                Debug.Log($"splitting large Packet: {pktOutputPacket.ToString()} of size:{pktOutputPacket.PacketTotalSize} ");

                //split packet and reassemble at other end
                List<LargePacket> lpkSplitPackets = SplitPacket(pktOutputPacket, conConnection.MaxPacketBytesToSend);

                //queue split packets to send 
                for (int i = 0; i < lpkSplitPackets.Count; i++)
                {
                    conConnection.QueuePacketToSend(lpkSplitPackets[i]);
                }

                //don't try and send packet because its beeing sent as split packets instead 
                return null;
            }

            return base.ProcessPacketForSending(conConnection, pktOutputPacket);
        }

        public override DataPacket ProcessReceivedPacket(Connection conConnection, DataPacket pktInputPacket)
        {
            if(pktInputPacket is LargePacket)
            {
                Debug.Log($"Processing LargePacket From {ParentConnection.m_lUserUniqueID} of size: {(pktInputPacket as LargePacket).PacketPayloadSize}");

                LargePacketSections.Add(pktInputPacket as LargePacket);

                //check if all the packet segments have arrived and the large packet can be decoded
                if (IsLargePacketListComplete(LargePacketSections))
                {
                    //
                    Debug.Log($"LargePacket From {ParentConnection.m_lUserUniqueID} merging {LargePacketSections.Count} large packet segments into source datapacket");

                    //decode the large packet from array of sub packets 
                    DataPacket dpkReconstructedPacket = CombineSplitPackets(LargePacketSections);

                    //process the reconstructed packet
                    conConnection.ProcessRecievedPacket(dpkReconstructedPacket);
                }

                return null;

            }

            return base.ProcessReceivedPacket(conConnection, pktInputPacket);
        }

        public bool IsLargePacketListComplete(List<LargePacket> lpkPacketSegments)
        {
            if (lpkPacketSegments[lpkPacketSegments.Count - 1].m_bIsLastPacketInSequence == 1)
            {
                return true;
            }

            return false;
        }

        public List<LargePacket> SplitPacket(DataPacket pktOutputPacket, int iMaxPacketSize)
        {
            //check if shared buffer is large enough 
            if (m_tParentPacketProcessor.m_bSharedBuffer.Length < pktOutputPacket.PacketTotalSize)
            {
                //resize to buffer to fit the large packet
                m_tParentPacketProcessor.m_bSharedBuffer = new byte[pktOutputPacket.PacketTotalSize];
            }

            List<LargePacket> lpkOutput = new List<LargePacket>();

            WriteByteStream wbsWriteStream = new WriteByteStream(m_tParentPacketProcessor.m_bSharedBuffer);

            byte bType = (byte)pktOutputPacket.GetTypeID;

            //encode the type of packet 
            ByteStream.Serialize(wbsWriteStream, ref bType);

            //encode packet into temp internal buffer
            pktOutputPacket.EncodePacket(wbsWriteStream);

            //divide packet up into sendable chunks 
            int iReadStartIndex = 0;

            while (iReadStartIndex < wbsWriteStream.ReadWriteHead)
            {
                //get packet to send data 
                LargePacket lpkLargePacket = ParentConnection.m_cifPacketFactory.CreateType<LargePacket>(LargePacket.TypeID);

                int iMTU = iMaxPacketSize - lpkLargePacket.HeaderSize;

                //calc number of bytes of the source payload to send
                int iReadCount = Mathf.Min(iMTU, wbsWriteStream.ReadWriteHead - iReadStartIndex);

                //select segment to send
                ArraySegment<byte> arsSegment = new ArraySegment<byte>(m_tParentPacketProcessor.m_bSharedBuffer, iReadStartIndex, iReadCount);

                lpkLargePacket.m_bPacketSegment.Clear();

                //add payload to packet
                lpkLargePacket.m_bPacketSegment.AddRange(arsSegment);

                //indicate this packet is not the last in the sequence
                lpkLargePacket.m_bIsLastPacketInSequence = 0;

                iReadStartIndex += iMTU;

                //check that packet size is within spec
                Debug.Assert(lpkLargePacket.PacketTotalSize <= iMaxPacketSize, $"Split packet size: {lpkLargePacket.PacketTotalSize} larger than allowd size{iMaxPacketSize}");

                lpkOutput.Add(lpkLargePacket);
            }

            //mark last packet in sequence
            lpkOutput[lpkOutput.Count - 1].m_bIsLastPacketInSequence = 1;

            return lpkOutput;
        }

        public DataPacket CombineSplitPackets(ICollection<LargePacket> colLargePakets)
        {
            //check that the buffer is big enough
            int iMinBufferSize = colLargePakets.Count * ParentConnection.m_iMaxBytesToSend;

            if (m_tParentPacketProcessor.m_bSharedBuffer.Length < iMinBufferSize)
            {
                m_tParentPacketProcessor.m_bSharedBuffer = new byte[iMinBufferSize];
            }

            int iWriteHead = 0;

            //copy packet data across 
            foreach (LargePacket lpkPacket in colLargePakets)
            {
                int iSubPacketSize = lpkPacket.m_bPacketSegment.Count;

                Array.Copy(lpkPacket.m_bPacketSegment.ToArray(), 0, m_tParentPacketProcessor.m_bSharedBuffer, iWriteHead, iSubPacketSize);

                iWriteHead += iSubPacketSize;
            }
            
            colLargePakets.Clear();

            ReadByteStream rbsReadStream = new ReadByteStream(m_tParentPacketProcessor.m_bSharedBuffer);

            //get the split packet type
            byte bPacketType = 0;
            ByteStream.Serialize(rbsReadStream, ref bPacketType);

            DataPacket dpkOutputPacket = ParentConnection.m_cifPacketFactory.CreateType<DataPacket>(bPacketType);

            dpkOutputPacket.DecodePacket(rbsReadStream);


            return dpkOutputPacket;
        }

        public override void OnConnectionReset()
        {
            LargePacketSections.Clear();

            base.OnConnectionReset();
        }
    }
}
