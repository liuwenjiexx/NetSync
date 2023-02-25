using System;
using System.Reflection;
using UnityEngine;

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

        public override void Serialize(NetworkWriter writer)
        {

            writer.WriteByte(action);
            writer.WriteNetworkInstanceId(netObj.InstanceId);
            switch (action)
            {
                case Action_RpcClient:
                case Action_RpcServer:
                    writer.WriteByte(rpcInfo.memberIndex);
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
                                Debug.LogError($"Write parameter error  method: {rpcInfo.method.DeclaringType}: {rpcInfo.method.Name} param: {pInfo.Name}, paramType: {pInfo.ParameterType}");
                            }
                            else
                            {
                                Debug.LogError($"Write parameter error  method: {rpcInfo.method.DeclaringType}: {rpcInfo.method.Name}");
                            }
                            throw ex;
                        }
                    }
                    break;
            }

        }


        public override void Deserialize(NetworkReader reader)
        {

            action = reader.ReadByte();

            if (action == 0)
                throw new Exception("action is 0");

            NetworkInstanceId instanceId;
            instanceId = reader.ReadNetworkInstanceId();

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

                    byte memberIndex = reader.ReadByte();
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
