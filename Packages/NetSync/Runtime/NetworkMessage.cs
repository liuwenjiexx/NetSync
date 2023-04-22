using System;

namespace Yanmonet.NetSync
{


    public class NetworkMessage
    {
        private ushort msgId;
        private NetworkReader reader;

        public byte[] rawPacket;

        public NetworkMessage()
        {
        }
        public ushort MsgId { get => msgId; set => msgId = value; }

        public NetworkManager NetworkManager;
        public ulong ClientId; 
        public ulong ReceiverId;
        public NetworkReader Reader { get => reader; set => reader = value; }

        public TMsg ReadMessage<TMsg>()
            where TMsg : MessageBase, new()
        {
            TMsg msg = new TMsg();
            msg.Deserialize(Reader);
            return msg;
        }
        public void ReadMessage(MessageBase msg)
        {
            msg.Deserialize(Reader);
        }

    }



    public delegate void NetworkMessageDelegate(NetworkMessage netMsg);


}