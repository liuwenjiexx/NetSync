using Yanmonet.NetSync.Messages;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Yanmonet.NetSync
{
    public class NetworkConnection : IDisposable
    {
        private Socket socket;
        internal bool isConnecting;
        internal bool isConnected;
        private NetworkReader reader;
        private Pool<NetworkWriter> writePool;
        private Queue<byte[]> sendMsgQueue;
        private byte[] currentSendMsg;
        private int currentSendPosition;
        private Dictionary<ushort, NetworkMessageDelegate> handlers;
        private bool isListening;
        private bool isInital;
        //private DateTime lastTryReconnectTime;
        private Dictionary<ulong, NetworkObject> objects;
        internal int pingDelay;
        private DateTime lastSendTime;
        private DateTime lastReceiveTime;
        private string address;
        private int port;
        private ulong connectionId;
        internal NetworkServer server;
        private bool ownerSoket;

        internal NetworkConnection(NetworkManager networkManager)
            : this(networkManager, null, null, false, false)
        {


        }
        internal NetworkConnection(NetworkServer server, Socket socket, bool ownerSoket, bool isListening)
            : this(server?.NetworkManager, server, socket, ownerSoket, isListening)
        {

        }
        internal NetworkConnection(NetworkManager networkManager, NetworkServer server, Socket socket, bool ownerSoket, bool isListening)
        {
            objects = new Dictionary<ulong, NetworkObject>();
            //if (socket == null) throw new ArgumentNullException("socket");
            this.server = server;

            this.socket = socket;

            this.isListening = isListening;
            //ReconnectInterval = 1000;
            //AutoReconnect = true;
            this.ownerSoket = ownerSoket;

            this.networkManager = networkManager;

            if (socket != null)
            {
                if (socket.Connected)
                {
                    isConnecting = true;
                    InitialSocket(socket);
                    if (!isListening)
                    {
                        //NetworkManager.Log($"Send Connect Msg, Client To Server ClientId: {connectionId}");
                        //SendMessage((ushort)NetworkMsgId.Connect, new ConnectMessage()
                        //{
                        //    clientId = connectionId,
                        //    toServer = true,
                        //    extra = extra,
                        //});
                    }
                }
                else
                {
                    Disconnect();
                }
            }
        }

        public ulong ConnectionId
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
            internal set { isConnecting = value; }
        }

        public bool IsConnected
        {
            get { return isConnected; }
            internal set { isConnected = value; }
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
        internal NetworkManager networkManager;
        public NetworkManager NetworkManager => networkManager ?? NetworkManager.Singleton;



        public event Action<NetworkConnection, byte[]> Connected;
        public event Action<NetworkConnection> Disconnected;
        public event Action<NetworkObject> ObjectAdded;
        public event Action<NetworkObject> ObjectRemoved;

        public Socket GetSocket()
        {
            return socket;
        }
        public bool HasHandler(ushort msgId)
        {
            if (handlers == null)
                return false;
            return handlers.ContainsKey(msgId);
        }


        public void RegisterHandler(ushort msgId, NetworkMessageDelegate handler)
        {
            if (handler == null)
                return;
            if (handlers == null)
                handlers = new();
            handlers[msgId] = handler;
        }

        public void UnregisterHandler(ushort msgId)
        {
            if (handlers == null)
                return;
            handlers.Remove(msgId);
        }

        public void SendMessage(ushort msgId, MessageBase msg = null)
        {
            var s = PackMessage(msgId, msg);
            if (s == null)
                return;
            SendPacket(msgId, s);
        }

        public void SendPacket(ushort msgId, byte[] packet)
        {
            NetworkManager.Log($"{ConnectionId} Send Msg: " + (msgId < (short)NetworkMsgId.Max ? (NetworkMsgId)msgId : msgId));
            if (NetworkManager.IsServer && connectionId == NetworkManager.ServerClientId)
            {
                HostHandleMessage(msgId, packet);
            }
            else
            {
                sendMsgQueue.Enqueue(packet);
            }
        }

        public byte[] PackMessage(ushort msgId, MessageBase msg = null)
        {
            byte[] bytes = null;
            try
            {
                NetworkWriter s;
                //NetworkManager.Log($"Send Msg: {(msgId < (int)NetworkMsgId.Max ? (NetworkMsgId)msgId : msgId)}");
                s = writePool.Get();

                s.BaseStream.Position = 0;
                s.BaseStream.SetLength(0);
                s.BeginWritePackage();
                s.SerializeValue(ref msgId);
                if (msg != null)
                {
                    msg.Serialize(s);
                }

                s.EndWritePackage();
                bytes = new byte[s.BaseStream.Length];
                s.BaseStream.Position = 0;
                s.BaseStream.Read(bytes, 0, bytes.Length);
                writePool.Unused(s);
            }
            catch (Exception ex)
            {
                NetworkManager.Log($"Write Message error, msgId: {msgId}, type: {msg.GetType().Name}");
                NetworkManager.LogException(ex);
            }
            return bytes;
        }


        public void Disconnect()
        {
            if (ownerSoket)
            {
                if (socket != null)
                {
                    try
                    {
                        ProcessSendMessage();
                    }
                    catch (Exception ex)
                    {
                        NetworkManager.Log(ex.Message);
                    }

                    try
                    {
                        socket.Disconnect(false);
                        socket.Dispose();
                    }
                    catch { }
                    socket = null;
                    ownerSoket = false;
                }
            }

            isConnecting = false;

            if (isConnected)
            {
                OnDisconnected();
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
            Connect(address, port, 0, null);
        }

        public void Connect(string address, int port, int version, byte[] data)
        {
            if (isListening)
                throw new Exception("is listen");

            if (!(isConnected || isConnecting))
            {
                //lastTryReconnectTime = DateTime.UtcNow;
                Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                ownerSoket = true;
                try
                {
                    s.Connect(address, port);
                    s.Blocking = false;
                }
                catch (Exception ex)
                {
                    s = null;
                    isConnecting = false;
                    //NetworkManager.Log($"Connect failed, address: {address}, port: {port}");
                    NetworkManager.LogException(ex);
                }

                if (s != null)
                {
                    Connect(s, version, data);
                }
            }

            this.address = address;
            this.port = port;
        }

        public void Connect(Socket socket, int version, byte[] data)
        {
            Disconnect();
            InitialSocket(socket);
            if (socket != null && socket.Connected)
            {
                isConnecting = true;
                SendMessage((ushort)NetworkMsgId.ConnectRequest, new ConnectRequestMessage()
                {
                    Version = version,
                    data = data,
                });

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
                sendMsgQueue = new();
            }
            else
            {
                while (sendMsgQueue.Count > 0)
                {
                    var tmp = sendMsgQueue.Dequeue();
                    //writePool.Unused(tmp);
                }
            }

            currentSendMsg = null;
        }
        internal void ProcessMessage()
        {
        }


        internal void Update()
        {
            if (socket == null)
                return;

            try
            {
                if (!socket.Connected)
                {
                    Disconnect();
                    return;
                }
            }
            catch (Exception ex)
            {
                Disconnect();
                NetworkManager.LogException(ex);
            }


            if (!(isConnecting || isConnected))
                return;

            UpdateObjects();

            ProcessSendMessage();

            ProcessReceiveMessage();



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
                        currentSendPosition = 0;
                        //if (currentSendMsg != null)
                        //{
                        //    ms = (MemoryStream)currentSendMsg.BaseStream;
                        //    ms.Position = 0;
                        //}
                    }
                }

                if (currentSendMsg == null)
                    break;


                //ms = (MemoryStream)currentSendMsg.BaseStream;
                //int count = (int)(ms.Length - ms.Position);
                int count = currentSendMsg.Length - currentSendPosition;
                if (count > 0)
                {

                    //int sendCount = socket.Send(ms.GetBuffer(), (int)ms.Position, count, SocketFlags.None);
                    int sendCount = socket.Send(currentSendMsg, currentSendPosition, count, SocketFlags.None);
                    if (sendCount > 0)
                    {
                        currentSendPosition += sendCount;
                        lastSendTime = DateTime.UtcNow;
                    }
                }
                if (currentSendPosition >= currentSendMsg.Length)
                {
                    //writePool.Unused(currentSendMsg);
                    currentSendMsg = null;
                    currentSendPosition = 0;
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

            try
            {
                if (!socket.Connected)
                {
                    Disconnect();
                    return;
                }

                int readCount = reader.ReadPackage();
                if (readCount > 0)
                {
                    lastReceiveTime = DateTime.UtcNow;
                }

                while (readCount > 0)
                {

                    ushort msgId = reader.ReadUInt16();

                    NetworkMessage netMsg = new NetworkMessage();
                    netMsg.MsgId = msgId;
                    netMsg.Connection = this;
                    netMsg.Reader = reader;
                    netMsg.rawPacket = reader.rawPacket;

                    NetworkManager.Log($"Client[{ConnectionId}] Receive Msg: {(msgId < (int)NetworkMsgId.Max ? (NetworkMsgId)msgId : msgId)}");
                    NetworkManager.InvokeHandler(netMsg);

                    readCount = reader.ReadPackage();
                    if (readCount > 0)
                    {
                        lastReceiveTime = DateTime.UtcNow;
                    }
                }
            }
            catch (Exception ex)
            {
                if (socket != null && socket.Connected)
                {
                    NetworkManager.LogException(ex);
                    //Debug.LogException(ex);
                }
                Disconnect();
                //throw;
            }
        }

        void HostHandleMessage(ushort msgId, byte[] packet)
        {
            MemoryStream ms = new MemoryStream(packet, 0, packet.Length, true, true);
            ms.Position = 0;
            ms.SetLength(packet.Length);
            NetworkReader reader = new NetworkReader(ms);
            reader.rawPacket = packet;

            NetworkMessage netMsg = new NetworkMessage();
            netMsg.MsgId = msgId;
            netMsg.Connection = this;
            netMsg.Reader = reader;
            netMsg.rawPacket = packet;
            NetworkManager.InvokeHandler(netMsg);
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

        internal void UpdateObjects()
        {
            if (!NetworkManager.IsServer)
            {
                foreach (var netObj in objects.Values)
                {
                    if (netObj != null)
                    {
                        try
                        {
                            netObj.InternalUpdate();
                        }
                        catch (Exception ex)
                        {
                            NetworkManager.LogException(ex);
                        }

                    }
                }
            }

            //foreach (var id in destoryObjIds)
            //{
            //    NetworkObject netObj;
            //    if (objects.TryGetValue(id, out netObj))
            //    {
            //        if (netObj != null)
            //            netObj.Despawn();
            //    }
            //}
            //  destoryObjIds.Clear(); 
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

            }
        }
        internal void RemoveObject(NetworkObject obj)
        {
            if (objects.ContainsKey(obj.InstanceId))
            {
                objects.Remove(obj.InstanceId);
                if (obj.IsSpawned)
                {
                    ObjectRemoved?.Invoke(obj);
                }
            }
        }

        internal bool ContainsObject(ulong instanceId)
        {
            return objects.ContainsKey(instanceId);
        }

        public NetworkObject GetObject(ulong instanceId)
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
        public void Ping()
        {
            long timestamp = NetworkManager.Timestamp;
            SendMessage((ushort)NetworkMsgId.Ping, PingMessage.Ping(timestamp));
        }


        public void OnConnected(byte[] data)
        {
            Connected?.Invoke(this, data);
        }
        public void OnDisconnected()
        {
            isConnecting = false;
            if (isConnected)
            {
                isConnected = false;
                NetworkManager.Log("Client Disconnect");
                Disconnected?.Invoke(this);
            }
        }
        public void OnObjectAdded(NetworkObject obj)
        {
            if (!obj.IsSpawned) return;
            NetworkManager.Log("Client Spawn Object: " + obj);
            ObjectAdded?.Invoke(obj);
        }
        public void OnObjectRemoved(NetworkObject obj)
        {
            if (!obj.IsSpawned) return;
            NetworkManager.Log("Client Despawn Object: " + obj);
            obj.IsSpawned = false;
            ObjectRemoved?.Invoke(obj);
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

        public override string ToString()
        {
            return $"Connection: {ConnectionId}";
        }

    }
}
