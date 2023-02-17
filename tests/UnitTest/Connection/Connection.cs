using Microsoft.VisualStudio.TestTools.UnitTesting;
using Yanmonet.NetSync;
using Yanmonet.NetSync.Messages;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

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
                using (NetworkConnection clientConn = new NetworkConnection())
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
        public void ConnectExtra()
        {
            using (var server = NewSocketListener())
            {
                using (NetworkConnection conn = new NetworkConnection())
                {
                    conn.Connect(localAddress, localPort, new StringMessage("AuthToken"));

                    conn.Update();

                    var s1 = server.Accept();
                    string serverData = null;
                    using (var serverConn = new NetworkConnection(null, s1, true, true))
                    {
                        serverConn.Connected += (o, netMsg) =>
                        {
                            var msg = netMsg.ReadMessage<StringMessage>();
                            serverData = msg.Value;
                        };
                        serverConn.Update();
                    }

                    Assert.AreEqual(serverData, "AuthToken");

                }
            }
        }
        [TestMethod]
        public void Disconnect_Client()
        {
            using (NewConnect(out var serverConn, out var clientConn))
            using (serverConn)
            using (clientConn)
            {
                //    clientConn = new NetworkConnection();
                //    clientConn.Connect(localAddress, localPort);

                //    serverConn = new NetworkConnection(null,serverSocket.Accept(), true);

                //    clientConn.Update();
                //    serverConn.Update();

                Assert.IsTrue(clientConn.IsConnected);
                Assert.IsTrue(serverConn.IsConnected);

                clientConn.Disconnect();
                Update(serverConn, clientConn);

                Assert.IsFalse(clientConn.IsConnected);
                Assert.IsTrue(serverConn.IsConnected);
            }

        }


        [TestMethod]
        public void Disconnect_Server()
        {
            using (NewConnect(out var serverConn, out var clientConn))
            using (serverConn)
            using (clientConn)
            {
                Assert.IsTrue(clientConn.IsConnected);
                Assert.IsTrue(serverConn.IsConnected);

                serverConn.Disconnect();

                Assert.IsTrue(clientConn.IsConnected);
                Assert.IsFalse(serverConn.IsConnected);
            }

        }

        [TestMethod]
        public void RegisterHandler()
        {
            NetworkConnection conn = new NetworkConnection();
            Assert.IsFalse(conn.HasHandler((short)NetworkMsgId.Max));

            conn.RegisterHandler((short)NetworkMsgId.Max, (netMsg) => { });
            Assert.IsTrue(conn.HasHandler((short)NetworkMsgId.Max));
        }
        [TestMethod]
        public void RegisterHandler_ConnectAfter()
        {
            NetworkConnection conn = new NetworkConnection();

            Assert.IsFalse(conn.HasHandler((short)NetworkMsgId.Max));

            conn.RegisterHandler((short)NetworkMsgId.Max, (netMsg) => { });
            Assert.IsTrue(conn.HasHandler((short)NetworkMsgId.Max));
        }

        [TestMethod]
        public void _Conn()
        {
            var server = NewTcpListener();

            using (NetworkConnection conn = new NetworkConnection())
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
