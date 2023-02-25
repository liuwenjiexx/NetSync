using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yanmonet.NetSync
{
    public class SyncListJsonString<T> : SyncList<T>
    {

        protected override void SerializeItem(IReaderWriter writer, T item)
        {
            string str = null;
            if (item != null)
            {
                str = MessageBase.SerializeToString(item);
            }
            writer.SerializeValue(ref str);
        }
        protected override T DeserializeItem(IReaderWriter reader)
        {
            string str = null;
            reader.SerializeValue(ref str);
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
