using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Networking
{
    public class NetworkLayoutPacket : DataPacket
    {
        public static int TypeID
        {
            get
            {
                return 6;
            }
        }

        public override int GetTypeID
        {
            get
            {
                return TypeID;
            }
        }

        public NetworkLayoutProcessor.NetworkLayout m_nlaNetworkLayout;

        public override int PacketSize
        {
            get
            {
                int iPacketSize = sizeof(Int32);

                iPacketSize += sizeof(Int64) * 2 * m_nlaNetworkLayout.m_conConnectionDetails.Count;

                return iPacketSize;
            }
        }

        public override void DecodePacket(PacketWrapper pkwPacketWrapper)
        {
            int iItemCount = 0;

            ByteStream.Serialize(pkwPacketWrapper.WriteStream, ref iItemCount);

            m_nlaNetworkLayout.m_conConnectionDetails = new List<NetworkLayoutProcessor.NetworkLayout.Connection>(iItemCount);

            for (int i = 0; i < iItemCount; i++)
            {
                NetworkLayoutProcessor.NetworkLayout.Connection conConnection = new NetworkLayoutProcessor.NetworkLayout.Connection();

                ByteStream.Serialize(pkwPacketWrapper.WriteStream, ref conConnection.m_lConnectionID);
                ByteStream.Serialize(pkwPacketWrapper.WriteStream, ref conConnection.m_dtmTimeOfConnection);

                m_nlaNetworkLayout.m_conConnectionDetails.Add(conConnection);
            }
        }

        public override void EncodePacket(PacketWrapper pkwPacketWrapper)
        {
            int iItemCount = m_nlaNetworkLayout.m_conConnectionDetails.Count;

            ByteStream.Serialize(pkwPacketWrapper.WriteStream,ref iItemCount);
            for(int i = 0; i < iItemCount; i++)
            {
                NetworkLayoutProcessor.NetworkLayout.Connection conConnection = m_nlaNetworkLayout.m_conConnectionDetails[i];

                ByteStream.Serialize(pkwPacketWrapper.WriteStream, ref conConnection.m_lConnectionID);
                ByteStream.Serialize(pkwPacketWrapper.WriteStream, ref conConnection.m_dtmTimeOfConnection);
            }
        }
    }
}
