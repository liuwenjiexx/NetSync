﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using System.Reflection;
using System.IO;
using System.Net.NetworkInformation;
using System.Net;
using System.Net.Sockets;
#if UNITY_ENGINE
using UnityEngine;
#endif

namespace Yanmonet.Network.Sync
{
    public static class NetworkUtility
    {
        private static readonly DateTime InitializeUtcTime = new DateTime(1970, 1, 1, 0, 0, 0);

        public static long ToTimestamp(DateTime time)
        {
            return (long)time.ToUniversalTime().Subtract(InitializeUtcTime).TotalMilliseconds;
        }

        /// <summary>
        /// int.MaxValue:   1970/1/25 20:31:23
        /// uint.MaxValue:  1970/2/19 17:02:47
        /// </summary>
        public static DateTime FromTimestamp(long milliseconds)
        {
            return InitializeUtcTime.AddMilliseconds(milliseconds);
        }

        public static uint ToTimestampSeconds(DateTime time)
        {
            return (uint)time.ToUniversalTime().Subtract(InitializeUtcTime).TotalSeconds;
        }

        public static DateTime FromTimestampSeconds(long seconds)
        {
            return InitializeUtcTime.AddSeconds(seconds);
        }

        /// <summary>
        /// uint.MaxValue:  2106/2/7 6:28:15
        /// int.MaxValue:   2038/1/19 3:14:07
        /// </summary>
        public static DateTime FromTimestampSeconds(uint seconds)
        {
            return InitializeUtcTime.AddSeconds(seconds);
        }

        internal static T CreateInstance<T>()
        {
            if (typeof(T).IsValueType)
                return default;
            return (T)Activator.CreateInstance(typeof(T));
        }


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
            NetworkWriter s = null;
            byte[] bytes = null;
            try
            {
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
                bytes = new byte[s.BaseStream.Length];
                s.BaseStream.Position = 0;
                s.BaseStream.Read(bytes, 0, bytes.Length);
            }
            finally
            {
                if (s != null)
                    writePool.Unused(s);
            }
            return bytes;
        }
        public static byte[] PackMessage(ushort msgId, INetworkSerializable msg = null)
        {
            NetworkWriter s = null;
            byte[] bytes = null;
            try
            {
                //NetworkManager.Log($"Send Msg: {(msgId < (int)NetworkMsgId.Max ? (NetworkMsgId)msgId : msgId)}");
                s = GetWriter();

                s.BaseStream.Position = 0;
                s.BaseStream.SetLength(0);
                s.BeginWritePackage();
                s.SerializeValue(ref msgId);
                if (msg != null)
                {
                    msg.NetworkSerialize(s);
                }

                s.EndWritePackage();
                bytes = new byte[s.BaseStream.Length];
                s.BaseStream.Position = 0;
                s.BaseStream.Read(bytes, 0, bytes.Length);
            }
            finally
            {
                if (s != null)
                    writePool.Unused(s);
            }
            return bytes;
        }
        public static byte[] Pack(INetworkSerializable msg)
        {

            NetworkWriter s = null;
            byte[] bytes = null;
            try
            {
                s = GetWriter();
                s.BaseStream.Position = 0;
                s.BaseStream.SetLength(0);
                s.BeginWritePackage();
                if (msg != null)
                {
                    msg.NetworkSerialize(s);
                }

                s.EndWritePackage();
                bytes = new byte[s.BaseStream.Length];
                s.BaseStream.Position = 0;
                s.BaseStream.Read(bytes, 0, bytes.Length);
            }
            finally
            {
                if (s != null)
                    writePool.Unused(s);
            }
            return bytes;
        }

        private static string localIP;

        public static string LocalIP
        {
            get
            {
                if (localIP == null)
                {
                    IPAddress ip = null;
                    foreach (var ipAddr in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
                    {
                        if (ipAddr.AddressFamily == AddressFamily.InterNetwork)
                        {
                            ip = ipAddr;
                            break;
                        }
                    }
                    if (ip != null)
                    {
                        localIP = ip.ToString();
                    }
                }
                return localIP;
            }
        }

        public static bool IsTcpPortUsed(int port)
        {
            foreach (var endPoint in IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpListeners())
            {
                if (endPoint.Port == port)
                    return true;
            }
            return false;
        }

        public static bool IsUdpPortUsed(int port)
        {
            foreach (var endPoint in IPGlobalProperties.GetIPGlobalProperties().GetActiveUdpListeners())
            {
                if (endPoint.Port == port)
                    return true;
            }
            return false;
        }
        public static bool IsPortUsed(int port)
        {
            return IsUdpPortUsed(port) || IsTcpPortUsed(port);
        }

        public static HashSet<int> GetAllUsedPorts()
        {
            HashSet<int> ports = new();
            foreach (var endPoint in IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpListeners())
            {
                ports.Add(endPoint.Port);
            }
            foreach (var endPoint in IPGlobalProperties.GetIPGlobalProperties().GetActiveUdpListeners())
            {
                ports.Add(endPoint.Port);
            }
            return ports;
        }

        public static ushort FindAvaliablePort(ushort startPort)
        {
            var usedPorts = NetworkUtility.GetAllUsedPorts();
            ushort port = startPort;
            while (usedPorts.Contains(port))
            {
                port++;
            }
            return port;
        }


        public static IEnumerable<Assembly> ReferencedAssemblies(Assembly referenced)
        {
            return ReferencedAssemblies(referenced, AppDomain.CurrentDomain.GetAssemblies());
        }

        public static IEnumerable<Assembly> ReferencedAssemblies(Assembly referenced, IEnumerable<Assembly> assemblies)
        {
            string fullName = referenced.FullName;

            foreach (var ass in assemblies)
            {
                if (referenced == ass)
                {
                    yield return ass;
                }
                else
                {
                    foreach (var refAss in ass.GetReferencedAssemblies())
                    {
                        if (fullName == refAss.FullName)
                        {
                            yield return ass;
                            break;
                        }
                    }
                }
            }
        }


        public static bool TryParseIPAddress(string ipString, out string serverType, out string address, out int port)
        {
            serverType = null;
            address = null;
            port = 0;

            if (string.IsNullOrEmpty(ipString))
            {
                return false;
            }

            if (ipString.IndexOf("://") < 0)
            {
                ipString = "multiplayer://" + ipString;
            }
            if (!Uri.TryCreate(ipString, UriKind.RelativeOrAbsolute, out var uri))
            {
                return false;
            }
            serverType = uri.Scheme.ToLower();
            address = uri.Host;
            port = uri.Port;
            if (port < 0)
                port = 0;
            return true;
        }

        public static uint Hash32(byte[] bytes) => XXHash.Hash32(bytes);

        public static uint Hash32(string text) => XXHash.Hash32(text);

        public static ulong Hash64(byte[] bytes) => XXHash.Hash64(bytes);

        public static ulong Hash64(string text) => XXHash.Hash64(text);

        public static Action<string> LogCallback;

        public static void Log(string msg)
        {
            if (LogCallback != null)
            {
                LogCallback(msg);
                return;
            }

#if UNITY_ENGINE
            Debug.Log(msg);
#endif
        }

    }
}
