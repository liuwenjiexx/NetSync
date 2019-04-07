using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Net
{
    internal class RpcInfo
    {
        public byte memberIndex;
        public MethodInfo method;
        public ParameterInfo[] parameters;
        public int paramCount;

        private static Dictionary<Type, RpcInfo[]> cachedRpcs;

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

        public static RpcInfo[] GetRpcInfos(Type type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            if (cachedRpcs == null)
                cachedRpcs = new Dictionary<Type, RpcInfo[]>();
            RpcInfo[] infos;
            if (!cachedRpcs.TryGetValue(type, out infos))
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
                        if (pInfo.ParameterType == typeof(NetworkConnection))
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
                    info.paramCount = info.parameters.Length;
                    list.Add(info);
                }

                if (list != null && list.Count > 0)
                {
                    infos = list.OrderBy(o => o.method.Name).ToArray();
                    for (int i = 0; i < infos.Length; i++)
                    {
                        var info = infos[i];
                        info.memberIndex = (byte)i;
                    }
                    cachedRpcs[type] = infos;
                }
            }
            return infos;
        }

    }
}
