using System;
using System.IO;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
namespace Yanmonet.NetSync
{

    public class NetworkReader : IReaderWriter
    {
        private MemoryStream msReader;
        private Socket baseSocket;
        private Stream baseStream;

        internal NetworkReader(Socket socket)
        {
            this.baseSocket = socket;
            msReader = new MemoryStream(1024 * 4);
        }
        public NetworkReader(Stream stream, MemoryStream buffer)
        {
            this.baseStream = stream;
            if (buffer == null)
                msReader = new MemoryStream(1024 * 4);
            else
                msReader = buffer;
        }
        internal NetworkReader(MemoryStream ms)
        {
            msReader = ms;
        }

        private ushort packageSize;
        public MemoryStream ReaderStream { get { return msReader; } }

        public ushort PackageSize { get { return packageSize; } }

        public byte[] rawPacket;

        public ushort ReadPackage()
        {
            if (packageSize > 0)
            {
                return ReadPackageContent();
            }

            if (baseSocket != null)
            {
                if (baseSocket.Available > 2)
                {
                    byte[] buff = msReader.GetBuffer();
                    baseSocket.Receive(buff, 2, SocketFlags.None);
                    packageSize = (ushort)(buff[0] << 8);
                    packageSize |= (ushort)(buff[1]);

                    msReader.Position = 0;
                    msReader.SetLength(packageSize);

                    return ReadPackageContent();
                }
            }
            else
            {
                if (baseStream.Length - baseStream.Position > 2)
                {
                    packageSize = (ushort)(baseStream.ReadByte() << 8);
                    packageSize |= (ushort)(baseStream.ReadByte());

                    msReader.Position = 0;
                    msReader.SetLength(packageSize);

                    return ReadPackageContent();
                }

            }
            return 0;
        }
        private ushort ReadPackageContent()
        {
            if (packageSize > 0)
            {

                int count = packageSize - (int)msReader.Position;
                if (count > 0)
                {
                    int readCount;
                    if (baseSocket != null)
                    {
                        readCount = baseSocket.Receive(msReader.GetBuffer(), (int)msReader.Position, count, SocketFlags.None);
                    }
                    else
                    {
                        readCount = baseStream.Read(msReader.GetBuffer(), (int)msReader.Position, count);
                    }
                    if (readCount > 0)
                    {
                        msReader.Position += readCount;
                    }
                }
                if (msReader.Position >= packageSize)
                {
                    var tmp = packageSize;
                    packageSize = 0;
                    msReader.Position = 0;
                    rawPacket = new byte[tmp];
                    Array.Copy(msReader.GetBuffer(), 0, rawPacket, 0, tmp);
                    return tmp;
                }
            }
            return 0;
        }

        public bool ReadPackage(out ushort msgId, out int length)
        {
            ushort size = ReadPackage();
            if (size > 0)
            {
                msgId = ReadUInt16();
                length = size - 2;
                return true;
            }
            msgId = 0;
            length = 0;
            return false;
        }

        private byte[] buff8 = new byte[8];
        private int position
        {
            get { return (int)msReader.Position; }
            set { msReader.Position = value; }
        }
        private byte[] buffer
        {
            get { return msReader.GetBuffer(); }
        }

        public bool IsReader => true;

        public bool IsWriter => false;

        public void Reset()
        {
            packageSize = 0;
        }

        internal byte ReadByte()
        {
            return (byte)msReader.ReadByte();
        }

        internal sbyte ReadInt8()
        {
            return (sbyte)msReader.ReadByte();
        }
        internal byte ReadUInt8()
        {
            return (byte)msReader.ReadByte();
        }

        internal short ReadInt16()
        {
            short value;
            position += NetworkBit.ReadInt16(buffer, position, out value);
            return value;
        }
        internal ushort ReadUInt16()
        {
            ushort value;
            position += NetworkBit.ReadUInt16(buffer, position, out value);
            return value;
        }
        internal char ReadChar()
        {
            char value;
            position += NetworkBit.ReadChar(buffer, position, out value);
            return value;
        }
        internal int ReadInt32()
        {
            int value;
            position += NetworkBit.ReadInt32(buffer, position, out value);
            return value;
        }
        internal uint ReadUInt32()
        {
            uint value;
            position += NetworkBit.ReadUInt32(buffer, position, out value);
            return value;
        }
        internal long ReadInt64()
        {
            long value;
            position += NetworkBit.ReadInt64(buffer, position, out value);
            return value;
        }
        internal ulong ReadUInt64()
        {
            ulong value;
            position += NetworkBit.ReadUInt64(buffer, position, out value);
            return value;
        }
        internal float ReadFloat32()
        {
            float value;
            position += NetworkBit.ReadFloat32(buffer, position, out value);
            return value;
        }
        internal double ReadFloat64()
        {
            double value;
            position += NetworkBit.ReadFloat64(buffer, position, out value);
            return value;
        }
        internal bool ReadBool()
        {
            byte b;
            b = ReadByte();
            return b == 0 ? false : true;
        }
        internal void Read(byte[] buffer, int offset, int length)
        {
            msReader.Read(buffer, offset, length);
        }
        internal string ReadString()
        {
            int count;
            count = ReadInt32();
            if (count == 0)
            {
                return null;
            }
            byte[] tmp = new byte[count];
            Read(tmp, 0, count);
            string str = Encoding.UTF8.GetString(tmp, 0, count);
            return str;
        }


        public Guid ReadGuid()
        {
            Guid value;
            position += NetworkBit.ReadGuid(buffer, position, out value);
            return value;
        }



        public void SerializeValue(ref byte value)
        {
            value = ReadByte();
        }

        public void SerializeValue(ref sbyte value)
        {
            value = ReadInt8();
        }

        public void SerializeValue(ref short value)
        {
            value = ReadInt16();
        }

        public void SerializeValue(ref ushort value)
        {
            value = ReadUInt16();
        }

        public void SerializeValue(ref int value)
        {
            value = ReadInt32();
        }

        public void SerializeValue(ref uint value)
        {
            value = ReadUInt32();
        }

        public void SerializeValue(ref long value)
        {
            value = ReadInt64();
        }

        public void SerializeValue(ref ulong value)
        {
            value = ReadUInt64();
        }

        public void SerializeValue(ref float value)
        {
            value = ReadFloat32();
        }

        public void SerializeValue(ref double value)
        {
            value = ReadFloat64();
        }

        public void SerializeValue(ref bool value)
        {
            value = ReadBool();
        }

        public void SerializeValue(ref string value)
        {
            value = ReadString();
        }

        public void SerializeValue(ref byte[] value, int offset, ref int length)
        {
            length = ReadInt32();
            if (value == null)
                value = new byte[length];
            else
            {
                if (value.Length < length)
                    throw new OverflowException();
            }
            Read(value, offset, length);
        }
        public void SerializeValue(ref Guid value)
        {
            value = ReadGuid();
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void SerializeValue<T>(ref T value, Unused unused = default) where T : unmanaged, Enum
        {
            int size = sizeof(T);

            switch (size)
            {
                case 1:
                    {
                        byte n = default;
                        SerializeValue(ref n);
                        value = (T)(object)n;
                    }
                    break;
                case 2:
                    {
                        short n = default;
                        SerializeValue(ref n);
                        value = (T)(object)n;
                    }
                    break;
                case 4:
                    {
                        int n = default;
                        SerializeValue(ref n);
                        value = (T)(object)n;
                    }
                    break;
                case 8:
                    {
                        long n = default;
                        SerializeValue(ref n);
                        value = (T)(object)n;
                    }
                    break;
            }

        }


        public void SerializeValue<T>(ref T value)
            where T : INetworkSerializable
        {
            value.NetworkSerialize(this);
        }



    }

}
