using System.Collections;
using System.Collections.Generic;



namespace Yanmonet.NetSync.Messages
{
    public class ChangeOwnerMessage : MessageBase
    {
        public ulong objectId;
        public ulong ownerClientId;

    }
}
