using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yanmonet.NetSync
{
    public class SyncListJsonString<T> : SyncList<T>
    {

        protected override void SerializeItem(NetworkWriter writer, T item)
        {
            string str = null;
            if (item != null)
            {
                str = MessageBase.SerializeToString(item);
            }
            writer.WriteString(str);
        }
        protected override T DeserializeItem(NetworkReader reader)
        {
            string str = reader.ReadString();
            if (string.IsNullOrEmpty(str))
            {
                return default;
            }
            var msg = Activator.CreateInstance(typeof(T));
            MessageBase.DeserializeFromString(str, msg);
            return (T)msg;
        }
    }
}
