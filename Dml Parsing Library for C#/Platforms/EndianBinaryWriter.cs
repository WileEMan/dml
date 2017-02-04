using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace WileyBlack.Platforms
{
    public class EndianBinaryWriter : BinaryWriter, IDisposable
    {
        #region "Initialization / Control / Cleanup"

        public bool IsLittleEndian;

        public EndianBinaryWriter(Stream stream, bool LittleEndian) : base(stream)
        {
            this.IsLittleEndian = LittleEndian;
        }

        public void Dispose()
        {
            base.Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~EndianBinaryWriter() { base.Dispose(false); }

        #endregion

        #region "Generalized writers"

        /// <summary>
        /// WriteUI() writes a value with only a specified number of bytes
        /// of outputs, always the least-significant bytes.  This can be used
        /// to write a truncated form of the value.  Although the function
        /// accepts an unsigned integer, in this case there is no difference
        /// between signed and unsigned and a signed value can be accepted as
        /// well.
        /// </summary>
        /// <param name="val">Value (or partial value) to be written</param>
        /// <param name="bytes">Least-significant number of bytes of Value to write</param>
        public void WriteUI(UInt64 val, int bytes)
        {
            if (IsLittleEndian)
            {
                // Writing to a little-endian stream regardless of memory architecture
                // i.e. the memory value 0x0A0B0C0D
                // First byte written:  0x0D
                // Second byte written: 0x0C
                // Third byte written:  0x0B
                // Fourth byte written: 0x0A

                // i.e. if bytes = 4, nShift=24.  
                // Byte 1: (val >> 0)
                // Byte 2: (val >> 8)
                // Byte 3: (val >> 16)
                // Byte 4: (val >> 24)

                int nShift = 0;
                for (int ii = 0; ii < bytes; ii++)
                {
                    Write((byte)(val >> nShift));
                    nShift += 8;
                }
            }
            else
            {
                // Writing to a big-endian stream regardless of memory architecture
                // i.e. the memory value 0x0A0B0C0D
                // First byte written:  0x0A
                // Second byte written: 0x0B
                // Third byte written:  0x0C
                // Fourth byte written: 0x0D

                // i.e. if bytes = 4, nShift=24.  
                // Byte 1: (val >> 24)
                // Byte 2: (val >> 16)
                // Byte 3: (val >> 8)
                // Byte 4: (val >> 0)

                int nShift = (bytes * 8) - 8;
                for (int ii = 0; ii < bytes; ii++)
                {
                    Write((byte)(val >> nShift));
                    nShift -= 8;
                }
            }
        }

        /// <summary>
        /// WriteLargeUI() provides behavior similar to WriteUI(), but supports
        /// values of an arbitrary length (beyond 64-bits).  WriteLargeUI() swaps
        /// the byte-order of the value if the stream's byte-order does not match
        /// the current architecture byte-order.  Otherwise it writes the Value
        /// directly.        
        /// </summary>
        /// <param name="val">Value to be written</param>        
        public void WriteLargeUI(byte[] Value)
        {
            if (IsLittleEndian == BitConverter.IsLittleEndian)
            {
                // No swapping is needed...write the array out as-is.
                Write(Value);
            }
            else
            {
                // Swapping is necessary.  Write in reversed order.
                for (int ii = Value.Length - 1; ii >= 0; ii--) Write((byte)Value[ii]);
            }
        }

        #endregion

        #region "Compact-Integer writers (Endian-independent)"

        public static int SizeCompact32(UInt32 Value)
        {
            if (Value <= 0x7F) return 1;
            else if (Value <= 0x3FFF) return 2;
            else if (Value <= 0x1FFFFF) return 3;
            else if (Value <= 0x0FFFFFFF) return 4;
            else return 5;
        }

        public void WriteCompact32(UInt32 Value)
        {
            if (Value <= 0x7F)
                Write((byte)(0x80 | Value));
            else if (Value <= 0x3FFF)
            {
                Write((byte)(0x40 | (Value >> 8)));
                Write((byte)Value);
            }
            else if (Value <= 0x1FFFFF)
            {
                Write((byte)(0x20 | (Value >> 16)));
                Write((byte)(Value >> 8));
                Write((byte)Value);
            }
            else if (Value <= 0x0FFFFFFF)
            {
                Write((byte)(0x10 | (Value >> 24)));
                Write((byte)(Value >> 16));
                Write((byte)(Value >> 8));
                Write((byte)Value);
            }
            else
            {
                Write((byte)(0x08));
                Write((byte)(Value >> 24));
                Write((byte)(Value >> 16));
                Write((byte)(Value >> 8));
                Write((byte)Value);
            }
        }        

        public void WriteCompact32b(UInt32 Value, bool ExtraBit)
        {
            if (Value <= 0x3F)
            {
                if (ExtraBit) 
                    Write((byte)(0xC0 | Value));
                else
                    Write((byte)(0x80 | Value));
            }
            else if (Value <= 0x1FFF)
            {
                if (ExtraBit)
                    Write((byte)(0x60 | (Value >> 8)));
                else
                    Write((byte)(0x40 | (Value >> 8)));
                Write((byte)Value);
            }
            else if (Value <= 0x0FFFFF)
            {
                if (ExtraBit)
                    Write((byte)(0x30 | (Value >> 16)));
                else
                    Write((byte)(0x20 | (Value >> 16)));
                Write((byte)(Value >> 8));
                Write((byte)Value);
            }
            else if (Value <= 0x07FFFFFF)
            {
                if (ExtraBit)
                    Write((byte)(0x18 | (Value >> 24)));
                else
                    Write((byte)(0x10 | (Value >> 24)));
                Write((byte)(Value >> 16));
                Write((byte)(Value >> 8));
                Write((byte)Value);
            }
            else
            {
                if (ExtraBit)
                    Write((byte)(0x0C));
                else
                    Write((byte)(0x08));
                Write((byte)(Value >> 24));
                Write((byte)(Value >> 16));
                Write((byte)(Value >> 8));
                Write((byte)Value);
            }
        }

        public static int SizeCompact64(UInt64 Value)
        {
            if (Value <= 0x7F) return 1;
            else if (Value <= 0x3FFF) return 2;
            else if (Value <= 0x1FFFFF) return 3;
            else if (Value <= 0x0FFFFFFF) return 4;
            else if (Value <= 0x07FFFFFFFF) return 5;
            else if (Value <= 0x03FFFFFFFFFF) return 6;
            else if (Value <= 0x01FFFFFFFFFFFF) return 7;
            else if (Value <= 0x00FFFFFFFFFFFFFF) return 8;
            else return 9;
        }        

        public void WriteCompact64(UInt64 Value)
        {            
            if (Value <= 0x7F)
                Write((byte)(0x80 | Value));
            else if (Value <= 0x3FFF)
            {
                Write((byte)(0x40 | (Value >> 8)));
                Write((byte)Value);
            }
            else if (Value <= 0x1FFFFF)
            {
                Write((byte)(0x20 | (Value >> 16)));
                Write((byte)(Value >> 8));
                Write((byte)Value);
            }
            else if (Value <= 0x0FFFFFFF)
            {
                Write((byte)(0x10 | (Value >> 24)));
                Write((byte)(Value >> 16));
                Write((byte)(Value >> 8));
                Write((byte)Value);
            }
            else if (Value <= 0x07FFFFFFFF)
            {
                Write((byte)(0x08 | (Value >> 32)));
                Write((byte)(Value >> 24));
                Write((byte)(Value >> 16));
                Write((byte)(Value >> 8));
                Write((byte)Value);
            }
            else if (Value <= 0x03FFFFFFFFFF)
            {
                Write((byte)(0x04 | (Value >> 40)));
                Write((byte)(Value >> 32));
                Write((byte)(Value >> 24));
                Write((byte)(Value >> 16));
                Write((byte)(Value >> 8));
                Write((byte)Value);
            }
            else if (Value <= 0x01FFFFFFFFFFFF)
            {
                Write((byte)(0x02 | (Value >> 48)));
                Write((byte)(Value >> 40));
                Write((byte)(Value >> 32));
                Write((byte)(Value >> 24));
                Write((byte)(Value >> 16));
                Write((byte)(Value >> 8));
                Write((byte)Value);
            }
            else if (Value <= 0x00FFFFFFFFFFFFFF)
            {
                Write((byte)(0x01 | (Value >> 56)));
                Write((byte)(Value >> 48));
                Write((byte)(Value >> 40));
                Write((byte)(Value >> 32));
                Write((byte)(Value >> 24));
                Write((byte)(Value >> 16));
                Write((byte)(Value >> 8));
                Write((byte)Value);
            }
            else             
            {
                Write((byte)(0x00));
                Write((byte)(Value >> 56));
                Write((byte)(Value >> 48));
                Write((byte)(Value >> 40));
                Write((byte)(Value >> 32));
                Write((byte)(Value >> 24));
                Write((byte)(Value >> 16));
                Write((byte)(Value >> 8));
                Write((byte)Value);
            }
        }

        public static int SizeCompactS64(Int64 Value)
        {
            // See WriteCompactS64() for commentary.
            
            if (Value >= 0)
            {
                if (Value <= 0x3F) return 1;
                else if (Value <= 0x1FFF) return 2;
                else if (Value <= 0x0FFFFF) return 3;
                else if (Value <= 0x07FFFFFF) return 4;
                else if (Value <= 0x03FFFFFFFF) return 5;
                else if (Value <= 0x01FFFFFFFFFF) return 6;
                else if (Value <= 0x00FFFFFFFFFFFF) return 7;
                else if (Value <= 0x007FFFFFFFFFFFFF) return 8;
                else return 9;                
            }
            else // Value < 0..
            {
                if (Value >= -64) return 1;
                else if (Value >= -8192) return 2;
                else if (Value >= -1048576) return 3;
                else if (Value >= -134217728) return 4;
                else if (Value >= -17179869184) return 5;
                else if (Value >= -2199023255552) return 6;
                else if (Value >= -281474976710656) return 7;
                else if (Value >= -36028797018963968) return 8;
                else return 9;
            }
        }

        public void WriteCompactS64(Int64 Value)
        {
            // Template:
            // Positive values must be <= 01111111 (127) for 8-bits.
            //                            011111111 (255) for 9-bits.
            //                            (2^(n-1))-1 for n-bits.
            // Negative values must be >= -128 for 8-bits.
            //                            -256 for 9-bits.
            //                            -(2^(n-1)) for n-bits.

            if (Value >= 0)
            {
                if (Value <= 0x3F)                  // 7-bits, top-bit zero
                    Write((byte)(0x80 | Value));
                else if (Value <= 0x1FFF)           // 14-bits, top-bit zero
                {
                    Write((byte)(0x40 | (Value >> 8)));
                    Write((byte)Value);
                }
                else if (Value <= 0x0FFFFF)         // 21-bits
                {
                    Write((byte)(0x20 | (Value >> 16)));
                    Write((byte)(Value >> 8));
                    Write((byte)Value);
                }
                else if (Value <= 0x07FFFFFF)
                {
                    Write((byte)(0x10 | (Value >> 24)));
                    Write((byte)(Value >> 16));
                    Write((byte)(Value >> 8));
                    Write((byte)Value);
                }
                else if (Value <= 0x03FFFFFFFF)
                {
                    Write((byte)(0x08 | (Value >> 32)));
                    Write((byte)(Value >> 24));
                    Write((byte)(Value >> 16));
                    Write((byte)(Value >> 8));
                    Write((byte)Value);
                }
                else if (Value <= 0x01FFFFFFFFFF)
                {
                    Write((byte)(0x04 | (Value >> 40)));
                    Write((byte)(Value >> 32));
                    Write((byte)(Value >> 24));
                    Write((byte)(Value >> 16));
                    Write((byte)(Value >> 8));
                    Write((byte)Value);
                }
                else if (Value <= 0x00FFFFFFFFFFFF)
                {
                    Write((byte)(0x02 | (Value >> 48)));
                    Write((byte)(Value >> 40));
                    Write((byte)(Value >> 32));
                    Write((byte)(Value >> 24));
                    Write((byte)(Value >> 16));
                    Write((byte)(Value >> 8));
                    Write((byte)Value);
                }
                else if (Value <= 0x007FFFFFFFFFFFFF)
                {
                    Write((byte)(0x01));
                    Write((byte)(Value >> 48));
                    Write((byte)(Value >> 40));
                    Write((byte)(Value >> 32));
                    Write((byte)(Value >> 24));
                    Write((byte)(Value >> 16));
                    Write((byte)(Value >> 8));
                    Write((byte)Value);
                }
                else
                {
                    Write((byte)(0x00));
                    Write((byte)(Value >> 56));
                    Write((byte)(Value >> 48));
                    Write((byte)(Value >> 40));
                    Write((byte)(Value >> 32));
                    Write((byte)(Value >> 24));
                    Write((byte)(Value >> 16));
                    Write((byte)(Value >> 8));
                    Write((byte)Value);
                }
            }
            else // Value < 0..
            {
                // (Value >= -2^N) where N are the number of bits available for representation.  Referencing
                // the 2's complement format, the top-bit must be a one for this negative number and is
                // excluded from N.  For example, the first template is 1xxx xxxx, showing 7-bits, but N=6.
                if (Value >= -64)                   // 6-bits + top-bit one
                    Write((byte)(0x80 | (Value & 0x7F)));
                else if (Value >= -8192)            // 13-bits + top-bit one
                {
                    Write((byte)(0x40 | ((Value >> 8) & 0x3F)));
                    Write((byte)Value);
                }
                else if (Value >= -1048576)         // 20-bits + top-bit one
                {
                    Write((byte)(0x20 | ((Value >> 16) & 0x1F)));
                    Write((byte)(Value >> 8));
                    Write((byte)Value);
                }
                else if (Value >= -134217728)       // 27-bits + top-one
                {
                    Write((byte)(0x10 | ((Value >> 24) & 0x0F)));
                    Write((byte)(Value >> 16));
                    Write((byte)(Value >> 8));
                    Write((byte)Value);
                }
                else if (Value >= -17179869184)     // 34-bits + top-one
                {
                    Write((byte)(0x08 | ((Value >> 32) & 0x07)));
                    Write((byte)(Value >> 24));
                    Write((byte)(Value >> 16));
                    Write((byte)(Value >> 8));
                    Write((byte)Value);
                }
                else if (Value >= -2199023255552)   // 41-bits + top-one
                {
                    Write((byte)(0x04 | ((Value >> 40) & 0x03)));
                    Write((byte)(Value >> 32));
                    Write((byte)(Value >> 24));
                    Write((byte)(Value >> 16));
                    Write((byte)(Value >> 8));
                    Write((byte)Value);
                }
                else if (Value >= 281474976710656)  // 48-bits + top-one
                {
                    Write((byte)(0x02 | ((Value >> 48) & 0x01)));
                    Write((byte)(Value >> 40));
                    Write((byte)(Value >> 32));
                    Write((byte)(Value >> 24));
                    Write((byte)(Value >> 16));
                    Write((byte)(Value >> 8));
                    Write((byte)Value);
                }
                else if (Value >= 36028797018963968)    // 55-bits + top-one
                {
                    Write((byte)(0x01));
                    Write((byte)(Value >> 48));
                    Write((byte)(Value >> 40));
                    Write((byte)(Value >> 32));
                    Write((byte)(Value >> 24));
                    Write((byte)(Value >> 16));
                    Write((byte)(Value >> 8));
                    Write((byte)Value);
                }
                else    // 64-bits
                {
                    Write((byte)(0x00));
                    Write((byte)(Value >> 56));
                    Write((byte)(Value >> 48));
                    Write((byte)(Value >> 40));
                    Write((byte)(Value >> 32));
                    Write((byte)(Value >> 24));
                    Write((byte)(Value >> 16));
                    Write((byte)(Value >> 8));
                    Write((byte)Value);
                }
            }            
        }

        #endregion

        #region "Primitive writers"

        public override void Write(short Value) { Write((ushort)Value); }
        public override void Write(ushort Value)
        {
            if (IsLittleEndian)
            {
                base.Write((byte)(Value)); base.Write((byte)(Value >> 8));
            }
            else
            {
                base.Write((byte)(Value >> 8)); base.Write((byte)(Value));
            }
        }

        public override void Write(float Value) {
            byte[] RawValue = BitConverter.GetBytes(Value);
            if (IsLittleEndian == BitConverter.IsLittleEndian)
            {
                base.Write(RawValue[0]); base.Write(RawValue[1]); base.Write(RawValue[2]); base.Write(RawValue[3]);
            }
            else
            {
                base.Write(RawValue[3]); base.Write(RawValue[2]); base.Write(RawValue[1]); base.Write(RawValue[0]);
            }
        }

        public override void Write(int Value) { Write((uint)Value); }
        public override void Write(uint Value)
        {
            if (IsLittleEndian)
            {
                base.Write((byte)(Value)); base.Write((byte)(Value >> 8));
                base.Write((byte)(Value >> 16)); base.Write((byte)(Value >> 24));
            }
            else
            {
                base.Write((byte)(Value >> 24)); base.Write((byte)(Value >> 16));
                base.Write((byte)(Value >> 8)); base.Write((byte)(Value));
            }
        }

        public override void Write(double Value) { Write((ulong)BitConverter.DoubleToInt64Bits(Value)); }
        public override void Write(long Value) { Write((ulong)Value); }
        public override void Write(ulong Value)
        {
            if (IsLittleEndian)
            {
                base.Write((byte)(Value)); base.Write((byte)(Value >> 8));
                base.Write((byte)(Value >> 16)); base.Write((byte)(Value >> 24));
                base.Write((byte)(Value >> 32)); base.Write((byte)(Value >> 40));
                base.Write((byte)(Value >> 48)); base.Write((byte)(Value >> 56));
            }
            else
            {
                base.Write((byte)(Value >> 56)); base.Write((byte)(Value >> 48));
                base.Write((byte)(Value >> 40)); base.Write((byte)(Value >> 32));
                base.Write((byte)(Value >> 24)); base.Write((byte)(Value >> 16));
                base.Write((byte)(Value >> 8)); base.Write((byte)(Value));
            }
        }
        
        public void WriteUI24(uint Value)
        {
            if (IsLittleEndian)
            {
                base.Write((byte)(Value)); base.Write((byte)(Value >> 8));
                base.Write((byte)(Value >> 16)); 
            }
            else
            {
                base.Write((byte)(Value >> 16));
                base.Write((byte)(Value >> 8)); base.Write((byte)(Value));
            }
        }

        public override void Write(byte[] buffer)
        {
            base.Write(buffer, 0, buffer.Length);
        }

        // Optimization: This could afford to be much faster...
        public void Write(sbyte[] buffer)
        {
            for (int ii = 0; ii < buffer.Length; ii++) base.Write(buffer[ii]);            
        }

        /// <summary>
        /// WriteRaw() will write a block without any endian conversion applied.
        /// It is a "pass-through" write.
        /// </summary>
        /// <param name="buffer">Buffer to be written</param>
        /// <param name="index">First index to be written</param>
        /// <param name="count">Number of bytes to write</param>
        public void WriteRaw(byte[] buffer, int index, int count)
        {
            base.Write(buffer, index, count);
        }

        #endregion

        #region "Endian Array writers"

        /*** Optimization: The array writing routines in particular would
         *   benefit from optimized, unmanaged, native code.
         */

        #region "Unsigned arrays"

        public void Write(ushort[] Data)
        {
            if (IsLittleEndian)
            {
                for (int ii = 0; ii < Data.Length; ii++)
                {
                    ushort Value = Data[ii];
                    base.Write((byte)(Value)); base.Write((byte)(Value >> 8));
                }
            }
            else
            {
                for (int ii = 0; ii < Data.Length; ii++)
                {
                    ushort Value = Data[ii];
                    base.Write((byte)(Value >> 8)); base.Write((byte)(Value));
                }
            }
        }

        public void Write(uint[] Data)
        {
            if (IsLittleEndian)
            {
                for (int ii = 0; ii < Data.Length; ii++)
                {
                    uint Value = Data[ii];
                    base.Write((byte)(Value)); base.Write((byte)(Value >> 8));
                    base.Write((byte)(Value >> 16)); base.Write((byte)(Value >> 24));
                }
            }
            else
            {
                for (int ii = 0; ii < Data.Length; ii++)
                {
                    uint Value = Data[ii];
                    base.Write((byte)(Value >> 24)); base.Write((byte)(Value >> 16));
                    base.Write((byte)(Value >> 8)); base.Write((byte)(Value));
                }
            }
        }

        public void Write(ulong[] Data)
        {
            if (IsLittleEndian)
            {
                for (int ii = 0; ii < Data.Length; ii++)
                {
                    ulong Value = Data[ii];
                    base.Write((byte)(Value)); base.Write((byte)(Value >> 8));
                    base.Write((byte)(Value >> 16)); base.Write((byte)(Value >> 24));
                    base.Write((byte)(Value >> 32)); base.Write((byte)(Value >> 40));
                    base.Write((byte)(Value >> 48)); base.Write((byte)(Value >> 56));
                }
            }
            else
            {
                for (int ii = 0; ii < Data.Length; ii++)
                {
                    ulong Value = Data[ii];
                    base.Write((byte)(Value >> 56)); base.Write((byte)(Value >> 48));
                    base.Write((byte)(Value >> 40)); base.Write((byte)(Value >> 32));
                    base.Write((byte)(Value >> 24)); base.Write((byte)(Value >> 16));
                    base.Write((byte)(Value >> 8)); base.Write((byte)(Value));
                }
            }
        }

        #endregion

        #region "Signed arrays"

        public void Write(short[] Data)
        {
            if (IsLittleEndian)
            {
                for (int ii = 0; ii < Data.Length; ii++)
                {
                    ushort Value = (ushort)Data[ii];
                    base.Write((byte)(Value)); base.Write((byte)(Value >> 8));
                }
            }
            else
            {
                for (int ii = 0; ii < Data.Length; ii++)
                {
                    ushort Value = (ushort)Data[ii];
                    base.Write((byte)(Value >> 8)); base.Write((byte)(Value));
                }
            }
        }

        public void Write(int[] Data)
        {
            if (IsLittleEndian)
            {
                for (int ii = 0; ii < Data.Length; ii++)
                {
                    uint Value = (uint)Data[ii];
                    base.Write((byte)(Value)); base.Write((byte)(Value >> 8));
                    base.Write((byte)(Value >> 16)); base.Write((byte)(Value >> 24));
                }
            }
            else
            {
                for (int ii = 0; ii < Data.Length; ii++)
                {
                    uint Value = (uint)Data[ii];
                    base.Write((byte)(Value >> 24)); base.Write((byte)(Value >> 16));
                    base.Write((byte)(Value >> 8)); base.Write((byte)(Value));
                }
            }
        }

        public void Write(long[] Data)
        {
            if (IsLittleEndian)
            {
                for (int ii = 0; ii < Data.Length; ii++)
                {
                    ulong Value = (ulong)Data[ii];
                    base.Write((byte)(Value)); base.Write((byte)(Value >> 8));
                    base.Write((byte)(Value >> 16)); base.Write((byte)(Value >> 24));
                    base.Write((byte)(Value >> 32)); base.Write((byte)(Value >> 40));
                    base.Write((byte)(Value >> 48)); base.Write((byte)(Value >> 56));
                }
            }
            else
            {
                for (int ii = 0; ii < Data.Length; ii++)
                {
                    ulong Value = (ulong)Data[ii];
                    base.Write((byte)(Value >> 56)); base.Write((byte)(Value >> 48));
                    base.Write((byte)(Value >> 40)); base.Write((byte)(Value >> 32));
                    base.Write((byte)(Value >> 24)); base.Write((byte)(Value >> 16));
                    base.Write((byte)(Value >> 8)); base.Write((byte)(Value));
                }
            }
        }

        #endregion

        #region "Floating-Point"

        public void Write(float[] Data)
        {
            if (IsLittleEndian == BitConverter.IsLittleEndian)
            {
                for (int ii = 0; ii < Data.Length; ii++)
                {
                    byte[] Value = BitConverter.GetBytes(Data[ii]);
                    base.Write(Value[0]); base.Write(Value[1]); base.Write(Value[2]); base.Write(Value[3]);
                }
            }
            else
            {
                for (int ii = 0; ii < Data.Length; ii++)
                {
                    byte[] Value = BitConverter.GetBytes(Data[ii]);
                    base.Write(Value[3]); base.Write(Value[2]); base.Write(Value[1]); base.Write(Value[0]);
                }
            }
        }

        public void Write(double[] Data)
        {
            if (IsLittleEndian)
            {
                for (int ii = 0; ii < Data.Length; ii++)
                {
                    ulong Value = (ulong)BitConverter.DoubleToInt64Bits(Data[ii]);
                    base.Write((byte)(Value)); base.Write((byte)(Value >> 8));
                    base.Write((byte)(Value >> 16)); base.Write((byte)(Value >> 24));
                    base.Write((byte)(Value >> 32)); base.Write((byte)(Value >> 40));
                    base.Write((byte)(Value >> 48)); base.Write((byte)(Value >> 56));
                }
            }
            else
            {
                for (int ii = 0; ii < Data.Length; ii++)
                {
                    ulong Value = (ulong)BitConverter.DoubleToInt64Bits(Data[ii]);
                    base.Write((byte)(Value >> 56)); base.Write((byte)(Value >> 48));
                    base.Write((byte)(Value >> 40)); base.Write((byte)(Value >> 32));
                    base.Write((byte)(Value >> 24)); base.Write((byte)(Value >> 16));
                    base.Write((byte)(Value >> 8)); base.Write((byte)(Value));
                }
            }
        }

        public void Write(decimal[] Data)
        {
            throw new NotImplementedException();
        }

        #endregion

        #endregion

        #region "Endian 2D (Row-Major) Array writers"

        /** Optimization continues to be important in EndianBinaryWriter, and this is another great
         *  example of a good place for it.  The matrices happen to be linear in memory already,
         *  so we are working really hard here in order to accomplish what could be accomplished
         *  quickly and easily. 
         */

        public void Write(byte[,] Matrix)
        {
            /** Optimization:   The original form of this routine was a nested forloop (2-D loop) which
             *  called base.Write() on each element.  This had performance of 13ms for ~1MB to a null stream.
             *  
             *  Next, a 1-D byte[] was created on the stack here and the matrix was copied into the linear
             *  array prior to making a single call to base.Write() on the entire linear array.  This had
             *  performance of 9ms for ~1MB to a null stream.
             *  
             *  Finally, a conversion to a linear byte array conducted using the Buffer.BlockCopy() 
             *  routine resulted in performance of 0.2ms for ~1MB to a null stream.
             *  
             *  IMPORTANT:  Calling base.Write(byte[]) had terrible performance.  It must have been calling
             *  Write() for each byte in the linear array.  Changing the call to 
             *  base.BaseStream.Write(byte[], int, int) resulted in drastic improvement - from over a second
             *  to a fraction of a ms!
             *  
             *  Similar savings should be considered in other EndianBinaryWriter routines where possible.
             */            
            byte[] linear = new byte[Matrix.Length];
            Buffer.BlockCopy(Matrix, 0, linear, 0, Matrix.Length);            
            base.BaseStream.Write(linear, 0, linear.Length);
        }

        public void Write(sbyte[,] Matrix)
        {
            for (int ii = 0; ii < Matrix.GetLength(0); ii++)
                for (int jj = 0; jj < Matrix.GetLength(1); jj++) base.Write(Matrix[ii, jj]);
        }

        #region "Unsigned 2D arrays"

#       if !AnyCPU
        unsafe 
#       endif
            public void Write(ushort[,] Data)
        {
#           if AnyCPU
            if (IsLittleEndian == BitConverter.IsLittleEndian)
            {
                // This technique, without endian-swap, profiled as:
                //  Profiler 'EndianBinaryWriter - Write U16 Matrix' saw performance of 0.549 ms < 0.890 ms (Mean) < 5.222 ms with stddev of 0.481 ms.  111 Samples.
                byte[] tmp = new byte[Data.GetLongLength(0) * Data.GetLongLength(1) * sizeof(ushort)];
                Buffer.BlockCopy(Data, 0, tmp, 0, tmp.Length);
                base.Write(tmp);
            }
            else if (IsLittleEndian)
            {
                // This technique profiled as:
                //  Profiler 'EndianBinaryWriter - Write U16 Matrix' saw performance of 0.015 s < 0.024 s (Mean) < 0.026 s with stddev of 1.851 ms.  66 Samples.
                for (int ii = 0; ii < Data.GetLength(0); ii++)
                {
                    for (int jj = 0; jj < Data.GetLength(1); jj++)
                    {
                        ushort Value = Data[ii,jj];
                        base.Write((byte)(Value)); base.Write((byte)(Value >> 8));
                    }
                }
            }
            else
            {                
                for (int ii = 0; ii < Data.GetLength(0); ii++)
                {
                    for (int jj = 0; jj < Data.GetLength(1); jj++)
                    {
                        ushort Value = Data[ii,jj];
                        base.Write((byte)(Value >> 8)); base.Write((byte)(Value));
                    }
                }
            }
#           else
            // This technique, without endian-swap, profiled as:
            //  Profiler 'EndianBinaryWriter - Write U16 Matrix' saw performance of 0.549 ms < 0.890 ms (Mean) < 5.222 ms with stddev of 0.481 ms.  111 Samples.
            byte[] tmp = new byte[Data.GetLongLength(0) * Data.GetLongLength(1) * sizeof(ushort)];

            if (IsLittleEndian != BitConverter.IsLittleEndian)
            {
                // With this endian-swap technique included:
                //  Profiler 'EndianBinaryWriter - Write U16 Matrix' saw performance of 3.352 ms < 5.450 ms (Mean) < 0.010 s with stddev of 0.795 ms.  104 Samples.
                int Count = tmp.Length / sizeof(ushort);
                fixed (byte* pTmp = tmp) 
                {
                    byte* p = pTmp;
                    /** For 2-byte swap **/
                    while (Count-- > 0) {
                        byte FirstByte = *p;
                        *p = *(p + 1);
                        p++;
                        *p = FirstByte;
                        p++;
                    }
                    /** For 4-byte swap:  (Also change Count)
                    while (Count-- > 0) {
                        byte a = *p;
                        p++;
                        byte b = *p;
                        *p = *(p + 1);
                        p++;
                        *p = b;
                        p++;
                        *(p - 3) = *p;
                        *p = a;
                        p++;
                    }
                     */
                }
            }

            Buffer.BlockCopy(Data, 0, tmp, 0, tmp.Length);
            base.Write(tmp);            
#           endif
        }

        public void Write(uint[,] Data)
        {
            if (IsLittleEndian)
            {
                for (int ii = 0; ii < Data.GetLength(0); ii++)
                {
                    for (int jj=0; jj < Data.GetLength(1); jj++)
                    {
                        uint Value = Data[ii,jj];
                        base.Write((byte)(Value)); base.Write((byte)(Value >> 8));
                        base.Write((byte)(Value >> 16)); base.Write((byte)(Value >> 24));
                    }
                }
            }
            else
            {
                for (int ii = 0; ii < Data.GetLength(0); ii++)
                {
                    for (int jj = 0; jj < Data.GetLength(1); jj++)
                    {
                        uint Value = Data[ii,jj];
                        base.Write((byte)(Value >> 24)); base.Write((byte)(Value >> 16));
                        base.Write((byte)(Value >> 8)); base.Write((byte)(Value));
                    }
                }
            }
        }

        public void Write(ulong[,] Data)
        {
            if (IsLittleEndian)
            {
                for (int ii = 0; ii < Data.GetLength(0); ii++)
                {
                    for (int jj = 0; jj < Data.GetLength(1); jj++)
                    {
                        ulong Value = Data[ii,jj];
                        base.Write((byte)(Value)); base.Write((byte)(Value >> 8));
                        base.Write((byte)(Value >> 16)); base.Write((byte)(Value >> 24));
                        base.Write((byte)(Value >> 32)); base.Write((byte)(Value >> 40));
                        base.Write((byte)(Value >> 48)); base.Write((byte)(Value >> 56));
                    }
                }
            }
            else
            {
                for (int ii = 0; ii < Data.GetLength(0); ii++)
                {
                    for (int jj = 0; jj < Data.GetLength(1); jj++)
                    {
                        ulong Value = Data[ii,jj];
                        base.Write((byte)(Value >> 56)); base.Write((byte)(Value >> 48));
                        base.Write((byte)(Value >> 40)); base.Write((byte)(Value >> 32));
                        base.Write((byte)(Value >> 24)); base.Write((byte)(Value >> 16));
                        base.Write((byte)(Value >> 8)); base.Write((byte)(Value));
                    }
                }
            }
        }

        #endregion

        #region "Signed 2D arrays"

        public void Write(short[,] Data)
        {
            if (IsLittleEndian)
            {
                for (int ii = 0; ii < Data.GetLength(0); ii++)
                {
                    for (int jj = 0; jj < Data.GetLength(1); jj++)
                    {
                        ushort Value = (ushort)Data[ii,jj];
                        base.Write((byte)(Value)); base.Write((byte)(Value >> 8));
                    }
                }
            }
            else
            {
                for (int ii = 0; ii < Data.GetLength(0); ii++)
                {
                    for (int jj = 0; jj < Data.GetLength(1); jj++)
                    {
                        ushort Value = (ushort)Data[ii,jj];
                        base.Write((byte)(Value >> 8)); base.Write((byte)(Value));
                    }
                }
            }
        }

        public void Write(int[,] Data)
        {
            if (IsLittleEndian)
            {
                for (int ii = 0; ii < Data.GetLength(0); ii++)
                {
                    for (int jj = 0; jj < Data.GetLength(1); jj++)
                    {
                        uint Value = (uint)Data[ii,jj];
                        base.Write((byte)(Value)); base.Write((byte)(Value >> 8));
                        base.Write((byte)(Value >> 16)); base.Write((byte)(Value >> 24));
                    }
                }
            }
            else
            {
                for (int ii = 0; ii < Data.GetLength(0); ii++)
                {
                    for (int jj = 0; jj < Data.GetLength(1); jj++)
                    {
                        uint Value = (uint)Data[ii,jj];
                        base.Write((byte)(Value >> 24)); base.Write((byte)(Value >> 16));
                        base.Write((byte)(Value >> 8)); base.Write((byte)(Value));
                    }
                }
            }
        }

        public void Write(long[,] Data)
        {
            if (IsLittleEndian)
            {
                for (int ii = 0; ii < Data.GetLength(0); ii++)
                {
                    for (int jj = 0; jj < Data.GetLength(1); jj++)
                    {
                        ulong Value = (ulong)Data[ii, jj];
                        base.Write((byte)(Value)); base.Write((byte)(Value >> 8));
                        base.Write((byte)(Value >> 16)); base.Write((byte)(Value >> 24));
                        base.Write((byte)(Value >> 32)); base.Write((byte)(Value >> 40));
                        base.Write((byte)(Value >> 48)); base.Write((byte)(Value >> 56));
                    }
                }
            }
            else
            {
                for (int ii = 0; ii < Data.GetLength(0); ii++)
                {
                    for (int jj = 0; jj < Data.GetLength(1); jj++)
                    {
                        ulong Value = (ulong)Data[ii,jj];
                        base.Write((byte)(Value >> 56)); base.Write((byte)(Value >> 48));
                        base.Write((byte)(Value >> 40)); base.Write((byte)(Value >> 32));
                        base.Write((byte)(Value >> 24)); base.Write((byte)(Value >> 16));
                        base.Write((byte)(Value >> 8)); base.Write((byte)(Value));
                    }
                }
            }
        }

        #endregion

        #region "Floating-Point 2D Arrays"

        public void Write(float[,] Data)
        {
            if (IsLittleEndian == BitConverter.IsLittleEndian)
            {
                for (int ii = 0; ii < Data.GetLength(0); ii++)
                {
                    for (int jj = 0; jj < Data.GetLength(1); jj++)
                    {
                        byte[] Value = BitConverter.GetBytes(Data[ii,jj]);
                        base.Write(Value[0]); base.Write(Value[1]); base.Write(Value[2]); base.Write(Value[3]);
                    }
                }
            }
            else
            {
                for (int ii = 0; ii < Data.GetLength(0); ii++)
                {
                    for (int jj = 0; jj < Data.GetLength(1); jj++)
                    {
                        byte[] Value = BitConverter.GetBytes(Data[ii,jj]);
                        base.Write(Value[3]); base.Write(Value[2]); base.Write(Value[1]); base.Write(Value[0]);
                    }
                }
            }
        }

        public void Write(double[,] Data)
        {
            if (IsLittleEndian)
            {
                for (int ii = 0; ii < Data.GetLength(0); ii++)
                {
                    for (int jj = 0; jj < Data.GetLength(1); jj++)
                    {
                        ulong Value = (ulong)BitConverter.DoubleToInt64Bits(Data[ii,jj]);
                        base.Write((byte)(Value)); base.Write((byte)(Value >> 8));
                        base.Write((byte)(Value >> 16)); base.Write((byte)(Value >> 24));
                        base.Write((byte)(Value >> 32)); base.Write((byte)(Value >> 40));
                        base.Write((byte)(Value >> 48)); base.Write((byte)(Value >> 56));
                    }
                }
            }
            else
            {
                for (int ii = 0; ii < Data.GetLength(0); ii++)
                {
                    for (int jj = 0; jj < Data.GetLength(1); jj++)
                    {
                        ulong Value = (ulong)BitConverter.DoubleToInt64Bits(Data[ii,jj]);
                        base.Write((byte)(Value >> 56)); base.Write((byte)(Value >> 48));
                        base.Write((byte)(Value >> 40)); base.Write((byte)(Value >> 32));
                        base.Write((byte)(Value >> 24)); base.Write((byte)(Value >> 16));
                        base.Write((byte)(Value >> 8)); base.Write((byte)(Value));
                    }
                }
            }
        }

        public void Write(decimal[,] Data)
        {
            throw new NotImplementedException();
        }

        #endregion

        #endregion
    }
}
