using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Net.Messages
{
    internal class ConnectMessage : MessageBase
    {
        public int connectionId;
        public bool toServer;
        public MessageBase extra;

        public override void Serialize(NetworkWriter writer)
        {
            writer.WriteInt32(connectionId);
            writer.WriteBool(toServer);
            if (extra != null)
                extra.Serialize(writer);
        }
        public override void Deserialize(NetworkReader reader)
        {
            connectionId = reader.ReadInt32();
            toServer = reader.ReadBool();

        }

    }
}
