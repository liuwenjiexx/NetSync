using System;
using System.Collections;
using System.Collections.Generic;

namespace Yanmonet.NetSync
{
    public interface IReaderWriter
    {
        public bool IsReader { get; }

        public bool IsWriter { get; }

        void SerializeValue(ref byte value);
        void SerializeValue(ref sbyte value);
        void SerializeValue(ref short value);
        void SerializeValue(ref ushort value);
        void SerializeValue(ref int value);
        void SerializeValue(ref uint value);
        void SerializeValue(ref long value);
        void SerializeValue(ref ulong value);
        void SerializeValue(ref float value);
        void SerializeValue(ref double value);
        void SerializeValue(ref bool value);
        void SerializeValue(ref string value);
        void SerializeValue(ref byte[] value, int offset, ref int length);
        void SerializeValue(ref Guid value);

        void SerializeValue<T>(ref T value, Unused unused = default) where T : unmanaged, Enum;


        //void SerializeValue<T>(ref T value) where T : INetworkSerializable;
    }

    public enum Unused
    {

    }
}
