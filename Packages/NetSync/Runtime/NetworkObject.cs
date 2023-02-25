using Yanmonet.NetSync.Messages;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using static Yanmonet.NetSync.NetworkObject;

namespace Yanmonet.NetSync
{
    public abstract class NetworkObject
    {
        internal NetworkInstanceId instanceId;
        internal NetworkObjectId objectId;
        private bool isClient;
        private bool isServer;

        internal List<NetworkConnection> observers;
        private ReadOnlyCollection<NetworkConnection> readonlyObservers;

        private uint syncVarDirtyBits;
        internal SyncVarState[] syncVarStates;



        public NetworkObject()
        {
            observers = new List<NetworkConnection>();
            readonlyObservers = observers.AsReadOnly();
            RegisterSyncVar();
        }

        public NetworkInstanceId InstanceId { get => instanceId; internal set { instanceId = value; } }

        public ReadOnlyCollection<NetworkConnection> Observers { get => readonlyObservers; }

        public bool IsClient { get => isClient; internal set => isClient = value; }

        public bool IsServer { get => isServer; internal set => isServer = value; }

        public bool IsServerAndClient { get => isServer && isClient; }

        public uint SyncVarDirtyBits { get => syncVarDirtyBits; }

        public NetworkConnection ConnectionToServer
        {
            get => connectionToServer;
            set
            {
                if (connectionToServer != value)
                {
                    connectionToServer = value;
                }
            }
        }
        //public NetworkConnection ConnectionToClient { get; internal set; }

        /// <summary>
        /// server owner is null, client owner is connectionToServer
        /// </summary>
        public NetworkConnection ConnectionToOwner { get; set; }

        private NetworkConnection connectionToServer;

        internal void SetConnection(NetworkConnection conn)
        {

        }



        #region Sync Var 


        public bool SetSyncVar<T>(T value, ref T fieldValue, uint dirtyBit)
        {
            bool changed = false;
            if (!object.Equals(value, fieldValue))
            {
                fieldValue = value;
                changed = true;
            }

            if (changed)
                SetDirtyBit(dirtyBit);
            return changed;
        }

        public void SetDirtyBit(uint dirtyBit)
        {
            syncVarDirtyBits |= dirtyBit;
        }
        public void DirtyAllBits()
        {
            syncVarDirtyBits |= uint.MaxValue;
        }
        public void ClearAllDirtyBits()
        {
            syncVarDirtyBits = 0;
        }

        internal SyncVarInfo GetSyncInfoByBits(uint bits)
        {
            for (int i = 0, len = syncVarStates.Length; i < len; i++)
            {
                if ((syncVarStates[i].syncVarInfo.bits & bits) == bits)
                    return syncVarStates[i].syncVarInfo;
            }
            return null;
        }
        internal SyncVarState GetStateByBits(uint bits)
        {
            for (int i = 0, len = syncVarStates.Length; i < len; i++)
            {
                if ((syncVarStates[i].syncVarInfo.bits & bits) == bits)
                    return syncVarStates[i];
            }
            return null;
        }




        private void RegisterSyncVar()
        {
            Type type = GetType();
            var syncVarInfos = SyncVarInfo.GetSyncVarInfos(type);
            if (syncVarInfos != null && syncVarInfos.Length > 0)
            {

                syncVarStates = new SyncVarState[syncVarInfos.Length];
                for (int i = 0; i < syncVarStates.Length; i++)
                {
                    syncVarStates[i] = new SyncVarState()
                    {
                        syncVarInfo = syncVarInfos[i],
                    };
                }
            }

            if (syncVarStates == null)
                syncVarStates = new SyncVarState[0];

            var syncListInfos = SyncListInfo.GetSyncListInfos(type);
            if (syncListInfos != null && syncListInfos.Length > 0)
            {

                for (int i = 0; i < syncListInfos.Length; i++)
                {
                    var info = syncListInfos[i];
                    object syncList = info.field.GetValue(this);
                    if (syncList != null)
                    {
                        info.InitMethod.Invoke(syncList, new object[] { this, info });
                    }
                }

            }

        }

        public static readonly MethodInfo SyncListInitMethod = typeof(SyncList<>).GetMethod("init", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);



        public void ResetValues()
        {
            for (int i = 0; i < syncVarStates.Length; i++)
            {
                var state = syncVarStates[i];
                state.value = state.syncVarInfo.field.GetValue(this);
            }
        }

        internal void SyncAll(NetworkConnection conn)
        {

            if (syncVarStates != null)
            {
                conn.SendMessage((short)NetworkMsgId.SyncVar, SyncVarMessage.ResponseSyncVar(this, uint.MaxValue));
            }

            var syncListInfos = SyncListInfo.GetSyncListInfos(GetType());
            if (syncListInfos != null)
            {
                for (int i = 0, len = syncListInfos.Length; i < len; i++)
                {
                    var info = syncListInfos[i];
                    var list = info.field.GetValue(this);
                    int j = 0;
                    foreach (var item in (IEnumerable)list)
                    {
                        conn.SendMessage((short)NetworkMsgId.SyncList, SyncListMessage.Add(this, info.memberIndex, (byte)j));
                        j++;
                    }
                }
            }
        }

        internal void InternalUpdate()
        {
            UpdateSyncVar();

            Update();
        }

        protected virtual void Update()
        {

        }

        void UpdateSyncVar()
        {
            if (isServer)
            {
                if (syncVarDirtyBits != 0)
                {
                    SyncVarMessage msg = SyncVarMessage.ResponseSyncVar(this, syncVarDirtyBits);
                    ClearAllDirtyBits();

                    foreach (var conn in observers)
                        conn.SendMessage((short)NetworkMsgId.SyncVar, msg);
                }
            }
            else if (isClient)
            {
                if (syncVarDirtyBits != 0)
                {
                    if (connectionToServer != null)
                    {
                        SyncVarMessage msg = SyncVarMessage.RequestSyncVar(this, syncVarDirtyBits);
                        ClearAllDirtyBits();
                        connectionToServer.SendMessage((short)NetworkMsgId.SyncVar, msg);
                    }
                }
            }
        }


        internal class SyncVarState
        {
            public SyncVarInfo syncVarInfo;
            public object value;
        }

        #endregion


        #region Rpc


        protected void Rpc(string methodName, params object[] args)
        {
            Rpc(null, methodName, args);
        }

        protected void Rpc(NetworkConnection conn, string methodName, params object[] args)
        {

            RpcInfo rpcInfo = RpcInfo.GetRpcInfo(GetType(), methodName);

            if (isClient)
            {
                var msg = RpcMessage.RpcServer(this, rpcInfo, args);
                connectionToServer.SendMessage((short)NetworkMsgId.Rpc, msg);
            }
            else if (isServer)
            {
                var msg = RpcMessage.RpcClient(this, rpcInfo, args);
                if (conn != null)
                {
                    conn.SendMessage((short)NetworkMsgId.Rpc, msg);
                }
                else
                {
                    foreach (var _conn in observers)
                    {
                        _conn.SendMessage((short)NetworkMsgId.Rpc, msg);
                    }
                }
            }
        }


        #endregion

        internal void Destrory()
        {
            Exception destroryEx = null;
            try
            {
                OnDestrory();
            }
            catch (Exception ex) { destroryEx = ex; }

            var info = NetworkObjectInfo.Get(objectId);

            if (info.destroy == null)
            {
                if (this is IDisposable)
                {
                    ((IDisposable)this).Dispose();
                }
            }
            else
            {
                info.destroy(this);
            }

            if (destroryEx != null)
                throw destroryEx;
        }

        protected virtual void OnDestrory()
        {
        }

        public override bool Equals(object obj)
        {
            NetworkObject netObj = obj as NetworkObject;
            if (obj == null)
                return false;
            return object.Equals(instanceId, netObj.instanceId);
        }


        public override int GetHashCode()
        {
            return instanceId.GetHashCode();
        }



    }
}
