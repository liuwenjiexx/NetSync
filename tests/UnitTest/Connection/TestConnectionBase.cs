using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace Yanmonet.NetSync.Test
{
    public class TestConnectionBase : TestBase
    {
        private NetworkConnection serverConn;
        private NetworkConnection clientConn;
        private Socket serverSocket;

        public NetworkConnection Server { get => serverConn; }
        public NetworkConnection Client { get => clientConn; }

        //[TestInitialize]
        //public override void TestInitialize()
        //{
        //    base.TestInitialize();
        //    serverSocket = NewSocketListener();

        //    clientConn = new NetworkConnection();
        //    clientConn.Connect(localAddress, localPort);
            
        //    serverConn = new NetworkConnection(null,serverSocket.Accept(), true);

        //    clientConn.Update();
        //    serverConn.Update();
        //}

        //protected void CleanupConnection()
        //{
        //    if (clientConn != null)
        //    {
        //        clientConn.Disconnect();
        //        clientConn.Update();

        //        clientConn.Dispose();
        //        clientConn = null;
        //    }
        //    if (serverConn != null)
        //    {
        //        serverConn.Disconnect();
        //        serverConn.Update();
        //        serverConn.Dispose();
        //        serverConn = null;
        //    }
        //    if (serverSocket != null)
        //    {
        //        serverSocket.Dispose();
        //        serverSocket = null;
        //    }
        //}

        [TestCleanup]
        public override void TestCleanup()
        {
            Update(serverConn, clientConn);
            base.TestCleanup();
        }
    }
}
