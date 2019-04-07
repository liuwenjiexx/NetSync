using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

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
        public override void Serialize(Stream writer)
        {
            using (var bw = new BinaryWriter(new DisposableStream(writer, false), Encoding.UTF8))
            {
                bw.Write(value??string.Empty);
            }
        }
        public override void Deserialize(Stream reader)
        {
            using (var br = new BinaryReader(new DisposableStream(reader, false), Encoding.UTF8))
            {
                this.value = br.ReadString();
            }
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
        public override void Serialize(Stream writer)
        {
            writer.WriteByte(value);
        }
        public override void Deserialize(Stream reader)
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
        public override void Serialize(Stream writer)
        {
            using (var bw = new BinaryWriter(new DisposableStream(writer, false), Encoding.UTF8))
            {
                bw.Write(value);
            }
        }
        public override void Deserialize(Stream reader)
        {
            using (var br = new BinaryReader(new DisposableStream(reader, false), Encoding.UTF8))
            {
                this.value = br.ReadInt32();
            }
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
        public override void Serialize(Stream writer)
        {
            using (var bw = new BinaryWriter(new DisposableStream(writer, false), Encoding.UTF8))
            {
                bw.Write(value);
            }
        }
        public override void Deserialize(Stream reader)
        {
            using (var br = new BinaryReader(new DisposableStream(reader, false), Encoding.UTF8))
            {
                this.value = br.ReadInt64();
            }
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

        public override void Serialize(Stream writer)
        {
            using (var bw = new BinaryWriter(new DisposableStream(writer, false), Encoding.UTF8))
            {
                bw.Write(value);
            }
        }
        public override void Deserialize(Stream reader)
        {
            using (var br = new BinaryReader(new DisposableStream(reader, false), Encoding.UTF8))
            {
                this.value = br.ReadSingle();
            }
        }
    }
}
