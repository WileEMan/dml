using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace WileyBlack.Platforms
{    
    public class EndianBinaryReader : IDisposable
    {
        #region "Initialization and Control"

        /// <summary>
        /// LittleEndian indicates if the underlying stream is represented in
        /// little-endian (true) or big-endian (false) format.  All Read..()
        /// functions in EndianBinaryReader which read more than 1 byte utilize
        /// LittleEndian to determine the sequencing expected from the stream.
        /// LittleEndian can be changed at any time to alter the sequencing applied.
        /// </summary>
        public bool LittleEndian;

        public Stream BaseStream;

        public EndianBinaryReader(Stream input, bool LittleEndian)            
        {
            this.LittleEndian = LittleEndian;
            this.BaseStream = input;
        }

        public void Dispose()
        {
            if (BaseStream != null) { BaseStream.Dispose(); BaseStream = null; }
            GC.SuppressFinalize(this);
        }

        ~EndianBinaryReader() { Dispose(); }

        public void Close()
        {
            if (BaseStream != null) BaseStream.Close();
        }

        #endregion

        #region "Elementary readers"

        public byte ReadByte()
        {
            int ret = BaseStream.ReadByte();
            if (ret < 0) throw new EndOfStreamException();
            return (byte)ret;
        }

        public byte[] ReadByte(int Count)
        {
            byte[] buffer = new byte[Count];
            int nRead = BaseStream.Read(buffer, 0, (int)Count);
            if (nRead < Count) throw new EndOfStreamException();
            return buffer;
        }

        public int Read(byte[] buffer, int offset, int count)
        {
            return BaseStream.Read(buffer, offset, count);
        }

        #endregion

        #region "Compact-Integer readers (Endian-Independent)"

        public UInt32 ReadCompact32()
        {
            UInt32 ret = 0;
            byte ch = ReadByte();

            if ((ch & 0x80) == 0x80)
                ret = ((UInt32)ch & 0x7F);
            else if ((ch & 0xC0) == 0x40)
            {
                ret = ((UInt32)ch & 0x3F) << 8;
                ret |= ((UInt32)ReadByte());
            }
            else if ((ch & 0xE0) == 0x20)
            {
                ret |= ((UInt32)ch & 0x1F) << 16;
                ret |= ((UInt32)ReadByte()) << 8;
                ret |= ((UInt32)ReadByte());
            }
            else if ((ch & 0xF0) == 0x10)
            {
                ret |= ((UInt32)ch & 0x0F) << 24;
                ret |= ((UInt32)ReadByte()) << 16;
                ret |= ((UInt32)ReadByte()) << 8;
                ret |= ((UInt32)ReadByte());
            }
            else if (ch == 0x08)
            {
                ret |= ((UInt32)ReadByte()) << 24;
                ret |= ((UInt32)ReadByte()) << 16;
                ret |= ((UInt32)ReadByte()) << 8;
                ret |= ((UInt32)ReadByte());
            }
            else
            {
                if (BaseStream.CanSeek)
                    throw new Exception("Invalid Compact-32 encoding sequence at position [" + BaseStream.Position + "].");
                else
                    throw new FormatException("Invalid Compact-32 encoding sequence.");
            }
            return ret;
        }

        public UInt64 ReadCompact64()
        {
            UInt64 ret = 0;
            byte ch = ReadByte();

            if ((ch & 0x80) == 0x80) ret = ((UInt64)ch & 0x7F);
            else if ((ch & 0xC0) == 0x40)
            {
                ret = ((UInt64)ch & 0x3F) << 8;
                ret |= ((UInt64)ReadByte());
            }
            else if ((ch & 0xE0) == 0x20)
            {
                ret = ((UInt64)ch & 0x1F) << 16;
                ret |= ((UInt64)ReadByte()) << 8;
                ret |= ((UInt64)ReadByte());
            }
            else if ((ch & 0xF0) == 0x10)
            {
                ret = ((UInt64)ch & 0x0F) << 24;
                ret |= ((UInt64)ReadByte()) << 16;
                ret |= ((UInt64)ReadByte()) << 8;
                ret |= ((UInt64)ReadByte());
            }
            else if ((ch & 0xF8) == 0x08)
            {
                ret = ((UInt64)ch & 0x07) << 32;
                ret |= ((UInt64)ReadByte()) << 24;
                ret |= ((UInt64)ReadByte()) << 16;
                ret |= ((UInt64)ReadByte()) << 8;
                ret |= ((UInt64)ReadByte());
            }
            else if ((ch & 0xFC) == 0x04)
            {
                ret = ((UInt64)ch & 0x03) << 40;
                ret |= ((UInt64)ReadByte()) << 32;
                ret |= ((UInt64)ReadByte()) << 24;
                ret |= ((UInt64)ReadByte()) << 16;
                ret |= ((UInt64)ReadByte()) << 8;
                ret |= ((UInt64)ReadByte());
            }
            else if ((ch & 0xFE) == 0x02)
            {
                ret = ((UInt64)ch & 0x01) << 48;
                ret |= ((UInt64)ReadByte()) << 40;
                ret |= ((UInt64)ReadByte()) << 32;
                ret |= ((UInt64)ReadByte()) << 24;
                ret |= ((UInt64)ReadByte()) << 16;
                ret |= ((UInt64)ReadByte()) << 8;
                ret |= ((UInt64)ReadByte());
            }
            else if (ch == 0x01)
            {
                ret = ((UInt64)ReadByte()) << 48;
                ret |= ((UInt64)ReadByte()) << 40;
                ret |= ((UInt64)ReadByte()) << 32;
                ret |= ((UInt64)ReadByte()) << 24;
                ret |= ((UInt64)ReadByte()) << 16;
                ret |= ((UInt64)ReadByte()) << 8;
                ret |= ((UInt64)ReadByte());
            }
            else if (ch == 0)
            {
                ret = ((UInt64)ReadByte()) << 56;
                ret |= ((UInt64)ReadByte()) << 48;
                ret |= ((UInt64)ReadByte()) << 40;
                ret |= ((UInt64)ReadByte()) << 32;
                ret |= ((UInt64)ReadByte()) << 24;
                ret |= ((UInt64)ReadByte()) << 16;
                ret |= ((UInt64)ReadByte()) << 8;
                ret |= ((UInt64)ReadByte());
            }
            else throw new Exception();     // This exception would indicate a logic flaw in this routine.
            
            return ret;
        }

        public Int64 ReadCompactS64()
        {
            UInt64 ret = 0;
            byte ch = ReadByte();

            if ((ch & 0x80) == 0x80) return SignExtend(((UInt64)ch & 0x7F), 7);
            else if ((ch & 0xC0) == 0x40)
            {
                ret = ((UInt64)ch & 0x3F) << 8;
                ret |= ((UInt64)ReadByte());
                return SignExtend(ret, 14);
            }
            else if ((ch & 0xE0) == 0x20)
            {
                ret = ((UInt64)ch & 0x1F) << 16;
                ret |= ((UInt64)ReadByte()) << 8;
                ret |= ((UInt64)ReadByte());
                return SignExtend(ret, 21);
            }
            else if ((ch & 0xF0) == 0x10)
            {
                ret = ((UInt64)ch & 0x0F) << 24;
                ret |= ((UInt64)ReadByte()) << 16;
                ret |= ((UInt64)ReadByte()) << 8;
                ret |= ((UInt64)ReadByte());
                return SignExtend(ret, 28);
            }
            else if ((ch & 0xF8) == 0x08)
            {
                ret = ((UInt64)ch & 0x07) << 32;
                ret |= ((UInt64)ReadByte()) << 24;
                ret |= ((UInt64)ReadByte()) << 16;
                ret |= ((UInt64)ReadByte()) << 8;
                ret |= ((UInt64)ReadByte());
                return SignExtend(ret, 35);
            }
            else if ((ch & 0xFC) == 0x04)
            {
                ret = ((UInt64)ch & 0x03) << 40;
                ret |= ((UInt64)ReadByte()) << 32;
                ret |= ((UInt64)ReadByte()) << 24;
                ret |= ((UInt64)ReadByte()) << 16;
                ret |= ((UInt64)ReadByte()) << 8;
                ret |= ((UInt64)ReadByte());
                return SignExtend(ret, 42);
            }
            else if ((ch & 0xFE) == 0x02)
            {
                ret = ((UInt64)ch & 0x01) << 48;
                ret |= ((UInt64)ReadByte()) << 40;
                ret |= ((UInt64)ReadByte()) << 32;
                ret |= ((UInt64)ReadByte()) << 24;
                ret |= ((UInt64)ReadByte()) << 16;
                ret |= ((UInt64)ReadByte()) << 8;
                ret |= ((UInt64)ReadByte());
                return SignExtend(ret, 49);
            }
            else if (ch == 0x01)
            {
                ret = ((UInt64)ReadByte()) << 48;
                ret |= ((UInt64)ReadByte()) << 40;
                ret |= ((UInt64)ReadByte()) << 32;
                ret |= ((UInt64)ReadByte()) << 24;
                ret |= ((UInt64)ReadByte()) << 16;
                ret |= ((UInt64)ReadByte()) << 8;
                ret |= ((UInt64)ReadByte());
                return SignExtend(ret, 56);
            }
            else if (ch == 0)
            {
                ret = ((UInt64)ReadByte()) << 56;
                ret |= ((UInt64)ReadByte()) << 48;
                ret |= ((UInt64)ReadByte()) << 40;
                ret |= ((UInt64)ReadByte()) << 32;
                ret |= ((UInt64)ReadByte()) << 24;
                ret |= ((UInt64)ReadByte()) << 16;
                ret |= ((UInt64)ReadByte()) << 8;
                ret |= ((UInt64)ReadByte());
                return (long)ret;
            }
            else throw new Exception();     // This exception would indicate a logic flaw in this routine.            
        }

        Int64 SignExtend(UInt64 Value, int nBits)
        {
            // See ReadI() for similar explanation...            
            ulong mask = (1UL << (nBits - 1));
            if ((Value & mask) != 0UL)
            {
                ulong extendmask = ulong.MaxValue << nBits;
                return (long)(Value | extendmask);
            }
            return (long)Value;
        }
        
        #endregion

        #region "Scalar readers"

        /// <summary>
        /// ReadLargeUI() reads an arbitrary length unsigned value from the underlying
        /// stream, compensating for the endianness of the stream as needed.  See ReadUI()
        /// for values up to 64-bits.  ReadLargeUI() performs the operation on values
        /// of arbitrary length beyond 64-bits, but it must still be a multiple of 8-bits.
        /// </summary>
        /// <seealso>ReadUI()</seealso>
        /// <param name="nBytes">The number of bytes representing the value in
        /// the underlying stream.</param>
        /// <returns>An unsigned bytewise representation of the value read from the
        /// stream.</returns>
        public byte[] ReadLargeUI(int nBytes)
        {
            byte[] ret = new byte[nBytes];
            if (LittleEndian == BitConverter.IsLittleEndian)
            {
                // No order swapping necessary.  Read the lowest address from the stream
                // into the lowest memory address.
                for (int ii = 0; ii < nBytes; ii++) ret[ii] = ReadByte();
                return ret;
            }
            else
            {
                // Order swapping is required.  Read the lowest address from the stream
                // into the highest memory address.
                for (int ii = nBytes - 1; ii >= 0; ii--) ret[ii] = ReadByte();
                return ret;
            }
        }

        /// <summary>
        /// ReadUI() reads an arbitrary length (up to 64-bits) unsigned value 
        /// from the underlying stream, compensating for the endianness of the 
        /// stream as needed.
        /// </summary>
        /// <seealso>ReadLargeUI()</seealso>
        /// <param name="nBytes">The number of bytes representing the value in
        /// the underlying stream.</param>
        /// <returns>An unsigned 64-bit representation of the value read from the
        /// stream.</returns>
        public UInt64 ReadUI(int nBytes)
        {
            UInt64 ret = 0;
            if (LittleEndian)
            {
                // Reading from a little-endian stream regardless of memory architecture
                // i.e. the memory value 0x0A0B0C0D
                // First byte read:  0x0D
                // Second byte read: 0x0C
                // Third byte read:  0x0B
                // Fourth byte read: 0x0A                

                int nShift = 0;
                for (int ii = 0; ii < nBytes; ii++)
                {
                    ret |= ((UInt64)ReadByte() << nShift);                    
                    nShift += 8;
                }
                return ret;
            }
            else
            {
                // Reading from a big-endian stream regardless of memory architecture
                // i.e. the memory value 0x0A0B0C0D
                // First byte read:  0x0A
                // Second byte read: 0x0B
                // Third byte read:  0x0C
                // Fourth byte read: 0x0D

                int nShift = (nBytes * 8) - 8;
                for (int ii = 0; ii < nBytes; ii++)
                {
                    ret |= ((UInt64)ReadByte() << nShift);
                    nShift -= 8;
                }
                return ret;
            }
        }

        public ushort ReadUInt16()
        {
            if (LittleEndian)
                return (ushort)((ushort)ReadByte() | ((ushort)ReadByte() << 8));
            else           
                return (ushort)(((ushort)ReadByte() << 8) | (ushort)ReadByte());
        }

        public uint ReadUInt24()
        {
            if (LittleEndian)
                return (uint)ReadByte() | ((uint)ReadByte() << 8) | ((uint)ReadByte() << 16);
            else
                return ((uint)ReadByte() << 16) | ((uint)ReadByte() << 8) | (uint)ReadByte();
        }

        public uint ReadUInt32()
        {
            if (LittleEndian)
                return (uint)ReadByte() | ((uint)ReadByte() << 8) | ((uint)ReadByte() << 16) | ((uint)ReadByte() << 24);            
            else            
                return ((uint)ReadByte() << 24) | ((uint)ReadByte() << 16) | ((uint)ReadByte() << 8) | (uint)ReadByte();            
        }

        public ulong ReadUInt64()
        {
            if (LittleEndian)            
                return (ulong)ReadByte() | ((ulong)ReadByte() << 8) | ((ulong)ReadByte() << 16) | ((ulong)ReadByte() << 24)
                    | ((ulong)ReadByte() << 32) | ((ulong)ReadByte() << 40) | ((ulong)ReadByte() << 48) | ((ulong)ReadByte() << 56);            
            else            
                return ((ulong)ReadByte() << 56) | ((ulong)ReadByte() << 48) | ((ulong)ReadByte() << 40) | ((ulong)ReadByte() << 32)
                    | ((ulong)ReadByte() << 24) | ((ulong)ReadByte() << 16) | ((ulong)ReadByte() << 8) | (ulong)ReadByte();            
        }

        public sbyte ReadSByte() { return (sbyte)ReadByte(); }
        public short ReadInt16() { return (short)ReadUInt16(); }
        public int ReadInt32() { return (int)ReadUInt32(); }
        public  long ReadInt64() { return (long)ReadUInt64(); }

        /// <summary>
        /// ReadInt24() reads a signed 24-bit value from the stream.  The
        /// value is sign-extended to be properly represented in the 32-bit
        /// return value.
        /// </summary>
        /// <returns>A 32-bit representation of the 24-bit stream value</returns>
        public int ReadInt24()
        {
            // See ReadI() for logic description.
            uint ret = ReadUInt24();
            ulong mask = 0x800000UL;
            if ((ret & mask) != 0UL)
            {
                ulong extendmask = 0xFF000000UL;
                return (int)(ret | extendmask);
            }
            return (int)ret;
        }

        /// <summary>
        /// ReadI() reads in an arbitrary length (up to 64-bit) signed integer
        /// from the stream.  ReadI() assumes that the value is stored in 2s
        /// compliment notation (the most commonly used method for storing
        /// signed values in computers).  ReadI() will sign-extend the value
        /// so that it is properly signed in the returned value.
        /// </summary>
        /// <param name="nBytes">The number of bytes representing the signed
        /// integer in the underlying stream.</param>
        /// <returns>A signed 64-bit value representation of the value.</returns>
        public long ReadI(int nBytes)
        {
            /** An example of a 2s compliment number:          
               Decimal	    7-bit notation	8-bit notation
                    −42	    1010110	        1101 0110
                     42	    0101010	        0010 1010
             * 
             *  An example of sign-extending from 16 to 32-bits:
             *  
                int signExtension(int instr) {
                    int value = (0x0000FFFF & instr);
                    int mask = 0x00008000;
                    if (mask & instr) value += 0xFFFF0000;                    
                    return value;
                }
            */

            ulong ret = ReadUI(nBytes);
            ulong mask = (1UL << ((nBytes * 8) - 1));
            if ((ret & mask) != 0UL)
            {
                ulong extendmask = ulong.MaxValue << (nBytes * 8);
                return (long)(ret | extendmask);
            }
            return (long)ret;
        }

        public float ReadSingle() {
            byte[] raw = new byte [4];
            if (LittleEndian == BitConverter.IsLittleEndian)
            {   
                /** Consider the memory value 0x0A0B0C0D
                 * 
                 *  Both LittleEndian case:    
                 *      First byte read:  0x0D
                 *      Second byte read: 0x0C
                 *      Third byte read:  0x0B
                 *      Fourth byte read: 0x0A
                 *      
                 *      On a little-endian system, BitConverter wants to receive 
                 *      the array [0D] [0C] [0B] [0A] where 0D has index 0.
                 *      
                 *  Both BigEndian case:    
                 *      First byte read:  0x0A
                 *      Second byte read: 0x0B
                 *      Third byte read:  0x0C
                 *      Fourth byte read: 0x0D
                 *
                 *      On a big-endian system, BitConverter wants to receive 
                 *      the array [0A] [0B] [0C] [0D] where 0A has index 0.
                 */

                for (int kk = 0; kk < 4; kk++) raw[kk] = ReadByte();
                return BitConverter.ToSingle(raw, 0);
            }
            else
            {
                /** Consider the memory value 0x0A0B0C0D
                 * 
                 *  LittleEndian stream, BigEndian system case:    
                 *      First byte read:  0x0D
                 *      Second byte read: 0x0C
                 *      Third byte read:  0x0B
                 *      Fourth byte read: 0x0A
                 *      
                 *      On a big-endian system, BitConverter wants to receive 
                 *      the array [0A] [0B] [0C] [0D] where 0A has index 0.
                 *      
                 *  BigEndian stream, LittleEndian system case:
                 *      First byte read:  0x0A
                 *      Second byte read: 0x0B
                 *      Third byte read:  0x0C
                 *      Fourth byte read: 0x0D
                 *
                 *      On a little-endian system, BitConverter wants to receive 
                 *      the array [0D] [0C] [0B] [0A] where 0D has index 0.
                 */

                for (int kk = 3; kk >= 0; kk--) raw[kk] = ReadByte();
                return BitConverter.ToSingle(raw, 0);
            }
        }
        
        public double ReadDouble()
        {
            // See ReadSingle() for logic description.
            byte[] raw = new byte[8];
            if (LittleEndian == BitConverter.IsLittleEndian)
            {
                for (int kk = 0; kk < 8; kk++) raw[kk] = ReadByte();
                return BitConverter.ToDouble(raw, 0);
            }
            else
            {
                for (int kk = 7; kk >= 0; kk--) raw[kk] = ReadByte();
                return BitConverter.ToDouble(raw, 0);
            }
        }

        private static decimal ToDecimal(byte[] bytes)
        {
            int[] bits = new int[4];
            bits[0] = ((bytes[0] | (bytes[1] << 8)) | (bytes[2] << 0x10)) | (bytes[3] << 0x18); //lo
            bits[1] = ((bytes[4] | (bytes[5] << 8)) | (bytes[6] << 0x10)) | (bytes[7] << 0x18); //mid
            bits[2] = ((bytes[8] | (bytes[9] << 8)) | (bytes[10] << 0x10)) | (bytes[11] << 0x18); //hi
            bits[3] = ((bytes[12] | (bytes[13] << 8)) | (bytes[14] << 0x10)) | (bytes[15] << 0x18); //flags

            return new decimal(bits);
        }

        private static byte[] GetBytes(decimal d)
        {
            byte[] bytes = new byte[16];

            int[] bits = decimal.GetBits(d);
            int lo = bits[0];
            int mid = bits[1];
            int hi = bits[2];
            int flags = bits[3];

            bytes[0] = (byte)lo;
            bytes[1] = (byte)(lo >> 8);
            bytes[2] = (byte)(lo >> 0x10);
            bytes[3] = (byte)(lo >> 0x18);
            bytes[4] = (byte)mid;
            bytes[5] = (byte)(mid >> 8);
            bytes[6] = (byte)(mid >> 0x10);
            bytes[7] = (byte)(mid >> 0x18);
            bytes[8] = (byte)hi;
            bytes[9] = (byte)(hi >> 8);
            bytes[10] = (byte)(hi >> 0x10);
            bytes[11] = (byte)(hi >> 0x18);
            bytes[12] = (byte)flags;
            bytes[13] = (byte)(flags >> 8);
            bytes[14] = (byte)(flags >> 0x10);
            bytes[15] = (byte)(flags >> 0x18);

            return bytes;
        }

        public decimal ReadDecimal()
        {
            throw new NotSupportedException("Need to verify the endian-ness behavior of decimal decoding.");
#           if false
            // See ReadSingle() for logic description.
            byte[] raw = new byte[10];
            if (IsLittleEndian == BitConverter.IsLittleEndian)
            {
                for (int kk = 0; kk < 16; kk++) raw[kk] = ReadByte();                
                return ToDecimal(raw);
            }
            else
            {
                for (int kk = 16; kk >= 0; kk--) raw[kk] = ReadByte();
                return ToDecimal(raw);
            }
#           endif
        }

        #endregion

        #region "Array readers"

        /** Optimization: These readers could afford to be optimized **/        

        public ushort[] ReadUInt16(int Count)
        {
            ushort[] ret = new ushort[Count];
            if (LittleEndian)
            {
                for (int ii = 0; ii < Count; ii++)
                {
                    ret[ii] = (ushort)((ushort)ReadByte() | ((ushort)ReadByte() << 8));
                }
            }
            else
            {
                for (int ii = 0; ii < Count; ii++)
                {
                    ret[ii] = (ushort)(((ushort)ReadByte() << 8) | (ushort)ReadByte());
                }
            }
            return ret;
        }

        public uint[] ReadUInt24(int Count)
        {
            uint[] ret = new uint[Count];
            if (LittleEndian)
            {
                for (int ii = 0; ii < Count; ii++)
                {
                    ret[ii] = (uint)ReadByte() | ((uint)ReadByte() << 8) | ((uint)ReadByte() << 16);
                }
            }
            else
            {
                for (int ii = 0; ii < Count; ii++)
                {
                    ret[ii] = ((uint)ReadByte() << 16) | ((uint)ReadByte() << 8) | (uint)ReadByte();
                }
            }
            return ret;
        }

        public uint[] ReadUInt32(int Count)
        {
            uint[] ret = new uint[Count];
            if (LittleEndian)
            {
                for (int ii = 0; ii < Count; ii++)
                {
                    ret[ii] = (uint)ReadByte() | ((uint)ReadByte() << 8) | ((uint)ReadByte() << 16) | ((uint)ReadByte() << 24);
                }
            }
            else
            {
                for (int ii = 0; ii < Count; ii++)
                {
                    ret[ii] = ((uint)ReadByte() << 24) | ((uint)ReadByte() << 16) | ((uint)ReadByte() << 8) | (uint)ReadByte();
                }
            }
            return ret;
        }

        public ulong[] ReadUInt64(int Count)
        {
            ulong[] ret = new ulong[Count];
            if (LittleEndian)
            {
                for (int ii = 0; ii < Count; ii++)
                {
                    ret[ii] = (ulong)ReadByte() | ((ulong)ReadByte() << 8) | ((ulong)ReadByte() << 16) | ((ulong)ReadByte() << 24)
                        | ((ulong)ReadByte() << 32) | ((ulong)ReadByte() << 40) | ((ulong)ReadByte() << 48) | ((ulong)ReadByte() << 56);
                }
            }
            else
            {
                for (int ii = 0; ii < Count; ii++)
                {
                    ret[ii] = ((ulong)ReadByte() << 56) | ((ulong)ReadByte() << 48) | ((ulong)ReadByte() << 40) | ((ulong)ReadByte() << 32)
                        | ((ulong)ReadByte() << 24) | ((ulong)ReadByte() << 16) | ((ulong)ReadByte() << 8) | (ulong)ReadByte();
                }
            }
            return ret;
        }

        public sbyte[] ReadSByte(int Count)
        {
            sbyte[] ret = new sbyte[Count];
            for (int ii = 0; ii < Count; ii++) ret[ii] = (sbyte)ReadByte();
            return ret;
        }

        public short[] ReadInt16(int Count)
        {
            short[] ret = new short[Count];
            if (LittleEndian)
            {
                for (int ii = 0; ii < Count; ii++)
                {
                    ret[ii] = (short)((ushort)ReadByte() | ((ushort)ReadByte() << 8));
                }
            }
            else
            {
                for (int ii = 0; ii < Count; ii++)
                {
                    ret[ii] = (short)(((ushort)ReadByte() << 8) | (ushort)ReadByte());
                }
            }
            return ret;
        }

        /// <summary>
        /// ReadInt24(Count) reads a signed 24-bit array from the stream.  The
        /// values are sign-extended to be properly represented in the 32-bit
        /// return value.
        /// </summary>
        /// <returns>A 32-bit representation of the 24-bit stream array</returns>
        public int[] ReadInt24(int Count)
        {
            int[] ret = new int[Count];
            const uint mask = 0x800000U;
            const uint extendmask = 0xFF000000U;
            uint tmp;
            if (LittleEndian)
            {
                for (int ii = 0; ii < Count; ii++)
                {
                    tmp = (uint)ReadByte() | ((uint)ReadByte() << 8) | ((uint)ReadByte() << 16);
                    if ((tmp & mask) != 0) ret[ii] = (int)(tmp | extendmask);
                    else ret[ii] = (int)tmp;
                }
            }
            else
            {
                for (int ii = 0; ii < Count; ii++)
                {
                    tmp = ((uint)ReadByte() << 16) | ((uint)ReadByte() << 8) | (uint)ReadByte();
                    if ((tmp & mask) != 0) ret[ii] = (int)(tmp | extendmask);
                    else ret[ii] = (int)tmp;
                }
            }
            return ret;
        }        

        public int[] ReadInt32(int Count)
        {
            int[] ret = new int[Count];
            if (LittleEndian)
            {
                for (int ii = 0; ii < Count; ii++)
                {
                    ret[ii] = (int)((uint)ReadByte() | ((uint)ReadByte() << 8) | ((uint)ReadByte() << 16) | ((uint)ReadByte() << 24));
                }
            }
            else
            {
                for (int ii = 0; ii < Count; ii++)
                {
                    ret[ii] = (int)(((uint)ReadByte() << 24) | ((uint)ReadByte() << 16) | ((uint)ReadByte() << 8) | (uint)ReadByte());
                }
            }
            return ret;
        }

        public long[] ReadInt64(int Count)
        {
            long[] ret = new long[Count];
            if (LittleEndian)
            {
                for (int ii = 0; ii < Count; ii++)
                {
                    ret[ii] = (long)((ulong)ReadByte() | ((ulong)ReadByte() << 8) | ((ulong)ReadByte() << 16) | ((ulong)ReadByte() << 24)
                        | ((ulong)ReadByte() << 32) | ((ulong)ReadByte() << 40) | ((ulong)ReadByte() << 48) | ((ulong)ReadByte() << 56));
                }
            }
            else
            {
                for (int ii = 0; ii < Count; ii++)
                {
                    ret[ii] = (long)(((ulong)ReadByte() << 56) | ((ulong)ReadByte() << 48) | ((ulong)ReadByte() << 40) | ((ulong)ReadByte() << 32)
                        | ((ulong)ReadByte() << 24) | ((ulong)ReadByte() << 16) | ((ulong)ReadByte() << 8) | (ulong)ReadByte());
                }
            }
            return ret;
        }

        public float[] ReadSingle(int Count)
        {
            // See scalar overload for comments.
            float[] ret = new float[Count];                        
            if (LittleEndian == BitConverter.IsLittleEndian)
            {                
                byte[] raw = ReadByte(Count * 4);
                for (int kk = 0, ii = 0; kk < raw.Length; kk += 4, ii++) ret[ii] = BitConverter.ToSingle(raw, kk);
                return ret;
            }
            else
            {                
                byte[] raw = new byte[4];
                for (int ii = 0; ii < Count; ii++)
                {
                    for (int kk = 3; kk >= 0; kk--) raw[kk] = ReadByte();
                    ret[ii] = BitConverter.ToSingle(raw, 0);
                }
                return ret;
            }
        }

        public double[] ReadDouble(int Count)
        {
            double[] ret = new double[Count];
            if (LittleEndian == BitConverter.IsLittleEndian)
            {
                byte[] raw = ReadByte(Count * 8);
                for (int kk = 0, ii = 0; kk < raw.Length; kk += 8, ii++) ret[ii] = BitConverter.ToDouble(raw, kk);
                return ret;
            }
            else
            {
                byte[] raw = new byte[8];
                for (int ii = 0; ii < Count; ii++)
                {
                    for (int kk = 7; kk >= 0; kk--) raw[kk] = ReadByte();
                    ret[ii] = BitConverter.ToDouble(raw, 0);
                }
                return ret;
            }
        }

        public decimal[] ReadDecimal(int Count)
        {
            throw new NotImplementedException();            // TODO
        }

        #endregion

        #region "2D Array readers"

        /** Optimization: These readers could really use optimization.  They really need an unsafe
         *  context or a DLL to be efficient. **/

        public byte[,] ReadByte(int Dimension0, int Dimension1)
        {            
#if false
            /** Disabled, better performance below. 
                Profiler 'EndianBinaryReader.ReadByte(2-D)' saw performance of 0.011 s < 0.012 s (Mean) < 0.016 s with stddev of 0.694 ms.  450 Samples.
            **/
            byte[,] ret = new byte[Dimension0, Dimension1];
            for (int ii = 0; ii < Dimension0; ii++)
            {
                for (int jj = 0; jj < Dimension1; jj++)
                {
                    ret[ii, jj] = ReadByte();
                }
            }
#endif

            /** Optimized version:
             *      Profiler 'EndianBinaryReader.ReadByte(2-D)' saw performance of 0.300 ms < 0.571 ms (Mean) < 4.025 ms with stddev of 0.534 ms.  480 Samples.
             */

            byte[] linear = new byte [Dimension0 * Dimension1];
            if (BaseStream.Read(linear, 0, linear.Length) != linear.Length)
                throw new EndOfStreamException();
            byte[,] ret = new byte[Dimension0, Dimension1];
            Buffer.BlockCopy(linear, 0, ret, 0, linear.Length);

            return ret;
        }

        public ushort[,] ReadUInt16(int Dimension0, int Dimension1)
        {
            ushort[,] ret = new ushort[Dimension0, Dimension1];
            if (LittleEndian)
            {
                for (int ii = 0; ii < Dimension0; ii++)
                {
                    for (int jj = 0; jj < Dimension1; jj++)
                    {
                        ret[ii,jj] = (ushort)((ushort)ReadByte() | ((ushort)ReadByte() << 8));
                    }
                }
            }
            else
            {
                for (int ii = 0; ii < Dimension0; ii++)
                {
                    for (int jj = 0; jj < Dimension1; jj++)
                    {
                        ret[ii,jj] = (ushort)(((ushort)ReadByte() << 8) | (ushort)ReadByte());
                    }
                }
            }
            return ret;
        }

        public uint[,] ReadUInt24(int Dimension0, int Dimension1)
        {
            uint[,] ret = new uint[Dimension0, Dimension1];
            if (LittleEndian)
            {
                for (int ii = 0; ii < Dimension0; ii++)
                {
                    for (int jj = 0; jj < Dimension1; jj++)
                    {
                        ret[ii,jj] = (uint)ReadByte() | ((uint)ReadByte() << 8) | ((uint)ReadByte() << 16);
                    }
                }
            }
            else
            {
                for (int ii = 0; ii < Dimension0; ii++)
                {
                    for (int jj = 0; jj < Dimension1; jj++)
                    {
                        ret[ii,jj] = ((uint)ReadByte() << 16) | ((uint)ReadByte() << 8) | (uint)ReadByte();
                    }
                }
            }
            return ret;
        }

        public uint[,] ReadUInt32(int Dimension0, int Dimension1)
        {
            uint[,] ret = new uint[Dimension0, Dimension1];
            if (LittleEndian)
            {
                for (int ii = 0; ii < Dimension0; ii++)
                {
                    for (int jj = 0; jj < Dimension1; jj++)
                    {
                        ret[ii, jj] = (uint)ReadByte() | ((uint)ReadByte() << 8) | ((uint)ReadByte() << 16) | ((uint)ReadByte() << 24);
                    }
                }
            }
            else
            {
                for (int ii = 0; ii < Dimension0; ii++)
                {
                    for (int jj = 0; jj < Dimension1; jj++)
                    {
                        ret[ii, jj] = ((uint)ReadByte() << 24) | ((uint)ReadByte() << 16) | ((uint)ReadByte() << 8) | (uint)ReadByte();
                    }
                }
            }
            return ret;
        }

        public ulong[,] ReadUInt64(int Dimension0, int Dimension1)
        {
            ulong[,] ret = new ulong[Dimension0, Dimension1];
            if (LittleEndian)
            {
                for (int ii = 0; ii < Dimension0; ii++)
                {
                    for (int jj = 0; jj < Dimension1; jj++)
                    {
                        ret[ii, jj] = (ulong)ReadByte() | ((ulong)ReadByte() << 8) | ((ulong)ReadByte() << 16) | ((ulong)ReadByte() << 24)
                            | ((ulong)ReadByte() << 32) | ((ulong)ReadByte() << 40) | ((ulong)ReadByte() << 48) | ((ulong)ReadByte() << 56);
                    }
                }
            }
            else
            {
                for (int ii = 0; ii < Dimension0; ii++)
                {
                    for (int jj = 0; jj < Dimension1; jj++)
                    {
                        ret[ii, jj] = ((ulong)ReadByte() << 56) | ((ulong)ReadByte() << 48) | ((ulong)ReadByte() << 40) | ((ulong)ReadByte() << 32)
                            | ((ulong)ReadByte() << 24) | ((ulong)ReadByte() << 16) | ((ulong)ReadByte() << 8) | (ulong)ReadByte();
                    }
                }
            }
            return ret;
        }

        public sbyte[,] ReadSByte(int Dimension0, int Dimension1)
        {
            sbyte[,] ret = new sbyte[Dimension0, Dimension1];
            for (int ii = 0; ii < Dimension0; ii++) 
                for (int jj = 0; jj < Dimension1; jj++)                    
                    ret[ii, jj] = (sbyte)ReadByte();
            return ret;
        }

        public short[,] ReadInt16(int Dimension0, int Dimension1)
        {
            short[,] ret = new short[Dimension0, Dimension1];
            if (LittleEndian)
            {
                for (int ii = 0; ii < Dimension0; ii++)
                {
                    for (int jj = 0; jj < Dimension1; jj++)
                    {
                        ret[ii, jj] = (short)((ushort)ReadByte() | ((ushort)ReadByte() << 8));
                    }
                }
            }
            else
            {
                for (int ii = 0; ii < Dimension0; ii++)
                {
                    for (int jj = 0; jj < Dimension1; jj++)
                    {
                        ret[ii, jj] = (short)(((ushort)ReadByte() << 8) | (ushort)ReadByte());
                    }
                }
            }
            return ret;
        }

        /// <summary>
        /// ReadInt24(Count) reads a signed 24-bit array from the stream.  The
        /// values are sign-extended to be properly represented in the 32-bit
        /// return value.
        /// </summary>
        /// <returns>A 32-bit representation of the 24-bit stream array</returns>
        public int[,] ReadInt24(int Dimension0, int Dimension1)
        {
            int[,] ret = new int[Dimension0, Dimension1];
            const uint mask = 0x800000U;
            const uint extendmask = 0xFF000000U;
            uint tmp;
            if (LittleEndian)
            {
                for (int ii = 0; ii < Dimension0; ii++)
                {
                    for (int jj = 0; jj < Dimension1; jj++)
                    {
                        tmp = (uint)ReadByte() | ((uint)ReadByte() << 8) | ((uint)ReadByte() << 16);
                        if ((tmp & mask) != 0) ret[ii, jj] = (int)(tmp | extendmask);
                        else ret[ii, jj] = (int)tmp;
                    }
                }
            }
            else
            {
                for (int ii = 0; ii < Dimension0; ii++)
                {
                    for (int jj = 0; jj < Dimension1; jj++)
                    {
                        tmp = ((uint)ReadByte() << 16) | ((uint)ReadByte() << 8) | (uint)ReadByte();
                        if ((tmp & mask) != 0) ret[ii, jj] = (int)(tmp | extendmask);
                        else ret[ii, jj] = (int)tmp;
                    }
                }
            }
            return ret;
        }

        public int[,] ReadInt32(int Dimension0, int Dimension1)
        {
            int[,] ret = new int[Dimension0, Dimension1];
            if (LittleEndian)
            {
                for (int ii = 0; ii < Dimension0; ii++)
                {
                    for (int jj = 0; jj < Dimension1; jj++)
                    {
                        ret[ii, jj] = (int)((uint)ReadByte() | ((uint)ReadByte() << 8) | ((uint)ReadByte() << 16) | ((uint)ReadByte() << 24));
                    }
                }
            }
            else
            {
                for (int ii = 0; ii < Dimension0; ii++)
                {
                    for (int jj = 0; jj < Dimension1; jj++)
                    {
                        ret[ii, jj] = (int)(((uint)ReadByte() << 24) | ((uint)ReadByte() << 16) | ((uint)ReadByte() << 8) | (uint)ReadByte());
                    }
                }
            }
            return ret;
        }

        public long[,] ReadInt64(int Dimension0, int Dimension1)
        {
            long[,] ret = new long[Dimension0, Dimension1];
            if (LittleEndian)
            {
                for (int ii = 0; ii < Dimension0; ii++)
                {
                    for (int jj = 0; jj < Dimension1; jj++)
                    {
                        ret[ii, jj] = (long)((ulong)ReadByte() | ((ulong)ReadByte() << 8) | ((ulong)ReadByte() << 16) | ((ulong)ReadByte() << 24)
                            | ((ulong)ReadByte() << 32) | ((ulong)ReadByte() << 40) | ((ulong)ReadByte() << 48) | ((ulong)ReadByte() << 56));
                    }
                }
            }
            else
            {
                for (int ii = 0; ii < Dimension0; ii++)
                {
                    for (int jj = 0; jj < Dimension1; jj++)
                    {
                        ret[ii, jj] = (long)(((ulong)ReadByte() << 56) | ((ulong)ReadByte() << 48) | ((ulong)ReadByte() << 40) | ((ulong)ReadByte() << 32)
                            | ((ulong)ReadByte() << 24) | ((ulong)ReadByte() << 16) | ((ulong)ReadByte() << 8) | (ulong)ReadByte());
                    }
                }
            }
            return ret;
        }

        public float[,] ReadSingle(int Dimension0, int Dimension1)
        {
            // See scalar overload for comments.
            float[,] ret = new float[Dimension0, Dimension1];
            if (LittleEndian == BitConverter.IsLittleEndian)
            {                
                for (int ii = 0; ii < Dimension0; ii++)
                {
                    byte[] raw = ReadByte(Dimension1 * 4);
                    for (int kk = 0, jj = 0; kk < raw.Length; kk += 4, jj++) ret[ii, jj] = BitConverter.ToSingle(raw, kk);
                }
                return ret;
            }
            else
            {
                byte[] raw = new byte[4];
                for (int ii = 0; ii < Dimension0; ii++)
                {
                    for (int jj = 0; jj < Dimension1; jj++)
                    {
                        for (int kk = 3; kk >= 0; kk--) raw[kk] = ReadByte();
                        ret[ii, jj] = BitConverter.ToSingle(raw, 0);
                    }
                }
                return ret;
            }
        }

        public double[,] ReadDouble(int Dimension0, int Dimension1)
        {
            double[,] ret = new double[Dimension0, Dimension1];
            if (LittleEndian == BitConverter.IsLittleEndian)
            {
                for (int ii = 0; ii < Dimension0; ii++)
                {
                    byte[] raw = ReadByte(Dimension1 * 8);
                    for (int kk = 0, jj = 0; kk < raw.Length; kk += 8, jj++) ret[ii, jj] = BitConverter.ToDouble(raw, kk);
                }
                return ret;
            }
            else
            {
                byte[] raw = new byte[8];
                for (int ii = 0; ii < Dimension0; ii++)
                {
                    for (int jj = 0; jj < Dimension1; jj++)
                    {
                        for (int kk = 7; kk >= 0; kk--) raw[kk] = ReadByte();
                        ret[ii, jj] = BitConverter.ToDouble(raw, 0);
                    }
                }
                return ret;
            }
        }

        public decimal[,] ReadDecimal(long Dimension0, long Dimension1)
        {
            throw new NotImplementedException();            // TODO
        }

        #endregion
    }
}
