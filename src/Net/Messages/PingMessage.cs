using System;


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

        public override void Serialize(NetworkWriter writer)
        {
            writer.WriteByte(action);
            switch (action)
            {
                case Action_Ping:
                    writer.WriteInt64(timestamp);
                    break;
                case Action_Reply:
                    writer.WriteInt64(timestamp);
                    writer.WriteInt64(replyTimestamp);
                    break;
            }
        }
        public override void Deserialize(NetworkReader reader)
        {
            action = reader.ReadByte();
            switch (action)
            {
                case Action_Ping:
                    timestamp = reader.ReadInt64();
                    break;
                case Action_Reply:
                    timestamp = reader.ReadInt64();
                    replyTimestamp = reader.ReadInt64();
                    break;
            }

        }
    }

}
