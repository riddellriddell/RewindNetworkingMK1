using Networking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sim
{
    public class EchoNetworkProcessor : NetworkPacketProcessor
    {
        protected float m_fEchoUpdateRate = 1f;
        
        protected NetworkConnection m_ncnNetwork;

        protected List<EchoConnectionProcessor> m_tcpTickStampProcessors;

        public override void OnAddToNetwork(NetworkConnection ncnNetwork)
        {
            //store network
            m_ncnNetwork = ncnNetwork;

            m_tcpTickStampProcessors = new List<EchoConnectionProcessor>();

            //add tick processors to all connections 
            for (int i = 0; i < m_ncnNetwork.m_conConnectionList.Count; i++)
            {
                if (m_ncnNetwork.m_conConnectionList[i] != null)
                {
                    AddProcessorToConnection(m_ncnNetwork.m_conConnectionList[i]);
                }
            }

        }

        public override void OnNewConnection(Connection conConnection)
        {
            AddProcessorToConnection(conConnection);
        }

        protected void AddProcessorToConnection(Connection conConnection)
        {
            EchoConnectionProcessor ecpEchoProcessor = new EchoConnectionProcessor(m_fEchoUpdateRate);

            m_tcpTickStampProcessors.Add(ecpEchoProcessor);

            conConnection.m_cppOrderedPacketProcessorList.Add(ecpEchoProcessor);
        }
    }

    /// <summary>
    /// this keeps track of the current tick of the connection 
    /// </summary>
    public class EchoConnectionProcessor : ConnectionPacketProcessor
    {
        public float RTT { get; private set; }
        
        //the value of the echo sent to check the rtt of the connection
        protected byte m_bEchoSent = 0;
        
        //the time the echo was sent
        protected float m_fTimeOfEchoSend = 0;

        //rate of update
        protected float m_fEchoUpdateRate = 1f;

        //time since last update
        protected float m_fTimeOfLastUpdate = 1f;

        public EchoConnectionProcessor(float fUpdateRate)
        {
            m_fEchoUpdateRate = fUpdateRate;
        }

        public override void Update(Connection conConnection)
        {
            //check if it is time for another update 
            if(UnityEngine.Time.realtimeSinceStartup - m_fTimeOfLastUpdate > m_fEchoUpdateRate && m_fTimeOfLastUpdate != float.MinValue)
            {
                //get echo value 
                m_bEchoSent = (byte)UnityEngine.Random.Range(byte.MinValue, byte.MinValue);

                //generate echo data packet
                NetTestPacket ntpEcho = conConnection.m_cifPacketFactory.CreateType<NetTestPacket>(NetTestPacket.TypeID);
                                
                //set echo value
                ntpEcho.m_bEcho = m_bEchoSent;
                ntpEcho.m_bIsReply = false;

                //reset time of echo send and time since last check
                m_fTimeOfEchoSend = m_fTimeOfLastUpdate = UnityEngine.Time.realtimeSinceStartup;

                //queue echo 
                conConnection.QueuePacketToSend(ntpEcho);
            }
        }

        public override DataPacket ProcessPacketForSending(Connection conConnection, DataPacket pktOutputPacket)
        {
            return pktOutputPacket;
        }

        public override DataPacket ProcessReceivedPacket(Connection conConnection, DataPacket pktInputPacket)
        {
            //check if packet is ping packet 
            if (pktInputPacket is NetTestPacket)
            {
                //get packet 
                NetTestPacket ntpEcho = pktInputPacket as NetTestPacket;

                //check if message is request or reply
                if(ntpEcho.m_bIsReply == false)
                {
                    //get echo Packet 
                    NetTestPacket ntpReply = conConnection.m_cifPacketFactory.CreateType<NetTestPacket>(NetTestPacket.TypeID);

                    //set reply values 
                    ntpReply.m_bEcho = ntpEcho.m_bEcho;
                    ntpReply.m_bIsReply = true;

                    //schedule a reply packet 
                    conConnection.QueuePacketToSend(ntpReply);
                }
                else
                {
                    //check if echo matches 
                    if(ntpEcho.m_bEcho != m_bEchoSent)
                    {
                        //bad echo reply probably error or hack? 
                    }
                    else
                    {
                        //update the time difference
                        RTT = m_fTimeOfEchoSend - UnityEngine.Time.realtimeSinceStartup;
                        m_fTimeOfEchoSend = float.MinValue;

                        //randomize echo value
                        m_bEchoSent = (byte)UnityEngine.Random.Range(byte.MinValue, byte.MinValue);
                    }
                }

                //consume echo packet as it is no longer needed 
                return null;
            }

            return pktInputPacket;
        }
    }
}
