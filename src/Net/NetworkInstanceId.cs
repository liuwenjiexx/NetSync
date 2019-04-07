using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Net
{
    public struct NetworkInstanceId
    {
        private uint value;

        public NetworkInstanceId(uint value)
        {
            this.value = value;
        }

        public uint Value { get => value; internal set => this.value = value; }

        public override int GetHashCode()
        {
            return value.GetHashCode();
        }

    }
}
