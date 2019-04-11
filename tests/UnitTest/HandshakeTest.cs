using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Net;
using Net.Messages;
using System.Linq;

namespace UnitTest
{

    [TestClass]
    public class HandshakeTest : TestBase
    {
        private class TestHandshakeServer : NetworkServer
        {
            public TestHandshakeServer(TcpListener server)
                : base(server)
            {

            }

            protected override NetworkClient AcceptClient(TcpClient tcpClient)
            {
                var client = new TestHandshakeClient(this, tcpClient.Client, true);
                client.Start();
                return client;
            }
        }

        class TestHandshakeClient : NetworkClient
        {
        
            public TestHandshakeClient(NetworkServer server, Socket socket, bool isListen)
                : base(server, socket, isListen)
            { 
            } 
      

            public TestHandshakeContext Context { get => context; }

            protected override void SendHandshakeMsg()
            {
                if (IsClient)
                {
                    Connection.SendMessage((short)NetworkMsgId.Handshake, new StringMessage("test client"));
                }
                else
                {
                    Connection.SendMessage((short)NetworkMsgId.Handshake);
                }
            }

            protected override void OnHandshakeMsg(NetworkMessage netMsg)
            {
                if (IsClient)
                {

                }
                else
                {
                    var msg = netMsg.ReadMessage<StringMessage>();
                    if (msg.Value != "test client")
                        throw new Exception("Handshake error");
                    SendHandshakeMsg();

                }
            }
        }


        [TestMethod]
        public void HandshakeSuccess()
        {
            Run(_HandshakeSuccess());
        }
        IEnumerator _HandshakeSuccess()
        {
            using (TestHandshakeServer server = new TestHandshakeServer(NewTcpListener()))
            {
                server.Start();

                TestHandshakeClient client = new TestHandshakeClient(null, NewTcpClient(), false);
                client.Start();
                foreach (var o in Wait()) yield return null;
                Assert.AreEqual(server.Clients.Count(), 1);
                Assert.IsTrue(client.IsRunning);

                client.Stop();
            }
        }
        [TestMethod]
        public void HandshakeError()
        {
            Run(_HandshakeError());
        }
        IEnumerator _HandshakeError()
        {
            using (TestHandshakeServer server = new TestHandshakeServer(NewTcpListener()))
            {
                server.Start();
                Assert.IsTrue(server.IsRunning);

                NetworkClient client = new NetworkClient(null, NewTcpClient(), false);
                client.Connection.AutoReconnect = false;
                client.Start();
                foreach (var o in Wait()) yield return null;

                Assert.AreEqual(server.Clients.Count(), 0);


                client.Ping();
                client.Ping();
                foreach (var o in Wait()) yield return null;

                Assert.IsFalse(client.Connection.IsSocketConnected);

                client.Stop();
            }
        }


        [NetworkObjectId("f0981ba1-765f-4fbf-824f-7b62e446019f")]
        class TestHandshakeContext : NetworkObject
        {
            public string msg;
        }


    }
}
