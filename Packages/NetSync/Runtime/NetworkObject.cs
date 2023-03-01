using Yanmonet.NetSync.Messages;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using static Yanmonet.NetSync.NetworkObject;
using System.Linq;
using UnityEngine;

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
        internal Dictionary<uint, NetworkVariableBase> variables;



        public NetworkObject()
        {
            observers = new List<ulong>();
            InitalizeVariable();
        }

        public ulong InstanceId { get => objectId; internal set { objectId = value; } }

        public IReadOnlyList<ulong> Observers { get => observers; }


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
            if (IsSpawned) throw new Exception("is Spawned");
            IsSpawned = true;
            sendSpawned = new();
            typeId = NetworkManager.GetTypeId(GetType());
            InstanceId = ++NetworkManager.Server.nextObjectId;

            NetworkManager.Server.objects[InstanceId] = this;
            OwnerClientId = ownerClientId;

            //if (!IsOwnedByServer)
            //{
            //    NetworkManager.Server.AddObserver();
            //}
            if (!IsOwnedByServer)
            {
                if (NetworkManager.clients.TryGetValue(ownerClientId, out var client))
                {
                    var conn = client.Connection;
                    ConnectionToOwner = conn;
                }
                AddObserver(ownerClientId);
            }

            foreach (var variable in variables.Values)
            {
                variable.ResetDirty();
            }
            isDirty = false;

            SendSpawnMsg();

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


        private HashSet<ulong> sendSpawned;

        private void SendSpawnMsg()
        {
            foreach (var conn in NetworkManager.GetAvaliableConnections(observers))
            {
                if (!conn.IsConnected)
                    continue;
                if (sendSpawned.Contains(conn.ConnectionId))
                    continue;
                sendSpawned.Add(conn.ConnectionId);
                NetworkManager.Log($"Send Create Object Msg, Type: {GetType()}");

                conn.SendMessage((ushort)NetworkMsgId.CreateObject, new CreateObjectMessage()
                {
                    typeId = typeId,
                    objectId = InstanceId,
                    ownerClientId = OwnerClientId,
                });

                if (IsOwnedByServer)
                {
                    SyncAll(conn);
                }

            }
        }


        public void Despawn(bool destrory = true)
        {
            if (!NetworkManager.IsServer) throw new NotServerException($"{nameof(Despawn)} only on server");

            if (IsSpawned)
            {
                if (NetworkManager.Server.objects.ContainsKey(InstanceId))
                {
                    foreach (var clientId in Observers.ToArray())
                    {
                        NetworkManager.Server.RemoveObserver(this, clientId);
                    }

                    NetworkManager.Server.objects.Remove(InstanceId);
                }

                InstanceId = default;
                IsSpawned = false;
                sendSpawned.Clear();
            }

            if (destrory)
            {
                Destrory();
            }
        }

        public void AddObserver(ulong clientId)
        {
            if (!NetworkManager.IsServer) throw new NotServerException($"{nameof(AddObserver)} only on server");
            NetworkManager.Server.AddObserver(this, clientId);

        }

        public void RemoveObserver(ulong clientId)
        {
            if (!NetworkManager.IsServer) throw new NotServerException($"{nameof(RemoveObserver)} only on server");
            NetworkManager.Server.RemoveObserver(this, clientId);
            sendSpawned.Remove(clientId);
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
                        var variable = field.GetValue(this) as NetworkVariableBase;
                        if (variable == null)
                        {
                            variable = Activator.CreateInstance(field.FieldType) as NetworkVariableBase;
                            field.SetValue(this, variable);
                        }
                        variable.Name = varInfo.field.Name;
                        variables[varInfo.hash] = variable;
                        variable.Initialize(this);
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

        internal void SyncAll(NetworkConnection conn)
        {

            if (syncVarStates != null)
            {
                // conn.SendMessage((ushort)NetworkMsgId.SyncVar, SyncVarMessage.ResponseSyncVar(this, uint.MaxValue));
            }

            
            conn.SendMessage((ushort)NetworkMsgId.SyncVar, new SyncVarMessage(this, false, true));

        }

        internal void UpdateVariable()
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
                        msg = new SyncVarMessage(this, true, false);
                        packet = NetworkUtility.PackMessage((ushort)NetworkMsgId.SyncVar, msg);

                        break;
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
            if (IsSpawned)
            {
                if (IsServer)
                {
                    SendSpawnMsg();
                }

                UpdateSyncVar();
                UpdateVariable();
                isDirty = false;
            }


            Update();
        }


        public bool IsDirty()
        {
            return isDirty;
        }

        public virtual void SetDirty()
        {
            this.isDirty = true;
        }



        protected virtual void Update()
        {

        }

        protected float time = 0;

        void UpdateSyncVar()
        {
            if (Time.time > time)
            {
                time = Time.time + 3;
            }
            if (IsOwner && syncVarDirtyBits != 0)
            {/*
                NetworkManager.Log($"{GetType().Name}, instance: {InstanceId}, isOwner: {IsOwner}, diryBits: {syncVarDirtyBits}");
                SyncVarMessage msg = SyncVarMessage.ResponseSyncVar(this, syncVarDirtyBits);
                ClearAllDirtyBits();
                if (IsServer)
                {
                    foreach (var conn in NetworkManager.GetAvaliableConnections(observers))
                    {
                        if (conn.ConnectionId == OwnerClientId)
                            continue;
                        conn.SendMessage((ushort)NetworkMsgId.SyncVar, msg);
                    }
                }
                else
                {
                    bool bbb = NetworkManager.LocalClient.Connection == connectionToServer;
                    NetworkManager.LocalClient.Connection.SendMessage((ushort)NetworkMsgId.SyncVar, msg);
                }*/
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

            if (IsClient)
            {
                var msg = RpcMessage.RpcServer(this, rpcInfo, args);
                connectionToServer.SendMessage((ushort)NetworkMsgId.Rpc, msg);
            }
            else if (IsServer)
            {
                var msg = RpcMessage.RpcClient(this, rpcInfo, args);
                if (conn != null)
                {
                    conn.SendMessage((ushort)NetworkMsgId.Rpc, msg);
                }
                else
                {
                    foreach (var _conn in NetworkManager.GetAvaliableConnections(observers))
                    {
                        _conn.SendMessage((ushort)NetworkMsgId.Rpc, msg);
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
