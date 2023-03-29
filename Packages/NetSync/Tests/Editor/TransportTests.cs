using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Yanmonet.NetSync.Editor.Tests
{
    public class TransportTests
    {
        [Test]
        public void Host()
        {
            SoketTransport server = new SoketTransport();
            server.StartServer();

            server.StartClient();

            ulong clientId;
            ArraySegment<byte> payload;
            float receiveTime;

            Assert.AreEqual(NetworkEvent.Connect, server.PollEvent(out clientId, out payload, out receiveTime));

        }

        [Test]
        public void Connect()
        {
            NetworkManager networkManager = new NetworkManager();
            NetworkManager networkManager2 = new NetworkManager();

            SoketTransport server = new SoketTransport();
            server.Initialize(networkManager);
            Assert.IsTrue(server.StartServer());

            SoketTransport client = new SoketTransport();
            client.Initialize(networkManager2);
            Assert.IsTrue(client.StartClient());
             
            ulong clientId;
            ArraySegment<byte> payload;
            float receiveTime;

            Assert.AreEqual(NetworkEvent.Connect, server.PollEvent(out clientId, out payload, out receiveTime));
            Assert.AreEqual(1, clientId);
        }

        public void AAA()
        {

        }

    }
}