using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine.UIElements;

namespace Yanmonet.NetSync
{

    public class NetworkWriter : IReaderWriter
    {
        private Stream baseStream;
        public int refCount;

        public NetworkWriter(Stream baseStream)
        {
            this.baseStream = baseStream;
        }

        public Stream BaseStream
        {
            get { return baseStream; }
        }

        public bool IsReader => false;

        public bool IsWriter => true;

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

        public void WriteRaw(byte[] value, int offset, int count)
        {
            baseStream.Write(value, offset, count);
        }

        public void WriteRaw(ArraySegment<byte> value)
        {
            WriteRaw(value.Array, value.Offset, value.Count);
        }

        private void WriteByte(byte value)
        {
            baseStream.WriteByte(value);
        }

        private void WriteInt8(sbyte value)
        {
            WriteByte((byte)value);
        }
        private void WriteUInt8(byte value)
        {
            WriteByte(value);
        }
        private void WriteInt16(short value)
        {
            WriteByte((byte)(value >> 0x8));
            WriteByte((byte)(value));
        }
        private void WriteUInt16(ushort value)
        {
            WriteByte((byte)(value >> 0x8));
            WriteByte((byte)(value));
        }
        private void WriteChar(char value)
        {
            WriteByte((byte)(value >> 0x8));
            WriteByte((byte)(value));
        }

        private void WriteInt32(int value)
        {
            WriteByte((byte)(value >> 0x18));
            WriteByte((byte)(value >> 0x10));
            WriteByte((byte)(value >> 0x8));
            WriteByte((byte)(value));
        }
        internal void WriteUInt32(uint value)
        {
            WriteByte((byte)(value >> 0x18));
            WriteByte((byte)(value >> 0x10));
            WriteByte((byte)(value >> 0x8));
            WriteByte((byte)(value));
        }
        private void WriteInt64(long value)
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
        private void WriteUInt64(ulong value)
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
        private void WriteFloat32(float value)
        {
            byte[] tmp = BitConverter.GetBytes(value);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(tmp);
            Write(tmp, 0, 4);
        }
        private void WriteFloat64(double value)
        {
            byte[] tmp = BitConverter.GetBytes(value);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(tmp);
            Write(tmp, 0, 8);
        }
        private void WriteBool(bool value)
        {
            WriteByte((byte)(value ? 1 : 0));
        }
        private void Write(byte[] buffer, int offset, int count)
        {
            baseStream.Write(buffer, offset, count);
        }

        private void WriteString(string value)
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
        internal void WriteGuid(Guid value)
        {
            byte[] bytes = value.ToByteArray();
            //little to big
            Array.Reverse(bytes, 0, 4);
            Array.Reverse(bytes, 4, 2);
            Array.Reverse(bytes, 6, 2);
            Write(bytes, 0, bytes.Length);
        }



        public void SerializeValue(ref byte value)
        {
            WriteByte(value);
        }

        public void SerializeValue(ref sbyte value)
        {
            WriteInt8(value);
        }

        public void SerializeValue(ref short value)
        {
            WriteInt16(value);
        }

        public void SerializeValue(ref ushort value)
        {
            WriteUInt16(value);
        }

        public void SerializeValue(ref int value)
        {
            WriteInt32(value);
        }

        public void SerializeValue(ref uint value)
        {
            WriteUInt32(value);
        }

        public void SerializeValue(ref long value)
        {
            WriteInt64(value);
        }

        public void SerializeValue(ref ulong value)
        {
            WriteUInt64(value);
        }

        public void SerializeValue(ref float value)
        {
            WriteFloat32(value);
        }

        public void SerializeValue(ref double value)
        {
            WriteFloat64(value);
        }

        public void SerializeValue(ref bool value)
        {
            WriteBool(value);
        }

        public void SerializeValue(ref string value)
        {
            WriteString(value);
        }

        public void SerializeValue(ref byte[] value, int offset, ref int length)
        {
            WriteInt32(length);
            if (length > 0)
            {
                Write(value, offset, length);
            }
        }



        public void SerializeValue(ref Guid value)
        {
            WriteGuid(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void SerializeValue<T>(ref T value, Unused unused = default) where T : unmanaged, Enum
        {
            int size = sizeof(T);

            switch (size)
            {
                case 1:
                    {
                        byte n = (byte)(object)value;
                        SerializeValue(ref n);
                    }
                    break;
                case 2:
                    {
                        short n = (short)(object)value;
                        SerializeValue(ref n);
                    }
                    break;
                case 4:
                    {
                        int n = (int)(object)value;
                        SerializeValue(ref n);
                    }
                    break;
                case 8:
                    {
                        long n = (long)(object)value;
                        SerializeValue(ref n);
                    }
                    break;
            }

        }

        //public void SerializeValue<T>(ref T value)
        //    where T : INetworkSerializable
        //{
        //    value.NetworkSerialize(this);
        //}
    }




}
