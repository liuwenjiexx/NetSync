using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Yanmonet.NetSync.Messages
{
    internal class DestroyObjectMessage : MessageBase
    {
        public ulong instanceId;
        public override void Deserialize(IReaderWriter reader)
        {
            reader.SerializeValue(ref instanceId);
        }
        public override void Serialize(IReaderWriter writer)
        {
            writer.SerializeValue(ref instanceId);
        }
    }
}
