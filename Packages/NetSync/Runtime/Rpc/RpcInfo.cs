﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;


namespace Yanmonet.Network.Sync
{
    internal class RpcInfo
    {
        public uint methodId;
        public MethodInfo method;
        public ParameterInfo[] parameters;
        public int paramCount;
        public string methodSignature;

        private static Dictionary<uint, RpcInfo> cachedIdMapRpcs;
        private static Dictionary<Type, RpcInfo[]> cachedTypeMapRpcs;

        public static RpcInfo GetRpcInfo(Type type, string methodName)
        {
            var infos = GetRpcInfos(type);
            var info = infos.Where(o => o.method.Name == methodName).FirstOrDefault();
            if (info == null)
                throw new Exception("not found rpc method " + type + "," + methodName);
            return info;
        }

        public static RpcInfo GetRpcInfo(Type type, int memberIndex)
        {
            var infos = GetRpcInfos(type);
            if (infos == null || infos.Length == 0)
                throw new Exception("not found rpc method " + type + ", " + memberIndex);

            return infos[memberIndex];
        }
        public static RpcInfo GetRpcInfo(uint methodId)
        {
            if (!cachedIdMapRpcs.TryGetValue(methodId, out var info))
                throw new Exception("not found rpc method, id: " + methodId);

            return info;
        }

        [DebuggerHidden]
        public static RpcInfo[] GetRpcInfos(Type type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            if (cachedTypeMapRpcs == null)
            {
                cachedTypeMapRpcs = new();
                cachedIdMapRpcs = new();
            }

            RpcInfo[] infos;
            if (!cachedTypeMapRpcs.TryGetValue(type, out infos))
            {
                List<RpcInfo> list = null;
                foreach (var mInfo in type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    var attr = (RpcAttribute)(mInfo.GetCustomAttributes(typeof(RpcAttribute), true).FirstOrDefault());
                    if (attr == null)
                        continue;

                    if (mInfo.ReturnType != typeof(void))
                        throw new Exception("method return type only [void]. " + mInfo);

                    foreach (var pInfo in mInfo.GetParameters())
                    {
                        if (pInfo.ParameterType == typeof(ServerRpcParams) ||
                            pInfo.ParameterType == typeof(ClientRpcParams))
                        {
                        }
                        else
                        {
                            if (!SyncVarMessage.CanSerializeType(pInfo.ParameterType))
                                throw new Exception("invalid parameter type :" + pInfo.ParameterType + "," + pInfo);
                        }
                        if (pInfo.IsOut)
                            throw new Exception("method parameter can't [out]. " + mInfo);
                    }

                    if (list == null)
                        list = new List<RpcInfo>();
                    RpcInfo info = new RpcInfo()
                    {
                        method = mInfo,
                        parameters = mInfo.GetParameters()
                    };
                    info.methodSignature = NetworkUtility.GetMethodSignature(mInfo);
                    info.methodId = NetworkUtility.GetMethodSignatureHash(mInfo);
                    info.paramCount = info.parameters.Length;
                    list.Add(info);
                    cachedIdMapRpcs[info.methodId] = info;
                }

                if (list != null && list.Count > 0)
                {
                    infos = list.OrderBy(o => o.method.Name).ToArray();

                    cachedTypeMapRpcs[type] = infos;
                }
                /*
                foreach(var mehtod in cachedIdMapRpcs.Values.OrderBy(o => o.methodId))
                {
                    UnityEngine.Debug.Log(mehtod.methodId + " = " + mehtod.method.Name);
                }*/
            }
            return infos;
        }

    }
}
