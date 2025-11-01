using System.Collections;
using System.Collections.Generic;


namespace Unity.Network.Sync
{
    public interface INetworkSerializable
    {
        public void NetworkSerialize(IReaderWriter readerWriter);
    }
}