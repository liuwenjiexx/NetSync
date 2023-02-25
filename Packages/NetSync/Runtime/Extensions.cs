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

        public static void SerializeValue(this IReaderWriter reader, ref NetworkInstanceId value)
        {
            uint n = value.Value;
            reader.SerializeValue(ref n);
            if (reader.IsReader)
            {
                var value2 = new NetworkInstanceId();
                value2.Value = n;
                value = value2;
            }
        }

        public static void SerializeValue(this IReaderWriter reader, ref NetworkObjectId value)
        {
            Guid guid = value.Value;
            reader.SerializeValue(ref guid);
            if (reader.IsReader)
            {
                value.Value = guid;
            }
        }
    }
}