using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Instrumentation;
using System.Text;

namespace Yanmonet.NetSync.Messages
{

    internal class CreateObjectMessage : MessageBase
    {
        public uint typeId;
        public ulong objectId;
        public ulong ownerClientId;

        public override void Serialize(IReaderWriter writer)
        {
            writer.SerializeValue(ref typeId);
            writer.SerializeValue(ref objectId);
            writer.SerializeValue(ref ownerClientId);
        }

        public override void Deserialize(IReaderWriter reader)
        {
            reader.SerializeValue(ref typeId);
            reader.SerializeValue(ref objectId);
            reader.SerializeValue(ref ownerClientId);
        }

    }

    internal class SpawnMessage : MessageBase
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

    internal class DespawnMessage : MessageBase
    {
        public ulong instanceId;

        public override void Serialize(IReaderWriter writer)
        {
            writer.SerializeValue(ref instanceId);
        }

        public override void Deserialize(IReaderWriter reader)
        {
            reader.SerializeValue(ref instanceId);
        }
    }
}
