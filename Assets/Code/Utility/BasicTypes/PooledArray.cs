using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;

namespace Code.Utility.BasicTypes
{
    //This is a simple tool to reduce the amount of memory allocations by pooling arrays 
    //and avoiding creating and deleting arrays if needed
    public sealed class PooledArray<T> : IDisposable,  IEnumerable<T>
    {
        private T[] m_tArray;
        public int ICount { get; private set; }
        
        public int Capacity => m_tArray?.Length ?? 0;

        public PooledArray(int iCount, bool bClear = false)
        {
            m_tArray = ArrayPool<T>.Shared.Rent(iCount);
            ICount = iCount;

            if (bClear)
            {
                Array.Clear(m_tArray, 0, iCount);
            }
        }
        
        public PooledArray(int iCount, int iCapacity, bool clear = false)
        {
            m_tArray = ArrayPool<T>.Shared.Rent(iCapacity);
            ICount = iCount;

            if (clear)
            {
                Array.Clear(m_tArray, 0, iCount);
            }
        }

        public T this[int index]
        {
            get
            {
                if (index >= ICount)
                    throw new IndexOutOfRangeException();
                return m_tArray[index];
            }
            set
            {
                if (index >= ICount)
                    throw new IndexOutOfRangeException();
                m_tArray[index] = value;
            }
        }

        public Span<T> AsSpan() => m_tArray.AsSpan(0, ICount);
        public T[] RawArray => m_tArray; // Use carefully

        public void Dispose()
        {
            ArrayPool<T>.Shared.Return(m_tArray, clearArray: true);
            m_tArray = null;
            ICount = 0;
        }

        public void ShallowCopy(in PooledArray<T> parCopyTarget)
        {
            //check if this array has enough space
            if (parCopyTarget.ICount > Capacity)
            {
                ArrayPool<T>.Shared.Return(m_tArray, clearArray: false);
                m_tArray = ArrayPool<T>.Shared.Rent(parCopyTarget.ICount);
                ICount = parCopyTarget.ICount;
            }
            
            //clone values from one array to the other
            Array.Copy(parCopyTarget.RawArray, m_tArray, ICount);
        }

        public Enumerator GetEnumerator() => new Enumerator(m_tArray, ICount);

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public struct Enumerator : IEnumerator<T>
        {
            private readonly T[] _array;
            private readonly int _count;
            private int _index;

            public Enumerator(T[] array, int count)
            {
                _array = array;
                _count = count;
                _index = -1;
            }

            public T Current => _array[_index];

            object IEnumerator.Current => Current;

            public bool MoveNext() => ++_index < _count;

            public void Reset() => _index = -1;

            public void Dispose() { }
        }
    }
}