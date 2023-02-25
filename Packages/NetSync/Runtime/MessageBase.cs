using System;

namespace Yanmonet.NetSync
{


    public class MessageBase
    {

        public static Action<IReaderWriter, MessageBase> DefaultDeserialize;
        public static Func<IReaderWriter, MessageBase, MessageBase> DefaultSerialize;

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


        public virtual void Serialize(IReaderWriter writer)
        {
            if (DefaultSerialize != null)
            {
                DefaultSerialize(writer, this);
            }
            else
            {
                string str;
                str = SerializeToString(this);
                writer.SerializeValue(ref str);
            }
        }
        public virtual void Deserialize(IReaderWriter reader)
        {
            if (DefaultDeserialize != null)
            {
                DefaultDeserialize(reader, this);
            }
            else
            {
                string str = null;
                reader.SerializeValue(ref str);
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


        public static void Write(IReaderWriter writer, Type valueType, object value)
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
                NetworkInstanceId value2 = default;
                writer.SerializeValue(ref value2);
                value = value2;
                return;
            }

            if (valueType == typeof(NetworkObjectId))
            {
                NetworkObjectId value2 = (NetworkObjectId)value;
                writer.SerializeValue(ref value2);
                return;
            }
            int int32;
            if (valueType == typeof(byte[]))
            {
                byte[] bytes = value as byte[];
                if (bytes == null)
                {
                    int32 = 0;
                    writer.SerializeValue(ref int32);
                }
                else
                {
                    int32 = bytes.Length;
                    writer.SerializeValue(ref bytes, 0, ref int32);
                }
                return;
            }

            switch (typeCode)
            {
                case TypeCode.Int32:
                    int32 = (int)value;
                    writer.SerializeValue(ref int32);
                    break;
                case TypeCode.Single:
                    float float32 = (float)value;
                    writer.SerializeValue(ref float32);
                    break;
                case TypeCode.String:
                    string str = value as string;
                    writer.SerializeValue(ref str);
                    break;
                case TypeCode.Boolean:
                    var b = (bool)value;
                    writer.SerializeValue(ref b);
                    break;
                case TypeCode.Byte:
                    var b1 = (byte)value;
                    writer.SerializeValue(ref b1);
                    break;
                case TypeCode.UInt64:
                    var ul = (ulong)value;
                    writer.SerializeValue(ref ul);
                    break;
            }
        }


        public static object Read(IReaderWriter reader, Type valueType)
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
                NetworkInstanceId id = new();
                reader.SerializeValue(ref id);
                value = id;
                return value;
            }

            if (valueType == typeof(NetworkObjectId))
            {
                NetworkObjectId id = new();
                reader.SerializeValue(ref id);
                value = id;
                return value;
            }

            if (valueType == typeof(byte[]))
            {
                byte[] bytes = null;
                int count = 0;
                reader.SerializeValue(ref bytes, 0, ref count);
                return bytes;
            }

            switch (typeCode)
            {
                case TypeCode.Int32:
                    int i32 = 0;
                    reader.SerializeValue(ref i32);
                    value = i32;
                    break;
                case TypeCode.Single:
                    float f32 = 0;
                    reader.SerializeValue(ref f32);
                    value = f32;
                    break;
                case TypeCode.String:
                    string str = null;
                    reader.SerializeValue(ref str);
                    value = str;
                    break;
                case TypeCode.Boolean:
                    bool b = false;
                    reader.SerializeValue(ref b);
                    value = b;
                    break;
                case TypeCode.Byte:
                    byte i8 = 0;
                    reader.SerializeValue(ref i8);
                    value = i8;
                    break;
                case TypeCode.UInt64:
                    ulong ulong64 = 0;
                    reader.SerializeValue(ref ulong64);
                    value = ulong64;
                    break;
            }
            return value;

        }
    }




}