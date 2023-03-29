using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Management;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor.PackageManager;
using static PlasticPipe.PlasticProtocol.Client.ConnectionCreator.PlasticProtoSocketConnection;

namespace Yanmonet.NetSync
{
    public class SoketTransport : INetworkTransport 
    {
        private TcpListener server;
        private Socket socket;
        public string address = "127.0.0.1";
        public string listenAddress = "0.0.0.0";
        public int port = 7777;
        private CancellationTokenSource cancellationTokenSource;
        private bool isServer;
        private bool isClient;
        private ulong nextClientId;
        private Dictionary<ulong, ClientData> clients;
        private LinkedList<ClientData> clientList;

        private Queue<EventData> eventQueue;

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
        private ClientData localClient;

        public void Initialize(NetworkManager networkManager = null)
        {
            initialized = true;
            this.networkManager = networkManager;
            clients = new Dictionary<ulong, ClientData>();
            clientList = new LinkedList<ClientData>();
            cancellationTokenSource = new CancellationTokenSource();

            writePool = new Pool<NetworkWriter>(() => new NetworkWriter(new MemoryStream()));

            semaphore = new SemaphoreSlim(3, 3);
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

                server = new TcpListener(new IPEndPoint(ipAddress, port));

                server.Start();
                acceptWorkerTask = Task.Run(AcceptWorker, cancellationTokenSource.Token);
            }
            catch (Exception ex)
            {
                isServer = false;
                cancellationTokenSource.Cancel();

                if (server != null)
                {
                    try
                    {
                        server.Stop();
                    }
                    catch (Exception ex2) { }
                    server = null;
                }
                try
                {
                    acceptWorkerTask.Wait();
                }
                catch { }
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
                localClient = new ClientData()
                {
                    socket = socket,
                    cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationTokenSource.Token),
                    IsLocalClient = true,
                    //Stream = tcpClient.GetStream(),
                    IsAccept = true,
                    IsConnected = false,
                    //Reader = new NetworkReader(tcpClient.GetStream(), new MemoryStream(1024 * 4)),
                    //Writer = new NetworkWriter(tcpClient.GetStream()),
                    ClientId = ulong.MaxValue,
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

                float connTimeout = NowTime + 5f;

                while (true)
                {
                    if (!socket.Connected)
                    {
                        throw new Exception("Client Disconnected");
                    }

                    if (!localClient.IsAccept)
                    {
                        if (localClient.IsConnected)
                        {
                            break;
                        }
                        else
                        {
                            throw new Exception($"Connect fail, {ConnectFailReson}");
                        }
                    }

                    if (NowTime > connTimeout)
                    {
                        throw new Exception("Connect Timeout");
                    }
                }
            }
            catch (Exception ex)
            {
                isClient = false;
                networkManager?.LogError(ex.Message + "\n" + ex.StackTrace);
                if (localClient != null)
                {
                    if (localClient.cancellationTokenSource != null)
                    {
                        localClient.cancellationTokenSource.Cancel();
                    }
                    if (localClient.sendWorkerTask != null)
                    {
                        try { localClient.sendWorkerTask.Wait(); } catch { }
                    }
                    if (localClient.receiveWorkerTask != null)
                    {
                        try { localClient.receiveWorkerTask.Wait(); } catch { }
                    }
                    localClient = null;
                }
                if (socket != null)
                {
                    try { socket.Dispose(); } catch { }
                    socket = null;
                }

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
                localClient.sendWaitEvent.Set();
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
                localClient.sendWaitEvent.Dispose();
                localClient = null;
            }


        }

        class EventData
        {
            public NetworkEvent Event;
            public ulong ClientId;
            public ArraySegment<byte> Payload;
            public float ReceiveTime;
        }


        public NetworkEvent PollEvent(out ulong clientId, out ArraySegment<byte> payload, out float receiveTime)
        {
            EventData eventData = default;
            bool hasEventData = false;
            if (eventQueue.Count > 0)
            {
                lock (lockObj)
                {
                    if (eventQueue.Count > 0)
                    {
                        eventData = eventQueue.Dequeue();
                        hasEventData = true;
                    }
                }
            }

            if (hasEventData)
            {
                clientId = eventData.ClientId;
                payload = eventData.Payload;
                receiveTime = eventData.ReceiveTime;
                return eventData.Event;
            }

            clientId = 0;
            payload = null;
            receiveTime = 0;
            return NetworkEvent.None;
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

                    ClientData client = null;
                    Socket socketClient = server.AcceptSocket();
                    if (socketClient != null)
                    {
                        try
                        {
                            client = new ClientData()
                            {
                                ClientId = ++nextClientId,
                                socket = socketClient,
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
            if (!clients.TryGetValue(clientId, out var client))
                return;

            Packet packet = new Packet();
            packet.ReceiverClientId = clientId;
            packet.Delivery = delivery;
            packet.Payload = new ArraySegment<byte>(PackMessage(MsgId.Data, payload));

            lock (this)
            {
                client.sendPacketQueue.Enqueue(packet);
                client.sendWaitEvent.Set();
            }
        }

        //SendWorker 和 ReceiveWorker 分开，并发发送和接收
        private async void SendWorker(ClientData client)
        {
            var cancelToken = client.cancellationTokenSource.Token;
            Packet packet = default;
            bool hasPacket = false;
            int sendCount = 0;
            while (true)
            {
                if (cancelToken.IsCancellationRequested)
                    break;

                semaphore.Wait();
                networkManager?.Log($"SendWorker: Local: {client.IsLocalClient}, {client.ClientId}");
                try
                {
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
                            int n = socket.Send(payload.Array, payload.Offset + sendCount, payload.Count - sendCount, SocketFlags.None);
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
                    networkManager?.LogException(ex);
                    throw;
                }
                finally
                {
                    semaphore.Release();
                }

                Thread.Sleep(100);
            }
        }

        private async void ReceiveWorker(ClientData client)
        {
            var cancelToken = client.cancellationTokenSource.Token;

            Packet packet;
            int packageSize = 0;
            var bufferReader = client.ReaderBuffer;
            var buffer = bufferReader.GetBuffer();
            var socket = client.socket;


            while (true)
            {
                if (cancelToken.IsCancellationRequested)
                    break;

                semaphore.Wait();
                networkManager?.Log($"ReceiveWorker: Local: {client.IsLocalClient}, {client.ClientId}");

                try
                {


                    if (!socket.Connected)
                    {
                        if (client.IsConnected)
                        {
                            client.IsAccept = false;
                            client.IsConnected = false;
                            lock (lockObj)
                            {
                                if (client.IsLocalClient)
                                {
                                    DisconnectLocalClient();
                                }
                                else
                                {
                                    DisconnectRemoteClient(client.ClientId);
                                }

                                eventQueue.Enqueue(new EventData()
                                {
                                    Event = NetworkEvent.Disconnect,
                                    ClientId = client.ClientId,
                                    ReceiveTime = NowTime,
                                });
                            }
                        }
                        break;
                    }

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

                                bufferReader.Position = 0;
                                bufferReader.SetLength(packageSize);
                            }
                        }

                        if (packageSize > 0)
                        {
                            int count = packageSize - (int)bufferReader.Position;
                            if (count > 0)
                            {
                                int readCount;
                                readCount = socket.Receive(buffer, (int)bufferReader.Position, count, SocketFlags.None);

                                if (readCount > 0)
                                {
                                    bufferReader.Position += readCount;
                                }
                            }
                        }

                        if (bufferReader.Position >= packageSize)
                        {
                            bufferReader.Position = 0;
                            packet = UnpackMessage(buffer, 0, packageSize);
                            packet.SenderClientId = client.ClientId;
                            HandleReceiveMsg(client, packet);
                        }
                        else
                        {
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
                    networkManager?.LogException(ex);
                    throw;
                }
                finally
                {
                    semaphore.Release();
                }
                Thread.Sleep(100);
            }
        }

        private void HandleReceiveMsg(ClientData client, Packet packet)
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
                            if (networkManager != null)
                            {
                                networkManager.ValidateConnect(0, request.Payload);
                            }

                            response.Success = true;
                            response.ClientId = client.ClientId;
                        }
                        catch (Exception ex)
                        {
                            response.Success = false;
                            response.Reson = ex.Message + "\n" + ex.Message;
                        }

                        EventData eventData = new EventData()
                        {
                            Event = NetworkEvent.Connect,
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
                                client.IsConnected = true;
                                client.IsAccept = false;
                                if (!client.IsLocalClient)
                                {
                                    EventData eventData = new EventData()
                                    {
                                        Event = NetworkEvent.Connect,
                                        ClientId = client.ClientId,
                                        ReceiveTime = NowTime,
                                    };
                                    lock (lockObj)
                                    {
                                        eventQueue.Enqueue(eventData);
                                    }
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
                        DisconnectRemoteClient(packet.SenderClientId);
                    }
                    break;
                case MsgId.Data:
                    {
                        byte[] bytes = new byte[packet.Payload.Count];
                        packet.Payload.CopyTo(bytes, 0);
                        EventData eventData = new EventData()
                        {
                            Event = NetworkEvent.Data,
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
            if (!clients.TryGetValue(clientId, out var client))
            {
                return;
            }
            _SendMsg(client, msgId, msg);
        }

        void _SendMsg(ClientData client, MsgId msgId, INetworkSerializable msg)
        {

            Packet packet = new Packet();
            packet.MsgId = msgId;
            packet.ReceiverClientId = client.ClientId;
            packet.Payload = PackMessage(msgId, msg);

            lock (lockObj)
            {
                client.sendPacketQueue.Enqueue(packet);
            }
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
            data = new byte[count];
            Array.Copy(buffer, offset, data, 0, count);
            Packet packet = new Packet();
            packet.MsgId = msgId;
            packet.Payload = new ArraySegment<byte>(data);
            return packet;
        }

        public void DisconnectLocalClient()
        {
            if (localClient.IsConnected)
            {
                localClient.IsConnected = false;

                lock (lockObj)
                {
                    eventQueue.Enqueue(new EventData()
                    {
                        Event = NetworkEvent.Disconnect,
                        ClientId = localClient.ClientId,
                        ReceiveTime = NowTime,
                    });
                }
            }
        }

        public void DisconnectRemoteClient(ulong clientId)
        {
            DisconnectRemoteClient(clientId, true);
        }

        void DisconnectRemoteClient(ulong clientId, bool sendMsg)
        {
            lock (lockObj)
            {
                if (clients.TryGetValue(clientId, out var client))
                {
                    clients.Remove(clientId);
                    clientList.Remove(client);

                    if (client.IsConnected)
                    {
                        client.IsConnected = false;

                        eventQueue.Enqueue(new EventData()
                        {
                            Event = NetworkEvent.Disconnect,
                            ClientId = clientId,
                            ReceiveTime = NowTime
                        });

                        //if (sendMsg)
                        //{
                        //    try
                        //    {
                        //        client.Stream.Write();
                        //        client.Stream.Flush();
                        //    }
                        //    catch { }
                        //}
                    }

                }
            }
        }

        public void Shutdown()
        {
            
        }
         

        class ClientData
        {
            public ulong ClientId;
            public Socket socket;
            public bool IsAccept;
            public bool IsConnected;
            public bool IsLocalClient;
            public bool processConnectEvent;
            public CancellationTokenSource cancellationTokenSource;


            public AutoResetEvent sendWaitEvent;
            public Task sendWorkerTask;
            public Task receiveWorkerTask;
            public Queue<Packet> sendPacketQueue;
            public Queue<Packet> receivePacketQueue;

            public MemoryStream ReaderBuffer;
            public int ReaderPacketLength;


            public ClientData()
            {
                sendWaitEvent = new AutoResetEvent(false);
                sendPacketQueue = new Queue<Packet>();
                receivePacketQueue = new Queue<Packet>();
                ReaderBuffer = new MemoryStream(new byte[1024 * 4]);

                ReaderPacketLength = 0;
            }

            public void Send(byte[] buffer)
            {
                if (buffer == null) return;
                Send(buffer, 0, buffer.Length);
            }

            public void Send(byte[] buffer, int offset, int length)
            {
                for (int i = 0; i < length;)
                {
                    int sendCount = socket.Send(buffer, offset + i, length - i, SocketFlags.None);
                    if (sendCount > 0)
                    {
                        i += sendCount;
                    }
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

        class ConnectRequestMessage : INetworkSerializable
        {
            public byte[] Payload;

            public void NetworkSerialize(IReaderWriter readerWriter)
            {
                if (readerWriter.IsReader)
                {
                    Payload = null;
                    int length = 0;
                    readerWriter.SerializeValue(ref Payload, 0, ref length);
                }
                else
                {
                    int length = 0;
                    if (Payload != null)
                        length = Payload.Length;
                    readerWriter.SerializeValue(ref Payload, 0, ref length);
                }
            }
        }

        class ConnectResponseMessage : INetworkSerializable
        {
            public bool Success;
            public string Reson;
            public ulong ClientId;

            public void NetworkSerialize(IReaderWriter readerWriter)
            {
                readerWriter.SerializeValue(ref Success);
                readerWriter.SerializeValue(ref Reson);
                readerWriter.SerializeValue(ref ClientId);

            }
        }

    }
}