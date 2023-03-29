#if UNITY_ENGINE
using UnityEngine;
using YMFramework;
#endif
using System;
using System.Collections;
using System.Collections.Generic;
using Yanmonet.NetSync.Messages;

namespace Yanmonet.NetSync
{
    public class NetworkManager
    {
        public string address = "127.0.0.1";
        public string listenAddress = "0.0.0.0";
        public int port = 7777;
        private NetworkServer server;
        private NetworkClient localClient;
        internal Dictionary<ushort, NetworkMessageDelegate> msgHandlers;

        public const ulong ServerClientId = 0;
        public Action<string> LogCallback;

        public NetworkManager()
        {
            InitializeIntegerSerialization();
            InitalizeMessageHandler();

            if (Singleton == null)
            {
                Singleton = this;
            }
        }


        public ulong LocalClientId { get; internal set; }

        public bool IsServer { get; private set; }

        public NetworkServer Server => server;

        public bool IsClient { get; private set; }

        public NetworkClient LocalClient => localClient;


        public bool IsHost => IsServer && IsClient;

        internal Dictionary<ulong, NetworkClient> clients;
        internal LinkedList<NetworkClient> clientList;
        internal LinkedList<ulong> clientIds;

        internal Dictionary<ulong, LinkedListNode<NetworkClient>> clientNodes;

        public IReadOnlyDictionary<ulong, NetworkClient> ConnnectedClients
        {
            get
            {
                if (!IsServer) throw new NotServerException("ConnnectedClients only access on server");
                return clients;
            }
        }
        public IReadOnlyCollection<NetworkClient> ConnnectedClientList
        {
            get
            {
                if (!IsServer) throw new NotServerException("ConnnectedClientList only access on server");
                return clientList;
            }
        }

        public IReadOnlyCollection<ulong> ConnnectedClientIds
        {
            get
            {
                if (!IsServer) throw new NotServerException("ConnnectedClientIds only access on server");
                return clientIds;
            }
        }

        public int Version;
        public byte[] ConnectionData = new byte[0];

        public ValidateConnectDelegate ValidateConnect;
        public delegate byte[] ValidateConnectDelegate(int version, byte[] payload);

        public static NetworkManager Singleton { get; private set; }


        private void Initalize()
        {
            IsServer = false;
            IsClient = false;
            clients = new();
            clientIds = new();
            clientList = new();
            clientNodes = new();
        }

        void InitalizeMessageHandler()
        {
            msgHandlers = new Dictionary<ushort, NetworkMessageDelegate>();

            msgHandlers = new Dictionary<ushort, NetworkMessageDelegate>();
            msgHandlers[(ushort)NetworkMsgId.ConnectRequest] = OnMessage_ConnectRequest;
            msgHandlers[(ushort)NetworkMsgId.ConnectResponse] = OnMessage_ConnectResponse;
            msgHandlers[(ushort)NetworkMsgId.Disconnect] = OnMessage_Disconnect;
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

        public void StartHost()
        {
            Initalize();
            IsServer = true;
            IsClient = true;
            LocalClientId = ServerClientId;

            try
            {
                server = new NetworkServer(this);
                server.Start(listenAddress, port);

                localClient = new NetworkClient(this);

                delayHostConn = true;
            }
            catch
            {
                IsServer = false;
                IsClient = false;
                if (localClient != null)
                {
                    try
                    {
                        localClient.Dispose();
                    }
                    catch { }
                    localClient = null;
                }
                if (server != null)
                {
                    try
                    {
                        server.Dispose();
                    }
                    catch { }
                    server = null;
                }

                LocalClientId = ulong.MaxValue;
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
                server = new NetworkServer(this);
                server.Start(port);
            }
            catch
            {
                if (server != null)
                {
                    try
                    {
                        server.Dispose();
                    }
                    catch { }
                    server = null;
                }

                LocalClientId = ulong.MaxValue;
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
                localClient = new NetworkClient(this);
                localClient.Connect(address, port, Version, ConnectionData);
            }
            catch
            {
                if (localClient != null)
                {
                    try
                    {
                        localClient.Dispose();
                    }
                    catch { }
                    localClient = null;
                }

                LocalClientId = ulong.MaxValue;
                throw;
            }
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


        #endregion


        public void InvokeHandler(NetworkMessage netMsg)
        {
            NetworkMessageDelegate handler;



            if (msgHandlers == null || !msgHandlers.TryGetValue(netMsg.MsgId, out handler))
            {
                // Console.WriteLine("not found msgId: " + netMsg.MsgId);
                return;
            }

            handler(netMsg);
        }
        bool delayHostConn;
        public void Update()
        {
            if (delayHostConn)
            {
                delayHostConn = false;

                byte[] resData = null;
                if (ValidateConnect != null)
                {
                    resData = ValidateConnect(Version, ConnectionData ?? new byte[0]);
                }

                Server.OnClientConnected(localClient);

                NetworkMessage netMsg = new NetworkMessage();

                localClient.Connection.ConnectionId = ServerClientId;
                localClient.Connection.IsConnecting = false;
                localClient.Connection.IsConnected = true;
                LocalClient.Connection.OnConnected(resData ?? new byte[0]);
            }

            if (IsServer)
            {
                server.Update();
            }
            if (IsClient)
            {
                localClient.Update();
            }
        }

        public NetworkConnection GetConnection(ulong clientId)
        {
            if (clients.TryGetValue(clientId, out var client))
                return client.Connection;
            return null;
        }

        public IEnumerable<NetworkConnection> GetAvaliableConnections(IEnumerable<ulong> clientIds)
        {
            foreach (var clientId in clientIds)
            {
                if (clients.TryGetValue(clientId, out var client))
                    yield return client.Connection;
            }
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
            instance.networkManager = this;
            return instance;
        }

        public void Shutdown()
        {
            IsServer = false;
            IsClient = false;

            if (localClient != null)
            {
                try
                {
                    localClient.Dispose();
                }
                catch { }
                localClient = null;
            }
            if (server != null)
            {
                try
                {
                    server.Dispose();
                }
                catch { }
                server = null;
            }
            if (clientIds != null)
            {
                clientIds.Clear();
                clientList.Clear();
                clientNodes.Clear();
                clients.Clear();
            }
            LocalClientId = ulong.MaxValue;
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

        #region Receive Message


        private void OnMessage_ConnectRequest(NetworkMessage netMsg)
        {
            var msg = netMsg.ReadMessage<ConnectRequestMessage>();
            var conn = netMsg.Connection;

            if (conn.isConnecting)
            {
                if (!IsServer) throw new NotServerException("Connect To Server Msg only server");

                try
                {

                    NetworkClient client;
                    if (!clients.TryGetValue(conn.ConnectionId, out client))
                    {
                        conn.isConnecting = false;
                        conn.isConnected = false;
                        Log("Not found client id: " + conn.ConnectionId);
                        return;
                    }
                    byte[] responseData = null;
                    if (ValidateConnect != null)
                    {
                        try
                        {
                            responseData = ValidateConnect(msg.Version, msg.data);
                        }
                        catch (Exception ex)
                        {
                            conn.isConnecting = false;
                            conn.isConnected = false;
                            conn.Disconnect();
                            LogException(ex);
                            return;
                        }
                    }
                    if (responseData == null)
                        responseData = new byte[0];

                    conn.isConnecting = false;
                    conn.isConnected = true;
                    //Log($"Send Accept Client Msg, ClientId: {conn.ConnectionId}");
                    conn.SendMessage((ushort)NetworkMsgId.ConnectResponse, new ConnectResponseMessage()
                    {
                        ownerClientId = conn.ConnectionId,
                        data = responseData
                    });
                    Server.OnClientConnected(client);
                    conn.OnConnected(responseData);
                }
                catch (Exception ex)
                {
                    conn.isConnected = false;
                    conn.Disconnect();
                    throw ex;
                }
            }
        }

        private void OnMessage_ConnectResponse(NetworkMessage netMsg)
        {

            var msg = netMsg.ReadMessage<ConnectResponseMessage>();
            var conn = netMsg.Connection;
            try
            {
                Log($"Client Receive Connect Msg, ClientId: {msg.ownerClientId}");
                conn.isConnecting = false;
                conn.isConnected = true;
                conn.ConnectionId = msg.ownerClientId;
                if (IsClient && LocalClient != null && LocalClient.Connection == conn)
                {
                    LocalClientId = conn.ConnectionId;
                }
                conn.OnConnected(msg.data ?? new byte[0]);
            }
            catch (Exception ex)
            {
                conn.isConnected = false;
                conn.Disconnect();
                throw ex;
            }
        }

        private static void OnMessage_Disconnect(NetworkMessage netMsg)
        {
            netMsg.Connection.Disconnect();
        }


        private static void OnMessage_CreateObject(NetworkMessage netMsg)
        {
            var conn = netMsg.Connection;
            if (conn.NetworkManager.IsServer)
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

            if (!conn.ContainsObject(instanceId))
            {
                NetworkObject instance = null;
                if (conn.NetworkManager.IsServer)
                {
                    instance = conn.NetworkManager.Server.GetObject(instanceId);
                }
                else
                {
                    var info = NetworkObjectInfo.Get(typeId);
                    conn.NetworkManager.Log($"Spawn Object '{info.type.Name}', instance: {instanceId}");

                    instance = conn.NetworkManager.CreateObject(typeId);
                    if (instance == null)
                        throw new Exception("create instance null, Type id:" + typeId);
                    instance.typeId = typeId;
                    instance.InstanceId = instanceId;
                    instance.networkManager = conn.NetworkManager;
                    instance.OwnerClientId = msg.ownerClientId;
                    instance.ConnectionToOwner = conn;
                    instance.Connection = conn;
                    if (instance.IsOwner)
                    {
                        instance.ConnectionToOwner = conn;
                    }
                    conn.AddObject(instance);
                }

            }
            //}
        }
        private static void OnMessage_DespawnObject(NetworkMessage netMsg)
        {
            var conn = netMsg.Connection;
            var msg = netMsg.ReadMessage<DespawnMessage>();

            ulong instanceId = msg.instanceId;

            NetworkObject instance;
            instance = conn.GetObject(instanceId);
            if (instance != null)
            {
                if (!instance.NetworkManager.IsServer)
                {
                    conn.RemoveObject(instance);
                    if (msg.isDestroy)
                    {
                        instance.Destrory();
                    }
                }
            }
        }

        private static void OnMessage_SpawnObject(NetworkMessage netMsg)
        {
            var msg = new SpawnMessage();
            netMsg.ReadMessage(msg);
            var conn = netMsg.Connection;

            NetworkObject netObj = null;

            if (conn.NetworkManager.IsServer)
            {
                netObj = conn.NetworkManager.Server.GetObject(msg.instanceId);
            }
            else
            {
                netObj = conn.GetObject(msg.instanceId);
            }

            if (netObj == null)
                return;

            netObj.OwnerClientId = msg.ownerClientId;
            netObj.IsSpawned = true;
            netObj.OnSpawned();

            conn.OnObjectAdded(netObj);

        }

        private void OnMessage_SyncVar(NetworkMessage netMsg)
        {
            var msg = new SyncVarMessage();
            msg.conn = netMsg.Connection;

            netMsg.ReadMessage(msg);

            if (IsServer)
            {
                //服务端收到的变量转发给其它端
                foreach (var conn in GetAvaliableConnections(msg.netObj.observers))
                {
                    if (conn.ConnectionId == msg.netObj.OwnerClientId)
                        continue;
                    conn.SendPacket(netMsg.MsgId, netMsg.rawPacket);
                    Log("Redirect Variable Msg: " + conn.ConnectionId);
                }
            }

        }

        private static void OnMessage_Rpc(NetworkMessage netMsg)
        {
            var msg = new RpcMessage();
            msg.conn = netMsg.Connection;

            netMsg.ReadMessage(msg);
        }

        public static long Timestamp
        {
            get { return Utils.Timestamp; }
        }

        private static void OnMessage_Ping(NetworkMessage netMsg)
        {
            var conn = netMsg.Connection;
            var pingMsg = netMsg.ReadMessage<PingMessage>();
            switch (pingMsg.Action)
            {
                case PingMessage.Action_Ping:
                    conn.SendMessage((ushort)NetworkMsgId.Ping, PingMessage.Reply(pingMsg, Timestamp));
                    break;
                case PingMessage.Action_Reply:
                    int timeout = (int)(pingMsg.ReplyTimestamp - pingMsg.Timestamp);
                    conn.pingDelay = timeout;
                    break;
            }
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

        public override string ToString()
        {
            return $"{address}:{port}";
        }

    }
}