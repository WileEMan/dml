using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace WileyBlack.Utility
{
    public class NullStream : Stream
    {
        public override bool CanRead { get { return false; } }
        public override bool CanSeek { get { return true; } }
        public override bool CanWrite { get { return true; } }
        public override bool CanTimeout { get { return false; } }

        public override long Length
        {
            get { return 0; }
        }

        public override long Position
        {
            get
            {
                return 0;
            }
            set
            {
            }
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
        }

        public override void WriteByte(byte value)
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        public override int ReadByte()
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return 0;
        }

        public override void Flush()
        {            
        }
    }
}
