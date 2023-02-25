using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Yanmonet.NetSync.Messages
{
    internal class ConnectMessage : MessageBase
    {
        public ulong connectionId;
        public bool toServer;
        public MessageBase extra;

        public override void Serialize(NetworkWriter writer)
        {
            writer.WriteUInt64(connectionId);
            writer.WriteBool(toServer);
            if (extra != null)
                extra.Serialize(writer);
        }
        public override void Deserialize(NetworkReader reader)
        {
            connectionId = reader.ReadUInt64();
            toServer = reader.ReadBool();

        }

    }
}
