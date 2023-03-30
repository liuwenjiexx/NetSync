using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using UnityEngine;
using Yanmonet.NetSync.Transport.Socket;



namespace Yanmonet.NetSync.Editor.Tests
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
            server.Initialize();
            Assert.IsTrue(server.StartServer());

            SocketTransport client = new SocketTransport();
            client.Initialize();
            Assert.IsTrue(client.StartClient());

            NetworkEvent @event;

            Assert.IsTrue(server.PollEvent(out @event));
            Assert.AreEqual(NetworkEventType.Connect, @event.Type);
            Assert.AreEqual(1,@event.ClientId);
            Assert.IsTrue(client.PollEvent(out @event));
            Assert.AreEqual(NetworkEventType.Connect, @event.Type);
            Assert.AreEqual(1, @event.ClientId);

            client.Shutdown();
            server.Shutdown();
        }

        [Test]
        public void DisconnectRemoteClient()
        {
            SocketTransport server = new SocketTransport();
            server.Initialize();
            server.StartServer();

            SocketTransport client = new SocketTransport();
            client.Initialize();
            client.StartClient();

            NetworkEvent @event;

            Assert.IsTrue(server.PollEvent(out @event));
            Assert.AreEqual(NetworkEventType.Connect, @event.Type);

            Assert.IsTrue(client.PollEvent(out @event));
            Assert.AreEqual(NetworkEventType.Connect, @event.Type);

            server.DisconnectRemoteClient(@event.ClientId);
      
            Assert.IsTrue(server.PollEvent(out @event));
            Assert.AreEqual(NetworkEventType.Disconnect, @event.Type);

            Assert.IsTrue(client.PollEvent(out @event));
            Assert.AreEqual(NetworkEventType.Disconnect, @event.Type);

            client.Shutdown();
            server.Shutdown();
        }

        [Test]
        public void DisconnectLocalClient()
        {
            SocketTransport server = new SocketTransport();
            server.Initialize();
            server.StartServer();

            SocketTransport client = new SocketTransport();
            client.Initialize();
            client.StartClient();

            NetworkEvent @event;

            Assert.IsTrue(server.PollEvent(out @event));
            Assert.AreEqual(NetworkEventType.Connect, @event.Type);

            Assert.IsTrue(client.PollEvent(out @event));
            Assert.AreEqual(NetworkEventType.Connect, @event.Type);

            client.DisconnectLocalClient();

            Assert.IsTrue(server.PollEvent(out @event));
            Assert.AreEqual(NetworkEventType.Disconnect, @event.Type);

            Assert.IsTrue(client.PollEvent(out @event));
            Assert.AreEqual(NetworkEventType.Disconnect, @event.Type);

            client.Shutdown();
            server.Shutdown();
        }

        [Test]
        public void ServerShutdown()
        {
            NetworkManager networkManager = new NetworkManager();

            SocketTransport server = new SocketTransport();
            server.Initialize(networkManager);
            Assert.IsTrue(server.StartServer());

            Thread.Sleep(100);
            server.Shutdown();
            Debug.Log("Shutdown");
        }


        [Test]
        public void ClientShutdown()
        {
            SocketTransport server = null;
            SocketTransport client = null;

            server = new SocketTransport();
            server.Initialize();
            server.StartServer();

            client = new SocketTransport();
            client.Initialize();
            client.StartClient();

            Thread.Sleep(100);
            Debug.Log("Shutdown");


            client.Shutdown();
            server.Shutdown();
        }
    }
}