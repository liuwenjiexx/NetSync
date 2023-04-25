using System;

namespace Yanmonet.NetSync
{

    public struct NetworkEvent
    {
        public NetworkEventType Type;

        public ulong ClientId;
        public ulong SenderClientId;
        public ArraySegment<byte> Payload;
        public float ReceiveTime;
    }

}
