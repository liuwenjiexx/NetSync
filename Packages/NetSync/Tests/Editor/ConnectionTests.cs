using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using UnityEngine;
using Yanmonet.NetSync.Transport.Socket;
using System.Threading.Tasks;
using System.Threading;

namespace Yanmonet.NetSync.Editor.Tests
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
        public void StartServer()
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
        public void StartClient()
        {
            int port = NextPort();
            NetworkManager server = CreateNetworkManager(port);
            NetworkManager client = CreateNetworkManager(port);


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

                Update(client);

                Assert.IsFalse(client.IsServer);
                Assert.IsTrue(client.IsClient);

                Assert.AreEqual(1, server.ConnnectedClientIds.Count);
                Assert.AreEqual(1, server.ConnnectedClientIds[0]);
                Assert.AreEqual(1, client.LocalClientId);

            }
            finally
            {
                client.Shutdown();
                server.Shutdown();
            }
            serverTask.Wait();
        }


        [Test]
        public void StartHost()
        {
            int port = NextPort();
            NetworkManager host = CreateNetworkManager(port);
            bool serverConnected = false;
            bool clientConnected = false;
            ulong serverConnectedId = 0;
            host.ClientConnected += (clientId) =>
            {
                serverConnected = true;
                serverConnectedId = clientId;
            };
            host.Connected += () =>
            {
                clientConnected = true;
            };


            Assert.IsFalse(host.IsServer);
            Assert.IsFalse(host.IsClient);

            host.StartHost();

            Assert.IsTrue(host.IsServer);
            Assert.IsTrue(host.IsClient);
            Assert.AreEqual(NetworkManager.ServerClientId, host.LocalClientId);

            Assert.IsTrue(clientConnected);
            Assert.IsTrue(serverConnected);
            Assert.AreEqual(NetworkManager.ServerClientId, serverConnectedId);

            host.Shutdown();
            Assert.IsFalse(host.IsServer);
            Assert.IsFalse(host.IsClient);
        }



        [Test]
        public void Client_Host()
        {
            int port = NextPort();
            NetworkManager host = CreateNetworkManager(port);
            bool serverConnected = false;
            bool clientConnected = false;
            ulong serverConnectedId = 0;
            host.ClientConnected += (clientId) =>
            {
                serverConnected = true;
                serverConnectedId = clientId;
            };
            host.Connected += () =>
            {
                clientConnected = true;
            };


            Assert.IsFalse(host.IsServer);
            Assert.IsFalse(host.IsClient);

            host.StartHost();
            var hostTask = Task.Run(() =>
            {
                while (host.IsServer)
                {
                    host.Update();
                    Thread.Sleep(1);
                }
            });
            Assert.IsTrue(host.IsServer);
            Assert.IsTrue(host.IsClient);

            NetworkManager client = CreateNetworkManager(port);

            client.StartClient();


            Assert.AreEqual(1, client.LocalClientId);

            Assert.IsTrue(clientConnected);
            Assert.IsTrue(serverConnected);
            Assert.AreEqual(1, serverConnectedId);

            client.Shutdown();
            host.Shutdown();
            hostTask.Wait();
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
            server.ClientDisconnected += (clientId) =>
            {
                serverDisconnected = true;
                serverDisconnectedId = clientId;
            };
            client.Disconnected += () =>
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

                Assert.AreEqual(0, server.ConnnectedClientIds.Count);
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
            serverManager.ClientDisconnected += (clientId) =>
            {
                serverDisconnected = true;
                serverDisconnectedId = clientId;
            };
            clientManager.Disconnected += () =>
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

                serverManager.DisconnectClient(serverManager.ConnnectedClientIds[0]);
                Update(clientManager, serverManager);

                Assert.AreEqual(0, serverManager.ConnnectedClientIds.Count);
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
            host.ClientDisconnected += (clientId) =>
            {
                serverDisconnected = true;
                serverDisconnectedId = clientId;
            };
            host.Disconnected += () =>
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