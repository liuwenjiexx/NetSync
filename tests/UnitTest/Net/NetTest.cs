using Microsoft.VisualStudio.TestTools.UnitTesting;
using Net;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Linq;

namespace UnitTest
{
    [TestClass]
    public class NetTest : TestBase
    {
        [TestMethod]
        public void StartServer()
        {
            Run(_StartServer());
        }
        public IEnumerator _StartServer()
        {
            using (NetworkServer server = new NetworkServer())
            {
                server.Start(localPort);
                Assert.IsTrue(server.IsRunning);

                foreach (var o in Wait()) yield return null;
                server.Stop();
                Assert.IsFalse(server.IsRunning);
            }
        }

        [TestMethod]
        public void StartClient()
        {
            Run(_StartClient());
        }
        public IEnumerator _StartClient()
        {
            using (NetworkServer server = new NetworkServer())
            {
                server.Start(localPort);
                Assert.IsTrue(server.IsRunning);

                NetworkClient client = new NetworkClient();

                client.Connect(localAddress, localPort);
                foreach (var o in Wait()) yield return null;

                Assert.IsTrue(client.IsRunning);
                Assert.IsTrue(client.Connection.IsConnected);
                Assert.AreEqual(server.Connections.Count(), 1);
                Assert.AreEqual(server.Clients.Count(), 1);

                client.Dispose();
                foreach (var o in Wait()) yield return null;
                Assert.IsFalse(client.IsRunning);
                server.Stop();
                Assert.IsFalse(server.IsRunning);
            }
        }
        [TestMethod]
        public void ConnectionId()
        {
            Run(_ConnectionId());
        }
        public IEnumerator _ConnectionId()
        {
            using (NetworkServer server = new NetworkServer())
            {
                server.Start(localPort);

                using (NetworkClient client = new NetworkClient())
                {
                    client.Connect(localAddress, localPort);
                
                    foreach (var o in Wait()) yield return null;

                    Assert.AreEqual(server.Connections.Count(), 1);
                    int connectionId = server.Connections.First().ConnectionId;
                    Assert.IsTrue(connectionId > 0,connectionId.ToString());
                    Assert.IsTrue(client.Connection.IsConnected);
                    Assert.AreEqual(client.Connection.ConnectionId, connectionId);
                    Assert.AreEqual(client.Connection, server.Connections.First());
                }
            }
        }

        [TestMethod]
        public void Reconnect()
        {
            Run(_Reconnect());
        }
        public IEnumerator _Reconnect()
        {
            using (NetworkServer server = new NetworkServer())
            {
                server.Start(localPort);

                NetworkClient client = new NetworkClient();
                client.Connect(localAddress, localPort);
                foreach (var o in Wait()) yield return null;

                Assert.IsTrue(client.Connection.IsConnected);
                Assert.IsTrue(client.Connection.Socket.Connected);


                bool connectedEvent = false;
                bool disconnectEvent = false;
                client.Connection.Connected += (c, netMsg) =>
                {
                    connectedEvent = true;
                };

                client.Connection.Disconnected += (c) =>
                {
                    disconnectEvent = true;
                    client.Connection.Connect(localAddress, localPort);
                };
                client.Connection.Socket.Close();
                foreach (var o in Wait()) yield return null;

                Assert.IsTrue(disconnectEvent);
                Assert.IsTrue(connectedEvent);

                foreach (var o in Wait()) yield return null;
                Assert.IsTrue(connectedEvent);
                Assert.IsTrue(client.Connection.Socket.Connected);

            }
        }
    }
}
