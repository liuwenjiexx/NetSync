﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Yanmonet.NetSync.Messages
{
    internal class ConnectMessage : MessageBase
    {
        public ulong connectionId;
        public bool toServer;
        public MessageBase extra;

        public override void Serialize(IReaderWriter writer)
        {
            writer.SerializeValue(ref connectionId);
            writer.SerializeValue(ref toServer);
            if (extra != null)
                extra.Serialize(writer);
        }
        public override void Deserialize(IReaderWriter reader)
        {
            reader.SerializeValue(ref connectionId);
            reader.SerializeValue(ref toServer);
        }

    }
}
