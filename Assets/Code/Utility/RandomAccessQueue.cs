using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomAccessQueue<T>
{
    private List<T> m_lstStorage;
    private int m_iQueueEnter = 0;
    private int m_iQueueExit = 0;
    private int m_iCount = 0;

    public int Count
    {
        get
        {
            return m_iCount;
        }
    }

    public int Capacity
    {
        get
        {
            return m_lstStorage.Count;
        }
    }

    public T this[int key]
    {
        get
        {
            return m_lstStorage[RemapIndex(key)];
        }
        set
        {
            m_lstStorage[RemapIndex(key)] = value;
        }
    }

    public RandomAccessQueue()
    {
        m_lstStorage = new List<T>();
    }

    public RandomAccessQueue(int iStartCount)
    {
        m_lstStorage = new List<T>(iStartCount);

        for (int i = 0; i < iStartCount; i++)
        {
            m_lstStorage.Add(default(T));
        }
    }

    public void Enqueue(T itemToQueue)
    {
        //check if the base data structure is too small
        if (m_iCount >= m_lstStorage.Count)
        {
            //expand list
            ExpandList(itemToQueue);
        }
        else if(m_iCount == 0)
        {
            //set item 
            m_lstStorage[m_iQueueEnter] = itemToQueue;
        }
        else
        {
            //shift enter index forwards
            m_iQueueEnter = (m_iQueueEnter + 1) % m_lstStorage.Count;

            //set item 
            m_lstStorage[m_iQueueEnter] = itemToQueue;
        }

        m_iCount++;
    }

    public T Dequeue()
    {
        //check if there is anything left in the queue
        if (m_iCount == 0)
        {
            return default(T);
        }

        int iIndex = m_iQueueExit;

        if (m_iCount > 1)
        {
            //index queue exit forwards
            m_iQueueExit = (m_iQueueExit + 1) % m_lstStorage.Count;
        }

        //reduce number of items in list
        m_iCount--;

        //return item at old pos
        return m_lstStorage[iIndex];

    }

    public T PeakDequeue()
    {
        if (m_iCount == 0)
        {
            return default(T);
        }

        return m_lstStorage[m_iQueueExit];
    }

    public T PeakEnqueue()
    {
        if (m_iCount == 0)
        {
            return default(T);
        }

        return m_lstStorage[m_iQueueEnter];
    }

    public void Clear()
    {
        m_iQueueEnter = 0;
        m_iQueueExit = 0;
        m_iCount = 0;

        for(int i = 0; i < m_lstStorage.Count; i++)
        {
            m_lstStorage[i] = default(T);
        }
    }

    private int RemapIndex(int iIndex)
    {
        if (iIndex < 0 || iIndex >= m_iCount)
        {
            throw new IndexOutOfRangeException("Index " + iIndex + "is less than 0 or greater than " + (m_iCount - 1));
        }

        //remap index 
        return (m_iQueueExit + iIndex) % m_lstStorage.Count;
    }

    private void ExpandList(T itemToAdd)
    {
        //check if item can just be tacked onto the end
        if (m_iQueueEnter >= m_lstStorage.Count - 1)
        {
            //just tack item onto the end 
            m_lstStorage.Add(itemToAdd);

            if (m_lstStorage.Count > 1)
            {
                m_iQueueEnter++;
            }

            return;

        }

        //index the head and tail forwards 
        m_iQueueEnter++;

        //add item before queue head 
        m_lstStorage.Insert(m_iQueueEnter, itemToAdd);

        //index queue exit forwards
        m_iQueueExit = (m_iQueueExit + 1) % m_lstStorage.Count;
    }

}

