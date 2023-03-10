using System;
using System.Reflection;

namespace Yanmonet.NetSync
{
    internal class RpcMessage : MessageBase
    {
        public uint methodId;
        public byte action;
        public NetworkObject netObj;
        public object[] args;
        public RpcInfo rpcInfo;
        public NetworkConnection conn;


        public const byte Action_RpcClient = 1;
        public const byte Action_RpcServer = 2;


        public static RpcMessage RpcClient(NetworkObject netObj, RpcInfo rpcInfo, object[] args)
        {
            return new RpcMessage()
            {
                methodId = rpcInfo.methodId,
                action = Action_RpcClient,
                netObj = netObj,
                rpcInfo = rpcInfo,
                args = args,
            };
        }
        public static RpcMessage RpcServer(NetworkObject netObj, RpcInfo rpcInfo, object[] args)
        {
            return new RpcMessage()
            {
                methodId = rpcInfo.methodId,
                action = Action_RpcServer,
                netObj = netObj,
                rpcInfo = rpcInfo,
                args = args,
            };
        }

        public override void Serialize(IReaderWriter writer)
        {
            writer.SerializeValue(ref methodId);
            writer.SerializeValue(ref action);
            writer.SerializeValue(ref netObj.objectId);
            switch (action)
            {
                case Action_RpcClient:
                case Action_RpcServer:
                    if (rpcInfo.paramCount > 0)
                    {
                        ParameterInfo pInfo = null;

                        if (rpcInfo.paramCount != args.Length)
                        {
                            throw new Exception($"Rpc '{rpcInfo.method}' parameter count: {rpcInfo.paramCount},  args count: {args.Length}");
                        }
                        try
                        {
                            for (int i = 0, j = 0, len = rpcInfo.paramCount; i < len; i++)
                            {
                                pInfo = rpcInfo.parameters[i];

                                if (i == 0 && pInfo.ParameterType == typeof(NetworkConnection))
                                    continue;
                                object arg = args[j++];

                                SyncVarMessage.Write(writer, pInfo.ParameterType, arg);

                            }
                        }
                        catch (Exception ex)
                        {
                            if (pInfo != null)
                            {
                                conn.NetworkManager.Log($"Write parameter error  method: {rpcInfo.method.DeclaringType}: {rpcInfo.method.Name} param: {pInfo.Name}, paramType: {pInfo.ParameterType}");
                            }
                            else
                            {
                                conn.NetworkManager.Log($"Write parameter error  method: {rpcInfo.method.DeclaringType}: {rpcInfo.method.Name}");
                            }
                            throw ex;
                        }
                    }
                    break;
            }

        }


        public override void Deserialize(IReaderWriter reader)
        {

            reader.SerializeValue(ref methodId);
            reader.SerializeValue(ref action);

            if (action == 0)
                throw new Exception("action is 0");
            ulong instanceId = new();
            reader.SerializeValue(ref instanceId);

            rpcInfo = RpcInfo.GetRpcInfo(methodId);
            netObj = null;

            if (conn.NetworkManager.IsServer)
            {
                netObj = conn.NetworkManager.Server.GetObject(instanceId);
            }
            else
            {
                netObj = conn.GetObject(instanceId);
            }

            if (netObj == null)
            {
                NetworkManager.Singleton.Log($"Rpc '{rpcInfo.methodSignature}', Get Object null, instance: {instanceId}");
                return;
            }

            switch (action)
            {
                case Action_RpcClient:
                case Action_RpcServer:

                    if (action == Action_RpcClient)
                    {
                        if (!netObj.NetworkManager.IsClient)
                            return;
                    }
                    else if (action == Action_RpcServer)
                    {
                        if (!netObj.NetworkManager.IsServer)
                            return;
                    }

                    object[] args = null;
                    if (rpcInfo.paramCount > 0)
                    {
                        args = new object[rpcInfo.paramCount];
                        for (int i = 0; i < rpcInfo.paramCount; i++)
                        {
                            var pInfo = rpcInfo.parameters[i];
                            if (i == 0 && pInfo.ParameterType == typeof(NetworkConnection))
                            {
                                args[i] = conn;
                            }
                            else
                            {
                                args[i] = SyncVarMessage.Read(reader, pInfo.ParameterType);
                            }
                        }
                    }
                    try
                    {
                        rpcInfo.method.Invoke(netObj, args);
                    }
                    catch (TargetInvocationException ex)
                    {
                        netObj.NetworkManager.Log($"Rpc invoke error, target: {netObj}, method: {rpcInfo.method.DeclaringType.Name}.{rpcInfo.method.Name}, args: [{(args == null ? "" : string.Join(", ", args))}]");
                        netObj.NetworkManager.LogException(ex.InnerException);
                    }
                    catch (Exception ex)
                    {
                        netObj.NetworkManager.Log($"Rpc invoke error, target: {netObj}, method: {rpcInfo.method.DeclaringType.Name}.{rpcInfo.method.Name}, args: [{(args == null ? "" : string.Join(", ", args))}]");
                        netObj.NetworkManager.LogException(ex);
                    }
                    break;
            }
        }



    }
}
