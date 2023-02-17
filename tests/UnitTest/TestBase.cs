using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Yanmonet.NetSync.Test
{


    public class TestBase
    {
        List<IEnumerator> runner;
        public static string localAddress = "localhost";
        public static int localPort = 7001;
        public static string UserId = "userid";

        internal MyServer server;
        internal MyClient client;

        [TestInitialize]
        public virtual void TestInitialize()
        {
            runner = new List<IEnumerator>();
        }

        [TestCleanup]
        public virtual void TestCleanup()
        {
            int n = 10;
            //while (n-- > 0)
            //    CoroutineBase.UpdateCoroutine();
        }

        protected void Run(IEnumerator r)
        {
            runner.Add(r);

            while (runner.Count > 0)
            {
                //       CoroutineBase.UpdateCoroutine();
                for (int i = 0; i < runner.Count; i++)
                {
                    var item = runner[i];
                    if (!item.MoveNext())
                    {
                        runner.RemoveAt(i);
                        i--;
                    }
                }
            }
        }

        protected IEnumerable Wait(int frameCount)
        {
            while (frameCount-- > 0)
                yield return null;
        }

        protected IEnumerable Wait()
        {
            return Wait(6);
        }

        protected async Task Update(NetworkServer server, NetworkClient client)
        {
            //for (int i = 0; i < 1; i++)
            //{
            //    server.Update();
            //    client.Update();
            //    //await Task.Delay(0);

            //}
            server.Update();
            client.Update();
        }

        protected void Update2(NetworkServer server, NetworkClient client)
        {
            for (int i = 0; i < 5; i++)
            {
                if (server != null)
                    server.Update();
                if (client != null)
                    client.Update();
            }
        }

        protected void Update()
        {
            if (server != null)
                server.Update();
            if (client != null)
                client.Update();
        }
        protected void Update(NetworkConnection server, NetworkConnection client)
        {
            for (int i = 0; i < 3; i++)
            {
                if (server != null)
                    server.Update();
                if (client != null)
                    client.Update();
            }
        }
        protected TcpListener NewTcpListener()
        {
            TcpListener tcpListener = new TcpListener(new IPEndPoint(IPAddress.Any, localPort));
            tcpListener.Start();
            return tcpListener;
        }
        protected Socket NewSocketListener()
        {
            Socket tcpListener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            tcpListener.Bind(new IPEndPoint(IPAddress.Any, localPort));
            tcpListener.Listen(10);
            return tcpListener;
        }

        protected Socket NewConnect(out NetworkConnection serverConn, out NetworkConnection clientConn)
        {
            var serverSocket = NewSocketListener();

            clientConn = new NetworkConnection();
            clientConn.Connect(localAddress, localPort);

            serverConn = new NetworkConnection(null, serverSocket.Accept(), true, true);

            for (int i = 0; i < 3; i++)
            {
                clientConn.Update();
                serverConn.Update();
            }

            return serverSocket;
        }

        protected NetworkConnection NewSocketListener2()
        {
            Socket tcpListener = NewSocketListener();
            return new NetworkConnection(null, tcpListener, true, true);
        }
        protected Socket NewTcpClient()
        {
            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.Connect(localAddress, localPort);
            return socket;
        }
        protected NetworkClient NewClient(MessageBase extra = null)
        {
            NetworkClient client = new NetworkClient();
            client.Connect(localAddress, localPort, extra);
            return client;
        }

        protected void NewClient(out NetworkServer server, out NetworkClient client)
        {
            server = new NetworkServer();
            server.Start(localPort);

            client = new NetworkClient();
            client.Connect(localAddress, localPort);

            Update2(server, client);
        }

    }
}
