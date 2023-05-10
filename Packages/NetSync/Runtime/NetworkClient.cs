using System;
using System.Collections.Generic;
using System.Reflection;
using Yanmonet.Network.Sync.Messages;
using Yanmonet.Network.Transport;

namespace Yanmonet.Network.Sync
{
    internal class NetworkClient : IDisposable
    {

        public ulong clientId;
        public ulong transportClientId;
        internal bool isRunning;

        private bool isClient;
        private Status status;
        public bool isConnected;

        private float lastSendTime;
        private float lastReceiveTime;
        private INetworkTransport transport;

        private bool isReady;

        public NetworkClient(NetworkManager manager)
        {
            networkManager = manager;

            //if (socket == null) throw new ArgumentNullException("socket");

        }

        public ulong ClientId
        {
            get => clientId;
            internal set => clientId = value;
        }


        internal int pingDelay;

        public bool IsRunning
        {
            get { return isRunning; }
        }

        public bool IsClient
        {
            get { return isClient; }
        }



        public float LastSendTime { get => lastSendTime; }
        public float LastReceiveTime { get => lastReceiveTime; internal set => lastReceiveTime = value; }

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





        public void Ping()
        {
            long timestamp = NetworkManager.Timestamp;

            SendMessage((ushort)NetworkMsgId.Ping, PingMessage.Ping(timestamp));
        }


        internal void SendMessage(ushort msgId, MessageBase msg = null)
        {
            networkManager.SendMessage(clientId, msgId, msg);
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


        }

        private List<ulong> destoryObjIds = new List<ulong>();



        protected virtual void OnServerConnect(byte[] data)
        {
            Ready();
        }

        protected virtual void OnClientConnect(byte[] data)
        {
            Ready();
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

        public override bool Equals(object obj)
        {
            var client = obj as NetworkClient;
            if (client != null)
                return clientId != 0 && object.Equals(clientId, client.clientId);
            return false;
        }
        public override int GetHashCode()
        {
            return clientId.GetHashCode();
        }

        public override string ToString()
        {
            return $"Connection: {clientId}";
        }


        public virtual void Dispose()
        {
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
