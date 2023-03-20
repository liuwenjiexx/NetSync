using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices.ComTypes;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Yanmonet.NetSync
{
    public class NetworkDiscovery
    {

        private UdpClient sendClient;
        private UdpClient receiveClient;
        private Dictionary<ushort, Action<IReaderWriter, IPEndPoint>> msgHandlers;
        private byte[] sendDiscoveryBytes;
        private byte[] sendLookupBytes;
        private string identifier;
        private int version;
        private string serverName;
        private string serverAddress;
        private int serverPort;
        private byte[] discoveryData;
        private byte[] lookupData;
        private DateTime? nextBroadcastTime;
        private CancellationTokenSource cancellationTokenSource;
        private CancellationToken cancellationToken;
        private int portMin;
        private int portMax;
        private List<IPEndPoint> broadcastAddressList;
        private bool initalized;


        public NetworkDiscovery()
        {
            msgHandlers = new();

            msgHandlers[(ushort)DiscoveryMsgIds.Lookup] = (reader, endPoint) =>
            {
                var data = new LookupMessage();
                data.Deserialize(reader);
                if (LookupCallback != null)
                {
                    nextBroadcastTime = DateTime.MinValue;
                    LookupCallback(data.userData);
                }
            };


            msgHandlers[(ushort)DiscoveryMsgIds.Discovery] = (reader, endPoint) =>
            {
                var data = new DiscoveryMessage();
                data.Deserialize(reader);

                if (data.identifier == Identifier)
                {
                    if (DiscoveryCallback != null)
                    {
                        DiscoveryData discoveryData = new DiscoveryData()
                        {
                            EndPoint = endPoint,
                            Name = data.name,
                            ServerPort = data.serverPort,
                            UserData = data.userData,
                        };
                        DiscoveryCallback(discoveryData);
                    }

                    //Log($"Broadcast Client, Receive from {endPoint}, Server Address: {remoteAddress}, port: {remotePort}");

                }
            };
        }


        public string Identifier
        {
            get => identifier;
            set
            {
                if (identifier != value)
                {
                    identifier = value;
                    sendDiscoveryBytes = null;
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
                    sendDiscoveryBytes = null;
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
                    sendDiscoveryBytes = null;
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
                    sendDiscoveryBytes = null;
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
                    sendDiscoveryBytes = null;
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

        public byte[] DiscoveryData
        {
            get => discoveryData;
            set
            {
                if (discoveryData != value)
                {
                    discoveryData = value;
                    sendDiscoveryBytes = null;
                }
            }
        }
        public byte[] LookupData
        {
            get => lookupData;
            set
            {
                if (lookupData != value)
                {
                    lookupData = value;
                }
            }
        }

        public float BroadcastInterval { get; set; } = 3f;

        public event Action<DiscoveryData> DiscoveryCallback;
        public event Action<byte[]> LookupCallback;

        public delegate void DiscoveryCallbackDelegate(EndPoint endPoint, int version, string name, byte[] userData);

        public void RegisterHandler(ushort msgId, Action<IReaderWriter, IPEndPoint> handler)
        {
            msgHandlers[msgId] = handler;
        }

        private byte[] GetSendDiscoveryData()
        {
            if (sendDiscoveryBytes == null)
            {
                var discoveryData = new DiscoveryMessage();
                discoveryData.identifier = identifier;
                discoveryData.version = version;
                discoveryData.name = serverName;
                discoveryData.serverAddress = serverAddress;
                discoveryData.serverPort = serverPort;
                discoveryData.userData = this.discoveryData;

                sendDiscoveryBytes = NetworkUtility.PackMessage((ushort)DiscoveryMsgIds.Discovery, discoveryData);
            }

            return sendDiscoveryBytes;
        }

        private byte[] GetSendLookupData()
        {
            if (sendLookupBytes == null)
            {
                var lookupMsg = new LookupMessage();
                lookupMsg.identifier = Identifier;
                lookupMsg.version = version;
                lookupMsg.name = serverName;
                lookupMsg.userData = lookupData;


                sendLookupBytes = NetworkUtility.PackMessage((ushort)DiscoveryMsgIds.Lookup, lookupMsg);
            }

            return sendLookupBytes;
        }

        private void Initalize()
        {
            if (initalized)
                return;
            initalized = true;
            cancellationTokenSource = new CancellationTokenSource();
            cancellationToken = cancellationTokenSource.Token;

            broadcastAddressList = new List<IPEndPoint>();
            //多播地址: 224.0.0.0-239.255.255.255
            //局部多播地址: 224.0.0.0～224.0.0.255
            //局部广播地址: 255.255.255.255
            for (int i = PortMin; i <= PortMax; i++)
            {
                broadcastAddressList.Add(new IPEndPoint(IPAddress.Broadcast, i));
            }
        }

        public void Start()
        {
            if (cancellationTokenSource != null)
            {
                Stop();
            }

            Initalize();

            nextBroadcastTime = DateTime.Now;
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
                if (!NetworkUtility.IsUdpPortUsed(i))
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

            NetworkManager.Singleton?.Log($"Start Discovery Server [{ServerName}], Identifier: '{Identifier}', Liststen: {receiveClient.Client.LocalEndPoint}, ({DateTime.Now.Subtract(startTime).TotalMilliseconds:0}ms)");


            if (sendClient != null)
            {
                SendLookupMsg();
            }

            Task.Run(ReceiveWorker, cancellationTokenSource.Token);


            //NetworkManager.Singleton?.Log($"Stop Discovery Client");
        }

        public void Update()
        {
            if (sendClient != null)
            {
                if (nextBroadcastTime.HasValue && DateTime.Now > nextBroadcastTime)
                {
                    SendDiscoveryMsg();
                }
            }

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

        private async Task SendDiscoveryMsg()
        {
            nextBroadcastTime = DateTime.Now.AddSeconds(BroadcastInterval);

            if (sendClient == null)
                return;
            try
            {
                if (cancellationToken.IsCancellationRequested)
                    return;


                byte[] serverBroadcastBytes = GetSendDiscoveryData();

                foreach (var broadcastAddress in broadcastAddressList)
                {
                    await sendClient.SendAsync(serverBroadcastBytes, serverBroadcastBytes.Length, broadcastAddress);
                    if (cancellationToken.IsCancellationRequested)
                        return;
                }

                NetworkManager.Singleton?.Log($"SendDiscoveryMsg");
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }


        private async Task SendLookupMsg()
        {
            if (sendClient == null)
                return;

            try
            {
                if (cancellationToken.IsCancellationRequested)
                    return;


                byte[] bytes = GetSendLookupData();

                foreach (var broadcastAddress in broadcastAddressList)
                {
                    await sendClient.SendAsync(bytes, bytes.Length, broadcastAddress);
                    if (cancellationToken.IsCancellationRequested)
                        return;
                }

                NetworkManager.Singleton?.Log($"SendLookupMsg");
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        public void Stop()
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
            Discovery = 1,
            Lookup,
            Max
        }
    }



}