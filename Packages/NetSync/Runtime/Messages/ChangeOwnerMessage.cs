using System.Collections;
using System.Collections.Generic;



namespace Yanmonet.NetSync.Messages
{
    public class ChangeOwnerMessage : MessageBase
    {
        public ulong instanceId;
        public ulong ownerClientId;
        public override void Serialize(IReaderWriter writer)
        {
            writer.SerializeValue(ref instanceId);
            writer.SerializeValue(ref ownerClientId);
        }
        public override void Deserialize(IReaderWriter reader)
        {
            reader.SerializeValue(ref instanceId);
            reader.SerializeValue(ref ownerClientId);
        }
    }
}
