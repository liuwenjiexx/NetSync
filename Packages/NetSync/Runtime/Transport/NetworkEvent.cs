using System;

namespace Yanmonet.NetSync
{

    public struct NetworkEvent
    {
        public NetworkEventType Type;
        public ulong ClientId;
        public ArraySegment<byte> Payload;
        public float ReceiveTime;
    }

}
