using System;

namespace Yanmonet.NetSync
{

    public struct NetworkEvent
    {
        public NetworkEventType Type;

        public ulong SenderId;
        public ArraySegment<byte> Payload;
        public float ReceiveTime;
    }

}
