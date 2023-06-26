using CodiceApp.EventTracking;
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
using static Codice.Client.Common.WebApi.WebApiEndpoints;

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



        void WaitConnected(SocketTransport server, int timeout = 1000)
        {
            DateTime time = DateTime.Now.AddMilliseconds(timeout);
            while (true)
            {
                if (server.clients.Count > 0)
                    break;
                if (time < DateTime.Now)
                    throw new TimeoutException();
                Thread.Sleep(5);
            }
        }


        [Test]
        public void Connect()
        {
            SocketTransport server = new SocketTransport();
            SocketTransport client = new SocketTransport();
            client.port = server.port = TestBase.NextPort();
            server.Initialize();
            server.StartServer();


            client.Initialize();
            client.StartClient();

            NetworkEvent @event;

            @event = server.PollEvent();
            Assert.AreEqual(NetworkEventType.Connect, @event.Type);
            Assert.AreEqual(1, @event.ClientId);

            @event = client.PollEvent();
            Assert.AreEqual(NetworkEventType.Connect, @event.Type);
            Assert.AreEqual(1, @event.ClientId);


            client.Shutdown();
            server.Shutdown();
        }

        [Test]
        public void Connect2()
        {
            SocketTransport server = new SocketTransport();
            SocketTransport client = new SocketTransport();
            SocketTransport client2 = new SocketTransport();
            client2.port = client.port = server.port = TestBase.NextPort();
            server.Initialize();
            server.StartServer();


            client.Initialize();
            client.StartClient();

            Thread.Sleep(5);

            client2.Initialize();
            client2.StartClient();

            NetworkEvent @event;

            @event = server.PollEvent();
            Assert.AreEqual(NetworkEventType.Connect, @event.Type);
            Assert.AreEqual(1, @event.ClientId);

            @event = client.PollEvent();
            Assert.AreEqual(NetworkEventType.Connect, @event.Type);
            Assert.AreEqual(1, @event.ClientId);


            @event = client2.PollEvent();
            Assert.AreEqual(NetworkEventType.Connect, @event.Type);
            Assert.AreEqual(2, @event.ClientId);

            client.Shutdown();
            client2.Shutdown();
            server.Shutdown();
        }


        [Test]
        public void NotConnect()
        {
            SocketTransport client = new SocketTransport();
            client.port = short.MaxValue;

            client.Initialize();
            client.StartClient();

            NetworkEvent @event;

            //@event = client.PollEvent(1000 * 10);
            @event = client.PollEvent(1000 * 10);
            Assert.AreEqual(NetworkEventType.Disconnect, @event.Type);
            client.Shutdown();
        }

        [Test]
        public void DisconnectLocalClient()
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

                @event = server.PollEvent();
                @event = client.PollEvent();


                Debug.Log("DisconnectLocalClient");
                client.DisconnectLocalClient();


                @event = server.PollEvent();
                Assert.AreEqual(NetworkEventType.Disconnect, @event.Type);

                @event = client.PollEvent();
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
        public void DisconnectRemoteClient()
        {
            SocketTransport server = new SocketTransport();
            SocketTransport client = new SocketTransport();
            client.port = server.port = TestBase.NextPort();
            server.Initialize();
            server.StartServer();

            client.Initialize();
            client.StartClient();
            NetworkEvent @event;

            client.PollEvent();
            @event = server.PollEvent();

            server.DisconnectRemoteClient(@event.ClientId);

            @event = server.PollEvent();
            Assert.AreEqual(NetworkEventType.Disconnect, @event.Type);

            @event = client.PollEvent();
            Assert.AreEqual(NetworkEventType.Disconnect, @event.Type);

            client.DisconnectLocalClient();
            server.DisconnectLocalClient();
        }


        [Test]
        public void ServerShutdown()
        {
            NetworkManager networkManager = new NetworkManager();

            SocketTransport server = new SocketTransport();
            server.port = TestBase.NextPort();
            server.Initialize(networkManager);
            server.StartServer();

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

            WaitConnected(server);

            Thread.Sleep(100);

            Debug.Log("Shutdown");
            client.DisconnectLocalClient();
            server.DisconnectLocalClient();
        }


        [Test]
        public void ClientSocketClose_ClientDisconnect()
        {
            SocketTransport server = new SocketTransport();
            SocketTransport client = new SocketTransport();
            client.port = server.port = TestBase.NextPort();
            server.Initialize();
            server.StartServer();

            client.Initialize();
            client.StartClient();

            NetworkEvent @event;

            @event = client.PollEvent();
            @event = server.PollEvent();

            client.Socket.Disconnect(false);

            @event = client.PollEvent();
            Assert.AreEqual(NetworkEventType.Disconnect, @event.Type);


            client.Shutdown();
            server.Shutdown();
        }

        //[Test]
        //public void ClientSocketClose_ServerDisconnect()
        //{
        //    SocketTransport server = new SocketTransport();
        //    SocketTransport client = new SocketTransport();
        //    client.port = server.port = TestBase.NextPort();
        //    server.Initialize();
        //    server.StartServer();

        //    client.Initialize();
        //    client.StartClient();

        //    NetworkEvent @event;

        //    @event = client.PollEvent();
        //    @event = server.PollEvent();

        //    client.Socket.Disconnect(false);

        //    @event= client.PollEvent();
        //    Assert.AreEqual(NetworkEventType.Disconnect, @event.Type);

        //    DateTime startTime = DateTime.Now;
        //    while (true)
        //    {

        //        if (server.PollEvent(out @event))
        //        {
        //            Debug.Log("Event: " + @event.Type + ", ClientId: " + @event.ClientId + ", time: " + DateTime.Now.Subtract(startTime).TotalSeconds);
        //            Assert.AreEqual(NetworkEventType.Disconnect, @event.Type);
        //            Assert.AreEqual(1, @event.ClientId);
        //            break;
        //        }
        //        if (DateTime.Now.Subtract(startTime).TotalSeconds > 5)
        //            throw new TimeoutException();
        //        Thread.Sleep(100);
        //    }

        //    Assert.IsFalse(server.PollEvent(out @event));
        //    Assert.IsTrue(client.PollEvent(out @event));
        //    Assert.AreEqual(NetworkEventType.Disconnect, @event.Type);


        //    client.DisconnectLocalClient();
        //    server.DisconnectLocalClient();
        //}


        //[Test]
        //public void ClientSocket_Close2()
        //{
        //    NetworkEvent @event;
        //    SocketTransport server = new SocketTransport();
        //    SocketTransport client = new SocketTransport();
        //    SocketTransport client2 = new SocketTransport();
        //    client2.port = client.port = server.port = TestBase.NextPort();
        //    server.Initialize();
        //    server.StartServer();

        //    client.Initialize();
        //    client.StartClient();
        //    @event = client.PollEvent();

        //    client2.Initialize();
        //    client2.StartClient();
        //    @event = client2.PollEvent();



        //    server.PollEvent(out @event);
        //    server.PollEvent(out @event);

        //    client.Socket.Disconnect(false);


        //    DateTime startTime = DateTime.Now;
        //    while (true)
        //    {
        //        if (server.PollEvent(out @event))
        //        {
        //            Debug.Log("Event: " + @event.Type + ", ClientId: " + @event.ClientId + ", time: " + DateTime.Now.Subtract(startTime).TotalSeconds);
        //            Assert.AreEqual(NetworkEventType.Disconnect, @event.Type);
        //            Assert.AreEqual(1, @event.ClientId);
        //            break;
        //        }
        //        if (DateTime.Now.Subtract(startTime).TotalSeconds > 5)
        //            throw new TimeoutException();
        //        Thread.Sleep(100);
        //    }

        //    Assert.IsFalse(server.PollEvent(out @event));
        //    Assert.IsTrue(client.PollEvent(out @event));
        //    Assert.AreEqual(NetworkEventType.Disconnect, @event.Type);
        //    Assert.IsFalse(client2.PollEvent(out @event));

        //    client.DisconnectLocalClient();
        //    client2.DisconnectLocalClient();
        //    server.DisconnectLocalClient();
        //}


        //[Test]
        //public void ClientSocket_Close3()
        //{
        //    SocketTransport server = new SocketTransport();
        //    SocketTransport client = new SocketTransport();
        //    SocketTransport client2 = new SocketTransport();
        //    client2.port = client.port = server.port = TestBase.NextPort();
        //    server.Initialize();
        //    server.StartServer();

        //    client.Initialize();
        //    client.StartClient();

        //    client2.Initialize();
        //    client2.StartClient();

        //    NetworkEvent @event;

        //    @event = client.PollEvent();
        //    @event = client2.PollEvent();

        //    server.PollEvent(out @event);
        //    server.PollEvent(out @event);


        //    client2.Socket.Disconnect(false);

        //    DateTime startTime = DateTime.Now;
        //    while (true)
        //    {
        //        if (server.PollEvent(out @event))
        //        {
        //            Debug.Log("Event: " + @event.Type + ", ClientId: " + @event.ClientId + ", time: " + DateTime.Now.Subtract(startTime).TotalSeconds);
        //            Assert.AreEqual(NetworkEventType.Disconnect, @event.Type);
        //            Assert.AreEqual(2, @event.ClientId);
        //            break;
        //        }
        //        if (DateTime.Now.Subtract(startTime).TotalSeconds > 5)
        //            throw new TimeoutException();
        //        Thread.Sleep(100);
        //    }

        //    Assert.IsFalse(server.PollEvent(out @event));
        //    Assert.IsFalse(client.PollEvent(out @event));
        //    Assert.IsTrue(client2.PollEvent(out @event));
        //    Assert.AreEqual(NetworkEventType.Disconnect, @event.Type);


        //    client.DisconnectLocalClient();
        //    client2.DisconnectLocalClient();
        //    server.DisconnectLocalClient();
        //}
    }


    static class Extensions
    {
        /*  public static Task<NetworkEvent> PollEventAsync(this INetworkTransport transport, float timeoutSeconds)
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

                  Thread.Sleep(5);
              }

              throw new Exception("Not Transport event");
          }*/

        public static NetworkEvent PollEvent(this INetworkTransport transport, int timeout = 1000)
        {
            NetworkEvent @event;
            DateTime time = DateTime.Now.AddMilliseconds(timeout);
            while (true)
            {
                if (transport.PollEvent(out @event))
                    break;
                if (time < DateTime.Now)
                    throw new TimeoutException();
                Thread.Sleep(5);
            }
            return @event;
        }

    }
}