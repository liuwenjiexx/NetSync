using System;

namespace Yanmonet.NetSync
{
    internal class SyncVarMessage : MessageBase
    {
        public NetworkObject netObj;
        public NetworkConnection conn;
        public uint bits;
        public byte action;
        public const byte Action_ResponseSyncVar = 1;
        public const byte Action_RequestSyncVar = 2;

        public SyncVarMessage(NetworkObject netObj)
        {
            this.netObj = netObj;
        }
        public SyncVarMessage()
        {
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

            {
                reader.SerializeValue(ref action);

                if (action == 0)
                    throw new Exception("action is 0");

                ulong instanceId = 0;
                reader.SerializeValue(ref instanceId);
                netObj = null;

                netObj = conn.GetObject(instanceId);
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
                            uint bits = (uint)(1 << (b - 1));
                            this.bits |= bits;
                            var state = netObj.GetStateByBits(bits);
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
                        break;
                    case Action_RequestSyncVar:
                        reader.SerializeValue(ref bits);
                        break;
                }
            }
        }



        public override void Serialize(IReaderWriter writer)
        {
            if (action == 0)
                throw new Exception("action is 0");

            {
                byte b = 0;

                writer.SerializeValue(ref action);
                writer.SerializeValue(ref netObj.objectId);
                switch (action)
                {
                    case Action_ResponseSyncVar:
                        SyncVarInfo info;
                        foreach (var state in netObj.syncVarStates)
                        {
                            info = state.syncVarInfo;
                            if ((info.bits & bits) == info.bits)
                            {
                                b = (byte)info.bits.SigleBitPosition();
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
                }
            }
        }

    }

}
