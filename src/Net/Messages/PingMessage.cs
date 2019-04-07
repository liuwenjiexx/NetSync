using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Net.Messages
{
    public class PingMessage : MessageBase
    {
        private byte action;
        private long timestamp;
        private long replyTimestamp;

        public const byte Action_Ping = 1;
        /// <summary>
        /// Ping回复
        /// </summary> 
        public const byte Action_Reply = 2;


        public byte Action { get => action; set => action = value; }

        public long Timestamp { get => timestamp; set => timestamp = value; }

        public long ReplyTimestamp { get => replyTimestamp; set => replyTimestamp = value; }

        public static PingMessage Ping(long timestamp)
        {
            return new PingMessage()
            {
                action = Action_Ping,
                timestamp = timestamp
            };
        }

        public static PingMessage Reply(PingMessage msg, long timestamp)
        {
            return new PingMessage()
            {
                action = Action_Reply,
                timestamp = msg.timestamp,
                replyTimestamp = timestamp
            };
        }

        public override void Serialize(Stream writer)
        {
            using (var w = new BinaryWriter(new DisposableStream(writer, false), Encoding.UTF8))
            {
                w.Write(action);
                switch (action)
                {
                    case Action_Ping:
                        w.Write(timestamp);
                        break;
                    case Action_Reply:
                        w.Write(timestamp);
                        w.Write(replyTimestamp);
                        break;
                }
            }
        }
        public override void Deserialize(Stream reader)
        {
            using (var r = new BinaryReader(new DisposableStream(reader, false), Encoding.UTF8))
            {
                action = r.ReadByte();
                switch (action)
                {
                    case Action_Ping:
                        timestamp = r.ReadInt64();
                        break;
                    case Action_Reply:
                        timestamp = r.ReadInt64();
                        replyTimestamp = r.ReadInt64();
                        break;
                }
            }
        }
    }

}
