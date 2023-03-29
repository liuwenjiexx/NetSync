using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using UnityEditor.PackageManager;
using UnityEngine;
using static Codice.Client.Common.WebApi.WebApiEndpoints;


namespace Yanmonet.NetSync.Editor.Tests
{
    public class TransportTests
    {
        //[Test]
        //public void Host()
        //{
        //    SoketTransport server = new SoketTransport();
        //    server.StartServer();

        //    server.StartClient();

        //    ulong clientId;
        //    ArraySegment<byte> payload;
        //    float receiveTime;

        //    Assert.AreEqual(NetworkEvent.Connect, server.PollEvent(out clientId, out payload, out receiveTime));

        //    server.Shutdown();
        //}

        [Test]
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

            SoketTransport server = new SoketTransport();
            server.Initialize(serverNetworkManager);
            Assert.IsTrue(server.StartServer());

            SoketTransport client = new SoketTransport();
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

            SoketTransport server = new SoketTransport();
            server.Initialize(serverNetworkManager);
            Assert.IsTrue(server.StartServer());

            SoketTransport client = new SoketTransport();
            client.Initialize(clientNetworkManager);
            Assert.IsFalse(client.StartClient());

            Assert.AreEqual("my error", client.ConnectFailReson);

            client.Shutdown();
            server.Shutdown();
        }

        [Test]
        public void Connect()
        {
            SoketTransport server = new SoketTransport();
            server.Initialize();
            Assert.IsTrue(server.StartServer());

            SoketTransport client = new SoketTransport();
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
            SoketTransport server = new SoketTransport();
            server.Initialize();
            server.StartServer();

            SoketTransport client = new SoketTransport();
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
            SoketTransport server = new SoketTransport();
            server.Initialize();
            server.StartServer();

            SoketTransport client = new SoketTransport();
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

            SoketTransport server = new SoketTransport();
            server.Initialize(networkManager);
            Assert.IsTrue(server.StartServer());

            Thread.Sleep(100);
            server.Shutdown();
            Debug.Log("Shutdown");
        }


        [Test]
        public void ClientShutdown()
        {
            SoketTransport server = null;
            SoketTransport client = null;

            server = new SoketTransport();
            server.Initialize();
            server.StartServer();

            client = new SoketTransport();
            client.Initialize();
            client.StartClient();

            Thread.Sleep(100);
            Debug.Log("Shutdown");


            client.Shutdown();
            server.Shutdown();
        }
    }
}