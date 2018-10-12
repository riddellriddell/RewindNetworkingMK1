using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class RandomAccessQueueUnitTest : MonoBehaviour
{
    public bool m_bRunTest;

    public RandomAccessQueue<int> m_raqTestQueue;

    public void Update()
    {
        if (m_bRunTest == false)
        {
            return;
        }

        m_bRunTest = false;

        m_raqTestQueue = new RandomAccessQueue<int>(5);

        //test queue

        //queue some random items
        for (int i = 0; i < m_raqTestQueue.Capacity; i++)
        {
            m_raqTestQueue.Enqueue(i);
        }

        //peek last itme 
        Debug.Log("Peak Item " + m_raqTestQueue.PeakDequeue());

        //print out results 
        while (m_raqTestQueue.Count > 0)
        {
            Debug.Log("Dequeued Item " + m_raqTestQueue.Dequeue());
        }

        //test random access 

        //queue some random items
        for (int i = 0; i < m_raqTestQueue.Capacity; i++)
        {
            m_raqTestQueue.Enqueue(i);
        }

        for (int i = 0; i < m_raqTestQueue.Count; i++)
        {
            Debug.Log("Random Access Item " + m_raqTestQueue[i]);
        }

        //test clear
        m_raqTestQueue.Clear();

        //test queue expansion 
        for (int i = 0; i < 20; i++)
        {
            m_raqTestQueue.Enqueue(i);
        }

        for (int i = 0; i < m_raqTestQueue.Count; i++)
        {
            Debug.Log("Random Access Item " + m_raqTestQueue[i]);
        }
    }
}