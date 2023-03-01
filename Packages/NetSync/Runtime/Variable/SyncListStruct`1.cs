//using System.Runtime.InteropServices;

//namespace Yanmonet.NetSync
//{
//    public class SyncListStruct<T> : SyncList<T>
//        where T : struct
//    {

//        private byte[] itemBuff;

//        private void EnsureItemBuff()
//        {
//            if (itemBuff == null)
//            {
//                int itemSize = Marshal.SizeOf(typeof(T));
//                itemBuff = new byte[itemSize];
//            }
//        }

//        protected override void SerializeItem(IReaderWriter writer, T item)
//        {
//            EnsureItemBuff();

//            item.ToBytes(itemBuff);
//            int n = itemBuff.Length;
//            writer.SerializeValue(ref itemBuff, 0, ref n);
//        }

//        protected override T DeserializeItem(IReaderWriter reader)
//        {

//            EnsureItemBuff();
//            itemBuff = null;
//            int n = 0;
//            reader.SerializeValue(ref itemBuff, 0, ref n);
//            var item = itemBuff.ToStruct<T>();
//            return item;
//        }


//    }
//}
