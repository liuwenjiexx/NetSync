using Yanmonet.NetSync.Messages;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Runtime.Serialization;

namespace Yanmonet.NetSync
{
    public class NetworkServer : IDisposable
    {
        private TcpListener server;
        private bool isRunning;


        internal Dictionary<ulong, NetworkObject> objects;
        internal uint nextObjectId;
        private ulong nextConnectionId;
        private List<ulong> destoryObjIds = new List<ulong>();
        private Type clientType;
        Task acceptClientTask;
        private int mainThreadId;
        private object lockObj;
        CancellationTokenSource cancellationTokenSource;

        public NetworkServer(NetworkManager manager, TcpListener server)
        {
            this.server = server;
            objects = new Dictionary<ulong, NetworkObject>();
            lockObj = new object();
            this.networkManager = manager;
        }
        public NetworkServer(NetworkManager manager = null)
            : this(manager, null)
        {
        }

        protected bool IsThreadSafe
        {
            get { return mainThreadId == 0 || mainThreadId == Thread.CurrentThread.ManagedThreadId; }
        }

        public bool IsRunning
        {
            get { return isRunning; }
        }

        public IEnumerable<NetworkConnection> Connections
        {
            get
            {
                foreach (var item in Clients)
                    yield return item.Connection;
            }
        }
        public IReadOnlyCollection<NetworkClient> Clients
        {
            get
            {
                return NetworkManager.clientList;
            }
        }
        public IEnumerable<NetworkObject> Objects { get => objects.Select(o => o.Value); }
        public Type ClientType { get => clientType; set => clientType = value; }
        private NetworkManager networkManager;
        public NetworkManager NetworkManager => networkManager ?? NetworkManager.Singleton;
        public event Action<NetworkServer> Started;
        public event Action<NetworkServer> Stoped;

        public event Action<NetworkServer, NetworkClient> ClientConnected;
        public event Action<NetworkServer, NetworkClient> ClientDisconnected;

        public void Start(int localPort)
        {
            Start("0.0.0.0", localPort);
        }

        public virtual void Start(string localAddress, int localPort)
        {
            if (isRunning)
                return;

            mainThreadId = Thread.CurrentThread.ManagedThreadId;

            IPAddress address = IPAddress.Parse(localAddress);
            TcpListener tcpListener = new TcpListener(new IPEndPoint(address, localPort));
            tcpListener.Start();
            this.server = tcpListener;
            cancellationTokenSource = new CancellationTokenSource();

            isRunning = true;
            NetworkManager.Log($"Network Server IP: {localAddress}, Port: {localPort}");
            NetworkManager.Log("Network Server Started");
            //Running(cancellationTokenSource.Token);
            try
            {
                if (Started != null)
                    Started(this);
            }
            catch (Exception ex) { throw ex; }

        }

        public virtual void Stop()
        {
            CheckThreadSafe();
            if (!isRunning)
                return;

            isRunning = false;
            cancellationTokenSource.Cancel();


            foreach (var obj in objects.Values.ToArray())
            {
                try
                {
                    obj.Despawn(true);
                }
                catch { }
            }
            objects.Clear();

            var clientList = NetworkManager.clientList;

            try
            {
                if (clientList != null)
                {
                    foreach (var host in clientList)
                    {
                        try { host.Connection.Dispose(); } catch { }
                    }

                }

            }
            catch { }

            clientList.Clear();
            NetworkManager.clients.Clear();
            NetworkManager.clientIds.Clear();
            NetworkManager.clientNodes.Clear();
            try
            {
                NetworkManager.Log("Server Stop");
                server.Stop();
            }
            catch { }
            try
            {
                if (Stoped != null)
                    Stoped(this);
            }
            catch (Exception ex)
            {
                NetworkManager.LogException(ex);
            }
            NetworkManager.Log("Network Server Stoped");
        }



        public void Update()
        {
            if (server == null)
                throw new Exception("listener null");

            LinkedListNode<NetworkClient> node;
            NetworkClient client;
            var clientList = NetworkManager.clientList;
            node = clientList.First;
            while (node != null)
            {
                client = node.Value;
                if (client == null || !(client.Connection.IsConnecting || client.Connection.IsConnected))
                {
                    node = node.RemoveAndNext();
                    //NetworkManager.Log($"Client {client.ClientId} Disconnected, IsConnecting: {client.Connection.IsConnecting}, IsConnected: {client.Connection.IsConnected}");
                    OnClientDisconnect(client);
                    ClientDisconnected?.Invoke(this, client);
                    continue;
                }

                client.Update();
                node = node.Next;
            }

            try
            {

                while (server.Pending())
                {
                    client = null;
                    TcpClient tcpClient = server.AcceptTcpClient();
                    if (tcpClient != null)
                    {
                        try
                        {
                            client = AcceptTcpClient(tcpClient, null);

                            if (client != null)
                            {
                                InitClient(client);
                                //if (!client.IsRunning)
                                //{
                                //client.Start();
                                //client.Running();
                                //}
                                RemoveConnection(client.Connection);
                                node = clientList.AddLast(client);
                                NetworkManager.clientNodes[client.ClientId] = node;
                                NetworkManager.clients[client.ClientId] = client;
                                NetworkManager.clientIds.AddLast(client.ClientId);

                            }
                        }
                        catch (Exception ex)
                        {
                            client = null;
                        }

                        if (client == null)
                        {
                            try
                            {
                                tcpClient.Client.Disconnect(false);
                                tcpClient.Close();
                            }
                            catch { }
                        }
                    }
                }


                UpdateObjects();
                OnUpdate();

            }
            catch (Exception ex)
            {
                NetworkManager.LogException(ex);
            }





        }




        private void InitClient(NetworkClient client)
        {

            client.Connection.Connected += (conn, netMsg) =>
              {
                  //try
                  //{
                  // OnClientConnect(client, netMsg);
                  //}
                  //catch (Exception ex)
                  //{
                  //    client.Stop();
                  //    Console.WriteLine(ex);
                  //}
                  //ClientConnected?.Invoke(this, client);

              };
        }


        public NetworkClient FindClient(ulong netId)
        {
            if (netId == NetworkManager.LocalClientId)
                return NetworkManager.LocalClient;

            return Clients.FirstOrDefault(o => o.ClientId == netId);
        }

        protected virtual void OnClientConnect(NetworkClient client, NetworkMessage netMsg)
        {
            //var msg = netMsg.ReadMessage<ConnectMessage>();
            if (netMsg.Connection.ConnectionId == 0)
            {

            }
            //ClientConnected?.Invoke(this, client);
        }


        protected virtual NetworkClient AcceptTcpClient(TcpClient netClient, MessageBase extra)
        {
            NetworkClient client;

            Type clientType = ClientType;
            if (clientType == null)
            {
                clientType = typeof(NetworkClient);
            }
            client = new NetworkClient(NetworkManager, this, netClient.Client, true, true);

            ulong connId = ++nextConnectionId;
            client.Connection.ConnectionId = connId;
            //NetworkManager.Log($"Accept Client {connId}, IsConnecting: {client.Connection.IsConnecting}, IsRunning: {client.IsRunning}");

            return client;
        }

        protected virtual void OnClientDisconnect(NetworkClient client)
        {
        }

        public NetworkClient GetClient(NetworkConnection connection)
        {
            LinkedListNode<NetworkClient> node;

            if (NetworkManager.clientNodes.TryGetValue(connection.ConnectionId, out node))
            {
                NetworkClient client = node.Value;
                //if (!client.IsRunning)
                //{
                //    RemoveConnection(connection);
                //    return null;
                //}
                return client;
            }
            return null;
        }

        public void RemoveConnection(NetworkConnection connection)
        {
            ulong clientId = connection.ConnectionId;
            if (NetworkManager.clientNodes.TryGetValue(clientId, out var node))
            {
                node.List.Remove(node);
                NetworkManager.clientNodes.Remove(clientId);
                NetworkManager.clientIds.Remove(clientId);
                NetworkManager.clients.Remove(clientId);
                try
                {
                    if (connection.IsConnected)
                        connection.Disconnect();
                    var client = node.Value;
                    //if (client.IsRunning)
                    //{
                    //client.Stop();
                    //}

                    ClientDisconnected?.Invoke(this, client);
                }
                catch { }
            }
        }

        protected virtual void OnUpdate() { }

        public void SendToAll(ushort msgId, MessageBase msg = null)
        {
            foreach (var conn in Connections)
            {
                conn.SendMessage(msgId, msg);
            }
        }

        #region Create Object

        public T CreateObject<T>(INetworkSerializable parameter = null)
            where T : NetworkObject
        {
            var id = NetworkManager.GetTypeId(typeof(T));
            return (T)CreateObject(id);
        }


        internal NetworkObject CreateObject(uint typeId)
        {
            var objInfo = NetworkObjectInfo.Get(typeId);

            NetworkObject instance = objInfo.create(typeId);
            if (instance == null)
                throw new Exception("create object, instance null");
            instance.typeId = typeId;
            instance.networkManager = NetworkManager;
            return instance;
        }



        public NetworkObject GetObject(ulong instanceId)
        {
            NetworkObject obj;
            objects.TryGetValue(instanceId, out obj);
            return obj;
        }




        #endregion



        public void RemoveObject(ulong instanceId)
        {
            destoryObjIds.Add(instanceId);
        }


        public void UpdateObjects()
        {
            if (objects != null)
            {
                foreach (var netObj in Objects)
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
                        if (!netObj.IsOwnedByServer)
                        {
                            var owner = netObj.ConnectionToOwner;
                            if (owner != null && owner.Socket != null && !(owner.IsConnecting || owner.IsConnected))
                            {
                                NetworkManager.Log("Owner not connected , socket: " + (owner.Socket != null));
                                destoryObjIds.Add(netObj.InstanceId);
                            }
                        }
                    }
                }

                foreach (var id in destoryObjIds)
                {
                    NetworkObject netObj;
                    if (objects.TryGetValue(id, out netObj))
                    {
                        if (netObj != null)
                            netObj.Despawn();
                    }
                }
                destoryObjIds.Clear();
            }
        }

        protected void CheckThreadSafe()
        {
            return;
            //if (mainThreadId == 0)
            //    return;
            //if (!IsThreadSafe)
            //{               
            //    throw new Exception("Not main thread " + mainThreadId + "," + Thread.CurrentThread.ManagedThreadId);
            //}
        }

        public void OnClientConnected(NetworkClient client)
        {
            ClientConnected?.Invoke(this, client);
        }

        public virtual void Dispose()
        {
            Stop();
        }

        ~NetworkServer()
        {
            Dispose();
        }

    }
}

