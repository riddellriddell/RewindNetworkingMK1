using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Networking
{
    /// <summary>
    /// this struct is used to sort messages into a cronological order
    /// </summary>
    public struct SortingValue : ICloneable, IComparable
    {
        public const int c_BitsPerSegment = 64;
        public const int c_TotalBytes = 16;

        public static SortingValue MinValue
        {
            get
            {
                return new SortingValue(ulong.MinValue, ulong.MinValue);
            }
        }

        public static SortingValue MaxValue
        {
            get
            {
                return new SortingValue(ulong.MaxValue, ulong.MaxValue);
            }
        }

        public static SortingValue Min(SortingValue msvValueA, SortingValue msvValueB)
        {
            if(msvValueA.CompareTo(msvValueB) > 0)
            {
                return msvValueB;
            }

            return msvValueA;
        }

        public static SortingValue Max(SortingValue msvValueA, SortingValue msvValueB)
        {
            if (msvValueA.CompareTo(msvValueB) < 0)
            {
                return msvValueB;
            }

            return msvValueA;
        }
        
        public ulong m_lSortValueA;
        public ulong m_lSortValueB;

        public SortingValue(Byte[] bValue)
        {
            m_lSortValueA = BitConverter.ToUInt64(bValue, 0);
            m_lSortValueB = BitConverter.ToUInt64(bValue, sizeof(UInt64));
        }

        public SortingValue(SortingValue msvSortValue)
        {
            m_lSortValueA = msvSortValue.m_lSortValueA;
            m_lSortValueB = msvSortValue.m_lSortValueB;
        }

        public SortingValue(ulong lSortValueA, ulong lSortValueB)
        {
            m_lSortValueA = lSortValueA;
            m_lSortValueB = lSortValueB;
        }

        //increments the sorting value by 1;
        public void NextSortValue()
        {
            //check for lower segment overflow
            if(m_lSortValueB == ulong.MaxValue)
            {
                m_lSortValueA++;
            }
            else
            {
                m_lSortValueB++;
            }
        }

        public object Clone()
        {
            return new SortingValue(this);
        }

        public int CompareTo(object obj)
        {
            SortingValue msvSortValue; 

            if (obj is SortingValue )
            {
                msvSortValue = (SortingValue)obj;
            }
            else if(obj is IPeerMessageNode)
            {
                msvSortValue = (obj as IPeerMessageNode).SortingValue;
            }
            else
            {
                throw new NotImplementedException("Code to GlobalMessageSortingValue object to target object not implemented");
            }

            if(msvSortValue.m_lSortValueA > m_lSortValueA)
            {
                return -1;
            }
            else if(msvSortValue.m_lSortValueA < m_lSortValueA)
            {
                return 1;
            }
            else if(msvSortValue.m_lSortValueB > m_lSortValueB)
            {
                return -1;
            }
            else if(msvSortValue.m_lSortValueB < m_lSortValueB)
            {
                return 1;
            }
            else
            {
                return 0;
            }
        }

    }

    public partial class ByteStream
    {
        //read and write a byte
        public static void Serialize(ReadByteStream rbsStream, ref SortingValue Output)
        {
            Serialize(rbsStream,ref Output.m_lSortValueA);
            Serialize(rbsStream, ref Output.m_lSortValueB);
        }

        public static void Serialize(WriteByteStream wbsByteStream, ref SortingValue Input)
        {
            Serialize(wbsByteStream, ref Input.m_lSortValueA);
            Serialize(wbsByteStream, ref Input.m_lSortValueB);
        }

        public static int DataSize(SortingValue Input)
        {
            return DataSize(Input.m_lSortValueA) + DataSize(Input.m_lSortValueB);
        }
    }
}
