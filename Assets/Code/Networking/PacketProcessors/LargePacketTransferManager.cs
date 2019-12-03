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
        public override int Priority { get; } = 12;

        public byte[] m_bSharedBuffer;

        public override DataPacket ProcessPacketForSending(long lUserID, DataPacket pktOutputPacket)
        {
            return base.ProcessPacketForSending(lUserID, pktOutputPacket);
        }
    }

    public class ConnectionLargePacketTransferManager : ManagedConnectionPacketProcessor<NetworkdLargePacketTransferManager>
    {
        public override int Priority { get; } = 12;

        public override DataPacket ProcessPacketForSending(Connection conConnection, DataPacket pktOutputPacket)
        {
            //check is packet  larger than the mtu and will need splitting 
            if (pktOutputPacket.PacketTotalSize > conConnection.MaxPacketBytesToSend)
            {
                //split packet and reassemble at other end
                List<LargePacket> lpkSplitPackets = SplitPacket(pktOutputPacket, conConnection.MaxPacketBytesToSend);

                //queue split packets to send 
                for (int i = 0; i < lpkSplitPackets.Count; i++)
                {
                    conConnection.QueuePacketToSend(lpkSplitPackets[i]);
                }
            }

            return base.ProcessPacketForSending(conConnection, pktOutputPacket);
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

            //index to keep track of sub packet number
            int iSubPacketNumber = 0;

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

                //set package index
                lpkLargePacket.m_iSubPacketNumber = iSubPacketNumber;

                //increment index
                iSubPacketNumber++;

                iReadStartIndex += iMTU;

                //check that packet size is within spec
                Debug.Assert(lpkLargePacket.PacketTotalSize <= iMaxPacketSize, $"Split packet size: {lpkLargePacket.PacketTotalSize} larger thatn allowd size{iMaxPacketSize}");

                lpkOutput.Add(lpkLargePacket);
            }

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
                int iNumberOfItems = lpkPacket.m_bPacketSegment.Count;

                Array.Copy(lpkPacket.m_bPacketSegment.ToArray(), 0, m_tParentPacketProcessor.m_bSharedBuffer, iWriteHead, iNumberOfItems);

                iWriteHead += iNumberOfItems;
            }

            ReadByteStream rbsReadStream = new ReadByteStream(m_tParentPacketProcessor.m_bSharedBuffer);

            //get the split packet type
            byte bPacketType = 0;
            ByteStream.Serialize(rbsReadStream, ref bPacketType);

            DataPacket dpkOutputPacket = ParentConnection.m_cifPacketFactory.CreateType<DataPacket>(bPacketType);

            dpkOutputPacket.DecodePacket(rbsReadStream);

            return dpkOutputPacket;
        }
    }
}
