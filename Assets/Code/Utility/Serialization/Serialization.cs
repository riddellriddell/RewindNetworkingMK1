using System;
using System.Collections.Generic;
using System.Text;

namespace Utility
{
    public partial class ByteStream
    {

        #region Serialize

        //read and write a byte
        public static bool Serialize(ReadByteStream rbsStream, ref Byte Output)
        {
            if (rbsStream.HasBytesRemaining(ByteStream.DataSize(Output)) == false)
            {
                return false;
            }

            Output = rbsStream.m_bData[rbsStream.ReadWriteHead];

            rbsStream.ReadWriteHead += DataSize(Output);

            return true;
        }

        public static bool Serialize(WriteByteStream wbsStream, ref Byte Input)
        {
            if (wbsStream.HasBytesRemaining(ByteStream.DataSize(Input)) == false)
            {
                return false;
            }

            wbsStream.m_bData[wbsStream.ReadWriteHead] = Input;

            wbsStream.ReadWriteHead += DataSize(Input);

            return true;
        }

        // read and write int
        public static bool Serialize(ReadByteStream rbsStream, ref Int32 Output)
        {
            if (rbsStream.HasBytesRemaining(ByteStream.DataSize(Output)) == false)
            {
                return false;
            }

            Output = BitConverter.ToInt32(rbsStream.m_bData, rbsStream.ReadWriteHead);
            rbsStream.ReadWriteHead += DataSize(Output);

            return true;
        }

        public static bool Serialize(WriteByteStream wbsStream, ref Int32 Input)
        {
            if (wbsStream.HasBytesRemaining(ByteStream.DataSize(Input)) == false)
            {
                return false;
            }

            Array.Copy(BitConverter.GetBytes(Input), 0, wbsStream.m_bData, wbsStream.ReadWriteHead, DataSize(Input));
            wbsStream.ReadWriteHead += DataSize(Input);

            return true;
        }


        // read and write int
        public static bool Serialize(ReadByteStream rbsStream, ref UInt32 Output)
        {
            if (rbsStream.HasBytesRemaining(ByteStream.DataSize(Output)) == false)
            {
                return false;
            }

            Output = BitConverter.ToUInt32(rbsStream.m_bData, rbsStream.ReadWriteHead);
            rbsStream.ReadWriteHead += DataSize(Output);

            return true;
        }

        public static bool Serialize(WriteByteStream wbsStream, ref UInt32 Input)
        {
            if (wbsStream.HasBytesRemaining(ByteStream.DataSize(Input)) == false)
            {
                return false;
            }

            Array.Copy(BitConverter.GetBytes(Input), 0, wbsStream.m_bData, wbsStream.ReadWriteHead, DataSize(Input));
            wbsStream.ReadWriteHead += DataSize(Input);

            return true;
        }


        // read and write long
        public static bool Serialize(ReadByteStream rbsStream, ref Int64 Output)
        {
            if (rbsStream.HasBytesRemaining(ByteStream.DataSize(Output)) == false)
            {
                return false;
            }

            Output = BitConverter.ToInt64(rbsStream.m_bData, rbsStream.ReadWriteHead);
            rbsStream.ReadWriteHead += DataSize(Output);

            return true;
        }

        public static bool Serialize(WriteByteStream wbsStream, ref Int64 Input)
        {
            if (wbsStream.HasBytesRemaining(ByteStream.DataSize(Input)) == false)
            {
                return false;
            }

            Array.Copy(BitConverter.GetBytes(Input), 0, wbsStream.m_bData, wbsStream.ReadWriteHead, DataSize(Input));
            wbsStream.ReadWriteHead += DataSize(Input);

            return true;
        }

        public static bool Serialize(ReadByteStream rbsStream, ref UInt64 Output)
        {
            if (rbsStream.HasBytesRemaining(ByteStream.DataSize(Output)) == false)
            {
                return false;
            }

            Output = BitConverter.ToUInt64(rbsStream.m_bData, rbsStream.ReadWriteHead);
            rbsStream.ReadWriteHead += DataSize(Output);

            return true;
        }

        public static bool Serialize(WriteByteStream wbsStream, ref UInt64 Input)
        {
            if (wbsStream.HasBytesRemaining(ByteStream.DataSize(Input)) == false)
            {
                return false;
            }

            Array.Copy(BitConverter.GetBytes(Input), 0, wbsStream.m_bData, wbsStream.ReadWriteHead, DataSize(Input));
            wbsStream.ReadWriteHead += DataSize(Input);

            return true;
        }

        //read and write utc time
        public static bool Serialize(ReadByteStream rbsStream, ref DateTime Output)
        {
            if (rbsStream.HasBytesRemaining(ByteStream.DataSize(Output)) == false)
            {
                return false;
            }

            Int64 lTick = BitConverter.ToInt64(rbsStream.m_bData, rbsStream.ReadWriteHead);

            if(lTick < DateTime.MinValue.Ticks || lTick > DateTime.MaxValue.Ticks)
            {
                
            }

            Output = new DateTime(lTick, DateTimeKind.Utc);
            rbsStream.ReadWriteHead += DataSize(Output);

            return true;
        }

        public static bool Serialize(WriteByteStream wbsStream, ref DateTime Input)
        {
            if (wbsStream.HasBytesRemaining(ByteStream.DataSize(Input)) == false)
            {
                return false;
            }

            Int64 lTick = Input.ToUniversalTime().Ticks;
            Array.Copy(BitConverter.GetBytes(lTick), 0, wbsStream.m_bData, wbsStream.ReadWriteHead, DataSize(Input));
            wbsStream.ReadWriteHead += DataSize(Input);

            return true;
        }

        //read and write number arrays
        public static bool Serialize(ReadByteStream rbsStream, ref Byte[] Output)
        {
            Int32 iItems = 0;

            if(Serialize(rbsStream, ref iItems) == false)
            {
                return false;
            }

            if (Output == null || Output.Length != iItems)
            {
                Output = new Byte[iItems];
            }

            for (int i = 0; i < iItems; i++)
            {
                Byte value = 0;
                if( Serialize(rbsStream, ref value) == false)
                {
                    return false;
                }

                Output[i] = value;
            }

            return true;
        }

        public static bool Serialize(WriteByteStream wbsStream, ref Byte[] Input)
        {
            Int32 iItems = 0;

            if (Input == null)
            {
                if(Serialize(wbsStream, ref iItems) == false)
                {
                    return false;
                }

                return true;
            }

            iItems = Input.Length;

            if(Serialize(wbsStream, ref iItems) == false)
            {
                return false;
            }

            for (int i = 0; i < iItems; i++)
            {
                Byte value = Input[i];
                if(Serialize(wbsStream, ref value) == false)
                {
                    return false;
                }
            }

            return true;
        }


        public static bool Serialize(ReadByteStream rbsStream, ref List<Byte> Output)
        {
            Int32 iItems = 0;

            if (Serialize(rbsStream, ref iItems) == false)
            {
                return false;
            }

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
                if (Serialize(rbsStream, ref value) == false)
                {
                    return false;
                }
                Output.Add(value);
            }

            return true;
        }

        public static bool Serialize(WriteByteStream wbsStream, ref List<Byte> Input)
        {
            Int32 iItems = 0;

            if (Input == null)
            {
                if (Serialize(wbsStream, ref iItems) == false)
                {
                    return false;
                }

                return true;
            }

            iItems = Input.Count;

            if (Serialize(wbsStream, ref iItems) == false)
            {
                return false;
            }

            for (int i = 0; i < iItems; i++)
            {
                Byte value = Input[i];
                if (Serialize(wbsStream, ref value) == false)
                {
                    return false;
                }
            }

            return true;
        }


        public static bool Serialize(ReadByteStream rbsStream, ref Int32[] Output)
        {
            Int32 iItems = 0;

            if (Serialize(rbsStream, ref iItems) == false)
            {
                return false;
            }

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
                Int32 value = 0;
                if (Serialize(rbsStream, ref value) == false)
                {
                    return false;
                }
                Output[i] = value;
            }

            return true;
        }

        public static bool Serialize(WriteByteStream wbsStream, ref Int32[] Input)
        {
            Int32 iItems = 0;

            if (Input == null)
            {
                if (Serialize(wbsStream, ref iItems) == false)
                {
                    return false;
                }

                return true;
            }

            iItems = Input.Length;

            if (Serialize(wbsStream, ref iItems) == false)
            {
                return false;
            }

            for (int i = 0; i < iItems; i++)
            {
                Int32 value = Input[i];
                if (Serialize(wbsStream, ref value) == false)
                {
                    return false;
                }
            }

            return true;
        }


        public static bool Serialize(ReadByteStream rbsStream, ref List<Int32> Output)
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

            if (Serialize(rbsStream, ref iItems) == false)
            {
                return false;
            }

            for (int i = 0; i < iItems; i++)
            {
                Int32 value = 0;
                if (Serialize(rbsStream, ref value) == false)
                {
                    return false;
                }

                Output[i] = value;
            }

            return true;
        }

        public static bool Serialize(WriteByteStream wbsStream, ref List<Int32> Input)
        {
            Int32 iItems = 0;

            if (Input == null)
            {
                if (Serialize(wbsStream, ref iItems) == false)
                {
                    return false;
                }

                return true;
            }

            iItems = Input.Count;

            if (Serialize(wbsStream, ref iItems) == false)
            {
                return false;
            }

            for (int i = 0; i < iItems; i++)
            {
                Int32 value = Input[i];
                if (Serialize(wbsStream, ref value) == false)
                {
                    return false;
                }
            }

            return true;
        }


        public static bool Serialize(ReadByteStream rbsStream, ref Int64[] Output)
        {
            Int32 iItems = 0;

            if (Serialize(rbsStream, ref iItems) == false)
            {
                return false;
            }

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
                Int64 value = 0;
                if (Serialize(rbsStream, ref value) == false)
                {
                    return false;
                }

                Output[i] = value;
            }

            return true;
        }

        public static bool Serialize(WriteByteStream wbsStream, ref Int64[] Input)
        {
            Int32 iItems = 0;

            if (Input == null)
            {
                if (Serialize(wbsStream, ref iItems) == false)
                {
                    return false;
                }

                return true;
            }

            iItems = Input.Length;

            if (Serialize(wbsStream, ref iItems) == false)
            {
                return false;
            }

            for (int i = 0; i < iItems; i++)
            {
                Int64 value = Input[i];
                if (Serialize(wbsStream, ref value) == false)
                {
                    return false;
                }
            }

            return true;
        }


        public static bool Serialize(ReadByteStream rbsStream, ref List<Int64> Output)
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

            if (Serialize(rbsStream, ref iItems) == false)
            {
                return false;
            }

            for (int i = 0; i < iItems; i++)
            {
                Int64 value = 0;
                if (Serialize(rbsStream, ref value) == false)
                {
                    return false;
                }

                Output[i] = value;
            }

            return true;
        }

        public static bool Serialize(WriteByteStream wbsStream, ref List<Int64> Input)
        {
            Int32 iItems = 0;

            if (Input == null)
            {
                if (Serialize(wbsStream, ref iItems) == false)
                {
                    return false;
                }

                return true;
            }

            iItems = Input.Count;

            if (Serialize(wbsStream, ref iItems) == false)
            {
                return false;
            }

            for (int i = 0; i < iItems; i++)
            {
                Int64 value = Input[i];
                if (Serialize(wbsStream, ref value) == false)
                {
                    return false;
                }
            }

            return true;
        }

        //read and write fixed array sizes 

        public static bool Serialize(ReadByteStream rbsStream, ref Byte[] Output, Int32 iItems)
        {
            if (Output == null || Output.Length != iItems)
            {
                Output = new Byte[iItems];
            }

            for (int i = 0; i < iItems; i++)
            {
                Byte value = 0;
                if( Serialize(rbsStream, ref value) == false)
            {
                return false;
            }
                Output[i] = value;
            }

            return true;
        }
        //TODO: Oprimize with an array copy of some kind
        public static bool Serialize(WriteByteStream wbsStream, ref Byte[] Input, Int32 iItems)
        {
            for (int i = 0; i < iItems; i++)
            {
                Byte value = Input[i];
                if (Serialize(wbsStream, ref value) == false)
                {
                    return false;
                }
            }

            return true;
        }


        public static bool Serialize(ReadByteStream rbsStream, ref List<Byte> Output, Int32 iItems)
        {
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
                if (Serialize(rbsStream, ref value) == false)
                {
                    return false;
                }

                Output.Add(value);
            }

            return true;
        }

        public static bool Serialize(WriteByteStream wbsStream, ref List<Byte> Input, Int32 iItems)
        {
            for (int i = 0; i < iItems; i++)
            {
                Byte value = Input[i];
                if (Serialize(wbsStream, ref value) == false)
                {
                    return false;
                }
            }

            return true;
        }


        //read and write string
        public static bool Serialize(ReadByteStream rbsStream, ref string Output)
        {
            List<Byte> bStringbytes = new List<byte>();

            //get string bytes
            if (Serialize(rbsStream, ref bStringbytes) == false)
            {
                return false;
            }

            Output = Encoding.ASCII.GetString(bStringbytes.ToArray());

            return true;
        }

        public static bool Serialize(WriteByteStream wbsStream, ref string Input)
        {
            List<Byte> bStringbytes = new List<byte>(Encoding.ASCII.GetBytes(Input));

            //serialize string bytes
            if (Serialize(wbsStream, ref bStringbytes) == false)
            {
                return false;
            }
            return true;
        }

        #endregion

        #region DataSize 
        public static int DataSize(Byte Input)
        {
            return sizeof(Byte);
        }

        public static int DataSize(Int32 Input)
        {
            return sizeof(Int32);
        }

        public static int DataSize(UInt32 Input)
        {
            return sizeof(UInt32);
        }

        public static int DataSize(Int64 Input)
        {
            return sizeof(Int64);
        }

        public static int DataSize(UInt64 Input)
        {
            return sizeof(UInt64);
        }

        public static int DataSize(DateTime Input)
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

        public static int DataSize(Byte[] Input, int iCount)
        {
            return  (sizeof(Byte) * iCount);
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
