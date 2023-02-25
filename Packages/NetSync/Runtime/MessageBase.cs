using System;

namespace Yanmonet.NetSync
{


    public class MessageBase
    {

        public static Action<NetworkReader, MessageBase> DefaultDeserialize;
        public static Func<NetworkWriter, MessageBase, MessageBase> DefaultSerialize;

        public static Action<string, object> DeserializeFromString;
        public static Func<object, string> SerializeToString;


        static MessageBase()
        {
            SerializeToString = (msg) =>
            {
                string jsonStr;
                jsonStr = Newtonsoft.Json.JsonConvert.SerializeObject(msg);
                return jsonStr;
            };
            DeserializeFromString = (str, msg) =>
            {
                Newtonsoft.Json.JsonConvert.PopulateObject(str, msg);
            };

        }


        public virtual void Serialize(NetworkWriter writer)
        {
            if (DefaultSerialize != null)
            {
                DefaultSerialize(writer, this);
            }
            else
            {
                string str;
                str = SerializeToString(this);
                writer.WriteString(str);
            }
        }
        public virtual void Deserialize(NetworkReader reader)
        {
            if (DefaultDeserialize != null)
            {
                DefaultDeserialize(reader, this);
            }
            else
            {
                string str = reader.ReadString();
                DeserializeFromString(str, this);
            }

        }

        public static bool CanSerializeType(Type type)
        {
            if (typeof(MessageBase).IsAssignableFrom(type))
                return true;

            TypeCode typeCode = Type.GetTypeCode(type);
            if (type == typeof(NetworkInstanceId))
                return true;
            if (type == typeof(NetworkObjectId))
                return true;
            if (type == typeof(byte[]))
                return true;
            switch (typeCode)
            {
                case TypeCode.String:
                case TypeCode.Int32:
                case TypeCode.Single:
                case TypeCode.Boolean:
                case TypeCode.Byte:
                case TypeCode.UInt64:
                    return true;
            }
            return false;
        }


        public static void Write(NetworkWriter writer, Type valueType, object value)
        {
            if (typeof(MessageBase).IsAssignableFrom(valueType))
            {
                if (value == null)
                {
                    throw new Exception("Write object null, value type: " + valueType);
                }
                MessageBase messageBase = (MessageBase)value;
                messageBase.Serialize(writer);
                return;
            }


            TypeCode typeCode = Type.GetTypeCode(valueType);
            if (valueType == typeof(NetworkInstanceId))
            {
                writer.WriteNetworkInstanceId((NetworkInstanceId)value);
                return;
            }

            if (valueType == typeof(NetworkObjectId))
            {
                writer.WriteNetworkObjectId((NetworkObjectId)value);
                return;
            }

            if (valueType == typeof(byte[]))
            {
                byte[] bytes = value as byte[];
                if (bytes == null)
                {
                    writer.WriteInt32(0);
                }
                else
                {
                    writer.WriteInt32(bytes.Length);
                    writer.Write(bytes, 0, bytes.Length);
                }
                return;
            }

            switch (typeCode)
            {
                case TypeCode.Int32:
                    writer.WriteInt32((int)value);
                    break;
                case TypeCode.Single:
                    writer.WriteFloat32((float)value);
                    break;
                case TypeCode.String:
                    if (value == null)
                        writer.WriteString(string.Empty);
                    else
                        writer.WriteString(value as string);
                    break;
                case TypeCode.Boolean:
                    writer.WriteBool((bool)value);
                    break;
                case TypeCode.Byte:
                    writer.WriteByte((byte)value);
                    break;
                case TypeCode.UInt64:
                    writer.WriteUInt64((ulong)value);
                    break;
            }
        }


        public static object Read(NetworkReader reader, Type valueType)
        {
            TypeCode typeCode = Type.GetTypeCode(valueType);
            object value = null;

            if (typeof(MessageBase).IsAssignableFrom(valueType))
            {
                MessageBase messageBase = (MessageBase)Activator.CreateInstance(valueType);
                messageBase.Deserialize(reader);
                return messageBase;
            }

            if (valueType == typeof(NetworkInstanceId))
            {
                value = reader.ReadNetworkInstanceId();
                return value;
            }

            if (valueType == typeof(NetworkObjectId))
            {
                value = reader.ReadNetworkObjectId();
                return value;
            }

            if (valueType == typeof(byte[]))
            {
                byte[] bytes;
                int count = reader.ReadInt32();
                if (count > 0)
                {
                    bytes = new byte[count];
                    reader.Read(bytes, 0, count);
                }
                else
                {
                    bytes = null;
                }
                return bytes;
            }

            switch (typeCode)
            {
                case TypeCode.Int32:
                    value = reader.ReadInt32();
                    break;
                case TypeCode.Single:
                    value = reader.ReadFloat32();
                    break;
                case TypeCode.String:
                    value = reader.ReadString();
                    break;
                case TypeCode.Boolean:
                    value = reader.ReadBool();
                    break;
                case TypeCode.Byte:
                    value = reader.ReadByte();
                    break;
                case TypeCode.UInt64:
                    value = reader.ReadUInt64();
                    break;
            }
            return value;

        }
    }




}