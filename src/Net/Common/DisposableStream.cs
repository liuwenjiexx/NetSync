using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Net
{

    internal class DisposableStream : Stream
    {
        private Stream baseStream;
        private bool disposable;

        public DisposableStream(Stream baseStream, bool baseDisposable)
        {
            this.baseStream = baseStream;
            this.disposable = baseDisposable;
        }

        public override bool CanRead => baseStream.CanRead;

        public override bool CanSeek => baseStream.CanSeek;

        public override bool CanWrite => baseStream.CanWrite;

        public override long Length => baseStream.Length;

        public override long Position { get => baseStream.Position; set => baseStream.Position = value; }

        public override void Flush()
        {
            baseStream.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return baseStream.Read(buffer, offset, count);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return baseStream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            baseStream.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            baseStream.Write(buffer, offset, count);
        }
        protected override void Dispose(bool disposing)
        {
            if (this.disposable)
            {
                baseStream.Dispose();
            }
        }
    }
}
