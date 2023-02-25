using System;

namespace Yanmonet.NetSync
{
    internal class SyncListMessage : MessageBase
    {
        public NetworkObject netObj;
        public NetworkConnection conn;
        public byte action;
        public byte memberIndex;
        public byte itemIndex;

        public const byte Action_Add = 1;
        public const byte Action_Insert = 2;
        public const byte Action_Set = 3;
        public const byte Action_Remove = 4;
        public const byte Action_RemoveAt = 5;
        public const byte Action_Clear = 6;

        #region Create


        public static SyncListMessage Add(NetworkObject obj, byte memberIndex, byte itemIndex)
        {
            return new SyncListMessage()
            {
                action = Action_Add,
                netObj = obj,
                memberIndex = memberIndex,
                itemIndex = itemIndex,
            };
        }
        public static SyncListMessage Insert(NetworkObject obj, byte memberIndex, byte itemIndex)
        {
            return new SyncListMessage()
            {
                action = Action_Insert,
                netObj = obj,
                memberIndex = memberIndex,
                itemIndex = itemIndex,
            };
        }
        public static SyncListMessage Set(NetworkObject obj, byte memberIndex, byte itemIndex)
        {
            return new SyncListMessage()
            {
                action = Action_Set,
                netObj = obj,
                memberIndex = memberIndex,
                itemIndex = itemIndex,
            };
        }
        public static SyncListMessage Remove(NetworkObject obj, byte memberIndex, byte itemIndex)
        {
            return new SyncListMessage()
            {
                action = Action_Add,
                netObj = obj,
                memberIndex = memberIndex,
                itemIndex = itemIndex,
            };
        }
        public static SyncListMessage RemoveAt(NetworkObject obj, byte memberIndex, byte itemIndex)
        {
            return new SyncListMessage()
            {
                action = Action_RemoveAt,
                netObj = obj,
                memberIndex = memberIndex,
                itemIndex = itemIndex,
            };
        }

        public static SyncListMessage Clear(NetworkObject obj, byte memberIndex)
        {
            return new SyncListMessage()
            {
                action = Action_Clear,
                netObj = obj,
                memberIndex = memberIndex,
            };
        }

        #endregion


        public override void Serialize(NetworkWriter writer)
        {
            if (action == 0)
                throw new Exception("action is 0");

            var info = SyncListInfo.GetSyncListInfo(netObj.GetType(), memberIndex);

            writer.WriteByte(action);
            writer.WriteNetworkInstanceId(netObj.InstanceId);
            writer.WriteByte(memberIndex);
            switch (action)
            {
                case Action_Add:
                case Action_Insert:
                case Action_Set:
                    object list = info.field.GetValue(netObj);

                    if (action == Action_Insert || action == Action_Set)
                    {
                        writer.WriteByte(itemIndex);
                    }
                    object item = info.ItemProperty.GetGetMethod().Invoke(list, new object[] { (int)itemIndex });

                    info.SerializeItemMethod.Invoke(list, new object[] { writer, item });
                    break;

                case Action_RemoveAt:
                    writer.WriteByte(itemIndex);
                    break;
            }

        }

        public override void Deserialize(NetworkReader reader)
        {
            action = reader.ReadByte();
            NetworkInstanceId instanceId;
            instanceId = reader.ReadNetworkInstanceId();
            netObj = null;
            netObj = conn.GetObject(instanceId);
            if (netObj == null)
                return;
            memberIndex = reader.ReadByte();
            var info = SyncListInfo.GetSyncListInfo(netObj.GetType(), memberIndex);

            object list = info.field.GetValue(netObj);
            switch (action)
            {
                case Action_Add:
                case Action_Insert:
                case Action_Set:
                    {
                        if (action == Action_Add)
                        {
                            itemIndex = (byte)(int)info.CountProperty.GetGetMethod().Invoke(list, null);
                        }
                        else
                        {
                            itemIndex = reader.ReadByte();
                        }
                        object item;
                        item = info.DeserializeItemMethod.Invoke(list, new object[] { reader });
                        info.InsertMethod.Invoke(list, new object[] { (int)itemIndex, item });
                    }
                    break;
                case Action_RemoveAt:
                case Action_Remove:
                    if (action == Action_Remove)
                    {
                        itemIndex = (byte)(int)info.CountProperty.GetGetMethod().Invoke(list, null);
                    }
                    else
                    {
                        itemIndex = reader.ReadByte();
                    }
                    info.RemoveAtMethod.Invoke(list, new object[] { itemIndex });
                    break;
                case Action_Clear:
                    info.ClearMethod.Invoke(list, null);
                    break;
            }

        }

    }

}
