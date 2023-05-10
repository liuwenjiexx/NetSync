using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.UIElements;
using Yanmonet.NetSync;

namespace Yanmonet.Network.Sync
{
    public static partial class Extensions
    {
        public static void SerializeValue<T>(this IReaderWriter readerWriter, ref T value, INetworkSerializableUnused unused = default)
            where T : INetworkSerializable, new()
        {
            if (value == null)
                value = new();
            value.NetworkSerialize(readerWriter);
        }


        public struct INetworkSerializableUnused
        {

        }

        public static void SerializeValue<T>(this IReaderWriter readerWriter, ref T value)
        {
            var serializer = Sync<T>.Serializer;
            if (serializer != null)
            {
                if (readerWriter.IsReader)
                    serializer.Read(readerWriter, ref value);
                else
                    serializer.Write(readerWriter, ref value);
                return;
            }

            var type = typeof(T);

            if (type.IsPrimitive)
            {

            }


            if (typeof(INetworkSerializable).IsAssignableFrom(type))
            {
                INetworkSerializable value2 = (INetworkSerializable)value;
                if (value2 == null)
                    value2 = Activator.CreateInstance<T>() as INetworkSerializable;

                value2.NetworkSerialize(readerWriter);
                return;
            }
        }



        public static void SerializeValue(this IReaderWriter readerWriter, ref Guid value)
        {
            int length = 16;
            byte[] buffer;
            buffer = new byte[length];
            if (readerWriter.IsReader)
            {
                readerWriter.SerializeValue(ref buffer, 0, ref length);
                NetworkBit.ReadGuid(buffer, 0, out value);
            }
            else
            {
                NetworkBit.WriteGuid(buffer, 0, value);
                readerWriter.SerializeValue(ref buffer, 0, ref length);
            }

        }

        public static void SerializeValue<TKey, TValue>(this IReaderWriter readerWriter, IDictionary<TKey, TValue> value)
        {
            TKey key;
            TValue _value;

            if (readerWriter.IsReader)
            {
                value.Clear();
                int count = 0;
                readerWriter.SerializeValue(ref count);

                for (int i = 0; i < count; i++)
                {
                    key = default;
                    _value = default;
                    readerWriter.SerializeValue(ref key);
                    readerWriter.SerializeValue(ref _value);
                    value[key] = _value;
                }
            }
            else
            {
                int count = value.Count;
                readerWriter.SerializeValue(ref count);
                foreach (var item in value)
                {
                    key = item.Key;
                    _value = item.Value;
                    readerWriter.SerializeValue(ref key);
                    readerWriter.SerializeValue(ref _value);
                }
            }
        }

    }
}
