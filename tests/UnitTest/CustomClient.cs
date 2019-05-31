using Microsoft.VisualStudio.TestTools.UnitTesting;
using Net;
using Net.Messages;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace UnitTest
{
    [TestClass]
    public class CustomClient : TestBase
    {
        class CustomContextServer : NetworkServer
        {


            //protected override void OnCreateObject(NetworkObject instance, NetworkMessage netMsg)
            //{
            //    if (instance is CustomContext)
            //    {
            //        if (netMsg != null)
            //        {
            //            var msg = netMsg.ReadMessage<StringMessage>();
            //            var ctx = (CustomContext)instance;
            //            ctx.msg = msg.Value;
            //        }
            //        return;
            //    }
            //    base.OnCreateObject(instance, netMsg);
            //}
            protected override NetworkClient AcceptClient(TcpClient netClient, MessageBase extra)
            {
                var client = new CustomContextClient(this, netClient.Client, true);

                return client;
            }
        }

        class CustomContextClient : NetworkClient
        {
            public CustomContextClient() : this(null, null, false)
            {
            }

            public CustomContextClient(NetworkServer server, Socket socket, bool isListen)
                : base(server, socket, isListen)
            {

                Connection.ObjectAdded += Connection_ObjectAdded;

            }
            private CustomContext context;

            public CustomContext Context { get => context; }

            private void Connection_ObjectAdded(NetworkObject obj)
            {
                if (obj is CustomContext)
                {
                    context = (CustomContext)obj;
                }
            }

            protected override void OnServerConnect(NetworkMessage netMsg)
            {
                var msg = netMsg.ReadMessage<StringMessage>();
                context = Server.CreateObject<CustomContext>();
                context.msg = msg.Value;
                Server.AddObserver(context, netMsg.Connection);
                base.OnServerConnect(netMsg);
            }

            protected override void OnClientConnect(NetworkMessage netMsg)
            {
                var obj = Connection.Objects.FirstOrDefault();
                base.OnClientConnect(netMsg);
            }

        }


        [NetworkObjectId("cdd65abe-627b-40ed-a855-f811fde11752")]
        class CustomContext : NetworkObject
        {
            [SyncVar]
            public string msg;
        }


        [TestMethod]
        public void TestCustomContext()
        {
            Run(_CustomContext());
        }
        IEnumerator _CustomContext()
        {
            using (CustomContextServer server = new CustomContextServer())
            {
                server.Start(localPort);
                using (CustomContextClient client = new CustomContextClient())
                {

                    client.Connect(localAddress, localPort, new StringMessage("hello"));
                    bool isReady = false;
                    client.OnReady += (c) =>
                    {
                        isReady = true;
                    };

                    foreach (var o in Wait()) yield return null;
                    Assert.IsTrue(isReady);

                    var serverCtx = (CustomContext)server.Objects.First();

                    var clientCtx = client.Context;
                    Assert.AreEqual(serverCtx.msg, "hello");
                    Assert.AreEqual(clientCtx.msg, "hello");

                    Assert.IsNotNull(client.Context);

                    //  client.Stop();
                    //foreach (var o in Wait()) yield return null;
                    //client.Connection.Socket.Shutdown(SocketShutdown.Both);
                    //client.Connection.Socket.Disconnect(false);
                    //Thread.Sleep(1000);
                    //var ccc = server.Clients.First().Connection.Socket.Connected;
                    //var cccc = client.Connection.Socket.Connected;
                    //Assert.AreEqual(server.Objects.Count(), 0);
                }
            }
        }


    }
}
