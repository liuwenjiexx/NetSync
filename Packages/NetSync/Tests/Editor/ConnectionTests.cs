using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using UnityEngine;

namespace Yanmonet.NetSync.Editor.Tests
{
    public class ConnectionTests : TestBase
    {
        [Test]
        public void Listen()
        {
            NetworkManager serverManager = new NetworkManager();
            using (NetworkConnection serverConn = new NetworkConnection(serverManager))
            {
                Assert.IsFalse(serverConn.IsListening);
                Assert.IsNull(serverConn.Socket);

                serverConn.Listen(localAddress, NextPort());
                Assert.IsTrue(serverConn.IsListening);
                Assert.IsNotNull(serverConn.Socket);
                Assert.IsFalse(serverConn.IsConnecting);
                Assert.IsFalse(serverConn.IsConnected);

                Update(serverConn);
                Assert.IsTrue(serverConn.IsListening);
                Assert.IsNotNull(serverConn.Socket);
            }
        }

        /*
        [Test]
        public void Connect()
        {
            string address = "127.0.0.1";
            int port = 7777;
            bool isClientConnected = false, isClientDisconnected = false;
            bool isServerConnected = false, isServerDisconnected = false;

            NetworkManager serverManager = new NetworkManager();
            using (NetworkConnection serverConn = new NetworkConnection(serverManager))
            {

                serverConn.Connected += (o, netMsg) =>
                {
                    isServerConnected = true;
                };
                serverConn.Disconnected += (o) =>
                {
                    isServerDisconnected = true;
                };
                serverConn.Listen(address, port);

                NetworkManager clientManager = new NetworkManager();
                using (NetworkConnection clientConn = new NetworkConnection(clientManager))
                {
                    clientConn.Connected += (o, netMsg) =>
                    {
                        isClientConnected = true;
                    };
                    clientConn.Disconnected += (o) =>
                    {
                        isClientDisconnected = true;
                    };

                    Assert.IsFalse(isClientConnected);
                    Assert.IsFalse(isClientDisconnected);
                    clientConn.Connect(address, port);
                    Update(serverConn, clientConn);

                    Assert.IsFalse(isClientConnected);
                    Assert.IsFalse(isClientDisconnected);

                    var s1 = serverConn.Accept();
                    Assert.IsNotNull(s1);
                     
                    Update(serverConn, clientConn);

                    Assert.IsTrue(isClientConnected);
                    Assert.IsFalse(isClientDisconnected);
                    Assert.IsTrue(isServerConnected);
                    Assert.IsFalse(isServerDisconnected);

                    Update(null, clientConn);
                }
                Assert.IsFalse(isClientConnected);
                Assert.IsTrue(isClientDisconnected);
                Assert.IsFalse(isServerConnected);
                Assert.IsTrue(isServerDisconnected);
            }
        }
        */

        [Test]
        public void Disconnect_Client()
        {
            NetworkManager serverManager = new NetworkManager();
            NetworkManager clientManager = new NetworkManager();
            clientManager.port = serverManager.port = NextPort();

            serverManager.StartServer();
            clientManager.StartClient();

            Update(serverManager, clientManager);
            var serverConn = serverManager.clients.Values.FirstOrDefault().Connection;
            var clientConn = clientManager.LocalClient.Connection;
            Assert.IsTrue(clientConn.IsConnected);
            Assert.IsTrue(serverConn.IsConnected);

            clientConn.Disconnect();
            Update(serverManager, clientManager);

            Assert.IsFalse(clientConn.IsConnected);
            Assert.IsTrue(serverConn.IsConnected);

            clientManager.Shutdown();
            serverManager.Shutdown();
        }


        //  [Test]
        public void Disconnect_Server()
        {
            NetworkManager serverManager = new NetworkManager();
            NetworkManager clientManager = new NetworkManager();
            clientManager.port = serverManager.port = NextPort();

            serverManager.StartServer();
            clientManager.StartClient();

            Update(serverManager, clientManager);
            var serverConn = serverManager.clients.Values.FirstOrDefault().Connection;
            var clientConn = clientManager.LocalClient.Connection;
            Assert.IsTrue(clientConn.IsConnected);
            Assert.IsTrue(serverConn.IsConnected);

            serverConn.Disconnect();
            Update(serverManager, clientManager);
            Update(serverManager, clientManager);

            Assert.IsFalse(clientConn.IsConnected);
            Assert.IsTrue(serverConn.IsConnected);

            clientManager.Shutdown();
            serverManager.Shutdown();

        }


        [Test]
        public void RegisterHandler()
        {
            NetworkManager manager = new NetworkManager();
            NetworkConnection conn = new NetworkConnection(manager);
            Assert.IsFalse(conn.HasHandler((ushort)NetworkMsgId.Max));

            conn.RegisterHandler((ushort)NetworkMsgId.Max, (netMsg) => { });
            Assert.IsTrue(conn.HasHandler((ushort)NetworkMsgId.Max));
            manager.Shutdown();
        }

        [Test]
        public void ConnectionData()
        {
            NetworkManager serverManager = new NetworkManager();
            NetworkManager clientManager = new NetworkManager();
            clientManager.port = serverManager.port = NextPort();
            string serverData = null;
            serverManager.ValidateConnect = (data) =>
            {
                serverData = Encoding.UTF8.GetString(data);
                return null;
            };
            serverManager.StartServer();
            serverManager.Update();

            clientManager.ConnectionData = Encoding.UTF8.GetBytes("AuthToken");
            clientManager.StartClient();

            Update(serverManager, clientManager);

            Assert.AreEqual("AuthToken", serverData);

            clientManager.Shutdown();
            serverManager.Shutdown();
        }

        [Test]
        public void StartServer()
        {
            NetworkManager serverManager = new NetworkManager();
            serverManager.port = NextPort();
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
                serverManager.Shutdown();
            }

        }


        [Test]
        public void StartClient()
        {
            NetworkManager serverManager = new NetworkManager();
            NetworkManager clientManager = new NetworkManager();
            clientManager.port = serverManager.port = NextPort();

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
                clientManager.Shutdown();
                serverManager.Shutdown();
            }
        }



    }
}