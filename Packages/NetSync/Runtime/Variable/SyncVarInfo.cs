using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Yanmonet.NetSync
{
    internal class SyncVarInfo
    {
        public byte index;
        public TypeCode typeCode;
        public uint bits;
        public uint hash;
        public FieldInfo field;
        public MethodInfo changeCallback;
        public bool isVariable;
        public string signature;

        private static Dictionary<Type, SyncVarInfo[]> cachedInfos;
        private static Dictionary<uint, SyncVarInfo> fieldMaps;

        public override string ToString()
        {
            return string.Format("field:{0}, type:{1}, bits:{2}", field.Name, field.FieldType.Name, bits);
        }


        public static SyncVarInfo[] GetSyncVarInfos(Type type)
        {
            if (cachedInfos == null)
            {
                cachedInfos = new Dictionary<Type, SyncVarInfo[]>();
                fieldMaps = new();
            }

            SyncVarInfo[] infos;
            if (!cachedInfos.TryGetValue(type, out infos))
            {
                List<SyncVarInfo> list = null;
                foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    if (typeof(SyncBase).IsAssignableFrom(field.FieldType))
                    {
                        if (field.FieldType.IsAbstract)
                            continue;
                        if (field.IsStatic)
                        {
                            throw new Exception("Network Variable can't static");
                            continue;
                        }
                        SyncVarInfo info = new SyncVarInfo();
                        info.field = field;
                        info.signature = NetworkUtility.GetFieldSignature(field);
                        info.hash = NetworkUtility.GetFieldSignatureHash(field);
                        info.isVariable = true;

                        if (fieldMaps.ContainsKey(info.hash))
                        {
                            var oldVar = fieldMaps[info.hash];
                            throw new Exception($"Network Variable hash duplicate, need rename field, field: {info.signature}, hash: {info.hash}, field2: {oldVar.signature}, hash: {info.hash}");
                        }
                        if (list == null)
                            list = new List<SyncVarInfo>();
                        list.Add(info);
                        fieldMaps[info.hash] = info;
                        continue;
                    }
                    
                }

                if (list != null)
                {
                    infos = new SyncVarInfo[list.Count];

                    byte index = 0;
                    foreach (var item in list.OrderBy(o => o.field.Name))
                    {
                        item.index = index++;
                    }


                    int n = 0;
                    for (int i = 0; i < list.Count; i++)
                    {
                        var info = list[i];
                        if (info.bits == 0)
                        {
                            uint bits = 0, tmp;
                            for (; n < 32; n++)
                            {
                                tmp = (uint)(1 << n);
                                if (list.Where(o => (o.bits & tmp) != 0).Count() == 0)
                                {
                                    bits = tmp;
                                    n++;
                                    break;
                                }
                            }
                            if (bits == 0)
                                throw new Exception("not avalible bits");
                            info.bits = bits;
                        }
                        else
                        {
                            for (int j = i - 1; j >= 0; j--)
                            {
                                if (list[j].bits == info.bits)
                                    throw new Exception("type: " + type.Name + " , field: " + info.field + ", repeat bits " + info.bits);
                            }

                        }

                        infos[info.index] = info;
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
