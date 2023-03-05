using System;
using System.Collections;
using System.Collections.Generic;



namespace Yanmonet.NetSync
{
    public interface INetworkVariableSerializer<T>
    {
        public void Write(IReaderWriter writer, ref T value);

        public void Read(IReaderWriter reader, ref T value);

        //public bool Equal(T a, T b);

    }



    internal class BytesSerializer : INetworkVariableSerializer<byte[]>
    {
        public void Write(IReaderWriter writer, ref byte[] value)
        {
            int len = value == null ? 0 : value.Length;
            writer.SerializeValue(ref value, 0, ref len);
        }
        public void Read(IReaderWriter reader, ref byte[] value)
        {
            int len = 0;
            reader.SerializeValue(ref value, 0, ref len);
        }


    }
    internal class Int8Serializer : INetworkVariableSerializer<sbyte>
    {
        public void Write(IReaderWriter writer, ref sbyte value)
        {
            writer.SerializeValue(ref value);
        }
        public void Read(IReaderWriter reader, ref sbyte value)
        {
            reader.SerializeValue(ref value);
        }
    }
    internal class UInt8Serializer : INetworkVariableSerializer<byte>
    {
        public void Write(IReaderWriter writer, ref byte value)
        {
            writer.SerializeValue(ref value);
        }
        public void Read(IReaderWriter reader, ref byte value)
        {
            reader.SerializeValue(ref value);
        }
    }
    internal class Int16Serializer : INetworkVariableSerializer<short>
    {
        public void Write(IReaderWriter writer, ref short value)
        {
            writer.SerializeValue(ref value);
        }
        public void Read(IReaderWriter reader, ref short value)
        {
            reader.SerializeValue(ref value);
        }
    }
    internal class UInt16Serializer : INetworkVariableSerializer<ushort>
    {
        public void Write(IReaderWriter writer, ref ushort value)
        {
            writer.SerializeValue(ref value);
        }
        public void Read(IReaderWriter reader, ref ushort value)
        {
            reader.SerializeValue(ref value);
        }
    }
    internal class Int32Serializer : INetworkVariableSerializer<int>
    {
        public void Write(IReaderWriter writer, ref int value)
        {
            writer.SerializeValue(ref value);
        }
        public void Read(IReaderWriter reader, ref int value)
        {
            reader.SerializeValue(ref value);
        }
    }
    internal class UInt32Serializer : INetworkVariableSerializer<uint>
    {
        public void Write(IReaderWriter writer, ref uint value)
        {
            writer.SerializeValue(ref value);
        }
        public void Read(IReaderWriter reader, ref uint value)
        {
            reader.SerializeValue(ref value);
        }
    }
    internal class Int64Serializer : INetworkVariableSerializer<long>
    {
        public void Write(IReaderWriter writer, ref long value)
        {
            writer.SerializeValue(ref value);
        }
        public void Read(IReaderWriter reader, ref long value)
        {
            reader.SerializeValue(ref value);
        }
    }
    internal class UInt64Serializer : INetworkVariableSerializer<ulong>
    {
        public void Write(IReaderWriter writer, ref ulong value)
        {
            writer.SerializeValue(ref value);
        }
        public void Read(IReaderWriter reader, ref ulong value)
        {
            reader.SerializeValue(ref value);
        }
    }
    internal class Float32Serializer : INetworkVariableSerializer<float>
    {
        public void Write(IReaderWriter writer, ref float value)
        {
            writer.SerializeValue(ref value);
        }
        public void Read(IReaderWriter reader, ref float value)
        {
            reader.SerializeValue(ref value);
        }
    }
    internal class Float64Serializer : INetworkVariableSerializer<double>
    {
        public void Write(IReaderWriter writer, ref double value)
        {
            writer.SerializeValue(ref value);
        }
        public void Read(IReaderWriter reader, ref double value)
        {
            reader.SerializeValue(ref value);
        }
    }
    internal class BoolSerializer : INetworkVariableSerializer<bool>
    {
        public void Write(IReaderWriter writer, ref bool value)
        {
            writer.SerializeValue(ref value);
        }
        public void Read(IReaderWriter reader, ref bool value)
        {
            reader.SerializeValue(ref value);
        }
    }
    internal class StringSerializer : INetworkVariableSerializer<string>
    {
        public void Write(IReaderWriter writer, ref string value)
        {
            writer.SerializeValue(ref value);
        }
        public void Read(IReaderWriter reader, ref string value)
        {
            reader.SerializeValue(ref value);
        }
    }
    internal class GuidSerializer : INetworkVariableSerializer<Guid>
    {
        public void Write(IReaderWriter writer, ref Guid value)
        {
            writer.SerializeValue(ref value);
        }
        public void Read(IReaderWriter reader, ref Guid value)
        {
            reader.SerializeValue(ref value);
        }
    }

}
