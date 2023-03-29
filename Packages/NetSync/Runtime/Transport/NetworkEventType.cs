using System.Collections;
using System.Collections.Generic;

namespace Yanmonet.NetSync
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
