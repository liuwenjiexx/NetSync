using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Yanmonet.Network.Sync.Messages
{
    internal class ConnectRequestMessage : MessageBase
    {
        public int Version;
        public byte[] Payload;

        public override void Serialize(IReaderWriter writer)
        {
            writer.SerializeValue(ref Version);
            if (Payload == null)
                Payload = new byte[0];
            int length = Payload.Length;
            writer.SerializeValue(ref Payload, 0, ref length);
        }
        public override void Deserialize(IReaderWriter reader)
        {
            reader.SerializeValue(ref Version);
            Payload = null;
            int length = 0;
            reader.SerializeValue(ref Payload, 0, ref length);
        }

    }

    internal class ConnectResponseMessage : MessageBase
    {
        public ulong clientId;
        public byte[] data;
        public bool Success;
        public string Reson;

        public override void Serialize(IReaderWriter writer)
        {
            writer.SerializeValue(ref Success);
            writer.SerializeValue(ref clientId);
            writer.SerializeValue(ref Reson);
            if (data == null)
                data = new byte[0];
            int length = 0;
            writer.SerializeValue(ref data, 0, ref length);

        }
        public override void Deserialize(IReaderWriter reader)
        {
            reader.SerializeValue(ref Success);
            reader.SerializeValue(ref clientId);
            reader.SerializeValue(ref Reson);
            int length = 0;
            data = null;
            reader.SerializeValue(ref data, 0, ref length);
        }

    }
}
