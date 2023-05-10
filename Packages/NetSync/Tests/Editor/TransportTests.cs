using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using UnityEngine;
using Yanmonet.Network.Transport;
using Yanmonet.Network.Transport.Socket;

namespace Yanmonet.Network.Sync.Editor.Tests
{

    public class TransportTests
    {
        //[Test]
        //public void Host()
        //{
        //    SocketTransport server = new SocketTransport();
        //    server.StartServer();

        //    server.StartClient();

        //    ulong clientId;
        //    ArraySegment<byte> payload;
        //    float receiveTime;

        //    Assert.AreEqual(NetworkEvent.Connect, server.PollEvent(out clientId, out payload, out receiveTime));

        //    server.Shutdown();
        //}

        /* [Test]
         public void ConnectValidate()
         {
             NetworkManager serverNetworkManager = new NetworkManager();
             NetworkManager clientNetworkManager = new NetworkManager();
             clientNetworkManager.ConnectionData = Encoding.UTF8.GetBytes("abc");
             string serverPayload = null;
             serverNetworkManager.ValidateConnect = (payload) =>
             {
                 serverPayload = Encoding.UTF8.GetString(payload);
                 return null;
             };

             SocketTransport server = new SocketTransport();
             server.Initialize(serverNetworkManager);
             Assert.IsTrue(server.StartServer());

             SocketTransport client = new SocketTransport();
             client.Initialize(clientNetworkManager);
             Assert.IsTrue(client.StartClient());

             Assert.AreEqual("abc", serverPayload);

             client.Shutdown();
             server.Shutdown();
         }
         [Test]
         public void ConnectValidateError()
         {
             NetworkManager serverNetworkManager = new NetworkManager();
             NetworkManager clientNetworkManager = new NetworkManager();

             serverNetworkManager.ValidateConnect = (payload) =>
             {
                 throw new Exception("my error");
             };

             SocketTransport server = new SocketTransport();
             server.Initialize(serverNetworkManager);
             Assert.IsTrue(server.StartServer());

             SocketTransport client = new SocketTransport();
             client.Initialize(clientNetworkManager);
             Assert.IsFalse(client.StartClient());

             Assert.AreEqual("my error", client.ConnectFailReson);

             client.Shutdown();
             server.Shutdown();
         }*/

        [Test]
        public void Connect()
        {
            SocketTransport server = new SocketTransport();
            SocketTransport client = new SocketTransport();
            client.port = server.port = TestBase.NextPort();
            server.Initialize();
            Assert.IsTrue(server.StartServer());


            client.Initialize();
            Assert.IsTrue(client.StartClient());

            NetworkEvent @event;

            Assert.IsTrue(server.PollEvent(out @event));
            Assert.AreEqual(NetworkEventType.Connect, @event.Type);
            Assert.AreEqual(1, @event.ClientId);

            Assert.IsTrue(client.PollEvent(out @event));
            Assert.AreEqual(NetworkEventType.Connect, @event.Type);
            Assert.AreEqual(1, @event.ClientId);

            client.DisconnectLocalClient();
            server.DisconnectLocalClient();
        }
        [Test]
        public void Connect3()
        {
            SocketTransport server = new SocketTransport();
            SocketTransport client = new SocketTransport();
            SocketTransport client2 = new SocketTransport();
            client2.port = client.port = server.port = TestBase.NextPort();
            server.Initialize();
            Assert.IsTrue(server.StartServer());


            client.Initialize();
            Assert.IsTrue(client.StartClient());

            client2.Initialize();
            Assert.IsTrue(client2.StartClient());

            NetworkEvent @event;

            Assert.IsTrue(server.PollEvent(out @event));
            Assert.AreEqual(NetworkEventType.Connect, @event.Type);
            Assert.AreEqual(1, @event.ClientId);

            Assert.IsTrue(client.PollEvent(out @event));
            Assert.AreEqual(NetworkEventType.Connect, @event.Type);
            Assert.AreEqual(1, @event.ClientId);

            Assert.IsTrue(client2.PollEvent(out @event));
            Assert.AreEqual(NetworkEventType.Connect, @event.Type);
            Assert.AreEqual(2, @event.ClientId);

            client.DisconnectLocalClient();
            client2.DisconnectLocalClient();
            server.DisconnectLocalClient();
        }


        [Test]
        public async void DisconnectRemoteClient()
        {
            SocketTransport server = new SocketTransport();
            SocketTransport client = new SocketTransport();
            client.port = server.port = TestBase.NextPort();
            server.Initialize();
            server.StartServer();

            client.Initialize();
            client.StartClient();

            NetworkEvent @event;

            Assert.IsTrue(server.PollEvent(out @event));
            Assert.AreEqual(NetworkEventType.Connect, @event.Type);

            Assert.IsTrue(client.PollEvent(out @event));
            Assert.AreEqual(NetworkEventType.Connect, @event.Type);

            server.DisconnectRemoteClient(@event.ClientId);

            @event = await server.PollEventAsync(1f);
            Assert.AreEqual(NetworkEventType.Disconnect, @event.Type);

            @event = await client.PollEventAsync(1f);
            Assert.AreEqual(NetworkEventType.Disconnect, @event.Type);

            client.DisconnectLocalClient();
            server.DisconnectLocalClient();
        }


        [Test]
        public async void DisconnectLocalClient()
        {
            NetworkManager networkManager = new NetworkManager();
            networkManager.LogLevel = LogLevel.Debug;

            SocketTransport server = new SocketTransport();
            SocketTransport client = new SocketTransport();


            try
            {
                server.logEnabled = true;
                client.logEnabled = true;

                client.port = server.port = TestBase.NextPort();
                server.Initialize(networkManager);
                server.StartServer();

                client.Initialize(networkManager);
                client.StartClient();

                NetworkEvent @event;

                Assert.IsTrue(server.PollEvent(out @event));
                Assert.AreEqual(NetworkEventType.Connect, @event.Type);

                Assert.IsTrue(client.PollEvent(out @event));
                Assert.AreEqual(NetworkEventType.Connect, @event.Type);

                Debug.Log("DisconnectLocalClient");
                client.DisconnectLocalClient();


                @event = await server.PollEventAsync(1f);
                Assert.AreEqual(NetworkEventType.Disconnect, @event.Type);

                @event = await client.PollEventAsync(1f);
                Assert.AreEqual(NetworkEventType.Disconnect, @event.Type);
            }
            finally
            {
                Debug.Log("Shutdown");
                client.DisconnectLocalClient();
                server.DisconnectLocalClient();
            }
        }

        [Test]
        public void ServerShutdown()
        {
            NetworkManager networkManager = new NetworkManager();

            SocketTransport server = new SocketTransport();
            server.port = TestBase.NextPort();
            server.Initialize(networkManager);
            Assert.IsTrue(server.StartServer());

            Thread.Sleep(100);

            Debug.Log("Shutdown");
            server.DisconnectLocalClient();

        }


        [Test]
        public void ClientShutdown()
        {
            SocketTransport server = null;
            SocketTransport client = null;

            server = new SocketTransport();
            client = new SocketTransport();
            client.port = server.port = TestBase.NextPort();
            server.Initialize();
            server.StartServer();

            client.Initialize();
            client.StartClient();

            Thread.Sleep(100);

            Debug.Log("Shutdown");
            client.DisconnectLocalClient();
            server.DisconnectLocalClient();
        }


        [Test]
        public void ClientSocket_Close()
        {
            SocketTransport server = new SocketTransport();
            SocketTransport client = new SocketTransport();
            client.port = server.port = TestBase.NextPort();
            server.Initialize();
            server.StartServer();

            client.Initialize();
            client.StartClient();

            NetworkEvent @event;

            server.PollEvent(out @event);
            client.PollEvent(out @event);


            client.Socket.Disconnect(false);


            DateTime startTime = DateTime.Now;
            while (true)
            {
                if (server.PollEvent(out @event))
                {
                    Debug.Log("Event: " + @event.Type + ", ClientId: " + @event.ClientId + ", time: " + DateTime.Now.Subtract(startTime).TotalSeconds);
                    Assert.AreEqual(NetworkEventType.Disconnect, @event.Type);
                    Assert.AreEqual(1, @event.ClientId);
                    break;
                }
                if (DateTime.Now.Subtract(startTime).TotalSeconds > 5)
                    throw new TimeoutException();
                Thread.Sleep(100);
            }

            Assert.IsFalse(server.PollEvent(out @event));
            Assert.IsTrue(client.PollEvent(out @event));
            Assert.AreEqual(NetworkEventType.Disconnect, @event.Type);


            client.DisconnectLocalClient();
            server.DisconnectLocalClient();
        }


        [Test]
        public void ClientSocket_Close2()
        {
            SocketTransport server = new SocketTransport();
            SocketTransport client = new SocketTransport();
            SocketTransport client2 = new SocketTransport();
            client2.port = client.port = server.port = TestBase.NextPort();
            server.Initialize();
            server.StartServer();

            client.Initialize();
            client.StartClient();

            client2.Initialize();
            client2.StartClient();

            NetworkEvent @event;

            server.PollEvent(out @event);
            server.PollEvent(out @event);
            client.PollEvent(out @event);
            client2.PollEvent(out @event);


            client.Socket.Disconnect(false);


            DateTime startTime = DateTime.Now;
            while (true)
            {
                if (server.PollEvent(out @event))
                {
                    Debug.Log("Event: " + @event.Type + ", ClientId: " + @event.ClientId + ", time: " + DateTime.Now.Subtract(startTime).TotalSeconds);
                    Assert.AreEqual(NetworkEventType.Disconnect, @event.Type);
                    Assert.AreEqual(1, @event.ClientId);
                    break;
                }
                if (DateTime.Now.Subtract(startTime).TotalSeconds > 5)
                    throw new TimeoutException();
                Thread.Sleep(100);
            }

            Assert.IsFalse(server.PollEvent(out @event));
            Assert.IsTrue(client.PollEvent(out @event));
            Assert.AreEqual(NetworkEventType.Disconnect, @event.Type);
            Assert.IsFalse(client2.PollEvent(out @event));

            client.DisconnectLocalClient();
            client2.DisconnectLocalClient();
            server.DisconnectLocalClient();
        }


        [Test]
        public void ClientSocket_Close3()
        {
            SocketTransport server = new SocketTransport();
            SocketTransport client = new SocketTransport();
            SocketTransport client2 = new SocketTransport();
            client2.port = client.port = server.port = TestBase.NextPort();
            server.Initialize();
            server.StartServer();

            client.Initialize();
            client.StartClient();

            client2.Initialize();
            client2.StartClient();

            NetworkEvent @event;

            server.PollEvent(out @event);
            server.PollEvent(out @event);
            client.PollEvent(out @event);
            client2.PollEvent(out @event);

            client2.Socket.Disconnect(false);

            DateTime startTime = DateTime.Now;
            while (true)
            {
                if (server.PollEvent(out @event))
                {
                    Debug.Log("Event: " + @event.Type + ", ClientId: " + @event.ClientId + ", time: " + DateTime.Now.Subtract(startTime).TotalSeconds);
                    Assert.AreEqual(NetworkEventType.Disconnect, @event.Type);
                    Assert.AreEqual(2, @event.ClientId);
                    break;
                }
                if (DateTime.Now.Subtract(startTime).TotalSeconds > 5)
                    throw new TimeoutException();
                Thread.Sleep(100);
            }

            Assert.IsFalse(server.PollEvent(out @event));
            Assert.IsFalse(client.PollEvent(out @event));
            Assert.IsTrue(client2.PollEvent(out @event));
            Assert.AreEqual(NetworkEventType.Disconnect, @event.Type);


            client.DisconnectLocalClient();
            client2.DisconnectLocalClient();
            server.DisconnectLocalClient();
        }
    }


    static class Extensions
    {
        public static Task<NetworkEvent> PollEventAsync(this INetworkTransport transport, float timeoutSeconds)
        {
            NetworkEvent @event;
            DateTime timeout = DateTime.Now.AddSeconds(timeoutSeconds);
            while (true)
            {
                if (transport.PollEvent(out @event))
                {
                    return Task.FromResult(@event);
                }

                if (DateTime.Now > timeout)
                    throw new TimeoutException();

                Thread.Sleep(0);
            }

            throw new Exception("Not Transport event");
        }
    }
}