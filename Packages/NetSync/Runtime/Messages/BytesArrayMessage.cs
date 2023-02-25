using System;

namespace Yanmonet.NetSync.Messages
{

    public class BytesArrayMessage : MessageBase
    {
        public BytesArrayMessage() { }
        public BytesArrayMessage(byte[][] bytes)
        {
            this.Array = bytes;
        }

        public byte[][] Array { get; set; }


        public override void Serialize(IReaderWriter writer)
        {
            int n;

            if (Array == null)
            {
                n = 0;
                writer.SerializeValue(ref n);
            }
            else
            {
                n = Array.Length;
                writer.SerializeValue(ref n);
                for (int i = 0; i < Array.Length; i++)
                {
                    byte[] bytes = Array[i];
                    int length = bytes.Length;
                    writer.SerializeValue(ref bytes, 0, ref length);
                }
            }
        }

        public override void Deserialize(IReaderWriter reader)
        {

            int count = 0;
            reader.SerializeValue(ref count);
            byte[][] array = new byte[count][];
            for (int i = 0; i < count; i++)
            {
                int length = 0;
                byte[] bytes = null;
                reader.SerializeValue(ref bytes, 0, ref length);
                Array[i] = bytes;
            }

        }


    }
}
