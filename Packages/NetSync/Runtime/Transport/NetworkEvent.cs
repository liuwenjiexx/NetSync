using System;

namespace Unity.Network.Transport
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
