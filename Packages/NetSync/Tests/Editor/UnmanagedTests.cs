using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;


namespace Yanmonet.NetSync.Editor.Tests
{
    public class UnmanagedTests
    {

        [Test]
        public void IsPrimitive()
        {
            Assert.IsTrue(typeof(byte).IsPrimitive);
            Assert.IsTrue(typeof(short).IsPrimitive);
            Assert.IsTrue(typeof(int).IsPrimitive);
            Assert.IsTrue(typeof(long).IsPrimitive);
            Assert.IsTrue(typeof(float).IsPrimitive);
            Assert.IsTrue(typeof(double).IsPrimitive);
            Assert.IsTrue(typeof(bool).IsPrimitive);


            Assert.IsFalse(typeof(ByteIntStruct).IsPrimitive);
            Assert.IsFalse(typeof(ByteEnum).IsPrimitive);
            Assert.IsFalse(typeof(string).IsPrimitive);
            Assert.IsFalse(typeof(object).IsPrimitive);

        }

        /// <summary>
        /// Not Unmanaged: Object, String
        /// </summary>
        [Test]
        public void Size()
        {
            Assert.AreEqual(1, NetworkBit.GetSize<byte>());
            Assert.AreEqual(2, NetworkBit.GetSize<short>());
            Assert.AreEqual(4, NetworkBit.GetSize<int>());
            Assert.AreEqual(8, NetworkBit.GetSize<long>());
            Assert.AreEqual(4, NetworkBit.GetSize<float>());
            Assert.AreEqual(8, NetworkBit.GetSize<double>());
            Assert.AreEqual(1, NetworkBit.GetSize<bool>());
            Assert.AreEqual(8, NetworkBit.GetSize<ByteIntStruct>());
            Assert.AreEqual(1, NetworkBit.GetSize<ByteEnum>());
            Assert.AreEqual(4, NetworkBit.GetSize<IntEnum>());
        }

        [Test]
        public void CopyToBytes()
        {
            int n = 1;
            byte[] buffer = new byte[4];
            MemoryStream ms = new MemoryStream(buffer);
            NetworkBit.WriteInt32(ms, n);

            CollectionAssert.AreEqual(new byte[] { 0, 0, 0, 1 }, buffer);
            CollectionAssert.AreEqual(new byte[] { 1, 0, 0, 0 }, MemCpyToBytes(ref n));
            CollectionAssert.AreEqual(new byte[] { 1, 0, 0, 0 }, AddressOfToBytes(ref n));

            ByteEnum byteEnum = ByteEnum.Item1;
            CollectionAssert.AreEqual(new byte[] { 1 }, MemCpyToBytes(ref byteEnum));

            IntEnum intEnum = IntEnum.Item1;
            CollectionAssert.AreEqual(new byte[] { 1, 0, 0, 0 }, MemCpyToBytes(ref intEnum));

            ByteIntStruct @struct1 = new ByteIntStruct() { b = 1, i = 2 };
            CollectionAssert.AreEqual(new byte[] {
                1, 0, 0, 0,
                2, 0, 0, 0 }, MemCpyToBytes(ref struct1));
        }


        internal static unsafe byte[] MemCpyToBytes<T>(ref T value) where T : unmanaged
        {
            int size = NetworkBit.GetSize<T>();
            byte[] buffer = new byte[size];
            fixed (T* ptr = &value)
            fixed (byte* dstPtr = buffer)
            {
                UnsafeUtility.MemCpy(dstPtr, ptr, size);
            }
            return buffer;
        }
        internal static unsafe byte[] AddressOfToBytes<T>(ref T value) where T : unmanaged
        {
            int size = NetworkBit.GetSize<T>();
            byte[] buffer = new byte[size];
            var ptr = UnsafeUtility.AddressOf(ref value);
            fixed (byte* dstPtr = buffer)
            {
                UnsafeUtility.MemCpy(dstPtr, ptr, size);
            }
            return buffer;
        }

        internal static unsafe bool ValueEquals<TValueType>(ref TValueType a, ref TValueType b) where TValueType : unmanaged
        {
            var aptr = UnsafeUtility.AddressOf(ref a);
            var bptr = UnsafeUtility.AddressOf(ref b);

            return UnsafeUtility.MemCmp(aptr, bptr, sizeof(TValueType)) == 0;
        }



        struct ByteIntStruct
        {
            public byte b;
            public int i;
        }

        enum ByteEnum : byte
        {
            None,
            Item1,
        }

        enum IntEnum : int
        {
            None,
            Item1,
        }


        //Assert.AreEqual(GetSigleBitsLength(0), 0);
        //Assert.AreEqual(GetSigleBitsLength(1<<0), 1);
        //Assert.AreEqual(GetSigleBitsLength(1 << 1), 2);
        //Assert.AreEqual(GetSigleBitsLength(1 << 2), 3);

        byte GetSigleBitsLength(uint bits)
        {
            if (bits == 0)
                return 0;
            byte length = 0;
            while (bits != 0)
            {
                bits >>= 1;
                length++;
            }
            return length;
        }
    }
}