using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Net
{
    public abstract class SyncList<T> : IList<T>
    {
        private List<T> list;
        private NetworkObject owner;
        private SyncListInfo info;

        public SyncList()
        {
            list = new List<T>();
        }

        public T this[int index]
        {
            get => list[index];
            set
            {
                if (!object.Equals(list[index], value))
                {
                    list[index] = value;
                    OnChanged(Operation.Set, index);
                }
            }
        }

        public int Count => list.Count;

        public bool IsReadOnly => false;

        public event SyncListChanged Changed;

        public void Add(T item)
        {
            list.Add(item);
            OnChanged(Operation.Add, list.Count - 1);
        }

        public void Clear()
        {
            if (Count > 0)
            {
                list.Clear();
                OnChanged(Operation.Clear, -1);
            }
        }

        public bool Contains(T item)
        {
            return list.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            list.CopyTo(array, arrayIndex);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return list.GetEnumerator();
        }

        public int IndexOf(T item)
        {
            return list.IndexOf(item);
        }

        public void Insert(int index, T item)
        {
            list.Insert(index, item);
            OnChanged(Operation.Insert, index);
        }

        public bool Remove(T item)
        {
            int index = IndexOf(item);
            if (index < 0)
                return false;
            RemoveAt(index);
            return true;
        }

        public void RemoveAt(int index)
        {
            if (index < 0 || index >= Count)
                throw new ArgumentOutOfRangeException(nameof(index));
            list.RemoveAt(index);
            OnChanged(Operation.RemoveAt, index);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return list.GetEnumerator();
        }



        internal void Init(NetworkObject owner, SyncListInfo info)
        {
            this.owner = owner;
            this.info = info;
        }

        public void OnChanged(Operation op, int itemIndex)
        {
            if (owner != null)
            {
                SyncListMessage msg = null;
                switch (op)
                {
                    case Operation.Add:
                        msg = SyncListMessage.Add(owner, info.memberIndex, (byte)itemIndex);
                        break;
                    case Operation.Insert:
                        msg = SyncListMessage.Insert(owner, info.memberIndex, (byte)itemIndex);
                        break;
                    case Operation.Set:
                        msg = SyncListMessage.Set(owner, info.memberIndex, (byte)itemIndex);
                        break;
                    case Operation.Remove:
                        msg = SyncListMessage.Remove(owner, info.memberIndex, (byte)itemIndex);
                        break;
                    case Operation.RemoveAt:
                        msg = SyncListMessage.RemoveAt(owner, info.memberIndex, (byte)itemIndex);
                        break;
                    case Operation.Clear:
                        msg = SyncListMessage.Clear(owner, info.memberIndex);
                        break;
                }

                foreach (var conn in owner.Observers)
                    conn.SendMessage((short)NetworkMsgId.SyncList, msg);
            }
            Changed?.Invoke(op, itemIndex);
        }

        protected abstract T DeserializeItem(Stream reader);
        protected abstract void SerializeItem(Stream writer, T item);

        public enum Operation
        {
            Add = 0,
            Insert = 2,
            Set = 5,
            Remove = 3,
            RemoveAt = 4,
            Clear = 1,
            Dirty = 6
        }

        public delegate void SyncListChanged(Operation op, int itemIndex);

    }
}
