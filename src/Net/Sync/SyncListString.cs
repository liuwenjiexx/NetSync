using System;

namespace Net
{
    public class SyncListString : SyncList<string>
    {
        protected override void SerializeItem(NetworkWriter writer, string item)
        {
            writer.WriteString(item ?? string.Empty);
        }
        protected override string DeserializeItem(NetworkReader reader)
        {
            string str = reader.ReadString();
            return str;
        }

    }
}
