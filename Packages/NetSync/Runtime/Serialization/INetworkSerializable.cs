using System.Collections;
using System.Collections.Generic;


namespace Yanmonet.Network.Sync
{
    public interface INetworkSerializable
    {
        public void NetworkSerialize(IReaderWriter readerWriter);
    }
}