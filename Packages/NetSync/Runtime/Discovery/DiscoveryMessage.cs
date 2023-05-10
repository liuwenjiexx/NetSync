using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;
using Yanmonet.NetSync;

namespace Yanmonet.Network.Sync
{

    public class DiscoveryRequest<T> : INetworkSerializable
        where T : INetworkSerializable, new()
    {
        private string identifier;
        private string serverName;
        private int version;
        private T data;

        public string Identifier { get => identifier; set => identifier = value; }
        public string ServerName { get => serverName; set => serverName = value; }
        public int Version { get => version; set => version = value; }
        public T Data { get => data; set => data = value; }

        public IPEndPoint Remote { get; set; }


        public void NetworkSerialize(IReaderWriter readerWriter)
        {
            readerWriter.SerializeValue(ref identifier);
            readerWriter.SerializeValue(ref serverName);
            readerWriter.SerializeValue(ref version);

            if (readerWriter.IsReader)
            {
                Data = new T();
            }
            else
            {
                if (Data == null)
                    Data = new T();
            }
            Data.NetworkSerialize(readerWriter);
        }
    }

    public class DiscoveryResponse<T> : INetworkSerializable
        where T : INetworkSerializable, new()
    {
        private string identifier;
        private string serverName;
        private int version;

        private T data;
        private IPEndPoint remote;

        public string Identifier { get => identifier; set => identifier = value; }
        public string ServerName { get => serverName; set => serverName = value; }
        public int Version { get => version; set => version = value; }

        public T Data { get => data; set => data = value; }
        public IPEndPoint Remote { get => remote; set => remote = value; }

        public void NetworkSerialize(IReaderWriter readerWriter)
        {
            readerWriter.SerializeValue(ref identifier);
            readerWriter.SerializeValue(ref serverName);
            readerWriter.SerializeValue(ref version);

            if (readerWriter.IsReader)
            {
                data = new();
            }
            else
            {
                if (data == null)
                    data = new T();
            }
            data.NetworkSerialize(readerWriter);
        }

    }
}
