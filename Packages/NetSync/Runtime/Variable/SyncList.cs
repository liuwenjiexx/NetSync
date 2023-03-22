using System;
using System.Collections;
using System.Collections.Generic;


namespace Yanmonet.NetSync
{
    public class SyncList<T> : SyncBase, IList<T>, IReadOnlyList<T>
    {
        private List<T> list = new();
        private List<SyncListEvent<T>> listEvents = new();


        public delegate void OnListChangedDelegate(SyncListEvent<T> changeEvent);

        public event OnListChangedDelegate OnListChanged;

        public SyncList()
            : this(default, DefaultReadPermission, DefaultWritePermission)
        {
        }

        public SyncList(
            SyncReadPermission readPermission = DefaultReadPermission,
            SyncWritePermission writePermission = DefaultWritePermission)
            : this(default, readPermission, writePermission)
        {
        }

        public SyncList(
            IEnumerable<T> values = default,
            SyncReadPermission readPermission = DefaultReadPermission,
            SyncWritePermission writePermission = DefaultWritePermission)
            : base(readPermission, writePermission)
        {
            if (values != null)
            {
                foreach (var value in values)
                {
                    list.Add(value);
                }
            }
        }

        public T this[int index]
        {

            get => list[index];
            set
            {
                CheckWrite();

                var previousValue = list[index];
                list[index] = value;

                var listEvent = new SyncListEvent<T>()
                {
                    Type = SyncListEvent<T>.EventType.Value,
                    Index = index,
                    Value = value,
                    PreviousValue = previousValue
                };

                HandleAddListEvent(listEvent);
            }
        }

        public int Count => list.Count;

        public bool IsReadOnly => false;


        public void Add(T item)
        {
            CheckWrite();

            list.Add(item);

            var listEvent = new SyncListEvent<T>()
            {
                Type = SyncListEvent<T>.EventType.Add,
                Value = item,
                Index = list.Count - 1
            };

            HandleAddListEvent(listEvent);
        }

        public void Clear()
        {
            CheckWrite();

            if (list.Count == 0)
                return;

            list.Clear();

            var listEvent = new SyncListEvent<T>()
            {
                Type = SyncListEvent<T>.EventType.Clear
            };

            HandleAddListEvent(listEvent);
        }

        public bool Contains(T item)
        {
            int index = list.IndexOf(item);
            return index != -1;
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            for (int i = 0; i < list.Count; i++)
            {
                int destIndex = arrayIndex + i;
                if (destIndex >= array.Length)
                    break;
                array[destIndex] = list[i];
            }
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
            CheckWrite();

            if (index < list.Count)
            {
                list.Insert(index, item);
            }
            else
            {
                list.Add(item);
            }

            var listEvent = new SyncListEvent<T>()
            {
                Type = SyncListEvent<T>.EventType.Insert,
                Index = index,
                Value = item
            };

            HandleAddListEvent(listEvent);
        }

        public bool Remove(T item)
        {

            CheckWrite();

            int index = list.IndexOf(item);
            if (index == -1)
            {
                return false;
            }

            list.RemoveAt(index);
            var listEvent = new SyncListEvent<T>()
            {
                Type = SyncListEvent<T>.EventType.Remove,
                Value = item
            };

            HandleAddListEvent(listEvent);
            return true;
        }

        public void RemoveAt(int index)
        {
            CheckWrite();

            list.RemoveAt(index);

            var listEvent = new SyncListEvent<T>()
            {
                Type = SyncListEvent<T>.EventType.RemoveAt,
                Index = index
            };

            HandleAddListEvent(listEvent);
        }
        private void HandleAddListEvent(SyncListEvent<T> listEvent)
        {
            listEvents.Add(listEvent);

            MarkNetworkObjectDirty();
            OnListChanged?.Invoke(listEvent);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return list.GetEnumerator();
        }

        public override bool IsDirty()
        {
            return base.IsDirty() || listEvents.Count > 0;
        }

        public override void ResetDirty()
        {
            base.ResetDirty();
            if (listEvents.Count > 0)
            {
                listEvents.Clear();
            }
        }



        internal void MarkNetworkObjectDirty()
        {
            SetDirty(true);
            //if (NetworkObject == null)
            //{
            //    Debug.LogWarning($"Sync Variable is written to, but doesn't know its NetworkObject yet. " +
            //                     "Are you modifying a Sync Variable before the NetworkObject is spawned?");
            //    return;
            //}

            //NetworkObject.NetworkManager.MarkNetworkObjectDirty(NetworkObject.NetworkObject);
        }


        public override void ReadDelta(IReaderWriter reader, bool keepDirtyDelta)
        {
            ushort deltaCount = default;
            SyncListEvent<T>.EventType eventType = default;

            reader.SerializeValue(ref deltaCount);

            T value;

            for (int i = 0; i < deltaCount; i++)
            {
                reader.SerializeValue(ref eventType);

                switch (eventType)
                {
                    case SyncListEvent<T>.EventType.Add:
                        {
                              value = default;
                            reader.SerializeValue(ref value);
                            list.Add(value);

                            if (OnListChanged != null)
                            {
                                OnListChanged(new SyncListEvent<T>
                                {
                                    Type = eventType,
                                    Index = list.Count - 1,
                                    Value = list[list.Count - 1]
                                });
                            }

                            if (keepDirtyDelta)
                            {
                                listEvents.Add(new SyncListEvent<T>()
                                {
                                    Type = eventType,
                                    Index = list.Count - 1,
                                    Value = list[list.Count - 1]
                                });
                                MarkNetworkObjectDirty();
                            }
                        }
                        break;
                    case SyncListEvent<T>.EventType.Insert:
                        {
                            int index = 0;
                            reader.SerializeValue(ref index);
                             value = default;
                            reader.SerializeValue(ref value);

                            if (index < list.Count)
                            {
                                list.Insert(index, value);
                            }
                            else
                            {
                                list.Add(value);
                            }

                            if (OnListChanged != null)
                            {
                                OnListChanged(new SyncListEvent<T>
                                {
                                    Type = eventType,
                                    Index = index,
                                    Value = list[index]
                                });
                            }

                            if (keepDirtyDelta)
                            {
                                listEvents.Add(new SyncListEvent<T>()
                                {
                                    Type = eventType,
                                    Index = index,
                                    Value = list[index]
                                });
                                MarkNetworkObjectDirty();
                            }
                        }
                        break;
                    case SyncListEvent<T>.EventType.Remove:
                        {
                            value = default;
                            reader.SerializeValue(ref value);
                            int index = list.IndexOf(value);
                            if (index == -1)
                            {
                                break;
                            }

                            list.RemoveAt(index);

                            if (OnListChanged != null)
                            {
                                OnListChanged(new SyncListEvent<T>
                                {
                                    Type = eventType,
                                    Index = index,
                                    Value = value
                                });
                            }

                            if (keepDirtyDelta)
                            {
                                listEvents.Add(new SyncListEvent<T>()
                                {
                                    Type = eventType,
                                    Index = index,
                                    Value = value
                                });
                                MarkNetworkObjectDirty();
                            }
                        }
                        break;
                    case SyncListEvent<T>.EventType.RemoveAt:
                        {
                            int index = 0;
                            reader.SerializeValue(ref index);
                              value = list[index];
                            list.RemoveAt(index);

                            if (OnListChanged != null)
                            {
                                OnListChanged(new SyncListEvent<T>
                                {
                                    Type = eventType,
                                    Index = index,
                                    Value = value
                                });
                            }

                            if (keepDirtyDelta)
                            {
                                listEvents.Add(new SyncListEvent<T>()
                                {
                                    Type = eventType,
                                    Index = index,
                                    Value = value
                                });
                                MarkNetworkObjectDirty();
                            }
                        }
                        break;
                    case SyncListEvent<T>.EventType.Value:
                        {
                            int index = 0;
                            reader.SerializeValue(ref index);
                            value = default;
                            reader.SerializeValue<T>(ref value);
                            if (index >= list.Count)
                            {
                                throw new Exception("Shouldn't be here, index is higher than list length");
                            }

                            var previousValue = list[index];
                            list[index] = value;

                            if (OnListChanged != null)
                            {
                                OnListChanged(new SyncListEvent<T>
                                {
                                    Type = eventType,
                                    Index = index,
                                    Value = value,
                                    PreviousValue = previousValue
                                });
                            }

                            if (keepDirtyDelta)
                            {
                                listEvents.Add(new SyncListEvent<T>()
                                {
                                    Type = eventType,
                                    Index = index,
                                    Value = value,
                                    PreviousValue = previousValue
                                });
                                MarkNetworkObjectDirty();
                            }
                        }
                        break;
                    case SyncListEvent<T>.EventType.Clear:
                        {
                            list.Clear();

                            if (OnListChanged != null)
                            {
                                OnListChanged(new SyncListEvent<T>
                                {
                                    Type = eventType,
                                });
                            }

                            if (keepDirtyDelta)
                            {
                                listEvents.Add(new SyncListEvent<T>()
                                {
                                    Type = eventType
                                });
                                MarkNetworkObjectDirty();
                            }
                        }
                        break;
                    case SyncListEvent<T>.EventType.Full:
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
            list.Clear();
            ushort count = 0;
            reader.SerializeValue(ref count);
            T value;
            for (int i = 0; i < count; i++)
            {
                value = default;
                reader.SerializeValue(ref value);
                list.Add(value);
            }
        }

        public override void WriteDelta(IReaderWriter writer)
        {
            if (base.IsDirty())
            {
                ushort n = 1;
                writer.SerializeValue(ref n);
                var t = SyncListEvent<T>.EventType.Full;
                writer.SerializeValue(ref t);
                Write(writer);

                return;
            }
            var evtCount = (ushort)listEvents.Count;
            writer.SerializeValue(ref evtCount);
            for (int i = 0; i < listEvents.Count; i++)
            {
                var evt = listEvents[i];
                var evtType = evt.Type;
                writer.SerializeValue(ref evtType);
                int index;
                T value;
                index = evt.Index;
                value = evt.Value;
                switch (evt.Type)
                {
                    case SyncListEvent<T>.EventType.Add:
                        {

                            writer.SerializeValue(ref value);
                        }
                        break;
                    case SyncListEvent<T>.EventType.Insert:
                        {
                            writer.SerializeValue(ref index);
                            writer.SerializeValue(ref value);
                        }
                        break;
                    case SyncListEvent<T>.EventType.Remove:
                        {
                            writer.SerializeValue(ref value);
                        }
                        break;
                    case SyncListEvent<T>.EventType.RemoveAt:
                        {
                            writer.SerializeValue(ref index);
                        }
                        break;
                    case SyncListEvent<T>.EventType.Value:
                        {
                            writer.SerializeValue(ref index);
                            writer.SerializeValue(ref value);
                        }
                        break;
                    case SyncListEvent<T>.EventType.Clear:

                        break;
                }
            }
        }

        public override void Write(IReaderWriter writer)
        {
            var n = (ushort)list.Count;
            writer.SerializeValue(ref n);
            for (int i = 0; i < list.Count; i++)
            {
                var item = list[i];
                writer.SerializeValue(ref item);
            }
        }

    }


    public struct SyncListEvent<T>
    {
        public enum EventType : byte
        {
            Add,
            Insert,
            Remove,
            RemoveAt,
            Value,
            Clear,
            Full
        }

        public EventType Type;

        public T Value;

        public T PreviousValue;

        public int Index;
    }
}
