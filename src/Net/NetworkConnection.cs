using Net.Messages;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace Net
{
    public class NetworkConnection : CoroutineBase, IDisposable
    {
        private Socket socket;
        private bool isConnecting;
        private bool isConnected;
        private NetworkReader reader;
        private Pool<NetworkWriter> writePool;
        private Queue<NetworkWriter> sendMsgQueue;
        private NetworkWriter currentSendMsg;
        private Dictionary<short, NetworkMessageDelegate> handlers;
        private static Dictionary<short, NetworkMessageDelegate> defaultHandlers;
        private bool isListening;
        private bool isInital;
        //private DateTime lastTryReconnectTime;
        private Dictionary<NetworkInstanceId, NetworkObject> objects;
        private int pingDelay;
        private DateTime lastSendTime;
        private DateTime lastReceiveTime;
        private string address;
        private int port;
        private int connectionId;
        internal NetworkServer server;

        public NetworkConnection()
        {
            objects = new Dictionary<NetworkInstanceId, NetworkObject>();

        }

        public NetworkConnection(NetworkServer server, Socket socket, bool isListening, MessageBase extra = null)
            : this()
        {
            //if (socket == null) throw new ArgumentNullException("socket");
            this.server = server;
            this.socket = socket;

            this.isListening = isListening;
            //ReconnectInterval = 1000;
            //AutoReconnect = true;


            if (socket != null)
            {
                if (socket.Connected)
                {

                    InitialSocket(socket);
                    isConnecting = true;
                    if (!isListening)
                    {
                        SendMessage((short)NetworkMsgId.Connect, new ConnectMessage()
                        {
                            connectionId = connectionId,
                            toServer = true,
                            extra = extra,
                        });
                    }
                    StartCoroutine(NextFrameRunning());
                }
                else
                {
                    Disconnect();
                }
            }
        }

        public int ConnectionId
        {
            get { return connectionId; }
            internal set { connectionId = value; }
        }

        public Socket Socket
        {
            get { return socket; }
        }

        public bool IsConnecting
        {
            get { return isConnecting; }
        }

        public bool IsConnected
        {
            get { return isConnected; }
        }

        public bool IsSocketConnected
        {
            get { return socket != null && socket.Connected; }
        }

        //public int ReconnectInterval { get; set; }

        //public bool AutoReconnect { get; set; }

        public bool HasSendMessage
        {
            get { return currentSendMsg != null || sendMsgQueue.Count > 0; }
        }

        public IEnumerable<NetworkObject> Objects
        {
            get
            {
                return objects.Select(o => o.Value);
            }
        }
        public IEnumerable<NetworkObject> OwnerObjects
        {
            get { return objects.Values.Where(o => o.ConnectionToOwner == this); }
        }

        public int PingDelay
        {
            get { return pingDelay; }
        }

        public DateTime LastSendTime { get => lastSendTime; }
        public DateTime LastReceiveTime { get => lastReceiveTime; }


        private static Dictionary<short, NetworkMessageDelegate> DefaultHandlers
        {
            get
            {
                if (defaultHandlers == null)
                {
                    defaultHandlers = new Dictionary<short, NetworkMessageDelegate>();
                    defaultHandlers[(short)NetworkMsgId.Connect] = OnMessage_Connect;
                    defaultHandlers[(short)NetworkMsgId.Disconnect] = OnMessage_Disconnect;
                    defaultHandlers[(short)NetworkMsgId.CreateObject] = OnMessage_CreateObject;
                    defaultHandlers[(short)NetworkMsgId.DestroyObject] = OnMessage_DestroryObject;
                    defaultHandlers[(short)NetworkMsgId.SyncVar] = OnMessage_SyncVar;
                    defaultHandlers[(short)NetworkMsgId.SyncList] = OnMessage_SyncList;
                    defaultHandlers[(short)NetworkMsgId.Rpc] = OnMessage_Rpc;
                    defaultHandlers[(short)NetworkMsgId.Ping] = OnMessage_Ping;
                }
                return defaultHandlers;
            }
        }
        public static long Timestamp
        {
            get { return Utils.Timestamp; }
        }

        public event Action<NetworkConnection, NetworkMessage> Connected;
        public event Action<NetworkConnection> Disconnected;
        public event Action<NetworkObject> ObjectAdded;
        public event Action<NetworkObject> ObjectRemoved;

        public Socket GetSocket()
        {
            return socket;
        }
        public bool HasHandler(short msgId)
        {
            if (handlers == null)
                return false;
            return handlers.ContainsKey(msgId);
        }


        public void RegisterHandler(short msgId, NetworkMessageDelegate handler)
        {
            if (handler == null)
                return;
            if (handlers == null)
                handlers = new Dictionary<short, NetworkMessageDelegate>();
            handlers[msgId] = handler;
        }

        public void UnregisterHandler(short msgId)
        {
            if (handlers == null)
                return;
            handlers.Remove(msgId);
        }

        public void SendMessage(short msgId, MessageBase msg = null)
        {
            NetworkWriter s;

            s = writePool.Get();

            s.BaseStream.Position = 0;
            s.BaseStream.SetLength(0);
            s.BeginWritePackage();
            s.WriteInt16(msgId);
            if (msg != null)
            {
                msg.Serialize(s);
            }

            s.EndWritePackage();

            sendMsgQueue.Enqueue(s);
        }


        public void Disconnect()
        {
            if (socket != null)
            {
                try
                {
                    ProcessSendMessage();
                }
                catch { }

                try
                {
                    socket.Disconnect(false);
                    socket.Close();
                }
                catch { }
                socket = null;
            }
            if (isConnected)
            {
                isConnected = false;
                Disconnected?.Invoke(this);
            }
        }

        public void Initial(string address, int port)
        {
            this.address = address;
            this.port = port;
            isInital = true;
        }


        public void Connect(string address, int port, MessageBase extra = null)
        {
            if (isListening)
                throw new Exception("is listen");

            if (!(isConnected || isConnecting))
            {
                isConnecting = true;
                //lastTryReconnectTime = DateTime.UtcNow;
                Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                try
                {
                    s.Connect(address, port);
                    s.Blocking = false;
                }
                catch (Exception ex)
                {
                    s = null;
                    isConnecting = false;
                    Console.WriteLine(ex);
                }

                if (s != null)
                {

                    Connect(s, extra);
                }
            }

            this.address = address;
            this.port = port;
        }

        public void Connect(Socket socket, MessageBase extra = null)
        {
            Disconnect();
            InitialSocket(socket);
            if (socket != null && socket.Connected)
            {
                isConnecting = true;
                SendMessage((short)NetworkMsgId.Connect, new ConnectMessage()
                {
                    connectionId = connectionId,
                    toServer = true,
                    extra = extra,
                });
                StartCoroutine(Running());
            }
        }

        private void InitialSocket(Socket socket)
        {
            this.socket = socket;
            reader = new NetworkReader(Socket);
            Clear();
        }

        void Clear()
        {
            if (writePool == null)
            {
                writePool = new Pool<NetworkWriter>(() => new NetworkWriter(new MemoryStream()));
            }

            if (sendMsgQueue == null)
            {
                sendMsgQueue = new Queue<NetworkWriter>();
            }
            else
            {
                while (sendMsgQueue.Count > 0)
                {
                    var tmp = sendMsgQueue.Dequeue();
                    writePool.Unused(tmp);
                }
            }

            currentSendMsg = null;
        }
        internal void ProcessMessage()
        {
        }


        internal IEnumerator Running()
        {
            yield return null;

            while (isConnecting || isConnected)
            {
                try
                {
                    if (!socket.Connected)
                    {
                        break;
                    }
                }
                catch (Exception ex)
                {
                    Disconnect();
                    Console.WriteLine(ex);
                }

                try
                {
                    ProcessSendMessage();

                    ProcessReceiveMessage();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                    break;
                }
                yield return null;
            }

            if (isConnected)
            {
                isConnected = false;
                Disconnected?.Invoke(this);
            }

        }
        internal IEnumerator NextFrameRunning()
        {
            yield return null;
            StartCoroutine(Running());
        }

        void ProcessSendMessage()
        {
            MemoryStream ms;

            while (isConnecting || isConnected)
            {
                if (!socket.Connected)
                {
                    //if (AutoReconnect && (DateTime.UtcNow - lastTryReconnectTime).TotalMilliseconds > ReconnectInterval)
                    //{
                    //    Connect();
                    //}
                    //if (!socket.Connected)
                    //    break;
                    break;
                }

                if (currentSendMsg == null)
                {

                    if (sendMsgQueue.Count > 0)
                    {
                        currentSendMsg = sendMsgQueue.Dequeue();
                        if (currentSendMsg != null)
                        {
                            ms = (MemoryStream)currentSendMsg.BaseStream;
                            ms.Position = 0;
                        }
                    }
                }

                if (currentSendMsg == null)
                    break;


                ms = (MemoryStream)currentSendMsg.BaseStream;
                int count = (int)(ms.Length - ms.Position);
                if (count > 0)
                {

                    int sendCount = socket.Send(ms.GetBuffer(), (int)ms.Position, count, SocketFlags.None);

                    if (sendCount > 0)
                    {
                        ms.Position += sendCount;
                        lastSendTime = DateTime.UtcNow;
                    }
                }
                if (ms.Position >= ms.Length)
                {
                    writePool.Unused(currentSendMsg);
                    currentSendMsg = null;
                }
                else
                {
                    break;
                }

            }
        }

        void ProcessReceiveMessage()
        {

            if (!(isConnecting || isConnected))
                return;

            int readCount = reader.ReadPackage();
            if (readCount > 0)
            {
                lastReceiveTime = DateTime.UtcNow;
            }

            while (readCount > 0)
            {

                short msgId = reader.ReadInt16();

                NetworkMessage netMsg = new NetworkMessage();
                netMsg.MsgId = msgId;
                netMsg.Connection = this;
                netMsg.Reader = reader;

                InvokeHandler(netMsg);

                readCount = reader.ReadPackage();
                if (readCount > 0)
                {
                    lastReceiveTime = DateTime.UtcNow;
                }
            }
        }


        public void InvokeHandler(NetworkMessage netMsg)
        {
            NetworkMessageDelegate handler;

            if (handlers == null || !handlers.TryGetValue(netMsg.MsgId, out handler))
            {
                var defaultHandlers = DefaultHandlers;
                if (defaultHandlers == null || !defaultHandlers.TryGetValue(netMsg.MsgId, out handler))
                {
                    Console.WriteLine("not found msgId: " + netMsg.MsgId);
                    return;
                }
            }

            handler(netMsg);
        }

        public void Flush(int timeout)
        {
            if (HasSendMessage)
            {
                var time = DateTime.Now.AddMilliseconds(timeout);
                while (HasSendMessage)
                {
                    ProcessSendMessage();
                    if (DateTime.Now > time)
                        break;
                    System.Threading.Thread.Sleep(1);
                }
            }

        }


        public IEnumerator WaitConnected(int timeout)
        {
            if (IsConnected && IsSocketConnected)
                yield break;

            DateTime endTime = DateTime.Now.AddMilliseconds(timeout);
            while (true)
            {
                if (IsConnected && IsSocketConnected)
                    break;

                if (DateTime.Now > endTime)
                    break;

                yield return null;
            }
        }

        public T FindObject<T>()
            where T : NetworkObject
        {
            foreach (var obj in objects.Values)
            {
                if (obj is T)
                    return (T)obj;
            }
            return null;
        }

        internal void AddObject(NetworkObject obj)
        {
            if (!objects.ContainsKey(obj.InstanceId))
            {
                objects[obj.InstanceId] = obj;
                ObjectAdded?.Invoke(obj);
            }
        }
        internal void RemoveObject(NetworkObject obj)
        {
            if (objects.ContainsKey(obj.InstanceId))
            {
                objects.Remove(obj.InstanceId);
                ObjectRemoved?.Invoke(obj);
            }
        }

        internal bool ContainsObject(NetworkInstanceId instanceId)
        {
            return objects.ContainsKey(instanceId);
        }

        public NetworkObject GetObject(NetworkInstanceId instanceId)
        {
            if (objects.ContainsKey(instanceId))
                return objects[instanceId];
            return null;
        }
        void CheckServer()
        {
            if (server == null)
                throw new Exception("is not server");
        }

        public void Dispose()
        {
            try
            {
                Disconnect();
            }
            catch { }
        }

        public override bool Equals(object obj)
        {
            var conn = obj as NetworkConnection;
            if (conn != null)
                return connectionId != 0 && object.Equals(connectionId, conn.connectionId);
            return false;
        }

        public override int GetHashCode()
        {
            return connectionId.GetHashCode();
        }


        #region Receive Message


        private static void OnMessage_Connect(NetworkMessage netMsg)
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
                        conn.Connected?.Invoke(conn, netMsg);
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
                            connectionId = conn.connectionId,
                            toServer = false,
                        });
                    }
                }
                else
                {
                    try
                    {
                        conn.ConnectionId = msg.connectionId;
                        conn.Connected?.Invoke(conn, null);
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
            NetworkObjectId objectId = msg.objectId;
            if (msg.toServer)
            {
                //conn.CheckServer();

                //var obj = conn.server.CreateObject(objectId, netMsg);
                //if (obj != null)
                //{
                //    obj.AddObserver(conn);
                //    obj.ConnectionToOwner = conn;
                //}
            }
            else
            {

                NetworkInstanceId instanceId = msg.instanceId;

                if (!conn.ContainsObject(instanceId))
                {
                    NetworkObject instance = null;
                    if (conn.server != null)
                    {
                        instance = conn.server.GetObject(instanceId);
                    }
                    else
                    {
                        var info = NetworkObjectInfo.Get(objectId);
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
        }
        private static void OnMessage_DestroryObject(NetworkMessage netMsg)
        {
            var conn = netMsg.Connection;
            var msg = netMsg.ReadMessage<DestroyObjectMessage>();

            NetworkInstanceId instanceId = msg.instanceId;

            NetworkObject instance;
            instance = conn.GetObject(instanceId);
            if (instance != null)
            {
                if (instance.IsClient)
                {
                    conn.RemoveObject(instance);
                    if (!instance.IsServer)
                    {
                        instance.Destrory();
                    }
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

        public void Ping()
        {
            long timestamp = Timestamp;
            SendMessage((short)NetworkMsgId.Ping, PingMessage.Ping(timestamp));
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

    }
}
