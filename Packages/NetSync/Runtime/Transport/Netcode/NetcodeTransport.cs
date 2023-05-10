#if NETCODE

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Unity.Collections;
using Unity.Netcode;
using Yanmonet.Network.Sync;

namespace Yanmonet.Network.Transport.Netcode
{
    using Debug = UnityEngine.Debug;
    using LogLevel = Sync.LogLevel;
    using NetMgr = Unity.Netcode.NetworkManager;
    using NetworkEvent = Network.Transport.NetworkEvent;
    using NetworkManager = Sync.NetworkManager;

    public class NetcodeTransport : INetworkTransport
    {
        private NetworkManager networkManager;
        private NetMgr netMgr;
        private ulong localClientId;
        private Dictionary<ulong, ulong> clientToBaseNetIds;
        private Dictionary<ulong, ulong> baseNetToClientIds;
        public NetcodeTransport Server;
        private Queue<NetworkEvent> eventQueue;
        private ulong NextClientId;
        private bool initalized;
        private Stopwatch time;
        private bool isServer;
        private string MessageName = "NetSync";
        public bool IsSupported => true;
        private Dictionary<ushort, MessageDelegate> handlers;

        public ulong ServerClientId => 0;

        public delegate void MessageDelegate(ulong senderClientId, ArraySegment<byte> payload);

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
            clientToBaseNetIds = new();
            baseNetToClientIds = new();
            NextClientId = 0;
            time = Stopwatch.StartNew();
            //netMgr.OnClientConnectedCallback += NetMgr_OnClientConnectedCallback;
            netMgr.CustomMessagingManager.RegisterNamedMessageHandler(MessageName, ReceiveMessage);
            isServer = false;
            initalized = true;
            handlers = new();
            handlers[(ushort)MsgId.ConnectResponse] = ConnectResponseHandle;
            handlers[(ushort)MsgId.Data] = DataHandle;
        }



        public static readonly List<NetcodeTransport> transports = new();

        //public override void OnNetworkSpawn()
        //{
        //    transports.Add(this);
        //    base.OnNetworkSpawn();
        //}

        //public override void OnNetworkDespawn()
        //{
        //    transports.Remove(this);
        //    base.OnNetworkDespawn();
        //}

        //private void NetMgr_OnClientConnectedCallback(ulong netId)
        //{
        //    if (mapClientIds.ContainsKey(netId))
        //        return;
        //    ulong clientId;
        //    clientId = ++NextClientId;
        //    OnClientConnect(clientId, netId);
        //}



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

            isServer = true;
            localClientId = ServerClientId;
            OnClientConnect(localClientId, netMgr.LocalClientId);


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

            localClientId = ulong.MaxValue;

            var writer = CreateWriter(MsgId.ConnectRequest, 0, new ArraySegment<byte>());
            SendNetMessage(0, writer);

            return true;
        }

        void OnClientConnect(ulong clientId, ulong baseNetId)
        {
            if (clientToBaseNetIds.ContainsKey(clientId))
                return;
            if (baseNetToClientIds.ContainsKey(baseNetId))
                return;

            clientToBaseNetIds[clientId] = baseNetId;
            baseNetToClientIds[baseNetId] = clientId;

            NetworkEvent @event = new NetworkEvent()
            {
                Type = NetworkEventType.Connect,
                ClientId = clientId,
                ReceiveTime = NowTime
            };
            if (isServer)
            {
                @event.SenderClientId = clientId;
            }
            else
            {
                @event.SenderClientId = ServerClientId;
            }
            eventQueue.Enqueue(@event);

        }


        public void DisconnectLocalClient()
        {
            if (!clientToBaseNetIds.TryGetValue(localClientId, out var netId))
                return;

            NetworkEvent @event = new NetworkEvent()
            {
                Type = NetworkEventType.Disconnect,
                ClientId = localClientId,
                ReceiveTime = NowTime
            };
            eventQueue.Enqueue(@event);

            clientToBaseNetIds.Remove(localClientId);
            baseNetToClientIds.Remove(netId);



        }

        public void DisconnectRemoteClient(ulong clientId)
        {
            if (clientToBaseNetIds.TryGetValue(clientId, out var netId))
                return;

            NetworkEvent @event = new NetworkEvent()
            {
                Type = NetworkEventType.Disconnect,
                ClientId = clientId,
                SenderClientId = clientId,
                ReceiveTime = NowTime
            };
            eventQueue.Enqueue(@event);

            clientToBaseNetIds.Remove(clientId);
            baseNetToClientIds.Remove(netId);
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
        FastBufferWriter CreateWriter(MsgId msgId, ulong clientId, Unity.Netcode.INetworkSerializable serializable)
        {
            FastBufferWriter writer = new FastBufferWriter(1000, Allocator.Temp);
            writer.WriteNetworkSerializable(serializable);
            return CreateWriter(msgId, clientId, writer.ToArray());
        }
        FastBufferWriter CreateWriter(MsgId msgId, ulong clientId, ArraySegment<byte> data)
        {
            var writer = new FastBufferWriter(1100, Allocator.Temp);

            writer.WriteValueSafe((ushort)msgId);
            //writer.WriteValueSafe(localClientId);
            writer.WriteValueSafe(clientId);
            writer.WriteValueSafe(data);
            return writer;
        }


        void SendNetMessage(ulong clientNetId, FastBufferWriter writer)
        {
            if (netMgr.IsServer)
            {
                netMgr.CustomMessagingManager.SendNamedMessage(MessageName, clientNetId, writer);
            }
            else
            {
                netMgr.CustomMessagingManager.SendNamedMessage(MessageName, NetMgr.ServerClientId, writer);
            }
        }

        public void SendMessage(ulong clientId, ArraySegment<byte> payload, NetworkDelivery delivery)
        {
            ulong netId;
            if (clientId == ServerClientId)
            {
                netId = NetMgr.ServerClientId;
            }
            else
            {
                if (!clientToBaseNetIds.TryGetValue(clientId, out netId))
                {
                    if (networkManager?.LogLevel <= LogLevel.Debug)
                        networkManager.LogError("NetcodeTransport send message fail, not clientId: " + clientId);
                    return;
                }
            }
            var writer = CreateWriter(MsgId.Data, clientId, payload);
            SendNetMessage(netId, writer);
        }


        private void ReceiveMessage(ulong senderClientId, FastBufferReader messagePayload)
        {
            ulong targetClientId;
            ArraySegment<byte> data;
            ushort msgId;
            messagePayload.ReadValueSafe(out msgId);
            messagePayload.ReadValueSafe(out targetClientId);
            messagePayload.ReadValueSafe(out data);

            switch ((MsgId)msgId)
            {
                case MsgId.ConnectRequest:
                    {
                        if (!isServer)
                            return;
                        if (!baseNetToClientIds.TryGetValue(senderClientId, out var clientId))
                        {
                            clientId = ++NextClientId;
                            OnClientConnect(clientId, senderClientId);
                        }

                        ConnectResponse response = new ConnectResponse();
                        response.clientId = clientId;

                        FastBufferWriter writer = CreateWriter(MsgId.ConnectResponse, clientId, response);

                        SendNetMessage(senderClientId, writer);
                        //networkManager.Log("======== ConnectRequest ");
                        return;
                    }
                case MsgId.ConnectResponse:
                    {
                        if (isServer) return;
                        FastBufferReader reader = new FastBufferReader(data, Allocator.Temp);
                        ConnectResponse response;
                        reader.ReadNetworkSerializable(out response);
                        if (localClientId != response.clientId)
                        {
                            localClientId = response.clientId;
                            OnClientConnect(localClientId, netMgr.LocalClientId);
                        }
                        //networkManager.Log("======== ConnectResponse senderId: " + senderClientId + ", localClientId: " + localClientId);
                        return;
                    }
                    break;
            }


            if (targetClientId != localClientId)
            {
                if (netMgr.IsServer)
                {
                    if (!clientToBaseNetIds.TryGetValue(targetClientId, out var netId))
                        return;

                    //转发消息
                    SendMessage(targetClientId, data, NetworkDelivery.ReliableSequenced);
                }
                return;
            }


            ulong senderClientId2;

            if (senderClientId == NetMgr.ServerClientId)
            {
                senderClientId2 = ServerClientId;
            }
            else
            {
                if (isServer)
                {
                    if (!baseNetToClientIds.TryGetValue(senderClientId, out senderClientId2))
                    {
                        Debug.Log($"{(isServer ? "Server" : "Client")} Unknow senderClientId: {senderClientId}");
                        return;
                    }
                }
                else
                {
                    Debug.Log($"{(isServer ? "Server" : "Client")} Unknow senderClientId: {senderClientId}");
                    return;
                }
            }


            if (!handlers.TryGetValue(msgId, out var handler))
            {
                Debug.Log($"Unknow Msg Id: {msgId}");
                return;
            }
            //Debug.Log("Receive Msg, senderClientId: " + senderClientId + " > " + senderClientId2);
            handler(senderClientId2, data);
        }

        public void Shutdown()
        {
            netMgr.CustomMessagingManager.UnregisterNamedMessageHandler(MessageName);
            if (clientToBaseNetIds != null)
            {
                foreach (var clientId in clientToBaseNetIds.Keys.ToArray())
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

            if (clientToBaseNetIds != null)
            {
                clientToBaseNetIds.Clear();
                eventQueue.Clear();
            }

            initalized = false;
        }

        void ConnectRequestHandle(ulong senderClientId, ArraySegment<byte> data)
        {

        }

        void ConnectResponseHandle(ulong senderClientId, ArraySegment<byte> data)
        {

        }

        void DataHandle(ulong senderClientId, ArraySegment<byte> data)
        {
            var evt = new NetworkEvent()
            {
                Type = NetworkEventType.Data,
                SenderClientId = senderClientId,
                ReceiveTime = NowTime,
                Payload = data
            };

            if (isServer)
            {
                evt.ClientId = senderClientId;
            }
            else
            {
                evt.ClientId = localClientId;
            }

            eventQueue.Enqueue(evt);
        }

        enum MsgId
        {
            Data,
            ConnectRequest,
            ConnectResponse,

        }

        class ConnectResponse : Unity.Netcode.INetworkSerializable
        {
            public ulong clientId;

            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : Unity.Netcode.IReaderWriter
            {
                serializer.SerializeValue(ref clientId);
            }
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