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
        internal Dictionary<short, NetworkMessageDelegate> msgHandlers;

        public const ulong ServerClientId = 0;
        public static Action<string> LogCallback;

        public NetworkManager()
        {
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
            msgHandlers = new Dictionary<short, NetworkMessageDelegate>();

            msgHandlers = new Dictionary<short, NetworkMessageDelegate>();
            msgHandlers[(short)NetworkMsgId.Connect] = OnMessage_Connect;
            msgHandlers[(short)NetworkMsgId.Disconnect] = OnMessage_Disconnect;
            msgHandlers[(short)NetworkMsgId.CreateObject] = OnMessage_CreateObject;
            msgHandlers[(short)NetworkMsgId.DestroyObject] = OnMessage_DestroryObject;
            msgHandlers[(short)NetworkMsgId.SyncVar] = OnMessage_SyncVar;
            msgHandlers[(short)NetworkMsgId.SyncList] = OnMessage_SyncList;
            msgHandlers[(short)NetworkMsgId.Rpc] = OnMessage_Rpc;
            msgHandlers[(short)NetworkMsgId.Ping] = OnMessage_Ping;

        }


        public void StartHost()
        {
            Initalize();
            IsServer = true;
            IsClient = true;
            LocalClientId = ulong.MaxValue;

            try
            {
                server = new NetworkServer(this);
                server.Start(listenAddress, port);

                localClient = new NetworkClient(this);
                localClient.Connection.ConnectionId = ServerClientId;
                localClient.Connect(address, port);
                LocalClientId = localClient.ClientId;
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
                throw;
            }
        }


        #region Network Object

        internal ulong GetTypeId(Type type)
        {
            ulong objectId = type.Hash32();
            return objectId;
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
                Console.WriteLine("not found msgId: " + netMsg.MsgId);
                return;
            }

            handler(netMsg);
        }

        public void Update()
        {
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

        public IEnumerable<NetworkConnection> GetConnections(IEnumerable<ulong> clientIds)
        {
            foreach (var clientId in clientIds)
            {
                if (clients.TryGetValue(clientId, out var client))
                    yield return client.Connection;
            }
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
                conn.isConnecting = false;
                conn.isConnected = true;
                if (msg.toServer)
                {
                    try
                    {
                        conn.OnConnected(netMsg);
                    }
                    catch (Exception ex)
                    {
                        conn.Disconnect();
                        throw ex;
                    }
                    if (conn.IsConnected)
                    {
                        conn.SendMessage((short)NetworkMsgId.Connect, new ConnectMessage()
                        {
                            connectionId = conn.ConnectionId,
                            toServer = false,
                        });
                    }
                }
                else
                {
                    try
                    {
                        conn.ConnectionId = msg.connectionId;
                        if (LocalClient != null && LocalClient.Connection == conn)
                        {
                            LocalClientId = conn.ConnectionId;
                        }
                        conn.OnConnected(null);
                    }
                    catch (Exception ex)
                    {
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
            var msg = netMsg.ReadMessage<CreateObjectMessage>();
            ulong typeId = msg.typeId;
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
                if (conn.server != null)
                {
                    instance = conn.server.GetObject(instanceId);
                }
                else
                {
                    var info = NetworkObjectInfo.Get(typeId);
                    instance = info.create(typeId);
                    if (instance == null)
                        throw new Exception("create instance null, Type id:" + typeId);
                    instance.typeId = typeId;
                    instance.InstanceId = instanceId;
                    instance.networkManager = conn.NetworkManager;
                    instance.ConnectionToOwner = conn;
                    instance.OwnerClientId = msg.ownerClientId;
                }

                instance.ConnectionToServer = conn;
                conn.AddObject(instance);
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

        private static void OnMessage_SyncVar(NetworkMessage netMsg)
        {
            var msg = new SyncVarMessage();
            msg.conn = netMsg.Connection;

            netMsg.ReadMessage(msg);
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
                    conn.SendMessage((short)NetworkMsgId.Ping, PingMessage.Reply(pingMsg, Timestamp));
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