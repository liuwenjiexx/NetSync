using Microsoft.VisualStudio.TestTools.UnitTesting;
using Net;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace UnitTest
{


    public class TestBase
    {
        List<IEnumerator> runner;
        public static string localAddress = "localhost";
        public static int localPort = 7001;
        public static string UserId = "userid";

        [TestInitialize]
        public virtual void TestInitialize()
        {
            runner = new List<IEnumerator>();
        }

        [TestCleanup]
        public virtual void TestCleanup()
        {
            int n = 10;
            while (n-- > 0)
                CoroutineBase.UpdateCoroutine();
        }

        protected void Run(IEnumerator r)
        {
            runner.Add(r);

            while (runner.Count > 0)
            {
                CoroutineBase.UpdateCoroutine();
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
        protected Socket NewTcpClient()
        {
            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.Connect(localAddress, localPort);
            return socket;

        }


    }
}
