using UnityEngine;

[ExecuteInEditMode]
public class SortedRandomAccessQueueUnitTest : MonoBehaviour
{
    public bool m_bRunTest = true;


    // Update is called once per frame
    void Update()
    {
        if (m_bRunTest == false)
        {
            return;
        }

        m_bRunTest = false;

        Debug.Log($"Compare Result {5.CompareTo(6)}");

        Debug.Log("Starting queue test");

        SortedRandomAccessQueue<int, int> squSortedQueue1 = new SortedRandomAccessQueue<int, int>();

        Debug.Log("Adding random items");

        for (int i = 0; i < 10; i++)
        {
            squSortedQueue1.TryInsertEnqueue(i, i, out int iCollisionIndex);
        }

        Debug.Log("Print out queue contence");

        for (int i = 0; i < squSortedQueue1.Count; i++)
        {
            Debug.Log($" Key:{squSortedQueue1.GetKeyAtIndex(i)} Value:{squSortedQueue1.GetValueAtIndex(i)}");
        }

        Debug.Log("Dequeueing 5 items");

        for (int i = 0; i < 5; i++)
        {
            squSortedQueue1.Dequeue(out int iKey, out int iValue);

            Debug.Log($"Dequed value Key:{iKey} Value:{iValue}");
        }

        Debug.Log("Remaining 5 items");

        for (int i = 0; i < squSortedQueue1.Count; i++)
        {
            Debug.Log($" Key:{squSortedQueue1.GetKeyAtIndex(i)} Value:{squSortedQueue1.GetValueAtIndex(i)}");
        }

        Debug.Log("Finding item 7");

        bool bWas7FOund = squSortedQueue1.TryGetValueOf(7, out int i7Value);

        Debug.Log($"7 key search result:{bWas7FOund} Value: {i7Value}");

        Debug.Log("Finding item 666");

        bool bWas666Found = squSortedQueue1.TryGetValueOf(666, out int i666Value);

        Debug.Log($"666 Value Search Result: {bWas666Found} Value:{i666Value}");

        Debug.Log("Clearing Array");

        squSortedQueue1.Clear();

        Debug.Log("Cleared Array Contence");

        for (int i = 0; i < squSortedQueue1.Count; i++)
        {
            Debug.Log($" Key:{squSortedQueue1.GetKeyAtIndex(i)} Value:{squSortedQueue1.GetValueAtIndex(i)}");
        }

        Debug.Log("Adding More Items");

        for (int i = 0; i < 10; i++)
        {
            squSortedQueue1.TryInsertEnqueue(i * 2, i * 2, out int iCollisionIndex);
        }

        Debug.Log("New Queue Contence");

        for (int i = 0; i < squSortedQueue1.Count; i++)
        {
            Debug.Log($" Key:{squSortedQueue1.GetKeyAtIndex(i)} Value:{squSortedQueue1.GetValueAtIndex(i)}");
        }

        Debug.Log("Inserting Item Into Middle start Of Queue");

        squSortedQueue1.TryInsertEnqueue(5, 5, out int iMidStartInsertCollisionIndex);

        Debug.Log("Queue with inserted Value");

        for (int i = 0; i < squSortedQueue1.Count; i++)
        {
            Debug.Log($" Key:{squSortedQueue1.GetKeyAtIndex(i)} Value:{squSortedQueue1.GetValueAtIndex(i)}");
        }

        Debug.Log("Inserting Item Into Middle end Of Queue");

        squSortedQueue1.TryInsertEnqueue(17, 17, out int iMidEndInsertCollisionIndex);

        Debug.Log("Queue with inserted Value");

        for (int i = 0; i < squSortedQueue1.Count; i++)
        {
            Debug.Log($" Key:{squSortedQueue1.GetKeyAtIndex(i)} Value:{squSortedQueue1.GetValueAtIndex(i)}");
        }

        Debug.Log("Inserting Item at start Of Queue");

        squSortedQueue1.TryInsertEnqueue(-1, -1, out int iStartInsertCollisionIndex);

        Debug.Log("Queue with item inserted at start");

        for (int i = 0; i < squSortedQueue1.Count; i++)
        {
            Debug.Log($" Key:{squSortedQueue1.GetKeyAtIndex(i)} Value:{squSortedQueue1.GetValueAtIndex(i)}");
        }

        Debug.Log("Inserting Duplicate");

        bool bInsertDuplicateResult = squSortedQueue1.TryInsertEnqueue(5, 5, out int iDuplicateCollisionIndex);

        Debug.Log($"duplicate insert succeded: {bInsertDuplicateResult} Collision Index:{iDuplicateCollisionIndex}");

        Debug.Log("Queue post duplicate insert");

        for (int i = 0; i < squSortedQueue1.Count; i++)
        {
            Debug.Log($" Key:{squSortedQueue1.GetKeyAtIndex(i)} Value:{squSortedQueue1.GetValueAtIndex(i)}");
        }



        Debug.Log("Remove item from entrance of queue");

        squSortedQueue1.Clear();

        for (int i = 0; i < squSortedQueue1.Capacity; i++)
        {
            squSortedQueue1.TryInsertEnqueue(i , i , out int iMatchingIndex);
        }

        for (int i = 0; i < squSortedQueue1.Count; i++)
        {
            Debug.Log($" Key:{squSortedQueue1.GetKeyAtIndex(i)} Value:{squSortedQueue1.GetValueAtIndex(i)}");
        }

        Debug.Log($"Removing item at index{squSortedQueue1.Count -1}");

        squSortedQueue1.Remove(squSortedQueue1.Count - 1);

        for (int i = 0; i < squSortedQueue1.Count; i++)
        {
            Debug.Log($" Key:{squSortedQueue1.GetKeyAtIndex(i)} Value:{squSortedQueue1.GetValueAtIndex(i)}");
        }


        Debug.Log("Remove item from exot of queue");

        squSortedQueue1.Clear();

        for (int i = 0; i < squSortedQueue1.Capacity; i++)
        {
            squSortedQueue1.TryInsertEnqueue(i, i, out int iMatchingIndex);
        }

        for (int i = 0; i < squSortedQueue1.Count; i++)
        {
            Debug.Log($" Key:{squSortedQueue1.GetKeyAtIndex(i)} Value:{squSortedQueue1.GetValueAtIndex(i)}");
        }

        Debug.Log($"Removing item at index{0}");

        squSortedQueue1.Remove(0);

        for (int i = 0; i < squSortedQueue1.Count; i++)
        {
            Debug.Log($" Key:{squSortedQueue1.GetKeyAtIndex(i)} Value:{squSortedQueue1.GetValueAtIndex(i)}");
        }


        Debug.Log("Remove item from entrance middle of queue");

        squSortedQueue1.Clear();

        for (int i = 0; i < squSortedQueue1.Capacity; i++)
        {
            squSortedQueue1.TryInsertEnqueue(i, i, out int iMatchingIndex);
        }

        for (int i = 0; i < squSortedQueue1.Count; i++)
        {
            Debug.Log($" Key:{squSortedQueue1.GetKeyAtIndex(i)} Value:{squSortedQueue1.GetValueAtIndex(i)}");
        }

        Debug.Log($"Removing item at index{squSortedQueue1.Count - 3}");

        squSortedQueue1.Remove(squSortedQueue1.Count - 3);

        for (int i = 0; i < squSortedQueue1.Count; i++)
        {
            Debug.Log($" Key:{squSortedQueue1.GetKeyAtIndex(i)} Value:{squSortedQueue1.GetValueAtIndex(i)}");
        }


        Debug.Log("Remove item from exit middle of queue");

        squSortedQueue1.Clear();

        for (int i = 0; i < squSortedQueue1.Capacity; i++)
        {
            squSortedQueue1.TryInsertEnqueue(i, i, out int iMatchingIndex);
        }

        for (int i = 0; i < squSortedQueue1.Count; i++)
        {
            Debug.Log($" Key:{squSortedQueue1.GetKeyAtIndex(i)} Value:{squSortedQueue1.GetValueAtIndex(i)}");
        }

        Debug.Log($"Removing item at index{2}");

        squSortedQueue1.Remove(2);

        for (int i = 0; i < squSortedQueue1.Count; i++)
        {
            Debug.Log($" Key:{squSortedQueue1.GetKeyAtIndex(i)} Value:{squSortedQueue1.GetValueAtIndex(i)}");
        }

        Debug.Log("Force capacity size growth");

        squSortedQueue1.Clear();

        for (int i = 0; i < squSortedQueue1.Capacity; i++)
        {
            squSortedQueue1.TryInsertEnqueue(i * 2, i * 2, out int iMatchingIndex);
        }

        Debug.Log($"Queue Count:{squSortedQueue1.Count} queue capacity:{squSortedQueue1.Capacity}");

        //insert item in middle to forse far end resize
        squSortedQueue1.TryInsertEnqueue(squSortedQueue1.PeakKeyEnqueue() - 1, squSortedQueue1.PeakKeyEnqueue() - 1, out int iCollision);

        Debug.Log($"Queue Count:{squSortedQueue1.Count} queue capacity:{squSortedQueue1.Capacity}");

        for (int i = 0; i < squSortedQueue1.Count; i++)
        {
            Debug.Log($" Key:{squSortedQueue1.GetKeyAtIndex(i)} Value:{squSortedQueue1.GetValueAtIndex(i)}");
        }

        CheckClearUpTo();

        CheckClearUpToInclnuding();

        CheckPurgeInsert();

        CheckBetweenDates();
    }

    public void FillQueueWithSeries(SortedRandomAccessQueue<int, int> m_srqSortedRandomAccessQueue, int iNumberOfItems, int iStartIndex)
    {
        for (int i = 0; i < iNumberOfItems; i++)
        {
            m_srqSortedRandomAccessQueue.TryInsertEnqueue(iStartIndex + i, iStartIndex + i, out int iIndexOfMatch);
        }

    }

    public void PrintOutQueueValues(SortedRandomAccessQueue<int, int> m_srqSortedRandomAccessQueue)
    {
        for (int i = 0; i < m_srqSortedRandomAccessQueue.Count; i++)
        {
            Debug.Log($" Key:{m_srqSortedRandomAccessQueue.GetKeyAtIndex(i)} Value:{m_srqSortedRandomAccessQueue.GetValueAtIndex(i)}");
        }
    }

    public bool CheckClearUpTo()
    {
        Debug.Log($"Starting Remove Up TO Check");

        SortedRandomAccessQueue<int, int> srqTestQueue = new SortedRandomAccessQueue<int, int>();

        //fill with items
        FillQueueWithSeries(srqTestQueue, 8, 1);

        Debug.Log($"Starting Values");

        //print queue
        PrintOutQueueValues(srqTestQueue);

        Debug.Log($"Clearing to 4");

        srqTestQueue.ClearTo(4);

        //print queue
        PrintOutQueueValues(srqTestQueue);

        Debug.Log($"Causing A value wrap around ");

        //fill with items
        FillQueueWithSeries(srqTestQueue, 4, 9);

        //print queue
        PrintOutQueueValues(srqTestQueue);


        Debug.Log($"Removing items upt ot 10 from accross the queue internal wrap around");

        //removing items past queue value wrap around 
        srqTestQueue.ClearTo(10);

        //print queue
        PrintOutQueueValues(srqTestQueue);

        //trying again with remove upto including 


        return true;
    }

    public bool CheckClearUpToInclnuding()
    {
        Debug.Log($"Starting clear Up To Including Check");

        SortedRandomAccessQueue<int, int> srqTestQueue = new SortedRandomAccessQueue<int, int>();

        //fill with items
        FillQueueWithSeries(srqTestQueue, 8, 1);

        Debug.Log($"Starting Values");

        //print queue
        PrintOutQueueValues(srqTestQueue);

        Debug.Log($"Clearing to 4");

        srqTestQueue.ClearToIncluding(4);

        //print queue
        PrintOutQueueValues(srqTestQueue);

        Debug.Log($"Causing A value wrap around ");

        //fill with items
        FillQueueWithSeries(srqTestQueue, 4, 9);

        //print queue
        PrintOutQueueValues(srqTestQueue);


        Debug.Log($"Removing items upt ot 10 from accross the queue internal wrap around");

        //removing items past queue value wrap around 
        srqTestQueue.ClearToIncluding(10);

        //print queue
        PrintOutQueueValues(srqTestQueue);

        //trying again with remove upto including 


        return true;
    }

    public bool CheckPurgeInsert()
    {
        Debug.Log($"Starting Purge entry Insert Check");

        SortedRandomAccessQueue<int, int> srqTestQueue = new SortedRandomAccessQueue<int, int>();

        //fill with items
        FillQueueWithSeries(srqTestQueue, 8, 1);

        Debug.Log($"Starting Values");

        //print queue
        PrintOutQueueValues(srqTestQueue);

        Debug.Log($"purge inserting at 4");

        srqTestQueue.EnterPurgeInsert(4, 4);

        //print queue
        PrintOutQueueValues(srqTestQueue);

        Debug.Log($"Causing A value wrap around ");

        //fill with items
        FillQueueWithSeries(srqTestQueue, 4, 9);

        //print queue
        PrintOutQueueValues(srqTestQueue);


        Debug.Log($"inserting item at 10 forcing the purge to wrap around");

        //removing items past queue value wrap around 
        srqTestQueue.EnterPurgeInsert(10,10);

        //print queue
        PrintOutQueueValues(srqTestQueue);

        //repeting the same purge insert
        Debug.Log($"Repeting the same purge insert");
               
        //removing items past queue value wrap around 
        srqTestQueue.EnterPurgeInsert(10, 10);

        //print queue
        PrintOutQueueValues(srqTestQueue);

        return true;
    }


    public bool CheckBetweenDates()
    {
        Debug.Log("starting Find Index Test");

        SortedRandomAccessQueue<int, int> squSortedQueue1 = new SortedRandomAccessQueue<int, int>();

        FillQueueWithSeries(squSortedQueue1, 10, 0);

        PrintOutQueueValues(squSortedQueue1);

        //get item at entrance and exit
        Debug.Log($"sort value at enqueue { squSortedQueue1.PeakKeyEnqueue()}");
        Debug.Log($"sort value at dequeue { squSortedQueue1.PeakKeyDequeue()}");

        //run simple test for items in bounds


        {
            bool bTestResult = squSortedQueue1.TryGetFirstIndexGreaterThan(5, out int iTestIndex);

            if (bTestResult == false || iTestIndex != 6)
            {
                Debug.LogError($"Incorrect result from find greater. returned result {bTestResult} and index {iTestIndex}");

                return false;
            }
        }

        {
            bool bTestResult = squSortedQueue1.TryGetFirstIndexLessThan(5, out int iTestIndex);

            if (bTestResult == false || iTestIndex != 4)
            {
                Debug.LogError($"Incorrect result from find less returned result {bTestResult} and index {iTestIndex}");

                return false;
            }
        }

        { 
            //run out of bounds test 
            bool bTestResult = squSortedQueue1.TryGetFirstIndexGreaterThan(9, out int iTestIndex);

            if (bTestResult == true || iTestIndex != int.MaxValue)
            {
                Debug.LogError($"Incorrect result from find greater. returned result {bTestResult} and index {iTestIndex}");

                return false;
            }
        }

        {
            bool bTestResult = squSortedQueue1.TryGetFirstIndexLessThan(0, out int iTestIndex);

            if (bTestResult == true || iTestIndex != int.MinValue)
            {
                Debug.LogError($"Incorrect result from find less returned result {bTestResult} and index {iTestIndex}");

                return false;
            }
        }

        Debug.Log("Find index test finished Successfully");

        return true;
    }
}
