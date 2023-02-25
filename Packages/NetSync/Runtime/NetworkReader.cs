using System;
using System.IO;
using System.Net.Sockets;
using System.Text;

namespace Yanmonet.NetSync
{

    public class NetworkReader
    {
        private MemoryStream msReader;
        private Socket baseStream;

        internal NetworkReader(Socket socket)
        {
            this.baseStream = socket;
            msReader = new MemoryStream(100);
        }

        private ushort packageSIze;
        public MemoryStream ReaderStream { get { return msReader; } }

        public ushort PackageSize { get { return packageSIze; } }

        public ushort ReadPackage()
        {
            if (packageSIze > 0)
            {
                return ReadPackageContent();
            }

            if (baseStream.Available > 2)
            {

                byte[] buff = msReader.GetBuffer();
                baseStream.Receive(buff, 2, SocketFlags.None);
                packageSIze = (ushort)(buff[0] << 8);
                packageSIze |= (ushort)(buff[1]);

                msReader.Position = 0;
                msReader.SetLength(packageSIze);

                return ReadPackageContent();
            }
            return 0;
        }
        private ushort ReadPackageContent()
        {
            if (packageSIze > 0)
            {

                int count = packageSIze - (int)msReader.Position;
                if (count > 0)
                {
                    int readCount;
                    readCount = baseStream.Receive(msReader.GetBuffer(), (int)msReader.Position, count, SocketFlags.None);
                    if (readCount > 0)
                    {
                        msReader.Position += readCount;
                    }
                }
                if (msReader.Position >= packageSIze)
                {
                    var tmp = packageSIze;
                    packageSIze = 0;
                    msReader.Position = 0;
                    return tmp;
                }
            }
            return 0;
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

        public byte ReadByte()
        {
            return (byte)msReader.ReadByte();
        }

        public sbyte ReadInt8()
        {
            return (sbyte)msReader.ReadByte();
        }
        public byte ReadUInt8()
        {
            return (byte)msReader.ReadByte();
        }

        public short ReadInt16()
        {
            short value;
            position += BigToInt16(buffer, position, out value);
            return value;
        }
        public ushort ReadUInt16()
        {
            ushort value;
            position += BigToUInt16(buffer, position, out value);
            return value;
        }
        public char ReadChar()
        {
            char value;
            position += BigToChar(buffer, position, out value);
            return value;
        }
        public int ReadInt32()
        {
            int value;
            position += BigToInt32(buffer, position, out value);
            return value;
        }
        public uint ReadUInt32()
        {
            uint value;
            position += BigToUInt32(buffer, position, out value);
            return value;
        }
        public long ReadInt64()
        {
            long value;
            position += BigToInt64(buffer, position, out value);
            return value;
        }
        public ulong ReadUInt64()
        {
            ulong value;
            position += BigToUInt64(buffer, position, out value);
            return value;
        }
        public float ReadFloat32()
        {
            float value;
            position += BigToFloat32(buffer, position, out value);
            return value;
        }
        public double ReadFloat64()
        {
            double value;
            position += BigToFloat64(buffer, position, out value);
            return value;
        }
        public bool ReadBool()
        {
            byte b;
            b = ReadByte(); 
            return b == 0 ? false : true;
        }
        public void Read(byte[] buffer, int offset, int length)
        {
            msReader.Read(buffer, offset, length);
        }
        public string ReadString()
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
            position += BigToGuid(buffer, position, out value);
            return value;
        }

        public NetworkInstanceId ReadNetworkInstanceId()
        {
            uint value = ReadUInt32();
            NetworkInstanceId id = new NetworkInstanceId(value);
            return id;
        }

        public NetworkObjectId ReadNetworkObjectId()
        {
            Guid guid = ReadGuid();
            NetworkObjectId id = new NetworkObjectId(guid);
            return id;
        }


        #region Big

        public static int BigToChar(byte[] buffer, int offset, out char value)
        {
            value = (char)((buffer[offset] << 0x8) | buffer[offset + 1]);
            return 2;
        }

        public static int BigToInt16(byte[] buffer, int offset, out short value)
        {
            value = (short)((buffer[offset] << 0x8) | buffer[offset + 1]);
            return 2;
        }

        public static int BigToUInt16(byte[] buffer, int offset, out ushort value)
        {
            value = (ushort)((buffer[offset] << 0x8) | buffer[offset + 1]);
            return 2;
        }

        public static int BigToInt32(byte[] buffer, int offset, out int value)
        {
            value = (buffer[offset] << 0x18) | (buffer[offset + 1] << 0x10) | (buffer[offset + 2] << 0x8) | buffer[offset + 3];
            return 4;
        }

        public static int BigToUInt32(byte[] buffer, int offset, out uint value)
        {
            value = (uint)((buffer[offset] << 0x18) | (buffer[offset + 1] << 0x10) | (buffer[offset + 2] << 0x8) | buffer[offset + 3]);
            return 4;
        }

        public static int BigToInt64(byte[] buffer, int offset, out long value)
        {
            long h = ((buffer[offset] << 0x18) | (buffer[offset + 1] << 0x10) | (buffer[offset + 2] << 0x8) | buffer[offset + 3]);
            long l = ((uint)((buffer[offset + 4] << 0x18) | (buffer[offset + 5] << 0x10) | (buffer[offset + 6] << 0x8) | buffer[offset + 7]));

            value = l | (h << 0x20);
            return 8;
        }

        public static int BigToUInt64(byte[] buffer, int offset, out ulong value)
        {
            long l;
            BigToInt64(buffer, offset, out l);
            value = (ulong)l;
            return 8;
        }


        public static int BigToFloat32(byte[] buffer, int offset, out float value)
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

        public static int BigToFloat64(byte[] buffer, int offset, out double value)
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


        public static int BigToGuid(byte[] buffer, int offset, out Guid value)
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



        #endregion




    }
}
