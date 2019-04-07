using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Net.Messages
{

    public class BytesArrayMessage : MessageBase
    {
        public BytesArrayMessage() { }
        public BytesArrayMessage(byte[][] bytes)
        {
            this.Array = bytes;
        }

        public byte[][] Array { get; set; }


        public override void Serialize(Stream writer)
        {
            using (var bw = new BinaryWriter(new DisposableStream(writer, false), Encoding.UTF8))
            {
                if (Array == null)
                {
                    bw.Write((int)0);
                }
                else
                {
                    bw.Write((int)Array.Length);
                    for (int i = 0; i < Array.Length; i++)
                    {
                        byte[] bytes = Array[i];
                        int length = bytes.Length;
                        bw.Write(length);
                        bw.Write(bytes, 0, length);
                    }
                }
            }
        }

        public override void Deserialize(Stream reader)
        {
            using (var br = new BinaryReader(new DisposableStream(reader, false), Encoding.UTF8))
            {
                int count = br.ReadInt32();
                byte[][] array = new byte[count][];
                for (int i = 0; i < count; i++)
                {
                    int length = br.ReadInt32();
                    byte[] bytes = new byte[length];
                    br.Read(bytes, 0, length);
                    Array[i] = bytes;
                }
            }

        }


    }
}
