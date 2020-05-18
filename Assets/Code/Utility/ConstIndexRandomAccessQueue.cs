using System;

//this class functions like a queue but the index for an item remains the same even when items are dequeued before it 
public class ConstIndexRandomAccessQueue<T> : RandomAccessQueue<T>
{
    //the number of items that have been dequeued before first index in the queue
    protected uint m_iBaseIndex = 0;

    public uint BaseIndex
    {
        get
        {
            return m_iBaseIndex;
        }
    }

    public uint HeadIndex
    {
        get
        {
            return m_iBaseIndex + (uint)m_iCount;
        }

    }

    public ConstIndexRandomAccessQueue(uint iStartIndex) : base()
    {
        m_iBaseIndex = iStartIndex;
    }

    public void SetNewBaseIndex(uint iNewBaseIndex)
    {
        m_iBaseIndex = iNewBaseIndex;
    }

    public new T this[int key]
    {
        get
        {
            return m_tStorage[RemapIndex((uint)key)];
        }
        set
        {
            m_tStorage[RemapIndex((uint)key)] = value;
        }
    }

    public T this[uint key]
    {
        get
        {
            return m_tStorage[RemapIndex(key)];
        }
        set
        {
            m_tStorage[RemapIndex(key)] = value;
        }
    }

    public new T Dequeue()
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
            m_iQueueExit = (m_iQueueExit + 1) % m_tStorage.Length;
        }

        //reduce number of items in list
        m_iCount--;

        //update the number of items that have been dequeued 
        m_iBaseIndex++;

        //return item at old pos
        return m_tStorage[iIndex];
    }

    public bool IsValidIndex(int iIndex)
    {
        return IsValidIndex((uint)iIndex);
    }

    public bool IsValidIndex(uint iIndex)
    {
        return iIndex >= m_iBaseIndex && iIndex < m_iBaseIndex + (uint)m_iCount;
    }

    private int RemapIndex(uint iIndex)
    {
        if (iIndex < m_iBaseIndex || iIndex >= m_iBaseIndex + m_iCount)
        {
            throw new IndexOutOfRangeException("Index " + iIndex + "is less than 0 or greater than " + (m_iCount - 1));
        }

        //remap index 
        return (m_iQueueExit + (int)(iIndex - m_iBaseIndex)) % m_tStorage.Length;
    }
}
