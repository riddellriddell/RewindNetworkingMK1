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
    }
}
