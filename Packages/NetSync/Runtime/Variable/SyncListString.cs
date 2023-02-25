using System;

namespace Yanmonet.NetSync
{
    public class SyncListString : SyncList<string>
    {
        protected override void SerializeItem(IReaderWriter writer, string item)
        {
            writer.SerializeValue(ref item);
        }
        protected override string DeserializeItem(IReaderWriter reader)
        {
            string str = null;
            reader.SerializeValue(ref str);
            return str;
        }

    }
}
