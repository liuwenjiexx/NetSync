using Microsoft.VisualStudio.TestTools.UnitTesting;
using Net;
using Net.Messages;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace UnitTest.Connection
{
    [TestClass]
    public class Connection : TestConnectionBase
    {
        [TestMethod]
        public void Event_Connected()
        {
            Run(_Event_Connected());
        }

        private IEnumerator _Event_Connected()
        {
            CleanupConnection();

            using (var server = NewSocketListener())
            {
                bool isClientConnected = false, isClientDisconnected = false;
                bool isServerConnected = false, isServerDisconnected = false;
                using (NetworkConnection conn = new NetworkConnection())
                {
                    conn.Connected += (o, netMsg) =>
                    {
                        isClientConnected = true;
                    };
                    conn.Disconnected += (o) =>
                    {
                        isClientDisconnected = true;
                    };
                    conn.Connect(localAddress, localPort);

                    foreach (var o in Wait()) yield return null;

                    var s1 = server.Accept();
                    Assert.IsNotNull(s1);

                    using (var serverConn = new NetworkConnection(null, s1, true))
                    {
                        serverConn.Connected += (o, netMsg) =>
                        {
                            isServerConnected = true;
                        };
                        serverConn.Disconnected += (o) =>
                        {
                            isServerDisconnected = true;
                        };
                        foreach (var o in Wait()) yield return null;
                        Assert.IsTrue(isClientConnected);
                        Assert.IsFalse(isClientDisconnected);
                        Assert.IsTrue(isServerConnected);
                        Assert.IsFalse(isServerDisconnected);


                        isClientConnected = false;
                        isClientDisconnected = false;
                        isServerConnected = false;
                        isServerDisconnected = false;
                    }
                    foreach (var o in Wait()) yield return null;
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
            Run(_ConnectExtra());
        }

        [TestMethod]
        public IEnumerator _ConnectExtra()
        {
            CleanupConnection();
            using (var server = NewSocketListener())
            {
                using (NetworkConnection conn = new NetworkConnection())
                {
                    conn.Connect(localAddress, localPort, new StringMessage("Text"));

                    foreach (var o in Wait()) yield return null;
                    var s1 = server.Accept();
                    string serverData = null;
                    using (var serverConn = new NetworkConnection(null,s1, true))
                    {
                        serverConn.Connected += (o, netMsg) =>
                        {
                            var msg = netMsg.ReadMessage<StringMessage>();
                            serverData = msg.Value;
                        };
                        foreach (var o in Wait()) yield return null;
                    }

                    Assert.AreEqual(serverData, "Text");

                }
            }
        }
        [TestMethod]
        public void Disconnect_Client()
        {
            Run(_Disconnect_Client());
        }

         
        public IEnumerator _Disconnect_Client()
        {
            foreach (var o in Wait()) yield return null;

            Assert.IsTrue(Client.IsConnected);
            Assert.IsTrue(Server.IsConnected);

            Client.Disconnect();

            Assert.IsFalse(Client.IsConnected);
            Assert.IsTrue(Server.IsConnected);
        }


        [TestMethod]
        public void Disconnect_Server()
        {
            Run(_Disconnect_Server());
        }

         
        public IEnumerator _Disconnect_Server()
        {
            foreach (var o in Wait()) yield return null;
            Assert.IsTrue(Client.IsConnected);
            Assert.IsTrue(Server.IsConnected);

            Server.Disconnect();

            Assert.IsTrue(Client.IsConnected);
            Assert.IsFalse(Server.IsConnected);
        }

        [TestMethod]
        public void RegisterHandler()
        {
            CleanupConnection();
            NetworkConnection conn = new NetworkConnection();
            Assert.IsFalse(conn.HasHandler((short)NetworkMsgId.Max));

            conn.RegisterHandler((short)NetworkMsgId.Max, (netMsg) => { });
            Assert.IsTrue(conn.HasHandler((short)NetworkMsgId.Max));
        }
        [TestMethod]
        public void RegisterHandler_ConnectAfter()
        {
            Assert.IsFalse(Client.HasHandler((short)NetworkMsgId.Max));

            Client.RegisterHandler((short)NetworkMsgId.Max, (netMsg) => { });
            Assert.IsTrue(Client.HasHandler((short)NetworkMsgId.Max));
        }

        [TestMethod]
        public void _Conn()
        {
            CleanupConnection();
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
