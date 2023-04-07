#if NETCODE

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Unity.Collections;
using Unity.Netcode;

namespace Yanmonet.NetSync.Transport.Netcode
{
    using NetMgr = Unity.Netcode.NetworkManager;


    public class NetcodeTransport : INetworkTransport
    {
        private NetworkManager networkManager;
        private NetMgr netMgr;
        private ulong localClientId;
        private Dictionary<ulong, ulong> mapNetIds;
        private Dictionary<ulong, ulong> mapClientIds;
        public NetcodeTransport Server;
        private Queue<NetworkEvent> eventQueue;
        private ulong NextClientId;
        private bool initalized;
        private Stopwatch time;
        private bool isServer;
        private string MessageName = "NetSync";
        public bool IsSupported => true;
        private Dictionary<ushort, Action<ulong, ArraySegment<byte>>> handlers;

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

            localClientId = ServerClientId;
            OnClientConnect(localClientId, netMgr.LocalClientId);


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

            localClientId = ulong.MaxValue;

            var writer = CreateWriter(MsgId.ConnectRequest, 0, new ArraySegment<byte>());
            SendNetMessage(0, writer);

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
                SenderId = clientId,
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
                SenderId = localClientId,
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
                SenderId = clientId,
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
                if (!mapNetIds.TryGetValue(clientId, out netId))
                {
                    if (networkManager?.LogLevel <= LogLevel.Debug)
                        networkManager.LogError("NetcodeTransport send message fail, not clientId: " + clientId);
                    return;
                }
            }
            var writer = CreateWriter(MsgId.Data, clientId, payload);
            SendNetMessage(netId, writer);
        }


        private void ReceiveMessage(ulong senderId, FastBufferReader messagePayload)
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
                        if (!mapClientIds.TryGetValue(senderId, out var clientId))
                        {
                            clientId = ++NextClientId;
                            OnClientConnect(clientId, senderId);
                        }

                        ConnectResponse response = new ConnectResponse();
                        response.clientId = clientId;

                        FastBufferWriter writer = CreateWriter(MsgId.ConnectResponse, clientId, response);

                        SendNetMessage(senderId, writer);
                        networkManager.Log("======== ConnectRequest ");
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
                        networkManager.Log("======== ConnectResponse " + senderId + ", " + localClientId);
                        return;
                    }
                    break;
            }


            if (targetClientId != localClientId)
            {
                if (netMgr.IsServer)
                {
                    if (!mapNetIds.TryGetValue(targetClientId, out var netId))
                        return;

                    //转发消息
                    SendMessage(targetClientId, data, NetworkDelivery.ReliableSequenced);
                }
                return;
            }


            ulong senderClientId;

            if (senderId == NetMgr.ServerClientId)
            {
                senderClientId = ServerClientId;
            }
            else
            {
                if (isServer)
                {
                    if (!mapClientIds.TryGetValue(senderId, out senderClientId))
                        return;
                }
                else
                {
                    senderClientId = ServerClientId;
                }
            }


            if (!handlers.TryGetValue(msgId, out var handler))
                return;
            handler(senderClientId, data);
        }

        public void Shutdown()
        {
            netMgr.CustomMessagingManager.UnregisterNamedMessageHandler(MessageName);
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

        void ConnectRequestHandle(ulong clientId, ArraySegment<byte> data)
        {

        }

        void ConnectResponseHandle(ulong clientId, ArraySegment<byte> data)
        {

        }

        void DataHandle(ulong clientId, ArraySegment<byte> data)
        {
            eventQueue.Enqueue(new NetworkEvent()
            {
                Type = NetworkEventType.Data,
                SenderId = clientId,
                ReceiveTime = NowTime,
                Payload = data
            });
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