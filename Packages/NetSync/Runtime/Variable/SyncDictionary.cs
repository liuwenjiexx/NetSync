using System;
using System.Collections;
using System.Collections.Generic;

namespace Yanmonet.Network.Sync
{
    public class SyncDictionary<TKey, TValue> : SyncBase, IDictionary<TKey, TValue>, IReadOnlyDictionary<TKey, TValue>
    {

        private Dictionary<TKey, TValue> dic = new();
        private List<SyncDictionaryEvent<TKey, TValue>> dicEvents = new();


        public delegate void OnDictionaryChangedDelegate(SyncDictionaryEvent<TKey, TValue> changeEvent);

        public event OnDictionaryChangedDelegate OnDictionaryChanged;



        public SyncDictionary()
            : this(default, DefaultReadPermission, DefaultWritePermission)
        {
        }

        public SyncDictionary(
            SyncReadPermission readPermission = DefaultReadPermission,
            SyncWritePermission writePermission = DefaultWritePermission)
            : this(default, readPermission, writePermission)
        {
        }

        public SyncDictionary(
            IEnumerable<KeyValuePair<TKey, TValue>> items = default,
            SyncReadPermission readPermission = DefaultReadPermission,
            SyncWritePermission writePermission = DefaultWritePermission)
            : base(readPermission, writePermission)
        {
            if (items != null)
            {
                foreach (var item in items)
                {
                    dic[item.Key] = item.Value;
                }
            }
        }



        public TValue this[TKey key]
        {
            get => dic[key];
            set
            {
                CheckWrite();

                var dicEvent = new SyncDictionaryEvent<TKey, TValue>()
                {
                    Key = key,
                    Value = value
                };
                if (dic.ContainsKey(key))
                {
                    dicEvent.PreviousValue = dic[key];
                    dicEvent.Type = SyncDictionaryEvent<TKey, TValue>.EventType.Value;
                }
                else
                {
                    dicEvent.PreviousValue = default;
                    dicEvent.Type = SyncDictionaryEvent<TKey, TValue>.EventType.Add;
                }

                dic[key] = value;

                HandleDictionaryEvent(dicEvent);

            }
        }

        public ICollection<TKey> Keys => dic.Keys;

        public ICollection<TValue> Values => dic.Values;

        public int Count => dic.Count;

        public bool IsReadOnly => false;

        IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys => dic.Keys;

        IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values => dic.Values;


        public void Add(TKey key, TValue value)
        {
            CheckWrite();

            if (dic.ContainsKey(key))
                throw new InvalidOperationException($"Key already exists, key: '{key}'");

            dic.Add(key, value);

            var dicEvent = new SyncDictionaryEvent<TKey, TValue>()
            {
                Type = SyncDictionaryEvent<TKey, TValue>.EventType.Add,
                Key = key,
                Value = value
            };
            HandleDictionaryEvent(dicEvent);
        }

        public void Add(KeyValuePair<TKey, TValue> item) => Add(item.Key, item.Value);

        public void Clear()
        {
            CheckWrite();

            if (dic.Count == 0)
                return;

            dic.Clear();

            var dicEvent = new SyncDictionaryEvent<TKey, TValue>()
            {
                Type = SyncDictionaryEvent<TKey, TValue>.EventType.Clear
            };

            HandleDictionaryEvent(dicEvent);
        }

        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            foreach (var item2 in dic)
            {
                if (dic.Comparer != null)
                {
                    if (!dic.Comparer.Equals(item2.Key, item.Key))
                        continue;
                }
                else if (!Equals(item2.Key, item.Key))
                {
                    continue;
                }

                if (!object.Equals(item2.Value, item.Value))
                    continue;
                return true;
            }

            return false;
        }

        public bool ContainsKey(TKey key) => dic.ContainsKey(key);

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            int index = 0;
            foreach (var item in dic)
            {
                if (arrayIndex + index >= array.Length)
                    break;
                array[arrayIndex + index] = item;
                index++;
            }
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => dic.GetEnumerator();

        public bool Remove(TKey key)
        {
            CheckWrite();

            if (!dic.TryGetValue(key, out var value))
                return false;

            dic.Remove(key);

            var dicEvent = new SyncDictionaryEvent<TKey, TValue>()
            {
                Type = SyncDictionaryEvent<TKey, TValue>.EventType.Remove,
                Key = key,
                PreviousValue = value,
            };

            HandleDictionaryEvent(dicEvent);
            return true;
        }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            CheckWrite();

            if (!Contains(item))
                return false;

            dic.Remove(item.Key);

            var dicEvent = new SyncDictionaryEvent<TKey, TValue>()
            {
                Type = SyncDictionaryEvent<TKey, TValue>.EventType.Remove,
                Key = item.Key,
                PreviousValue = item.Value
            };

            HandleDictionaryEvent(dicEvent);
            return true;
        }

        public bool TryGetValue(TKey key, out TValue value) => dic.TryGetValue(key, out value);

        IEnumerator IEnumerable.GetEnumerator() => dic.GetEnumerator();


        private void HandleDictionaryEvent(SyncDictionaryEvent<TKey, TValue> dicEvent)
        {
            dicEvents.Add(dicEvent);

            MarkNetworkObjectDirty();
            OnDictionaryChanged?.Invoke(dicEvent);
        }

        public override bool IsDirty()
        {
            return base.IsDirty() || dicEvents.Count > 0;
        }

        public override void ResetDirty()
        {
            base.ResetDirty();
            if (dicEvents.Count > 0)
            {
                dicEvents.Clear();
            }
        }
        internal void MarkNetworkObjectDirty()
        {
            SetDirty(true);
        }


        #region Serialization


        public override void WriteDelta(IReaderWriter writer)
        {
            if (base.IsDirty())
            {
                ushort n = 1;
                writer.SerializeValue(ref n);
                var evt = SyncDictionaryEvent<TKey, TValue>.EventType.Full;
                writer.SerializeValue(ref evt);
                Write(writer);
                return;
            }

            var evtCount = (ushort)dicEvents.Count;
            writer.SerializeValue(ref evtCount);

            TKey key;
            TValue value;

            for (int i = 0; i < dicEvents.Count; i++)
            {
                var evt = dicEvents[i];
                var evtType = evt.Type;
                writer.SerializeValue(ref evtType);
                key = evt.Key;
                value = evt.Value;
                switch (evt.Type)
                {
                    case SyncDictionaryEvent<TKey, TValue>.EventType.Add:
                        {
                            writer.SerializeValue(ref key);
                            writer.SerializeValue(ref value);
                        }
                        break;
                    case SyncDictionaryEvent<TKey, TValue>.EventType.Remove:
                        {
                            writer.SerializeValue(ref key);
                            writer.SerializeValue(ref value);
                        }
                        break;
                    case SyncDictionaryEvent<TKey, TValue>.EventType.Value:
                        {
                            writer.SerializeValue(ref key);
                            writer.SerializeValue(ref value);
                        }
                        break;
                    case SyncDictionaryEvent<TKey, TValue>.EventType.Clear:
                        break;
                }
            }
        }

        public override void Write(IReaderWriter writer)
        {
            ushort count = (ushort)dic.Count;
            writer.SerializeValue(ref count);
            TKey key;
            TValue value;
            foreach (var item in dic)
            {
                key = item.Key;
                value = item.Value;
                writer.SerializeValue(ref key);
                writer.SerializeValue(ref value);
            }
        }



        public override void ReadDelta(IReaderWriter reader, bool keepDirtyDelta)
        {
            ushort deltaCount = default;
            SyncDictionaryEvent<TKey, TValue>.EventType eventType = default;

            reader.SerializeValue(ref deltaCount);
            TKey key;
            TValue value;
            for (int i = 0; i < deltaCount; i++)
            {
                reader.SerializeValue(ref eventType);

                switch (eventType)
                {
                    case SyncDictionaryEvent<TKey, TValue>.EventType.Add:
                        {
                            key = default;
                            reader.SerializeValue(ref key);
                            value = default;
                            reader.SerializeValue(ref value);

                            dic.Add(key, value);

                            if (OnDictionaryChanged != null)
                            {
                                OnDictionaryChanged(new SyncDictionaryEvent<TKey, TValue>
                                {
                                    Type = eventType,
                                    Key = key,
                                    Value = value
                                });
                            }

                            if (keepDirtyDelta)
                            {
                                dicEvents.Add(new SyncDictionaryEvent<TKey, TValue>
                                {
                                    Type = eventType,
                                    Key = key,
                                    Value = value
                                });
                                MarkNetworkObjectDirty();
                            }
                        }
                        break;
                    case SyncDictionaryEvent<TKey, TValue>.EventType.Remove:
                        {
                            key = default;
                            reader.SerializeValue(ref key);

                            if (!dic.TryGetValue(key, out value))
                            {
                                break;
                            }

                            dic.Remove(key);

                            if (OnDictionaryChanged != null)
                            {
                                OnDictionaryChanged(new SyncDictionaryEvent<TKey, TValue>
                                {
                                    Type = eventType,
                                    Key = key,
                                    Value = value
                                });
                            }

                            if (keepDirtyDelta)
                            {
                                dicEvents.Add(new SyncDictionaryEvent<TKey, TValue>
                                {
                                    Type = eventType,
                                    Key = key,
                                    Value = value
                                });
                                MarkNetworkObjectDirty();
                            }
                        }
                        break;
                    case SyncDictionaryEvent<TKey, TValue>.EventType.Value:
                        {
                            key = default;
                            reader.SerializeValue(ref key);
                            value = default;
                            reader.SerializeValue(ref value);

                            if (!dic.TryGetValue(key, out var previousValue))
                            {
                                break;
                            }

                            dic[key] = value;

                            if (OnDictionaryChanged != null)
                            {
                                OnDictionaryChanged(new SyncDictionaryEvent<TKey, TValue>
                                {
                                    Type = eventType,
                                    Key = key,
                                    Value = value,
                                    PreviousValue = previousValue
                                });
                            }

                            if (keepDirtyDelta)
                            {
                                dicEvents.Add(new SyncDictionaryEvent<TKey, TValue>
                                {
                                    Type = eventType,
                                    Key = key,
                                    Value = value,
                                    PreviousValue = previousValue
                                });
                                MarkNetworkObjectDirty();
                            }
                        }
                        break;
                    case SyncDictionaryEvent<TKey, TValue>.EventType.Clear:
                        {
                            dic.Clear();

                            if (OnDictionaryChanged != null)
                            {
                                OnDictionaryChanged(new SyncDictionaryEvent<TKey, TValue>
                                {
                                    Type = eventType,
                                });
                            }

                            if (keepDirtyDelta)
                            {
                                dicEvents.Add(new SyncDictionaryEvent<TKey, TValue>
                                {
                                    Type = eventType
                                });
                                MarkNetworkObjectDirty();
                            }
                        }
                        break;
                    case SyncDictionaryEvent<TKey, TValue>.EventType.Full:
                        {
                            Read(reader);
                            ResetDirty();
                        }
                        break;
                }
            }
        }

        public override void Read(IReaderWriter reader)
        {
            dic.Clear();
            ushort count = 0;
            reader.SerializeValue(ref count);
            TKey key;
            TValue value;
            for (int i = 0; i < count; i++)
            {
                key = default;
                value = default;
                reader.SerializeValue(ref key);
                reader.SerializeValue(ref value);
                dic.Add(key, value);
            }
        }

        #endregion
    }

    public struct SyncDictionaryEvent<TKey, TValue>
    {
        public enum EventType : byte
        {
            Add,
            Remove,
            Value,
            Clear,
            Full
        }

        public EventType Type;

        public TKey Key;

        public TValue Value;

        public TValue PreviousValue;

    }

}
