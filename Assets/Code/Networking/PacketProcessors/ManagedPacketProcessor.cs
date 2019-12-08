using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Networking
{
    /// <summary>
    /// this class and its companion connection class provide the base functionality for creating 
    /// network and connection processors 
    /// </summary>
    public abstract class ManagedNetworkPacketProcessor<T> : BaseNetworkPacketProcessor where T : BaseConnectionPacketProcessor, IManagedConnectionPacketProcessor, new()
    {
        public NetworkConnection ParentNetworkConnection { get; private set; } = null;

        public Dictionary<long, T> ChildConnectionProcessors { get; } = new Dictionary<long, T>();

        protected virtual T NewConnectionProcessor()
        {
            return new T();
        }

        protected virtual void OnClientProcessorDisconnect(Connection conConnection,T tConnectionProcessor)
        {

        }

        protected virtual void AddDependentPacketsToPacketFactory(ClassWithIDFactory cifPacketFactory)
        {

        }

        public override void OnAddToNetwork(NetworkConnection ncnNetwork)
        {
            ParentNetworkConnection = ncnNetwork;

            //chance to add the packet types that this processor is reliant on
            AddDependentPacketsToPacketFactory(ParentNetworkConnection.PacketFactory);

            //add processing component to each existing connection
            foreach (Connection conConnection in ParentNetworkConnection.ConnectionList.Values)
            {
                //create new packet processor
                T connectionProcessor = NewConnectionProcessor();

                //add it to list of child packet processors
                ChildConnectionProcessors.Add(conConnection.m_lUserUniqueID, connectionProcessor);

                //add processor to connection
                conConnection.AddPacketProcessor(connectionProcessor);
            }

            base.OnAddToNetwork(ncnNetwork);
        }

        public override void OnNewConnection(Connection conConnection)
        {
            //create new packet processor
            T connectionProcessor = NewConnectionProcessor();

            connectionProcessor.SetParentProcessor(this);

            //add it to list of child packet processors
            ChildConnectionProcessors.Add(conConnection.m_lUserUniqueID, connectionProcessor);

            //add processor to connection
            conConnection.AddPacketProcessor(connectionProcessor);

            base.OnNewConnection(conConnection);
        }

        public override void OnConnectionDisconnect(Connection conConnection)
        {
            base.OnConnectionDisconnect(conConnection);

            ChildConnectionProcessors.Remove(conConnection.m_lUserUniqueID);
        }
    }

    public interface IManagedConnectionPacketProcessor
    {
        void SetParentProcessor(BaseNetworkPacketProcessor parentPacketProcessor);
    }

    /// <summary>
    /// this class works with a matching ManagedNetworkPacketProcessor to store a reffence to its paretn packet processor
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class ManagedConnectionPacketProcessor<T> : BaseConnectionPacketProcessor, IManagedConnectionPacketProcessor where T : BaseNetworkPacketProcessor
    {
        protected T m_tParentPacketProcessor;

        public virtual void SetParentProcessor(BaseNetworkPacketProcessor parentPacketProcessor)
        {
            m_tParentPacketProcessor = (T)parentPacketProcessor;
        }
    }
}
