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
    public class NetworkDiscovery
    {

        private UdpClient sendClient;
        private UdpClient receiveClient;
        private Dictionary<ushort, Action<IReaderWriter, IPEndPoint>> msgHandlers;
        private byte[] sendDiscoveryBytes;
        private string identifier;
        private int version;
        private string name;
        private string serverAddress;
        private int serverPort;
        private byte[] discoveryData;
        private byte[] lookupData;
        private DateTime nextBroadcastTime;
        private CancellationTokenSource cancellationTokenSource;
        private CancellationToken cancellationToken;
        private int port;
        private IPEndPoint sendAddress;


        public NetworkDiscovery()
        {
            msgHandlers = new();

            msgHandlers[(ushort)DiscoveryMsgIds.Lookup] = (reader, endPoint) =>
            {
                var data = new LookupMessage();
                data.Deserialize(reader);
                if (LookupCallback != null)
                {
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


        public string Name
        {
            get => name;
            set
            {
                if (name != value)
                {
                    name = value;
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

        public int Port
        {
            get => port;
            set
            {
                if (port != value)
                {
                    port = value;
                    sendAddress = null;
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
                discoveryData.name = name;
                discoveryData.serverAddress = serverAddress;
                discoveryData.serverPort = serverPort;
                discoveryData.userData = this.discoveryData;

                sendDiscoveryBytes = NetworkUtility.PackMessage((ushort)DiscoveryMsgIds.Discovery, discoveryData);
            }

            return sendDiscoveryBytes;
        }

        public void Start()
        {
            if (cancellationTokenSource != null)
            {
                Stop();
            }
            cancellationTokenSource = new CancellationTokenSource();
            cancellationToken = cancellationTokenSource.Token;
            StartServer();
            StartClient();
        }

        public void StartServer()
        {
            DateTime startTime = DateTime.Now;
            sendClient = new UdpClient(new IPEndPoint(IPAddress.Any, 0));
            //多播地址: 224.0.0.0-239.255.255.255
            //局部多播地址: 224.0.0.0～224.0.0.255
            //局部广播地址: 255.255.255.255
            sendAddress = new IPEndPoint(IPAddress.Parse("255.255.255.255"), Port);

            NetworkManager.Singleton?.Log($"Start Discovery Server, Identifier: '{Identifier}', Target Address: {sendAddress},Server Address: {ServerAddress}, Server Port: {ServerPort}, ({DateTime.Now.Subtract(startTime).TotalMilliseconds:0}ms)");

            if (cancellationTokenSource == null)
            {
                cancellationTokenSource = new CancellationTokenSource();
                cancellationToken = cancellationTokenSource.Token;
            }
        }

        public async Task StartClient()
        {
            DateTime startTime = DateTime.Now;

            receiveClient = new UdpClient(new IPEndPoint(IPAddress.Any, Port));

            NetworkManager.Singleton?.Log($"Start Discovery Client, Liststen: {receiveClient.Client.LocalEndPoint}, ({DateTime.Now.Subtract(startTime).TotalMilliseconds:0}ms)");

            MemoryStream stream = new MemoryStream(1024 * 4);
            NetworkReader reader = new NetworkReader(stream, null);

            if (cancellationTokenSource == null)
            {
                cancellationTokenSource = new CancellationTokenSource();
                cancellationToken = cancellationTokenSource.Token;
            }

            UdpReceiveResult result;
            while (true)
            {
                try
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;


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
                            if (packCount > 1)
                                NetworkManager.Singleton?.Log("Read Package count: " + packCount);
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
            try
            {
                sendClient.Dispose();
            }
            catch { }
            sendClient = null;
            //NetworkManager.Singleton?.Log($"Stop Discovery Client");
        }

        public void Update()
        {
            if (sendClient != null)
            {
                if (DateTime.Now > nextBroadcastTime)
                {
                    SendDiscoveryMsg();
                }
            }

            if (receiveClient != null)
            {

            }
        }


        private async Task SendDiscoveryMsg()
        {
            nextBroadcastTime = DateTime.Now.AddSeconds(BroadcastInterval);

            try
            {
                if (cancellationToken.IsCancellationRequested)
                    return;


                byte[] serverBroadcastBytes = GetSendDiscoveryData();

                await sendClient.SendAsync(serverBroadcastBytes, serverBroadcastBytes.Length, sendAddress);
                //NetworkManager.Singleton?.Log($"SendDiscoveryMsg");
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        public void Stop()
        {
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