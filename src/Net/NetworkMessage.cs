using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;

namespace Net
{


    public class NetworkMessage
    {
        private short msgId;
        private Stream reader;
        private NetworkConnection connection;


        public NetworkMessage()
        {
        }
        public short MsgId { get => msgId; set => msgId = value; }
        public NetworkConnection Connection { get => connection; set => connection = value; }
        public Stream Reader { get => reader; set => reader = value; }

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