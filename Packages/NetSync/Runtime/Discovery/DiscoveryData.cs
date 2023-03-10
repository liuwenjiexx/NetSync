using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;

namespace Yanmonet.NetSync
{

    public class DiscoveryData
    {
        private string name;
        private IPEndPoint endPoint;
        private int serverPort;
        private byte[] userData;

        public string Name { get => name; internal set => name = value; }
        public IPEndPoint EndPoint { get => endPoint; internal set => endPoint = value; }
        public int ServerPort { get => serverPort; internal set => serverPort = value; }
        public byte[] UserData { get => userData; internal set => userData = value; }
    }
}
