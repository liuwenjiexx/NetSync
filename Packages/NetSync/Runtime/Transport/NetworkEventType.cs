using System.Collections;
using System.Collections.Generic;

namespace Unity.Network.Transport
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
