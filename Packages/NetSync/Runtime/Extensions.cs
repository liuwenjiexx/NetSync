using System;
using System.Collections;
using System.Collections.Generic;

namespace Yanmonet.NetSync
{
    public static class Extensions2
    {

        public static void SerializeValue(this NetworkReader reader, ref Guid value)
        {
            value = reader.ReadGuid();
        }
        public static void SerializeValue(this NetworkWriter writer, ref Guid value)
        {
            writer.WriteGuid(value);
        }

 

     
    }
}