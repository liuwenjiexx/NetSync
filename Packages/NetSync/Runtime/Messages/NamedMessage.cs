using System.Collections;
using System.Collections.Generic;
using Yanmonet.Network.Sync;

namespace Yanmonet.Network.Sync.Messages
{
    internal class NamedMessage : MessageBase
    {
        public uint nameHash;
        public INetworkSerializable data;

        public override void Serialize(IReaderWriter writer)
        {
            writer.SerializeValue(ref nameHash);
            writer.SerializeValue(ref data);
        }

        public override void Deserialize(IReaderWriter reader)
        {
            reader.SerializeValue(ref nameHash);
        }
    }
}