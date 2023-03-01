using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Yanmonet.NetSync
{
    public static class NetworkBit
    {
        public static unsafe int GetSize<T>()
         where T : unmanaged
        {
            return sizeof(T);
        }

        #region Read

        public static int ReadInt8(byte[] buffer, int offset, out sbyte value)
        {
            value = (sbyte)buffer[offset];
            return 1;
        }
        public static void ReadInt8(byte[] buffer, ref int offset, out sbyte value)
        {
            value = (sbyte)buffer[offset];
            offset += 1;
        }

        public static int ReadUInt8(byte[] buffer, int offset, out byte value)
        {
            value = buffer[offset];
            return 1;
        }
        public static void ReadUInt8(byte[] buffer, ref int offset, out byte value)
        {
            value = buffer[offset];
            offset += 1;
        }

        public static void ReadInt16(byte[] buffer, ref int offset, out short value)
        {
            value = (short)((buffer[offset] << 0x8) | buffer[offset + 1]);
            offset += 2;
        }

        public static int ReadInt16(byte[] buffer, int offset, out short value)
        {
            value = (short)((buffer[offset] << 0x8) | buffer[offset + 1]);
            return 2;
        }
        public static void ReadUInt16(byte[] buffer, ref int offset, out ushort value)
        {
            value = (ushort)((buffer[offset] << 0x8) | buffer[offset + 1]);
            offset += 2;
        }

        public static int ReadUInt16(byte[] buffer, int offset, out ushort value)
        {
            value = (ushort)((buffer[offset] << 0x8) | buffer[offset + 1]);
            return 2;
        }

        public static void ReadChar(byte[] buffer, ref int offset, out char value)
        {
            value = (char)((buffer[offset] << 0x8) | buffer[offset + 1]);
            offset += 2;
        }

        public static int ReadChar(byte[] buffer, int offset, out char value)
        {
            value = (char)((buffer[offset] << 0x8) | buffer[offset + 1]);
            return 2;
        }
        public static void ReadInt32(byte[] buffer, ref int offset, out int value)
        {
            value = (buffer[offset] << 0x18) | (buffer[offset + 1] << 0x10) | (buffer[offset + 2] << 0x8) | buffer[offset + 3];
            offset += 4;
        }

        public static int ReadInt32(byte[] buffer, int offset, out int value)
        {
            value = (buffer[offset] << 0x18) | (buffer[offset + 1] << 0x10) | (buffer[offset + 2] << 0x8) | buffer[offset + 3];
            return 4;
        }

        public static int ReadUInt32(byte[] buffer, int offset, out uint value)
        {
            value = (uint)((buffer[offset] << 0x18) | (buffer[offset + 1] << 0x10) | (buffer[offset + 2] << 0x8) | buffer[offset + 3]);
            return 4;
        }
        public static void ReadUInt32(byte[] buffer, ref int offset, out uint value)
        {
            offset += ReadUInt32(buffer, offset, out value);
        }

        public static int ReadInt64(byte[] buffer, int offset, out long value)
        {
            long h = ((buffer[offset] << 0x18) | (buffer[offset + 1] << 0x10) | (buffer[offset + 2] << 0x8) | buffer[offset + 3]);
            long l = ((uint)((buffer[offset + 4] << 0x18) | (buffer[offset + 5] << 0x10) | (buffer[offset + 6] << 0x8) | buffer[offset + 7]));

            value = l | (h << 0x20);
            return 8;
        }

        public static void ReadInt64(byte[] buffer, ref int offset, out long value)
        {
            offset += ReadInt64(buffer, offset, out value);
        }

        public static int ReadUInt64(byte[] buffer, int offset, out ulong value)
        {
            long l;
            ReadInt64(buffer, offset, out l);
            value = (ulong)l;
            return 8;
        }
        public static void ReadUInt64(byte[] buffer, ref int offset, out ulong value)
        {
            offset += ReadUInt64(buffer, offset, out value);
        }

        public static int ReadFloat32(byte[] buffer, int offset, out float value)
        {
            if (BitConverter.IsLittleEndian)
            {
                byte[] tmp = new byte[4];
                Array.Copy(buffer, offset, tmp, 0, tmp.Length);
                Array.Reverse(tmp);
                value = BitConverter.ToSingle(tmp, 0);
            }
            else
            {
                value = BitConverter.ToSingle(buffer, offset);
            }
            return 4;
        }
        public static void ReadFloat32(byte[] buffer, ref int offset, out float value)
        {
            offset += ReadFloat32(buffer, offset, out value);
        }

        public static int ReadFloat64(byte[] buffer, int offset, out double value)
        {
            if (BitConverter.IsLittleEndian)
            {
                byte[] tmp = new byte[8];
                Array.Copy(buffer, offset, tmp, 0, tmp.Length);
                Array.Reverse(tmp);
                value = BitConverter.ToDouble(tmp, 0);
            }
            else
            {
                value = BitConverter.ToDouble(buffer, offset);
            }
            return 8;
        }

        public static void ReadFloat64(byte[] buffer, ref int offset, out double value)
        {
            offset += ReadFloat64(buffer, offset, out value);
        }
        public static int ReadBool(byte[] buffer, int offset, out bool value)
        {
            value = buffer[offset] != 0;
            return 1;
        }

        public static void ReadBool(byte[] buffer, ref int offset, out bool value)
        {
            offset += ReadBool(buffer, offset, out value);
        }

        public static int ReadString(byte[] buffer, int offset, out string value)
        {
            int bytesCount;
            ReadInt32(buffer, ref offset, out bytesCount);
            value = Encoding.UTF8.GetString(buffer, offset, bytesCount);
            return bytesCount;
        }

        public static void ReadString(byte[] buffer, ref int offset, out string value)
        {
            offset += ReadString(buffer, offset, out value);
        }

        public static int ReadGuid(byte[] buffer, int offset, out Guid value)
        {
            byte[] bytes = new byte[16];

            Array.Copy(buffer, offset, bytes, 0, 16);
            //big to little
            Array.Reverse(bytes, 0, 4);
            Array.Reverse(bytes, 4, 2);
            Array.Reverse(bytes, 6, 2);

            value = new Guid(bytes);
            return 16;
        }

        public static void ReadGuid(byte[] buffer, ref int offset, out Guid value)
        {
            offset += ReadGuid(buffer, offset, out value);
        }

        #endregion

        #region Write

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteInt8(Stream writer, sbyte value)
        {
            writer.WriteByte((byte)value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteUInt8(Stream writer, byte value)
        {
            writer.WriteByte(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteInt16(Stream writer, short value)
        {
            WriteUInt8(writer, (byte)(value >> 0x8));
            WriteUInt8(writer, (byte)(value));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteUInt16(Stream writer, ushort value)
        {
            WriteUInt8(writer, (byte)(value >> 0x8));
            WriteUInt8(writer, (byte)(value));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteChar(Stream writer, char value)
        {
            WriteUInt8(writer, (byte)(value >> 0x8));
            WriteUInt8(writer, (byte)(value));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteInt32(Stream writer, int value)
        {
            WriteUInt8(writer, (byte)(value >> 0x18));
            WriteUInt8(writer, (byte)(value >> 0x10));
            WriteUInt8(writer, (byte)(value >> 0x8));
            WriteUInt8(writer, (byte)(value));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteUInt32(Stream writer, uint value)
        {
            WriteUInt8(writer, (byte)(value >> 0x18));
            WriteUInt8(writer, (byte)(value >> 0x10));
            WriteUInt8(writer, (byte)(value >> 0x8));
            WriteUInt8(writer, (byte)(value));
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteInt64(Stream writer, long value)
        {
            WriteUInt8(writer, (byte)(value >> 0x38));
            WriteUInt8(writer, (byte)(value >> 0x30));
            WriteUInt8(writer, (byte)(value >> 0x28));
            WriteUInt8(writer, (byte)(value >> 0x20));
            WriteUInt8(writer, (byte)(value >> 0x18));
            WriteUInt8(writer, (byte)(value >> 0x10));
            WriteUInt8(writer, (byte)(value >> 0x8));
            WriteUInt8(writer, (byte)(value));
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteUInt64(Stream writer, ulong value)
        {
            WriteUInt8(writer, (byte)(value >> 0x38));
            WriteUInt8(writer, (byte)(value >> 0x30));
            WriteUInt8(writer, (byte)(value >> 0x28));
            WriteUInt8(writer, (byte)(value >> 0x20));
            WriteUInt8(writer, (byte)(value >> 0x18));
            WriteUInt8(writer, (byte)(value >> 0x10));
            WriteUInt8(writer, (byte)(value >> 0x8));
            WriteUInt8(writer, (byte)(value));
        }
        public static void WriteFloat32(Stream writer, float value)
        {
            byte[] tmp = BitConverter.GetBytes(value);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(tmp);
            WriteBytes(writer, tmp, 0, 4);
        }
        public static void WriteFloat64(Stream writer, double value)
        {
            byte[] tmp = BitConverter.GetBytes(value);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(tmp);
            WriteBytes(writer, tmp, 0, 8);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteBool(Stream writer, bool value)
        {
            WriteUInt8(writer, (byte)(value ? 1 : 0));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteBytes(Stream writer, byte[] buffer, int offset, int count)
        {
            writer.Write(buffer, offset, count);
        }

        public static void WriteString(Stream writer, string value)
        {
            if (value == null || value.Length == 0)
            {
                WriteInt32(writer, 0);
            }
            else
            {
                byte[] bytes = Encoding.UTF8.GetBytes(value);
                WriteInt32(writer, bytes.Length);
                WriteBytes(writer, bytes, 0, bytes.Length);
            }
        }

        public static void WriteGuid(Stream writer, Guid value)
        {
            byte[] bytes = value.ToByteArray();
            //little to big
            Array.Reverse(bytes, 0, 4);
            Array.Reverse(bytes, 4, 2);
            Array.Reverse(bytes, 6, 2);
            WriteBytes(writer, bytes, 0, bytes.Length);
        }



        #endregion
    }
}
