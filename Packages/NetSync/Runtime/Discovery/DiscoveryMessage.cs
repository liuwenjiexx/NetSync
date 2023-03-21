using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;
using Yanmonet.NetSync;

namespace Yanmonet.NetSync
{

    public class DiscoveryRequest<T> : INetworkSerializable
        where T : INetworkSerializable, new()
    {
        private string identifier;
        private string serverName;
        private int version;
        private T userData;

        public string Identifier { get => identifier; set => identifier = value; }
        public string ServerName { get => serverName; set => serverName = value; }
        public int Version { get => version; set => version = value; }
        public T UserData { get => userData; set => userData = value; }

        public IPEndPoint Remote { get; set; }


        public void NetworkSerialize(IReaderWriter readerWriter)
        {
            readerWriter.SerializeValue(ref identifier);
            readerWriter.SerializeValue(ref serverName);
            readerWriter.SerializeValue(ref version);

            if (readerWriter.IsReader)
            {
                UserData = new T();
            }
            else
            {
                if (UserData == null)
                    UserData = new T();
            }
            UserData.NetworkSerialize(readerWriter);
        }
    }

    public class DiscoveryResponse<T> : INetworkSerializable
        where T : INetworkSerializable, new()
    {
        private string identifier;
        private string serverName;
        private int version;

        private T userData;
        private IPEndPoint remote;

        public string Identifier { get => identifier; set => identifier = value; }
        public string ServerName { get => serverName; set => serverName = value; }
        public int Version { get => version; set => version = value; }

        public T UserData { get => userData; set => userData = value; }
        public IPEndPoint Remote { get => remote; set => remote = value; }

        public void NetworkSerialize(IReaderWriter readerWriter)
        {
            readerWriter.SerializeValue(ref identifier);
            readerWriter.SerializeValue(ref serverName);
            readerWriter.SerializeValue(ref version);

            if (readerWriter.IsReader)
            {
                userData = new();
            }
            else
            {
                if (userData == null)
                    userData = new T();
            }
            userData.NetworkSerialize(readerWriter);
        }

    }
}
