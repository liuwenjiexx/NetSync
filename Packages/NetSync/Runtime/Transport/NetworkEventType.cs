using System.Collections;
using System.Collections.Generic;

namespace Yanmonet.Network.Transport
{
    public enum NetworkEventType
    {
        None = 0,
        Connect,
        Disconnect,
        Data,
        Error,
    }
}
