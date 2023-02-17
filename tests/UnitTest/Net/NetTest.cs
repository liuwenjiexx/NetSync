using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Linq;

namespace Yanmonet.NetSync.Test
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

            NewClient(out var server, out var client);
            using (server)
            using (client)
            {

                Assert.IsTrue(client.IsRunning);
                Assert.IsTrue(client.Connection.IsConnected);
                Assert.AreEqual(server.Connections.Count(), 1);
                Assert.AreEqual(server.Clients.Count(), 1);

                client.Dispose();
                Update2(server, client);
                Assert.IsFalse(client.IsRunning);
                server.Stop();
                Assert.IsFalse(server.IsRunning);
            }
        }
        [TestMethod]
        public void ConnectionId()
        {
            NewClient(out var server, out var client);
            using (server)
            using (client)
            {
                Assert.AreEqual(server.Connections.Count(), 1);
                int connectionId = server.Connections.First().ConnectionId;
                Assert.IsTrue(connectionId > 0, connectionId.ToString());
                Assert.IsTrue(client.Connection.IsConnected);
                Assert.AreEqual(client.Connection.ConnectionId, connectionId);
                Assert.AreEqual(client.Connection, server.Connections.First());
            }
        }

        [TestMethod]
        public void Reconnect()
        {
            NewClient(out var server, out var client);
            using (server)
            using (client)
            {
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
                Update2(server, client);

                Assert.IsTrue(disconnectEvent);
                Assert.IsTrue(connectedEvent);

                Update2(server, client);
                Assert.IsTrue(connectedEvent);
                Assert.IsTrue(client.Connection.Socket.Connected);

            }
        }
    }
}
