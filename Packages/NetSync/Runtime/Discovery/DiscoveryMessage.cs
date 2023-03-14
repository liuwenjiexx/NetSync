using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Yanmonet.NetSync;

namespace Yanmonet.NetSync
{

    internal class DiscoveryMessage : MessageBase
    {
        public string identifier;
        public int version;
        public string name;
        public string serverAddress;
        public int serverPort;
        public byte[] userData;

        public override void Serialize(IReaderWriter writer)
        {
            writer.SerializeValue(ref identifier);
            writer.SerializeValue(ref version);
            writer.SerializeValue(ref name);
            writer.SerializeValue(ref serverAddress);
            writer.SerializeValue(ref serverPort);

            if (userData == null)
                userData = new byte[0];
            int length = userData.Length;
            writer.SerializeValue(ref userData, 0, ref length);

        }

        public override void Deserialize(IReaderWriter reader)
        {
            reader.SerializeValue(ref identifier);
            reader.SerializeValue(ref version);
            reader.SerializeValue(ref name);
            reader.SerializeValue(ref serverAddress);
            reader.SerializeValue(ref serverPort);

            int length = 0;
            userData = null;
            reader.SerializeValue(ref userData, 0, ref length);
        }

    }
    internal class LookupMessage : MessageBase
    {
        public string identifier;
        public byte[] userData;
        public string name;
        public int version;

        public override void Serialize(IReaderWriter writer)
        {
            writer.SerializeValue(ref identifier);
            writer.SerializeValue(ref version);
            writer.SerializeValue(ref name);
            
            if (userData == null)
                userData = new byte[0];
            int length = userData.Length;
            writer.SerializeValue(ref userData, 0, ref length);

        }

        public override void Deserialize(IReaderWriter reader)
        {
            reader.SerializeValue(ref identifier);
            reader.SerializeValue(ref version);
            reader.SerializeValue(ref name);

            int length = 0;
            userData = null;
            reader.SerializeValue(ref userData, 0, ref length);
        }
    }
}
