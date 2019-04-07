using System;
using System.IO;
using System.Text;

namespace Net
{


    public class MessageBase
    {

        public static Action<Stream, MessageBase> DefaultDeserialize;
        public static Func<Stream, MessageBase, MessageBase> DefaultSerialize;

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


        public virtual void Serialize(Stream writer)
        {
            if (DefaultSerialize != null)
            {
                DefaultSerialize(writer, this);
            }
            else
            {
                using (var bw = new BinaryWriter(new DisposableStream(writer, false), Encoding.UTF8))
                {
                    string str;
                    str = SerializeToString(this);
                    bw.Write(str);
                }
            }
        }
        public virtual void Deserialize(Stream reader)
        {
            if (DefaultDeserialize != null)
            {
                DefaultDeserialize(reader, this);
            }
            else
            {
                using (var br = new BinaryReader(new DisposableStream(reader, false), Encoding.UTF8))
                {
                    string str = br.ReadString();
                    DeserializeFromString(str, this);
                }
            }

        }

        public static bool CanSerializeType(Type type)
        {
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
                    return true;
            }
            return false;
        }


        public static void Write(BinaryWriter writer, Type valueType, object value)
        {
            TypeCode typeCode = Type.GetTypeCode(valueType);
            if (valueType == typeof(NetworkInstanceId))
            {
                writer.Write(((NetworkInstanceId)value).Value);
                return;
            }

            if (valueType == typeof(NetworkObjectId))
            {
                writer.Write(((NetworkObjectId)value).Value.ToString());
                return;
            }

            if (valueType == typeof(byte[]))
            {
                byte[] bytes = value as byte[];
                if (bytes == null)
                {
                    writer.Write(0);
                }
                else
                {
                    writer.Write(bytes.Length);
                    writer.Write(bytes, 0, bytes.Length);
                }
                return;
            }

            switch (typeCode)
            {
                case TypeCode.Int32:
                    writer.Write((int)value);
                    break;
                case TypeCode.Single:
                    writer.Write((float)value);
                    break;
                case TypeCode.String:
                    if (value == null)
                        writer.Write(string.Empty);
                    else
                        writer.Write(value as string);
                    break;
                case TypeCode.Boolean:
                    writer.Write((bool)value);
                    break;
                case TypeCode.Byte:
                    writer.Write((byte)value);
                    break;
            }
        }


        public static object Read(BinaryReader reader, Type valueType)
        {
            TypeCode typeCode = Type.GetTypeCode(valueType);
            object value = null;
            if (valueType == typeof(NetworkInstanceId))
            {
                value = new NetworkInstanceId(reader.ReadUInt32());
                return value;
            }

            if (valueType == typeof(NetworkObjectId))
            {
                value = new NetworkObjectId(new Guid(reader.ReadString()));
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
                    value = reader.ReadSingle();
                    break;
                case TypeCode.String:
                    value = reader.ReadString();
                    break;
                case TypeCode.Boolean:
                    value = reader.ReadBoolean();
                    break;
                case TypeCode.Byte:
                    value = reader.ReadByte();
                    break;
            }
            return value;

        }
    }




}