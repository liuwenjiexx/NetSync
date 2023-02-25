#if UNITY_2021
using UnityEngine;
using YMFramework;
#endif

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


namespace Yanmonet.NetSync
{
    public class NetworkServer : IDisposable
    {
        private TcpListener server;
        private bool isRunning;
        private LinkedList<NetworkClient> hostList;
        private Dictionary<NetworkConnection, LinkedListNode<NetworkClient>> hostDic;
        private Dictionary<NetworkInstanceId, NetworkObject> objects;
        private uint nextInstanceId;
        private ulong nextConnectionId;
        private List<NetworkInstanceId> destoryObjIds = new List<NetworkInstanceId>();
        private Type clientType;
        Task acceptClientTask;
        private int mainThreadId;
        private object lockObj;
        CancellationTokenSource cancellationTokenSource;

        public NetworkServer(TcpListener server)
        {
            this.server = server;
            objects = new Dictionary<NetworkInstanceId, NetworkObject>();
            lockObj = new object();
        }
        public NetworkServer()
            : this(null)
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
                foreach (var item in hostList)
                    yield return item.Connection;
            }
        }
        public IReadOnlyCollection<NetworkClient> Clients
        {
            get
            {
                return hostList;
            }
        }
        public IEnumerable<NetworkObject> Objects { get => objects.Select(o => o.Value); }
        public Type ClientType { get => clientType; set => clientType = value; }

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
            hostList = new LinkedList<NetworkClient>();
            hostDic = new Dictionary<NetworkConnection, LinkedListNode<NetworkClient>>();

            isRunning = true;
            NetworkUtility.Log($"Game Server IP: {localAddress}, Port: {localPort}");
            NetworkUtility.Log("Network Server Started");
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
                DestroyObject(obj);
            }

            try
            {
                if (hostList != null)
                {
                    foreach (var host in hostList)
                    {
                        try { host.Connection.Dispose(); } catch { }
                    }
                    hostList.Clear();
                }
                if (hostDic != null)
                    hostDic.Clear();
            }
            catch { }

            try
            {
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
                NetworkUtility.Log(ex);
            }
            NetworkUtility.Log("Network Server Stoped");
        }



        public void Update()
        {
            if (server == null)
                throw new Exception("listener null");

            LinkedListNode<NetworkClient> node;
            NetworkClient client;

            node = hostList.First;
            while (node != null)
            {
                client = node.Value;
                if (client == null || !client.IsRunning || !client.Connection.Socket.Connected)
                {
                    node = node.RemoveAndNext();
                    NetworkUtility.Log("Client Disconnected");
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
                                node = hostList.AddLast(client);
                                hostDic[client.Connection] = node;
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
                NetworkUtility.Log(ex);
            }




            //#if UNITY_2021
            //                await new WaitForEndOfFrame();
            //#else
            //                await Task.Delay(30);
            //#endif


        }




        private void InitClient(NetworkClient client)
        {

            client.Connection.Connected += (conn, netMsg) =>
              {
                  //try
                  //{
                  OnClientConnect(client, netMsg);
                  //}
                  //catch (Exception ex)
                  //{
                  //    client.Stop();
                  //    Console.WriteLine(ex);
                  //}
                  //ClientConnected?.Invoke(this, client);

              };
        }



        protected virtual void OnClientConnect(NetworkClient client, NetworkMessage netMsg)
        {
            //var msg = netMsg.ReadMessage<ConnectMessage>();
            if (netMsg.Connection.ConnectionId == 0)
            {
                ulong connId = ++nextConnectionId;
                netMsg.Connection.ConnectionId = connId;
            }
            ClientConnected?.Invoke(this, client);
        }


        protected virtual NetworkClient AcceptTcpClient(TcpClient netClient, MessageBase extra)
        {
            NetworkClient client;

            Type clientType = ClientType;
            if (clientType == null)
            {
                clientType = typeof(NetworkClient);
            }
            client = (NetworkClient)Activator.CreateInstance(clientType, new object[] { this, netClient.Client, true, true });

            //  client.Start();
            //client.Running();

            NetworkUtility.Log("Accept Client");

            return client;
        }

        protected virtual void OnClientDisconnect(NetworkClient client)
        {
        }

        public NetworkClient GetClient(NetworkConnection connection)
        {
            LinkedListNode<NetworkClient> node;

            if (hostDic.TryGetValue(connection, out node))
            {
                NetworkClient client = node.Value;
                if (!client.IsRunning)
                {
                    RemoveConnection(connection);
                    return null;
                }
                return client;
            }
            return null;
        }

        public void RemoveConnection(NetworkConnection connection)
        {

            if (hostDic.ContainsKey(connection))
            {
                var node = hostDic[connection];
                node.List.Remove(node);
                hostDic.Remove(connection);
                try
                {
                    if (connection.IsConnected)
                        connection.Disconnect();
                    var client = node.Value;
                    if (client.IsRunning)
                    {
                        //client.Stop();
                    }

                    ClientDisconnected?.Invoke(this, client);
                }
                catch { }
            }
        }

        protected virtual void OnUpdate() { }

        public void SendToAll(short msgId, MessageBase msg = null)
        {
            foreach (var conn in Connections)
            {
                conn.SendMessage(msgId, msg);
            }
        }

        #region Create Object

        public T CreateObject<T>(NetworkMessage netMsg = null)
            where T : NetworkObject
        {
            var id = NetworkObjectId.GetObjectId(typeof(T));
            return (T)CreateObject(id, null);
        }

        public NetworkObject CreateObject(NetworkObjectId objectId)
        {
            return CreateObject(objectId, null);
        }

        internal NetworkObject CreateObject(NetworkObjectId objectId, NetworkMessage netMsg)
        {
            var objInfo = NetworkObjectInfo.Get(objectId);

            NetworkObject instance = objInfo.create(objectId);
            if (instance == null)
                throw new Exception("create object, instance null");
            instance.InstanceId = new NetworkInstanceId(++nextInstanceId);
            instance.objectId = objectId;
            instance.IsServer = true;
            objects[instance.InstanceId] = instance;

            OnCreateObject(instance, netMsg);

            return instance;
        }

        protected virtual void OnCreateObject(NetworkObject obj, NetworkMessage netMsg)
        {
        }


        public NetworkObject GetObject(NetworkInstanceId instanceId)
        {
            NetworkObject obj;
            objects.TryGetValue(instanceId, out obj);
            return obj;
        }

        public void DestroyObject(NetworkObject obj)
        {

            if (objects.ContainsKey(obj.InstanceId))
            {
                foreach (var conn in obj.Observers.ToArray())
                {
                    RemoveObserver(obj, conn);
                }

                objects.Remove(obj.InstanceId);

                obj.Destrory();
            }
        }


        #endregion



        public void AddObserver(NetworkObject obj, NetworkConnection conn)
        {
            if (obj == null) throw new ArgumentNullException(nameof(obj));
            if (conn == null) throw new ArgumentNullException(nameof(conn));

            if (!obj.IsServer)
                throw new Exception("not server object");

            if (!obj.observers.Contains(conn))
            {
                obj.observers.Add(conn);
                conn.AddObject(obj);

                conn.SendMessage((short)NetworkMsgId.CreateObject,
                    new CreateObjectMessage()
                    {
                        toServer = false,
                        objectId = obj.objectId,
                        instanceId = obj.InstanceId,
                    });

                obj.SyncAll(conn);
            }
        }

        public void RemoveObserver(NetworkObject obj, NetworkConnection conn)
        {
            if (!obj.IsServer)
                throw new Exception("not server object");

            if (obj.observers.Remove(conn))
            {
                conn.RemoveObject(obj);

                conn.SendMessage((short)NetworkMsgId.DestroyObject, new DestroyObjectMessage()
                {
                    instanceId = obj.InstanceId,
                });
            }
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
                            NetworkUtility.Log(ex);
                        }
                        var owner = netObj.ConnectionToOwner;
                        if (owner != null && !owner.IsConnected)
                        {
                            destoryObjIds.Add(netObj.InstanceId);
                        }
                    }
                }

                foreach (var id in destoryObjIds)
                {
                    NetworkObject netObj;
                    if (objects.TryGetValue(id, out netObj))
                    {
                        if (netObj != null)
                            DestroyObject(netObj);
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

