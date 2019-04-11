using System;
using System.Linq;

namespace Net
{
    public struct NetworkObjectId
    {
        private Guid value;

        public NetworkObjectId(Guid value)
        {
            this.value = value;
        }

        public Guid Value { get => value; set => this.value = value; }


        public static NetworkObjectId New()
        {
            return new NetworkObjectId(Guid.NewGuid());
        }

        public static NetworkObjectId GetObjectId(Type type)
        {
            var idAttr = (NetworkObjectIdAttribute)type.GetCustomAttributes(typeof(NetworkObjectIdAttribute), false).FirstOrDefault();
            if (idAttr == null)
                throw new Exception("type:" + type + " not contains " + typeof(NetworkObjectIdAttribute));
            return new NetworkObjectId(idAttr.Guid);
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
