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

        public static NetworkManager Singleton { get; private set; }

        private void Initalize()
        {
            IsServer = false;
            IsClient = false;
            clients = new();
            clientIds = new();
            clientList = new();
            clientNodes = new();
            InitalizeMessageHandler();
        }

        void InitalizeMessageHandler()
        {
            msgHandlers = new Dictionary<ushort, NetworkMessageDelegate>();

            msgHandlers = new Dictionary<ushort, NetworkMessageDelegate>();
            msgHandlers[(ushort)NetworkMsgId.Connect] = OnMessage_Connect;
            msgHandlers[(ushort)NetworkMsgId.Disconnect] = OnMessage_Disconnect;
            msgHandlers[(ushort)NetworkMsgId.CreateObject] = OnMessage_CreateObject;
            msgHandlers[(ushort)NetworkMsgId.DestroyObject] = OnMessage_DestroryObject;
            msgHandlers[(ushort)NetworkMsgId.SyncVar] = OnMessage_SyncVar;
            msgHandlers[(ushort)NetworkMsgId.SyncList] = OnMessage_SyncList;
            msgHandlers[(ushort)NetworkMsgId.Rpc] = OnMessage_Rpc;
            msgHandlers[(ushort)NetworkMsgId.Ping] = OnMessage_Ping;

        }

        static bool serializationInitalized;

        internal static void InitializeIntegerSerialization()
        {
            if (serializationInitalized)
                return;
            serializationInitalized = true;

            NetworkVariable<byte>.Serializer = new UInt8Serializer();
            NetworkVariable<byte>.AreEqual = NetworkVariable<byte>.ValueEquals;
            NetworkVariable<short>.Serializer = new Int16Serializer();
            NetworkVariable<short>.AreEqual = NetworkVariable<short>.ValueEquals;
            NetworkVariable<ushort>.Serializer = new UInt16Serializer();
            NetworkVariable<ushort>.AreEqual = NetworkVariable<ushort>.ValueEquals;
            NetworkVariable<int>.Serializer = new Int32Serializer();
            NetworkVariable<int>.AreEqual = NetworkVariable<int>.ValueEquals;
            NetworkVariable<uint>.Serializer = new UInt32Serializer();
            NetworkVariable<uint>.AreEqual = NetworkVariable<uint>.ValueEquals;
            NetworkVariable<long>.Serializer = new Int64Serializer();
            NetworkVariable<long>.AreEqual = NetworkVariable<long>.ValueEquals;
            NetworkVariable<ulong>.Serializer = new UInt64Serializer();
            NetworkVariable<ulong>.AreEqual = NetworkVariable<ulong>.ValueEquals;
            NetworkVariable<float>.Serializer = new Float32Serializer();
            NetworkVariable<float>.AreEqual = NetworkVariable<float>.ValueEquals;
            NetworkVariable<double>.Serializer = new Float64Serializer();
            NetworkVariable<double>.AreEqual = NetworkVariable<double>.ValueEquals;
            NetworkVariable<bool>.Serializer = new BoolSerializer();
            NetworkVariable<bool>.AreEqual = NetworkVariable<bool>.ValueEquals;
            NetworkVariable<string>.Serializer = new StringSerializer();
            NetworkVariable<string>.AreEqual = NetworkVariable<bool>.EqualityEqualsObject;
            NetworkVariable<Guid>.Serializer = new GuidSerializer();
            NetworkVariable<Guid>.AreEqual = NetworkVariable<Guid>.ValueEquals;
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

                delayConn = true;
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
                localClient.Connect(address, port);
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
                SyncListInfo.GetSyncListInfos(type);
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
        bool delayConn;
        public void Update()
        {
            if (delayConn)
            {
                delayConn = false;

                Server.OnClientConnected(localClient);

                NetworkMessage netMsg = new NetworkMessage();

                localClient.Connection.ConnectionId = ServerClientId;
                localClient.Connection.IsConnecting = false;
                localClient.Connection.IsConnected = true;
                LocalClient.Connection.OnConnected(netMsg);
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


        private void OnMessage_Connect(NetworkMessage netMsg)
        {
            var msg = netMsg.ReadMessage<ConnectMessage>();
            var conn = netMsg.Connection;

            if (conn.isConnecting)
            {

                if (msg.toServer)
                {
                    if (!IsServer) throw new NotServerException("Connect To Server Msg only server");

                    conn.isConnecting = false;
                    conn.isConnected = true;

                    Log($"Send Accept Client Msg, ClientId: {conn.ConnectionId}");
                    conn.SendMessage((ushort)NetworkMsgId.Connect, new ConnectMessage()
                    {
                        clientId = conn.ConnectionId,
                        toServer = false,
                    });

                    NetworkClient client;

                    try
                    {
                        if (!clients.TryGetValue(conn.ConnectionId, out client))
                        {
                            throw new Exception("Not found client id: " + conn.ConnectionId);
                        }
                        Server.OnClientConnected(client);
                        conn.OnConnected(netMsg);
                    }
                    catch (Exception ex)
                    {
                        conn.isConnected = false;
                        conn.Disconnect();
                        throw ex;
                    }
                }
                else
                {
                    try
                    {
                        Log($"Client Receive Connect Msg, ClientId: {msg.clientId}");
                        conn.isConnecting = false;
                        conn.isConnected = true;
                        conn.ConnectionId = msg.clientId;
                        if (LocalClient != null && LocalClient.Connection == conn)
                        {
                            LocalClientId = conn.ConnectionId;
                        }
                        conn.OnConnected(null);
                    }
                    catch (Exception ex)
                    {
                        conn.isConnected = false;
                        conn.Disconnect();
                        throw ex;
                    }
                }
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
                    instance.IsSpawned = true;

                    instance.ConnectionToOwner = conn;
                    instance.OwnerClientId = msg.ownerClientId;
                    instance.ConnectionToServer = conn;
                    if (instance.IsOwner)
                    {
                        instance.ConnectionToOwner = conn;
                    }
                    conn.AddObject(instance);
                    instance.OnSpawned();
                }

            }
            //}
        }
        private static void OnMessage_DestroryObject(NetworkMessage netMsg)
        {
            var conn = netMsg.Connection;
            var msg = netMsg.ReadMessage<DestroyObjectMessage>();

            ulong instanceId = msg.instanceId;

            NetworkObject instance;
            instance = conn.GetObject(instanceId);
            if (instance != null)
            {
                if (!instance.NetworkManager.IsServer)
                {
                    conn.RemoveObject(instance);
                    instance.Destrory();
                }
            }
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
        private static void OnMessage_SyncList(NetworkMessage netMsg)
        {
            var msg = new SyncListMessage();
            msg.conn = netMsg.Connection;

            netMsg.ReadMessage(msg);
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

    }
}