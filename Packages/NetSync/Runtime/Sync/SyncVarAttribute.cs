using System;

namespace Yanmonet.NetSync
{

    public class SyncVarAttribute : Attribute
    {
        public uint Bits { get; set; }

        public string ChangeCallback { get; set; }

        
    }


}
