using System;

namespace Yanmonet.Network.Sync
{


    public class MessageBase
    {

        public static Action<IReaderWriter, MessageBase> DefaultDeserialize;
        public static Func<IReaderWriter, MessageBase, MessageBase> DefaultSerialize;

        public static Action<string, object> DeserializeFromString;
        public static Func<object, string> SerializeToString;


        static MessageBase()
        {
            //SerializeToString = (msg) =>
            //{
            //    string jsonStr;
            //    jsonStr = Newtonsoft.Json.JsonConvert.SerializeObject(msg);
            //    return jsonStr;
            //};
            //DeserializeFromString = (str, msg) =>
            //{
            //    Newtonsoft.Json.JsonConvert.PopulateObject(str, msg);
            //};

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
            if (typeof(INetworkSerializable).IsAssignableFrom(type))
                return true;

            if (typeof(MessageBase).IsAssignableFrom(type))
                return true;

            TypeCode typeCode = Type.GetTypeCode(type);

            if (type == typeof(byte[]))
                return true;
            switch (typeCode)
            {
                case TypeCode.String:
                case TypeCode.Int32:
                case TypeCode.UInt32:
                case TypeCode.Single:
                case TypeCode.Double:
                case TypeCode.Boolean:
                case TypeCode.Byte:
                case TypeCode.Int64:
                case TypeCode.UInt64:
                    return true;
            }
            return false;
        }


        public static void Write(IReaderWriter writer, Type valueType, object value)
        {
            if (typeof(INetworkSerializable).IsAssignableFrom(valueType))
            {
                INetworkSerializable s = value as INetworkSerializable;
                if (s == null)
                {
                    throw new Exception("INetworkSerializable type object null, value type: " + valueType);
                }
                s.NetworkSerialize(writer);
                return;
            }

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
                case TypeCode.UInt32:
                    uint uint32 = (uint)value;
                    writer.SerializeValue(ref uint32);
                    break;
                case TypeCode.Single:
                    float float32 = (float)value;
                    writer.SerializeValue(ref float32);
                    break;
                case TypeCode.Double:
                    double float64 = (double)value;
                    writer.SerializeValue(ref float64);
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
                case TypeCode.Int64:
                    var int64 = (long)value;
                    writer.SerializeValue(ref int64);
                    break;
                case TypeCode.UInt64:
                    var uint64 = (ulong)value;
                    writer.SerializeValue(ref uint64);
                    break;
            }
        }


        public static object Read(IReaderWriter reader, Type valueType)
        {
            TypeCode typeCode = Type.GetTypeCode(valueType);
            object value = null;

            if (typeof(INetworkSerializable).IsAssignableFrom(valueType))
            {
                INetworkSerializable s = (INetworkSerializable)Activator.CreateInstance(valueType);
                s.NetworkSerialize(reader);
                return s;
            }

            if (typeof(MessageBase).IsAssignableFrom(valueType))
            {
                MessageBase messageBase = (MessageBase)Activator.CreateInstance(valueType);
                messageBase.Deserialize(reader);
                return messageBase;
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
                    int int32 = 0;
                    reader.SerializeValue(ref int32);
                    value = int32;
                    break;
                case TypeCode.UInt32:
                    uint uint32 = 0;
                    reader.SerializeValue(ref uint32);
                    value = uint32;
                    break;
                case TypeCode.Single:
                    float float32 = 0;
                    reader.SerializeValue(ref float32);
                    value = float32;
                    break;
                case TypeCode.Double:
                    double float64 = 0;
                    reader.SerializeValue(ref float64);
                    value = float64;
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
                    byte uint8 = 0;
                    reader.SerializeValue(ref uint8);
                    value = uint8;
                    break;
                case TypeCode.Int64:
                    long int64 = 0;
                    reader.SerializeValue(ref int64);
                    value = int64;
                    break;
                case TypeCode.UInt64:
                    ulong uint64 = 0;
                    reader.SerializeValue(ref uint64);
                    value = uint64;
                    break;
            }
            return value;

        }

        public virtual void Handle(NetworkMessage netMsg)
        {

        }

    }




}