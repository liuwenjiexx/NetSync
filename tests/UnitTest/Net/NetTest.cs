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
            NetworkManager serverManager = new NetworkManager();
            try
            {
                Assert.IsFalse(serverManager.IsServer);
                Assert.IsFalse(serverManager.IsClient);

                serverManager.StartServer();
                Update(serverManager);

                Assert.IsTrue(serverManager.IsServer);
                Assert.IsFalse(serverManager.IsClient);

                var server = serverManager.Server;

                Assert.IsTrue(server.IsRunning);

                serverManager.Shutdown();
                Assert.IsFalse(server.IsRunning);
            }
            finally
            {
                serverManager.Dispose();
            }

        }


        [TestMethod]
        public void StartClient()
        {
            NetworkManager serverManager = new NetworkManager();
            NetworkManager clientManager = new NetworkManager();

            try
            {
                serverManager.StartServer();
                clientManager.StartClient();

                var server = serverManager.Server;
                Update(clientManager, serverManager);

                Assert.IsFalse(clientManager.IsServer);
                Assert.IsTrue(clientManager.IsClient);

                Assert.IsNotNull(clientManager.LocalClient);
                var client = clientManager.LocalClient;

                Assert.IsTrue(client.IsRunning);
                Assert.IsTrue(client.Connection.IsConnected);
                Assert.AreEqual(1uL, clientManager.LocalClientId);
                Assert.AreEqual(1uL, client.ClientId);
                 
                Assert.AreEqual(1, server.Connections.Count());
                Assert.AreEqual(server.Clients.Count(), 1);

                client.Dispose();
                Update(clientManager, serverManager);

                Assert.IsFalse(client.IsRunning);
            }
            finally
            {
                clientManager.Dispose();
                serverManager.Dispose();
            }
        }
        //[TestMethod]
        //public void LocalClientId()
        //{
        //    NetworkManager serverManager = new NetworkManager();
        //    NetworkManager clientManager = new NetworkManager();
        //    try
        //    {
        //        serverManager.StartServer();
        //        clientManager.StartClient();

        //        Update(clientManager, serverManager);

        //        Assert.AreEqual(1, serverManager.ConnnectedClientIds.Count());
        //        Assert.AreEqual(1uL, serverManager.ConnnectedClientIds.First());
        //        Assert.AreEqual(1uL, clientManager.LocalClient.ClientId);
        //    }
        //    finally
        //    {
        //        clientManager.Dispose();
        //        serverManager.Dispose();
        //    }
        //}

        // [TestMethod]
        public void Reconnect()
        {
            NetworkManager serverManager = new NetworkManager();
            NetworkManager clientManager = new NetworkManager();
            try
            {
                serverManager.StartServer();

                clientManager.StartClient();
                Update(clientManager, serverManager);

                var server = serverManager.Server;
                var client = clientManager.LocalClient;

                Assert.IsTrue(client.Connection.IsConnected);
                Assert.IsTrue(client.Connection.Socket.Connected);

                client.Connection.Socket.Close();
                Update(clientManager, serverManager);
                Assert.IsFalse(client.Connection.IsConnected);

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

                Update(clientManager, serverManager);

                Assert.IsTrue(disconnectEvent);
                Assert.IsTrue(connectedEvent);


                Update(clientManager, serverManager);
                Assert.IsTrue(connectedEvent);
                Assert.IsTrue(client.Connection.Socket.Connected);
            }
            finally
            {
                clientManager.Dispose();
                serverManager.Dispose();
            }
        }
    }
}
