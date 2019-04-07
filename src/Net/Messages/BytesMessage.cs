using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Net.Messages
{

    public class BytesMessage : MessageBase
    {
        public BytesMessage() { }
        public BytesMessage(byte[] bytes)
        {
            this.Bytes = bytes;
        }

        public byte[] Bytes { get; set; }


        public override void Serialize(Stream writer)
        {
            using (var bw = new BinaryWriter(new DisposableStream(writer, false), Encoding.UTF8))
            {
                if (Bytes == null)
                {
                    bw.Write((int)0);
                }
                else
                {
                    int length = Bytes.Length;
                    bw.Write(length);
                    bw.Write(Bytes, 0, length);
                }
            }
        }

        public override void Deserialize(Stream reader)
        {
            using (var br = new BinaryReader(new DisposableStream(reader, false), Encoding.UTF8))
            {
                int length = br.ReadInt32();
                byte[] bytes = new byte[length];
                br.Read(bytes, 0, length);
                Bytes = bytes;
            }

        }

    }

}
