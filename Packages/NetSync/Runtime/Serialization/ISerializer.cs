using System.Collections;
using System.Collections.Generic;

namespace Unity.Network.Sync
{
    public interface ISerializer
    {
        public bool IsReader { get; }

        public bool IsWriter { get; }

        void SerializeValue(ref int value);

        void SerializeValue(ref string value);
    }
}