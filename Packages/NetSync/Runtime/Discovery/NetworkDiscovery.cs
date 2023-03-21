using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Yanmonet.NetSync
{
    public abstract class NetworkDiscovery<TRequest, TResponse>
        where TRequest : INetworkSerializable, new()
        where TResponse : INetworkSerializable, new()
    {

        protected UdpClient sendClient;
        private UdpClient receiveClient;
        private Dictionary<ushort, Action<IReaderWriter, IPEndPoint>> msgHandlers;
        private string identifier;
        private int version;
        private string serverName;
        private string serverAddress;
        private int serverPort;
        protected DateTime? nextBroadcastTime;
        private CancellationTokenSource cancellationTokenSource;
        private CancellationToken cancellationToken;
        private int portMin;
        private int portMax;
        private List<IPEndPoint> broadcastAddressList;
        private bool initalized;


        public NetworkDiscovery()
        {
            msgHandlers = new();

            RegisterHandler((ushort)DiscoveryMsgIds.DiscoveryRequest, DiscoveryRequestHandler);
            RegisterHandler((ushort)DiscoveryMsgIds.DiscoveryResponse, DiscoveryResponseHandler);
        }


        public string Identifier
        {
            get => identifier;
            set
            {
                if (identifier != value)
                {
                    identifier = value;
                }
            }
        }


        public int Version
        {
            get => version;
            set
            {
                if (version != value)
                {
                    version = value;
                }
            }
        }


        public string ServerName
        {
            get => serverName;
            set
            {
                if (serverName != value)
                {
                    serverName = value;
                }
            }
        }

        public string ServerAddress
        {
            get => serverAddress;
            set
            {
                if (serverAddress != value)
                {
                    serverAddress = value;
                }
            }
        }

        public int ServerPort
        {
            get => serverPort;
            set
            {
                if (serverPort != value)
                {
                    serverPort = value;
                }
            }
        }

        public int PortMin
        {
            get => portMin;
            set
            {
                if (portMin != value)
                {
                    portMin = value;
                    broadcastAddressList = null;
                }
            }
        }

        public int PortMax
        {
            get => portMax;
            set
            {
                if (portMax != value)
                {
                    portMax = value;
                    broadcastAddressList = null;
                }
            }
        }

        public float BroadcastInterval { get; set; } = 3f;

        public List<IPEndPoint> BroadcastAddressList => broadcastAddressList;

        public void RegisterHandler(ushort msgId, Action<IReaderWriter, IPEndPoint> handler)
        {
            msgHandlers[msgId] = handler;
        }

        void DiscoveryRequestHandler(IReaderWriter reader, IPEndPoint remote)
        {
            var request = new DiscoveryRequest<TRequest>();
            request.NetworkSerialize(reader);

            if (request.Identifier != Identifier)
                return;

            request.Remote = remote;

            OnDiscoveryRequest(request);

        }

        void DiscoveryResponseHandler(IReaderWriter reader, IPEndPoint remote)
        {
            var response = new DiscoveryResponse<TResponse>();
            response.NetworkSerialize(reader);

            if (response.Identifier != Identifier)
                return;
            response.Remote = remote;

            OnDiscoveryResponse(response);


        }

        private void Initalize()
        {
            if (initalized)
                return;
            initalized = true;
            cancellationTokenSource = new CancellationTokenSource();
            cancellationToken = cancellationTokenSource.Token;

            broadcastAddressList = new List<IPEndPoint>();

            nextBroadcastTime = DateTime.MinValue;
            //多播地址: 224.0.0.0-239.255.255.255
            //局部多播地址: 224.0.0.0～224.0.0.255
            //局部广播地址: 255.255.255.255
            for (int i = PortMin; i <= PortMax; i++)
            {
                broadcastAddressList.Add(new IPEndPoint(IPAddress.Broadcast, i));
            }
        }

        public virtual void Start()
        {
            if (cancellationTokenSource != null)
            {
                Stop();
            }

            Initalize();

            //    StartServer();
            //    StartClient();
            //}


            //public void StartServer()
            //{
            //    DateTime startTime = DateTime.Now;

            //    Initalize();
            //    sendClient = new UdpClient(new IPEndPoint(IPAddress.Any, 0));

            //    //sendClient = new UdpClient(Port)
            //    //{
            //    //    EnableBroadcast = true,
            //    //    MulticastLoopback = false
            //    //};

            //    NetworkManager.Singleton?.Log($"Start Discovery Server [{ServerName}], Identifier: '{Identifier}', Broadcast Port: [{PortMin}-{PortMax}],Server Address: {ServerAddress}, Server Port: {ServerPort}, ({DateTime.Now.Subtract(startTime).TotalMilliseconds:0}ms)");

            //}

            //public async Task StartClient()
            //{
            DateTime startTime = DateTime.Now;
            //Initalize();
            int port = 0;
            for (int i = PortMin; i <= PortMax; i++)
            {
                if (!NetworkUtility.IsPortUsed(i))
                {
                    port = i;
                    break;
                }
            }

            if (port == 0)
                return;


            //receiveClient = new UdpClient(new IPEndPoint(IPAddress.Any, Port));
            receiveClient = new UdpClient(port)
            {
                EnableBroadcast = true,
                MulticastLoopback = false
            };
            sendClient = receiveClient;

            //NetworkManager.Singleton?.Log($"Start Discovery Server [{ServerName}], Identifier: '{Identifier}', Liststen: {receiveClient.Client.LocalEndPoint}, ({DateTime.Now.Subtract(startTime).TotalMilliseconds:0}ms)");


            Task.Run(ReceiveWorker, cancellationTokenSource.Token);


            //NetworkManager.Singleton?.Log($"Stop Discovery Client");
        }

        public virtual void Update()
        {


            if (receiveClient != null)
            {

            }
        }

        async void ReceiveWorker()
        {
            UdpReceiveResult result;

            MemoryStream stream = new MemoryStream(1024 * 4);
            NetworkReader reader = new NetworkReader(stream, null);

            var cancellationToken = cancellationTokenSource.Token;

            while (true)
            {
                try
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;

                    //多网卡会重复收数据
                    result = await receiveClient.ReceiveAsync();

                    if (cancellationToken.IsCancellationRequested)
                        break;

                    var buffer = result.Buffer;

                    if (buffer != null && buffer.Length > 0)
                    {
                        reader.Reset();
                        //if (stream.Position == stream.Length)
                        {
                            stream.Position = 0;
                            stream.SetLength(0);
                        }

                        stream.Write(buffer, 0, buffer.Length);
                        stream.Position = 0;
                        int packetSize = 0;
                        ushort msgId = 0;
                        int packCount = 0;
                        while (reader.ReadPackage(out msgId, out packetSize))
                        {
                            packCount++;
                            if (msgHandlers.TryGetValue(msgId, out var handler))
                            {
                                handler(reader, result.RemoteEndPoint);
                            }
                            //if (packCount > 1)
                            //NetworkManager.Singleton?.Log("Read Package count: " + packCount + ", farme:" + result.RemoteEndPoint);
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;
                    Debug.LogException(ex);
                }
            }
        }

        private async Task Broadcast(byte[] data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            if (data.Length == 0) return;

            foreach (var broadcastAddress in broadcastAddressList)
            {
                await sendClient.SendAsync(data, data.Length, broadcastAddress);
                if (cancellationToken.IsCancellationRequested)
                    break;
            }
        }


        protected abstract void OnDiscoveryRequest(DiscoveryRequest<TRequest> request);
        protected abstract void OnDiscoveryResponse(DiscoveryResponse<TResponse> response);


        public async Task SendDiscoveryRequest(TRequest requestData)
        {
            nextBroadcastTime = DateTime.Now.AddSeconds(BroadcastInterval);

            if (sendClient == null)
                return;

            try
            {
                if (cancellationToken.IsCancellationRequested)
                    return;

                var data = new DiscoveryRequest<TRequest>();
                data.Identifier = Identifier;
                data.ServerName = serverName;
                data.Version = version;
                data.UserData = requestData;

                byte[] bytes = NetworkUtility.PackMessage((ushort)DiscoveryMsgIds.DiscoveryRequest, data);

                await Broadcast(bytes);

                //NetworkManager.Singleton?.Log($"SendLookupMsg");
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        public async Task SendDiscoveryResponse(TResponse responseData, IPEndPoint remote)
        {

            if (sendClient == null)
                return;
            try
            {
                if (cancellationToken.IsCancellationRequested)
                    return;

                var data = new DiscoveryResponse<TResponse>();
                data.Identifier = identifier;
                data.ServerName = serverName;
                data.Version = version;
                data.UserData = responseData;

                byte[] bytes = NetworkUtility.PackMessage((ushort)DiscoveryMsgIds.DiscoveryResponse, data);

                await sendClient.SendAsync(bytes, bytes.Length, remote);
                if (cancellationToken.IsCancellationRequested)
                    return;

                //NetworkManager.Singleton?.Log($"SendDiscoveryMsg");
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }


        public virtual void Stop()
        {
            initalized = false;

            if (cancellationTokenSource != null)
            {
                cancellationTokenSource.Cancel();
                cancellationTokenSource = null;
            }

            if (sendClient != null)
            {
                try
                {
                    sendClient.Dispose();
                }
                catch { }
                sendClient = null;
            }

            if (receiveClient != null)
            {
                try
                {
                    receiveClient.Dispose();
                }
                catch { }
                receiveClient = null;
            }

            //NetworkManager.Singleton?.Log($"Stop Discovery");
        }

        enum DiscoveryMsgIds
        {
            DiscoveryRequest = 1,
            DiscoveryResponse,
            Max
        }
    }



}