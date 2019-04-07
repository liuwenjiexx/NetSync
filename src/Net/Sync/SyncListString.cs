using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Net
{
    public class SyncListString : SyncList<string>
    {
        protected override void SerializeItem(Stream writer, string item)
        {
            using (var bw = new BinaryWriter(new DisposableStream(writer, false), Encoding.UTF8))
            {
                bw.Write(item ?? string.Empty);
            }
        }
        protected override string DeserializeItem(Stream reader)
        {
            using (var br = new BinaryReader(new DisposableStream(reader, false), Encoding.UTF8))
            {
                string str = br.ReadString();
                return str;
            }
        }

    }
}
