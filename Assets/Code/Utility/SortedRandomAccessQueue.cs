using System;
using UnityEngine;

#region SortBasedOnKeyType
//this class represents a queue like structure where items are removed in order from one end but new items are added in a non linear way
public class SortedRandomAccessQueue<TKey, TValue> where TKey : IComparable
{
    private const int c_iDefaultCapacity = 8;
    private TKey[] m_keyKeys;
    private TValue[] m_valValues;
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
            return m_keyKeys.Length;
        }
    }

    public SortedRandomAccessQueue()
    {
        m_valValues = new TValue[c_iDefaultCapacity];
        m_keyKeys = new TKey[c_iDefaultCapacity];
    }

    public SortedRandomAccessQueue(int iStartCapacity)
    {
        m_valValues = new TValue[iStartCapacity];
        m_keyKeys = new TKey[iStartCapacity];
    }

    public TValue GetValueAtIndex(int iIndex)
    {
        return m_valValues[RemapIndex(iIndex)];
    }

    public void SetValueAtIndex(int iIndex, ref TValue Value)
    {
        m_valValues[RemapIndex(iIndex)] = Value;
    }

    public TKey GetKeyAtIndex(int iIndex)
    {
        return m_keyKeys[RemapIndex(iIndex)];
    }

    //Changes the key at index, CAUTION this does not resort the list and will cause problems if you change the key to a 
    //value greater than or less than the keys higher and lower neighbours 
    public void SetKeyAtIndex(int iIndex, ref TKey Key)
    {
        m_keyKeys[RemapIndex(iIndex)] = Key;
    }

    //find the first instance greater than index
    //if there are no items in list this function returns false and an index of 0
    //if all items in list are less than key then this function reutrns false and 
    //and index of max value
    public bool TryGetFirstIndexGreaterThan(in TKey Key, out int iIndex)
    {
        //check if there are any values in the array
        if (m_iCount == 0)
        {
            //return index for empty list 
            iIndex = 0;

            return false;
        }

        //check if new value is past end list 
        if (Compare(m_keyKeys[m_iQueueEnter], Key) < 0)
        {
            //return index for no value greater than key
            iIndex = int.MaxValue;

            return false;
        }

        //perform binary sarch on remaining values 

        int iSearchWindowMin = 0;
        int iSearchWindowMax = m_iCount - 1;

        while (iSearchWindowMin < iSearchWindowMax)
        {
            int iMid = (iSearchWindowMin + iSearchWindowMax) / 2;

            if (Compare(m_keyKeys[UnsafeRemapIndex(iMid)], Key) < 1)
            {
                iSearchWindowMin = iMid + 1;
            }
            else
            {
                iSearchWindowMax = iMid;
            }
        }

        iIndex = iSearchWindowMax;

        return true;
    }

    public bool TryGetFirstIndexGreaterThan(in TKey Key, out int iIndex, out bool bCollision, out int iCollisionIndex)
    {
        iCollisionIndex = 0;
        bCollision = false;

        //check if there are any values in the array
        if (m_iCount == 0)
        {
            //return index for empty list 
            iIndex = 0;

            return false;
        }

        //check if new value is past end list 
        if (Compare(m_keyKeys[m_iQueueEnter], Key) < 0)
        {
            //return index for no value greater than key
            iIndex = int.MaxValue;

            return false;
        }

        //perform binary sarch on remaining values 

        int iSearchWindowMin = 0;
        int iSearchWindowMax = m_iCount - 1;

        while (iSearchWindowMin < iSearchWindowMax)
        {
            int iMid = (iSearchWindowMin + iSearchWindowMax) / 2;

            int iCompareResult = Compare(m_keyKeys[UnsafeRemapIndex(iMid)], Key);

            if (iCompareResult < 1)
            {
                iSearchWindowMin = iMid + 1;

                if (iCompareResult == 0)
                {
                    iCollisionIndex = iMid;
                    bCollision = true;
                }
            }
            else
            {
                iSearchWindowMax = iMid;
            }
        }

        iIndex = iSearchWindowMax;

        return true;
    }

    //find first index less than key
    //if there are no items in list this funciton returns false and and index of 0
    //if all items in list are greater than key this function returns false and an index of min value
    public bool TryGetFirstIndexLessThan(in TKey Key, out int iIndex)
    {
        //check if there are any values in the array
        if (m_iCount == 0)
        {
            //return index for empty list 
            iIndex = 0;

            return false;
        }

        //check if new value is past end list 
        if (Compare(m_keyKeys[0], Key) > 0)
        {
            //return index for no value less than key
            iIndex = int.MinValue;

            return false;
        }

        //perform binary sarch on remaining values 

        int iSearchWindowMin = 0;
        int iSearchWindowMax = m_iCount - 1;

        while (iSearchWindowMin < iSearchWindowMax)
        {
            int iMid = (iSearchWindowMin + iSearchWindowMax + 1) / 2;

            if (Compare(m_keyKeys[UnsafeRemapIndex(iMid)], Key) > -1)
            {
                iSearchWindowMax = iMid - 1;
            }
            else
            {
                iSearchWindowMin = iMid;
            }
        }

        iIndex = iSearchWindowMax;

        return true;
    }

    public bool TryGetFirstIndexLessThan(in TKey Key, out int iIndex, out bool bCollision, out int iCollisionIndex)
    {
        iCollisionIndex = 0;
        bCollision = false;

        //check if there are any values in the array
        if (m_iCount == 0)
        {
            //return index for empty list 
            iIndex = 0;

            return false;
        }

        //check if new value is past end list 
        if (Compare(m_keyKeys[0], Key) > 0)
        {
            //return index for no value greater than key
            iIndex = int.MinValue;

            return false;
        }

        //perform binary sarch on remaining values 

        int iSearchWindowMin = 0;
        int iSearchWindowMax = m_iCount - 1;

        while (iSearchWindowMin < iSearchWindowMax)
        {
            int iMid = (iSearchWindowMin + iSearchWindowMax + 1) / 2;

            int iCompareResult = Compare(m_keyKeys[UnsafeRemapIndex(iMid)], Key);

            if (iCompareResult > -1)
            {
                iSearchWindowMax = iMid - 1;

                if (iCompareResult == 0)
                {
                    iCollisionIndex = iMid;
                    bCollision = true;
                }
            }
            else
            {
                iSearchWindowMin = iMid;
            }
        }

        iIndex = iSearchWindowMax;

        return true;
    }

    //performs binary search for key returns false if not found 
    public bool TryGetIndexOf(in TKey Key, out int iIndex)
    {
        int iSearchWindowMin = 0;
        int iSearchWindowMax = m_iCount - 1;

        while (iSearchWindowMin <= iSearchWindowMax)
        {
            int iMid = (iSearchWindowMin + iSearchWindowMax) / 2;

            int iCompareResult = Compare(m_keyKeys[UnsafeRemapIndex(iMid)], Key);

            if (iCompareResult < 0)
            {
                iSearchWindowMax = iMid - 1;
            }
            else if (iCompareResult > 0)
            {
                iSearchWindowMin = iMid + 1;
            }
            else
            {
                iIndex = iMid;

                return true;
            }
        }

        iIndex = 0;

        return false;
    }

    //returns the value corresponding to the key if it exists in the queue
    //if it cant be found value is default and false is returned 
    public bool TryGetValueOf(in TKey keyKey, out TValue valValue)
    {
        if (TryGetIndexOf(keyKey, out int iIndex) == true)
        {
            valValue = GetValueAtIndex(iIndex);

            return true;
        }

        valValue = default(TValue);

        return false;
    }

    //inserts this item into the priority queue while keeping all the items that exist after it 
    //returns false and does not insert on collision with existing item
    public bool TryInsertEnqueue(in TKey itemKey, in TValue itemToQueue, out int iIndexOfMatchingItem)
    {
        //get index to insert 
        TryGetFirstIndexGreaterThan(itemKey, out int iIndex, out bool bCollision, out iIndexOfMatchingItem);

        if (bCollision)
        {
            return false;
        }

        //inject the item into the queue
        Inject(itemKey, itemToQueue, iIndex);

        iIndexOfMatchingItem = 0;

        return true;
    }

    //removes all items that exists after this item then inserts this item 
    public void EnterPurgeInsert(in TKey itemKey, in TValue itemToQueue)
    {
        //check if there is no items before this item
        if (TryGetFirstIndexLessThan(itemKey, out int iIndex) == false)
        {
            Clear();

            m_keyKeys[0] = itemKey;
            m_valValues[0] = itemToQueue;

            m_iCount++;
        }

        //caclc index of insert 
        iIndex++;

        //check if array is large enough
        if (iIndex >= m_keyKeys.Length)
        {
            //expand queue
            ChangeCapacity(m_keyKeys.Length * 2);

            //set new value
            m_iQueueEnter = iIndex;
            m_iCount = iIndex + 1;
            m_keyKeys[m_iQueueEnter] = itemKey;
            m_valValues[m_iQueueEnter] = itemToQueue;

            return;
        }

        //clear all items from array
        for (int i = iIndex + 1; i < m_iCount; i++)
        {
            int iAddressToClear = UnsafeRemapIndex(i);
            m_keyKeys[iAddressToClear] = default(TKey);
            m_valValues[iAddressToClear] = default(TValue);
        }

        m_iQueueEnter = UnsafeRemapIndex(iIndex);
        m_keyKeys[m_iQueueEnter] = itemKey;
        m_valValues[m_iQueueEnter] = itemToQueue;
        m_iCount = iIndex + 1;

        return;
    }

    public bool Dequeue(out TKey keyKey, out TValue valValue)
    {
        //check if there is anything left in the queue
        if (m_iCount == 0)
        {
            keyKey = default(TKey);
            valValue = default(TValue);
            return false;
        }

        //return item at old pos
        keyKey = m_keyKeys[m_iQueueExit];
        valValue = m_valValues[m_iQueueExit];

        //reset old values
        m_keyKeys[m_iQueueExit] = default(TKey);
        m_valValues[m_iQueueExit] = default(TValue);

        //index queue exit forwards
        m_iQueueExit = (m_iQueueExit + 1) % m_valValues.Length;

        //reduce number of items in list
        m_iCount--;

        return true;
    }

    //removes all items after an index excluding index
    public void ClearFrom(int iIndex)
    {
        for (int i = iIndex + 1; i < m_iCount; i++)
        {
            int iAddressToClear = UnsafeRemapIndex(i);

            m_keyKeys[iAddressToClear] = default(TKey);
            m_valValues[iAddressToClear] = default(TValue);
        }

        m_iQueueEnter = RemapIndex(iIndex);
        m_iCount = iIndex + 1;

    }

    //remove all items including index
    public void ClearFromIncluding(int iIndex)
    {
        for (int i = iIndex; i < m_iCount; i++)
        {
            int iAddressToClear = UnsafeRemapIndex(i);

            m_keyKeys[iAddressToClear] = default(TKey);
            m_valValues[iAddressToClear] = default(TValue);
        }

        m_iQueueEnter = RemapIndex(iIndex);
        m_iCount = iIndex;
    }

    //removes all items before an index
    public void ClearTo(int iIndex)
    {
        //clean up items being removed 
        for (int i = iIndex - 1; i > -1; i--)
        {
            int iAddressToClear = UnsafeRemapIndex(i);

            m_keyKeys[iAddressToClear] = default(TKey);
            m_valValues[iAddressToClear] = default(TValue);
        }

        m_iQueueExit = RemapIndex(iIndex);
        m_iCount = m_iCount - iIndex;
    }

    //remove all items before including index
    public void ClearToIncluding(int iIndex)
    {
        if (iIndex >= m_iCount)
        {
            Clear();

            return;
        }

        //clean up items being removed 
        for (int i = iIndex; i > -1; i--)
        {
            int iAddressToClear = UnsafeRemapIndex(i);

            m_keyKeys[iAddressToClear] = default(TKey);
            m_valValues[iAddressToClear] = default(TValue);
        }

        iIndex++;

        m_iQueueExit = RemapIndex(iIndex);
        m_iCount = m_iCount - iIndex;
    }

    //clears all items from key onward 
    public void ClearFrom(in TKey keyKey)
    {
        if (TryGetFirstIndexGreaterThan(keyKey, out int iIndex) == false)
        {
            //either the queue is empty or no items are larger than key
            return;
        }

        ClearFromIncluding(iIndex);
    }

    public void ClearTo(in TKey keyKey)
    {
        if (TryGetFirstIndexLessThan(keyKey, out int iIndex) == false)
        {
            //either there are no items in queue or all items are greater than target
            return;
        }

        ClearToIncluding(iIndex);
    }

    public TKey PeakKeyDequeue()
    {
        if (m_iCount == 0)
        {
            return default(TKey);
        }

        return m_keyKeys[m_iQueueExit];
    }

    public TKey PeakKeyEnqueue()
    {
        if (m_iCount == 0)
        {
            return default(TKey);
        }

        return m_keyKeys[m_iQueueEnter];
    }

    public TValue PeakValueDequeue()
    {
        if (m_iCount == 0)
        {
            return default(TValue);
        }

        return m_valValues[m_iQueueExit];
    }

    public TValue PeakValueEnqueue()
    {
        if (m_iCount == 0)
        {
            return default(TValue);
        }

        return m_valValues[m_iQueueEnter];
    }

    public void Clear()
    {
        m_iQueueEnter = 0;
        m_iQueueExit = 0;
        m_iCount = 0;

        //TODO:: make this only reset values that have already been set
        for (int i = 0; i < m_valValues.Length; i++)
        {
            m_valValues[i] = default(TValue);
            m_keyKeys[i] = default(TKey);
        }
    }

    public void Remove(int iIndex)
    {
        //check if index is within range 
        if (iIndex < 0 || iIndex >= m_iCount)
        {
            return;
        }

        //check for last item in list 
        if (m_iCount == 1)
        {
            Clear();
            return;
        }

        //check which is easier end to shift, the exit segment or the enter segment 
        if (iIndex > m_iCount / 2)
        {
            //calculate the index in the array to remove the value
            int iRemoveIndex = UnsafeRemapIndex(iIndex);

            //shift values from removal point to end of array backwards
            int iItemsToShift = Math.Min((m_iCount - iIndex) - 1, (m_keyKeys.Length - 1) - iRemoveIndex);

            if (iItemsToShift > 0)
            {
                Array.Copy(m_keyKeys, iRemoveIndex + 1, m_keyKeys, iRemoveIndex, iItemsToShift);
                Array.Copy(m_valValues, iRemoveIndex + 1, m_valValues, iRemoveIndex, iItemsToShift);
            }

            //shift wrapped around values forwards
            if (m_iQueueEnter < iRemoveIndex)
            {
                //shift last value around 
                m_keyKeys[m_keyKeys.Length - 1] = m_keyKeys[0];
                m_valValues[m_keyKeys.Length - 1] = m_valValues[0];

                if (m_iQueueEnter > 0)
                {
                    Array.Copy(m_keyKeys, 1, m_keyKeys, 0, m_iQueueEnter);
                    Array.Copy(m_valValues, 1, m_valValues, 0, m_iQueueEnter);
                }
            }

            //clear unused value at old end of queue
            m_keyKeys[m_iQueueEnter] = default;
            m_valValues[m_iQueueEnter] = default;

            //calculate the new queue entrace point
            m_iQueueEnter = ((m_iQueueEnter + m_keyKeys.Length) - 1) % m_keyKeys.Length;

            //increment the number of items in queue
            m_iCount--;
        }
        else
        {
            //calculate the index in the array to remove the value from
            int iRemoveIndex = UnsafeRemapIndex(iIndex);

            //shift values from remove point to start of queue of array forward
            int iItemsToShift = Math.Min(iIndex, iRemoveIndex);

            if (iItemsToShift > 0)
            {
                int iCopyFromIndex = iRemoveIndex - iItemsToShift;

                Array.Copy(m_keyKeys, iCopyFromIndex, m_keyKeys, iCopyFromIndex + 1, iItemsToShift);
                Array.Copy(m_valValues, iCopyFromIndex, m_valValues, iCopyFromIndex + 1, iItemsToShift);
            }

            //shift wrapped around values forwards
            if (m_iQueueExit > iRemoveIndex)
            {
                //shift last value around 
                m_keyKeys[0] = m_keyKeys[m_keyKeys.Length - 1];
                m_valValues[0] = m_valValues[m_keyKeys.Length - 1];

                int iWrappedAroundItems = (m_keyKeys.Length - 1) - m_iQueueExit;

                if (iWrappedAroundItems > 0)
                {
                    Array.Copy(m_keyKeys, m_iQueueExit, m_keyKeys, m_iQueueExit + 1, iWrappedAroundItems);
                    Array.Copy(m_valValues, m_iQueueExit, m_valValues, m_iQueueExit + 1, iWrappedAroundItems);
                }
            }

            //clear value to be removed 
            m_keyKeys[m_iQueueExit] = default;
            m_valValues[m_iQueueExit] = default;

            //calculate the new queue entrace point
            m_iQueueExit = (m_iQueueExit + 1) % m_keyKeys.Length;

            //increment the number of items in queue
            m_iCount--;
        }
    }

    private int Compare(in TKey keyCompareFrom, in TKey keyCompareTo)
    {
        return keyCompareFrom.CompareTo(keyCompareTo);
    }

    private int RemapIndex(int iIndex)
    {
        if (iIndex < 0 || iIndex >= m_iCount)
        {
            throw new IndexOutOfRangeException("Index " + iIndex + "is less than 0 or greater than " + (m_iCount - 1));
        }

        //remap index 
        return UnsafeRemapIndex(iIndex);
    }

    private int UnsafeRemapIndex(int iIndex)
    {
        return (m_iQueueExit + iIndex) % m_valValues.Length;
    }

    private void Inject(in TKey keyKeyToInject, in TValue valValueToInject, int iIndex)
    {
        //check for out of bounds insert 
        iIndex = Mathf.Clamp(iIndex, 0, m_iCount);

        //check if this will require a resize 
        if (m_iCount + 1 > m_keyKeys.Length)
        {
            int iNewCapacity = m_keyKeys.Length * 2;

            //create new arrays
            TKey[] keyNewKeyArray = new TKey[iNewCapacity];
            TValue[] valNewValueArray = new TValue[iNewCapacity];

            //check for empty resize
            if (m_iCount == 0)
            {
                keyNewKeyArray[0] = keyKeyToInject;
                valNewValueArray[0] = valValueToInject;

                m_keyKeys = keyNewKeyArray;
                m_valValues = valNewValueArray;

                m_iCount = 1;
                m_iQueueExit = 0;
                m_iQueueEnter = 0;

                return;
            }

            //check if items exist before the insert location
            if (iIndex > 0)
            {
                //copy all the parts before the insert
                int iItemsToCopy = Math.Min(iIndex, m_iCount);

                //calc segments to copy
                int iPartALength = Math.Min(m_iQueueExit + iItemsToCopy, m_keyKeys.Length) - m_iQueueExit;
                int iPartBLength = iItemsToCopy - iPartALength;

                //copy accross existing values 
                Array.Copy(m_keyKeys, m_iQueueExit, keyNewKeyArray, 0, iPartALength);
                Array.Copy(m_valValues, m_iQueueExit, valNewValueArray, 0, iPartALength);

                //copy accross wrap around segment
                if (iPartBLength > 0)
                {
                    Array.Copy(m_keyKeys, 0, keyNewKeyArray, iPartALength, iPartBLength);
                    Array.Copy(m_valValues, 0, valNewValueArray, iPartALength, iPartBLength);
                }
            }

            //insert item
            keyNewKeyArray[iIndex] = keyKeyToInject;
            valNewValueArray[iIndex] = valValueToInject;

            int iRemainingItemsToCopy = m_iCount - iIndex;

            //check if items exist after the insert location
            if (iRemainingItemsToCopy > 0)
            {
                //get start index
                int iStartIndex = RemapIndex(iIndex);

                //calc segments to copy
                int iPartALength = Math.Min(iStartIndex + iRemainingItemsToCopy, m_keyKeys.Length) - iStartIndex;
                int iPartBLength = iRemainingItemsToCopy - iPartALength;

                //copy accross existing values 
                Array.Copy(m_keyKeys, iStartIndex, keyNewKeyArray, iIndex + 1, iPartALength);
                Array.Copy(m_valValues, iStartIndex, valNewValueArray, iIndex + 1, iPartALength);

                //copy accross wrap around segment
                if (iPartBLength > 0)
                {
                    Array.Copy(m_keyKeys, 0, keyNewKeyArray, iPartALength, iPartBLength);
                    Array.Copy(m_valValues, 0, valNewValueArray, iPartALength, iPartBLength);
                }
            }

            m_keyKeys = keyNewKeyArray;
            m_valValues = valNewValueArray;

            m_iCount = m_iCount + 1;
            m_iQueueExit = 0;
            m_iQueueEnter = m_iCount - 1;

            return;
        }

        //check for empty structure
        if (m_iCount == 0)
        {
            m_keyKeys[0] = keyKeyToInject;
            m_valValues[0] = valValueToInject;

            m_iCount = m_iCount + 1;
            m_iQueueExit = 0;
            m_iQueueEnter = m_iCount - 1;

            return;
        }

        //check if adding to end of queue
        if (iIndex >= m_iCount)
        {
            //shift enter index forwards
            m_iQueueEnter = (m_iQueueEnter + 1) % m_valValues.Length;

            //set item 
            m_keyKeys[m_iQueueEnter] = keyKeyToInject;
            m_valValues[m_iQueueEnter] = valValueToInject;

            //increment the number of items in queue
            m_iCount++;

            return;
        }

        //check which is easier end to shift, the exit segment or the enter segment 
        if (iIndex > m_iCount / 2)
        {
            //calculate the new queue entrace point
            m_iQueueEnter = (m_iQueueEnter + 1) % m_keyKeys.Length;

            //calculate the index in the array to insert the value
            int iInsertIndex = UnsafeRemapIndex(iIndex);

            //shift wrapped around values forwards
            if (m_iQueueEnter < iInsertIndex)
            {
                if (m_iQueueEnter > 0)
                {
                    Array.Copy(m_keyKeys, 0, m_keyKeys, 1, m_iQueueEnter);
                    Array.Copy(m_valValues, 0, m_valValues, 1, m_iQueueEnter);
                }

                //shift last value around 
                m_keyKeys[0] = m_keyKeys[m_keyKeys.Length - 1];
                m_valValues[0] = m_valValues[m_keyKeys.Length - 1];
            }

            //shift values from insertion point to end of array forwards
            int iItemsToShift = Math.Min((m_iCount - iIndex), (m_keyKeys.Length - 1) - iInsertIndex);

            if (iItemsToShift > 0)
            {
                Array.Copy(m_keyKeys, iInsertIndex, m_keyKeys, iInsertIndex + 1, iItemsToShift);
                Array.Copy(m_valValues, iInsertIndex, m_valValues, iInsertIndex + 1, iItemsToShift);
            }

            //insert item
            m_keyKeys[iInsertIndex] = keyKeyToInject;
            m_valValues[iInsertIndex] = valValueToInject;

            //increment the number of items in queue
            m_iCount++;
        }
        else
        {
            //calculate the new queue entrace point
            m_iQueueExit = (m_iQueueExit + m_keyKeys.Length - 1) % m_keyKeys.Length;

            //calculate the index in the array to insert the value
            int iInsertIndex = UnsafeRemapIndex(iIndex);

            //shift wrapped around values forwards
            if (m_iQueueExit > iInsertIndex)
            {
                int iWrappedAroundItems = (m_keyKeys.Length - 1) - m_iQueueExit;

                if (iWrappedAroundItems > 0)
                {
                    Array.Copy(m_keyKeys, m_iQueueExit + 1, m_keyKeys, m_iQueueExit, iWrappedAroundItems);
                    Array.Copy(m_valValues, m_iQueueExit + 1, m_valValues, m_iQueueExit, iWrappedAroundItems);
                }

                //shift last value around 
                m_keyKeys[m_keyKeys.Length - 1] = m_keyKeys[0];
                m_valValues[m_keyKeys.Length - 1] = m_valValues[0];

            }

            //shift values from insertion point to start of queue of array backward
            int iItemsToShift = Math.Min(iIndex, iInsertIndex);

            if (iItemsToShift > 0)
            {
                int iCopyToIndex = iInsertIndex - iItemsToShift;

                Array.Copy(m_keyKeys, iCopyToIndex + 1, m_keyKeys, iCopyToIndex, iItemsToShift);
                Array.Copy(m_valValues, iCopyToIndex + 1, m_valValues, iCopyToIndex, iItemsToShift);
            }

            //insert item
            m_keyKeys[iInsertIndex] = keyKeyToInject;
            m_valValues[iInsertIndex] = valValueToInject;

            //increment the number of items in queue
            m_iCount++;
        }
    }

    //expands or contracts the underlying data structure 
    //if new size is smaller than number of items in list then
    //the more recently added items are dropped 
    private void ChangeCapacity(int iNewCapacity)
    {
        //create new arrays
        TKey[] keyNewKeyArray = new TKey[iNewCapacity];
        TValue[] valNewValueArray = new TValue[iNewCapacity];

        //check for empty resize
        if (m_iCount == 0)
        {
            m_keyKeys = keyNewKeyArray;
            m_valValues = valNewValueArray;

            m_iCount = 0;
            m_iQueueExit = 0;
            m_iQueueEnter = 0;

            return;
        }

        int iItemsToCopy = Math.Min(iNewCapacity, m_iCount);

        //calc segments to copy
        int iPartALength = Math.Min(m_iQueueExit + iItemsToCopy, m_keyKeys.Length) - m_iQueueExit;
        int iPartBLength = iItemsToCopy - iPartALength;

        //copy accross existing values 
        Array.Copy(m_keyKeys, m_iQueueExit, keyNewKeyArray, 0, iPartALength);
        Array.Copy(m_valValues, m_iQueueExit, valNewValueArray, 0, iPartALength);

        //copy accross wrap around segment
        if (iPartBLength > 0)
        {
            Array.Copy(m_keyKeys, 0, keyNewKeyArray, iPartALength, iPartBLength);
            Array.Copy(m_valValues, 0, valNewValueArray, iPartALength, iPartBLength);
        }

        m_iCount = iItemsToCopy;
        m_iQueueExit = 0;
        m_iQueueEnter = iItemsToCopy - 1;
    }
}

//this class represents a queue like structure where items are removed in order from one end but new items are added in a non linear way
public class SortedRandomAccessQueue<T> where T : IComparable
{
    private const int c_iDefaultCapacity = 8;
    private T[] m_tValues;
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
            return m_tValues.Length;
        }
    }

    public SortedRandomAccessQueue()
    {
        m_tValues = new T[c_iDefaultCapacity];
    }

    public SortedRandomAccessQueue(int iStartCapacity)
    {
        m_tValues = new T[iStartCapacity];
    }

    public T GetValueAtIndex(int iIndex)
    {
        return m_tValues[RemapIndex(iIndex)];
    }

    public void SetValueAtIndex(int iIndex, ref T Value)
    {
        m_tValues[RemapIndex(iIndex)] = Value;
    }

    //find the first instance greater than index
    //if there are no items in list this function returns false and an index of 0
    //if all items in list are less than key then this function reutrns false and 
    //and index of max value
    public bool TryGetFirstIndexGreaterThan(in T tVal, out int iIndex)
    {
        //check if there are any values in the array
        if (m_iCount == 0)
        {
            //return index for empty list 
            iIndex = 0;

            return false;
        }

        //check if new value is past end list 
        if (Compare(m_tValues[m_iQueueEnter], tVal) < 0)
        {
            //return index for no value greater than key
            iIndex = int.MaxValue;

            return false;
        }

        //perform binary sarch on remaining values 

        int iSearchWindowMin = 0;
        int iSearchWindowMax = m_iCount - 1;

        while (iSearchWindowMin < iSearchWindowMax)
        {
            int iMid = (iSearchWindowMin + iSearchWindowMax) / 2;

            if (Compare(m_tValues[UnsafeRemapIndex(iMid)], tVal) < 1)
            {
                iSearchWindowMin = iMid + 1;
            }
            else
            {
                iSearchWindowMax = iMid;
            }
        }

        iIndex = iSearchWindowMax;

        return true;
    }

    public bool TryGetFirstIndexGreaterThan(in T tVal, out int iIndex, out bool bCollision, out int iCollisionIndex)
    {
        iCollisionIndex = 0;
        bCollision = false;

        //check if there are any values in the array
        if (m_iCount == 0)
        {
            //return index for empty list 
            iIndex = 0;

            return false;
        }

        //check if new value is past end list 
        if (Compare(m_tValues[m_iQueueEnter], tVal) < 0)
        {
            //return index for no value greater than key
            iIndex = int.MaxValue;

            return false;
        }

        //perform binary sarch on remaining values 

        int iSearchWindowMin = 0;
        int iSearchWindowMax = m_iCount - 1;

        while (iSearchWindowMin < iSearchWindowMax)
        {
            int iMid = (iSearchWindowMin + iSearchWindowMax) / 2;

            int iCompareResult = Compare(m_tValues[UnsafeRemapIndex(iMid)], tVal);

            if (iCompareResult < 1)
            {
                iSearchWindowMin = iMid + 1;

                if (iCompareResult == 0)
                {
                    iCollisionIndex = iMid;
                    bCollision = true;
                }
            }
            else
            {
                iSearchWindowMax = iMid;
            }
        }

        iIndex = iSearchWindowMax;

        return true;
    }

    //find first index less than key
    //if there are no items in list this funciton returns false and and index of 0
    //if all items in list are greater than key this function returns false and an index of min value
    public bool TryGetFirstIndexLessThan(in T tVal, out int iIndex)
    {
        //check if there are any values in the array
        if (m_iCount == 0)
        {
            //return index for empty list 
            iIndex = 0;

            return false;
        }

        //check if new value is past end list 
        if (Compare(m_tValues[0], tVal) > 0)
        {
            //return index for no value less than key
            iIndex = int.MinValue;

            return false;
        }

        //perform binary sarch on remaining values 

        int iSearchWindowMin = 0;
        int iSearchWindowMax = m_iCount - 1;

        while (iSearchWindowMin < iSearchWindowMax)
        {
            int iMid = (iSearchWindowMin + iSearchWindowMax + 1) / 2;

            if (Compare(m_tValues[UnsafeRemapIndex(iMid)], tVal) > -1)
            {
                iSearchWindowMax = iMid - 1;
            }
            else
            {
                iSearchWindowMin = iMid;
            }
        }

        iIndex = iSearchWindowMax;

        return true;
    }

    public bool TryGetFirstIndexLessThan(in T tVal, out int iIndex, out bool bCollision, out int iCollisionIndex)
    {
        iCollisionIndex = 0;
        bCollision = false;

        //check if there are any values in the array
        if (m_iCount == 0)
        {
            //return index for empty list 
            iIndex = 0;

            return false;
        }

        //check if new value is past end list 
        if (Compare(m_tValues[0], tVal) > 0)
        {
            //return index for no value greater than key
            iIndex = int.MinValue;

            return false;
        }

        //perform binary sarch on remaining values 

        int iSearchWindowMin = 0;
        int iSearchWindowMax = m_iCount - 1;

        while (iSearchWindowMin < iSearchWindowMax)
        {
            int iMid = (iSearchWindowMin + iSearchWindowMax + 1) / 2;

            int iCompareResult = Compare(m_tValues[UnsafeRemapIndex(iMid)], tVal);

            if (iCompareResult > -1)
            {
                iSearchWindowMax = iMid - 1;

                if (iCompareResult == 0)
                {
                    iCollisionIndex = iMid;
                    bCollision = true;
                }
            }
            else
            {
                iSearchWindowMin = iMid;
            }
        }

        iIndex = iSearchWindowMax;

        return true;
    }

    //performs binary search for key returns false if not found 
    public bool TryGetIndexOf(in T tVal, out int iIndex)
    {
        int iSearchWindowMin = 0;
        int iSearchWindowMax = m_iCount - 1;

        while (iSearchWindowMin <= iSearchWindowMax)
        {
            int iMid = (iSearchWindowMin + iSearchWindowMax) / 2;

            int iCompareResult = Compare(m_tValues[UnsafeRemapIndex(iMid)], tVal);

            if (iCompareResult < 0)
            {
                iSearchWindowMax = iMid - 1;
            }
            else if (iCompareResult > 0)
            {
                iSearchWindowMin = iMid + 1;
            }
            else
            {
                iIndex = iMid;

                return true;
            }
        }

        iIndex = 0;

        return false;
    }

    //inserts this item into the priority queue while keeping all the items that exist after it 
    //returns false and does not insert on collision with existing item
    public bool TryInsertEnqueue(in T tVal, out int iIndexOfMatchingItem)
    {
        //get index to insert 
        TryGetFirstIndexGreaterThan(tVal, out int iIndex, out bool bCollision, out iIndexOfMatchingItem);

        if (bCollision)
        {
            return false;
        }

        //inject the item into the queue
        Inject(tVal, iIndex);

        iIndexOfMatchingItem = 0;

        return true;
    }

    //removes all items that exists after this item then inserts this item 
    public void EnterPurgeInsert(in T tVal)
    {
        //check if there is no items before this item
        if (TryGetFirstIndexLessThan(tVal, out int iIndex) == false)
        {
            Clear();

            m_tValues[0] = tVal;

            m_iCount++;
        }

        //caclc index of insert 
        iIndex++;

        //check if array is large enough
        if (iIndex >= m_tValues.Length)
        {
            //expand queue
            ChangeCapacity(m_tValues.Length * 2);

            //set new value
            m_iQueueEnter = iIndex;
            m_iCount = iIndex + 1;
            m_tValues[m_iQueueEnter] = tVal;

            return;
        }

        //clear all items from array
        for (int i = iIndex + 1; i < m_iCount; i++)
        {
            int iAddressToClear = UnsafeRemapIndex(i);
            m_tValues[iAddressToClear] = default(T);

        }

        m_iQueueEnter = UnsafeRemapIndex(iIndex);
        m_tValues[m_iQueueEnter] = tVal;
        m_iCount = iIndex + 1;

        return;
    }

    public bool Dequeue(out T tVal)
    {
        //check if there is anything left in the queue
        if (m_iCount == 0)
        {
            tVal = default(T);
            return false;
        }

        //return item at old pos
        tVal = m_tValues[m_iQueueExit];

        //reset old values
        m_tValues[m_iQueueExit] = default(T);

        //index queue exit forwards
        m_iQueueExit = (m_iQueueExit + 1) % m_tValues.Length;

        //reduce number of items in list
        m_iCount--;

        return true;
    }

    //removes all items after an index excluding index
    public void ClearFrom(int iIndex)
    {
        for (int i = iIndex + 1; i < m_iCount; i++)
        {
            int iAddressToClear = UnsafeRemapIndex(i);

            m_tValues[iAddressToClear] = default(T);
        }

        m_iQueueEnter = RemapIndex(iIndex);
        m_iCount = iIndex + 1;

    }

    //remove all items including index
    public void ClearFromIncluding(int iIndex)
    {
        for (int i = iIndex; i < m_iCount; i++)
        {
            int iAddressToClear = UnsafeRemapIndex(i);

            m_tValues[iAddressToClear] = default(T);
        }

        m_iQueueEnter = RemapIndex(iIndex);
        m_iCount = iIndex;
    }

    //removes all items before an index
    public void ClearTo(int iIndex)
    {
        //clean up items being removed 
        for (int i = iIndex - 1; i > -1; i--)
        {
            int iAddressToClear = UnsafeRemapIndex(i);

            m_tValues[iAddressToClear] = default(T);
        }

        m_iQueueExit = RemapIndex(iIndex);
        m_iCount = m_iCount - iIndex;
    }

    //remove all items before including index
    public void ClearToIncluding(int iIndex)
    {
        if (iIndex >= m_iCount)
        {
            Clear();

            return;
        }

        //clean up items being removed 
        for (int i = iIndex; i > -1; i--)
        {
            int iAddressToClear = UnsafeRemapIndex(i);

            m_tValues[iAddressToClear] = default(T);
        }

        iIndex++;

        m_iQueueExit = RemapIndex(iIndex);
        m_iCount = m_iCount - iIndex;
    }

    //clears all items from key onward 
    public void ClearFrom(in T tVal)
    {
        if (TryGetFirstIndexGreaterThan(tVal, out int iIndex) == false)
        {
            //either the queue is empty or no items are larger than key
            return;
        }

        ClearFromIncluding(iIndex);
    }

    public void ClearTo(in T tVal)
    {
        if (TryGetFirstIndexLessThan(tVal, out int iIndex) == false)
        {
            //either there are no items in queue or all items are greater than target
            return;
        }

        ClearToIncluding(iIndex);
    }

    public T PeakDequeue()
    {
        if (m_iCount == 0)
        {
            return default(T);
        }

        return m_tValues[m_iQueueExit];
    }

    public T PeakEnqueue()
    {
        if (m_iCount == 0)
        {
            return default(T);
        }

        return m_tValues[m_iQueueEnter];
    }

    public void Clear()
    {
        m_iQueueEnter = 0;
        m_iQueueExit = 0;
        m_iCount = 0;

        //TODO:: make this only reset values that have already been set
        for (int i = 0; i < m_tValues.Length; i++)
        {
            m_tValues[i] = default(T);
        }
    }

    public void Remove(int iIndex)
    {
        //check if index is within range 
        if (iIndex < 0 || iIndex >= m_iCount)
        {
            return;
        }

        //check for last item in list 
        if (m_iCount == 1)
        {
            Clear();
            return;
        }

        //check which is easier end to shift, the exit segment or the enter segment 
        if (iIndex > m_iCount / 2)
        {
            //calculate the index in the array to remove the value
            int iRemoveIndex = UnsafeRemapIndex(iIndex);

            //shift values from removal point to end of array backwards
            int iItemsToShift = Math.Min((m_iCount - iIndex) - 1, (m_tValues.Length - 1) - iRemoveIndex);

            if (iItemsToShift > 0)
            {
                Array.Copy(m_tValues, iRemoveIndex + 1, m_tValues, iRemoveIndex, iItemsToShift);
            }

            //shift wrapped around values forwards
            if (m_iQueueEnter < iRemoveIndex)
            {
                //shift last value around 
                m_tValues[m_tValues.Length - 1] = m_tValues[0];

                if (m_iQueueEnter > 0)
                {
                    Array.Copy(m_tValues, 1, m_tValues, 0, m_iQueueEnter);
                }
            }

            //clear unused value at old end of queue
            m_tValues[m_iQueueEnter] = default;

            //calculate the new queue entrace point
            m_iQueueEnter = ((m_iQueueEnter + m_tValues.Length) - 1) % m_tValues.Length;

            //increment the number of items in queue
            m_iCount--;
        }
        else
        {
            //calculate the index in the array to remove the value from
            int iRemoveIndex = UnsafeRemapIndex(iIndex);

            //shift values from remove point to start of queue of array forward
            int iItemsToShift = Math.Min(iIndex, iRemoveIndex);

            if (iItemsToShift > 0)
            {
                int iCopyFromIndex = iRemoveIndex - iItemsToShift;

                Array.Copy(m_tValues, iCopyFromIndex, m_tValues, iCopyFromIndex + 1, iItemsToShift);
            }

            //shift wrapped around values forwards
            if (m_iQueueExit > iRemoveIndex)
            {
                //shift last value around 
                m_tValues[0] = m_tValues[m_tValues.Length - 1];

                int iWrappedAroundItems = (m_tValues.Length - 1) - m_iQueueExit;

                if (iWrappedAroundItems > 0)
                {
                    Array.Copy(m_tValues, m_iQueueExit, m_tValues, m_iQueueExit + 1, iWrappedAroundItems);
                }
            }

            //clear value to be removed 
            m_tValues[m_iQueueExit] = default;

            //calculate the new queue entrace point
            m_iQueueExit = (m_iQueueExit + 1) % m_tValues.Length;

            //increment the number of items in queue
            m_iCount--;
        }
    }

    private int Compare(in T tCompareFrom, in T tCompareTo)
    {
        return tCompareFrom.CompareTo(tCompareTo);
    }

    private int RemapIndex(int iIndex)
    {
        if (iIndex < 0 || iIndex >= m_iCount)
        {
            throw new IndexOutOfRangeException("Index " + iIndex + "is less than 0 or greater than " + (m_iCount - 1));
        }

        //remap index 
        return UnsafeRemapIndex(iIndex);
    }

    private int UnsafeRemapIndex(int iIndex)
    {
        return (m_iQueueExit + iIndex) % m_tValues.Length;
    }

    private void Inject(in T tVal, int iIndex)
    {
        //check for out of bounds insert 
        iIndex = Mathf.Clamp(iIndex, 0, m_iCount);

        //check if this will require a resize 
        if (m_iCount + 1 > m_tValues.Length)
        {
            int iNewCapacity = m_tValues.Length * 2;

            //create new arrays
            T[] keyNewKeyArray = new T[iNewCapacity];

            //check for empty resize
            if (m_iCount == 0)
            {
                keyNewKeyArray[0] = tVal;

                m_tValues = keyNewKeyArray;

                m_iCount = 1;
                m_iQueueExit = 0;
                m_iQueueEnter = 0;

                return;
            }

            //check if items exist before the insert location
            if (iIndex > 0)
            {
                //copy all the parts before the insert
                int iItemsToCopy = Math.Min(iIndex, m_iCount);

                //calc segments to copy
                int iPartALength = Math.Min(m_iQueueExit + iItemsToCopy, m_tValues.Length) - m_iQueueExit;
                int iPartBLength = iItemsToCopy - iPartALength;

                //copy accross existing values 
                Array.Copy(m_tValues, m_iQueueExit, keyNewKeyArray, 0, iPartALength);

                //copy accross wrap around segment
                if (iPartBLength > 0)
                {
                    Array.Copy(m_tValues, 0, keyNewKeyArray, iPartALength, iPartBLength);
                }
            }

            //insert item
            keyNewKeyArray[iIndex] = tVal;

            int iRemainingItemsToCopy = m_iCount - iIndex;

            //check if items exist after the insert location
            if (iRemainingItemsToCopy > 0)
            {
                //get start index
                int iStartIndex = RemapIndex(iIndex);

                //calc segments to copy
                int iPartALength = Math.Min(iStartIndex + iRemainingItemsToCopy, m_tValues.Length) - iStartIndex;
                int iPartBLength = iRemainingItemsToCopy - iPartALength;

                //copy accross existing values 
                Array.Copy(m_tValues, iStartIndex, keyNewKeyArray, iIndex + 1, iPartALength);

                //copy accross wrap around segment
                if (iPartBLength > 0)
                {
                    Array.Copy(m_tValues, 0, keyNewKeyArray, iPartALength, iPartBLength);
                }
            }

            m_tValues = keyNewKeyArray;

            m_iCount = m_iCount + 1;
            m_iQueueExit = 0;
            m_iQueueEnter = m_iCount - 1;

            return;
        }

        //check for empty structure
        if (m_iCount == 0)
        {
            m_tValues[0] = tVal;

            m_iCount = m_iCount + 1;
            m_iQueueExit = 0;
            m_iQueueEnter = m_iCount - 1;

            return;
        }

        //check if adding to end of queue
        if (iIndex >= m_iCount)
        {
            //shift enter index forwards
            m_iQueueEnter = (m_iQueueEnter + 1) % m_tValues.Length;

            //set item 
            m_tValues[m_iQueueEnter] = tVal;

            //increment the number of items in queue
            m_iCount++;

            return;
        }

        //check which is easier end to shift, the exit segment or the enter segment 
        if (iIndex > m_iCount / 2)
        {
            //calculate the new queue entrace point
            m_iQueueEnter = (m_iQueueEnter + 1) % m_tValues.Length;

            //calculate the index in the array to insert the value
            int iInsertIndex = UnsafeRemapIndex(iIndex);

            //shift wrapped around values forwards
            if (m_iQueueEnter < iInsertIndex)
            {
                if (m_iQueueEnter > 0)
                {
                    Array.Copy(m_tValues, 0, m_tValues, 1, m_iQueueEnter);
                }

                //shift last value around 
                m_tValues[0] = m_tValues[m_tValues.Length - 1];
            }

            //shift values from insertion point to end of array forwards
            int iItemsToShift = Math.Min((m_iCount - iIndex), (m_tValues.Length - 1) - iInsertIndex);

            if (iItemsToShift > 0)
            {
                Array.Copy(m_tValues, iInsertIndex, m_tValues, iInsertIndex + 1, iItemsToShift);
            }

            //insert item
            m_tValues[iInsertIndex] = tVal;

            //increment the number of items in queue
            m_iCount++;
        }
        else
        {
            //calculate the new queue entrace point
            m_iQueueExit = (m_iQueueExit + m_tValues.Length - 1) % m_tValues.Length;

            //calculate the index in the array to insert the value
            int iInsertIndex = UnsafeRemapIndex(iIndex);

            //shift wrapped around values forwards
            if (m_iQueueExit > iInsertIndex)
            {
                int iWrappedAroundItems = (m_tValues.Length - 1) - m_iQueueExit;

                if (iWrappedAroundItems > 0)
                {
                    Array.Copy(m_tValues, m_iQueueExit + 1, m_tValues, m_iQueueExit, iWrappedAroundItems);
                }

                //shift last value around 
                m_tValues[m_tValues.Length - 1] = m_tValues[0];

            }

            //shift values from insertion point to start of queue of array backward
            int iItemsToShift = Math.Min(iIndex, iInsertIndex);

            if (iItemsToShift > 0)
            {
                int iCopyToIndex = iInsertIndex - iItemsToShift;

                Array.Copy(m_tValues, iCopyToIndex + 1, m_tValues, iCopyToIndex, iItemsToShift);
            }

            //insert item
            m_tValues[iInsertIndex] = tVal;

            //increment the number of items in queue
            m_iCount++;
        }
    }

    //expands or contracts the underlying data structure 
    //if new size is smaller than number of items in list then
    //the more recently added items are dropped 
    private void ChangeCapacity(int iNewCapacity)
    {
        //create new arrays
        T[] keyNewKeyArray = new T[iNewCapacity];

        //check for empty resize
        if (m_iCount == 0)
        {
            m_tValues = keyNewKeyArray;

            m_iCount = 0;
            m_iQueueExit = 0;
            m_iQueueEnter = 0;

            return;
        }

        int iItemsToCopy = Math.Min(iNewCapacity, m_iCount);

        //calc segments to copy
        int iPartALength = Math.Min(m_iQueueExit + iItemsToCopy, m_tValues.Length) - m_iQueueExit;
        int iPartBLength = iItemsToCopy - iPartALength;

        //copy accross existing values 
        Array.Copy(m_tValues, m_iQueueExit, keyNewKeyArray, 0, iPartALength);

        //copy accross wrap around segment
        if (iPartBLength > 0)
        {
            Array.Copy(m_tValues, 0, keyNewKeyArray, iPartALength, iPartBLength);
        }

        m_iCount = iItemsToCopy;
        m_iQueueExit = 0;
        m_iQueueEnter = iItemsToCopy - 1;
    }
}

#endregion

//same as the above queues but uses a custom sorting function
#region SortUsingLambdaFunction
//this class represents a queue like structure where items are removed in order from one end but new items are added in a non linear way
public class SortedRandomAccessQueueUsingLambda<TKey, TValue>
{
    private Func<TKey, TKey, int> m_fncCompareFunction;
    private const int c_iDefaultCapacity = 8;
    private TKey[] m_keyKeys;
    private TValue[] m_valValues;
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
            return m_keyKeys.Length;
        }
    }

    public SortedRandomAccessQueueUsingLambda(Func<TKey, TKey, int> fncCompareFunction)
    {
        m_fncCompareFunction = fncCompareFunction;
        m_valValues = new TValue[c_iDefaultCapacity];
        m_keyKeys = new TKey[c_iDefaultCapacity];
    }

    public SortedRandomAccessQueueUsingLambda(Func<TKey, TKey, int> fncCompareFunction, int iStartCapacity)
    {
        m_fncCompareFunction = fncCompareFunction;
        m_valValues = new TValue[iStartCapacity];
        m_keyKeys = new TKey[iStartCapacity];
    }

    public TValue GetValueAtIndex(int iIndex)
    {
        return m_valValues[RemapIndex(iIndex)];
    }

    public void SetValueAtIndex(int iIndex, ref TValue Value)
    {
        m_valValues[RemapIndex(iIndex)] = Value;
    }

    public TKey GetKeyAtIndex(int iIndex)
    {
        return m_keyKeys[RemapIndex(iIndex)];
    }

    //Changes the key at index, CAUTION this does not resort the list and will cause problems if you change the key to a 
    //value greater than or less than the keys higher and lower neighbours 
    public void SetKeyAtIndex(int iIndex, ref TKey Key)
    {
        m_keyKeys[RemapIndex(iIndex)] = Key;
    }

    //find the first instance greater than index
    //if there are no items in list this function returns false and an index of 0
    //if all items in list are less than key then this function reutrns false and 
    //and index of max value
    public bool TryGetFirstIndexGreaterThan(in TKey Key, out int iIndex)
    {
        //check if there are any values in the array
        if (m_iCount == 0)
        {
            //return index for empty list 
            iIndex = 0;

            return false;
        }

        //check if new value is past end list 
        if (Compare(m_keyKeys[m_iQueueEnter], Key) < 0)
        {
            //return index for no value greater than key
            iIndex = int.MaxValue;

            return false;
        }

        //perform binary sarch on remaining values 

        int iSearchWindowMin = 0;
        int iSearchWindowMax = m_iCount - 1;

        while (iSearchWindowMin < iSearchWindowMax)
        {
            int iMid = (iSearchWindowMin + iSearchWindowMax) / 2;

            if (Compare(m_keyKeys[UnsafeRemapIndex(iMid)], Key) < 1)
            {
                iSearchWindowMin = iMid + 1;
            }
            else
            {
                iSearchWindowMax = iMid;
            }
        }

        iIndex = iSearchWindowMax;

        return true;
    }

    public bool TryGetFirstIndexGreaterThan(in TKey Key, out int iIndex, out bool bCollision, out int iCollisionIndex)
    {
        iCollisionIndex = 0;
        bCollision = false;

        //check if there are any values in the array
        if (m_iCount == 0)
        {
            //return index for empty list 
            iIndex = 0;

            return false;
        }

        //check if new value is past end list 
        if (Compare(m_keyKeys[m_iQueueEnter], Key) < 0)
        {
            //return index for no value greater than key
            iIndex = int.MaxValue;

            return false;
        }

        //perform binary sarch on remaining values 

        int iSearchWindowMin = 0;
        int iSearchWindowMax = m_iCount - 1;

        while (iSearchWindowMin < iSearchWindowMax)
        {
            int iMid = (iSearchWindowMin + iSearchWindowMax) / 2;

            int iCompareResult = Compare(m_keyKeys[UnsafeRemapIndex(iMid)], Key);

            if (iCompareResult < 1)
            {
                iSearchWindowMin = iMid + 1;

                if (iCompareResult == 0)
                {
                    iCollisionIndex = iMid;
                    bCollision = true;
                }
            }
            else
            {
                iSearchWindowMax = iMid;
            }
        }

        iIndex = iSearchWindowMax;

        return true;
    }

    //find first index less than key
    //if there are no items in list this funciton returns false and and index of 0
    //if all items in list are greater than key this function returns false and an index of min value
    public bool TryGetFirstIndexLessThan(in TKey Key, out int iIndex)
    {
        //check if there are any values in the array
        if (m_iCount == 0)
        {
            //return index for empty list 
            iIndex = 0;

            return false;
        }

        //check if new value is past end list 
        if (Compare(m_keyKeys[0], Key) > 0)
        {
            //return index for no value less than key
            iIndex = int.MinValue;

            return false;
        }

        //perform binary sarch on remaining values 

        int iSearchWindowMin = 0;
        int iSearchWindowMax = m_iCount - 1;

        while (iSearchWindowMin < iSearchWindowMax)
        {
            int iMid = (iSearchWindowMin + iSearchWindowMax + 1) / 2;

            if (Compare(m_keyKeys[UnsafeRemapIndex(iMid)], Key) > -1)
            {
                iSearchWindowMax = iMid - 1;
            }
            else
            {
                iSearchWindowMin = iMid;
            }
        }

        iIndex = iSearchWindowMax;

        return true;
    }

    public bool TryGetFirstIndexLessThan(in TKey Key, out int iIndex, out bool bCollision, out int iCollisionIndex)
    {
        iCollisionIndex = 0;
        bCollision = false;

        //check if there are any values in the array
        if (m_iCount == 0)
        {
            //return index for empty list 
            iIndex = 0;

            return false;
        }

        //check if new value is past end list 
        if (Compare(m_keyKeys[0], Key) > 0)
        {
            //return index for no value greater than key
            iIndex = int.MinValue;

            return false;
        }

        //perform binary sarch on remaining values 

        int iSearchWindowMin = 0;
        int iSearchWindowMax = m_iCount - 1;

        while (iSearchWindowMin < iSearchWindowMax)
        {
            int iMid = (iSearchWindowMin + iSearchWindowMax + 1) / 2;

            int iCompareResult = Compare(m_keyKeys[UnsafeRemapIndex(iMid)], Key);

            if (iCompareResult > -1)
            {
                iSearchWindowMax = iMid - 1;

                if (iCompareResult == 0)
                {
                    iCollisionIndex = iMid;
                    bCollision = true;
                }
            }
            else
            {
                iSearchWindowMin = iMid;
            }
        }

        iIndex = iSearchWindowMax;

        return true;
    }

    //performs binary search for key returns false if not found 
    public bool TryGetIndexOf(in TKey Key, out int iIndex)
    {
        int iSearchWindowMin = 0;
        int iSearchWindowMax = m_iCount - 1;

        while (iSearchWindowMin <= iSearchWindowMax)
        {
            int iMid = (iSearchWindowMin + iSearchWindowMax) / 2;

            int iCompareResult = Compare(m_keyKeys[UnsafeRemapIndex(iMid)], Key);

            if (iCompareResult < 0)
            {
                iSearchWindowMax = iMid - 1;
            }
            else if (iCompareResult > 0)
            {
                iSearchWindowMin = iMid + 1;
            }
            else
            {
                iIndex = iMid;

                return true;
            }
        }

        iIndex = 0;

        return false;
    }

    //returns the value corresponding to the key if it exists in the queue
    //if it cant be found value is default and false is returned 
    public bool TryGetValueOf(in TKey keyKey, out TValue valValue)
    {
        if (TryGetIndexOf(keyKey, out int iIndex) == true)
        {
            valValue = GetValueAtIndex(iIndex);

            return true;
        }

        valValue = default(TValue);

        return false;
    }

    //inserts this item into the priority queue while keeping all the items that exist after it 
    //returns false and does not insert on collision with existing item
    public bool TryInsertEnqueue(in TKey itemKey, in TValue itemToQueue, out int iIndexOfMatchingItem)
    {
        //get index to insert 
        TryGetFirstIndexGreaterThan(itemKey, out int iIndex, out bool bCollision, out iIndexOfMatchingItem);

        if (bCollision)
        {
            return false;
        }

        //inject the item into the queue
        Inject(itemKey, itemToQueue, iIndex);

        iIndexOfMatchingItem = 0;

        return true;
    }

    //removes all items that exists after this item then inserts this item 
    public void EnterPurgeInsert(in TKey itemKey, in TValue itemToQueue)
    {
        //check if there is no items before this item
        if (TryGetFirstIndexLessThan(itemKey, out int iIndex) == false)
        {
            Clear();

            m_keyKeys[0] = itemKey;
            m_valValues[0] = itemToQueue;

            m_iCount++;
        }

        //caclc index of insert 
        iIndex++;

        //check if array is large enough
        if (iIndex >= m_keyKeys.Length)
        {
            //expand queue
            ChangeCapacity(m_keyKeys.Length * 2);

            //set new value
            m_iQueueEnter = iIndex;
            m_iCount = iIndex + 1;
            m_keyKeys[m_iQueueEnter] = itemKey;
            m_valValues[m_iQueueEnter] = itemToQueue;

            return;
        }

        //clear all items from array
        for (int i = iIndex + 1; i < m_iCount; i++)
        {
            int iAddressToClear = UnsafeRemapIndex(i);
            m_keyKeys[iAddressToClear] = default(TKey);
            m_valValues[iAddressToClear] = default(TValue);
        }

        m_iQueueEnter = UnsafeRemapIndex(iIndex);
        m_keyKeys[m_iQueueEnter] = itemKey;
        m_valValues[m_iQueueEnter] = itemToQueue;
        m_iCount = iIndex + 1;

        return;
    }

    public bool Dequeue(out TKey keyKey, out TValue valValue)
    {
        //check if there is anything left in the queue
        if (m_iCount == 0)
        {
            keyKey = default(TKey);
            valValue = default(TValue);
            return false;
        }

        //return item at old pos
        keyKey = m_keyKeys[m_iQueueExit];
        valValue = m_valValues[m_iQueueExit];

        //reset old values
        m_keyKeys[m_iQueueExit] = default(TKey);
        m_valValues[m_iQueueExit] = default(TValue);

        //index queue exit forwards
        m_iQueueExit = (m_iQueueExit + 1) % m_valValues.Length;

        //reduce number of items in list
        m_iCount--;

        return true;
    }

    //removes all items after an index excluding index
    public void ClearFrom(int iIndex)
    {
        for (int i = iIndex + 1; i < m_iCount; i++)
        {
            int iAddressToClear = UnsafeRemapIndex(i);

            m_keyKeys[iAddressToClear] = default(TKey);
            m_valValues[iAddressToClear] = default(TValue);
        }

        m_iQueueEnter = RemapIndex(iIndex);
        m_iCount = iIndex + 1;

    }

    //remove all items including index
    public void ClearFromIncluding(int iIndex)
    {
        for (int i = iIndex; i < m_iCount; i++)
        {
            int iAddressToClear = UnsafeRemapIndex(i);

            m_keyKeys[iAddressToClear] = default(TKey);
            m_valValues[iAddressToClear] = default(TValue);
        }

        m_iQueueEnter = RemapIndex(iIndex);
        m_iCount = iIndex;
    }

    //removes all items before an index
    public void ClearTo(int iIndex)
    {
        //clean up items being removed 
        for (int i = iIndex - 1; i > -1; i--)
        {
            int iAddressToClear = UnsafeRemapIndex(i);

            m_keyKeys[iAddressToClear] = default(TKey);
            m_valValues[iAddressToClear] = default(TValue);
        }

        m_iQueueExit = RemapIndex(iIndex);
        m_iCount = m_iCount - iIndex;
    }

    //remove all items before including index
    public void ClearToIncluding(int iIndex)
    {
        if (iIndex >= m_iCount)
        {
            Clear();

            return;
        }

        //clean up items being removed 
        for (int i = iIndex; i > -1; i--)
        {
            int iAddressToClear = UnsafeRemapIndex(i);

            m_keyKeys[iAddressToClear] = default(TKey);
            m_valValues[iAddressToClear] = default(TValue);
        }

        iIndex++;

        m_iQueueExit = RemapIndex(iIndex);
        m_iCount = m_iCount - iIndex;
    }

    //clears all items from key onward 
    public void ClearFrom(in TKey keyKey)
    {
        if (TryGetFirstIndexGreaterThan(keyKey, out int iIndex) == false)
        {
            //either the queue is empty or no items are larger than key
            return;
        }

        ClearFromIncluding(iIndex);
    }

    public void ClearTo(in TKey keyKey)
    {
        if (TryGetFirstIndexLessThan(keyKey, out int iIndex) == false)
        {
            //either there are no items in queue or all items are greater than target
            return;
        }

        ClearToIncluding(iIndex);
    }

    public TKey PeakKeyDequeue()
    {
        if (m_iCount == 0)
        {
            return default(TKey);
        }

        return m_keyKeys[m_iQueueExit];
    }

    public TKey PeakKeyEnqueue()
    {
        if (m_iCount == 0)
        {
            return default(TKey);
        }

        return m_keyKeys[m_iQueueEnter];
    }

    public TValue PeakValueDequeue()
    {
        if (m_iCount == 0)
        {
            return default(TValue);
        }

        return m_valValues[m_iQueueExit];
    }

    public TValue PeakValueEnqueue()
    {
        if (m_iCount == 0)
        {
            return default(TValue);
        }

        return m_valValues[m_iQueueEnter];
    }

    public void Clear()
    {
        m_iQueueEnter = 0;
        m_iQueueExit = 0;
        m_iCount = 0;

        //TODO:: make this only reset values that have already been set
        for (int i = 0; i < m_valValues.Length; i++)
        {
            m_valValues[i] = default(TValue);
            m_keyKeys[i] = default(TKey);
        }
    }

    public void Remove(int iIndex)
    {
        //check if index is within range 
        if (iIndex < 0 || iIndex >= m_iCount)
        {
            return;
        }

        //check for last item in list 
        if (m_iCount == 1)
        {
            Clear();
            return;
        }

        //check which is easier end to shift, the exit segment or the enter segment 
        if (iIndex > m_iCount / 2)
        {
            //calculate the index in the array to remove the value
            int iRemoveIndex = UnsafeRemapIndex(iIndex);

            //shift values from removal point to end of array backwards
            int iItemsToShift = Math.Min((m_iCount - iIndex) - 1, (m_keyKeys.Length - 1) - iRemoveIndex);

            if (iItemsToShift > 0)
            {
                Array.Copy(m_keyKeys, iRemoveIndex + 1, m_keyKeys, iRemoveIndex, iItemsToShift);
                Array.Copy(m_valValues, iRemoveIndex + 1, m_valValues, iRemoveIndex, iItemsToShift);
            }

            //shift wrapped around values forwards
            if (m_iQueueEnter < iRemoveIndex)
            {
                //shift last value around 
                m_keyKeys[m_keyKeys.Length - 1] = m_keyKeys[0];
                m_valValues[m_keyKeys.Length - 1] = m_valValues[0];

                if (m_iQueueEnter > 0)
                {
                    Array.Copy(m_keyKeys, 1, m_keyKeys, 0, m_iQueueEnter);
                    Array.Copy(m_valValues, 1, m_valValues, 0, m_iQueueEnter);
                }
            }

            //clear unused value at old end of queue
            m_keyKeys[m_iQueueEnter] = default;
            m_valValues[m_iQueueEnter] = default;

            //calculate the new queue entrace point
            m_iQueueEnter = ((m_iQueueEnter + m_keyKeys.Length) - 1) % m_keyKeys.Length;

            //increment the number of items in queue
            m_iCount--;
        }
        else
        {
            //calculate the index in the array to remove the value from
            int iRemoveIndex = UnsafeRemapIndex(iIndex);

            //shift values from remove point to start of queue of array forward
            int iItemsToShift = Math.Min(iIndex, iRemoveIndex);

            if (iItemsToShift > 0)
            {
                int iCopyFromIndex = iRemoveIndex - iItemsToShift;

                Array.Copy(m_keyKeys, iCopyFromIndex, m_keyKeys, iCopyFromIndex + 1, iItemsToShift);
                Array.Copy(m_valValues, iCopyFromIndex, m_valValues, iCopyFromIndex + 1, iItemsToShift);
            }

            //shift wrapped around values forwards
            if (m_iQueueExit > iRemoveIndex)
            {
                //shift last value around 
                m_keyKeys[0] = m_keyKeys[m_keyKeys.Length - 1];
                m_valValues[0] = m_valValues[m_keyKeys.Length - 1];

                int iWrappedAroundItems = (m_keyKeys.Length - 1) - m_iQueueExit;

                if (iWrappedAroundItems > 0)
                {
                    Array.Copy(m_keyKeys, m_iQueueExit, m_keyKeys, m_iQueueExit + 1, iWrappedAroundItems);
                    Array.Copy(m_valValues, m_iQueueExit, m_valValues, m_iQueueExit + 1, iWrappedAroundItems);
                }
            }

            //clear value to be removed 
            m_keyKeys[m_iQueueExit] = default;
            m_valValues[m_iQueueExit] = default;

            //calculate the new queue entrace point
            m_iQueueExit = (m_iQueueExit + 1) % m_keyKeys.Length;

            //increment the number of items in queue
            m_iCount--;
        }
    }

    private int Compare(in TKey keyCompareFrom, in TKey keyCompareTo)
    {
        return m_fncCompareFunction.Invoke(keyCompareFrom,keyCompareTo);
    }

    private int RemapIndex(int iIndex)
    {
        if (iIndex < 0 || iIndex >= m_iCount)
        {
            throw new IndexOutOfRangeException("Index " + iIndex + "is less than 0 or greater than " + (m_iCount - 1));
        }

        //remap index 
        return UnsafeRemapIndex(iIndex);
    }

    private int UnsafeRemapIndex(int iIndex)
    {
        return (m_iQueueExit + iIndex) % m_valValues.Length;
    }

    private void Inject(in TKey keyKeyToInject, in TValue valValueToInject, int iIndex)
    {
        //check for out of bounds insert 
        iIndex = Mathf.Clamp(iIndex, 0, m_iCount);

        //check if this will require a resize 
        if (m_iCount + 1 > m_keyKeys.Length)
        {
            int iNewCapacity = m_keyKeys.Length * 2;

            //create new arrays
            TKey[] keyNewKeyArray = new TKey[iNewCapacity];
            TValue[] valNewValueArray = new TValue[iNewCapacity];

            //check for empty resize
            if (m_iCount == 0)
            {
                keyNewKeyArray[0] = keyKeyToInject;
                valNewValueArray[0] = valValueToInject;

                m_keyKeys = keyNewKeyArray;
                m_valValues = valNewValueArray;

                m_iCount = 1;
                m_iQueueExit = 0;
                m_iQueueEnter = 0;

                return;
            }

            //check if items exist before the insert location
            if (iIndex > 0)
            {
                //copy all the parts before the insert
                int iItemsToCopy = Math.Min(iIndex, m_iCount);

                //calc segments to copy
                int iPartALength = Math.Min(m_iQueueExit + iItemsToCopy, m_keyKeys.Length) - m_iQueueExit;
                int iPartBLength = iItemsToCopy - iPartALength;

                //copy accross existing values 
                Array.Copy(m_keyKeys, m_iQueueExit, keyNewKeyArray, 0, iPartALength);
                Array.Copy(m_valValues, m_iQueueExit, valNewValueArray, 0, iPartALength);

                //copy accross wrap around segment
                if (iPartBLength > 0)
                {
                    Array.Copy(m_keyKeys, 0, keyNewKeyArray, iPartALength, iPartBLength);
                    Array.Copy(m_valValues, 0, valNewValueArray, iPartALength, iPartBLength);
                }
            }

            //insert item
            keyNewKeyArray[iIndex] = keyKeyToInject;
            valNewValueArray[iIndex] = valValueToInject;

            int iRemainingItemsToCopy = m_iCount - iIndex;

            //check if items exist after the insert location
            if (iRemainingItemsToCopy > 0)
            {
                //get start index
                int iStartIndex = RemapIndex(iIndex);

                //calc segments to copy
                int iPartALength = Math.Min(iStartIndex + iRemainingItemsToCopy, m_keyKeys.Length) - iStartIndex;
                int iPartBLength = iRemainingItemsToCopy - iPartALength;

                //copy accross existing values 
                Array.Copy(m_keyKeys, iStartIndex, keyNewKeyArray, iIndex + 1, iPartALength);
                Array.Copy(m_valValues, iStartIndex, valNewValueArray, iIndex + 1, iPartALength);

                //copy accross wrap around segment
                if (iPartBLength > 0)
                {
                    Array.Copy(m_keyKeys, 0, keyNewKeyArray, iPartALength, iPartBLength);
                    Array.Copy(m_valValues, 0, valNewValueArray, iPartALength, iPartBLength);
                }
            }

            m_keyKeys = keyNewKeyArray;
            m_valValues = valNewValueArray;

            m_iCount = m_iCount + 1;
            m_iQueueExit = 0;
            m_iQueueEnter = m_iCount - 1;

            return;
        }

        //check for empty structure
        if (m_iCount == 0)
        {
            m_keyKeys[0] = keyKeyToInject;
            m_valValues[0] = valValueToInject;

            m_iCount = m_iCount + 1;
            m_iQueueExit = 0;
            m_iQueueEnter = m_iCount - 1;

            return;
        }

        //check if adding to end of queue
        if (iIndex >= m_iCount)
        {
            //shift enter index forwards
            m_iQueueEnter = (m_iQueueEnter + 1) % m_valValues.Length;

            //set item 
            m_keyKeys[m_iQueueEnter] = keyKeyToInject;
            m_valValues[m_iQueueEnter] = valValueToInject;

            //increment the number of items in queue
            m_iCount++;

            return;
        }

        //check which is easier end to shift, the exit segment or the enter segment 
        if (iIndex > m_iCount / 2)
        {
            //calculate the new queue entrace point
            m_iQueueEnter = (m_iQueueEnter + 1) % m_keyKeys.Length;

            //calculate the index in the array to insert the value
            int iInsertIndex = UnsafeRemapIndex(iIndex);

            //shift wrapped around values forwards
            if (m_iQueueEnter < iInsertIndex)
            {
                if (m_iQueueEnter > 0)
                {
                    Array.Copy(m_keyKeys, 0, m_keyKeys, 1, m_iQueueEnter);
                    Array.Copy(m_valValues, 0, m_valValues, 1, m_iQueueEnter);
                }

                //shift last value around 
                m_keyKeys[0] = m_keyKeys[m_keyKeys.Length - 1];
                m_valValues[0] = m_valValues[m_keyKeys.Length - 1];
            }

            //shift values from insertion point to end of array forwards
            int iItemsToShift = Math.Min((m_iCount - iIndex), (m_keyKeys.Length - 1) - iInsertIndex);

            if (iItemsToShift > 0)
            {
                Array.Copy(m_keyKeys, iInsertIndex, m_keyKeys, iInsertIndex + 1, iItemsToShift);
                Array.Copy(m_valValues, iInsertIndex, m_valValues, iInsertIndex + 1, iItemsToShift);
            }

            //insert item
            m_keyKeys[iInsertIndex] = keyKeyToInject;
            m_valValues[iInsertIndex] = valValueToInject;

            //increment the number of items in queue
            m_iCount++;
        }
        else
        {
            //calculate the new queue entrace point
            m_iQueueExit = (m_iQueueExit + m_keyKeys.Length - 1) % m_keyKeys.Length;

            //calculate the index in the array to insert the value
            int iInsertIndex = UnsafeRemapIndex(iIndex);

            //shift wrapped around values forwards
            if (m_iQueueExit > iInsertIndex)
            {
                int iWrappedAroundItems = (m_keyKeys.Length - 1) - m_iQueueExit;

                if (iWrappedAroundItems > 0)
                {
                    Array.Copy(m_keyKeys, m_iQueueExit + 1, m_keyKeys, m_iQueueExit, iWrappedAroundItems);
                    Array.Copy(m_valValues, m_iQueueExit + 1, m_valValues, m_iQueueExit, iWrappedAroundItems);
                }

                //shift last value around 
                m_keyKeys[m_keyKeys.Length - 1] = m_keyKeys[0];
                m_valValues[m_keyKeys.Length - 1] = m_valValues[0];

            }

            //shift values from insertion point to start of queue of array backward
            int iItemsToShift = Math.Min(iIndex, iInsertIndex);

            if (iItemsToShift > 0)
            {
                int iCopyToIndex = iInsertIndex - iItemsToShift;

                Array.Copy(m_keyKeys, iCopyToIndex + 1, m_keyKeys, iCopyToIndex, iItemsToShift);
                Array.Copy(m_valValues, iCopyToIndex + 1, m_valValues, iCopyToIndex, iItemsToShift);
            }

            //insert item
            m_keyKeys[iInsertIndex] = keyKeyToInject;
            m_valValues[iInsertIndex] = valValueToInject;

            //increment the number of items in queue
            m_iCount++;
        }
    }

    //expands or contracts the underlying data structure 
    //if new size is smaller than number of items in list then
    //the more recently added items are dropped 
    private void ChangeCapacity(int iNewCapacity)
    {
        //create new arrays
        TKey[] keyNewKeyArray = new TKey[iNewCapacity];
        TValue[] valNewValueArray = new TValue[iNewCapacity];

        //check for empty resize
        if (m_iCount == 0)
        {
            m_keyKeys = keyNewKeyArray;
            m_valValues = valNewValueArray;

            m_iCount = 0;
            m_iQueueExit = 0;
            m_iQueueEnter = 0;

            return;
        }

        int iItemsToCopy = Math.Min(iNewCapacity, m_iCount);

        //calc segments to copy
        int iPartALength = Math.Min(m_iQueueExit + iItemsToCopy, m_keyKeys.Length) - m_iQueueExit;
        int iPartBLength = iItemsToCopy - iPartALength;

        //copy accross existing values 
        Array.Copy(m_keyKeys, m_iQueueExit, keyNewKeyArray, 0, iPartALength);
        Array.Copy(m_valValues, m_iQueueExit, valNewValueArray, 0, iPartALength);

        //copy accross wrap around segment
        if (iPartBLength > 0)
        {
            Array.Copy(m_keyKeys, 0, keyNewKeyArray, iPartALength, iPartBLength);
            Array.Copy(m_valValues, 0, valNewValueArray, iPartALength, iPartBLength);
        }

        m_iCount = iItemsToCopy;
        m_iQueueExit = 0;
        m_iQueueEnter = iItemsToCopy - 1;
    }
}

//this class represents a queue like structure where items are removed in order from one end but new items are added in a non linear way
public class SortedRandomAccessQueueUsingLambda<T>
{
    private Func<T, T, int> m_fncCompareFunction;
    private const int c_iDefaultCapacity = 8;
    private T[] m_tValues;
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
            return m_tValues.Length;
        }
    }

    public SortedRandomAccessQueueUsingLambda(Func<T, T, int> fncCompareFunction)
    {
        m_fncCompareFunction = fncCompareFunction;
        m_tValues = new T[c_iDefaultCapacity];
    }

    public SortedRandomAccessQueueUsingLambda(Func<T, T, int> fncCompareFunction, int iStartCapacity)
    {
        m_fncCompareFunction = fncCompareFunction;
        m_tValues = new T[iStartCapacity];
    }

    public T GetValueAtIndex(int iIndex)
    {
        return m_tValues[RemapIndex(iIndex)];
    }

    public void SetValueAtIndex(int iIndex, ref T Value)
    {
        m_tValues[RemapIndex(iIndex)] = Value;
    }

    //find the first instance greater than index
    //if there are no items in list this function returns false and an index of 0
    //if all items in list are less than key then this function reutrns false and 
    //and index of max value
    public bool TryGetFirstIndexGreaterThan(in T tVal, out int iIndex)
    {
        //check if there are any values in the array
        if (m_iCount == 0)
        {
            //return index for empty list 
            iIndex = 0;

            return false;
        }

        //check if new value is past end list 
        if (Compare(m_tValues[m_iQueueEnter], tVal) < 0)
        {
            //return index for no value greater than key
            iIndex = int.MaxValue;

            return false;
        }

        //perform binary sarch on remaining values 

        int iSearchWindowMin = 0;
        int iSearchWindowMax = m_iCount - 1;

        while (iSearchWindowMin < iSearchWindowMax)
        {
            int iMid = (iSearchWindowMin + iSearchWindowMax) / 2;

            if (Compare(m_tValues[UnsafeRemapIndex(iMid)], tVal) < 1)
            {
                iSearchWindowMin = iMid + 1;
            }
            else
            {
                iSearchWindowMax = iMid;
            }
        }

        iIndex = iSearchWindowMax;

        return true;
    }

    public bool TryGetFirstIndexGreaterThan(in T tVal, out int iIndex, out bool bCollision, out int iCollisionIndex)
    {
        iCollisionIndex = 0;
        bCollision = false;

        //check if there are any values in the array
        if (m_iCount == 0)
        {
            //return index for empty list 
            iIndex = 0;

            return false;
        }

        //check if new value is past end list 
        if (Compare(m_tValues[m_iQueueEnter], tVal) < 0)
        {
            //return index for no value greater than key
            iIndex = int.MaxValue;

            return false;
        }

        //perform binary sarch on remaining values 

        int iSearchWindowMin = 0;
        int iSearchWindowMax = m_iCount - 1;

        while (iSearchWindowMin < iSearchWindowMax)
        {
            int iMid = (iSearchWindowMin + iSearchWindowMax) / 2;

            int iCompareResult = Compare(m_tValues[UnsafeRemapIndex(iMid)], tVal);

            if (iCompareResult < 1)
            {
                iSearchWindowMin = iMid + 1;

                if (iCompareResult == 0)
                {
                    iCollisionIndex = iMid;
                    bCollision = true;
                }
            }
            else
            {
                iSearchWindowMax = iMid;
            }
        }

        iIndex = iSearchWindowMax;

        return true;
    }

    //find first index less than key
    //if there are no items in list this funciton returns false and and index of 0
    //if all items in list are greater than key this function returns false and an index of min value
    public bool TryGetFirstIndexLessThan(in T tVal, out int iIndex)
    {
        //check if there are any values in the array
        if (m_iCount == 0)
        {
            //return index for empty list 
            iIndex = 0;

            return false;
        }

        //check if new value is past end list 
        if (Compare(m_tValues[0], tVal) > 0)
        {
            //return index for no value less than key
            iIndex = int.MinValue;

            return false;
        }

        //perform binary sarch on remaining values 

        int iSearchWindowMin = 0;
        int iSearchWindowMax = m_iCount - 1;

        while (iSearchWindowMin < iSearchWindowMax)
        {
            int iMid = (iSearchWindowMin + iSearchWindowMax + 1) / 2;

            if (Compare(m_tValues[UnsafeRemapIndex(iMid)], tVal) > -1)
            {
                iSearchWindowMax = iMid - 1;
            }
            else
            {
                iSearchWindowMin = iMid;
            }
        }

        iIndex = iSearchWindowMax;

        return true;
    }

    public bool TryGetFirstIndexLessThan(in T tVal, out int iIndex, out bool bCollision, out int iCollisionIndex)
    {
        iCollisionIndex = 0;
        bCollision = false;

        //check if there are any values in the array
        if (m_iCount == 0)
        {
            //return index for empty list 
            iIndex = 0;

            return false;
        }

        //check if new value is past end list 
        if (Compare(m_tValues[0], tVal) > 0)
        {
            //return index for no value greater than key
            iIndex = int.MinValue;

            return false;
        }

        //perform binary sarch on remaining values 

        int iSearchWindowMin = 0;
        int iSearchWindowMax = m_iCount - 1;

        while (iSearchWindowMin < iSearchWindowMax)
        {
            int iMid = (iSearchWindowMin + iSearchWindowMax + 1) / 2;

            int iCompareResult = Compare(m_tValues[UnsafeRemapIndex(iMid)], tVal);

            if (iCompareResult > -1)
            {
                iSearchWindowMax = iMid - 1;

                if (iCompareResult == 0)
                {
                    iCollisionIndex = iMid;
                    bCollision = true;
                }
            }
            else
            {
                iSearchWindowMin = iMid;
            }
        }

        iIndex = iSearchWindowMax;

        return true;
    }

    //performs binary search for key returns false if not found 
    public bool TryGetIndexOf(in T tVal, out int iIndex)
    {
        int iSearchWindowMin = 0;
        int iSearchWindowMax = m_iCount - 1;

        while (iSearchWindowMin <= iSearchWindowMax)
        {
            int iMid = (iSearchWindowMin + iSearchWindowMax) / 2;

            int iCompareResult = Compare(m_tValues[UnsafeRemapIndex(iMid)], tVal);

            if (iCompareResult < 0)
            {
                iSearchWindowMax = iMid - 1;
            }
            else if (iCompareResult > 0)
            {
                iSearchWindowMin = iMid + 1;
            }
            else
            {
                iIndex = iMid;

                return true;
            }
        }

        iIndex = 0;

        return false;
    }

    //inserts this item into the priority queue while keeping all the items that exist after it 
    //returns false and does not insert on collision with existing item
    public bool TryInsertEnqueue(in T tVal, out int iIndexOfMatchingItem)
    {
        //get index to insert 
        TryGetFirstIndexGreaterThan(tVal, out int iIndex, out bool bCollision, out iIndexOfMatchingItem);

        if (bCollision)
        {
            return false;
        }

        //inject the item into the queue
        Inject(tVal, iIndex);

        iIndexOfMatchingItem = 0;

        return true;
    }

    //removes all items that exists after this item then inserts this item 
    public void EnterPurgeInsert(in T tVal)
    {
        //check if there is no items before this item
        if (TryGetFirstIndexLessThan(tVal, out int iIndex) == false)
        {
            Clear();

            m_tValues[0] = tVal;

            m_iCount++;
        }

        //caclc index of insert 
        iIndex++;

        //check if array is large enough
        if (iIndex >= m_tValues.Length)
        {
            //expand queue
            ChangeCapacity(m_tValues.Length * 2);

            //set new value
            m_iQueueEnter = iIndex;
            m_iCount = iIndex + 1;
            m_tValues[m_iQueueEnter] = tVal;

            return;
        }

        //clear all items from array
        for (int i = iIndex + 1; i < m_iCount; i++)
        {
            int iAddressToClear = UnsafeRemapIndex(i);
            m_tValues[iAddressToClear] = default(T);

        }

        m_iQueueEnter = UnsafeRemapIndex(iIndex);
        m_tValues[m_iQueueEnter] = tVal;
        m_iCount = iIndex + 1;

        return;
    }

    public bool Dequeue(out T tVal)
    {
        //check if there is anything left in the queue
        if (m_iCount == 0)
        {
            tVal = default(T);
            return false;
        }

        //return item at old pos
        tVal = m_tValues[m_iQueueExit];

        //reset old values
        m_tValues[m_iQueueExit] = default(T);

        //index queue exit forwards
        m_iQueueExit = (m_iQueueExit + 1) % m_tValues.Length;

        //reduce number of items in list
        m_iCount--;

        return true;
    }

    //removes all items after an index excluding index
    public void ClearFrom(int iIndex)
    {
        for (int i = iIndex + 1; i < m_iCount; i++)
        {
            int iAddressToClear = UnsafeRemapIndex(i);

            m_tValues[iAddressToClear] = default(T);
        }

        m_iQueueEnter = RemapIndex(iIndex);
        m_iCount = iIndex + 1;

    }

    //remove all items including index
    public void ClearFromIncluding(int iIndex)
    {
        for (int i = iIndex; i < m_iCount; i++)
        {
            int iAddressToClear = UnsafeRemapIndex(i);

            m_tValues[iAddressToClear] = default(T);
        }

        m_iQueueEnter = RemapIndex(iIndex);
        m_iCount = iIndex;
    }

    //removes all items before an index
    public void ClearTo(int iIndex)
    {
        //clean up items being removed 
        for (int i = iIndex - 1; i > -1; i--)
        {
            int iAddressToClear = UnsafeRemapIndex(i);

            m_tValues[iAddressToClear] = default(T);
        }

        m_iQueueExit = RemapIndex(iIndex);
        m_iCount = m_iCount - iIndex;
    }

    //remove all items before including index
    public void ClearToIncluding(int iIndex)
    {
        if (iIndex >= m_iCount)
        {
            Clear();

            return;
        }

        //clean up items being removed 
        for (int i = iIndex; i > -1; i--)
        {
            int iAddressToClear = UnsafeRemapIndex(i);

            m_tValues[iAddressToClear] = default(T);
        }

        iIndex++;

        m_iQueueExit = RemapIndex(iIndex);
        m_iCount = m_iCount - iIndex;
    }

    //clears all items from key onward 
    public void ClearFrom(in T tVal)
    {
        if (TryGetFirstIndexGreaterThan(tVal, out int iIndex) == false)
        {
            //either the queue is empty or no items are larger than key
            return;
        }

        ClearFromIncluding(iIndex);
    }

    public void ClearTo(in T tVal)
    {
        if (TryGetFirstIndexLessThan(tVal, out int iIndex) == false)
        {
            //either there are no items in queue or all items are greater than target
            return;
        }

        ClearToIncluding(iIndex);
    }

    public T PeakDequeue()
    {
        if (m_iCount == 0)
        {
            return default(T);
        }

        return m_tValues[m_iQueueExit];
    }

    public T PeakEnqueue()
    {
        if (m_iCount == 0)
        {
            return default(T);
        }

        return m_tValues[m_iQueueEnter];
    }

    public void Clear()
    {
        m_iQueueEnter = 0;
        m_iQueueExit = 0;
        m_iCount = 0;

        //TODO:: make this only reset values that have already been set
        for (int i = 0; i < m_tValues.Length; i++)
        {
            m_tValues[i] = default(T);
        }
    }

    public void Remove(int iIndex)
    {
        //check if index is within range 
        if (iIndex < 0 || iIndex >= m_iCount)
        {
            return;
        }

        //check for last item in list 
        if (m_iCount == 1)
        {
            Clear();
            return;
        }

        //check which is easier end to shift, the exit segment or the enter segment 
        if (iIndex > m_iCount / 2)
        {
            //calculate the index in the array to remove the value
            int iRemoveIndex = UnsafeRemapIndex(iIndex);

            //shift values from removal point to end of array backwards
            int iItemsToShift = Math.Min((m_iCount - iIndex) - 1, (m_tValues.Length - 1) - iRemoveIndex);

            if (iItemsToShift > 0)
            {
                Array.Copy(m_tValues, iRemoveIndex + 1, m_tValues, iRemoveIndex, iItemsToShift);
            }

            //shift wrapped around values forwards
            if (m_iQueueEnter < iRemoveIndex)
            {
                //shift last value around 
                m_tValues[m_tValues.Length - 1] = m_tValues[0];

                if (m_iQueueEnter > 0)
                {
                    Array.Copy(m_tValues, 1, m_tValues, 0, m_iQueueEnter);
                }
            }

            //clear unused value at old end of queue
            m_tValues[m_iQueueEnter] = default;

            //calculate the new queue entrace point
            m_iQueueEnter = ((m_iQueueEnter + m_tValues.Length) - 1) % m_tValues.Length;

            //increment the number of items in queue
            m_iCount--;
        }
        else
        {
            //calculate the index in the array to remove the value from
            int iRemoveIndex = UnsafeRemapIndex(iIndex);

            //shift values from remove point to start of queue of array forward
            int iItemsToShift = Math.Min(iIndex, iRemoveIndex);

            if (iItemsToShift > 0)
            {
                int iCopyFromIndex = iRemoveIndex - iItemsToShift;

                Array.Copy(m_tValues, iCopyFromIndex, m_tValues, iCopyFromIndex + 1, iItemsToShift);
            }

            //shift wrapped around values forwards
            if (m_iQueueExit > iRemoveIndex)
            {
                //shift last value around 
                m_tValues[0] = m_tValues[m_tValues.Length - 1];

                int iWrappedAroundItems = (m_tValues.Length - 1) - m_iQueueExit;

                if (iWrappedAroundItems > 0)
                {
                    Array.Copy(m_tValues, m_iQueueExit, m_tValues, m_iQueueExit + 1, iWrappedAroundItems);
                }
            }

            //clear value to be removed 
            m_tValues[m_iQueueExit] = default;

            //calculate the new queue entrace point
            m_iQueueExit = (m_iQueueExit + 1) % m_tValues.Length;

            //increment the number of items in queue
            m_iCount--;
        }
    }

    private int Compare(in T tCompareFrom, in T tCompareTo)
    {
        return m_fncCompareFunction(tCompareFrom,tCompareTo);
    }

    private int RemapIndex(int iIndex)
    {
        if (iIndex < 0 || iIndex >= m_iCount)
        {
            throw new IndexOutOfRangeException("Index " + iIndex + "is less than 0 or greater than " + (m_iCount - 1));
        }

        //remap index 
        return UnsafeRemapIndex(iIndex);
    }

    private int UnsafeRemapIndex(int iIndex)
    {
        return (m_iQueueExit + iIndex) % m_tValues.Length;
    }

    private void Inject(in T tVal, int iIndex)
    {
        //check for out of bounds insert 
        iIndex = Mathf.Clamp(iIndex, 0, m_iCount);

        //check if this will require a resize 
        if (m_iCount + 1 > m_tValues.Length)
        {
            int iNewCapacity = m_tValues.Length * 2;

            //create new arrays
            T[] keyNewKeyArray = new T[iNewCapacity];

            //check for empty resize
            if (m_iCount == 0)
            {
                keyNewKeyArray[0] = tVal;

                m_tValues = keyNewKeyArray;

                m_iCount = 1;
                m_iQueueExit = 0;
                m_iQueueEnter = 0;

                return;
            }

            //check if items exist before the insert location
            if (iIndex > 0)
            {
                //copy all the parts before the insert
                int iItemsToCopy = Math.Min(iIndex, m_iCount);

                //calc segments to copy
                int iPartALength = Math.Min(m_iQueueExit + iItemsToCopy, m_tValues.Length) - m_iQueueExit;
                int iPartBLength = iItemsToCopy - iPartALength;

                //copy accross existing values 
                Array.Copy(m_tValues, m_iQueueExit, keyNewKeyArray, 0, iPartALength);

                //copy accross wrap around segment
                if (iPartBLength > 0)
                {
                    Array.Copy(m_tValues, 0, keyNewKeyArray, iPartALength, iPartBLength);
                }
            }

            //insert item
            keyNewKeyArray[iIndex] = tVal;

            int iRemainingItemsToCopy = m_iCount - iIndex;

            //check if items exist after the insert location
            if (iRemainingItemsToCopy > 0)
            {
                //get start index
                int iStartIndex = RemapIndex(iIndex);

                //calc segments to copy
                int iPartALength = Math.Min(iStartIndex + iRemainingItemsToCopy, m_tValues.Length) - iStartIndex;
                int iPartBLength = iRemainingItemsToCopy - iPartALength;

                //copy accross existing values 
                Array.Copy(m_tValues, iStartIndex, keyNewKeyArray, iIndex + 1, iPartALength);

                //copy accross wrap around segment
                if (iPartBLength > 0)
                {
                    Array.Copy(m_tValues, 0, keyNewKeyArray, iPartALength, iPartBLength);
                }
            }

            m_tValues = keyNewKeyArray;

            m_iCount = m_iCount + 1;
            m_iQueueExit = 0;
            m_iQueueEnter = m_iCount - 1;

            return;
        }

        //check for empty structure
        if (m_iCount == 0)
        {
            m_tValues[0] = tVal;

            m_iCount = m_iCount + 1;
            m_iQueueExit = 0;
            m_iQueueEnter = m_iCount - 1;

            return;
        }

        //check if adding to end of queue
        if (iIndex >= m_iCount)
        {
            //shift enter index forwards
            m_iQueueEnter = (m_iQueueEnter + 1) % m_tValues.Length;

            //set item 
            m_tValues[m_iQueueEnter] = tVal;

            //increment the number of items in queue
            m_iCount++;

            return;
        }

        //check which is easier end to shift, the exit segment or the enter segment 
        if (iIndex > m_iCount / 2)
        {
            //calculate the new queue entrace point
            m_iQueueEnter = (m_iQueueEnter + 1) % m_tValues.Length;

            //calculate the index in the array to insert the value
            int iInsertIndex = UnsafeRemapIndex(iIndex);

            //shift wrapped around values forwards
            if (m_iQueueEnter < iInsertIndex)
            {
                if (m_iQueueEnter > 0)
                {
                    Array.Copy(m_tValues, 0, m_tValues, 1, m_iQueueEnter);
                }

                //shift last value around 
                m_tValues[0] = m_tValues[m_tValues.Length - 1];
            }

            //shift values from insertion point to end of array forwards
            int iItemsToShift = Math.Min((m_iCount - iIndex), (m_tValues.Length - 1) - iInsertIndex);

            if (iItemsToShift > 0)
            {
                Array.Copy(m_tValues, iInsertIndex, m_tValues, iInsertIndex + 1, iItemsToShift);
            }

            //insert item
            m_tValues[iInsertIndex] = tVal;

            //increment the number of items in queue
            m_iCount++;
        }
        else
        {
            //calculate the new queue entrace point
            m_iQueueExit = (m_iQueueExit + m_tValues.Length - 1) % m_tValues.Length;

            //calculate the index in the array to insert the value
            int iInsertIndex = UnsafeRemapIndex(iIndex);

            //shift wrapped around values forwards
            if (m_iQueueExit > iInsertIndex)
            {
                int iWrappedAroundItems = (m_tValues.Length - 1) - m_iQueueExit;

                if (iWrappedAroundItems > 0)
                {
                    Array.Copy(m_tValues, m_iQueueExit + 1, m_tValues, m_iQueueExit, iWrappedAroundItems);
                }

                //shift last value around 
                m_tValues[m_tValues.Length - 1] = m_tValues[0];

            }

            //shift values from insertion point to start of queue of array backward
            int iItemsToShift = Math.Min(iIndex, iInsertIndex);

            if (iItemsToShift > 0)
            {
                int iCopyToIndex = iInsertIndex - iItemsToShift;

                Array.Copy(m_tValues, iCopyToIndex + 1, m_tValues, iCopyToIndex, iItemsToShift);
            }

            //insert item
            m_tValues[iInsertIndex] = tVal;

            //increment the number of items in queue
            m_iCount++;
        }
    }

    //expands or contracts the underlying data structure 
    //if new size is smaller than number of items in list then
    //the more recently added items are dropped 
    private void ChangeCapacity(int iNewCapacity)
    {
        //create new arrays
        T[] keyNewKeyArray = new T[iNewCapacity];

        //check for empty resize
        if (m_iCount == 0)
        {
            m_tValues = keyNewKeyArray;

            m_iCount = 0;
            m_iQueueExit = 0;
            m_iQueueEnter = 0;

            return;
        }

        int iItemsToCopy = Math.Min(iNewCapacity, m_iCount);

        //calc segments to copy
        int iPartALength = Math.Min(m_iQueueExit + iItemsToCopy, m_tValues.Length) - m_iQueueExit;
        int iPartBLength = iItemsToCopy - iPartALength;

        //copy accross existing values 
        Array.Copy(m_tValues, m_iQueueExit, keyNewKeyArray, 0, iPartALength);

        //copy accross wrap around segment
        if (iPartBLength > 0)
        {
            Array.Copy(m_tValues, 0, keyNewKeyArray, iPartALength, iPartBLength);
        }

        m_iCount = iItemsToCopy;
        m_iQueueExit = 0;
        m_iQueueEnter = iItemsToCopy - 1;
    }
}

#endregion
