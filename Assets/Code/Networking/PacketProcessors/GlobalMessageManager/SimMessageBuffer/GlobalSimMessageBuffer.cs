using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Networking
{
    //this class creates a queue of valid messages 
    public class GlobalSimMessageBuffer
    {
        //all messages before this are guaranteed not to change (kinda, if they do the local peer is desynched)
        public SortingValue m_svaSafeMessageTime;

        //the most recent time messages were recieved from all peers 
        public SortingValue m_svaAllRecievedTime;

        //queue of all the messages 
        public SortedRandomAccessQueue<SortingValue, ISimMessagePayload> m_squMessageQueue;


        public void QueueSimMessage(SortingValue svaTime, ISimMessagePayload smpMessage)
        {

        }

        public void Clear(SortingValue svaClearUpTo)
        {
            m_squMessageQueue.ClearTo(svaClearUpTo);
        } 
        
    }
}
