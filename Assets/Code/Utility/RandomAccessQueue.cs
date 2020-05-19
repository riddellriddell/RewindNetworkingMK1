using System;

public class RandomAccessQueue<T> : ICloneable
{
    protected const int c_iDefaultItemCount = 8;
    protected T[] m_tStorage;
    protected int m_iQueueEnter = 0;
    protected int m_iQueueExit = 0;
    protected int m_iCount = 0;

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
            return m_tStorage.Length;
        }
    }

    public T this[int key]
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

    public RandomAccessQueue()
    {
        m_tStorage = new T[c_iDefaultItemCount];
    }

    public RandomAccessQueue(int iStartCount)
    {
        m_tStorage = new T[c_iDefaultItemCount];
    }

    public void Enqueue(T itemToQueue)
    {
        //check if the base data structure is too small
        if (m_iCount >= m_tStorage.Length)
        {
            //expand list
            ChangeCapacity(m_tStorage.Length * 2);
        }
        else if (m_iCount == 0)
        {
            //set item 
            m_tStorage[m_iQueueEnter] = itemToQueue;

            m_iCount++;

            return;
        }

        //shift enter index forwards
        m_iQueueEnter = (m_iQueueEnter + 1) % m_tStorage.Length;

        //set item 
        m_tStorage[m_iQueueEnter] = itemToQueue;

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
            m_iQueueExit = (m_iQueueExit + 1) % m_tStorage.Length;
        }

        //reduce number of items in list
        m_iCount--;

        //return item at old pos
        return m_tStorage[iIndex];

    }

    public T PeakDequeue()
    {
        if (m_iCount == 0)
        {
            return default(T);
        }

        return m_tStorage[m_iQueueExit];
    }

    public T PeakEnqueue()
    {
        if (m_iCount == 0)
        {
            return default(T);
        }

        return m_tStorage[m_iQueueEnter];
    }

    public void Clear()
    {
        m_iQueueEnter = 0;
        m_iQueueExit = 0;
        m_iCount = 0;

        for (int i = 0; i < m_tStorage.Length; i++)
        {
            m_tStorage[i] = default(T);
        }
    }

    public object Clone()
    {
        //create clone of current list
        RandomAccessQueue<T> raqClonedQueue = new RandomAccessQueue<T>(this.Capacity);

        if (typeof(ICloneable).IsAssignableFrom(typeof(T)))
        {
            for (int i = 0; i < this.Count; i++)
            {
                T value = this[i];

                if (value != null)
                {
                    raqClonedQueue.Enqueue((T)(value as ICloneable).Clone());
                }
                else
                {
                    raqClonedQueue.Enqueue(value);
                }
            }
        }
        else
        {
            //copy underlying storage
            m_tStorage.CopyTo(raqClonedQueue.m_tStorage, 0);
            raqClonedQueue.m_iCount = this.Count;
            raqClonedQueue.m_iQueueEnter = this.m_iQueueEnter;
            raqClonedQueue.m_iQueueExit = this.m_iQueueExit;
        }

        return raqClonedQueue;
    }

    private int RemapIndex(int iIndex)
    {
        if (iIndex < 0 || iIndex >= m_iCount)
        {
            throw new IndexOutOfRangeException("Index " + iIndex + "is less than 0 or greater than " + (m_iCount - 1));
        }

        //remap index 
        return (m_iQueueExit + iIndex) % m_tStorage.Length;
    }

    public void ChangeCapacity(int iNewCapacity)
    {
        //create new arrays
        T[] tNewStorage = new T[iNewCapacity];

        //check for empty resize
        if (m_iCount == 0)
        {
            m_tStorage = tNewStorage;

            m_iCount = 0;
            m_iQueueExit = 0;
            m_iQueueEnter = 0;

            return;
        }

        int iItemsToCopy = Math.Min(iNewCapacity, m_iCount);

        //calc segments to copy
        int iPartALength = Math.Min(m_iQueueExit + iItemsToCopy, m_tStorage.Length) - m_iQueueExit;
        int iPartBLength = iItemsToCopy - iPartALength;

        //copy accross existing values 
        Array.Copy(m_tStorage, m_iQueueExit, tNewStorage, 0, iPartALength);

        //copy accross wrap around segment
        if (iPartBLength > 0)
        {
            Array.Copy(m_tStorage, 0, tNewStorage, iPartALength, iPartBLength);
        }

        m_tStorage = tNewStorage;
        m_iCount = iItemsToCopy;
        m_iQueueExit = 0;
        m_iQueueEnter = iItemsToCopy - 1;
    }

}

