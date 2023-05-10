using System;
using System.Collections;
using System.Collections.Generic;
#if UNITY_ENGINE
using UnityEngine;
using Unity.Collections.LowLevel.Unsafe;
#endif

namespace Yanmonet.Network.Sync
{
#if UNITY_ENGINE
    [Serializable]
#endif
    public class Sync<T> : SyncBase
    {

#if UNITY_ENGINE
        [SerializeField]
#endif
        private T value;

        public delegate void OnValueChangedDelegate(T previousValue, T newValue);
        public delegate bool EqualsDelegate(ref T a, ref T b);

        public OnValueChangedDelegate OnValueChanged;
        public static ISyncVariableSerializer<T> Serializer = new DefaultSyncVariableSerializer<T>();

        public static EqualsDelegate AreEqual;

        public delegate void WriteValueDelegate(IReaderWriter writer, in T value);

        public delegate void ReadValueDelegate(IReaderWriter reader, out T value);

        public static WriteValueDelegate WriteValue;

        public static ReadValueDelegate ReadValue;

        public Sync(T value = default,
         SyncReadPermission readPermission = DefaultReadPermission,
         SyncWritePermission writePermission = DefaultWritePermission)
         : base(readPermission, writePermission)
        {
            this.value = value;
        }

        static Sync()
        {
            Type type = typeof(T);
            if (type.IsPrimitive)
            {
                //AreEqual = ValueEquals;
            }
        }

        public virtual T Value
        {
            get => value;
            set
            {
                CheckWrite();

                if (AreEqual == null ? object.Equals(this.value, value) : AreEqual(ref this.value, ref value))
                {
                    return;
                }

                Set(value);
            }
        }

        private void Set(T value)
        {
            SetDirty(true);
            T previousValue = this.value;
            this.value = value;
            OnValueChanged?.Invoke(previousValue, this.value);
        }
        public override void WriteDelta(IReaderWriter writer)
        {
            Write(writer);
        }

        public override void Write(IReaderWriter writer)
        {
            Write(writer, ref value);
        }
        public override void ReadDelta(IReaderWriter reader, bool keepDirtyDelta)
        {
            T previousValue = value;
            Read(reader, ref value);

            if (keepDirtyDelta)
            {
                SetDirty(true);
            }

            OnValueChanged?.Invoke(previousValue, value);
        }


        public override void Read(IReaderWriter reader)
        {
            Read(reader, ref value);
        }



        internal static void Write(IReaderWriter writer, ref T value)
        {
            Serializer.Write(writer, ref value);
        }

        internal static void Read(IReaderWriter reader, ref T value)
        {
            Serializer.Read(reader, ref value);
        }



        public static unsafe bool ValueEquals<TValueType>(ref TValueType a, ref TValueType b) where TValueType : unmanaged
        {
#if UNITY_ENGINE
            var aptr = UnsafeUtility.AddressOf(ref a);
            var bptr = UnsafeUtility.AddressOf(ref b);

            return UnsafeUtility.MemCmp(aptr, bptr, sizeof(TValueType)) == 0;
#else 
            return object.Equals(a, b);
#endif
        }

        public static bool EqualityEqualsObject<TValueType>(ref TValueType a, ref TValueType b) where TValueType : class, IEquatable<TValueType>
        {
            if (a == null)
            {
                return b == null;
            }

            if (b == null)
            {
                return false;
            }

            return a.Equals(b);
        }

        public static bool EqualityEquals<TValueType>(ref TValueType a, ref TValueType b) where TValueType : unmanaged, IEquatable<TValueType>
        {
            return a.Equals(b);
        }


        public override string ToString()
        {
            return $"{Name}={Value}";
        }

    }



    class DefaultSyncVariableSerializer<T> : ISyncVariableSerializer<T>
    {




        public void Read(IReaderWriter reader, ref T value)
        {
            if (typeof(INetworkSerializable).IsAssignableFrom(typeof(T)))
            {
                INetworkSerializable s = (INetworkSerializable)value;
                if (s == null)
                    s = (INetworkSerializable)NetworkUtility.CreateInstance<T>();
                s.NetworkSerialize(reader);
                value = (T)s;
                return;
            }
            throw new NotImplementedException($"{typeof(T)} Not Implement INetworkVariableSerializer");
        }

        public void Write(IReaderWriter writer, ref T value)
        {
            if (typeof(INetworkSerializable).IsAssignableFrom(typeof(T)))
            {
                INetworkSerializable s = (INetworkSerializable)value;
                if (s == null)
                    s = (INetworkSerializable)NetworkUtility.CreateInstance<T>();
                s.NetworkSerialize(writer);
                return;
            }

            throw new NotImplementedException($"{typeof(T)} Not Implement INetworkVariableSerializer");
        }

    }

}