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

        public override void Serialize(IReaderWriter writer)
        {
            writer.SerializeValue(ref toServer);
            writer.SerializeValue(ref objectId);
            writer.SerializeValue(ref instanceId);
            if (parameter != null)
                parameter.Serialize(writer);
        }
        public override void Deserialize(IReaderWriter reader)
        {
            reader.SerializeValue(ref toServer);
            reader.SerializeValue(ref objectId);
            reader.SerializeValue(ref instanceId);

        }

    }
}
