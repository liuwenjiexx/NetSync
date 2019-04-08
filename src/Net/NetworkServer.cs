using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;

namespace Net
{
    public class NetworkServer : CoroutineBase, IDisposable
    {
        private TcpListener server;
        private bool isRunning;
        private LinkedList<NetworkClient> hostList;
        private Dictionary<NetworkConnection, LinkedListNode<NetworkClient>> hostDic;
        private Dictionary<NetworkInstanceId, NetworkObject> objects;
        private uint nextInstanceId;

        public NetworkServer(TcpListener server)
        {
            this.server = server;
            objects = new Dictionary<NetworkInstanceId, NetworkObject>();
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
        public IEnumerable<NetworkClient> Clients
        {
            get
            {
                foreach (var item in hostList)
                    yield return item;
            }
        }
        public IEnumerable<NetworkObject> Objects { get => objects.Select(o => o.Value); }

        public event Action<NetworkServer> Started;
        public event Action<NetworkServer> Stoped;

        public event Action<NetworkServer, NetworkClient> Connected;
        public event Action<NetworkServer, NetworkClient> Disconnected;


        public virtual void Start()
        {
            CheckThreadSafe();
            if (isRunning)
                return;

            StartCoroutine(Running());
        }

        public virtual void Stop()
        {
            CheckThreadSafe();
            if (!isRunning)
                return;

            isRunning = false;
        }

        private IEnumerator Running()
        {
            if (server == null)
                throw new Exception("listener null");

            hostList = new LinkedList<NetworkClient>();
            hostDic = new Dictionary<NetworkConnection, LinkedListNode<NetworkClient>>();
            LinkedListNode<NetworkClient> node;
            NetworkClient client;
            isRunning = true;
            try
            {
                if (Started != null)
                    Started(this);
            }
            catch (Exception ex) { Console.WriteLine(ex); }
            while (isRunning)
            {
                node = hostList.First;
                while (node != null)
                {
                    client = node.Value;
                    if (client == null || !client.IsRunning || !client.Connection.Socket.Connected)
                    {
                        node = node.RemoveAndNext();
                        OnClientDisconnect(client);
                        Disconnected?.Invoke(this, client);
                        continue;
                    }
                    node = node.Next;
                }

                try
                {
                    while (server.Pending())
                    {
                        TcpClient tcpClient = server.AcceptTcpClient();
                        if (tcpClient != null)
                        {
                            try
                            {
                                client = AcceptClient(tcpClient);

                                if (client != null)
                                {
                                    if (!client.IsRunning)
                                        client = null;
                                }
                            }
                            catch (Exception ex)
                            {
                                try { tcpClient.Close(); } catch { }
                                Console.WriteLine(ex);
                                continue;
                            }
                            if (client != null)
                            {
                                RemoveConnection(client.Connection);
                                node = hostList.AddLast(client);
                                hostDic[client.Connection] = node;
                                client.Connected += Client_Connected;

                            }
                            else
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

                    UpdateSyncVar();
                    OnUpdate();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }




                yield return null;
            }

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
            catch (Exception ex) { Console.WriteLine(ex); }
        }

        private void Client_Connected(NetworkClient client)
        {
            Connected?.Invoke(this, client);
        }

        protected virtual NetworkClient AcceptClient(TcpClient netClient)
        {
            var client = new NetworkClient(this, netClient.Client, true);
            client.Start();
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
                        client.Stop();
                    }

                    Disconnected?.Invoke(this, client);
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

        #region Create Instance

        public T CreateObject<T>()
            where T : NetworkObject
        {
            var id = NetworkObjectId.GetObjectId(typeof(T));
            return (T)CreateObject(id);
        }

        public NetworkObject CreateObject(NetworkObjectId objectId)
        {
            if (NetworkClient.createInstanceInfos == null || !NetworkClient.createInstanceInfos.ContainsKey(objectId))
                throw new Exception("not contains object id:" + objectId);
            var objInfo = NetworkClient.createInstanceInfos[objectId];
            NetworkObject instance = objInfo.create(objectId);
            if (instance == null)
                throw new Exception("create instance null, object id:" + objectId);

            instance.InstanceId = new NetworkInstanceId(++nextInstanceId);
            instance.objectId = objectId;
            instance.IsServer = true;
            objects[instance.InstanceId] = instance;


            return instance;
        }

        public NetworkObject GetObject(NetworkInstanceId instanceId)
        {
            NetworkObject obj;
            objects.TryGetValue(instanceId, out obj);
            return obj;
        }

        public void DestroyObject(NetworkObject instance)
        {

            if (objects.ContainsKey(instance.InstanceId))
            {
                foreach (var conn in instance.Observers.ToArray())
                {
                    instance.RemoveObserver(conn);
                }

                objects.Remove(instance.InstanceId);

                instance.Destrory();
            }
        }


        #endregion


        public void UpdateSyncVar()
        {
            if (objects != null)
            {
                foreach (var netObj in Objects)
                {
                    if (netObj != null)
                    {
                        netObj.Update();

                    }
                }
            }
        }

        public virtual void Dispose()
        {
            Stop();
        }
    }
}
