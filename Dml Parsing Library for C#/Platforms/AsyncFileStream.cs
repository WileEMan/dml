using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace WileyBlack.Dml.Platforms
{
    public class AsyncStream : Stream
    {
        private Stream BaseStream;

        public AsyncStream(Stream BaseStream)
        {
            if ((BaseStream is FileStream) && !((FileStream)BaseStream).IsAsync)
                throw new ArgumentException("Stream provided to an AsyncStream must be opened for asynchronous in order for AsyncStream to provide benefit.");
            this.BaseStream = BaseStream;
        }

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
            BaseStream.BeginWrite(buffer, offset, count, OnWriteComplete, null);
        }

        public override void WriteByte(byte value)
        {
        }

        private void OnWriteComplete(IAsyncResult Result)
        {
            if (Result.CompletedSynchronously)
            {
                Console.Write("Completed synchronously, callback notified.\n");
                return;
            }

            try
            {
                BaseStream.EndWrite(Result);
            }
            catch (Exception ex)
            {
                Console.Write("Error with write: " + ex.Message + "\n");
                return;
            }

            Console.Write("Completed asynchronously.\n");
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
