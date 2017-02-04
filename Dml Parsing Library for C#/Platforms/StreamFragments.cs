#if true

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
using System.IO;
using System.Text;
using System.Security.Cryptography;

namespace WileyBlack.Utility
{    
    /// <summary>
    /// StreamFragment is a stream which references a larger stream and provides streaming
    /// access to a subsection of that larger stream.  For instance, if a MemoryStream
    /// is used to represent 3 files in memory with known boundaries between the three files,
    /// then a StreamFragment could be used to provide a Stream that represents just one
    /// of the 3 files by setting it up to reference bytes N..M of the larger stream.
    /// </summary>
    public class StreamFragment : Stream
    {
        Stream BaseStream;

        long m_StartPos;        // Inclusive        
        long m_EndPos;          // Exclusive

        /// <summary>
        /// In case Position is not available, we still need to keep track of how many bytes have
        /// been consumed.  m_Position represents the current location within the StreamFragment,
        /// not the BaseStream's position.  m_Position is not used when BaseStream.CanSeek is true
        /// because the caller may have circumvented the StreamFragment's seek - it is better to
        /// use the Position call in this case.
        /// </summary>
        long m_Position;

        public override long Position
        {
            get
            {
                if (!BaseStream.CanSeek) throw new NotSupportedException();
                return BaseStream.Position - m_StartPos;
            }
            set
            {
                if (!BaseStream.CanSeek) throw new NotSupportedException();
                long DeltaPosition = (value - m_StartPos);
                BaseStream.Position = value - m_StartPos;
                m_Position = value;
                if (OnConsume != null) OnConsume(DeltaPosition);
            }
        }

        public override long Length { get { return m_EndPos - m_StartPos; } }

        /// <summary>
        /// Remaining returns the number of bytes remaining in the current stream
        /// fragment.  In the case of a stream which supports seeking, this 
        /// calculation includes any seeks which have been performed on the stream.
        /// If seeking operations on the base stream have moved the position out of
        /// the stream fragment, this value can be negative.
        /// </summary>
        public long Remaining
        {
            get
            {
                if (BaseStream.CanSeek) return m_EndPos - BaseStream.Position;
                return m_EndPos - m_Position;
            }
        }

        public delegate void ConsumptionHandler(long Bytes);
        public event ConsumptionHandler OnConsume;

        /// <summary>
        /// Constructs a StreamFragment object where the fragment begins at
        /// the current location within BaseStream and continues for FragmentLength
        /// bytes.
        /// </summary>
        /// <param name="BaseStream">The BaseStream from which the fragment stream will read.</param>
        /// <param name="FragmentLength">The length of the fragment within the base stream, in bytes.</param>
        public StreamFragment(Stream BaseStream, UInt64 FragmentLength)
        {
            this.BaseStream = BaseStream;
            if (BaseStream.CanSeek)
            {
                m_StartPos = BaseStream.Position;
                m_EndPos = m_StartPos + (long)FragmentLength;
            }
            else { m_StartPos = 0; m_EndPos = (long)FragmentLength; }
            m_Position = 0;
        }

        /// <summary>
        /// Constructs a StreamFragment object within a base stream.  This overload
        /// requires a BaseStream which supports seeking, and allows defining the
        /// fragment as arbitrary locations within the base stream.  
        /// </summary>
        /// <param name="BaseStream">The BaseStream from which the fragment stream will read.</param>
        /// <param name="FragmentOffset">The location of the first byte of the fragment within the BaseStream.</param>
        /// <param name="FragmentLength">The length of the fragment within the base stream, in bytes.</param>
        public StreamFragment(Stream BaseStream, UInt64 FragmentOffset, UInt64 FragmentLength)
        {
            this.BaseStream = BaseStream;
            if (!BaseStream.CanSeek) throw new ArgumentException("This overload requires a stream which supports seeking.");
            m_StartPos = (long)FragmentOffset;
            m_EndPos = m_StartPos + (long)FragmentLength;
            m_Position = BaseStream.Position - m_StartPos;
        }

        public override bool CanRead { get { return BaseStream.CanRead; } }
        public override bool CanWrite { get { return BaseStream.CanWrite; } }
        public override bool CanSeek { get { return BaseStream.CanSeek; } }
        public override bool CanTimeout { get { return BaseStream.CanTimeout; } }

        /// <summary>
        /// IsAtEnd returns true only if the current position of the stream is
        /// exactly past the end of stream.  In the case of a stream which supports
        /// seeking, this includes verifying that the base stream is positioned
        /// just past this fragment.
        /// </summary>
        public bool IsAtEnd
        {
            get
            {
                if (BaseStream.CanSeek) { return Position == m_EndPos; }
                else return m_Position == m_EndPos;
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (offset < 0) throw new ArgumentOutOfRangeException();
            if (count < 0) throw new ArgumentOutOfRangeException();

            if (BaseStream.CanSeek)
            {
                long BasePosition = (long)BaseStream.Position;
                if (BasePosition < m_StartPos) throw new IOException("Cannot read from stream fragment before start of fragment.");
                long BytesAvailable = m_EndPos - BasePosition;
                if (BytesAvailable < (long)count) count = (int)BytesAvailable;
                if (count <= 0) return 0;
                int nBytesRead = BaseStream.Read(buffer, offset, count);
                if (OnConsume != null) OnConsume(nBytesRead);
                return nBytesRead;
            }
            else
            {
                long BytesAvailable = m_EndPos - m_Position;
                if (BytesAvailable < (long)count) count = (int)BytesAvailable;
                if (count <= 0) return 0;
                int nBytesRead = BaseStream.Read(buffer, offset, count);
                m_Position += nBytesRead;
                if (OnConsume != null) OnConsume(nBytesRead);
                return nBytesRead;
            }
        }

        public override int ReadByte()
        {
            if (BaseStream.CanSeek)
            {
                long BasePosition = BaseStream.Position;
                if (BasePosition < m_StartPos) throw new IOException("Cannot read from stream fragment before start of fragment.");
                long BytesAvailable = m_EndPos - BasePosition;
                if (BytesAvailable < (long)1) return -1;
                if (OnConsume != null) OnConsume(1);
                return BaseStream.ReadByte();
            }
            else
            {
                long BytesAvailable = m_EndPos - m_Position;
                if (BytesAvailable < (long)1) return -1;                
                int ret = BaseStream.ReadByte();
                if (ret != -1) { 
                    m_Position++; 
                    if (OnConsume != null) OnConsume(1); 
                }
                return ret;
            }
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (offset < 0) throw new ArgumentOutOfRangeException();
            if (count < 0) throw new ArgumentOutOfRangeException();

            if (BaseStream.CanSeek)
            {
                long BasePosition = BaseStream.Position;
                if (BasePosition < m_StartPos) throw new IOException("Cannot write to stream fragment before start of fragment.");
                long BytesAllowed = m_EndPos - BasePosition;
                if ((long)count > BytesAllowed) throw new IOException("Attempted to write to stream fragment beyond end of fragment.");
                BaseStream.Write(buffer, offset, count);                
            }
            else
            {
                long BytesAllowed = m_EndPos - m_Position;
                if ((long)count > BytesAllowed) throw new IOException("Attempted to write to stream fragment beyond end of fragment.");
                BaseStream.Write(buffer, offset, count);
                m_Position += count;
            }
        }

        public override void WriteByte(byte value)
        {
            if (BaseStream.CanSeek)
            {
                long BasePosition = BaseStream.Position;
                if (BasePosition < m_StartPos) throw new IOException("Cannot write to stream fragment before start of fragment.");
                long BytesAllowed = m_EndPos - BasePosition;
                if (BytesAllowed < 1) throw new IOException("Attempted to write to stream fragment beyond end of fragment.");
                BaseStream.WriteByte(value);
            }
            else
            {
                long BytesAllowed = m_EndPos - m_Position;
                if (BytesAllowed < 1) throw new IOException("Attempted to write to stream fragment beyond end of fragment.");
                BaseStream.WriteByte(value);
                m_Position++;                
            }
        }

        public override void Flush() { BaseStream.Flush(); }
        public override long Seek(long offset, SeekOrigin origin) {
            long PrevPos = BaseStream.Position;
            long ret = BaseStream.Seek(offset, origin);
            long NewPos = BaseStream.Position;
            long DeltaPos = NewPos - PrevPos;
            if (OnConsume != null) OnConsume(DeltaPos);
            return ret;
        }
        public override void SetLength(long value)
        {
            throw new NotSupportedException("Length of a stream fragment must be defined at creation.");
        }
    }

    /// <summary>
    /// StreamWithHash provides a hash algorithm calculation on all data passing through
    /// a stream.  The StreamWithHash class prohibits seeking on the stream even if the
    /// base stream supports seeking, but reading and writing are supported if the base
    /// stream supports them.  Once all data is read or written from/to the stream, call
    /// Hash to retrieve the calculated hash code.  Calling Hash closes the stream if
    /// it is not already closed.  Closing or disposing the StreamWithHash does not
    /// affect the base stream.
    /// </summary>
    public class StreamWithHash : Stream
    {
        public Stream BaseStream;
        public HashAlgorithm Algorithm;

        public bool IsClosed { get { return BaseStream == null; } }

        public override long Length { get { return BaseStream.Length; } }        

        public int HashSize { get { return Algorithm.HashSize; } }

        /// <summary>
        /// Hash retrieves the final hash code for all data read or written from/to the
        /// stream.  Hash closes the stream if it is not already closed.
        /// </summary>
        public byte[] Hash
        {
            get
            {
                if (BaseStream != null) Close();
                return Algorithm.Hash;
            }
        }

        /// <summary>
        /// Constructs a StreamWithCRC32 object that will calculate the CRC-32
        /// from data read or written on the stream.
        /// </summary>
        /// <param name="BaseStream">The BaseStream from which the fragment stream will read.</param>        
        public StreamWithHash(Stream BaseStream, HashAlgorithm Algorithm)
        {
            this.BaseStream = BaseStream;
            this.Algorithm = Algorithm;

            if (Algorithm.InputBlockSize == 1) HashBuffer = new byte[4090];
            else HashBuffer = new byte[Algorithm.InputBlockSize];
        }

        public override bool CanRead { get { return BaseStream.CanRead; } }
        public override bool CanWrite { get { return BaseStream.CanWrite; } }
        public override bool CanSeek { get { return false; } }
        public override bool CanTimeout { get { return BaseStream.CanTimeout; } }

        byte[] HashBuffer;
        int Used = 0;

        public override void Close()
        {
            BaseStream = null;            
            Algorithm.TransformFinalBlock(HashBuffer, 0, Used);
        }

        public void FlushHash()
        {
            if (Used > 0)
            {
                Algorithm.TransformBlock(HashBuffer, 0, Used, HashBuffer, 0);
                Used = 0;
            }
        }

        public override void Flush()
        {
            BaseStream.Flush();
            FlushHash();
        }        

        void BufferBlock(byte[] buffer, int offset, int count)
        {
            if (Used + count > HashBuffer.Length)
            {
                if (Used < HashBuffer.Length)
                {
                    int Length = HashBuffer.Length - Used;
                    Array.Copy(buffer, offset, HashBuffer, Used, Length);
                    offset += Length;
                    count -= Length;
                    Used += Length;
                }

                FlushHash();
            }

            while (count > HashBuffer.Length)
            {
                Array.Copy(buffer, offset, HashBuffer, 0, HashBuffer.Length);
                offset += HashBuffer.Length;
                count -= HashBuffer.Length;
                Used = HashBuffer.Length;
                FlushHash();
            }
                                            
            Array.Copy(buffer, offset, HashBuffer, Used, count);
            Used += count;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int nBytesRead = BaseStream.Read(buffer, offset, count);
            BufferBlock(buffer, offset, nBytesRead);            
            return nBytesRead;
        }

        /// <summary>
        /// The ReadAll() is called in order to force the entire remaining stream to be
        /// read.  This must be done before the Hash can be retrieved.  If the user has
        /// already retrieved the entire stream, this function has no effect.
        /// </summary>
        public void ReadAll()
        {
            byte[] tmp = new byte[4090];
            for (; ; )
            {
                int nBytesRead = Read(tmp, 0, tmp.Length);
                if (nBytesRead < tmp.Length) return;
            }
        }

        public override int ReadByte()
        {                            
            int ret = BaseStream.ReadByte();
            if (ret != -1) {
                if (Used + 1 > HashBuffer.Length) FlushHash();
                HashBuffer[Used++] = (byte)ret;                
            }
            return ret;            
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            BufferBlock(buffer, offset, count);
            BaseStream.Write(buffer, offset, count);
        }

        public override void WriteByte(byte value)
        {
            if (Used + 1 > HashBuffer.Length) FlushHash();
            HashBuffer[Used++] = value;
            BaseStream.WriteByte(value);
        }

        public override long Position
        {
            get
            {
                throw new NotSupportedException();
            }
            set
            {
                throw new NotSupportedException();
            }
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }
    }
}

#endif