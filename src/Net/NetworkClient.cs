using Net.Messages;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Linq;
using System.Reflection;

namespace Net
{
    public class NetworkClient : CoroutineBase, IDisposable
    {
        private NetworkConnection conn;
        private bool isRunning;

        private bool isClient;
        private NetworkServer server;
        private Status status;
        private string address;
        private int port;
        private bool isReady;

        public NetworkClient()
            : this(null, null, false)
        {

        }

        public NetworkClient(NetworkServer server, Socket socket, bool isListen)
        {
            this.server = server;
            isClient = !isListen;

            conn = new NetworkConnection(server, socket, isListen);

            if (IsClient)
            {

                ConnectionToServer = conn;
            }
            else
            {
                ConnectionToClient = conn;
            }
            conn.Connected += Conn_Connected;
            conn.Disconnected += Conn_Disconnected;
        }

        public NetworkConnection Connection
        {
            get { return conn; }
        }
        public NetworkConnection ConnectionToServer;
        public NetworkConnection ConnectionToClient;

        public bool IsRunning
        {
            get { return isRunning; }
        }

        public bool IsClient
        {
            get { return isClient; }
        }



        protected NetworkServer Server { get => server; }

        public IEnumerable<NetworkObject> Objects
        {
            get { return conn.Objects; }
        }

        public int Port { get => port; }
        public string Address { get => address; }

        //public event Action<NetworkClient> Started;
        //public event Action<NetworkClient> Stoped;
        public event Action<NetworkClient> OnReady;
        //public event Action<NetworkClient> Connected;
        //public event Action<NetworkClient> Disconnected;

        static NetworkClient()
        {
            Action<Assembly> assemblyLoad = (ass) =>
            {
                foreach (var type in ass.GetTypes())
                {
                    if (!type.IsSubclassOf(typeof(NetworkObject)))
                        continue;
                    var attr = type.GetCustomAttributes(typeof(NetworkObjectIdAttribute), false).FirstOrDefault() as NetworkObjectIdAttribute;
                    if (attr == null)
                        continue;
                    var id = NetworkObjectId.GetObjectId(type);
                    if (!NetworkObjectInfo.Has(id))
                        RegisterObject(id, type, _CreateObject, null);
                }
            };

            AppDomain.CurrentDomain.AssemblyLoad += (sender, args) =>
            {
                assemblyLoad(args.LoadedAssembly);
            }; ;

            foreach (var ass in AppDomain.CurrentDomain.GetAssemblies())
            {
                assemblyLoad(ass);
            }
        }



        static NetworkObject _CreateObject(NetworkObjectId objectId)
        {
            NetworkObjectInfo objInfo = NetworkObjectInfo.Get(objectId);
            return Activator.CreateInstance(objInfo.type) as NetworkObject;
        }


        public virtual void Connect(string address, int port, MessageBase extra = null)
        {
            if (isRunning)
                return;
            isRunning = true;
            conn.Connect(address, port, extra);
            StartCoroutine(Running());
        }

        public void Disconnect()
        {
            isRunning = false;
            isReady = false;

            if (conn.IsConnected)
            {
                try
                {
                    conn.SendMessage((short)NetworkMsgId.Disconnect);
                    conn.Flush(1000);
                }
                catch { }
                conn.Disconnect();
            }

        }

        protected void Ready()
        {
            if (!isReady)
            {
                isReady = true;
                OnReady?.Invoke(this);
            }
        }

        internal IEnumerator Running()
        {

            isRunning = true;

            using (Connection)
            {

                while (isRunning)
                {
                    try
                    {
                        if (!(conn.IsConnecting || conn.IsConnected))
                            break;
                        //conn.ProcessMessage();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                    }
                    yield return null;
                }
            }

            isRunning = false;
            isReady = false;

            //Stoped?.Invoke(this);
        }

        public void Send(short msgId, MessageBase msg = null)
        {
            Connection.SendMessage(msgId, msg);
        }
        protected virtual void OnServerConnect(NetworkMessage netMsg)
        {
            Ready();
        }

        protected virtual void OnClientConnect(NetworkMessage netMsg)
        {
            Ready();
        }

        private void Conn_Connected(NetworkConnection obj, NetworkMessage netMsg)
        {
            if (isRunning)
            {

                try
                {
                    if (isClient)
                    {
                        OnClientConnect(netMsg);
                    }
                    else
                    {
                        OnServerConnect(netMsg);
                    }
                }
                catch (Exception ex)
                {
                    isRunning = false;
                }
            }
        }

        private void Conn_Disconnected(NetworkConnection obj)
        {
            if (!(status == Status.Stoped || status == Status.ConnectError))
            {
                status = Status.Connecting;
            }

            Disconnect();
            //Disconnected?.Invoke(this);
        }




        enum Status
        {
            Connecting,
            ConnectError,
            Connected,
            Stoped,
        }



        //public  void SendMessage(short msgId, MessageBase msg = null)
        //{
        //    CheckThreadSafe();
        //    if (!isRunning)
        //        return;

        //    client.conn.SendMessage(msgId, msg);
        //}



        #region Create Object


        public static void RegisterObject<T>(CreateObjectDelegate create, DestroyObjectDelegate destrory = null)
        {
            RegisterObject(typeof(T), create, destrory);
        }

        public static void RegisterObject(Type type, CreateObjectDelegate create, DestroyObjectDelegate destrory = null)
        {
            NetworkObjectId objectId = NetworkObjectId.GetObjectId(type);

            RegisterObject(objectId, type, create, destrory);
        }

        private static void RegisterObject(NetworkObjectId objectId, Type type, CreateObjectDelegate create, DestroyObjectDelegate destrory = null)
        {
            if (objectId.Value == Guid.Empty)
                throw new ArgumentException("value is empty", nameof(objectId));

            if (create == null)
                throw new ArgumentNullException(nameof(create));

            if (type != null)
            {
                SyncVarInfo.GetSyncVarInfos(type);
                SyncListInfo.GetSyncListInfos(type);
            }

            NetworkObjectInfo info = new NetworkObjectInfo()
            {
                objectId = objectId,
                create = create,
                destroy = destrory,
                type = type,
            };
            NetworkObjectInfo.Add(info);
        }

        public static void UnregisterObject(NetworkObjectId objectId)
        {
            NetworkObjectInfo.Remove(objectId);

        }
        public void CreateObject<T>(MessageBase parameter = null)
            where T : NetworkObject
        {
            var id = NetworkObjectId.GetObjectId(typeof(T));
            CreateObject(id, parameter);
        }
        public void CreateObject(NetworkObjectId objectId, MessageBase parameter = null)
        {
            var msg = new CreateObjectMessage()
            {
                toServer = true,
                objectId = objectId,
                parameter = parameter
            };

            conn.SendMessage((short)NetworkMsgId.CreateObject, msg);
        }



        public virtual void Dispose()
        {
            //Stop();
            Disconnect();
        }

        ~NetworkClient()
        {
            Dispose();
        }

        #endregion

    }

    public delegate NetworkObject CreateObjectDelegate(NetworkObjectId objectId);
    public delegate void DestroyObjectDelegate(NetworkObject instance);



    internal class NetworkObjectInfo
    {
        public NetworkObjectId objectId;
        public CreateObjectDelegate create;
        public DestroyObjectDelegate destroy;
        public Type type;
        static Dictionary<NetworkObjectId, NetworkObjectInfo> createInstanceInfos;

        public static void Add(NetworkObjectInfo objInfo)
        {
            if (createInstanceInfos == null)
                createInstanceInfos = new Dictionary<NetworkObjectId, NetworkObjectInfo>();

            createInstanceInfos[objInfo.objectId] = objInfo;
        }
        public static bool Has(NetworkObjectId objectId)
        {
            return (createInstanceInfos != null && createInstanceInfos.ContainsKey(objectId));
        }

        public static NetworkObjectInfo Get(NetworkObjectId objectId)
        {
            if (createInstanceInfos == null || !createInstanceInfos.ContainsKey(objectId))
                throw new Exception("not contains object id:" + objectId);
            var objInfo = createInstanceInfos[objectId];
            return objInfo;
        }

        public static void Remove(NetworkObjectId objectId)
        {
            if (createInstanceInfos != null)
            {
                createInstanceInfos.Remove(objectId);
            }
        }

    }



}
