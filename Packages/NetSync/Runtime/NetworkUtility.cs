using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using System.Reflection;
using System.IO;

namespace Yanmonet.NetSync
{
    public static class NetworkUtility
    {

        public static string GetMethodSignature(MethodInfo method)
        {
            Type type = method.DeclaringType;
            string assemblyName = type.Assembly.GetName().Name;
            return $"{assemblyName}.dll / {method.ReturnType.FullName} {type.FullName}::{method.Name}({string.Join(",", method.GetParameters().Select(o => o.ParameterType.FullName))})";
        }

        public static uint GetMethodSignatureHash(MethodInfo method)
        {
            string sign = GetMethodSignature(method);
            return XXHash.Hash32(sign);
        }

        public static string GetFieldSignature(FieldInfo field)
        {
            Type type = field.DeclaringType;
            string assemblyName = type.Assembly.GetName().Name;
            return $"{assemblyName}.dll / {field.FieldType.FullName} {type.FullName}::{field.Name}";
        }
        public static uint GetFieldSignatureHash(FieldInfo field)
        {
            string sign = GetFieldSignature(field);
            return XXHash.Hash32(sign);
        }

        static Pool<NetworkWriter> writePool;
        public static NetworkWriter GetWriter()
        {
            if (writePool == null)
            {
                writePool = new Pool<NetworkWriter>(() => new NetworkWriter(new MemoryStream()));
            }

            var s = writePool.Get();
            
            return s;
        }

        public static void UnusedWriter(NetworkWriter writer)
        {
            writer.refCount--;
            if (writer.refCount > 0)
            {
                return;
            }
            writePool.Unused(writer);
        }

        public static byte[] PackMessage(ushort msgId, MessageBase msg = null)
        {
            NetworkWriter s;
            //NetworkManager.Log($"Send Msg: {(msgId < (int)NetworkMsgId.Max ? (NetworkMsgId)msgId : msgId)}");
            s = GetWriter();

            s.BaseStream.Position = 0;
            s.BaseStream.SetLength(0);
            s.BeginWritePackage();
            s.SerializeValue(ref msgId);
            if (msg != null)
            {
                msg.Serialize(s);
            }

            s.EndWritePackage();
            byte[] bytes = new byte[s.BaseStream.Length];
            s.BaseStream.Position = 0;
            s.BaseStream.Read(bytes, 0, bytes.Length);
            writePool.Unused(s);
            return bytes;
        }

    }
}
