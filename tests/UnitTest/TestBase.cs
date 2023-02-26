using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
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

        protected NetworkServer server;
        protected NetworkClient client;

        protected NetworkManager serverManager;
        protected NetworkManager clientManager;
        static int nextPort = 7777;

        protected void OpenNetwork()
        {
            Assert.IsNull(serverManager);
            Assert.IsNull(clientManager);

            Console.WriteLine("Open Network");
            serverManager = new NetworkManager();
            clientManager = new NetworkManager();
            nextPort++;
            //Console.WriteLine("Port: " + nextPort);
            serverManager.port = nextPort;
            clientManager.port = nextPort;
            serverManager.StartServer();
            clientManager.StartClient();
            server = serverManager.Server;
            client = clientManager.LocalClient;
            Update(serverManager, clientManager);

        }

        protected void OpenNetwork<T>()
            where T : NetworkObject, new()
        {
            OpenNetwork();
            RegisterObject<T>();
        }

        protected void RegisterObject<T>()
            where T : NetworkObject, new()
        {
            serverManager.RegisterObject<T>((id) =>
            {
                return new T();
            });
            clientManager.RegisterObject<T>((id) =>
            {
                return new T();
            });
        }

        protected void CloseNetwork()
        {
            Console.WriteLine("Close Network");
            if (clientManager != null)
            {
                clientManager.Dispose();
                clientManager = null;
            }

            if (serverManager != null)
            {
                serverManager.Dispose();
                serverManager = null;
            }
        }


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

            CloseNetwork();
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
        protected void Update(params NetworkManager[] manager)
        {
            for (int i = 0; i < 5; i++)
            {
                foreach (var mgr in manager)
                {
                    mgr.Update();
                }
            }
        }

        protected async Task UpdateAsync(NetworkManager manager)
        {
            for (int i = 0; i < 5; i++)
            {
                manager.Update();
                await Task.Delay(10);
            }
        }

        protected void Update()
        {
            //if (server != null)
            //    server.Update();
            //if (client != null)
            //    client.Update();
            serverManager?.Update();
            clientManager?.Update();
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
        protected NetworkClient NewClient(NetworkManager manager, MessageBase extra = null)
        {
            NetworkClient client = new NetworkClient(manager);
            client.Connect(localAddress, localPort, extra);
            return client;
        }

        protected void NewClient(NetworkManager manager, out NetworkServer server, out NetworkClient client)
        {
            server = new NetworkServer(manager);
            server.Start(localPort);

            client = new NetworkClient(manager);
            client.Connect(localAddress, localPort);

            Update2(server, client);
        }

    }
}
