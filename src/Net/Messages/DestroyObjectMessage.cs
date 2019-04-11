using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Net.Messages
{
    internal class DestroyObjectMessage : MessageBase
    {
        public NetworkInstanceId instanceId;
    }
}
