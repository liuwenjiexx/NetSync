using Microsoft.VisualStudio.TestTools.UnitTesting;
using Net;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.Net;

namespace UnitTest
{
    [TestClass]
    public class NetTest : TestBase
    {
        string AAA
        {
            get
            {
                Console.WriteLine("aaa");
                return "aaa";
            }
        }

        [TestMethod]
        public void NetServerStart()
        {
            TEST1(AAA);
            TEST2(AAA);
            Run(_NetServerStart());
        }
        public IEnumerator _NetServerStart()
        {
            int port = 1000;
            TcpListener tcpListener = new TcpListener(new IPEndPoint(IPAddress.Any, port));
            tcpListener.Start();
            NetworkServer server = new NetworkServer(tcpListener);
            server.Start();

            Assert.IsTrue(server.IsRunning);

            yield return null;
            server.Stop();
            Assert.IsFalse(server.IsRunning);
            tcpListener.Stop();
        }
        [TestMethod]
        public void NetClientStart()
        {
            Run(_NetClientStart());
        }
        public IEnumerator _NetClientStart()
        {
            int port = 1000;
            TcpListener tcpListener = new TcpListener(new IPEndPoint(IPAddress.Any, port));
            tcpListener.Start();
            NetworkServer server = new NetworkServer(tcpListener);
            server.Start();
            Assert.IsTrue(server.IsRunning);

            TcpClient tcpClient = new TcpClient();
            tcpClient.Connect("127.0.0.1", port);
            NetworkClient client = new NetworkClient(null, tcpClient.Client, false);
            client.Start();
            Assert.IsTrue(client.IsRunning);

            yield return null;
            client.Stop();
            Assert.IsFalse(client.IsRunning);
            yield return null;
            server.Stop();
            Assert.IsFalse(server.IsRunning);
            tcpListener.Stop();
        }

        [System.Diagnostics.Conditional("COND")]
        static void TEST1(string str)
        {
            Console.WriteLine("test1:" + str);
        }
        [System.Diagnostics.Conditional("COND")]
        void TEST2(string str)
        {
            Console.WriteLine("test2:" + str);
        }
        [TestMethod]
        public void Reconnect()
        {
            Run(_Reconnect());
        }
        public IEnumerator _Reconnect()
        {
            int port = 1000;
            TcpListener tcpListener = new TcpListener(new IPEndPoint(IPAddress.Any, port));
            tcpListener.Start();
            NetworkServer server = new NetworkServer(tcpListener);
            server.Start();

            TcpClient tcpClient = new TcpClient();
            tcpClient.Connect("127.0.0.1", port);
            NetworkClient client = new NetworkClient(null, tcpClient.Client, false);
            client.Start();
            bool connectedEvent = false;
            bool disconnectEvent = false;
            client.Connection.Connected += (c) =>
            {
                connectedEvent = true;
            };

            client.Connection.Disconnected += (c) =>
            {
                disconnectEvent = true;
            };
            Assert.IsTrue(client.Connection.IsConnected);
            Assert.IsTrue(client.Connection.Socket.Connected);

            connectedEvent = false;
            disconnectEvent = false;
            Assert.IsTrue(client.Connection.AllowReconnect);

            client.Connection.Socket.Disconnect(false);
            yield return null;
            Assert.IsTrue(disconnectEvent);
            Assert.IsTrue(connectedEvent);

            connectedEvent = false;
            disconnectEvent = false;
            client.Connection.Socket.Disconnect(false);
            yield return null;
            System.Threading.Thread.Sleep(client.Connection.ReconnectInterval + 1);
            yield return null;
            Assert.IsTrue(connectedEvent);
            Assert.IsTrue(client.Connection.Socket.Connected);

            yield return null;
            server.Stop();
            tcpListener.Stop();
        }
    }
}
