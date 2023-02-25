using System;


namespace Yanmonet.NetSync.Messages
{

    public class BytesMessage : MessageBase
    {
        private byte[] bytes;
        public BytesMessage() { }
        public BytesMessage(byte[] bytes)
        {
            this.bytes = bytes;
        }

        public byte[] Bytes { get => bytes; }


        public override void Serialize(IReaderWriter writer)
        {
            int n = bytes.Length;
            writer.SerializeValue(ref bytes, 0, ref n);
        }

        public override void Deserialize(IReaderWriter reader)
        {
            int length = 0;
            bytes = null;
            reader.SerializeValue(ref bytes, 0, ref length);
        }

    }

}
