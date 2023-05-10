#if UNITY_ENGINE
using UnityEngine;
#endif
using System;
using System.Collections.Generic;
using Yanmonet.Network.Sync.Messages;
using System.IO;
using System.Linq;
using System.Threading;

namespace Yanmonet.Network.Sync
{

    public class NetworkManager
    {
        //public string address = "127.0.0.1";
        //public string listenAddress = "0.0.0.0";
        //public int port = 7777;
        //private NetworkClient serverClient;
        private NetworkClient localClient;
        internal Dictionary<ushort, NetworkMessageDelegate> msgHandlers;

        public const ulong ServerClientId = 0;
        public Action<string> LogCallback;
        private HashSet<ulong> destoryObjIds;
        public string ConnectFailReson;
        private LogLevel logLevel = LogLevel.Error;

        public NetworkManager()
        {
            InitializeIntegerSerialization();
            InitalizeMessageHandler();

            if (Singleton == null)
            {
                Singleton = this;
            }
        }


        internal uint NextObjectId;
        internal Dictionary<ulong, NetworkObject> objects;
        private LinkedList<NetworkObject> spawnedObjects;

        public IReadOnlyCollection<NetworkObject> SpawnedObjects => spawnedObjects;


        public ulong LocalClientId { get; internal set; }

        public bool IsServer { get; private set; }


        public bool IsClient { get; private set; }

        public bool IsConnectedClient { get; internal set; }

        internal NetworkClient LocalClient => localClient;


        public bool IsHost => IsServer && IsClient;

        private ulong NextClientId;

        private Dictionary<ulong, NetworkClient> transportToClients;
        internal Dictionary<ulong, NetworkClient> clients;
        internal List<ulong> clientIds;


        public event Action<NetworkManager, ulong> ClientConnected;
        public event Action<NetworkManager, ulong> ClientDisconnected;


        public event Action<NetworkObject> ObjectCreated;
        public event Action<NetworkObject> ObjectSpawned;
        public event Action<NetworkObject> ObjectDespawned;



        //internal IReadOnlyDictionary<ulong, NetworkClient> ConnnectedClients
        //{
        //    get
        //    {
        //        if (!IsServer) throw new NotServerException("ConnnectedClients only access on server");
        //        return clients;
        //    }
        //}
        //internal IReadOnlyCollection<NetworkClient> ConnnectedClientList
        //{
        //    get
        //    {
        //        if (!IsServer) throw new NotServerException("ConnnectedClientList only access on server");
        //        return clientList;
        //    }
        //}

        public IReadOnlyList<ulong> ConnectedClientIds
        {
            get
            {
                if (!IsServer) throw new NotServerException($"{nameof(ConnectedClientIds)} only access on server");
                return clientIds;
            }
        }

        public int Version;
        public byte[] ConnectionData = new byte[0];

        public ValidateConnectDelegate ValidateConnect;
        public delegate byte[] ValidateConnectDelegate(byte[] payload);

        public static NetworkManager Singleton { get; private set; }

        private INetworkTransport transport;
        public INetworkTransport Transport { get => transport; set => transport = value; }

        public string Address { get; set; }


        private void Initalize()
        {
            if (transport == null) throw new Exception("Transport null");

            IsServer = false;
            IsClient = false;
            clients = new();
            clientIds = new();
            destoryObjIds = new();
            transportToClients = new();
            objects = new();
            spawnedObjects = new();
            startTime = DateTime.Now;
            NextClientId = 0;
            NextObjectId = 0;
            ConnectFailReson = null;
        }

        void InitalizeMessageHandler()
        {
            msgHandlers = new Dictionary<ushort, NetworkMessageDelegate>();

            msgHandlers = new Dictionary<ushort, NetworkMessageDelegate>();
            msgHandlers[(ushort)NetworkMsgId.ConnectRequest] = OnMessage_ConnectRequest;
            msgHandlers[(ushort)NetworkMsgId.ConnectResponse] = OnMessage_ConnectResponse;
            msgHandlers[(ushort)NetworkMsgId.CreateObject] = OnMessage_CreateObject;
            msgHandlers[(ushort)NetworkMsgId.Despawn] = OnMessage_DespawnObject;
            msgHandlers[(ushort)NetworkMsgId.Spawn] = OnMessage_SpawnObject;
            msgHandlers[(ushort)NetworkMsgId.SyncVar] = OnMessage_SyncVar;
            msgHandlers[(ushort)NetworkMsgId.Rpc] = OnMessage_Rpc;
            msgHandlers[(ushort)NetworkMsgId.Ping] = OnMessage_Ping;

        }

        static bool serializationInitalized;

        internal static void InitializeIntegerSerialization()
        {
            if (serializationInitalized)
                return;
            serializationInitalized = true;

            Sync<byte>.Serializer = new UInt8Serializer();
            Sync<byte>.AreEqual = Sync<byte>.ValueEquals;
            Sync<short>.Serializer = new Int16Serializer();
            Sync<short>.AreEqual = Sync<short>.ValueEquals;
            Sync<ushort>.Serializer = new UInt16Serializer();
            Sync<ushort>.AreEqual = Sync<ushort>.ValueEquals;
            Sync<int>.Serializer = new Int32Serializer();
            Sync<int>.AreEqual = Sync<int>.ValueEquals;
            Sync<uint>.Serializer = new UInt32Serializer();
            Sync<uint>.AreEqual = Sync<uint>.ValueEquals;
            Sync<long>.Serializer = new Int64Serializer();
            Sync<long>.AreEqual = Sync<long>.ValueEquals;
            Sync<ulong>.Serializer = new UInt64Serializer();
            Sync<ulong>.AreEqual = Sync<ulong>.ValueEquals;
            Sync<float>.Serializer = new Float32Serializer();
            Sync<float>.AreEqual = Sync<float>.ValueEquals;
            Sync<double>.Serializer = new Float64Serializer();
            Sync<double>.AreEqual = Sync<double>.ValueEquals;
            Sync<bool>.Serializer = new BoolSerializer();
            Sync<bool>.AreEqual = Sync<bool>.ValueEquals;
            Sync<string>.Serializer = new StringSerializer();
            Sync<string>.AreEqual = Sync<bool>.EqualityEqualsObject;
            Sync<Guid>.Serializer = new GuidSerializer();
            Sync<Guid>.AreEqual = Sync<Guid>.ValueEquals;
        }
        private DateTime startTime;


        private float NowTime
        {
            get
            {
                return (float)DateTime.Now.Subtract(startTime).TotalSeconds;
            }
        }


        public void StartHost()
        {
            Initalize();
            IsServer = true;
            IsClient = true;
            LocalClientId = ServerClientId;

            try
            {
                transport.Initialize(this);
                if (!transport.StartServer())
                {
                    transport.DisconnectLocalClient();
                    IsServer = false;
                    IsClient = false;
                    return;
                }

                if (ValidateConnect != null)
                {
                    try
                    {
                        ValidateConnect?.Invoke(ConnectionData);
                    }
                    catch (Exception ex)
                    {
                        ConnectFailReson = ex.Message;
                        throw ex;
                    }
                }

                OnServerTransportConnect(transport.ServerClientId);
                var client = clients[ServerClientId];
                client.isConnected = true;
                IsConnectedClient = true;
                ClientConnected?.Invoke(this, client.ClientId);
            }
            catch
            {
                Shutdown();
                throw;
            }
        }

        public void StartServer()
        {
            Initalize();
            IsServer = true;
            IsClient = false;
            LocalClientId = ServerClientId;

            try
            {
                transport.Initialize(this);
                if (!transport.StartServer())
                {
                    transport.DisconnectLocalClient();
                    IsServer = false;
                    return;
                }

            }
            catch
            {
                Shutdown();
                throw;
            }
        }

        public void StartClient()
        {
            Initalize();
            IsServer = false;
            IsClient = true;
            LocalClientId = ulong.MaxValue;

            try
            {
                transport.Initialize(this);
                if (!transport.StartClient())
                {
                    transport.DisconnectLocalClient();
                    IsClient = false;
                    return;
                }

                //NetworkEvent @event;
                //transport.PollEvent(out @event);

                //if (@event.Type != NetworkEventType.Connect)
                //    throw new Exception($"Not Connect Network Event, Event: {@event.Type}");

                //localClient = new NetworkClient(this);
                //localClient.transportClientId = @event.ClientId;

                //float timeout = NowTime + 10;
                //while (true)
                //{
                //    Update();

                //    if (!IsClient)
                //        return;
                //    if (localClient.isConnected)
                //    {
                //        break;
                //    }

                //    if (NowTime > timeout)
                //    {
                //        throw new TimeoutException();
                //    }

                //    Thread.Sleep(5);
                //}

                //localClient.ClientId = LocalClientId;

                //Connected?.Invoke(this);

            }
            catch
            {
                Shutdown();
                throw;
            }
        }

        public bool IsConnected(ulong clientId)
        {
            if (!IsServer) new NotServerException();
            if (clientId == ServerClientId)
                return true;
            return clientIds.Contains(clientId);
        }


        #region Network Object

        internal uint GetTypeId(Type type)
        {
            uint objectId = type.Hash32();
            return objectId;
        }

        public void RegisterObject<T>()
            where T : NetworkObject, new()
        {
            RegisterObject(typeof(T), typeId => new T());
        }

        public void RegisterObject<T>(CreateObjectDelegate create, DestroyObjectDelegate destrory = null)
        {
            RegisterObject(typeof(T), create, destrory);
        }

        public void RegisterObject(Type type, CreateObjectDelegate create, DestroyObjectDelegate destrory = null)
        {
            ulong objectId = GetTypeId(type);

            RegisterObject(objectId, type, create, destrory);
        }

        private void RegisterObject(ulong objectId, Type type, CreateObjectDelegate create, DestroyObjectDelegate destrory = null)
        {
            if (create == null)
                throw new ArgumentNullException(nameof(create));

            if (type != null)
            {
                SyncVarInfo.GetSyncVarInfos(type);
                RpcInfo.GetRpcInfos(type);
            }

            NetworkObjectInfo info = new NetworkObjectInfo()
            {
                typeId = objectId,
                create = create,
                destroy = destrory,
                type = type,
            };
            NetworkObjectInfo.Add(info);
        }

        public void UnregisterObject(Type type)
        {
            var objectId = GetTypeId(type);
            NetworkObjectInfo.Remove(objectId);

        }


        public T CreateObject<T>()
          where T : NetworkObject
        {
            T instance;
            var typeId = GetTypeId(typeof(T));
            instance = (T)CreateObject(typeId);
            return instance;
        }

        public NetworkObject CreateObject(uint typeId)
        {
            var objInfo = NetworkObjectInfo.Get(typeId);

            NetworkObject instance = objInfo.create(typeId);
            if (instance == null)
                throw new Exception("create object, instance null");

            instance.typeId = typeId;

            //调用 Spawn 需要
            instance.networkManager = this;
            //Debug.Log("Create Object: " + instance + ", " + transport);
            ObjectCreated?.Invoke(instance);

            return instance;
        }

        internal void SpawnObject(NetworkObject obj)
        {
            if (!objects.ContainsKey(obj.InstanceId))
                return;
            if (obj.IsSpawned)
                return;
            obj.IsSpawned = true;
            spawnedObjects.AddLast(obj);
            try
            {
                obj.OnSpawned();
            }
            catch (Exception ex) { LogException(ex); }

            try
            {
                ObjectSpawned?.Invoke(obj);
            }
            catch (Exception ex) { LogException(ex); }
        }

        internal void DespawnObject(NetworkObject obj)
        {
            if (!objects.ContainsKey(obj.InstanceId))
                return;

            if (!obj.IsSpawned)
                return;

            obj.IsSpawned = false;
            spawnedObjects.Remove(obj);

            foreach (var variable in obj.variables.Values)
            {
                //variable.networkObject = null;
            }

            try
            {
                obj.OnDespawned();
            }
            catch (Exception ex) { LogException(ex); }

            try
            {
                ObjectDespawned?.Invoke(obj);
            }
            catch (Exception ex) { LogException(ex); }

            //  obj.InstanceId = 0;

        }

        internal void DestroryObject(NetworkObject obj)
        {
            if (obj.isDestrory)
                return;
            ulong objId = obj.InstanceId;

            if (obj.IsSpawned)
            {
                DespawnObject(obj);
            }

            obj.isDestrory = true;
            objects.Remove(objId);

            var info = NetworkObjectInfo.Get(obj.typeId);

            if (info.destroy != null)
            {
                try
                {
                    info.destroy(obj);
                }
                catch (Exception ex) { LogException(ex); }
            }


            try
            {
                obj.OnDestrory();
            }
            catch (Exception ex) { LogException(ex); }


            if (obj is IDisposable)
            {
                try
                {
                    ((IDisposable)obj).Dispose();
                }
                catch (Exception ex) { LogException(ex); }
            }
        }

        internal bool ContainsObject(ulong instanceId)
        {
            return objects.ContainsKey(instanceId);
        }

        public NetworkObject GetObject(ulong instanceId)
        {
            NetworkObject obj;
            objects.TryGetValue(instanceId, out obj);
            return obj;
        }

        public void UpdateObjects()
        {

            foreach (var netObj in SpawnedObjects)
            {
                if (netObj != null)
                {
                    try
                    {
                        netObj.InternalUpdate();
                    }
                    catch (Exception ex)
                    {
                        LogException(ex);
                    }

                }
            }

            if (destoryObjIds.Count > 0)
            {
                foreach (var id in destoryObjIds)
                {
                    NetworkObject netObj;
                    if (objects.TryGetValue(id, out netObj))
                    {
                        objects.Remove(id);
                        if (netObj.IsSpawned)
                        {
                            netObj.Despawn();
                        }
                        netObj.Destrory();
                    }
                }
                destoryObjIds.Clear();
            }
        }
        #endregion


        internal void InvokeHandler(NetworkMessage netMsg)
        {
            NetworkMessageDelegate handler;



            if (msgHandlers == null || !msgHandlers.TryGetValue(netMsg.MsgId, out handler))
            {
                if (LogLevel <= LogLevel.Error)
                    LogError(netMsg.ClientId, "Unknown msgId: " + netMsg.MsgId);
                return;
            }
            try
            {

                handler(netMsg);
            }
            catch (Exception ex)
            {
                if (LogLevel <= LogLevel.Error)
                    LogError(netMsg.ClientId, "Handle message error, msgId: " + netMsg.MsgId);
                LogException(ex);
            }
        }




        internal void HostHandleMessage(ulong clientId, ushort msgId, byte[] packet)
        {
            MemoryStream ms = new MemoryStream(packet, 0, packet.Length, true, true);
            ms.Position = 0;
            ms.SetLength(packet.Length);
            NetworkReader reader = new NetworkReader(ms);

            NetworkMessage netMsg = new NetworkMessage();
            netMsg.MsgId = msgId;
            netMsg.NetworkManager = this;
            netMsg.ReceiverId = clientId;
            netMsg.Reader = reader;
            netMsg.rawPacket = packet;
            InvokeHandler(netMsg);
        }

        public void Update()
        {

            NetworkEvent evt;
            ulong? clientId = null;
            NetworkClient client;
            while (transport.PollEvent(out evt))
            {
                clientId = null;
                client = null;

                if (evt.ClientId == transport.ServerClientId)
                {
                    clientId = ServerClientId;
                }
                //else if (IsClient && localClient != null && localClient.transportClientId == evt.SenderId)
                //{
                //    senderId = localClient.clientId;
                //    client = localClient;
                //}
                else
                {
                    if (transportToClients.TryGetValue(evt.ClientId, out client))
                    {
                        clientId = client.ClientId;
                    }
                    //else if (IsClient && !IsServer)
                    //{
                    //    clientId = ulong.MaxValue;
                    //}
                }

                if (client != null)
                {
                    client.LastReceiveTime = evt.ReceiveTime;
                }

                switch (evt.Type)
                {
                    case NetworkEventType.Data:
                        {
                            /*  if (client == null)
                              {
                                  if (LogLevel <= LogLevel.Debug)
                                      Log($"Ignore receive msg, TransportClient [{evt.ClientId}] client null, ThreadId: {Thread.CurrentThread.ManagedThreadId}");
                              }
                              else*/ /*if (!client.isConnected)
                              {
                                  if (LogLevel <= LogLevel.Debug)
                                      Log($"Ignore receive msg, [{client?.clientId}] client.isConnected false, ThreadId: {Thread.CurrentThread.ManagedThreadId}");
                              }
                              else*/

                            //Unity 网络一开始会同步一些数据


                            if (clientId.HasValue)
                            {
                                NetworkMessage netMsg = new NetworkMessage();
                                netMsg.NetworkManager = this;
                                var payload = evt.Payload;
                                byte[] rawPacket = new byte[evt.Payload.Count];
                                Buffer.BlockCopy(payload.Array, payload.Offset, rawPacket, 0, payload.Count);
                                netMsg.rawPacket = rawPacket;

                                NetworkReader reader = new NetworkReader(rawPacket);

                                ushort msgId = reader.ReadUInt16();

                                netMsg.MsgId = msgId;
                                netMsg.ClientId = clientId.Value;
                                netMsg.Reader = reader;

                                if (LogLevel <= LogLevel.Debug)
                                {
                                    //Log(senderId.Value, $"Receive Message [{(NetworkMsgId)msgId}]");
                                }
                                //if (client == null)
                                //{
                                //    if (LogLevel <= LogLevel.Debug)
                                //    {
                                //        Log($"msg [{msgId},{(NetworkMsgId)msgId}],  client null clientId: {evt.ClientId} has senderId: {senderId.HasValue}, {senderId}");
                                //    }
                                //}
                                InvokeHandler(netMsg);
                            }
                            else
                            {
                                if (LogLevel <= LogLevel.Debug)
                                {
                                    Log("not senderId value");
                                }
                            }
                        }
                        break;
                    case NetworkEventType.Connect:
                        if (IsServer)
                        {
                            OnServerTransportConnect(evt.ClientId);
                        }
                        else
                        {
                            OnClientTransportConnect(evt.ClientId);
                        }
                        break;
                    case NetworkEventType.Disconnect:

                        if (IsServer)
                        {
                            OnServerTransportDisconnect(evt.ClientId);
                        }
                        else
                        {
                            OnClientTransportDisconnect(evt.ClientId);
                        }
                        break;
                    case NetworkEventType.Error:
                        {
                            LogError(clientId.Value, "Network Transport error");
                            Shutdown();
                        }
                        break;

                }
            }

            var objNode = spawnedObjects.First;
            LinkedListNode<NetworkObject> next;
            NetworkObject obj;
            while (objNode != null)
            {
                next = objNode.Next;
                obj = objNode.Value;
                obj.InternalUpdate();
                objNode = next;
            }


        }




        private void OnServerTransportConnect(ulong transportClientId)
        {
            NetworkClient client;
            if (transportToClients.ContainsKey(transportClientId))
                return;

            client = new NetworkClient(this);
            ulong clientId;
            if (transportClientId == transport.ServerClientId)
            {
                clientId = ServerClientId;
            }
            else
            {
                clientId = ++NextClientId;
            }


            client.transportClientId = transportClientId;
            client.ClientId = clientId;
            client.isConnected = false;
            //NetworkManager.Log($"Accept Client {connId}, IsConnecting: {client.Connection.IsConnecting}, IsRunning: {client.IsRunning}");

            transportToClients[transportClientId] = client;
            clients[client.ClientId] = client;
            clientIds.Add(client.ClientId);

            //try
            //{
            //    ClientConnected?.Invoke(this, clientId);
            //}
            //catch (Exception ex) { LogException(ex); }

        }
        private void OnClientTransportConnect(ulong transportClientId)
        {
            NetworkClient client;
            if (localClient != null && localClient.transportClientId == transportClientId)
                return;

            client = new NetworkClient(this);

            client.transportClientId = transportClientId;
            client.ClientId = ulong.MaxValue;
            client.isConnected = false;

            transportToClients[transportClientId] = client;
            localClient = client;

            var connectRequest = new ConnectRequestMessage()
            {
                Payload = ConnectionData,
            };

            if (LogLevel <= LogLevel.Debug)
            {
                //Log($"[Client] Send Message: {NetworkMsgId.ConnectRequest}");
            }

            transport.SendMessage(transport.ServerClientId, new ArraySegment<byte>(PackMessage((ushort)NetworkMsgId.ConnectRequest, connectRequest)), NetworkDelivery.ReliableSequenced);


        }

        private void OnServerTransportDisconnect(ulong transportClientId)
        {
            NetworkClient client;
            //if (LogLevel <= LogLevel.Debug)
            //    Log($"OnServerTransportDisconnect  transportClientId: {transportClientId}");
            if (!transportToClients.TryGetValue(transportClientId, out client))
            {
                return;
            }
            ulong clientId = client.ClientId;


            var objNode = spawnedObjects.First;
            LinkedListNode<NetworkObject> next;
            while (objNode != null)
            {
                next = objNode.Next;
                var obj = objNode.Value;
                if (obj.IsSpawned)
                {
                    if (obj.observers.Contains(clientId))
                    {
                        obj.RemoveObserver(clientId);
                    }

                    if (obj.OwnerClientId == clientId)
                    {
                        obj.Despawn();
                        DestroryObject(obj);
                    }
                }

                objNode = next;
            }

            transportToClients.Remove(transportClientId);

            clientIds.Remove(clientId);
            clients.Remove(clientId);

            if (client.isConnected)
            {
                try
                {
                    if (LogLevel <= LogLevel.Debug)
                        Log($"[{clientId}] Client disconnected because the transport disconnect");
                    ClientDisconnected?.Invoke(this, clientId);
                }
                catch (Exception ex) { LogException(ex); }
                client.isConnected = false;
            }

        }

        private void OnClientTransportDisconnect(ulong transportClientId)
        {
            ulong clientId;
            NetworkClient client = LocalClient;
            //if (LogLevel <= LogLevel.Debug)
            //    Log($"OnClientTransportDisconnect  transportClientId: {transportClientId}");
            if (!IsClient)
                return;
            if (client == null || client.transportClientId != transportClientId)
                return;

            clientId = client.clientId;


            var objNode = spawnedObjects.First;
            LinkedListNode<NetworkObject> next;
            NetworkObject obj;
            while (objNode != null)
            {
                next = objNode.Next;
                obj = objNode.Value;
                if (obj.IsSpawned)
                {
                    if (obj.IsSpawned)
                    {
                        DespawnObject(obj);
                    }
                    DestroryObject(obj);
                }

                objNode = next;
            }

            if (client.isConnected)
            {
                try
                {
                    if (LogLevel <= LogLevel.Debug)
                        Log($"[{clientId}] Client disconnected because the transport disconnect");
                    ClientDisconnected?.Invoke(this, clientId);
                }
                catch (Exception ex) { LogException(ex); }
                client.isConnected = false;
            }

            Shutdown();
        }

        public bool ContainsClient(ulong clientId)
        {
            return clients.ContainsKey(clientId);
        }

        internal NetworkClient GetClient(ulong clientId)
        {
            if (clients.TryGetValue(clientId, out var client))
                return client;
            return null;
        }
        internal IEnumerable<NetworkClient> GetAvaliableClients(IEnumerable<ulong> clientIds)
        {
            foreach (var clientId in clientIds)
            {
                if (clients.TryGetValue(clientId, out var client))
                    yield return client;
            }
        }

        public void DisconnectClient(ulong clientId)
        {
            if (!IsServer) throw new NotServerException();

            var client = GetClient(clientId);
            if (client == null)
                return;

            transport.DisconnectRemoteClient(client.transportClientId);

            OnServerTransportDisconnect(client.transportClientId);
        }


        public void Shutdown()
        {
            if (IsClient)
            {
                IsClient = false;
                if (LocalClientId != ServerClientId)
                {
                    transport.DisconnectLocalClient();
                }

                if ((localClient != null && localClient.isConnected) || (LocalClientId == ServerClientId))
                {
                    try
                    {
                        if (LogLevel <= LogLevel.Debug)
                            Log($"Shutdown ClientDisconnected {LocalClientId}");
                        ClientDisconnected?.Invoke(this, LocalClientId);
                    }
                    catch (Exception ex) { LogException(ex); }
                }
                LocalClientId = ulong.MaxValue;
            }

            if (IsServer)
            {
                foreach (var client in clients.Values.ToArray())
                {
                    DisconnectClient(client.ClientId);
                }
                IsServer = false;
                clientIds.Clear();
                clients.Clear();
                transportToClients.Clear();
            }

            if (transport != null)
            {
                transport.DisconnectLocalClient();
            }

        }


        #region Send Message


        internal void SendMessage(ulong clientId, ushort msgId, MessageBase msg = null)
        {
            var s = PackMessage(msgId, msg);
            if (s == null)
                return;
            SendPacket(clientId, msgId, s);
        }

        internal void SendPacket(ulong clientId, ushort msgId, byte[] packet, NetworkDelivery delivery = NetworkDelivery.ReliableSequenced)
        {
            if (LogLevel <= LogLevel.Debug)
            {
                //Log(clientId, $"Send Msg: " + (msgId < (short)NetworkMsgId.Max ? (NetworkMsgId)msgId : msgId));
            }

            NetworkClient client = null;
            ulong transportClientId = 0;
            if (IsServer)
            {
                if (clientId == ServerClientId)
                {
                    HostHandleMessage(clientId, msgId, packet);
                }
                else
                {
                    client = GetClient(clientId);
                    transportClientId = client.ClientId;
                }
            }
            else
            {
                client = LocalClient;
                transportClientId = transport.ServerClientId;
            }

            if (client != null)
            {
                transport.SendMessage(transportClientId, new ArraySegment<byte>(packet), delivery);
            }

        }


        internal byte[] PackMessage(ushort msgId, MessageBase msg = null)
        {
            byte[] bytes = null;
            try
            {
                NetworkWriter s;
                //NetworkManager.Log($"Send Msg: {(msgId < (int)NetworkMsgId.Max ? (NetworkMsgId)msgId : msgId)}");

                s = NetworkUtility.GetWriter();

                s.BaseStream.Position = 0;
                s.BaseStream.SetLength(0);
                //s.BeginWritePackage();
                s.SerializeValue(ref msgId);
                if (msg != null)
                {
                    msg.Serialize(s);
                }

                //s.EndWritePackage();
                bytes = new byte[s.BaseStream.Length];
                s.BaseStream.Position = 0;
                s.BaseStream.Read(bytes, 0, bytes.Length);
                NetworkUtility.UnusedWriter(s);
            }
            catch (Exception ex)
            {
                if (LogLevel <= LogLevel.Error)
                {
                    LogError($"Write Message error, msgId: {msgId}, type: {msg.GetType().Name}");
                    LogException(ex);
                }
            }
            return bytes;
        }

        #endregion

        #region Receive Message


        private static void OnMessage_ConnectRequest(NetworkMessage netMsg)
        {
            var netMgr = netMsg.NetworkManager;
            if (!netMgr.IsServer) throw new NotServerException("Connect To Server Msg only server");
            var msg = netMsg.ReadMessage<ConnectRequestMessage>();

            ulong clientId = netMsg.ClientId;
            NetworkClient client = null;
            var resp = new ConnectResponseMessage();
            try
            {

                client = netMgr.GetClient(clientId);
                if (client == null)
                {
                    netMsg.NetworkManager.Log($"Get Client null, id: {clientId}");
                    return;
                }
                resp.Success = true;
                resp.clientId = clientId;

                byte[] responseData = null;
                if (netMgr.ValidateConnect != null)
                {
                    try
                    {
                        responseData = netMgr.ValidateConnect(msg.Payload);

                    }
                    catch (Exception ex)
                    {
                        netMgr.LogException(ex);
                        netMgr.DisconnectClient(clientId);
                        resp.Success = false;
                        resp.Reson = ex.Message;
                    }
                }

                if (responseData == null)
                    responseData = new byte[0];
                resp.data = responseData;
                if (netMgr.LogLevel <= LogLevel.Debug)
                    netMgr.Log($"Send Accept Client Msg, ClientId: {clientId}");
                netMgr.SendMessage(clientId, (ushort)NetworkMsgId.ConnectResponse, resp);

                if (resp.Success)
                {

                    client.isConnected = true;
                    try
                    {
                        if (netMgr.LogLevel <= LogLevel.Debug)
                            netMgr.Log($"ConnectRequest, [{clientId}] ClientConnected");
                        netMgr.ClientConnected?.Invoke(netMgr, clientId);
                    }
                    catch (Exception ex) { netMgr.LogException(ex); }
                }
                else
                {
                    netMgr.DisconnectClient(clientId);
                }
            }
            catch (Exception ex)
            {
                netMgr.LogException(ex);
                if (client != null)
                {
                    client.isConnected = false;
                }
                netMgr.DisconnectClient(clientId);

            }

        }

        private static void OnMessage_ConnectResponse(NetworkMessage netMsg)
        {
            var netMgr = netMsg.NetworkManager;
            var msg = netMsg.ReadMessage<ConnectResponseMessage>();

            if (netMgr.LocalClient == null)
            {
                netMgr.Log("LocalClient null");
                return;
            }


            NetworkClient client = netMgr.LocalClient;
            netMgr.LocalClientId = msg.clientId;
            client.clientId = msg.clientId;

            if (msg.Success)
            {
                client.isConnected = true;
                try
                {
                    if (netMgr.IsClient)
                    {
                        netMgr.IsConnectedClient = true;
                    }
                    if (netMgr.LogLevel <= LogLevel.Debug)
                        netMgr.Log($"ConnectResponse [{client.clientId}] ClientConnected");
                    netMgr.ClientConnected?.Invoke(netMgr, client.clientId);
                }
                catch (Exception ex) { netMgr.LogException(ex); }
            }
            else
            {
                if (!string.IsNullOrEmpty(msg.Reson))
                {
                    netMgr.LogError(msg.Reson);
                }
                netMgr.Shutdown();
            }
        }

        /*
        private static void OnMessage_Disconnect(NetworkMessage netMsg)
        {
            netMsg.Connection.Disconnect();
        }
        */

        private static void OnMessage_CreateObject(NetworkMessage netMsg)
        {
            var netMgr = netMsg.NetworkManager;
            if (netMgr.IsServer)
                return;

            var msg = netMsg.ReadMessage<CreateObjectMessage>();
            uint typeId = msg.typeId;
            //if (msg.toServer)
            //{
            //    conn.CheckServer();
            //    var server = conn.server;
            //    var obj = conn.server.CreateObject(objectId, netMsg);
            //    if (obj != null)
            //    {
            //        obj.ConnectionToOwner = conn;
            //        server.AddObserver(obj, conn);
            //    }
            //}
            //else
            //{

            ulong instanceId = msg.objectId;

            if (!netMgr.ContainsObject(instanceId))
            {
                NetworkObject instance = null;

                var info = NetworkObjectInfo.Get(typeId);

                instance = netMgr.CreateObject(typeId);
                if (instance == null)
                    throw new Exception("create instance null, Type id:" + typeId);
                instance.InstanceId = instanceId;
                instance.OwnerClientId = msg.ownerClientId;

                netMgr.objects[instanceId] = instance;

                if (netMgr.LogLevel <= LogLevel.Debug)
                {
                    netMgr.Log(netMsg.ClientId, $"Receive Message: Create Object [{info.type.Name}], instanceId: {instanceId}, owner: {instance.OwnerClientId}");
                }
            }
            //}
        }

        private static void OnMessage_SpawnObject(NetworkMessage netMsg)
        {
            var netMgr = netMsg.NetworkManager;
            var msg = new SpawnMessage();
            netMsg.ReadMessage(msg);

            NetworkObject netObj;

            netObj = netMgr.GetObject(msg.instanceId);

            if (netObj == null)
                return;
            if (netObj.IsSpawned)
                return;

            netObj.OwnerClientId = msg.ownerClientId;


            if (netMgr.LogLevel <= LogLevel.Debug)
            {
                netMgr.Log(netMsg.ClientId, $"Receive Message: Spawn Object [{netObj.GetType().Name}] instanceId: {msg.instanceId}, owner: {netObj.OwnerClientId}");
            }

            netMgr.SpawnObject(netObj);


        }

        private static void OnMessage_DespawnObject(NetworkMessage netMsg)
        {
            var netMgr = netMsg.NetworkManager;
            if (netMgr.IsServer)
                return;

            var msg = netMsg.ReadMessage<DespawnMessage>();

            NetworkObject instance;
            instance = netMgr.GetObject(msg.instanceId);
            if (instance == null)
                return;
            if (!instance.IsSpawned)
                return;


            if (netMgr.LogLevel <= LogLevel.Debug)
            {
                netMgr.Log(netMsg.ClientId, $"Receive Message: Despawn Object [{instance.GetType().Name}] instanceId: {instance.InstanceId}, destroy: {msg.isDestroy}");
            }

            netMgr.DespawnObject(instance);

            if (msg.isDestroy)
            {
                netMgr.DestroryObject(instance);
            }
        }

        private void OnMessage_SyncVar(NetworkMessage netMsg)
        {
            var msg = new SyncVarMessage();
            msg.netMgr = netMsg.NetworkManager;

            netMsg.ReadMessage(msg);

            if (IsServer)
            {
                //服务端收到的变量转发给其它端
                foreach (var clientId in msg.netObj.observers)
                {
                    if (clientId == msg.netObj.OwnerClientId)
                        continue;
                    SendPacket(clientId, netMsg.MsgId, netMsg.rawPacket);
                    if (LogLevel <= LogLevel.Debug)
                        Log(netMsg.ClientId, "Redirect Sync Variable");
                }
            }

        }

        private static void OnMessage_Rpc(NetworkMessage netMsg)
        {
            var msg = new RpcMessage();
            msg.netMgr = netMsg.NetworkManager;

            netMsg.ReadMessage(msg);
        }

        public static long Timestamp
        {
            get { return Utils.Timestamp; }
        }

        public LogLevel LogLevel { get => logLevel; set => logLevel = value; }

        private static void OnMessage_Ping(NetworkMessage netMsg)
        {
            var netMgr = netMsg.NetworkManager;
            var client = netMgr.GetClient(netMsg.ClientId);
            var pingMsg = netMsg.ReadMessage<PingMessage>();
            switch (pingMsg.Action)
            {
                case PingMessage.Action_Ping:
                    netMgr.SendMessage(client.ClientId, (ushort)NetworkMsgId.Ping, PingMessage.Reply(pingMsg, Timestamp));
                    break;
                case PingMessage.Action_Reply:
                    int timeout = (int)(pingMsg.ReplyTimestamp - pingMsg.Timestamp);
                    client.pingDelay = timeout;
                    break;
            }
        }

        #endregion


        #region Log

        string GetClientLogPrefix(ulong clientId)
        {
            if (IsServer)
            {
                return $"[Server] [{clientId}] ";
            }
            return $"[Client] [{clientId}] ";
        }

        public void Log(ulong clientId, string msg)
        {
            Log($"{GetClientLogPrefix(clientId)}{msg}");
        }

        public void LogError(ulong clientId, string error)
        {
            Log($"{GetClientLogPrefix(clientId)}{error}");
        }

        public void Log(string msg)
        {
            if (LogCallback != null)
            {
                LogCallback(msg);
                return;
            }

#if UNITY_ENGINE
            Debug.Log(msg);
#else
            Console.WriteLine(msg);
#endif

        }

        public void LogError(string error)
        {
            if (LogCallback != null)
            {
                LogCallback(error);
                return;
            }

#if UNITY_ENGINE
            Debug.LogError(error);
#else
            Console.WriteLine(error);
#endif

        }
        public void LogException(Exception ex)
        {
            if (LogCallback != null)
            {
                LogCallback(ex.Message + "\n" + ex.StackTrace);
                return;
            }

#if UNITY_ENGINE
            Debug.LogException(ex);
#else
            Console.WriteLine(ex.Message + "\n" + ex.StackTrace);
#endif

        }

        #endregion

        public void Dispose()
        {
            Shutdown();

            if (Singleton == this)
            {
                Singleton = null;
            }
        }

        //public override string ToString()
        //{
        //    return $"{address}:{port}";
        //}

        public override string ToString()
        {
            return transport?.ToString();
        }

    }
}