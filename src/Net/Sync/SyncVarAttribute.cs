using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Net
{

    public class SyncVarAttribute : Attribute
    {
        public uint Bits { get; set; }

        public string ChangeCallback { get; set; }

        
    }


}
