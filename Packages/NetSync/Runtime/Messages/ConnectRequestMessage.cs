using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Yanmonet.NetSync.Messages
{
    internal class ConnectRequestMessage : MessageBase
    {
        public int Version;
        public byte[] data;

        public override void Serialize(IReaderWriter writer)
        {
            writer.SerializeValue(ref Version);
            if (data == null)
                data = new byte[0];
            int length = data.Length;
            writer.SerializeValue(ref data, 0, ref length);
        }
        public override void Deserialize(IReaderWriter reader)
        {
            reader.SerializeValue(ref Version);
            data = null;
            int length = 0;
            reader.SerializeValue(ref data, 0, ref length);
        }

    }

    internal class ConnectResponseMessage : MessageBase
    {
        public ulong ownerClientId;
        public byte[] data;

        public override void Serialize(IReaderWriter writer)
        {
            writer.SerializeValue(ref ownerClientId);
            if (data == null)
                data = new byte[0];
            int length = 0;
            writer.SerializeValue(ref data, 0, ref length);

        }
        public override void Deserialize(IReaderWriter reader)
        {
            reader.SerializeValue(ref ownerClientId);
            int length = 0;
            data = null;
            reader.SerializeValue(ref data, 0, ref length);
        }

    }
}
