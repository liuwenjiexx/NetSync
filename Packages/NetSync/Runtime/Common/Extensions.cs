using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Yanmonet.NetSync
{
    static partial class Extensions
    {
        public static Type GetGenericTypeDefinition(this Type type, Type genericType)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));
            if (genericType == null) throw new ArgumentNullException(nameof(genericType));
            if (!genericType.IsGenericType) throw new ArgumentException("Not Is Generic Type", nameof(genericType));

            if (genericType.IsClass)
            {
                Type t = type;
                while (t != null)
                {
                    if (t.IsGenericType && t.GetGenericTypeDefinition() == genericType)
                        return t;
                    t = t.BaseType;
                }
            }
            else
            {
                foreach (var it in type.GetInterfaces())
                {
                    if (it.IsGenericType && it.GetGenericTypeDefinition() == genericType)
                        return it;
                }
            }
            return null;
        }
        public static void ToBytes<T>(this T structObj, byte[] buff)
             where T : struct
        {
            int structSize = buff.Length;
            IntPtr ptr = Marshal.AllocHGlobal(structSize);
            try
            {
                Marshal.StructureToPtr(structObj, ptr, false);
                Marshal.Copy(ptr, buff, 0, structSize);
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }
        }

        public static T ToStruct<T>(this byte[] buff)
            where T : struct
        {
            Type strcutType = typeof(T);
            int structSize = buff.Length;
            IntPtr ptr = Marshal.AllocHGlobal(structSize);
            try
            {
                Marshal.Copy(buff, 0, ptr, structSize);
                return (T)Marshal.PtrToStructure(ptr, strcutType);
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }
        }



    }
}
