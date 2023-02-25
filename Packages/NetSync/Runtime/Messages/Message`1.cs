﻿using System;


namespace Yanmonet.NetSync.Messages
{

    public class Message<T> : MessageBase
    {
        public Message()
        {
        }

        public Message(T value)
        {
            this.Value = value;
        }

        public T Value { get; set; }
    }

}
