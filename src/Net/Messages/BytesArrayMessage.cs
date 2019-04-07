using System;

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


        public override void Serialize(NetworkWriter writer)
        {

            if (Array == null)
            {
                writer.WriteInt32((int)0);
            }
            else
            {
                writer.WriteInt32((int)Array.Length);
                for (int i = 0; i < Array.Length; i++)
                {
                    byte[] bytes = Array[i];
                    int length = bytes.Length;
                    writer.WriteInt32(length);
                    writer.Write(bytes, 0, length);
                }
            }
        }

        public override void Deserialize(NetworkReader reader)
        {

            int count = reader.ReadInt32();
            byte[][] array = new byte[count][];
            for (int i = 0; i < count; i++)
            {
                int length = reader.ReadInt32();
                byte[] bytes = new byte[length];
                reader.Read(bytes, 0, length);
                Array[i] = bytes;
            }

        }


    }
}
