using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Yanmonet.NetSync
{
    public interface ISerializer
    {
        public bool IsReader { get; }

        public bool IsWriter { get; }

        void SerializeValue(ref int value);

        void SerializeValue(ref string value);
    }
}