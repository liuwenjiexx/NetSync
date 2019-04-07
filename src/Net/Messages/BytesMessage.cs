using System;


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


        public override void Serialize(NetworkWriter writer)
        {
            if (Bytes == null)
            {
                writer.WriteInt32((int)0);
            }
            else
            {
                int length = Bytes.Length;
                writer.WriteInt32(length);
                writer.Write(Bytes, 0, length);
            }
        }

        public override void Deserialize(NetworkReader reader)
        {
            int length = reader.ReadInt32();
            byte[] bytes = new byte[length];
            reader.Read(bytes, 0, length);
            Bytes = bytes;
        }

    }

}
