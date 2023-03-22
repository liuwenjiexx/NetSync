using Yanmonet.NetSync.Messages;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using static Yanmonet.NetSync.NetworkObject;
using System.Linq;
using System.Data;
#if UNITY_ENGINE
using UnityEngine;
#endif

namespace Yanmonet.NetSync
{
    public abstract class NetworkObject
    {
        internal uint typeId;
        internal ulong objectId;

        private bool isDirty;

        internal List<ulong> observers;


        private uint syncVarDirtyBits;
        internal SyncVarState[] syncVarStates;
        internal Dictionary<uint, SyncBase> variables;

        public NetworkObject()
        {
            observers = new List<ulong>();
            InitalizeVariable();
        }

        public ulong InstanceId { get => objectId; internal set { objectId = value; } }

        public IReadOnlyList<ulong> Observers { get => observers; }


        public uint SyncVarDirtyBits { get => syncVarDirtyBits; }

        public NetworkConnection Connection
        {
            get => remote;
            set
            {
                if (remote != value)
                {
                    remote = value;
                }
            }
        }

        //public NetworkConnection ConnectionToClient { get; internal set; }

        /// <summary>
        /// server owner is null, client owner is connectionToServer
        /// </summary>
        public NetworkConnection ConnectionToOwner { get; set; }

        private NetworkConnection remote;

        internal NetworkManager networkManager;
        public NetworkManager NetworkManager => networkManager ?? NetworkManager.Singleton;
        public bool IsSpawned { get; internal set; }
        public ulong OwnerClientId { get; internal set; }

        //  public bool IsLocalPlayer => NetworkManager != null && IsPlayerObject && OwnerClientId == NetworkManager.LocalClientId;

        public bool IsOwner => NetworkManager != null && OwnerClientId == NetworkManager.LocalClientId;

        public bool IsOwnedByServer => NetworkManager != null && OwnerClientId == NetworkManager.ServerClientId;

        public bool IsServer => NetworkManager.IsServer;
        public bool IsClient => NetworkManager.IsClient;


        public void Spawn()
        {
            SpawnWithOwnership(NetworkManager.ServerClientId);
        }

        public void SpawnWithOwnership(ulong ownerClientId)
        {
            if (!NetworkManager.IsServer) throw new NotServerException("Spawn only on server");
            if (IsSpawned) throw new Exception($"{GetType().Name} is Spawned");
            IsSpawned = true;

            typeId = NetworkManager.GetTypeId(GetType());
            InstanceId = ++NetworkManager.Server.nextObjectId;

            NetworkManager.Server.objects[InstanceId] = this;
            OwnerClientId = ownerClientId;

            //if (!IsOwnedByServer)
            //{
            //    NetworkManager.Server.AddObserver();
            //}

            foreach (var variable in variables.Values)
            {
                variable.ResetDirty();
                variable.networkObject = this;
            }
            isDirty = false;

            if (!IsOwnedByServer)
            {
                if (NetworkManager.clients.TryGetValue(ownerClientId, out var client))
                {
                    var conn = client.Connection;
                    ConnectionToOwner = conn;
                }
                AddObserver(ownerClientId);
            }


            OnSpawned();

            if (IsClient)
            {
                NetworkManager.LocalClient.Connection.OnObjectAdded(this);
            }
        }

        internal protected virtual void OnSpawned()
        {
        }
        internal protected virtual void OnDespawned()
        {
        }



        private void SendSpawnMsg(ulong clientId)
        {
            if (clientId == NetworkManager.ServerClientId)
                return;

            NetworkConnection conn = null;
            if (NetworkManager.clients.TryGetValue(clientId, out var client))
            {
                conn = client.Connection;
            }
            if (conn == null)
                return;

            //NetworkManager.Log($"Msg Spawn Object Type: {GetType()}, Client: {conn.ConnectionId}");

            conn.SendMessage((ushort)NetworkMsgId.CreateObject, new CreateObjectMessage()
            {
                typeId = typeId,
                objectId = InstanceId,
                ownerClientId = OwnerClientId,
            });

            SyncVariable(conn);

            conn.SendMessage((ushort)NetworkMsgId.Spawn, new SpawnMessage()
            {
                instanceId = InstanceId,
                ownerClientId = OwnerClientId,
            });
        }


        public void Despawn(bool destrory = true)
        {
            if (!IsServer) throw new NotServerException($"{nameof(Despawn)} only on server");
            if (!IsSpawned) throw new Exception("AddObserver require Spawned");

            if (NetworkManager.Server.objects.ContainsKey(InstanceId))
            {
                foreach (var clientId in Observers.ToArray())
                {
                    RemoveObserver(clientId, destrory);
                }

                //NetworkManager.Server.RemoveObject(InstanceId);
                NetworkManager.Server.objects.Remove(InstanceId);
            }

            InstanceId = default;
            IsSpawned = false;
            
            foreach (var variable in variables.Values)
            {
                //variable.networkObject = null;
            }

            OnDespawned();

            if (destrory)
            {
                Destrory();
            }
        }

        public void AddObserver(ulong clientId)
        {
            if (!NetworkManager.IsServer) throw new NotServerException($"{nameof(AddObserver)} only on server");
            if (!IsSpawned) throw new Exception("AddObserver require Spawned");

            if (clientId == NetworkManager.ServerClientId)
                return;

            if (observers.Contains(clientId))
                return;
            NetworkConnection conn;

            if (!NetworkManager.clients.TryGetValue(clientId, out var client))
                return;
            conn = client.Connection;

            SyncState();

            observers.Add(clientId);
            conn.AddObject(this);
            SendSpawnMsg(clientId);

        }

        public void RemoveObserver(ulong clientId)
        {
            RemoveObserver(clientId, true);
        }
        private void RemoveObserver(ulong clientId, bool isDestroy)
        {
            if (!IsServer) throw new NotServerException($"{nameof(RemoveObserver)} only on server");
            if (!IsSpawned) throw new Exception("RemoveObserver require Spawned");
            if (!observers.Contains(clientId))
                return;
            NetworkConnection conn;
            if (!NetworkManager.clients.TryGetValue(clientId, out var client))
                return;

            SyncState();

            conn = client.Connection;

            conn.RemoveObject(this);
            //NetworkManager.Log("Remvoe Observer: " + this + ", client: " + clientId);
            conn.SendMessage((ushort)NetworkMsgId.Despawn, new DespawnMessage()
            {
                instanceId = InstanceId,
                isDestroy = isDestroy
            });
        }

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
            if (syncVarDirtyBits != 0)
            {
                NetworkManager.Log($"ClearAllDirtyBits {GetType().Name}, instance: {InstanceId}, isOwner: {IsOwner}, OwnerClientId: {OwnerClientId}, ClientId: {NetworkManager.LocalClientId}, diryBits: {syncVarDirtyBits}");
                syncVarDirtyBits = 0;
            }
        }

        internal SyncVarInfo GetSyncInfoByBits(uint bits)
        {
            for (int i = 0, len = syncVarStates.Length; i < len; i++)
            {
                if (syncVarStates[i] == null) continue;
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

        internal SyncVarState GetStateByIndex(byte index)
        {
            for (int i = 0, len = syncVarStates.Length; i < len; i++)
            {
                if (syncVarStates[i] == null) continue;
                if (syncVarStates[i].syncVarInfo.index == index)
                    return syncVarStates[i];
            }
            return null;
        }


        private void InitalizeVariable()
        {
            if (variables == null)
                variables = new();
            variables.Clear();
            Type type = GetType();
            var syncVarInfos = SyncVarInfo.GetSyncVarInfos(type);
            if (syncVarInfos != null && syncVarInfos.Length > 0)
            {
                syncVarStates = new SyncVarState[syncVarInfos.Length];
                for (int i = 0; i < syncVarStates.Length; i++)
                {
                    var varInfo = syncVarInfos[i];
                    var field = varInfo.field;
                    if (varInfo.isVariable)
                    {
                        var variable = field.GetValue(this) as SyncBase;
                        if (variable == null)
                        {
                            try
                            {
                                variable = Activator.CreateInstance(field.FieldType) as SyncBase;
                            }
                            catch
                            {
                                NetworkManager.Log($"Create Instance error, field: {field.DeclaringType.Name}.{field.Name}, type: {field.FieldType}");
                                throw;
                            }
                            field.SetValue(this, variable);
                        }
                        variable.Name = varInfo.field.Name;
                        variable.networkObject= this;
                        variables[varInfo.hash] = variable;
                    }
                    else
                    {
                        syncVarStates[i] = new SyncVarState()
                        {
                            syncVarInfo = syncVarInfos[i],
                        };
                    }
                }
            }

            if (syncVarStates == null)
                syncVarStates = new SyncVarState[0];


        }



        public void ResetValues()
        {
            for (int i = 0; i < syncVarStates.Length; i++)
            {
                var state = syncVarStates[i];
                state.value = state.syncVarInfo.field.GetValue(this);
            }
        }

        internal void SyncVariable(NetworkConnection conn)
        {

            if (syncVarStates != null)
            {
                // conn.SendMessage((ushort)NetworkMsgId.SyncVar, SyncVarMessage.ResponseSyncVar(this, uint.MaxValue));
            }

            conn.SendMessage((ushort)NetworkMsgId.SyncVar, new SyncVarMessage(this, false, true));
        }

        internal void SyncVariableDelta()
        {

            byte[] packet = null;
            SyncVarMessage msg = null;
            foreach (var pair in variables)
            {
                var variable = pair.Value;
                if (variable.CanClientWrite(NetworkManager.LocalClientId))
                {
                    if (variable.IsDirty())
                    {
                        if (packet == null)
                        {
                            msg = new SyncVarMessage(this, true, false);
                            packet = NetworkUtility.PackMessage((ushort)NetworkMsgId.SyncVar, msg);
                        }
                        variable.ResetDirty();
                    }
                }
            }

            if (packet != null)
            {
                if (IsServer)
                {
                    foreach (var conn in NetworkManager.GetAvaliableConnections(observers))
                    {
                        conn.SendPacket((ushort)NetworkMsgId.SyncVar, packet);
                    }
                }
                else
                {
                    NetworkManager.LocalClient.Connection.SendPacket((ushort)NetworkMsgId.SyncVar, packet);
                }
            }


        }

        internal void InternalUpdate()
        {

            SyncState();

            Update();
        }


        public bool IsDirty()
        {
            return isDirty;
        }

        internal virtual void SetDirty()
        {
            this.isDirty = true;
        }



        protected virtual void Update()
        {

        }

        protected float time = 0;


        //更新对象状态
        public void SyncState()
        {
            if (IsSpawned)
            {
                if (isDirty)
                {
                    SyncVariableDelta();
                    isDirty = false;
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


        private ServerRpcInfo serverRpc;

        protected void BeginServerRpc(string methodName, params object[] args)
        {
            __BeginServerRpc__(methodName, default, args);
        }


        public void __BeginServerRpc__(string methodName, ServerRpcParams rpcParams, params object[] args)
        {
            serverRpc = new ServerRpcInfo();
            serverRpc.serverParams = rpcParams;
            serverRpc.rpcInfo = RpcInfo.GetRpcInfo(GetType(), methodName);
            serverRpc.args = args;
        }

        protected void __EndServerRpc__()
        {
            if (!IsServer)
            {
                var msg = RpcMessage.RpcServer(this, serverRpc.rpcInfo, serverRpc.args);
                remote.SendMessage((ushort)NetworkMsgId.Rpc, msg);
            }
        }
        protected bool __ReturnServerRpc__()
        {
            return !IsServer;
        }

        private ClientRpcInfo clientRpc;
        struct ServerRpcInfo
        {
            public ServerRpcParams serverParams;
            public RpcInfo rpcInfo;
            public object[] args;
        }
        struct ClientRpcInfo
        {
            public RpcInfo rpcInfo;
            public object[] args;
            public ClientRpcParams clientParams;
        }

        protected void BeginClientRpc(string methodName, params object[] args)
        {
            __BeginClientRpc__(methodName, default, args);
        }

        protected void __BeginClientRpc__(string methodName, ClientRpcParams rpcParams, params object[] args)
        {
            clientRpc = new ClientRpcInfo();
            clientRpc.clientParams = rpcParams;
            clientRpc.rpcInfo = RpcInfo.GetRpcInfo(GetType(), methodName);
            clientRpc.args = args;
        }

        protected void __EndClientRpc__()
        {
            if (IsServer)
            {
                var rpcInfo = clientRpc.rpcInfo;
                RpcMessage msg = RpcMessage.RpcClient(this, rpcInfo, clientRpc.args);

                SyncState();

                foreach (var _conn in NetworkManager.GetAvaliableConnections(observers))
                {
                    if (_conn.ConnectionId == NetworkManager.ServerClientId)
                    {
                        continue;
                    }

                    if (clientRpc.clientParams.clients != null && !clientRpc.clientParams.clients.Contains(_conn.ConnectionId))
                        continue;

                    //NetworkManager.Log($"Rpc {GetType().Name}:{rpcInfo.method.Name} Client [{_conn.ConnectionId}]");
                    _conn.SendMessage((ushort)NetworkMsgId.Rpc, msg);
                }

            }
        }

        protected bool __ReturnClientRpc__()
        {
            return !IsClient;
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

            var info = NetworkObjectInfo.Get(typeId);

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
            return object.Equals(objectId, netObj.objectId);
        }


        public override int GetHashCode()
        {
            return objectId.GetHashCode();
        }

        public override string ToString()
        {
            return $"{GetType().Name}({InstanceId})";
        }

    }
}
