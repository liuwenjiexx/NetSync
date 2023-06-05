using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using UnityEngine;
using Yanmonet.Network.Transport.Socket;
using System.Threading.Tasks;
using System.Threading;

namespace Yanmonet.Network.Sync.Editor.Tests
{
    public class ConnectionTests : TestBase
    {
        //[Test]
        //public void Listen()
        //{
        //    NetworkManager serverManager = new NetworkManager();
        //    using (NetworkConnection serverConn = new NetworkConnection(serverManager))
        //    {
        //        Assert.IsFalse(serverConn.IsListening);
        //        Assert.IsNull(serverConn.Socket);

        //        serverConn.Listen(localAddress, NextPort());
        //        Assert.IsTrue(serverConn.IsListening);
        //        Assert.IsNotNull(serverConn.Socket);
        //        Assert.IsFalse(serverConn.IsConnecting);
        //        Assert.IsFalse(serverConn.IsConnected);

        //        Update(serverConn);
        //        Assert.IsTrue(serverConn.IsListening);
        //        Assert.IsNotNull(serverConn.Socket);
        //    }
        //}

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


        //[Test]
        //public void RegisterHandler()
        //{
        //    NetworkManager manager = new NetworkManager();

        //    Assert.IsFalse(conn.HasHandler((ushort)NetworkMsgId.Max));

        //    manager.RegisterHandler((ushort)NetworkMsgId.Max, (netMsg) => { });
        //    Assert.IsTrue(conn.HasHandler((ushort)NetworkMsgId.Max));
        //    manager.Shutdown();
        //}




        [Test]
        public void Server()
        {
            int port = NextPort();
            NetworkManager server = CreateNetworkManager(port);

            try
            {
                Assert.IsFalse(server.IsServer);
                Assert.IsFalse(server.IsClient);

                server.StartServer();
                Update(server);

                Assert.IsTrue(server.IsServer);
                Assert.IsFalse(server.IsClient);
                Assert.AreEqual(0, server.LocalClientId);

                server.Shutdown();
                Assert.IsFalse(server.IsServer);
                Assert.IsFalse(server.IsClient);
            }
            finally
            {
                server.Shutdown();
            }

        }

        [Test]
        public void Server_ClientConnected()
        {
            int port = NextPort();
            NetworkManager server = CreateNetworkManager(port);
            bool clientConnected = false;
            server.ClientConnected += (n, clientId) =>
            {
                clientConnected = false;
            };

            server.StartServer();
            Update(server);

            Assert.IsFalse(clientConnected);

            server.Shutdown();

        }

        [Test]
        public void Client()
        {
            int port = NextPort();
            NetworkManager server = CreateNetworkManager(port);
            NetworkManager client = CreateNetworkManager(port);



            server.StartServer();

            try
            {
                client.StartClient();

                Update(server, client);
                Update(server, client);
                Update(server, client);

                Assert.IsFalse(client.IsServer);
                Assert.IsTrue(client.IsClient);

                Assert.AreEqual(1, server.ConnectedClientIds.Count);
                Assert.AreEqual(1, server.ConnectedClientIds[0]);
                Assert.AreEqual(1, client.LocalClientId);

            }
            finally
            {
                client.Shutdown();
                server.Shutdown();
            }
        }


        [Test]
        public void Connected()
        {
            int port = NextPort();
            NetworkManager server = CreateNetworkManager(port);
            NetworkManager client = CreateNetworkManager(port);

            List<ulong> serverClientIds = new();
            List<ulong> clientClientIds = new();
            server.ClientConnected += (n, clientId) =>
            {
                serverClientIds.Add(clientId);
            };
            client.ClientConnected += (n, clientId) =>
            {
                clientClientIds.Add(clientId);
            };

            server.StartServer();
            client.StartClient();

            Update(server, client);

            Assert.AreEqual(1, serverClientIds.Count);
            Assert.AreEqual(1, serverClientIds[0]);

            Assert.AreEqual(1, clientClientIds.Count);
            Assert.AreEqual(1, clientClientIds[0]);

        }



        [Test]
        public void NotConnected()
        {
            NetworkManager client = CreateNetworkManager(int.MaxValue);

            List<ulong> disconnectClientIds = new();

            client.ClientDisconnected += (n, clientId) =>
            {
                disconnectClientIds.Add(clientId);
            };

            client.StartClient();

            Update(client);

            Assert.AreEqual(1, disconnectClientIds.Count);
            Assert.AreEqual(ulong.MaxValue, disconnectClientIds[0]);

        }


        [Test]
        public void Host()
        {
            int port = NextPort();
            NetworkManager host = CreateNetworkManager(port);

            Assert.IsFalse(host.IsServer);
            Assert.IsFalse(host.IsClient);

            host.StartHost();

            Update(host);
            Update(host);

            Assert.IsTrue(host.IsServer);
            Assert.IsTrue(host.IsClient);
            Assert.AreEqual(NetworkManager.ServerClientId, host.LocalClientId);

            Assert.AreEqual(1, host.ConnectedClientIds.Count);
            Assert.AreEqual(NetworkManager.ServerClientId, host.ConnectedClientIds[0]);

            host.Shutdown();
            Assert.IsFalse(host.IsServer);
            Assert.IsFalse(host.IsClient);
        }


        [Test]
        public void Host_ClientConnected()
        {
            int port = NextPort();
            NetworkManager host = CreateNetworkManager(port);

            List<ulong> hostConnectedIds = new List<ulong>();
            host.ClientConnected += (netMgr, clientId) =>
            {
                hostConnectedIds.Add(clientId);
            };

            host.StartHost();

            Update(host);

            Assert.IsTrue(host.IsServer);
            Assert.IsTrue(host.IsClient);

            Assert.AreEqual(1, hostConnectedIds.Count);
            Assert.AreEqual(NetworkManager.ServerClientId, hostConnectedIds[0]);

            host.Shutdown();
            Assert.IsFalse(host.IsServer);
            Assert.IsFalse(host.IsClient);
        }


        [Test]
        public void Client_Host()
        {
            int port = NextPort();
            NetworkManager host = CreateNetworkManager(port);

            Assert.IsFalse(host.IsServer);
            Assert.IsFalse(host.IsClient);

            host.StartHost();

            Assert.IsTrue(host.IsServer);
            Assert.IsTrue(host.IsClient);

            NetworkManager client = CreateNetworkManager(port);

            client.StartClient();
            Update(host, client);

            Assert.AreEqual(1, client.LocalClientId);

            Assert.AreEqual(2, host.ConnectedClientIds.Count);
            Assert.AreEqual(0, host.ConnectedClientIds[0]);
            Assert.AreEqual(1, host.ConnectedClientIds[1]);

            client.Shutdown();
            host.Shutdown();
        }


        [Test]
        public void Client_Shutdown()
        {
            var port = NextPort();
            NetworkManager server = CreateNetworkManager(port);
            NetworkManager client = CreateNetworkManager(port);

            bool serverDisconnected = false;
            bool clientDisconnected = false;
            ulong serverDisconnectedId = 0;
            server.ClientDisconnected += (netMgr, clientId) =>
            {
                serverDisconnected = true;
                serverDisconnectedId = clientId;
            };
            client.ClientDisconnected += (netMgr, clientId) =>
            {
                clientDisconnected = true;
            };

            server.StartServer();
            var serverTask = Task.Run(() =>
            {
                while (server.IsServer)
                {
                    server.Update();
                    Thread.Sleep(5);
                }
            });

            try
            {
                client.StartClient();

                Update(client, server);

                client.Shutdown();
                Update(client, server);

                Assert.AreEqual(0, server.ConnectedClientIds.Count);
                Assert.IsFalse(client.IsClient);

                Assert.IsTrue(serverDisconnected);
                Assert.AreEqual(1, serverDisconnectedId);
                Assert.IsTrue(clientDisconnected);

            }
            finally
            {
                server.Shutdown();
            }
            serverTask.Wait();
        }


        [Test]
        public void Server_Disconnect()
        {
            var port = NextPort();
            NetworkManager serverManager = CreateNetworkManager(port);
            NetworkManager clientManager = CreateNetworkManager(port);

            bool serverDisconnected = false;
            bool clientDisconnected = false;
            ulong serverDisconnectedId = 0;
            serverManager.ClientDisconnected += (netMgr, clientId) =>
            {
                serverDisconnected = true;
                serverDisconnectedId = clientId;
            };
            clientManager.ClientDisconnected += (netMgr, clientId) =>
            {
                clientDisconnected = true;
            };

            serverManager.StartServer();
            var serverTask = Task.Run(() =>
            {
                while (serverManager.IsServer)
                {
                    serverManager.Update();
                    Thread.Sleep(1);
                }
            });

            try
            {

                clientManager.StartClient();

                Update(clientManager, serverManager);

                serverManager.DisconnectClient(serverManager.ConnectedClientIds[0]);
                Update(clientManager, serverManager);

                Assert.AreEqual(0, serverManager.ConnectedClientIds.Count);
                Assert.IsFalse(clientManager.IsClient);


                Assert.IsTrue(serverDisconnected);
                Assert.AreEqual(1, serverDisconnectedId);
                Assert.IsTrue(clientDisconnected);

            }
            finally
            {
                clientManager.Shutdown();
                serverManager.Shutdown();
            }
            serverTask.Wait();
        }


        [Test]
        public void Host_Shutdown()
        {
            var port = NextPort();
            NetworkManager host = CreateNetworkManager(port);

            bool serverDisconnected = false;
            bool clientDisconnected = false;
            ulong serverDisconnectedId = 0;
            host.ClientDisconnected += (netMgr, clientId) =>
            {
                serverDisconnected = true;
                serverDisconnectedId = clientId;
            };
            host.ClientDisconnected += (netMgr, clientId) =>
            {
                clientDisconnected = true;
            };

            host.StartHost();


            host.Shutdown();

            Assert.IsTrue(serverDisconnected);
            Assert.AreEqual(NetworkManager.ServerClientId, serverDisconnectedId);
            Assert.IsTrue(clientDisconnected);
        }


        [Test]
        public void ConnectionData()
        {
            var port = NextPort();
            NetworkManager serverManager = CreateNetworkManager(port);
            NetworkManager clientManager = CreateNetworkManager(port);

            string serverData = null;
            serverManager.ValidateConnect = (data) =>
            {
                serverData = Encoding.UTF8.GetString(data);
                return null;
            };

            serverManager.StartServer();
            var serverTask = Task.Run(() =>
            {
                while (serverManager.IsServer)
                {
                    serverManager.Update();
                    Thread.Sleep(1);
                }
            });


            clientManager.ConnectionData = Encoding.UTF8.GetBytes("AuthToken");
            clientManager.StartClient();

            Update(clientManager);

            Assert.AreEqual("AuthToken", serverData);

            clientManager.Shutdown();
            serverManager.Shutdown();
            serverTask.Wait();
        }

    }
}