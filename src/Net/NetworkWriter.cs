using System;
using System.IO;
using System.Text;

namespace Net
{

    public class NetworkWriter
    {
        private Stream baseStream;

        internal NetworkWriter(Stream baseStream)
        {
            this.baseStream = baseStream;
        }
        public Stream BaseStream
        {
            get { return baseStream; }
        }

        public void BeginWritePackage()
        {
            baseStream.Seek(0, SeekOrigin.Begin);
            baseStream.SetLength(0);
            WritePackageSize(0);
        }

        public void EndWritePackage()
        {
            ushort packageSize;
            packageSize = (ushort)(baseStream.Length - GetPackageSizeBytesSize());
            baseStream.Seek(0, SeekOrigin.Begin);
            WritePackageSize(packageSize);
            baseStream.Seek(0, SeekOrigin.Begin);
        }

        public void Flush()
        {
            baseStream.Flush();
        }


        ushort GetPackageSizeBytesSize()
        {
            return 2;
        }

        private void WritePackageSize(ushort packageSize)
        {
            WriteByte((byte)((packageSize >> 8) & 0xFF));
            WriteByte((byte)(packageSize & 0xFF));
        }
         
        public void WriteByte(byte value)
        {
            baseStream.WriteByte(value);
        }

        public void WriteInt8(sbyte value)
        {
            WriteByte((byte)value);
        }
        public void WriteUInt8(byte value)
        {
            WriteByte(value);
        }
        public void WriteInt16(short value)
        {
            WriteByte((byte)(value >> 0x8));
            WriteByte((byte)(value));
        }
        public void WriteUInt16(ushort value)
        {
            WriteByte((byte)(value >> 0x8));
            WriteByte((byte)(value));
        }
        public void WriteChar(char value)
        {
            WriteByte((byte)(value >> 0x8));
            WriteByte((byte)(value));
        }

        public void WriteInt32(int value)
        {
            WriteByte((byte)(value >> 0x18));
            WriteByte((byte)(value >> 0x10));
            WriteByte((byte)(value >> 0x8));
            WriteByte((byte)(value));
        }
        public void WriteUInt32(uint value)
        {
            WriteByte((byte)(value >> 0x18));
            WriteByte((byte)(value >> 0x10));
            WriteByte((byte)(value >> 0x8));
            WriteByte((byte)(value));
        }
        public void WriteInt64(long value)
        {
            WriteByte((byte)(value >> 0x38));
            WriteByte((byte)(value >> 0x30));
            WriteByte((byte)(value >> 0x28));
            WriteByte((byte)(value >> 0x20));
            WriteByte((byte)(value >> 0x18));
            WriteByte((byte)(value >> 0x10));
            WriteByte((byte)(value >> 0x8));
            WriteByte((byte)(value));
        }
        public void WriteUInt64(ulong value)
        {
            WriteByte((byte)(value >> 0x38));
            WriteByte((byte)(value >> 0x30));
            WriteByte((byte)(value >> 0x28));
            WriteByte((byte)(value >> 0x20));
            WriteByte((byte)(value >> 0x18));
            WriteByte((byte)(value >> 0x10));
            WriteByte((byte)(value >> 0x8));
            WriteByte((byte)(value));
        }
        public void WriteFloat32(float value)
        {
            byte[] tmp = BitConverter.GetBytes(value);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(tmp);
            Write(tmp, 0, 4);
        }
        public void WriteFloat64(double value)
        {
            byte[] tmp = BitConverter.GetBytes(value);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(tmp);
            Write(tmp, 0, 8);
        }
        public void WriteBool(bool value)
        {
            WriteByte((byte)(value ? 1 : 0));
        }
        public void Write(byte[] buffer, int offset, int count)
        {
            baseStream.Write(buffer, offset, count);
        }

        public void WriteString(string value)
        {
            if (value == null || value.Length == 0)
            {
                WriteUInt32(0);
            }
            else
            {
                byte[] bytes = Encoding.UTF8.GetBytes(value);
                WriteUInt32((uint)bytes.Length);
                Write(bytes, 0, bytes.Length);
            }
        }
        public void WriteGuid(Guid value)
        {
            byte[] bytes = value.ToByteArray();
            //little to big
            Array.Reverse(bytes, 0, 4);
            Array.Reverse(bytes, 4, 2);
            Array.Reverse(bytes, 6, 2);
            Write(bytes, 0, bytes.Length);
        }
        public void WriteNetworkInstanceId(NetworkInstanceId value)
        {
            WriteUInt32(value.Value);
        }

        public void WriteNetworkObjectId(NetworkObjectId value)
        {
            WriteGuid(value.Value);
        }

       


    }




}
