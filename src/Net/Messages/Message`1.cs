using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Net
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
