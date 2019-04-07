using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Net
{

    internal class LinkedDictionary<TKey, TValue>:IEnumerable<TValue>       
    {
        private LinkedList<TValue> list;
        private Dictionary<TKey, LinkedListNode<TValue>> dic;

        public LinkedDictionary()
        {
            this.list = new LinkedList<TValue>();
            this.dic = new Dictionary<TKey, LinkedListNode<TValue>>();
        }

        public TValue this[TKey key]
        {
            get => dic[key].Value;
            set => Add(key, value);
        }

        public LinkedList<TValue> LinkedList { get => list; }
        public Dictionary<TKey, LinkedListNode<TValue>> Dictionary { get => dic; }

        public LinkedListNode<TValue> Last { get => list.First; }
        public LinkedListNode<TValue> First { get => list.Last; }
        public int Count { get => list.Count; }

        public ICollection<TKey> Keys => dic.Keys;

        public ICollection<TValue> Values => list;

        public void Add(TKey key, TValue value)
        {
            LinkedListNode<TValue> node;

            Remove(key);

            node = list.AddLast(value);
            dic.Add(key, node);
        }


        public bool ContainsKey(TKey key)
        {
            return dic.ContainsKey(key);
        }
        public void Clear()
        {
            list.Clear();
            dic.Clear();
        }

        public void Remove(TKey key)
        {
            LinkedListNode<TValue> node;

            if (dic.TryGetValue(key, out node))
            {
                list.Remove(node);
                dic.Remove(key);
            }
        }

        public IEnumerator<TValue> GetEnumerator()
        {
            return list.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return list.GetEnumerator();
        }
    }
}
