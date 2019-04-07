using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Net
{
    public class SyncListStruct<T> : SyncList<T>
        where T : struct
    {

        private byte[] itemBuff;

        private void EnsureItemBuff()
        {
            if (itemBuff == null)
            {
                int itemSize = Marshal.SizeOf(typeof(T));
                itemBuff = new byte[itemSize];
            }
        }

        protected override void SerializeItem(NetworkWriter writer, T item)
        {
            EnsureItemBuff();

            item.ToBytes(itemBuff);
            writer.Write(itemBuff, 0, itemBuff.Length);
        }

        protected override T DeserializeItem(NetworkReader reader)
        {

            EnsureItemBuff();

            reader.Read(itemBuff, 0, itemBuff.Length);
            var item = itemBuff.ToStruct<T>();
            return item;
        }


    }
}
