using Net.Messages;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;

namespace Net
{
    public class NetworkClient : CoroutineBase,IDisposable
    {
        private NetworkConnection conn;
        private bool isRunning;
        private int pingDelay;
        private bool isClient;
        private NetworkServer server;
        private Status status;

        public NetworkClient(NetworkServer server, Socket socket, bool isListen)
        {
            this.server = server;
            isClient = !isListen;

            conn = new NetworkConnection(socket, isListen);
            conn.RegisterHandler((short)NetworkMsgId.Handshake, OnReceive_HandshakeMsg);
            conn.RegisterHandler((short)NetworkMsgId.Ping, OnReceive_Ping);

            if (IsClient)
            {
                conn.RegisterHandler((short)NetworkMsgId.CreateObject, OnReceive_CreateObject);
                conn.RegisterHandler((short)NetworkMsgId.DestroyObject, OnReceive_DestroryObject);
                ConnectionToServer = conn;
            }
            else
            {
                ConnectionToClient = conn;
            }
            conn.Connected += Conn_Connected;
            conn.Disconnected += Conn_Disconnected;
        }

        public NetworkConnection Connection
        {
            get { return conn; }
        }
        public NetworkConnection ConnectionToServer;
        public NetworkConnection ConnectionToClient;

        public bool IsRunning
        {
            get { return isRunning; }
        }

        public bool IsClient
        {
            get { return isClient; }
        }

        public static long Timestamp
        {
            get { return Utils.Timestamp; }
        }

        protected NetworkServer Server { get => server; }

        public int PingDelay
        {
            get { return pingDelay; }
        }
        public IEnumerable<NetworkObject> Objects
        {
            get { return conn.Objects; }
        }

        public event Action<NetworkClient> Started;
        public event Action<NetworkClient> Stoped;
        public event Action<NetworkClient> Connected;
        public event Action<NetworkClient> Disconnected;



        public virtual void Start()
        {
            if (isRunning)
                return;

            StartCoroutine(Running());
        }

        public virtual void Stop()
        {
            CheckThreadSafe();
            isRunning = false;
        }


        IEnumerator Running()
        {

            isRunning = true;

            Started?.Invoke(this);

            using (Connection)
            {
                SendHandshakeMsg();

                while (isRunning)
                {
                    try
                    {
                        if (!conn.IsConnected)
                            break;
                        conn.ProcessMessage();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                    }
                    yield return null;
                }
            }

            isRunning = false;
            

            Stoped?.Invoke(this);
        }

        public void Send(short msgId, MessageBase msg = null)
        {
            Connection.SendMessage(msgId, msg);
        }


        private void Conn_Connected(NetworkConnection obj)
        {

        }

        private void Conn_Disconnected(NetworkConnection obj)
        {
            if (!(status == Status.Stoped || status == Status.HandshakeError))
            {
                status = Status.Handshaking;
            }
            Disconnected?.Invoke(this);
        }
 

        protected virtual void SendHandshakeMsg()
        {

            if (IsClient)
            {
                Connection.SendMessage((short)NetworkMsgId.Handshake);
            }
            else
            {
                Connection.SendMessage((short)NetworkMsgId.Handshake);
            }

        }

        protected virtual void OnHandshakeMsg(NetworkMessage netMsg)
        {

        }

        private void OnReceive_HandshakeMsg(NetworkMessage netMsg)
        {
            if (status == Status.Handshaking)
            {
                try
                {
                    OnHandshakeMsg(netMsg);
                    status = Status.Connected;
                    Connected?.Invoke(this);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                    status = Status.HandshakeError;
                    Connection.AutoReconnect = false;
                    Stop();
                }
            }
        }

        enum Status
        {
            Handshaking,
            HandshakeError,
            Connected,
            Stoped,
        }

        #region Ping

        public void Ping()
        {

            long timestamp = NetworkClient.Timestamp;
            conn.SendMessage((short)NetworkMsgId.Ping, PingMessage.Ping(timestamp));

        }

        private void OnReceive_Ping(NetworkMessage netMsg)
        {
            var pingMsg = netMsg.ReadMessage<PingMessage>();
            switch (pingMsg.Action)
            {
                case PingMessage.Action_Ping:
                    netMsg.Connection.SendMessage((short)NetworkMsgId.Ping, PingMessage.Reply(pingMsg, Timestamp));
                    break;
                case PingMessage.Action_Reply:
                    int timeout = (int)(pingMsg.ReplyTimestamp - pingMsg.Timestamp);
                    pingDelay = timeout;
                    break;
            }
        }


        #endregion

        //public  void SendMessage(short msgId, MessageBase msg = null)
        //{
        //    CheckThreadSafe();
        //    if (!isRunning)
        //        return;

        //    client.conn.SendMessage(msgId, msg);
        //}



        #region Create Object



        internal static Dictionary<NetworkObjectId, NetworkObjectInfo> createInstanceInfos;

        public static void RegisterObject<T>(CreateInstanceDelegate create, DestroyInstanceDelegate destrory = null)
        {
            NetworkObjectId objectId = NetworkObjectId.GetObjectId(typeof(T));
            Type type = typeof(T);
            SyncVarInfo.GetSyncVarInfos(type);
            SyncListInfo.GetSyncListInfos(type);
            RegisterObject(objectId, create, destrory);
        }

        public static void RegisterObject(NetworkObjectId objectId, CreateInstanceDelegate create, DestroyInstanceDelegate destrory = null)
        {
            if (objectId.Value == Guid.Empty)
                throw new ArgumentException("value is empty", nameof(objectId));

            if (create == null)
                throw new ArgumentNullException(nameof(create));

            NetworkObjectInfo info = new NetworkObjectInfo()
            {
                objectId = objectId,
                create = create,
                destroy = destrory,
            };
            if (createInstanceInfos == null)
                createInstanceInfos = new Dictionary<NetworkObjectId, NetworkObjectInfo>();
            createInstanceInfos[objectId] = info;
        }

        public static void UnregisterObject(NetworkObjectId objectId)
        {
            if (createInstanceInfos != null)
            {
                createInstanceInfos.Remove(objectId);
            }
        }




        private void ClientCreateObject(NetworkConnection conn, NetworkObjectId objectId, NetworkInstanceId instanceId)
        {

            if (!conn.ContainsObject(instanceId))
            {
                NetworkObject instance = null;
                if (server != null)
                {
                    instance = server.GetObject(instanceId);
                }
                else
                {

                    if (!createInstanceInfos.ContainsKey(objectId))
                        throw new Exception("not contains object id:" + objectId);
                    var info = createInstanceInfos[objectId];
                    instance = info.create(objectId);
                    if (instance == null)
                        throw new Exception("create instance null, object id:" + objectId);
                    instance.InstanceId = instanceId;
                    instance.objectId = objectId;
                    instance.ConnectionToOwner = conn;
                }
                instance.IsClient = true;

                instance.ConnectionToServer = conn;
                conn.AddObject(instance);
            }
        }

        private static void ClientDestroyObject(NetworkConnection conn, NetworkInstanceId instanceId)
        {
            NetworkObject instance;
            instance = conn.GetObject(instanceId);
            if (instance != null)
            {
                conn.RemoveObject(instance);
                if (!instance.IsServer)
                {
                    instance.Destrory();
                }
            }
        }


        private void OnReceive_CreateObject(NetworkMessage netMsg)
        {
            if (IsClient)
            {
                var msg = netMsg.ReadMessage<CreateObjectMessage>();
                NetworkObjectId objectId = msg.objectId;
                NetworkInstanceId instanceId = msg.instanceId;

                ClientCreateObject(netMsg.Connection, objectId, instanceId);
            }
        }
        private void OnReceive_DestroryObject(NetworkMessage netMsg)
        {
            if (IsClient)
            {
                var msg = netMsg.ReadMessage<DestroyObjectMessage>();
                NetworkInstanceId instanceId = msg.instanceId;
                ClientDestroyObject(netMsg.Connection, instanceId);
            }
        }

        public virtual void Dispose()
        {
            Stop();
        }


        #endregion

    }

    public delegate NetworkObject CreateInstanceDelegate(NetworkObjectId objectId);
    public delegate void DestroyInstanceDelegate(NetworkObject instance);



    internal class NetworkObjectInfo
    {
        public NetworkObjectId objectId;
        public CreateInstanceDelegate create;
        public DestroyInstanceDelegate destroy;
    }

    internal class CreateObjectMessage : MessageBase
    {
        public NetworkObjectId objectId;
        public NetworkInstanceId instanceId;
    }
    internal class DestroyObjectMessage : MessageBase
    {
        public NetworkInstanceId instanceId;
    }

}
