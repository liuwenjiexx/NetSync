using Yanmonet.NetSync.Messages;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;



namespace Yanmonet.NetSync
{
    public class NetworkClient : IDisposable
    {
        private NetworkConnection conn;
        internal bool isRunning;

        private bool isClient;
        private Status status;
        private string address;
        private int port;
        private bool isReady;
        private NetworkServer server;

        public NetworkClient(NetworkManager manager)
            : this(manager, null, null, false, false)
        {
        }

        internal NetworkClient(NetworkManager manager, NetworkServer server, Socket socket, bool ownerSocket, bool isListen)
        {
            this.server = server;
            isClient = !isListen;
            networkManager = manager;
            conn = new NetworkConnection(networkManager, server, socket, ownerSocket, isListen);

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

            if (server != null && socket != null)
            {
                isRunning = true;
            }

        }

        public ulong ClientId
        {
            get
            {
                if (conn != null)
                    return conn.ConnectionId;
                return 0;
            }
        }

        public ulong ServerClientId
        {
            get; internal set;
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

        private NetworkManager networkManager;
        public NetworkManager NetworkManager => networkManager ?? NetworkManager.Singleton;

        static NetworkClient()
        {
            Action<Assembly> assemblyLoad = (ass) =>
            {
                //foreach (var type in ass.GetTypes())
                //{
                //    if (!type.IsSubclassOf(typeof(NetworkObject)))
                //        continue;
                //    var attr = type.GetCustomAttributes(typeof(NetworkObjectIdAttribute), false).FirstOrDefault() as NetworkObjectIdAttribute;
                //    if (attr == null)
                //        continue;
                //    var id = NetworkObjectId.GetTypeId(type);
                //    if (!NetworkObjectInfo.Has(id))
                //        RegisterObject(id, type, _CreateObject, null);
                //}
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



        static NetworkObject _CreateObject(ulong objectId)
        {
            NetworkObjectInfo objInfo = NetworkObjectInfo.Get(objectId);
            return Activator.CreateInstance(objInfo.type) as NetworkObject;
        }

        public void Connect(string address, int port)
        {
            Connect(address, port, 0, null);
        }


        public virtual void Connect(string address, int port, int version, byte[] data)
        {
            if (isRunning)
                return;
            conn.Connect(address, port, version, data);
            isRunning = true;
        }

        public void Disconnect()
        {
            isRunning = false;
            isReady = false;

            if (conn.IsConnected)
            {
                try
                {
                    conn.SendMessage((ushort)NetworkMsgId.Disconnect);
                    conn.Flush(1000);
                }
                catch { }
                conn.Disconnect();
            }

            //Stoped?.Invoke(this);
        }

        protected void Ready()
        {
            if (!isReady)
            {
                isReady = true;
                OnReady?.Invoke(this);
            }
        }

        public void Update()
        {

            if (!(conn.IsConnecting || conn.IsConnected))
            {
                //NetworkManager.Log($"Connection Update: {conn.ConnectionId}, IsConnecting: {conn.IsConnecting}, IsConnected: {conn.IsConnected}");
                return;
            }


            //using (Connection)
            {

                //  while (isRunning)
                {
                    try
                    {
                        conn.Update();
                        //conn.ProcessMessage();
                    }
                    catch (Exception ex)
                    {
                        //NetworkManager.LogError($"Connection Error: {conn.Address}:{conn.Port}\n{ex.Message}\n{ex.StackTrace}");
                    }

                }
            }



        }

        public void Send(ushort msgId, MessageBase msg = null)
        {
            Connection.SendMessage(msgId, msg);
        }
        protected virtual void OnServerConnect(byte[] data)
        {
            Ready();
        }

        protected virtual void OnClientConnect(byte[] data)
        {
            Ready();
        }

        private void Conn_Connected(NetworkConnection obj, byte[] data)
        {
            if (isRunning)
            {

                try
                {
                    if (isClient)
                    {
                        OnClientConnect(data);
                    }
                    else
                    {
                        OnServerConnect(data);
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

        public static void Initalize()
        {
            NetworkObjectInfo.createInstanceInfos.Clear();
        }

        //public void CreateObject<T>(MessageBase parameter = null)
        //   where T : NetworkObject
        //{
        //    var id = NetworkManager.GetObjectId(typeof(T));
        //    CreateObject(id, parameter);
        //}
        //private void CreateObject(ulong objectId, MessageBase parameter = null)
        //{
        //    var msg = new CreateObjectMessage()
        //    {
        //        toServer = true,
        //        objectId = objectId,
        //        parameter = parameter
        //    };

        //    conn.SendMessage((short)NetworkMsgId.CreateObject, msg);
        //}



        public virtual void Dispose()
        {
            //Stop();
            Disconnect();
        }

        ~NetworkClient()
        {
            Dispose();
        }


    }

    public delegate NetworkObject CreateObjectDelegate(ulong typeId);
    public delegate void DestroyObjectDelegate(NetworkObject instance);



    internal class NetworkObjectInfo
    {
        public ulong typeId;
        public CreateObjectDelegate create;
        public DestroyObjectDelegate destroy;
        public Type type;
        internal static Dictionary<ulong, NetworkObjectInfo> createInstanceInfos;

        public static void Add(NetworkObjectInfo objInfo)
        {
            if (createInstanceInfos == null)
                createInstanceInfos = new Dictionary<ulong, NetworkObjectInfo>();

            createInstanceInfos[objInfo.typeId] = objInfo;
        }
        public static bool Has(ulong typeId)
        {
            return (createInstanceInfos != null && createInstanceInfos.ContainsKey(typeId));
        }

        public static NetworkObjectInfo Get(ulong typeId)
        {
            if (createInstanceInfos == null || !createInstanceInfos.ContainsKey(typeId))
                throw new Exception("not contains Type id:" + typeId);
            var objInfo = createInstanceInfos[typeId];
            return objInfo;
        }

        public static void Remove(ulong typeId)
        {
            if (createInstanceInfos != null)
            {
                createInstanceInfos.Remove(typeId);
            }
        }

    }



}
