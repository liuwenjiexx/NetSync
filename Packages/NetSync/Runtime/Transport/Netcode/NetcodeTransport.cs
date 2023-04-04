#if NETCODE

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEditor.PackageManager;
using System.Management;
using System.Linq;
using System.Diagnostics;
using System.Security.Policy;

namespace Yanmonet.NetSync.Transport.Netcode
{
    using NetMgr = Unity.Netcode.NetworkManager;


    public class NetcodeTransport : NetworkBehaviour, INetworkTransport
    {
        private NetworkManager networkManager;
        private NetMgr netMgr;
        private ulong localClientId;
        private Dictionary<ulong, ulong> mapNetIds;
        private Dictionary<ulong, ulong> mapClientIds;

        private Queue<NetworkEvent> eventQueue;
        private ulong NextClientId;
        private bool initalized;
        private Stopwatch time;
        private bool isServer;

        public bool IsSupported => true;

        public ulong ServerClientId => 0;

        private float NowTime
        {
            get => (float)time.Elapsed.TotalSeconds;
        }

        public NetMgr NetcodeManager
        {
            get => netMgr;
            set => netMgr = value;
        }

        public void Initialize(NetworkManager networkManager = null)
        {
            this.networkManager = networkManager;


            if (!netMgr)
            {
                netMgr = NetMgr.Singleton;
            }
            if (!netMgr)
                throw new Exception("NetworkManager null");

            if (!(netMgr.IsClient || netMgr.IsClient))
            {
                throw new Exception("NetworkManager not start");
            }

            eventQueue = new();
            mapNetIds = new();
            mapClientIds = new();
            NextClientId = 0;
            time = Stopwatch.StartNew();
            netMgr.OnClientConnectedCallback += NetMgr_OnClientConnectedCallback;
            isServer = false;
            initalized = true;

        }

        private void NetMgr_OnClientConnectedCallback(ulong netId)
        {
            if (mapClientIds.ContainsKey(netId))
                return;
            ulong clientId;
            clientId = ++NextClientId;
            OnClientConnect(clientId, netId);
        }



        public bool StartServer()
        {
            if (!initalized)
            {
                try
                {
                    Initialize();
                }
                catch (Exception ex)
                {
                    networkManager?.LogException(ex);
                    return false;
                }
            }

            localClientId = ServerClientId;
            OnClientConnect(localClientId, netMgr.LocalClientId);

            foreach (var netId in netMgr.ConnectedClientsIds)
            {
                if (netId != netMgr.LocalClientId)
                {
                    ulong clientId = ++NextClientId;
                    OnClientConnect(clientId, netId);
                }
            }
            isServer = true;
            return true;
        }

        public bool StartClient()
        {
            if (!initalized)
            {
                try
                {
                    Initialize();
                }
                catch (Exception ex)
                {
                    networkManager?.LogException(ex);
                    return false;
                }
            }

            mapNetIds[0] = 0;
            mapClientIds[0] = 0;

            localClientId = ++NextClientId;
            OnClientConnect(localClientId, netMgr.LocalClientId);
            return true;
        }

        void OnClientConnect(ulong clientId, ulong netId)
        {
            if (mapNetIds.ContainsKey(clientId))
                return;
            if (mapClientIds.ContainsKey(netId))
                return;

            mapNetIds[clientId] = netId;
            mapClientIds[netId] = clientId;

            NetworkEvent @event = new NetworkEvent()
            {
                Type = NetworkEventType.Connect,
                ClientId = clientId,
                ReceiveTime = NowTime
            };
            eventQueue.Enqueue(@event);
        }


        public void DisconnectLocalClient()
        {
            if (!mapNetIds.TryGetValue(localClientId, out var netId))
                return;

            NetworkEvent @event = new NetworkEvent()
            {
                Type = NetworkEventType.Disconnect,
                ClientId = localClientId,
                ReceiveTime = NowTime
            };
            eventQueue.Enqueue(@event);

            mapNetIds.Remove(localClientId);
            mapClientIds.Remove(netId);

        }

        public void DisconnectRemoteClient(ulong clientId)
        {
            if (mapNetIds.TryGetValue(clientId, out var netId))
                return;

            NetworkEvent @event = new NetworkEvent()
            {
                Type = NetworkEventType.Disconnect,
                ClientId = clientId,
                ReceiveTime = NowTime
            };
            eventQueue.Enqueue(@event);

            mapNetIds.Remove(clientId);
            mapClientIds.Remove(netId);
        }
        public bool PollEvent(out NetworkEvent @event)
        {
            if (eventQueue.Count > 0)
            {
                @event = eventQueue.Dequeue();
                return true;
            }

            @event = default;
            return false;
        }

        public void Send(ulong clientId, ArraySegment<byte> payload, NetworkDelivery delivery)
        {
            if (!mapNetIds.TryGetValue(clientId, out var netId))
                return;

            if (IsServer)
            {
                Unity.Netcode.ClientRpcParams clientRpcParams = new();
                clientRpcParams.Send.TargetClientIds = new List<ulong>() { netId };
                SendClientRpc(clientId, payload, clientRpcParams);
            }
            else
            {
                SendServerRpc(clientId, payload);
            }


        }


        [Unity.Netcode.ServerRpc]
        void SendServerRpc(ulong targetClientId, ArraySegment<byte> data, Unity.Netcode.ServerRpcParams serverRpcParams = default)
        {
            if (!mapNetIds.TryGetValue(targetClientId, out var netId))
                return;


            if (targetClientId == localClientId)
            {
                byte[] data2 = new byte[data.Count];
                if (data.Count > 0)
                {
                    Array.Copy(data.Array, data.Offset, data2, 0, data.Count);
                }

                eventQueue.Enqueue(new NetworkEvent()
                {
                    Type = NetworkEventType.Data,
                    ClientId = targetClientId,
                    ReceiveTime = NowTime,
                    Payload = data2
                });
                return;
            }


            //转发消息
            Unity.Netcode.ClientRpcParams clientRpcParams = new();
            clientRpcParams.Send.TargetClientIds = new List<ulong>() { netId };
            SendClientRpc(targetClientId, data, clientRpcParams);

        }


        [Unity.Netcode.ClientRpc]
        void SendClientRpc(ulong targetClientId, ArraySegment<byte> data, Unity.Netcode.ClientRpcParams clientRpcParams = default)
        {
            if (!mapNetIds.TryGetValue(targetClientId, out var netId))
                return;

            if (targetClientId != localClientId)
                return;

            byte[] data2 = new byte[data.Count];
            if (data.Count > 0)
            {
                Array.Copy(data.Array, data.Offset, data2, 0, data.Count);
            }

            eventQueue.Enqueue(new NetworkEvent()
            {
                Type = NetworkEventType.Data,
                ClientId = targetClientId,
                ReceiveTime = NowTime,
                Payload = data2
            });
        }

        public void Shutdown()
        {
            if (mapNetIds != null)
            {
                foreach (var clientId in mapNetIds.Keys.ToArray())
                {
                    if (clientId == localClientId)
                    {
                        DisconnectLocalClient();
                    }
                    else
                    {
                        DisconnectRemoteClient(clientId);
                    }
                }
            }

            if (time != null)
            {
                time.Stop();
                time = null;
            }

            if (mapNetIds != null)
            {
                mapNetIds.Clear();
                eventQueue.Clear();
            }

            initalized = false;
        }


    }

    static class SerializationExtensions
    {
        public static void ReadValueSafe(this FastBufferReader reader, out ArraySegment<byte> data)
        {
            byte[] bytes;
            ushort length;
            reader.ReadValueSafe(out length);
            bytes = new byte[length];
            reader.ReadBytesSafe(ref bytes, length);
            data = new ArraySegment<byte>(bytes);
        }

        public static void WriteValueSafe(this FastBufferWriter writer, in ArraySegment<byte> data)
        {
            ushort length = (ushort)data.Count;
            writer.WriteValueSafe(length);
            writer.WriteBytesSafe(data.Array, data.Count, data.Offset);
        }
    }
}

#endif