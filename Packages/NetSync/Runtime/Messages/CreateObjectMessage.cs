using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Yanmonet.NetSync.Messages
{

    internal class CreateObjectMessage : MessageBase
    {
        public bool toServer;
        public NetworkObjectId objectId;
        public NetworkInstanceId instanceId;
        public MessageBase parameter;

        public override void Serialize(NetworkWriter writer)
        {
            writer.WriteBool(toServer);
            writer.WriteNetworkObjectId(objectId);
            writer.WriteNetworkInstanceId(instanceId);
            if (parameter != null)
                parameter.Serialize(writer);
        }
        public override void Deserialize(NetworkReader reader)
        {
            toServer = reader.ReadBool();
            objectId = reader.ReadNetworkObjectId();
            instanceId = reader.ReadNetworkInstanceId();

        }

    }
}
