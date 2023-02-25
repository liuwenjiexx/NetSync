using System.Collections;
using System.Collections.Generic;


namespace Yanmonet.NetSync
{
    public interface INetworkSerializable
    {
        public void NetworkSerialize(IReaderWriter readerWriter);
    }
}