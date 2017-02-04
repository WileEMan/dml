/***
    Copyright (c) 2012 by Wiley Black
    All rights reserved.

    This source code is made available under a custom license that I’m calling Open/Reference.  It is similar 
    to a LGPL with the following major exceptions:
        o	Binary-only uses do not require acknowledgment or credit (it is appreciated, however).
        o	Tivoization is permitted as long as it does not violate other rights herein.
        o	There are limitations on distribution of modified source code designed to protect the original 
            author and to encourage changes to be routed through the original author.

    Redistribution and use in binary forms, with or without modification, are permitted provided that the 
    following conditions are met:
        o   Agreement is accepted that omitting acknowledgment or copyright mention cannot be construed as a 
            valid claim to the rights or ownership protected herein.

    Public redistribution in source form with modification is not permitted.  

    Distribution and use in source form with modification within an organization and its immediate identified 
    partners are permitted provided the following conditions are met.  Distribution and use in source form with 
    modification is permitted between recognized private partners provided the following conditions are met.  
    Public or private redistribution and use in source form without modification is permitted provided the following 
    conditions are met.    
        o   Redistributions of source code, with or without modification, must retain the above copyright notice, 
            this license, and the following disclaimer.
        o   Modified source code must contain at least a one line prefix or postfix to this notice stating that it is 
            a derived work and providing an Internet resource where the original may be obtained.  For example, 
            “Derived from the original…” and a URL is sufficient.

    Integration in larger works is considered a “use” and not a modification as long as no changes to the source were 
    made.  Binary forms include compiled forms such as object representation (including that suitable for static linkage) 
    and intermediate language representations.

    Relicensing (such as bundling) of modified source and any binary forms are permitted but requires equal or more 
    restrictive license terms and may not reassign ownership or rights of this source code.

    THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, 
    INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE 
    DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, 
    SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR 
    SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, 
    WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE 
    USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.  
***/

using System;
using System.Data;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Diagnostics;

namespace WileyBlack.Platforms
{
    /// <summary>
    /// LSBBitStream provides a Stream which also supports bitwise read operations.  That is,
    /// individual bits can be read from the stream, or segments less than 8-bits in width.
    /// The LSBBitStream provides a small buffer which facilitates this, since underlying
    /// stream access must always be bytewise.  
    /// 
    /// The LSBBitStream class supports reading only in LSB-first-order.  That is, the first 
    /// bit read is taken from the least-significant of the 8-bits of the first byte.  This
    /// is the directionality used by DEFLATE, but may not always be the case.  Note that
    /// this is not the same as endianness, which specifies the sequencing of bytes within
    /// a multibyte value.
    /// 
    /// The LSBBitStream class provides read-support only at this time.  A writable implementation
    /// is possible but is not provided.
    /// </summary>
    public class LSBBitStream : Stream
    {
        public Stream BaseStream;        

        public LSBBitStream(Stream BaseStream)
        {
            this.BaseStream = BaseStream;
        }
         
        public override long Length { get { throw new NotSupportedException(); } }
        public override bool CanRead { get { return true; } }
        public override bool CanWrite { get { return false; } }
        public override bool CanSeek { get { return false; } }
        public override bool CanTimeout { get { return false; } }

        public override void Write(byte[] buffer, int offset, int count) { throw new NotImplementedException(); }
        public override void WriteByte(byte value) { throw new NotImplementedException(); }

        public override long Position
        {
            get { throw new NotSupportedException(); }
            set { throw new NotSupportedException(); }
        }

        public override long Seek(long offset, SeekOrigin origin) { throw new NotSupportedException(); }
        public override void SetLength(long value) { throw new NotSupportedException(); }        

        public override void Close() { BaseStream = null; }
        public override void Flush() { BaseStream.Flush(); }

        public override int Read(byte[] buffer, int offset, int count) 
        {
            if (count == 0) return 0;

            if (BitsInBuffer != 0)
                throw new Exception("ReadByte() can only be used on byte-aligned boundaries.");

            for (int nRead = 0; nRead < count; nRead++)
            {
                int nByte = BaseStream.ReadByte();
                if (nByte < 0) return nRead;
                buffer[offset++] = (byte)nByte;
            }
            return count;
        }

        public int BitsInBuffer = 0;
        public uint BitBuffer = 0;

        public uint ReadBits(int nBits)
        {
            if (nBits == 0) return 0;

            // Make sure enough bits are loaded...
            while (BitsInBuffer < nBits)
            {
                int ReadNext = BaseStream.ReadByte();
                if (ReadNext < 0) throw new EndOfStreamException();
                BitBuffer = BitBuffer | (uint)((uint)ReadNext) << BitsInBuffer;
                BitsInBuffer += 8;
            }

            // Build a mask for the requested bits...
            uint mask = (1U << nBits) - 1;

            // Unload bits from buffer...
            uint ret = BitBuffer & mask;
            BitBuffer >>= nBits;
            BitsInBuffer -= nBits;
            return ret;
        }

        public void FlushCurrentByte() { BitsInBuffer = 0; }

        public override int ReadByte()
        {
 	        if (BitsInBuffer == 8)
            {
                BitsInBuffer = 0;
                return (byte)BitBuffer;
            }

            if (BitsInBuffer == 0)
            {
                return BaseStream.ReadByte();                
            }
            
            throw new Exception("ReadByte() can only be used on byte-aligned boundaries.");
        }

        public byte ReadByteOrThrow()
        {
            if (BitsInBuffer == 8)
            {
                BitsInBuffer = 0;
                return (byte)BitBuffer;
            }

            if (BitsInBuffer == 0)
            {
                int ret = BaseStream.ReadByte();
                if (ret < 0) throw new EndOfStreamException();
                return (byte)ret;
            }

            throw new Exception("ReadByte() can only be used on byte-aligned boundaries.");
        }
    }

    /// <summary>
    /// The HistoryRingBuffer class is used by DeflateStreamEx in order to accomodate the
    /// history buffer which is required for DEFLATE unpacking.  The buffer provides
    /// a fixed-size memory window of the most recent unpacked data.  
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal class HistoryRingBuffer<T>
    {
        T[] Buffer;
        long iHead;             // Data is read from the head, and then removed from the ring.
        long iTail;			    // Data is added at the tail, and the length increases.

        public HistoryRingBuffer(int nElements)
        {
            Buffer = new T[nElements+1];        // One entry is lost by the case where iHead = iTail.
            iHead = iTail = 0;
        }

        public long Length
        {
            get
            {
                if (iTail < iHead) return (Buffer.Length - iHead) + iTail;		// Wraps-around
	            else return iTail - iHead;									    // Linear or empty
            }
        }

        public void Add(T obj)
        {
            Buffer[iTail++] = obj;
		    if (iTail >= Buffer.Length) iTail = 0;
            if (iTail == iHead) 
            {
                // This happens when the buffer is full and we have just overwritten the
                // oldest entry.
                iHead++;
                if (iHead >= Buffer.Length) iHead = 0;
            }
        }

        public void Add(T[] obj, int offset, int count)
        {
            // TODO: Optimization possible here.
            while (count > 0) { Add(obj[offset++]); count--; }            
        }

        /// <summary>
        /// GetHistory() retrieves the Nth-from-last element in the ring buffer.  For example,
        /// calling Add(1), Add(2), and Add(3) on a new buffer results in the buffer containing
        /// the entries 1, 2, and 3.  Calling GetHistory(1) returns 3.  Calling GetHistory(3)
        /// returns 1.  Calling GetHistory(0) or with a distance greater than the buffer's
        /// length result in an exception.
        /// </summary>
        /// <param name="nDistance">Distance in history of element to retrieve.  Must be greater
        /// than zero and less than or equal to Length.</param>
        /// <returns>The value of the element.</returns>
        public T GetHistory(long nDistance)
        {
            if (nDistance <= 0 || nDistance > Length) throw new ArgumentOutOfRangeException();            
            long ii = iTail - nDistance;
            if (ii < 0) ii += Buffer.Length;
            return Buffer[ii];
        }        
    }

    /// <summary>
    /// DeflateStreamEx decompresses a stream which was compressed using the
    /// DEFLATE algorithm.  It is very similar to the .NET built-in DeflateStream
    /// class except that it only performs decompression.  The built-in DeflateStream
    /// class will read to the end of a stream whenever used and will not indicate
    /// the location where reading ended.  The DeflateStreamEx class does not have
    /// this limitation and can be used with DML.  Significant optimization is
    /// possible and needed for the DeflateStreamEx class.
    /// </summary>
    public class DeflateStreamEx : Stream
    {
        #region "Stream Interface"

        LSBBitStream BitStream;        

        public DeflateStreamEx(Stream BaseStream)
        {
            this.BitStream = new LSBBitStream(BaseStream);
        }
         
        public override long Length { get { throw new NotSupportedException(); } }
        public override bool CanRead { get { return true; } }
        public override bool CanWrite { get { return false; } }
        public override bool CanSeek { get { return false; } }
        public override bool CanTimeout { get { return false; } }

        public override void Write(byte[] buffer, int offset, int count) { throw new NotSupportedException(); }
        public override void WriteByte(byte value) { throw new NotSupportedException(); }

        public override long Position
        {
            get { throw new NotSupportedException(); }
            set { throw new NotSupportedException(); }
        }

        public override long Seek(long offset, SeekOrigin origin) { throw new NotSupportedException(); }
        public override void SetLength(long value) { throw new NotSupportedException(); }        

        public override void Close() { BitStream = null; }
        public override void Flush() { BitStream.Flush(); }        

        #endregion

        enum BlockType
        {
            None,
            Noncompressed,
            FixedHuffman,
            DynamicHuffman
        }

        BlockType CurrentBlockType = BlockType.None;
        bool FinalBlock = false;
        bool EOS = false;

        /// <summary>
        /// The DEFLATE compression technique supports distance codes whereby
        /// an instruction tells the decoder to look back in history and repeat
        /// something that was previously decompressed.  The HistoryRingBuffer
        /// facilitates this.  History in DEFLATE can cross block boundaries,
        /// but the largest distance code allowed is 32768.
        /// </summary>
        HistoryRingBuffer<byte> History = new HistoryRingBuffer<byte>(32768);
        
        const int MAXBITS = 15;              /* maximum bits in a code */        

        public override int Read(byte[] buffer, int offset, int count)
        {
            for (int nRead = 0; nRead < count; )
            {
                switch (CurrentBlockType)
                {
                    case BlockType.Noncompressed:
                        {
                            int nBlockRead = ReadNCByte(buffer, offset, count - nRead);                            
                            nRead += nBlockRead;
                            offset += nBlockRead;
                            continue;
                        }

                    default:
                        int nByte = ReadByte();
                        if (nByte < 0) return nRead;
                        buffer[offset++] = (byte)nByte;
                        nRead++;
                        continue;
                }                
            }
            return count;
        }

        public override int ReadByte() 
        {
            int ret;
            for(;;)
            {
                switch (CurrentBlockType)
                {
                    case BlockType.None: 
                        if (EOS) return -1;
                        StartNextBlock();
                        continue;

                    case BlockType.Noncompressed:                        
                        ret = ReadNCByte();     // ReadNCByte() returns only a byte, EOS is unexpected.                        
                        return ret;

                    case BlockType.FixedHuffman:
                    case BlockType.DynamicHuffman:
                        ret = ReadCompressedByte();
                        if (ret < 0) continue;                        
                        return ret;

                    default: throw new NotSupportedException("Illegal block type.");
                }
            }            
        }

        void StartNextBlock()
        {
            FinalBlock = (BitStream.ReadBits(1) != 0);
            uint BTYPE = BitStream.ReadBits(2);

            switch (BTYPE)
            {
                case 0:
                    CurrentBlockType = BlockType.Noncompressed;
                    StartNoncompressedBlock();
                    break;

                case 1:
                    CurrentBlockType = BlockType.FixedHuffman;
                    StartFixedHuffmanBlock();
                    break;

                case 2:
                    CurrentBlockType = BlockType.DynamicHuffman;
                    StartDynamicHuffmanBlock();
                    break;

                default: throw new FormatException("Unsupported block type within DEFLATE stream.");
            }
        }

        #region "Non-Compressed Blocks"

        int NCBlockRemaining = 0;

        void StartNoncompressedBlock()
        {
            BitStream.FlushCurrentByte();

            NCBlockRemaining = ((int)BitStream.ReadByteOrThrow()) | ((int)BitStream.ReadByteOrThrow() << 8);
            int ComplBlockLength = ((int)BitStream.ReadByteOrThrow()) | ((int)BitStream.ReadByteOrThrow() << 8);
            if (NCBlockRemaining != (~ComplBlockLength & 0xffff))
                throw new FormatException("Corrupt or invalid block length in non-compressed block.");
        }

        byte ReadNCByte()
        {
            if (NCBlockRemaining > 1)
            {
                NCBlockRemaining--;
                byte ret = BitStream.ReadByteOrThrow();
                History.Add(ret); 
                return ret;
            }

            if (NCBlockRemaining == 1)
            {
                byte ret = BitStream.ReadByteOrThrow();
                History.Add(ret);

                NCBlockRemaining = 0;
                CurrentBlockType = BlockType.None;
                if (FinalBlock) EOS = true;
                
                return ret;
            }

            throw new NotSupportedException();
        }

        int ReadNCByte(byte[] buffer, int offset, int count)
        {
            int start_offset = offset;

            if (NCBlockRemaining > count)
            {
                for (int ii = 0; ii < count; ii++) buffer[offset++] = BitStream.ReadByteOrThrow();
                History.Add(buffer, start_offset, count);
                NCBlockRemaining -= count;
                return count;
            }

            int nBytes;
            if (NCBlockRemaining == count) nBytes = count;
            else nBytes = NCBlockRemaining;
            
            for (int ii = 0; ii < nBytes; ii++) buffer[offset++] = BitStream.ReadByteOrThrow();
            History.Add(buffer, start_offset, nBytes);
            NCBlockRemaining = 0;
            CurrentBlockType = BlockType.None;
            if (FinalBlock) EOS = true;
            return nBytes;
        }

        #endregion        

        #region "Huffman Table Decoding"

        public class Huffman
        {
            /// <summary>
            /// The CLCount array gives the number of codes sharing a common bit-length.  For example,
            /// CLCount[3] gives the number of symbols which are represented by 3-bit codes.  CLCount[0]
            /// gives the number of symbols which have no code.
            /// </summary>
            public uint[] CLCount;

            /// <summary>
            /// UnusedSymbols gives the number of symbols which have no code.  These symbols do not
            /// appear in the decompressed data and therefore do not require a code representation.
            /// </summary>
            public int UnusedSymbols;

            /// <summary>
            /// UnusedCodes gives the number of codes which are not assigned to a symbol.
            /// </summary>
            public int UnusedCodes;                        

            protected Huffman()
            {
            }
            
            public Huffman(uint[] CodeLength)
            {
                Init(CodeLength);
            }

            int MinCodeLength = 1;          // Smallest code length, in bits.
            uint[] FirstCode;              // Given a code length (bits, index), give the first code defined at that length.  Inclusive.
            uint[] LastCode;               // Given a code length (bits, index), give the last code defined at that length.  Exclusive.
            uint[] SymbolTable;            // Given a code (index), return a symbol

            /// <summary>
            /// Performs the transformation described in RFC 1951 Section 3.2.2.  
            /// </summary>
            /// <param name="CodeLength">The length, in bits, of each code.  A zero value indicates
            /// a symbol which will not be used.  The indices of this array correspond to the
            /// symbols being represented.</param>
            /// <returns></returns>
            public void Init(uint[] CodeLength)
            {
                /** Example, from RFC 1951:
                 *  Consider the alphabet ABCDEFGH, with bit lengths (3, 3, 3, 3, 3, 2, 4, 4).
                 */

                // 1. Count the number of codes for each code length. Let bl_count[bl] be the number of codes of length bl, bl >= 1.
                CLCount = new uint[16];
                for (int symbol = 0; symbol < CodeLength.Length; symbol++) CLCount[CodeLength[symbol]]++;
                
                UnusedSymbols = (int)CLCount[0];
                CLCount[0] = 0;

                // Unused symbols have a CodeLength of zero, which will add to bl_count[0].  If they all
                // are unused we have a problem...
                if (CLCount[0] == CodeLength.Length) throw new Exception("No symbols used!");

                /** After step 1, we have:
                 *  N           = {   ..., 2,      3,      4, ... }
                 *  bl_count[N] = { 0, 0,  1,      5,      2, ... }     (a.k.a. CLCount)
                 */

                // 2) Find the numerical value of the smallest code for each code length:
                uint[] next_code = new uint[16];
                uint code = 0;
                for (int bits = 1; bits < 16; bits++)
                {
                    code = (code + CLCount[bits - 1]) << 1;
                    next_code[bits] = code;
                }

                /** Step 2 computes the following next_code values:
                 *  N               = { 0,  1,  2,  3,  4, ... }
                 *  next_code[N]    = { 0,  0,  0,  2, 14, ... }
                 */

                // 3) Assign numerical values to all codes, using consecutive values for all codes of the same length 
                // with the base values determined at step 2. Codes that are never used (which have a bit length of 
                // zero) must not be assigned a value.
                
                /** When decoding, our first task will be to identify to bit length.  Since codes
                 *  are all consecutive for the same bit length and a code never repeats as a
                 *  prefix in a longer code, this can be accomplished by checking the range as
                 *  we read in the sequence. **/
                FirstCode = new uint[16];
                LastCode = new uint[16];
                uint FinalCodeP1 = 0;
                for (int bits = 1; bits < 16; bits++)
                {
                    FirstCode[bits] = (uint)next_code[bits];
                    LastCode[bits] = (uint)next_code[bits] + (uint)CLCount[bits];
                    if (CLCount[bits] > 0) FinalCodeP1 = LastCode[bits];
                }

                for (int bits = 15; bits > 0; bits--)
                {
                    if (CLCount[bits] > 0) MinCodeLength = bits;
                }
                
                /** Step 3+ **/

#               if false
                Debug.Write("Huffman next_code[] table:\n");
                for (int ii = 0; ii < next_code.Length; ii++)
                {
                    if (CLCount[ii] > 0)
                        Debug.Write(string.Format("\t[{0}] {1} (Count={2})\n", ii, next_code[ii], CLCount[ii]));
                    else
                        Debug.Write(string.Format("\t[{0}] {1}\n", ii, next_code[ii]));
                }
                Debug.Write("End table.\n");
#               endif
                
                /** When decoding and after having identified the bit length, the code comes
                 *  easy.  Once we have the code, we need the symbol.  The Symbol array will
                 *  give us a symbol given a code. **/
                SymbolTable = new uint[FinalCodeP1];
                code = 0;
                for (uint symbol = 0; symbol < CodeLength.Length; symbol++)
                {
                    if (CodeLength[symbol] > 0)
                    {
                        code = next_code[CodeLength[symbol]]++;
                        SymbolTable[code] = symbol;
                    }
                }

#               if false
                Debug.Write("Huffman CLCount[] table:\n");
                for (int ii = 0; ii < this.CLCount.Length; ii++) Debug.Write(string.Format("\t[{0}] {1}\n", ii, this.CLCount[ii]));
                Debug.Write("End table.\n");
#               endif

                /** Step 3 produces the following code values:
                    Symbol Length   Code
                    ------ ------   ----
                    A       3        010 (2)
                    B       3        011 (3)
                    C       3        100 (4)
                    D       3        101 (5)
                    E       3        110 (6)
                    F       2         00 (0)
                    G       4       1110 (14)
                    H       4       1111 (15)
                  
                    We've also used the codes to build up the SymbolTable.
                 */
            }

            public uint Decode(LSBBitStream Src)
            {
                // TODO: Optimization!  This function is by far the most time-critical function in
                // all of DeflateStreamEx.

                uint code = 0;
                for (int bits = 0; bits < MinCodeLength; bits++)
                {
                    code <<= 1;
                    code |= Src.ReadBits(1);
                }
                
                for (int bits = MinCodeLength; bits < 16; bits++)
                {
                    // We do not yet know if the code is complete or if we need
                    // to read more bits...to find out, we check the valid range
                    // of codes at this bit length...
                    if (code < LastCode[bits] && code >= FirstCode[bits])
                    {
                        // The posited code falls within a valid range of codes at
                        // this length.  Since a utilized code can never occur as a 
                        // prefix to a longer code (by the rules), we have located
                        // the code.  Now return the symbol.
                        return SymbolTable[code];
                    }

                    // The code is incomplete - we need more bits!
                    code <<= 1;

                    // Make sure enough bits are loaded...
                    if (Src.BitsInBuffer == 0)
                    {
                        int ReadNext = Src.BaseStream.ReadByte();
                        if (ReadNext < 0) throw new EndOfStreamException();
                        Src.BitBuffer = (byte)ReadNext;
                        Src.BitsInBuffer = 8;
                    }                    

                    // Unload bits from buffer...
                    code |= Src.BitBuffer & 1;                    
                    Src.BitBuffer >>= 1;
                    Src.BitsInBuffer --;
                }

                throw new Exception("Compressed stream corrupt - an unassigned or invalid code was found within the stream.");
            }
        }

        Huffman CurrentLengthCodes;
        Huffman CurrentDistanceCodes;

        #endregion

        #region "Fixed Huffman Blocks - Setup"

        public class FixedLengthHuffman : Huffman
        {
            public FixedLengthHuffman()
            {
                uint[] CodeLength = new uint [288];

                int symbol = 0;
                for (; symbol <= 143; symbol++) CodeLength[symbol] = 8;
                for (; symbol <= 255; symbol++) CodeLength[symbol] = 9;
                for (; symbol <= 279; symbol++) CodeLength[symbol] = 7;
                for (; symbol <= 287; symbol++) CodeLength[symbol] = 8;
                Init(CodeLength);
            }
        }

        public class FixedDistanceHuffman : Huffman
        {
            public FixedDistanceHuffman()
            {
                uint[] CodeLength = new uint [32];

                for (int symbol = 0; symbol < CodeLength.Length; symbol++) CodeLength[symbol] = 5;
                Init(CodeLength);
            }
        }

        static FixedLengthHuffman FixedLengthCodes = new FixedLengthHuffman();
        static FixedDistanceHuffman FixedDistanceCodes = new FixedDistanceHuffman();

        void StartFixedHuffmanBlock()
        {
            CurrentLengthCodes = FixedLengthCodes;
            CurrentDistanceCodes = FixedDistanceCodes;
        }

        #endregion

        #region "Dynamic Huffman Blocks - Start"

        static int[] HeaderCLSequence = { 16, 17, 18, 0, 8, 7, 9, 6, 10, 5, 11, 4, 12, 3, 13, 2, 14, 1, 15 };
        Huffman ReadHeaderCodes(int HCLEN)
        {
            uint[] CodeLength = new uint [HeaderCLSequence.Length];
            for (int ii = 0; ii < HCLEN; ii++) CodeLength[HeaderCLSequence[ii]] = BitStream.ReadBits(3);
            return new Huffman(CodeLength);
        }

        void StartDynamicHuffmanBlock()
        {
            // The dynamic huffman block consists of a custom literal/length alphabet and a custom distance
            // alphabet.  These alphabets are applied to the block's compressed content.  The alphabets
            // themselves are stored using a Huffman encoding which we will call the 'Header' alphabet.
            
            uint HLIT = BitStream.ReadBits(5) + 257;        // # of Literal/Length codes - 257 (257 - 286)
            uint HDIST = BitStream.ReadBits(5) + 1;         // # of Distance codes - 1 (1 - 32)
            if (HLIT > 286 || HDIST > 32) throw new FormatException("Compressed data corrupt: Invalid header values in dynamic block.");

            uint HCLEN = BitStream.ReadBits(4) + 4;         // # of Code Length codes - 4 (4 - 19)
            Huffman HeaderCodes = ReadHeaderCodes((int)HCLEN);

            /** Read dynamic literal/length and distance tables **/

            /**
             * The code length repeat codes can cross from the literal/length 
             * alphabet block to the distance alphabet block.  Thus we need
             * to read in all the code lengths for the two alphabets in one
             * operation, then we can divy them out into the two alphabets.
             */
            
            uint[] DynCodeLength = new uint[HLIT + HDIST];

            uint Repeats = 0;
            uint PrevCodeLength = 0;             
            for (int ii = 0; ii < HLIT + HDIST; )
            {
                if (Repeats > 0)
                {
                    DynCodeLength[ii++] = PrevCodeLength;
                    Repeats--;
                    continue;
                }

                uint symbol = HeaderCodes.Decode(BitStream);

                if (symbol < 16)                // 0..15: Literal value of the code length
                {
                    DynCodeLength[ii++] = symbol;
                    PrevCodeLength = symbol;
                }
                else if (symbol == 16)          // 16: Repeat last symbol N times, where N is 3..6
                {
                    if (ii == 0) throw new FormatException("Compressed stream corrupt: Header used symbol repeat on first value.");
                    Repeats = BitStream.ReadBits(2) + 3;
                }
                else if (symbol == 17)          // 17: Repeat zero value N times, where N is 3..10
                {
                    PrevCodeLength = 0;
                    Repeats = BitStream.ReadBits(3) + 3;
                }
                else if (symbol == 18)          // 18: Repeat zero value N times, where N is 11..138
                {
                    PrevCodeLength = 0;
                    Repeats = BitStream.ReadBits(7) + 11;
                }
                else throw new NotSupportedException();
            }

            /** Split the dynamic code length list into the literal/length table and the distance table **/
            uint[] LitCodeLength = new uint[HLIT];
            uint[] DistCodeLength = new uint[HDIST];
            
            for (uint ii = 0; ii < HLIT; ii++) LitCodeLength[ii] = DynCodeLength[ii];
            for (uint jj = 0, ii = HLIT; jj < HDIST; ii++, jj++) DistCodeLength[jj] = DynCodeLength[ii];

            // Verify that the end of block code (256) is present...
            if (HLIT < 256 || LitCodeLength[256] == 0) throw new FormatException("Compression stream corrupt: Dynamic block did not contain a termination code.");

            CurrentLengthCodes = new Huffman(LitCodeLength);
            CurrentDistanceCodes = new Huffman(DistCodeLength);            
        }

        #endregion

        #region "Compressed block content"

        /// <summary>
        /// When more than one byte is decoded in a single operation, we have to store the
        /// extra bytes somewhere (assuming we are reading a byte at a time).  We already
        /// have to maintain 'History' in order to facilitate backward string copies that
        /// are a cornerstone of deflate's compression mechanism.  When QueuedHistory is
        /// greater than zero, it indicates a number of bytes which are stored in History 
        /// but have not yet been returned from the DeflateStreamEx.  QueuedHistory should 
        /// never exceed 258 bytes based on deflate rules.
        /// </summary>
        int QueuedHistory = 0;

        static uint[] RepeatLengthBase = 
            { 3, 4, 5, 6, 7, 8, 9, 10, 11, 13, 15, 17, 19, 23, 27, 31, 35, 43, 51, 59, 67, 83, 99, 115, 131, 163, 195, 227, 258};        
        static uint[] BackwardDistanceBase = 
            { 1, 2, 3, 4, 5, 7, 9, 13, 17, 25, 33, 49, 65, 97, 129, 193, 257, 385, 513, 769, 1025, 1537, 2049, 3073, 4097, 
                6145, 8193, 12289, 16385, 24577};
        static int[] RepeatLengthExtraBits =
            { 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 2, 2, 2, 2, 3, 3, 3, 3, 4, 4, 4, 4, 5, 5, 5, 5, 0 };
        static int[] BackwardDistanceExtraBits = 
            { 0, 0, 0, 0, 1, 1, 2, 2, 3, 3, 4, 4, 5, 5, 6, 6, 7, 7, 8, 8, 9, 9, 10, 10, 11, 11, 12, 12, 13, 13};

        int ReadCompressedByte()
        {
            if (QueuedHistory > 0) return History.GetHistory(QueuedHistory--);
            
            uint symbol = CurrentLengthCodes.Decode(BitStream);
            if (symbol < 256)
            {
                // Symbols 0..255:  The symbol is the literal content.
                History.Add((byte)symbol);
                return (int)symbol;
            }
            else if (symbol > 256)
            {
                // Symbols 257..285:  The symbol indicates a <length, backward distance> pair.
                //                    The symbol value gives part of the length information.

                if (symbol > 285) throw new FormatException("Compressed stream corrupt: An invalid lit/length symbol (greater than 285) was encountered.");
                symbol -= 257;
                uint RepeatLength = RepeatLengthBase[symbol] + BitStream.ReadBits(RepeatLengthExtraBits[symbol]);

                // Read distance symbol...
                symbol = CurrentDistanceCodes.Decode(BitStream);
                uint BackwardDistance = BackwardDistanceBase[symbol] + BitStream.ReadBits(BackwardDistanceExtraBits[symbol]);
                if (BackwardDistance > History.Length) throw new FormatException("Compressed stream corrupt: backward distance exceeded history.");

                // Add the repeated string to the history, but mark that part of the history is pending return to the
                // caller with QueuedHistory...
                for (int ii = 0; ii < RepeatLength; ii++) History.Add((byte)History.GetHistory(BackwardDistance));
                QueuedHistory += (int)RepeatLength;
                // Return the first byte from the queued history, and remove it from the queue.
                return History.GetHistory(QueuedHistory--);
            }
            else if (symbol == 256)
            {
                // Symbol 256:  End of block
                CurrentBlockType = BlockType.None;
                if (FinalBlock) EOS = true;
                return -1;          // Caller needs to check for another block, unless EOS is set.
            }

            throw new NotSupportedException();
        }

        #endregion
    }
}
