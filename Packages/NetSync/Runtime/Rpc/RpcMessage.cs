using System;
using System.Reflection;

namespace Yanmonet.NetSync
{
    internal class RpcMessage : MessageBase
    {
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
                action = Action_RpcServer,
                netObj = netObj,
                rpcInfo = rpcInfo,
                args = args,
            };
        }

        public override void Serialize(IReaderWriter writer)
        {

            writer.SerializeValue(ref action);
            writer.SerializeValue(ref netObj.instanceId);
            switch (action)
            {
                case Action_RpcClient:
                case Action_RpcServer:
                    writer.SerializeValue(ref rpcInfo.memberIndex);
                    if (rpcInfo.paramCount > 0)
                    {
                        ParameterInfo pInfo = null;
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
                                NetworkUtility.Log($"Write parameter error  method: {rpcInfo.method.DeclaringType}: {rpcInfo.method.Name} param: {pInfo.Name}, paramType: {pInfo.ParameterType}");
                            }
                            else
                            {
                                NetworkUtility.Log($"Write parameter error  method: {rpcInfo.method.DeclaringType}: {rpcInfo.method.Name}");
                            }
                            throw ex;
                        }
                    }
                    break;
            }

        }


        public override void Deserialize(IReaderWriter reader)
        {

            reader.SerializeValue(ref action);

            if (action == 0)
                throw new Exception("action is 0");

            NetworkInstanceId instanceId = new();
            reader.SerializeValue(ref instanceId);

            netObj = null;

            netObj = conn.GetObject(instanceId);
            if (netObj == null)
                return;

            switch (action)
            {
                case Action_RpcClient:
                case Action_RpcServer:

                    if (action == Action_RpcClient)
                    {
                        if (!netObj.IsClient)
                            return;
                    }
                    else if (action == Action_RpcServer)
                    {
                        if (!netObj.IsServer)
                            return;
                    }

                    byte memberIndex = 0;
                    reader.SerializeValue(ref memberIndex);
                    rpcInfo = RpcInfo.GetRpcInfo(netObj.GetType(), memberIndex);
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

                    rpcInfo.method.Invoke(netObj, args);
                    break;
            }
        }



    }
}
