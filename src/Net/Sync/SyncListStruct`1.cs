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

        protected override void SerializeItem(Stream writer, T item)
        {
            EnsureItemBuff(); 

            using (var bw = new BinaryWriter(new DisposableStream(writer, false), Encoding.UTF8))
            {
                item.ToBytes(itemBuff);
                bw.Write(itemBuff, 0, itemBuff.Length);
            }
        }

        protected override T DeserializeItem(Stream reader)
        {

            EnsureItemBuff();

            using (var br = new BinaryReader(new DisposableStream(reader, false), Encoding.UTF8))
            {

                br.Read(itemBuff, 0, itemBuff.Length);
                var item = itemBuff.ToStruct<T>();
                return item;
            }
        }


    }
}
