using System;
using System.Collections;
using System.Collections.Generic;
using System.Windows.Forms.DataVisualization.Charting;
using Unity.Collections.LowLevel.Unsafe;
using UnityEditor;
using UnityEngine;

namespace Yanmonet.NetSync
{
    [Serializable]
    public class NetworkVariable<T> : NetworkVariableBase
    {

        [SerializeField]
        private T value;

        public delegate void OnValueChangedDelegate(T previousValue, T newValue);
        public delegate bool EqualsDelegate(ref T a, ref T b);

        public OnValueChangedDelegate OnValueChanged;
        public static INetworkVariableSerializer<T> Serializer = new DefaultNetworkVariableSerializer<T>();

        public static EqualsDelegate AreEqual;

        public delegate void WriteValueDelegate(IReaderWriter writer, in T value);

        public delegate void ReadValueDelegate(IReaderWriter reader, out T value);

        public static WriteValueDelegate WriteValue;

        public static ReadValueDelegate ReadValue;

        public NetworkVariable(T value = default,
         NetworkVariableReadPermission readPermission = DefaultReadPermission,
         NetworkVariableWritePermission writePermission = DefaultWritePermission)
         : base(readPermission, writePermission)
        {
            this.value = value;
        }

        static NetworkVariable()
        {
            Type type= typeof(T);
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
                // Compare bitwise
                if (AreEqual(ref this.value, ref value))
                {
                    return;
                }

                if (NetworkObject != null && !CanClientWrite(NetworkObject.NetworkManager.LocalClientId))
                {
                    throw new InvalidOperationException($"Client is not allowed to write to this NetworkVariable '{NetworkObject.GetType().Name}.{Name}'");
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
            var aptr = UnsafeUtility.AddressOf(ref a);
            var bptr = UnsafeUtility.AddressOf(ref b);

            return UnsafeUtility.MemCmp(aptr, bptr, sizeof(TValueType)) == 0;
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



    class DefaultNetworkVariableSerializer<T> : INetworkVariableSerializer<T>
    {




        public void Read(IReaderWriter reader, ref T value)
        {
            if (typeof(INetworkSerializable).IsAssignableFrom(typeof(T)))
            {
                INetworkSerializable s = (INetworkSerializable)value;
                if (s == null)
                    throw new Exception($"NetworkVariableSerializer Type '{typeof(T)}' value not null");
                s.NetworkSerialize(reader);
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
                    throw new Exception($"NetworkVariableSerializer Type '{typeof(T)}' value not null");
                s.NetworkSerialize(writer);
                return;
            }

            throw new NotImplementedException($"{typeof(T)} Not Implement INetworkVariableSerializer");
        }

    }

}