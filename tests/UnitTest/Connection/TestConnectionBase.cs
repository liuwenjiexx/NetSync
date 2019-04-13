using Microsoft.VisualStudio.TestTools.UnitTesting;
using Net;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace UnitTest
{
    public class TestConnectionBase : TestBase
    {
        private NetworkConnection server;
        private NetworkConnection client;
        private Socket serverSocket;

        public NetworkConnection Server { get => server; }
        public NetworkConnection Client { get => client; }

        [TestInitialize]
        public override void TestInitialize()
        {
            base.TestInitialize();
            serverSocket = NewSocketListener();

            client = new NetworkConnection();
            client.Connect(localAddress, localPort);

            server = new NetworkConnection(serverSocket.Accept(), true);
        }

        protected void CleanupConnection()
        {
            if (client != null)
            {
                client.Dispose();
                client = null;
            }
            if (server != null)
            {
                server.Dispose();
                server = null;
            }
            if (serverSocket != null)
            {
                serverSocket.Dispose();
                serverSocket = null;
            }
        }

        [TestCleanup]
        public override void TestCleanup()
        {
            CleanupConnection();
            base.TestCleanup();
        }
    }
}
