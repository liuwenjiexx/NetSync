using System;

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

        public bool IsEmpty
        {
            get { return value == 0; }
        }


        public override int GetHashCode()
        {
            return value.GetHashCode();
        }
        public override string ToString()
        {
            return value.ToString();
        }

    }
}
