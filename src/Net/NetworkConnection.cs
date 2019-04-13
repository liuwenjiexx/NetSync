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
        private bool isConnected;
        private NetworkReader reader;
        private Pool<NetworkWriter> writePool;
        private Queue<NetworkWriter> sendMsgQueue;
        private NetworkWriter currentSendMsg;
        private Dictionary<short, NetworkMessageDelegate> handlers;
        private bool isListening;
        private bool isInital;
        //private DateTime lastTryReconnectTime;
        private Dictionary<NetworkInstanceId, NetworkObject> objects;

        private DateTime lastSendTime;
        private DateTime lastReceiveTime;
        private string address;
        private int port;


        public NetworkConnection()
        {
            objects = new Dictionary<NetworkInstanceId, NetworkObject>();
            handlers = new Dictionary<short, NetworkMessageDelegate>();

            RegisterHandler((short)NetworkMsgId.SyncVar, OnReceive_SyncVar);
            RegisterHandler((short)NetworkMsgId.SyncList, OnReceive_SyncList);
            RegisterHandler((short)NetworkMsgId.Rpc, OnReceive_Rpc);
        }

        public NetworkConnection(Socket socket, bool isListening)
            :this()
        {
            if (socket == null) throw new ArgumentNullException("socket");

            this.socket = socket;

            this.isListening = isListening;
            //ReconnectInterval = 1000;
            //AutoReconnect = true;
   

            if (socket != null)
            {

                if (socket.Connected)
                {
                    StartCoroutine(Running());
                }
                else
                {
                    Disconnect();
                }
            }
        }

        public Socket Socket
        {
            get { return socket; }
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

        public DateTime LastSendTime { get => lastSendTime; }
        public DateTime LastReceiveTime { get => lastReceiveTime; }

        public event Action<NetworkConnection> Connected;
        public event Action<NetworkConnection> Disconnected;
        public event Action<NetworkObject> ObjectAdded;
        public event Action<NetworkObject> ObjectRemoved;

        public void RegisterHandler(short msgId, NetworkMessageDelegate handler)
        {
            if (handler == null)
                return;
 
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


        public void Connect(string address, int port)
        {
            if (isListening)
                throw new Exception("is listen");

            if (!isConnected)
            {
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
                    Console.WriteLine(ex);
                }

                if (s != null)
                {
                    if (s.Connected)
                    {
                        InitialSocket(s);
                        isConnected = true;
                    }
                }

                if (isConnected)
                {
                    this.address = address;
                    this.port = port;
                    StartCoroutine(Running());
                    Connected?.Invoke(this);
                }

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
            InitialSocket(socket);
            this.isConnected = true;
            Connected?.Invoke(this);

            while (isConnected)
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

        void ProcessSendMessage()
        {
            MemoryStream ms;

            while (isConnected)
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

            if (!isConnected)
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

        public bool HasHandler(short msgId)
        {
            return handlers.ContainsKey(msgId);
        }

        public void InvokeHandler(NetworkMessage netMsg)
        {
            NetworkMessageDelegate handler;

            if (handlers == null || !handlers.TryGetValue(netMsg.MsgId, out handler))
            {
                //   if (this.handlers == null || !this.handlers.TryGetValue(msgId, out handler))
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

        #region Receive Message

        private static void OnReceive_SyncVar(NetworkMessage netMsg)
        {
            var msg = new SyncVarMessage();
            msg.conn = netMsg.Connection;

            netMsg.ReadMessage(msg); 
        }
        private static void OnReceive_SyncList(NetworkMessage netMsg)
        {
            var msg = new SyncListMessage();
            msg.conn = netMsg.Connection;

            netMsg.ReadMessage(msg);
        }
        private static void OnReceive_Rpc(NetworkMessage netMsg)
        {
            var msg = new RpcMessage();
            msg.conn = netMsg.Connection;

            netMsg.ReadMessage(msg);
        }

        #endregion

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
                return object.Equals(Socket, conn.Socket);
            var socket = obj as Socket;
            if (socket != null)
                return object.Equals(this.Socket, socket);
            return false;
        }
        public override int GetHashCode()
        {
            return Socket.GetHashCode();
        }

    }
}
