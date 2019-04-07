using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Net
{

    public class NetworkWriter : Stream
    {
        private MemoryStream msWriiter;
        private Stream baseStream;

        public NetworkWriter(Stream baseStream)
        {
            msWriiter = new MemoryStream(100);
            this.baseStream = baseStream;
        }
        public Stream BaseStream
        {
            get { return baseStream; }
        }
        public override bool CanRead
        {
            get { return baseStream.CanRead; }
        }

        public override bool CanSeek
        {
            get { return false; }
        }

        public override bool CanWrite
        {
            get { return baseStream.CanWrite; }
        }

        public override long Length => throw new NotImplementedException();

        public override long Position { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public void BeginWritePackage()
        {
            msWriiter.Seek(0, SeekOrigin.Begin);
            msWriiter.SetLength(0);
            WritePackageSize(0);
        }

        public void EndWritePackage()
        {
            ushort packageSize;
            msWriiter.Seek(0, SeekOrigin.Begin);
            packageSize = (ushort)(msWriiter.Length - GetPackageSizeBytesSize());
            WritePackageSize(packageSize);
            baseStream.Write(msWriiter.GetBuffer(), 0, (int)msWriiter.Length);
            msWriiter.Seek(0, SeekOrigin.Begin);
        }

        public override void Flush()
        {
            baseStream.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new Exception();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            msWriiter.Write(buffer, offset, count);
        }

        ushort GetPackageSizeBytesSize()
        {
            return 2;
        }

        private void WritePackageSize(ushort packageSize)
        {
            msWriiter.WriteByte((byte)((packageSize >> 8) & 0xFF));
            msWriiter.WriteByte((byte)(packageSize & 0xFF));
        }

    }




}
