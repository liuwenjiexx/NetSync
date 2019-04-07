using System;

namespace Net
{

    [AttributeUsage(AttributeTargets.Class)]
    public class NetworkObjectIdAttribute : Attribute
    {
        public NetworkObjectIdAttribute(string guid)
        {
            Guid = new Guid(guid);
        }

        public Guid Guid { get; set; }
    }

}
