using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Networking
{
    public partial class ByteStream
    {

        #region Serialize

        //read and write a byte
        public static void Serialize(ReadByteStream rbsStream ,ref Byte Output)
        {
            Output = rbsStream.m_bData[rbsStream.ReadWriteHead];

            rbsStream.ReadWriteHead += DataSize(Output);
        }

        public static void Serialize(WriteByteStream wbsStream,ref Byte Input)
        {
            wbsStream.m_bData[wbsStream.ReadWriteHead] = Input;

            wbsStream.ReadWriteHead += DataSize(Input);
        }

        // read and write int
        public static void Serialize(ReadByteStream rbsStream, ref Int32 Output)
        {
            Output = BitConverter.ToInt32(rbsStream.m_bData, rbsStream.ReadWriteHead);
            rbsStream.ReadWriteHead += DataSize(Output);
        }

        public static void Serialize(WriteByteStream wbsStream,ref Int32 Input)
        {
            Array.Copy(BitConverter.GetBytes(Input), 0, wbsStream.m_bData, wbsStream.ReadWriteHead, DataSize(Input));
            wbsStream.ReadWriteHead += DataSize(Input);
        }

        // read and write long
        public static void Serialize(ReadByteStream rbsStream, ref Int64 Output)
        {
            Output = BitConverter.ToInt64(rbsStream.m_bData, rbsStream.ReadWriteHead);
            rbsStream.ReadWriteHead += DataSize(Output);
        }

        public static void Serialize(WriteByteStream wbsStream,ref Int64 Input)
        {
            Array.Copy(BitConverter.GetBytes(Input), 0, wbsStream.m_bData, wbsStream.ReadWriteHead, DataSize(Input));
            wbsStream.ReadWriteHead += DataSize(Input);
        }

        //read and write utc time
        public static void Serialize(ReadByteStream rbsStream, ref DateTime Output)
        {
            Int64 lTick = BitConverter.ToInt64(rbsStream.m_bData, rbsStream.ReadWriteHead);
            Output = new DateTime(lTick, DateTimeKind.Utc);
            rbsStream.ReadWriteHead += DataSize(Output);
        }

        public static void Serialize(WriteByteStream wbsStream, ref DateTime Input)
        {
            Int64 lTick = Input.ToUniversalTime().Ticks;
            Array.Copy(BitConverter.GetBytes(lTick), 0, wbsStream.m_bData, wbsStream.ReadWriteHead, DataSize(Input));
            wbsStream.ReadWriteHead += DataSize(Input);
        }
        
        //read and write number arrays
        public static void Serialize(ReadByteStream rbsStream, ref Byte[] Output)
        {
            Int32 iItems = 0;

            Serialize(rbsStream, ref iItems);

            if (Output == null || Output.Length != iItems)
            {
                Output = new  Byte[iItems];
            }
            else 
            {
                Output = new Byte[0];
            }

            for (int i = 0; i < iItems; i++)
            {
                Byte value = 0;
                Serialize(rbsStream, ref value);
                Output[i] = value;
            }
        }

        public static void Serialize(WriteByteStream wbsStream, ref Byte[] Input)
        {
            Int32 iItems = 0;

            if (Input == null)
            {
                Serialize(wbsStream, ref iItems);
            }

            iItems = Input.Length;

            Serialize(wbsStream, ref iItems);

            for (int i = 0; i < iItems; i++)
            {
                Byte value = Input[i];
                Serialize(wbsStream, ref value);
            }
        }


        public static void Serialize(ReadByteStream rbsStream, ref List<Byte> Output)
        {
            Int32 iItems = 0;
            
            Serialize(rbsStream, ref iItems);

            if (Output == null)
            {
                Output = new List<Byte>(iItems);
            }
            else
            {
                Output.Clear();
            }

            for (int i = 0; i < iItems; i++)
            {
                Byte value = 0;
                Serialize(rbsStream, ref value);
                Output.Add(value);
            }
        }

        public static void Serialize(WriteByteStream wbsStream, ref List<Byte> Input)
        {
            Int32 iItems = 0;

            if (Input == null)
            {
                Serialize(wbsStream, ref iItems);
            }

            iItems = Input.Count;

            Serialize(wbsStream, ref iItems);

            for (int i = 0; i < iItems; i++)
            {
                Byte value = Input[i];
                Serialize(wbsStream, ref value);
            }
        }


        public static void Serialize(ReadByteStream rbsStream, ref Int32[] Output)
        {
            Int32 iItems = 0;

            Serialize(rbsStream, ref iItems);

            if (Output == null || Output.Length != iItems)
            {
                Output = new Int32[iItems];
            }
            else
            {
                Output = new Int32[0];
            }

            for (int i = 0; i < iItems; i++)
            {
                Byte value = 0;
                Serialize(rbsStream, ref value);
                Output[i] = value;
            }
        }

        public static void Serialize(WriteByteStream wbsStream, ref Int32[] Input)
        {
            Int32 iItems = 0;

            if (Input == null)
            {
                Serialize(wbsStream, ref iItems);
            }

            iItems = Input.Length;

            Serialize(wbsStream, ref iItems);

            for (int i = 0; i < iItems; i++)
            {
                Int32 value = Input[i];
                Serialize(wbsStream, ref value);
            }
        }


        public static void Serialize(ReadByteStream rbsStream, ref List<Int32> Output)
        {
            Int32 iItems = 0;

            if (Output == null)
            {
                Output = new List<Int32>();
            }
            else
            {
                Output.Clear();
            }
            
            Serialize(rbsStream,ref iItems);
            
            for(int i = 0; i < iItems; i++)
            {
                Int32 value = 0;
                Serialize(rbsStream,ref value);
                Output[i] = value;
            }
        }

        public static void Serialize(WriteByteStream wbsStream, ref List<Int32> Input)
        {
            Int32 iItems = 0;

            if (Input == null)
            {
                Serialize(wbsStream,ref iItems);
            }

            iItems = Input.Count;

            Serialize(wbsStream, ref iItems);

            for (int i = 0; i < iItems; i++)
            {
                Int32 value = Input[i];
                Serialize(wbsStream, ref value);
            }
        }


        public static void Serialize(ReadByteStream rbsStream, ref Int64[] Output)
        {
            Int32 iItems = 0;

            Serialize(rbsStream, ref iItems);

            if (Output == null || Output.Length != iItems)
            {
                Output = new Int64[iItems];
            }
            else
            {
                Output = new Int64[0];
            }

            for (int i = 0; i < iItems; i++)
            {
                Byte value = 0;
                Serialize(rbsStream, ref value);
                Output[i] = value;
            }
        }

        public static void Serialize(WriteByteStream wbsStream, ref Int64[] Input)
        {
            Int32 iItems = 0;

            if (Input == null)
            {
                Serialize(wbsStream, ref iItems);
            }

            iItems = Input.Length;

            Serialize(wbsStream, ref iItems);

            for (int i = 0; i < iItems; i++)
            {
                Int64 value = Input[i];
                Serialize(wbsStream, ref value);
            }
        }


        public static void Serialize(ReadByteStream rbsStream, ref List<Int64> Output)
        {
            Int32 iItems = 0;

            if (Output == null)
            {
                Output = new List<Int64>();
            }
            else
            {
                Output.Clear();
            }

            Serialize(rbsStream, ref iItems);

            for (int i = 0; i < iItems; i++)
            {
                Int64 value = 0;
                Serialize(rbsStream, ref value);
                Output[i] = value;
            }
        }

        public static void Serialize(WriteByteStream wbsStream, ref List<Int64> Input)
        {
            Int32 iItems = 0;

            if (Input == null)
            {
                Serialize(wbsStream, ref iItems);
            }

            iItems = Input.Count;

            Serialize(wbsStream, ref iItems);

            for (int i = 0; i < iItems; i++)
            {
                Int64 value = Input[i];
                Serialize(wbsStream, ref value);
            }
        }

        //read and write string
        public static void Serialize(ReadByteStream rbsStream, ref string Output)
        {
            List<Byte> bStringbytes = new List<byte>();

            //get string bytes
            Serialize(rbsStream, ref bStringbytes);

            Output = Encoding.ASCII.GetString(bStringbytes.ToArray());
        }

        public static void Serialize(WriteByteStream wbsStream, ref string Input)
        {
            List<Byte> bStringbytes = new List<byte>(Encoding.ASCII.GetBytes(Input));

            //serialize string bytes
            Serialize(wbsStream, ref bStringbytes);
        }

        #endregion

        #region DataSize 
        public static int DataSize(Byte input)
        {
            return sizeof(Byte);
        }

        public static int DataSize(Int32 input)
        {
            return sizeof(Int32);
        }

        public static int DataSize(Int64 input)
        {
            return sizeof(Int64);
        }

        public static int DataSize(DateTime input)
        {
            return sizeof(Int64);
        }

        public static int DataSize(Byte[] Input)
        {
            Int32 iItems = 0;

            if (Input == null)
            {
                return DataSize(iItems);
            }

            iItems = Input.Length;

            return DataSize(iItems) + (sizeof(Byte) * iItems);
        }

        public static int DataSize(Int32[] Input)
        {
            Int32 iItems = 0;

            if (Input == null)
            {
                return DataSize(iItems);
            }

            iItems = Input.Length;

            return DataSize(iItems) + (sizeof(Int32) * iItems);
        }

        public static int DataSize(Int64[] Input)
        {
            Int32 iItems = 0;

            if (Input == null)
            {
                return DataSize(iItems);
            }

            iItems = Input.Length;

            return DataSize(iItems) + (sizeof(Int64) * iItems);
        }
        
        public static int DataSize(List<Byte> Input)
        {
            Int32 iItems = 0;

            if (Input == null)
            {
                return DataSize(iItems);
            }

            iItems = Input.Count;

            return DataSize(iItems) + (sizeof(Byte) * iItems);
        }

        public static int DataSize(List<Int32> Input)
        {
            Int32 iItems = 0;

            if (Input == null)
            {
                return DataSize(iItems);
            }

            iItems = Input.Count;

            return DataSize(iItems) + (sizeof(Int32) * iItems);
        }

        public static int DataSize(List<Int64> Input)
        {
            Int32 iItems = 0;

            if (Input == null)
            {
                return DataSize(iItems);
            }

            iItems = Input.Count;

            return DataSize(iItems) + (sizeof(Int64) * iItems);
        }

        public static int DataSize(string Input)
        {
            Int32 iItems = 0;

            if (Input == null)
            {
                return DataSize(iItems);
            }

            iItems = ASCIIEncoding.ASCII.GetByteCount(Input);

            return DataSize(iItems) + (sizeof(Byte) * iItems);
        }
        #endregion
    }
}
