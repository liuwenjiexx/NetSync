using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Yanmonet.NetSync
{
    internal class SyncListInfo
    {
        public byte memberIndex;
        public FieldInfo field;
        public PropertyInfo ItemProperty;
        public PropertyInfo CountProperty;
        public MethodInfo InitMethod;
        public MethodInfo ClearMethod;
        public MethodInfo InsertMethod;
        public MethodInfo RemoveAtMethod;
        public MethodInfo DeserializeItemMethod;
        public MethodInfo SerializeItemMethod;

        private static Dictionary<Type, SyncListInfo[]> cachedInfos;

        public override string ToString()
        {
            return string.Format("field:{0}, type:{1}", field.Name, field.FieldType.Name);
        }

        public static SyncListInfo GetSyncListInfo(Type type, int memberIndex)
        {
            var infos = GetSyncListInfos(type);
            if (infos == null || memberIndex >= infos.Length)
                throw new Exception("invalid synclist index:" + memberIndex + "," + type);
            return infos[memberIndex];
        }

        public static SyncListInfo[] GetSyncListInfos(Type type)
        {
            if (cachedInfos == null)
                cachedInfos = new Dictionary<Type, SyncListInfo[]>();

            SyncListInfo[] infos;
            if (!cachedInfos.TryGetValue(type, out infos))
            {
                List<SyncListInfo> list = null;
                foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    Type listType = field.FieldType.GetGenericTypeDefinition(typeof(SyncList<>));
                    if (listType == null)
                        continue;
                    SyncListInfo info = new SyncListInfo();
                    info.field = field;
                    info.ItemProperty = listType.GetProperty("Item");
                    info.CountProperty = listType.GetProperty("Count");
                    info.InitMethod = listType.GetMethod("Init", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    info.InsertMethod = listType.GetMethod("Insert");
                    info.RemoveAtMethod = listType.GetMethod("RemoveAt");
                    info.ClearMethod = listType.GetMethod("Clear");
                    info.DeserializeItemMethod = listType.GetMethod("DeserializeItem", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    info.SerializeItemMethod = listType.GetMethod("SerializeItem", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (list == null)
                        list = new List<SyncListInfo>();
                    info.memberIndex = (byte)list.Count();
                    list.Add(info);
                }

                if (list != null)
                {
                    if (list.Count > 32)
                        throw new Exception("max 32 sync var");

                    infos = new SyncListInfo[list.Count];

                    foreach (var info in list)
                    {
                        infos[info.memberIndex] = info;
                    }
                }
                else
                {
                    infos = null;
                }
                cachedInfos[type] = infos;
            }
            return infos;
        }
    }
}
