using System;


namespace Yanmonet.NetSync.Messages
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

        public override void Serialize(IReaderWriter writer)
        {
            writer.SerializeValue(ref action);
            switch (action)
            {
                case Action_Ping:
                    writer.SerializeValue(ref timestamp);
                    break;
                case Action_Reply:
                    writer.SerializeValue(ref timestamp);
                    writer.SerializeValue(ref replyTimestamp);
                    break;
            }
        }
        public override void Deserialize(IReaderWriter reader)
        {
            reader.SerializeValue(ref action);
            switch (action)
            {
                case Action_Ping:
                    reader.SerializeValue(ref timestamp);
                    break;
                case Action_Reply:
                    reader.SerializeValue(ref timestamp);
                    reader.SerializeValue(ref replyTimestamp);
                    break;
            }

        }
    }

}
