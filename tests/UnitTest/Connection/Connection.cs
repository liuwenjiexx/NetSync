using Microsoft.VisualStudio.TestTools.UnitTesting;
using Yanmonet.NetSync;
using Yanmonet.NetSync.Messages;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace Yanmonet.NetSync.Test.Connection
{
    [TestClass]
    public class Connection : TestConnectionBase
    {
        [TestMethod]
        public void Event_Connected()
        {

            using (var server = NewSocketListener())
            {
                bool isClientConnected = false, isClientDisconnected = false;
                bool isServerConnected = false, isServerDisconnected = false;
                NetworkManager manager = new NetworkManager();
                using (NetworkConnection clientConn = new NetworkConnection(manager))
                {
                    clientConn.Connected += (o, netMsg) =>
                    {
                        isClientConnected = true;
                    };
                    clientConn.Disconnected += (o) =>
                    {
                        isClientDisconnected = true;
                    };
                    clientConn.Connect(localAddress, localPort);
                    Update(null, clientConn);

                    Assert.IsFalse(isClientConnected);
                    Assert.IsFalse(isClientDisconnected);

                    var s1 = server.Accept();
                    Assert.IsNotNull(s1);

                    using (var serverConn = new NetworkConnection(null, s1, true, true))
                    {
                        serverConn.Connected += (o, netMsg) =>
                        {
                            isServerConnected = true;
                        };
                        serverConn.Disconnected += (o) =>
                        {
                            isServerDisconnected = true;
                        };

                        Update(serverConn, clientConn);

                        Assert.IsTrue(isClientConnected);
                        Assert.IsFalse(isClientDisconnected);
                        Assert.IsTrue(isServerConnected);
                        Assert.IsFalse(isServerDisconnected);


                        isClientConnected = false;
                        isClientDisconnected = false;
                        isServerConnected = false;
                        isServerDisconnected = false;
                    }

                    Update(null, clientConn);
                }
                Assert.IsFalse(isClientConnected);
                Assert.IsTrue(isClientDisconnected);
                Assert.IsFalse(isServerConnected);
                Assert.IsTrue(isServerDisconnected);
            }
        }

        [TestMethod]
        public void ConnectionData()
        {
            NetworkManager serverManager = new NetworkManager();
            string serverData = null;
            serverManager.ValidateConnect = (version, data) =>
            {
                serverData = Encoding.UTF8.GetString(data);
                return null;
            };
            serverManager.StartServer();
            serverManager.Update();

            NetworkManager clientManager = new NetworkManager();
            clientManager.ConnectionData = Encoding.UTF8.GetBytes("AuthToken");
            clientManager.StartClient();

            Update(serverManager, clientManager);

            Assert.AreEqual("AuthToken", serverData);

            clientManager.Dispose();
            serverManager.Dispose();
        }
        [TestMethod]
        public void Disconnect_Client()
        {
            NetworkManager serverManager = new NetworkManager();
            NetworkManager clientManager = new NetworkManager();

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


        [TestMethod]
        public void Disconnect_Server()
        {
            NetworkManager serverManager = new NetworkManager();
            NetworkManager clientManager = new NetworkManager();

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

        [TestMethod]
        public void RegisterHandler()
        {
            NetworkManager manager = new NetworkManager();
            NetworkConnection conn = new NetworkConnection(manager);
            Assert.IsFalse(conn.HasHandler((ushort)NetworkMsgId.Max));

            conn.RegisterHandler((ushort)NetworkMsgId.Max, (netMsg) => { });
            Assert.IsTrue(conn.HasHandler((ushort)NetworkMsgId.Max));
        }
        [TestMethod]
        public void RegisterHandler_ConnectAfter()
        {
            NetworkManager manager = new NetworkManager();
            NetworkConnection conn = new NetworkConnection(manager);

            Assert.IsFalse(conn.HasHandler((ushort)NetworkMsgId.Max));

            conn.RegisterHandler((ushort)NetworkMsgId.Max, (netMsg) => { });
            Assert.IsTrue(conn.HasHandler((ushort)NetworkMsgId.Max));
        }

        [TestMethod]
        public void _Conn()
        {
            var server = NewTcpListener();

            NetworkManager manager = new NetworkManager();
            using (NetworkConnection conn = new NetworkConnection(manager))
            {
                conn.Connect(localAddress, localPort);
                bool bbb = conn.Socket.Connected;
                var sss = server.AcceptSocket();
                //conn.Socket.Disconnect(false);
                //conn.Socket.Close();
                conn.Socket.Dispose();
                //conn.Socket.BeginDisconnect(true, (o) =>
                //{
                //   Socket obj = (System.Net.Sockets.Socket)o.AsyncState;
                //    var ccccc = sss;
                //    bbb = conn.Socket.Connected;

                //}, sss);
                System.Threading.Thread.Sleep(100);

                bbb = conn.Socket.Connected;
            }
            server.Stop();
        }

    }
}
