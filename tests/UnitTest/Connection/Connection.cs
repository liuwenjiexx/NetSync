using Microsoft.VisualStudio.TestTools.UnitTesting;
using Net;
using System;
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
            CleanupConnection();
            using (var server = NewSocketListener())
            {
                bool isConnected = false, isDisconnected = false;
                using (NetworkConnection conn = new NetworkConnection())
                {
                    conn.Connected += (o) =>
                    {
                        isConnected = true;
                    };
                    conn.Disconnected += (o) =>
                    {
                        isDisconnected = true;
                    };
                    conn.Connect(localAddress, localPort);
                    Assert.IsTrue(isConnected);
                    Assert.IsFalse(isDisconnected);

                    isConnected = false;
                    isDisconnected = false;
                }
                Assert.IsFalse(isConnected);
                Assert.IsTrue(isDisconnected);
            }
        }

        [TestMethod]
        public void Disconnect_Client()
        {
            Assert.IsTrue(Client.IsConnected);
            Assert.IsTrue(Server.IsConnected);

            Client.Disconnect();

            Assert.IsFalse(Client.IsConnected);
            Assert.IsTrue(Server.IsConnected);
        }


        [TestMethod]
        public void Disconnect_Server()
        {
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
