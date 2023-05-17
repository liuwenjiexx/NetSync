﻿using System;
using System.Collections.Generic;
using System.Reflection;

namespace Yanmonet.Network.Sync
{
    internal class RpcMessage : MessageBase
    {
        public uint methodId;
        public byte action;
        public NetworkObject netObj;
        public object[] args;
        public RpcInfo rpcInfo;
        public NetworkManager netMgr;


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
           // UnityEngine.Debug.Log("====== Rpc Serialize: " + rpcInfo.method.Name + ", " + methodId);
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

                        //if (rpcInfo.paramCount != args.Length)
                        //{
                        //    throw new Exception($"Rpc '{rpcInfo.method}' parameter count: {rpcInfo.paramCount},  args count: {args.Length}");
                        //}
                        try
                        {
                            for (int i = 0, j = 0, len = rpcInfo.paramCount; i < len; i++)
                            {
                                pInfo = rpcInfo.parameters[i];

                                if (pInfo.ParameterType == typeof(ServerRpcParams) || pInfo.ParameterType == typeof(ClientRpcParams))
                                    continue;
                                object arg = args[j++];

                                SyncVarMessage.Write(writer, pInfo.ParameterType, arg);

                            }
                        }
                        catch (Exception ex)
                        {
                            if (pInfo != null)
                            {
                                netMgr.Log($"Write parameter error  method: {rpcInfo.method.DeclaringType}: {rpcInfo.method.Name} param: {pInfo.Name}, paramType: {pInfo.ParameterType}");
                            }
                            else
                            {
                                netMgr.Log($"Write parameter error  method: {rpcInfo.method.DeclaringType}: {rpcInfo.method.Name}");
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

            //UnityEngine.Debug.Log("====== Rpc Deserialize: " + methodId);
            rpcInfo = RpcInfo.GetRpcInfo(methodId);

            netObj = netMgr.GetObject(instanceId);

            if (netObj == null)
            {
                netMgr.Log($"Rpc '{rpcInfo.methodSignature}', Get Object null, instance: {instanceId}");
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
                        ParameterInfo pInfo = null;
                        try
                        {
                            args = new object[rpcInfo.paramCount];
                            for (int i = 0; i < rpcInfo.paramCount; i++)
                            {
                                pInfo = rpcInfo.parameters[i];
                                if (pInfo.ParameterType == typeof(ServerRpcParams))
                                {
                                    args[i] = new ServerRpcParams();
                                }
                                else if (pInfo.ParameterType == typeof(ClientRpcParams))
                                {
                                    args[i] = new ClientRpcParams();
                                }
                                else
                                {
                                    args[i] = SyncVarMessage.Read(reader, pInfo.ParameterType);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            if (pInfo != null)
                            {
                                netObj.NetworkManager.LogError($"Rpc read parameter error: {pInfo.Name}, method: {rpcInfo.method.Name}");
                            }
                            netObj.NetworkManager.LogException(ex);
                        }
                    }
                    try
                    {
                        rpcInfo.method.Invoke(netObj, args);
                    }
                    catch (TargetInvocationException ex)
                    {
                        netObj.NetworkManager.LogError($"Rpc invoke error, target: {netObj.GetType().Name}, method: {rpcInfo.method.DeclaringType.Name}.{rpcInfo.method.Name}, args: [{(args == null ? "" : string.Join(", ", args))}]");
                        netObj.NetworkManager.LogException(ex.InnerException);
                    }
                    catch (Exception ex)
                    {
                        netObj.NetworkManager.LogError($"Rpc invoke error, target: {netObj.GetType().Name}, method: {rpcInfo.method.DeclaringType.Name}.{rpcInfo.method.Name}, args: [{(args == null ? "" : string.Join(", ", args))}]");
                        netObj.NetworkManager.LogException(ex);
                    }
                    break;
            }
        }



    }

    public struct ServerRpcParams
    {
    }
    public struct ClientRpcParams
    {
        public List<ulong> clients;

        public static ClientRpcParams FromReceiver(ulong clientId)
        {
            ClientRpcParams clientRpcParams = new ClientRpcParams();
            var receiver = new List<ulong>
            {
                clientId
            };
            clientRpcParams.clients = receiver;
            return clientRpcParams;
        }

        public static ClientRpcParams FromReceiver(List<ulong> clientIds)
        {
            ClientRpcParams clientRpcParams = new ClientRpcParams();
            clientRpcParams.clients = clientIds;
            return clientRpcParams;
        }



        public static implicit operator ClientRpcParams(ulong receiverClientId) => FromReceiver(receiverClientId);

        public static implicit operator ClientRpcParams(List<ulong> receiverClientIds) => FromReceiver(receiverClientIds);

    }
}
