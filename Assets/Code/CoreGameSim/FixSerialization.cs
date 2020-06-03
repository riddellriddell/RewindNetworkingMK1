using FixedPointy;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Utility
{
    public class FixSerialization
    {
        public static bool Serialize(ReadByteStream rbsByteStream, ref Fix Output)
        {
            int iRaw = 0;

            if (ByteStream.Serialize(rbsByteStream, ref iRaw) == false)
            {
                return false;
            }

            Output = new Fix(iRaw);

            return true;
        }

        public static bool Serialize(WriteByteStream wbsByteStream, ref Fix Input)
        {
            int iRaw = Input.Raw;

            if (ByteStream.Serialize(wbsByteStream, ref iRaw) == false)
            {
                return false;
            }

            return true;
        }


        public static bool Serialize(ReadByteStream rbsStream, ref List<Fix> Output)
        {
            Int32 iItems = 0;

            if (Output == null)
            {
                Output = new List<Fix>();
            }
            else
            {
                Output.Clear();
            }

            if (ByteStream.Serialize(rbsStream, ref iItems) == false)
            {
                return false;
            }

            for (int i = 0; i < iItems; i++)
            {
                Fix value = 0;
                if (Serialize(rbsStream, ref value) == false)
                {
                    return false;
                }

                Output[i] = value;
            }

            return true;
        }

        public static bool Serialize(WriteByteStream wbsStream, ref List<Fix> Input)
        {
            Int32 iItems = 0;

            if (Input == null)
            {
                if (ByteStream.Serialize(wbsStream, ref iItems) == false)
                {
                    return false;
                }

                return true;
            }

            iItems = Input.Count;

            if (ByteStream.Serialize(wbsStream, ref iItems) == false)
            {
                return false;
            }

            for (int i = 0; i < iItems; i++)
            {
                Fix value = Input[i];
                if (Serialize(wbsStream, ref value) == false)
                {
                    return false;
                }
            }

            return true;
        }

        //read and write number arrays
        public static bool Serialize(ReadByteStream rbsStream, ref Fix[] Output)
        {
            Int32 iItems = 0;

            if (ByteStream.Serialize(rbsStream, ref iItems) == false)
            {
                return false;
            }

            if (Output == null || Output.Length != iItems)
            {
                Output = new Fix[iItems];
            }

            for (int i = 0; i < iItems; i++)
            {
                Fix value = new Fix(0);
                if (Serialize(rbsStream, ref value) == false)
                {
                    return false;
                }

                Output[i] = value;
            }

            return true;
        }

        public static bool Serialize(WriteByteStream wbsStream, ref Fix[] Input)
        {
            Int32 iItems = 0;

            if (Input == null)
            {
                if (ByteStream.Serialize(wbsStream, ref iItems) == false)
                {
                    return false;
                }

                return true;
            }

            iItems = Input.Length;

            if (ByteStream.Serialize(wbsStream, ref iItems) == false)
            {
                return false;
            }

            for (int i = 0; i < iItems; i++)
            {
                Fix value = Input[i];
                if (Serialize(wbsStream, ref value) == false)
                {
                    return false;
                }
            }

            return true;
        }

        public static bool Serialize(ReadByteStream rbsStream, ref Fix[] Output, Int32 iItems)
        {
            if (Output == null || Output.Length != iItems)
            {
                Output = new Fix[iItems];
            }

            for (int i = 0; i < iItems; i++)
            {
                Fix value = new Fix();
                if (Serialize(rbsStream, ref value) == false)
                {
                    return false;
                }
                Output[i] = value;
            }

            return true;
        }
        //TODO: Oprimize with an array copy of some kind
        public static bool Serialize(WriteByteStream wbsStream, ref Fix[] Input, Int32 iItems)
        {
            for (int i = 0; i < iItems; i++)
            {
                Fix value = Input[i];
                if (Serialize(wbsStream, ref value) == false)
                {
                    return false;
                }
            }

            return true;
        }


        public static bool Serialize(ReadByteStream rbsStream, ref List<Fix> Output, Int32 iItems)
        {
            if (Output == null)
            {
                Output = new List<Fix>(iItems);
            }
            else
            {
                Output.Clear();
            }

            for (int i = 0; i < iItems; i++)
            {
                Fix value = new Fix();
                if (Serialize(rbsStream, ref value) == false)
                {
                    return false;
                }

                Output.Add(value);
            }

            return true;
        }

        public static bool Serialize(WriteByteStream wbsStream, ref List<Fix> Input, Int32 iItems)
        {
            for (int i = 0; i < iItems; i++)
            {
                Fix value = Input[i];
                if (Serialize(wbsStream, ref value) == false)
                {
                    return false;
                }
            }

            return true;
        }



        #region DataSize 
        public static int DataSize(Fix Input)
        {
            return sizeof(Int32);
        }
        
        public static int DataSize(Fix[] Input)
        {
            if(Input == null)
            {
                return sizeof(Int32);
            }

            return (sizeof(Int32) * Input.Length) + sizeof(Int32);
        }

        public static int DataSize(List<Fix> Input)
        {
            if (Input == null)
            {
                return sizeof(Int32);
            }

            return (sizeof(Int32) * Input.Count) + sizeof(Int32);
        }

        public static int DataSize(Fix[] Input, int iCount)
        {
            return sizeof(Int32) * iCount;
        }

        public static int DataSize(List<Fix> Input, int iCount)
        {
            return sizeof(Int32) * iCount;
        }

        #endregion
    }
}