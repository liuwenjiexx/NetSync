using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Yanmonet.Network.Sync.Messages;
#if UNITY_ENGINE
#endif

namespace Yanmonet.Network.Sync
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



        //public NetworkConnection ConnectionToClient { get; internal set; }

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
            if (!NetworkManager.IsServer) throw new NotServerException($"{nameof(Spawn)} only on server");
            SpawnWithOwnership(NetworkManager.ServerClientId);
        }

        public void SpawnWithOwnership(ulong ownerClientId)
        {
            if (!NetworkManager.IsServer) throw new NotServerException($"{nameof(SpawnWithOwnership)} only on server");
            if (IsSpawned) throw new Exception($"{GetType().Name} is Spawned");

            typeId = NetworkManager.GetTypeId(GetType());
            InstanceId = ++NetworkManager.NextObjectId;

            //if (!IsOwnedByServer)
            //{
            //    NetworkManager.Server.AddObserver();
            //}

            NetworkManager.objects[InstanceId] = this;

            foreach (var variable in variables.Values)
            {
                variable.ResetDirty();
                variable.networkObject = this;
            }
            isDirty = false;
            OwnerClientId = ownerClientId;
            NetworkManager.SpawnObject(this);

            if (OwnerClientId != NetworkManager.ServerClientId)
            {
                AddObserver(ownerClientId);
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

            if (!NetworkManager.clients.TryGetValue(clientId, out var client))
            {
                return;
            }



            networkManager.SendMessage(clientId, (ushort)NetworkMsgId.CreateObject, new CreateObjectMessage()
            {
                typeId = typeId,
                objectId = InstanceId,
                ownerClientId = OwnerClientId,
            });

            SyncVariable(client);

            if (NetworkManager.LogLevel <= LogLevel.Debug)
                NetworkManager.Log($"Send Spawn Msg, Object Type: {GetType()}, InstanceId: {InstanceId}, Client: {clientId}");
            networkManager.SendMessage(clientId, (ushort)NetworkMsgId.Spawn, new SpawnMessage()
            {
                instanceId = InstanceId,
                ownerClientId = OwnerClientId,
            });
        }


        public void Despawn(bool destrory = true)
        {
            if (!IsServer) throw new NotServerException($"{nameof(Despawn)} only on server");
            if (!IsSpawned) throw new Exception("AddObserver require Spawned");


            if (observers.Count > 0)
            {
                foreach (var clientId in Observers.ToArray())
                {
                    RemoveObserver(clientId, destrory);
                }
            }

            NetworkManager.DespawnObject(this);

            if (destrory)
            {
                NetworkManager.DestroryObject(this);
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


            if (!NetworkManager.ContainsClient(clientId))
                return;

            //新客户端孵化对象前，确保为全量状态同步
            SyncState();

            observers.Add(clientId);

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
            if (clientId == NetworkManager.ServerClientId)
                return;

            if (!observers.Contains(clientId))
                return;

            if (!NetworkManager.clients.TryGetValue(clientId, out var client))
                return;

            SyncState();

            observers.Remove(clientId);

            //NetworkManager.Log("Remvoe Observer: " + this + ", client: " + clientId);
            client.SendMessage((ushort)NetworkMsgId.Despawn, new DespawnMessage()
            {
                instanceId = InstanceId,
                isDestroy = isDestroy
            });
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
                        variable.networkObject = this;
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

        internal void SyncVariable(NetworkClient client)
        {

            if (syncVarStates != null)
            {
                // conn.SendMessage((ushort)NetworkMsgId.SyncVar, SyncVarMessage.ResponseSyncVar(this, uint.MaxValue));
            }

            client.SendMessage((ushort)NetworkMsgId.SyncVar, new SyncVarMessage(this, false, true));
        }

        internal void SyncVariableDelta()
        {

            byte[] packet = null;
            SyncVarMessage msg = null;
            foreach (var pair in variables)
            {
                var variable = pair.Value;
                if (variable.IsDirty() && variable.CanClientWrite(NetworkManager.LocalClientId))
                {
                    if (packet == null)
                    {
                        msg = new SyncVarMessage(this, true, false);
                        packet = NetworkManager.PackMessage((ushort)NetworkMsgId.SyncVar, msg);
                    }
                    variable.ResetDirty();
                }
            }

            if (packet != null)
            {
                if (IsServer)
                {
                    foreach (var clientId in observers)
                    {
                        NetworkManager.SendPacket(clientId, (ushort)NetworkMsgId.SyncVar, packet);
                    }
                }
                else
                {
                    NetworkManager.SendPacket(NetworkManager.LocalClientId, (ushort)NetworkMsgId.SyncVar, packet);
                }
            }


        }

        internal void InternalUpdate()
        {

            SyncState();

            OnUpdate();
        }


        public bool IsDirty()
        {
            return isDirty;
        }

        internal virtual void SetDirty()
        {
            this.isDirty = true;
        }



        protected virtual void OnUpdate()
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


        internal ServerRpcInfo serverRpc;
 

        protected void BeginServerRpc(string methodName, params object[] args)
        {
            __BeginServerRpc__(methodName, default, args);
        }

        [DebuggerHidden]
        public void __BeginServerRpc__(string methodName, ServerRpcParams rpcParams, params object[] args)
        {
            if (remoteRpc)
            {
                return;
            }

            if (!IsClient)
            {
                NetworkManager.LogError("ServerRpc only client call");
                return;
            }

            serverRpc = new ServerRpcInfo();
            serverRpc.serverParams = rpcParams;
            serverRpc.rpcInfo = RpcInfo.GetRpcInfo(GetType(), methodName);
            serverRpc.args = args;
        }

        [DebuggerHidden]
        protected void __EndServerRpc__()
        {
            if (remoteRpc)
            {
                return;
            }

            if (!IsServer)
            {
                var msg = RpcMessage.RpcServer(this, serverRpc.rpcInfo, serverRpc.args);
                networkManager.SendMessage(NetworkManager.ServerClientId, (ushort)NetworkMsgId.Rpc, msg);
            }
        }
        [DebuggerHidden]
        protected bool __ReturnServerRpc__()
        {
            remoteRpc = false;
            return !IsServer;
        }

        internal ClientRpcInfo clientRpc;
        internal bool remoteRpc;
        internal struct ServerRpcInfo
        {
            public ServerRpcParams serverParams;
            public RpcInfo rpcInfo;
            public object[] args;
            public bool fromRemote;
        }
        internal struct ClientRpcInfo
        {
            public RpcInfo rpcInfo;
            public object[] args;
            public ClientRpcParams clientParams;
            public bool fromRemote;
        }
        [DebuggerHidden]
        protected void BeginClientRpc(string methodName, params object[] args)
        {
            __BeginClientRpc__(methodName, default, args);
        }
        [DebuggerHidden]
        protected void __BeginClientRpc__(string methodName, ClientRpcParams rpcParams, params object[] args)
        {
            if (remoteRpc)
            {
                return;
            }

            if (!this.clientRpc.fromRemote && !IsServer)
            {
                NetworkManager.LogError("ClientRpc only server call");
                return;
            }

            clientRpc = new ClientRpcInfo();
            clientRpc.clientParams = rpcParams;
            clientRpc.rpcInfo = RpcInfo.GetRpcInfo(GetType(), methodName);
            clientRpc.args = args;
        }
        [DebuggerHidden]
        protected void __EndClientRpc__()
        {
            if (remoteRpc)
            {
                return;
            }

            if (IsServer)
            {
                var rpcInfo = clientRpc.rpcInfo;
                RpcMessage msg = RpcMessage.RpcClient(this, rpcInfo, clientRpc.args);

                SyncState();

                foreach (var client in NetworkManager.GetAvaliableClients(observers))
                {
                    if (client.ClientId == NetworkManager.ServerClientId)
                    {
                        continue;
                    }

                    if (clientRpc.clientParams.clients != null && !clientRpc.clientParams.clients.Contains(client.ClientId))
                        continue;

                    //NetworkManager.Log($"Rpc {GetType().Name}:{rpcInfo.method.Name} Client [{_conn.ClientId}]");
                    client.SendMessage((ushort)NetworkMsgId.Rpc, msg);
                }

            }
        }

        protected bool __ReturnClientRpc__()
        {
            remoteRpc = false;
            if (!IsClient)
            {
                return true;
            }

            var clientParams = clientRpc.clientParams;
            var clients = clientParams.clients;

            if (IsServer)
            {
                if (clients == null || clients.Contains(NetworkManager.LocalClientId))
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }

            //if (NetworkManager.LocalClientId == NetworkManager.ServerClientId)
            //{
            //    return false;
            //}


            return false;
        }

        #endregion
        internal bool isDestrory;

        public void Destrory()
        {
            if (!NetworkManager.IsServer) throw new NotServerException($"{nameof(Destrory)} only on server");

            if (isDestrory) return;

            if (IsSpawned)
            {
                Despawn(false);
            }

            NetworkManager.DestroryObject(this);
        }

        internal protected virtual void OnDestrory()
        {

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
