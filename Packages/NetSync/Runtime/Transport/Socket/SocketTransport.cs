using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Yanmonet.NetSync.Transport.Socket
{
    using Socket = System.Net.Sockets.Socket;

    public class SocketTransport : INetworkTransport
    {

        private Socket socket;
        public string address = "127.0.0.1";
        public string listenAddress = "0.0.0.0";
        public int port = 7777;
        private CancellationTokenSource cancellationTokenSource;
        private bool isServer;
        private bool isClient;
        private ulong nextClientId;
        private Dictionary<ulong, SocketClient> clients;
        private LinkedList<SocketClient> clientList;

        private Queue<NetworkEvent> eventQueue;

        private object lockObj = new();

        private Pool<NetworkWriter> writePool;
        NetworkManager networkManager;
        private Task acceptWorkerTask;
        public string ConnectFailReson;
        SemaphoreSlim semaphore;

        private DateTime startTime;


        private float NowTime
        {
            get
            {
                return (float)DateTime.Now.Subtract(startTime).TotalSeconds;
            }
        }

        public bool IsSupported => true;

        public ulong ServerClientId => NetworkManager.ServerClientId;

        private bool initialized;
        private SocketClient localClient;

        public void Initialize(NetworkManager networkManager = null)
        {
            initialized = true;
            this.networkManager = networkManager;
            clients = new Dictionary<ulong, SocketClient>();
            clientList = new LinkedList<SocketClient>();
            cancellationTokenSource = new CancellationTokenSource();

            writePool = new Pool<NetworkWriter>(() => new NetworkWriter(new MemoryStream()));

            semaphore = new SemaphoreSlim(2, 3);
            eventQueue = new();
            startTime = DateTime.Now;

        }

        public bool StartServer()
        {
            if (!initialized) throw new Exception("Not initailized");
            isServer = true;

            try
            {
                IPAddress ipAddress;
                if (string.IsNullOrEmpty(listenAddress))
                {
                    ipAddress = IPAddress.Any;
                }
                else
                {
                    ipAddress = IPAddress.Parse(listenAddress);
                }

                //server = new TcpListener(new IPEndPoint(ipAddress, port));
                socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                socket.Bind(new IPEndPoint(IPAddress.Parse(listenAddress), port));
                socket.Listen(port);

                acceptWorkerTask = Task.Run(AcceptWorker, cancellationTokenSource.Token);
            }
            catch (Exception ex)
            {
                LogException(ex);
                Shutdown();

                return false;
            }
            return true;
        }

        public bool StartClient()
        {
            if (!initialized) throw new Exception("Not initailized");

            isClient = true;

            try
            {
                socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                socket.Connect(address, port);
                socket.Blocking = false;
                localClient = new SocketClient(socket)
                {
                    cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationTokenSource.Token),
                    IsLocalClient = true,
                    IsAccept = true,
                    IsConnected = false
                };


                localClient.sendWorkerTask = Task.Run(() => SendWorker(localClient), localClient.cancellationTokenSource.Token);
                localClient.receiveWorkerTask = Task.Run(() => ReceiveWorker(localClient), localClient.cancellationTokenSource.Token);


                ConnectRequestMessage connRequest = new ConnectRequestMessage();
                if (networkManager != null)
                {
                    connRequest.Payload = networkManager.ConnectionData;
                }

                //client.Send(PackMessage(MsgId.ConnectRequest, connRequest));

                _SendMsg(localClient, MsgId.ConnectRequest, connRequest);

                float connTimeout = NowTime + 10f;

                while (true)
                {
                    if (!localClient.IsAccept)
                    {
                        break;
                    }

                    if (NowTime > connTimeout)
                    {
                        throw new Exception("Connect Timeout");
                    }
                    Thread.Sleep(1);
                }


                if (!localClient.IsConnected)
                {
                    Shutdown();
                    return false;
                }

            }
            catch (Exception ex)
            {
                LogException(ex);
                Shutdown();
                return false;
            }

            return true;
        }

        public void Stop()
        {
            if (isServer)
            {
                isServer = false;
            }

            if (isClient)
            {
                isClient = false;
                localClient.cancellationTokenSource.Cancel();
                localClient.sendEvent.Set();
                try
                {
                    localClient.sendWorkerTask.Wait();
                }
                catch { }

                try
                {
                    localClient.receiveWorkerTask.Wait();
                }
                catch { }
                localClient.sendEvent.Dispose();
                localClient = null;
            }


        }


        public bool PollEvent(out NetworkEvent @event)
        {
            if (eventQueue.Count > 0)
            {
                lock (lockObj)
                {
                    if (eventQueue.Count > 0)
                    {
                        @event = eventQueue.Dequeue();
                        return true;
                    }
                }
            }

            @event = default;
            return false;
        }

        void AcceptWorker()
        {
            var cancelToken = cancellationTokenSource.Token;

            while (true)
            {
                try
                {
                    if (cancelToken.IsCancellationRequested)
                        break;

                    SocketClient client = null;
                    Socket socketClient = socket.Accept();
                    if (socketClient != null)
                    {
                        try
                        {
                            client = new SocketClient(socketClient)
                            {
                                ClientId = ++nextClientId,
                                IsAccept = true,
                                IsConnected = false,
                                IsLocalClient = false,
                                //Stream = socketClient.GetStream(),
                                cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationTokenSource.Token),
                                //Writer = new NetworkWriter(socketClient.GetStream()),
                                //Reader = new NetworkReader(socketClient.GetStream(), new MemoryStream(1024 * 4)),
                            };

                            clients[client.ClientId] = client;
                            clientList.AddLast(client);

                            client.sendWorkerTask = Task.Run(() => SendWorker(client), client.cancellationTokenSource.Token);
                            client.receiveWorkerTask = Task.Run(() => ReceiveWorker(client), client.cancellationTokenSource.Token);
                            if (networkManager?.LogLevel <= LogLevel.Debug)
                            {
                                Log($"Accept Client {client.ClientId}");
                            }
                        }
                        catch (Exception ex)
                        {
                            if (client != null)
                            {
                                DisconnectRemoteClient(client.ClientId);
                                client = null;
                            }
                        }

                        if (client == null)
                        {
                            try
                            {
                                socketClient.Close();
                            }
                            catch { }
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (cancelToken.IsCancellationRequested)
                    {
                        return;
                    }
                }
                finally
                {
                }
            }
        }




        public void Send(ulong clientId, ArraySegment<byte> payload, NetworkDelivery delivery)
        {
            SocketClient client;
            if (isClient && (clientId == ServerClientId))
            {
                client = localClient;
            }
            else
            {
                if (!clients.TryGetValue(clientId, out client))
                    return;
            }

            Packet packet = new Packet();
            packet.ReceiverClientId = clientId;
            packet.Delivery = delivery;
            packet.Payload = new ArraySegment<byte>(PackMessage(MsgId.Data, payload));

            lock (this)
            {
                client.sendPacketQueue.Enqueue(packet);
            }
            client.sendEvent.Set();
        }

        //SendWorker 和 ReceiveWorker 分成两个方法并发发送和接收
        private async void SendWorker(SocketClient client)
        {
            var cancelToken = client.cancellationTokenSource.Token;
            Packet packet = default;
            bool hasPacket = false;
            int sendCount = 0;
            Socket socket = client.socket;

            while (true)
            {
                if (hasPacket)
                {
                    client.sendEvent.WaitOne(100);
                }
                else
                {
                    client.sendEvent.WaitOne();
                }

                semaphore.Wait();
                try
                {
                    if (cancelToken.IsCancellationRequested)
                        break;

                    if (client.WillDisconnect && client.sendPacketQueue.Count == 0)
                        break;

                    if (!socket.Connected)
                        break;

                    while (client.sendPacketQueue.Count > 0)
                    {
                        if (!hasPacket)
                        {
                            if (client.sendPacketQueue.Count > 0)
                            {
                                lock (lockObj)
                                {
                                    if (client.sendPacketQueue.Count > 0)
                                    {
                                        packet = client.sendPacketQueue.Dequeue();
                                        hasPacket = true;
                                        sendCount = 0;
                                    }
                                }
                            }
                        }

                        if (hasPacket)
                        {
                            var payload = packet.Payload;

                            if (networkManager?.LogLevel <= LogLevel.Debug)
                            {
                                Log($"[{client.ClientId}] Send [{packet.MsgId}] Msg, bytes: {payload.Count}");
                            }

                            int n;
                            try
                            {
                                n = socket.Send(payload.Array, payload.Offset + sendCount, payload.Count - sendCount, SocketFlags.None);
                            }
                            catch
                            {
                                try
                                {
                                    socket.Dispose();
                                }
                                catch { }
                                client.socket = null;
                                throw;
                            }
                            if (n > 0)
                            {
                                sendCount += n;
                                if (sendCount >= payload.Count)
                                {
                                    hasPacket = false;
                                }
                            }

                            if (hasPacket)
                                break;
                        }

                    }
                }
                catch (Exception ex)
                {
                    if (cancelToken.IsCancellationRequested)
                    {
                        break;
                    }
                    LogException(ex);
                    break;
                }
                finally
                {
                    semaphore.Release();
                }

            }

            if (client.IsLocalClient)
            {
                DisconnectLocalClient();
            }
            else
            {
                DisconnectRemoteClient(client.ClientId);
            }

            if (networkManager?.LogLevel <= LogLevel.Debug)
            {
                Log($"[{client.ClientId}] Send Worker Done");
            }
        }

        private async void ReceiveWorker(SocketClient client)
        {
            var cancelToken = client.cancellationTokenSource.Token;

            Packet packet;
            byte[] buffer = new byte[1024 * 4];
            int packageSize = 0;
            int offset = 0;
            var socket = client.socket;
            ArraySegment<byte> arrayBuffer = new ArraySegment<byte>(buffer);
            //networkManager?.Log($"Start ReceiveWorker: Local: {client.IsLocalClient}, {client.ClientId}, Time: {NowTime:0.#}");
            socket.Blocking = true;
            while (true)
            {
                try
                {

                    if (cancelToken.IsCancellationRequested)
                        break;

                    //var result = socket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, (a) =>
                    //{
                    //    Debug.Log("Callback");
                    //}, null);
                    //Debug.Log(result.AsyncWaitHandle.WaitOne());
                    //int total = socket.EndReceive(result);

                    int total;
                    try
                    {
                        total = await socket.ReceiveAsync(arrayBuffer, SocketFlags.None);
                    }
                    catch
                    {
                        try
                        {
                            socket.Dispose();
                        }
                        catch { }
                        client.socket = null;
                        break;
                    }

                    packageSize = 0;
                    offset = 0;
                    if (total > 0)
                    {
                        if (networkManager?.LogLevel <= LogLevel.Debug)
                        {
                            Log($"[{client.ClientId}] Receive: " + total);
                        }
                    }
                    semaphore.Wait();

                    try
                    {
                        if (cancelToken.IsCancellationRequested)
                            break;

                        if (!(client.IsAccept || client.IsConnected))
                            break;

                        if (client.WillDisconnect && total == 0)
                            break;

                        if (!socket.Connected)
                        {
                            break;
                        }


                        /*
                        while (true)
                        {

                            if (cancelToken.IsCancellationRequested)
                                break;

                            if (packageSize == 0)
                            {
                                if (socket.Available > 2)
                                {
                                    socket.Receive(buffer, 2, SocketFlags.None);
                                    packageSize = (ushort)(buffer[0] << 8) | (ushort)(buffer[1]);
                                    offset = 0;
                                }
                            }

                            if (packageSize > 0)
                            {
                                int count = packageSize - offset;
                                if (count > 0)
                                {
                                    int readCount;
                                    readCount = socket.Receive(buffer, offset, count, SocketFlags.None);

                                    if (readCount > 0)
                                    {
                                        offset += readCount;
                                    }
                                }
                            }

                            if (packageSize > 0 && offset >= packageSize)
                            {
                                packet = UnpackMessage(buffer, 0, packageSize);
                                offset = 0;
                                packageSize = 0;
                                packet.SenderClientId = client.ClientId;
                                try
                                {
                                    HandleReceiveMsg(client, packet);
                                }
                                catch (Exception ex)
                                {
                                    if (cancelToken.IsCancellationRequested)
                                        break;
                                    LogException(ex);
                                }
                            }
                            else
                            {
                                break;
                            }
                        }*/

                        if (total > 0)
                        {

                            while (true)
                            {
                                if (cancelToken.IsCancellationRequested)
                                    break;

                                if (offset >= total)
                                    break;
                                if (total - offset < 1)
                                {
                                    LogError($"Error receive byte, total: {total}, offset: {offset}");
                                    break;
                                }

                                packageSize = (ushort)(buffer[offset] << 8) | (ushort)(buffer[offset + 1]);
                                offset += 2;
                                packet = UnpackMessage(buffer, offset, packageSize);
                                offset += packageSize;

                                packet.SenderClientId = client.ClientId;
                                try
                                {
                                    if (networkManager?.LogLevel <= LogLevel.Debug)
                                    {
                                        Log($"[{client.ClientId}] Receive [{packet.MsgId}] Msg");
                                    }

                                    HandleReceiveMsg(client, packet);
                                }
                                catch (Exception ex)
                                {
                                    if (cancelToken.IsCancellationRequested)
                                        break;
                                    LogException(ex);
                                }
                            }
                        }
                    }
                    catch
                    {
                        if (cancelToken.IsCancellationRequested)
                            break;
                        throw;
                    }
                    finally
                    {
                        semaphore.Release();
                    }

                    if (total <= 0)
                    {
                        client.receiveEvent.WaitOne(10);
                        continue;
                    }

                }
                catch (Exception ex)
                {
                    if (cancelToken.IsCancellationRequested)
                        break;

                    LogException(ex);
                    throw;
                }
            }

            if (client.IsLocalClient)
            {
                DisconnectLocalClient();
            }
            else
            {
                DisconnectRemoteClient(client.ClientId);
            }

            if (networkManager?.LogLevel <= LogLevel.Debug)
            {
                Log($"[{client.ClientId}] Receive Worker Done");
            }
        }

        private void HandleReceiveMsg(SocketClient client, Packet packet)
        {
            NetworkReader reader;
            reader = new NetworkReader(packet.Payload);

            switch (packet.MsgId)
            {
                case MsgId.ConnectRequest:
                    {
                        if (!isServer)
                            return;
                        ConnectResponseMessage response = new ConnectResponseMessage();

                        try
                        {
                            var request = new ConnectRequestMessage();
                            request.NetworkSerialize(reader);

                            client.IsConnected = true;
                            client.IsAccept = false;

                            response.Success = true;
                            response.ClientId = client.ClientId;

                        }
                        catch (Exception ex)
                        {
                            response.Success = false;
                            response.Reson = ex.Message;
                        }

                        NetworkEvent eventData = new NetworkEvent()
                        {
                            Type = NetworkEventType.Connect,
                            ReceiveTime = NowTime,
                            ClientId = client.ClientId,
                        };

                        lock (lockObj)
                        {
                            eventQueue.Enqueue(eventData);
                        }
                        _SendMsg(packet.SenderClientId, MsgId.ConnectResponse, response);
                    }
                    break;
                case MsgId.ConnectResponse:
                    {
                        ConnectResponseMessage response = new ConnectResponseMessage();
                        response.NetworkSerialize(reader);
                        client.ClientId = response.ClientId;
                        if (response.Success)
                        {
                            if (client.IsAccept)
                            {
                                NetworkEvent eventData = new NetworkEvent()
                                {
                                    Type = NetworkEventType.Connect,
                                    ClientId = client.ClientId,
                                    ReceiveTime = NowTime,
                                };
                                lock (lockObj)
                                {
                                    eventQueue.Enqueue(eventData);
                                    client.IsConnected = true;
                                    client.IsAccept = false;
                                }

                            }
                        }
                        else
                        {
                            if (client.IsAccept)
                            {
                                client.IsAccept = false;
                                ConnectFailReson = response.Reson;
                            }
                        }
                    }
                    break;
                case MsgId.Disconnect:
                    {
                        if (client.IsLocalClient)
                        {
                            DisconnectLocalClient(false);
                        }
                        else
                        {
                            DisconnectRemoteClient(packet.SenderClientId, false);
                        }
                    }
                    break;
                case MsgId.Data:
                    {
                        byte[] bytes = new byte[packet.Payload.Count];
                        packet.Payload.CopyTo(bytes, 0);
                        NetworkEvent eventData = new NetworkEvent()
                        {
                            Type = NetworkEventType.Data,
                            ClientId = packet.SenderClientId,
                            Payload = new ArraySegment<byte>(bytes),
                            ReceiveTime = NowTime,
                        };
                        lock (lockObj)
                        {
                            eventQueue.Enqueue(eventData);
                        }
                    }
                    break;
            }
        }

        void _SendMsg(ulong clientId, MsgId msgId, INetworkSerializable msg)
        {
            SocketClient client;
            if (isClient && clientId == localClient.ClientId)
            {
                client = localClient;
            }
            else
            {
                if (!clients.TryGetValue(clientId, out client))
                {
                    return;
                }
            }
            _SendMsg(client, msgId, msg);
        }

        void _SendMsg(SocketClient client, MsgId msgId, INetworkSerializable msg)
        {

            Packet packet = new Packet();
            packet.MsgId = msgId;
            packet.ReceiverClientId = client.ClientId;
            packet.Payload = PackMessage(msgId, msg);

            lock (lockObj)
            {
                client.sendPacketQueue.Enqueue(packet);
            }
            client.sendEvent.Set();
        }


        private byte[] PackMessage(MsgId msgId, INetworkSerializable payload)
        {
            byte[] bytes = null;

            NetworkWriter s;
            //NetworkManager.Log($"Send Msg: {(msgId < (int)NetworkMsgId.Max ? (NetworkMsgId)msgId : msgId)}");
            s = writePool.Get();

            s.BeginWritePackage();
            {
                byte _msgId = (byte)msgId;
                s.SerializeValue(ref _msgId);

                if (payload != null)
                {
                    payload.NetworkSerialize(s);
                }
            }
            s.EndWritePackage();

            bytes = new byte[s.BaseStream.Length];
            s.BaseStream.Position = 0;
            s.BaseStream.Read(bytes, 0, bytes.Length);
            writePool.Unused(s);

            return bytes;
        }

        private byte[] PackMessage(MsgId msgId, ArraySegment<byte> payload)
        {
            byte[] bytes = null;

            NetworkWriter s;
            //NetworkManager.Log($"Send Msg: {(msgId < (int)NetworkMsgId.Max ? (NetworkMsgId)msgId : msgId)}");
            s = writePool.Get();

            s.BeginWritePackage();
            {
                byte _msgId = (byte)msgId;
                s.SerializeValue(ref _msgId);

                if (payload.Count > 0)
                {
                    s.WriteRaw(payload);
                }
            }
            s.EndWritePackage();

            bytes = new byte[s.BaseStream.Length];
            s.BaseStream.Position = 0;
            s.BaseStream.Read(bytes, 0, bytes.Length);
            writePool.Unused(s);

            return bytes;
        }

        private Packet UnpackMessage(byte[] buffer, int offset, int length)
        {
            int count = length;
            MsgId msgId = (MsgId)buffer[offset];
            byte[] data = null;
            count--;
            offset++;
            data = new byte[count];
            Array.Copy(buffer, offset, data, 0, count);
            Packet packet = new Packet();
            packet.MsgId = msgId;
            packet.Payload = new ArraySegment<byte>(data);
            return packet;
        }

        public void DisconnectLocalClient()
        {
            DisconnectLocalClient(true);
        }

        private void DisconnectLocalClient(bool sendMsg)
        {
            SocketClient client = null;
            lock (lockObj)
            {
                client = this.localClient;
                if (client == null)
                    return;
                this.localClient = null;
            }

            DisconnectClient(client, sendMsg);

        }

        public void DisconnectRemoteClient(ulong clientId)
        {
            DisconnectRemoteClient(clientId, true);
        }

        void DisconnectRemoteClient(ulong clientId, bool sendMsg)
        {
            SocketClient client;
            lock (lockObj)
            {
                if (!clients.TryGetValue(clientId, out client))
                    return;
                clients.Remove(clientId);
                clientList.Remove(client);
            }

            DisconnectClient(client, sendMsg);
        }

        void DisconnectClient(SocketClient client, bool sendMsg)
        {
            lock (lockObj)
            {
                if (client.WillDisconnect)
                    return;
                client.WillDisconnect = true;
            }

            //断开超时时间
            client.cancellationTokenSource.CancelAfter(100);

            try
            {
                if (client.socket != null)
                {
                    client.socket.NoDelay = true;
                }
            }
            catch { }

            if (client.IsConnected)
            {
                eventQueue.Enqueue(new NetworkEvent()
                {
                    Type = NetworkEventType.Disconnect,
                    ClientId = client.ClientId,
                    ReceiveTime = NowTime
                });

                if (sendMsg)
                {
                    lock (lockObj)
                    {
                        _SendMsg(client, MsgId.Disconnect, null);
                    }
                }
            }

            client.sendEvent.Set();
            try { client.sendWorkerTask.Wait(); } catch { }
            client.sendEvent.Dispose();

            client.cancellationTokenSource.Cancel();

            if (client.socket != null)
            {
                try
                {
                    client.socket.Dispose();
                }
                catch { }
            }

            client.receiveEvent.Set();
            try { client.receiveWorkerTask.Wait(); } catch { }
            client.receiveEvent.Dispose();

            client.IsConnected = false;
        }

        public void Shutdown()
        {
            if (!(isServer || isClient))
                return;

            if (isClient)
            {
                isClient = false;
                DisconnectLocalClient();
            }

            cancellationTokenSource.Cancel();

            if (isServer)
            {
                isServer = false;

                float timeout = NowTime + 1f;
                while (clients.Count > 0 && NowTime < timeout)
                {
                    Thread.Sleep(0);
                }

            }

            if (socket != null)
            {
                try { socket.Dispose(); } catch { }
                socket = null;
            }

            if (acceptWorkerTask != null)
            {
                acceptWorkerTask.Wait();
                acceptWorkerTask = null;
            }

            if (semaphore != null)
            {
                semaphore.Dispose();
                semaphore = null;
            }

            if (networkManager?.LogLevel <= LogLevel.Debug)
            {
                Log($"Shutdown");
            }
        }


        #region Log

        public bool logEnabled;


        public void Log(string msg)
        {
            if (!logEnabled) return;
            networkManager?.Log($"[{(isServer ? "Server" : "Client")}] {msg}");
        }
        public void LogError(string error)
        {
            if (!logEnabled) return;
            networkManager?.LogError($"[{(isServer ? "Server" : "Client")}] {error}");
        }
        public void LogException(Exception ex)
        {
            if (!logEnabled) return;
            networkManager.LogException(ex);
        }

        #endregion

        public override string ToString()
        {
            return $"{address}:{port}";
        }

        ~SocketTransport()
        {
            if (cancellationTokenSource != null && !cancellationTokenSource.IsCancellationRequested)
            {
                cancellationTokenSource.Cancel();
            }
        }

    }


    struct Packet
    {
        public MsgId MsgId;
        public ulong SenderClientId;
        public ulong ReceiverClientId;
        public ArraySegment<byte> Payload;
        public NetworkDelivery Delivery;
    }


    enum MsgId
    {
        ConnectRequest = 1,
        ConnectResponse,
        Disconnect,
        Data
    }


}