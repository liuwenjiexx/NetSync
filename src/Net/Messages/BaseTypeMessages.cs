using System;


namespace Net.Messages
{

    public class StringMessage : MessageBase
    {
        private string value;
        public StringMessage() { }
        public StringMessage(string value)
        {
            this.value = value;
        }
        public string Value { get => value; set => this.value = value; }
        public override void Serialize(NetworkWriter writer)
        {
            writer.WriteString(value);
        }
        public override void Deserialize(NetworkReader reader)
        {
            this.value = reader.ReadString();
        }
    }

    public class ByteMessage : MessageBase
    {
        private byte value;
        public ByteMessage() { }
        public ByteMessage(byte value)
        {
            this.value = value;
        }
        public byte Value { get => value; set => this.value = value; }
        public override void Serialize(NetworkWriter writer)
        {
            writer.WriteByte(value);
        }
        public override void Deserialize(NetworkReader reader)
        {
            value = (byte)reader.ReadByte();
        }

    }
    public class Int32Message : MessageBase
    {
        private int value;
        public Int32Message() { }
        public Int32Message(int value)
        {
            this.value = value;
        }
        public int Value { get => value; set => this.value = value; }
        public override void Serialize(NetworkWriter writer)
        {
            writer.WriteInt32(value);
        }
        public override void Deserialize(NetworkReader reader)
        {
            this.value = reader.ReadInt32();
        }

    }
    public class Int64Message : MessageBase
    {
        private long value;
        public Int64Message() { }
        public Int64Message(long value)
        {
            this.value = value;
        }
        public long Value { get => value; set => this.value = value; }

        //interface IMessageSerialize
        //{
        //    void Serialize(Stream writer);
        //    void Deserialize(Stream reader);
        //}
        public override void Serialize(NetworkWriter writer)
        {
            writer.WriteInt64(value);
        }
        public override void Deserialize(NetworkReader reader)
        {
            this.value = reader.ReadInt64();
        }
    }
    public class Float32Message : MessageBase
    {
        private float value;
        public Float32Message() { }
        public Float32Message(float value)
        {
            this.value = value;
        }
        public float Value { get => value; set => this.value = value; }

        public override void Serialize(NetworkWriter writer)
        {
            writer.WriteFloat32(value);
        }
        public override void Deserialize(NetworkReader reader)
        {
            this.value = reader.ReadFloat32();
        }
    }
}
