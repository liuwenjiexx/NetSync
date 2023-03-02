using System;

namespace Yanmonet.NetSync
{
    internal class SyncVarMessage : MessageBase
    {
        public NetworkObject netObj;
        public NetworkConnection conn;
        public uint bits;
        public byte action;
        public bool isDelta;
        public bool isAll;
        public bool keepDirty;
        public byte type;        
        public const byte Action_ResponseSyncVar = 1;
        public const byte Action_RequestSyncVar = 2;


        public SyncVarMessage(NetworkObject netObj)
        {
            this.netObj = netObj;
        }
        public SyncVarMessage()
        {
        }

        public SyncVarMessage(NetworkObject netObj, bool isDelta, bool keepDirty)
        {
            this.netObj = netObj;
            this.isDelta = isDelta;
            this.keepDirty = keepDirty;
        }

        public static SyncVarMessage ResponseSyncVar(NetworkObject netObj, uint bits)
        {
            return new SyncVarMessage(netObj)
            {
                action = Action_ResponseSyncVar,
                bits = bits
            };
        }

        public static SyncVarMessage RequestSyncVar(NetworkObject netObj, uint bits)
        {
            return new SyncVarMessage(netObj)
            {
                action = Action_RequestSyncVar,
                bits = bits,
            };
        }


        public override void Deserialize(IReaderWriter reader)
        {
            this.bits = 0;
            ulong instanceId = 0;

            reader.SerializeValue(ref action);
            reader.SerializeValue(ref instanceId);
            reader.SerializeValue(ref isDelta);
            netObj = null;

            if (conn.NetworkManager.IsServer)
            {
                netObj = conn.NetworkManager.Server.GetObject(instanceId);
            }
            else
            {
                netObj = conn.GetObject(instanceId);
            }

            if (netObj == null)
                return;

            switch (action)
            {
                case Action_ResponseSyncVar:
                    while (true)
                    {
                        byte b = 0;
                        reader.SerializeValue(ref b);
                        if (b == 0)
                            break;
                        b--;
                        //netObj.NetworkManager.Log($"Read variable, type: {netObj.GetType().Name}, instance: {instanceId}, index: {b}");
                        var state = netObj.GetStateByIndex(b);
                        if (state == null)
                        {
                            throw new NullReferenceException($"{netObj.GetType()}, State null, instance: {instanceId}, index: {b}");
                        }

                        object value = null;

                        value = Read(reader, state.syncVarInfo.field.FieldType);

                        try
                        {
                            state.value = value;
                            if (netObj != null)
                            {
                                if (state.syncVarInfo.changeCallback != null)
                                {
                                    state.syncVarInfo.changeCallback.Invoke(netObj, new object[] { value });
                                }
                                else
                                {
                                    state.syncVarInfo.field.SetValue(netObj, value);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            conn.NetworkManager.LogException(ex);
                        }
                    }

                    if (netObj.IsServer)
                    {
                        foreach (var _conn in netObj.NetworkManager.GetAvaliableConnections(netObj.Observers))
                        {
                            if (_conn.ConnectionId == netObj.OwnerClientId)
                                continue;
                            _conn.SendMessage((ushort)NetworkMsgId.SyncVar, this);
                        }

                    }

                    break;
                case Action_RequestSyncVar:
                    reader.SerializeValue(ref bits);
                    break;
                default:
                    uint hash = 0;
                    NetworkVariableBase variable = null;

                    while (true)
                    {
                        reader.SerializeValue(ref hash);
                        //netObj.NetworkManager.Log($"Read variable, type: {netObj}, hash: {hash}");
                        if (hash == 0)
                            break;
                        if (!netObj.variables.TryGetValue(hash, out variable))
                        {
                            throw new Exception($"Not found variable, hash: {hash}");
                        }

                        if (isDelta)
                        {
                            variable.ReadDelta(reader, false);
                        }
                        else
                        {
                            variable.Read(reader);
                        }
                        //netObj.NetworkManager.Log($"Read variable, type: {netObj}, variable: {variable}");
                    }
                    break;
            }
        }



        public override void Serialize(IReaderWriter writer)
        {
            byte b = 0;

            writer.SerializeValue(ref action);
            writer.SerializeValue(ref netObj.objectId);
            writer.SerializeValue(ref isDelta);

            switch (action)
            {
                case Action_ResponseSyncVar:
                    SyncVarInfo info;
                    foreach (var state in netObj.syncVarStates)
                    {
                        if (state == null) continue;
                        info = state.syncVarInfo;
                        if (info.isVariable)
                            continue;
                        if ((info.bits & bits) == info.bits)
                        {
                            //netObj.NetworkManager.Log($"Write variable, type: {netObj.GetType().Name}, instance: {netObj.InstanceId}, index: {info.index}");
                            b = (byte)(info.index + 1);
                            writer.SerializeValue(ref b);
                            object value = state.syncVarInfo.field.GetValue(netObj);
                            state.value = value;

                            Write(writer, info.field.FieldType, value);
                        }
                    }

                    //write end
                    b = 0;
                    writer.SerializeValue(ref b);
                    break;
                case Action_RequestSyncVar:
                    writer.SerializeValue(ref bits);
                    break;
                default:
                    uint hash = 0;

                    foreach (var pair in netObj.variables)
                    {
                        var variable = pair.Value;
                        if (!variable.CanClientWrite(netObj.NetworkManager.LocalClientId))
                            continue;
                        if (!isDelta || variable.IsDirty())
                        {
                            hash = pair.Key;
                            writer.SerializeValue(ref hash);
                            if (isDelta)
                            {
                                variable.WriteDelta(writer);
                            }
                            else
                            {
                                variable.Write(writer);
                            }
                            if (!keepDirty)
                            {
                                variable.ResetDirty();
                            }
                            //netObj.NetworkManager.Log($"Write variable, type: {netObj}, variable: {variable}, hash: {hash}");
                        }
                    }
                    hash = 0;
                    writer.SerializeValue(ref hash);
                    break;
            }

        }

    }

}
