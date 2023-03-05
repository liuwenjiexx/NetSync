using System;
using System.Collections;
using System.Collections.Generic;


namespace Yanmonet.NetSync
{
    public class NetworkListSerializable<T> : NetworkVariableBase, IList<T>, IReadOnlyList<T>
        where T : INetworkSerializable, new()
    {
        private List<T> list = new();
        private List<NetworkListEvent<T>> listEvents = new();


        public delegate void OnListChangedDelegate(NetworkListEvent<T> changeEvent);

        public event OnListChangedDelegate OnListChanged;

        public NetworkListSerializable()
        {
        }

        public NetworkListSerializable(NetworkVariableReadPermission readPermission = DefaultReadPermission,
            NetworkVariableWritePermission writePermission = DefaultWritePermission)
            : base(readPermission, writePermission)
        {
        }

        public NetworkListSerializable(IEnumerable<T> values = default,
            NetworkVariableReadPermission readPermission = DefaultReadPermission,
            NetworkVariableWritePermission writePermission = DefaultWritePermission)
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
                if (NetworkObject != null && !CanClientWrite(NetworkObject.NetworkManager.LocalClientId))
                {
                    throw new InvalidOperationException("Client is not allowed to write to this NetworkList");
                }

                var previousValue = list[index];
                list[index] = value;

                var listEvent = new NetworkListEvent<T>()
                {
                    Type = NetworkListEvent<T>.EventType.Value,
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

            if (NetworkObject != null && !CanClientWrite(NetworkObject.NetworkManager.LocalClientId))
            {
                throw new InvalidOperationException("Client is not allowed to write to this NetworkList");
            }

            list.Add(item);

            var listEvent = new NetworkListEvent<T>()
            {
                Type = NetworkListEvent<T>.EventType.Add,
                Value = item,
                Index = list.Count - 1
            };

            HandleAddListEvent(listEvent);
        }

        public void Clear()
        {
            if (NetworkObject != null && !CanClientWrite(NetworkObject.NetworkManager.LocalClientId))
            {
                throw new InvalidOperationException("Client is not allowed to write to this NetworkVariable");
            }

            if (list.Count == 0)
                return;

            list.Clear();

            var listEvent = new NetworkListEvent<T>()
            {
                Type = NetworkListEvent<T>.EventType.Clear
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
            if (NetworkObject != null && !CanClientWrite(NetworkObject.NetworkManager.LocalClientId))
            {
                throw new InvalidOperationException("Client is not allowed to write to this NetworkList");
            }

            if (index < list.Count)
            {
                list.Insert(index, item);
            }
            else
            {
                list.Add(item);
            }

            var listEvent = new NetworkListEvent<T>()
            {
                Type = NetworkListEvent<T>.EventType.Insert,
                Index = index,
                Value = item
            };

            HandleAddListEvent(listEvent);
        }

        public bool Remove(T item)
        {

            if (NetworkObject != null && !CanClientWrite(NetworkObject.NetworkManager.LocalClientId))
            {
                throw new InvalidOperationException("Client is not allowed to write to this NetworkList");
            }

            int index = list.IndexOf(item);
            if (index == -1)
            {
                return false;
            }

            list.RemoveAt(index);
            var listEvent = new NetworkListEvent<T>()
            {
                Type = NetworkListEvent<T>.EventType.Remove,
                Value = item
            };

            HandleAddListEvent(listEvent);
            return true;
        }

        public void RemoveAt(int index)
        {
            if (!CanClientWrite(NetworkObject.NetworkManager.LocalClientId))
            {
                throw new InvalidOperationException("Client is not allowed to write to this NetworkList");
            }

            list.RemoveAt(index);

            var listEvent = new NetworkListEvent<T>()
            {
                Type = NetworkListEvent<T>.EventType.RemoveAt,
                Index = index
            };

            HandleAddListEvent(listEvent);
        }
        private void HandleAddListEvent(NetworkListEvent<T> listEvent)
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
            //    Debug.LogWarning($"NetworkList is written to, but doesn't know its NetworkObject yet. " +
            //                     "Are you modifying a NetworkList before the NetworkObject is spawned?");
            //    return;
            //}

            //NetworkObject.NetworkManager.MarkNetworkObjectDirty(NetworkObject.NetworkObject);
        }


        public override void ReadDelta(IReaderWriter reader, bool keepDirtyDelta)
        {
            ushort deltaCount = default;
            NetworkListEvent<T>.EventType eventType = default;

            reader.SerializeValue(ref deltaCount);

            for (int i = 0; i < deltaCount; i++)
            {
                reader.SerializeValue(ref eventType);

                switch (eventType)
                {
                    case NetworkListEvent<T>.EventType.Add:
                        {
                            var value = new T();
                            reader.SerializeValue<T>(ref value);
                            list.Add(value);

                            if (OnListChanged != null)
                            {
                                OnListChanged(new NetworkListEvent<T>
                                {
                                    Type = eventType,
                                    Index = list.Count - 1,
                                    Value = list[list.Count - 1]
                                });
                            }

                            if (keepDirtyDelta)
                            {
                                listEvents.Add(new NetworkListEvent<T>()
                                {
                                    Type = eventType,
                                    Index = list.Count - 1,
                                    Value = list[list.Count - 1]
                                });
                                MarkNetworkObjectDirty();
                            }
                        }
                        break;
                    case NetworkListEvent<T>.EventType.Insert:
                        {
                            int index = 0;
                            reader.SerializeValue(ref index);
                            var value = new T();
                            reader.SerializeValue<T>(ref value);

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
                                OnListChanged(new NetworkListEvent<T>
                                {
                                    Type = eventType,
                                    Index = index,
                                    Value = list[index]
                                });
                            }

                            if (keepDirtyDelta)
                            {
                                listEvents.Add(new NetworkListEvent<T>()
                                {
                                    Type = eventType,
                                    Index = index,
                                    Value = list[index]
                                });
                                MarkNetworkObjectDirty();
                            }
                        }
                        break;
                    case NetworkListEvent<T>.EventType.Remove:
                        {
                            var value = new T();
                            reader.SerializeValue<T>(ref value);
                            int index = list.IndexOf(value);
                            if (index == -1)
                            {
                                break;
                            }

                            list.RemoveAt(index);

                            if (OnListChanged != null)
                            {
                                OnListChanged(new NetworkListEvent<T>
                                {
                                    Type = eventType,
                                    Index = index,
                                    Value = value
                                });
                            }

                            if (keepDirtyDelta)
                            {
                                listEvents.Add(new NetworkListEvent<T>()
                                {
                                    Type = eventType,
                                    Index = index,
                                    Value = value
                                });
                                MarkNetworkObjectDirty();
                            }
                        }
                        break;
                    case NetworkListEvent<T>.EventType.RemoveAt:
                        {
                            int index = 0;
                            reader.SerializeValue(ref index);
                            T value = list[index];
                            list.RemoveAt(index);

                            if (OnListChanged != null)
                            {
                                OnListChanged(new NetworkListEvent<T>
                                {
                                    Type = eventType,
                                    Index = index,
                                    Value = value
                                });
                            }

                            if (keepDirtyDelta)
                            {
                                listEvents.Add(new NetworkListEvent<T>()
                                {
                                    Type = eventType,
                                    Index = index,
                                    Value = value
                                });
                                MarkNetworkObjectDirty();
                            }
                        }
                        break;
                    case NetworkListEvent<T>.EventType.Value:
                        {
                            int index = 0;
                            reader.SerializeValue(ref index);
                            var value = new T();
                            reader.SerializeValue<T>(ref value);
                            if (index >= list.Count)
                            {
                                throw new Exception("Shouldn't be here, index is higher than list length");
                            }

                            var previousValue = list[index];
                            list[index] = value;

                            if (OnListChanged != null)
                            {
                                OnListChanged(new NetworkListEvent<T>
                                {
                                    Type = eventType,
                                    Index = index,
                                    Value = value,
                                    PreviousValue = previousValue
                                });
                            }

                            if (keepDirtyDelta)
                            {
                                listEvents.Add(new NetworkListEvent<T>()
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
                    case NetworkListEvent<T>.EventType.Clear:
                        {
                            //Read nothing
                            list.Clear();

                            if (OnListChanged != null)
                            {
                                OnListChanged(new NetworkListEvent<T>
                                {
                                    Type = eventType,
                                });
                            }

                            if (keepDirtyDelta)
                            {
                                listEvents.Add(new NetworkListEvent<T>()
                                {
                                    Type = eventType
                                });
                                MarkNetworkObjectDirty();
                            }
                        }
                        break;
                    case NetworkListEvent<T>.EventType.Full:
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
            for (int i = 0; i < count; i++)
            {
                T value = new T();
                reader.SerializeValue<T>(ref value);
                list.Add(value);
            }
        }

        public override void WriteDelta(IReaderWriter writer)
        {
            if (base.IsDirty())
            {
                ushort n = 1;
                writer.SerializeValue(ref n);
                var t = NetworkListEvent<T>.EventType.Full;
                writer.SerializeValue(ref t);
                Write(writer);

                return;
            }
            var evtCount = (ushort)listEvents.Count;
            writer.SerializeValue(ref evtCount);
            for (int i = 0; i < listEvents.Count; i++)
            {
                var element = listEvents[i];
                var evtType = element.Type;
                writer.SerializeValue(ref evtType);
                int index;
                T value;
                index = element.Index;
                value = element.Value;
                switch (element.Type)
                {
                    case NetworkListEvent<T>.EventType.Add:
                        {

                            writer.SerializeValue(ref value);
                        }
                        break;
                    case NetworkListEvent<T>.EventType.Insert:
                        {
                            writer.SerializeValue(ref index);
                            writer.SerializeValue(ref value);
                        }
                        break;
                    case NetworkListEvent<T>.EventType.Remove:
                        {
                            writer.SerializeValue(ref value);
                        }
                        break;
                    case NetworkListEvent<T>.EventType.RemoveAt:
                        {
                            writer.SerializeValue(ref index);
                        }
                        break;
                    case NetworkListEvent<T>.EventType.Value:
                        {
                            writer.SerializeValue(ref index);
                            writer.SerializeValue(ref value);
                        }
                        break;
                    case NetworkListEvent<T>.EventType.Clear:
                        {
                            //Nothing has to be written
                        }
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


    public struct NetworkListEvent<T>
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
