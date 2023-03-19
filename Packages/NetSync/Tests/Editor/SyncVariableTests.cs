using NUnit.Framework;
using System;
using System.Linq;
using UnityEngine;

namespace Yanmonet.NetSync.Editor.Tests
{
    public class SyncVariableTests : TestBase
    {
        class ServerVarData : NetworkObject
        {
            private Sync<int> intVar = new Sync<int>(writePermission: SyncWritePermission.Server);
            private Sync<float> floatVar = new Sync<float>(writePermission: SyncWritePermission.Server);
            private Sync<string> stringVar = new Sync<string>(writePermission: SyncWritePermission.Server);

            public int IntVar { get => intVar.Value; set => intVar.Value = value; }
            public float FloatVar { get => floatVar.Value; set => floatVar.Value = value; }
            public string StringVar { get => stringVar.Value; set => stringVar.Value = value; }
        }

        class OwnerVarData : NetworkObject
        {
            private Sync<int> intVar = new Sync<int>(writePermission: SyncWritePermission.Owner);
            private Sync<float> floatVar = new Sync<float>(writePermission: SyncWritePermission.Owner);
            private Sync<string> stringVar = new Sync<string>(writePermission: SyncWritePermission.Owner);

            public int IntVar { get => intVar.Value; set => intVar.Value = value; }
            public float FloatVar { get => floatVar.Value; set => floatVar.Value = value; }
            public string StringVar { get => stringVar.Value; set => stringVar.Value = value; }
        }


        class ClientMySyncVarData : NetworkObject
        {

            private Sync<string> stringVar = new Sync<string>(
                writePermission: SyncWritePermission.Owner);

            private Sync<int> intVar = new Sync<int>(
                writePermission: SyncWritePermission.Owner);

            private Sync<float> floatVar = new Sync<float>(
                writePermission: SyncWritePermission.Owner);

            public string StringVar { get => stringVar.Value; set => stringVar.Value = value; }

            public int IntVar { get => intVar.Value; set => intVar.Value = value; }

            public float FloatVar { get => floatVar.Value; set => floatVar.Value = value; }

        }



        [Test]
        [OpenNetwork]
        public void ServerVariable()
        {
            var serverData = serverManager.CreateObject<OwnerVarData>();
            serverData.Spawn();
            serverData.AddObserver(clientManager.LocalClientId);
            Update();
            var clientData = (OwnerVarData)client.Objects.First();

            Assert.AreEqual(0, serverData.IntVar);
            Assert.AreEqual(0, clientData.IntVar);

            serverData.IntVar = 1;
            Update();
            Assert.AreEqual(1, serverData.IntVar);
            Assert.AreEqual(1, clientData.IntVar);
        }

        [Test]
        [OpenNetwork]
        public void ClientVariable()
        {
            var serverData = serverManager.CreateObject<OwnerVarData>();
            serverData.SpawnWithOwnership(clientManager.LocalClientId);
            Update();
            var clientData = (OwnerVarData)client.Objects.First();

            Assert.AreEqual(0, serverData.IntVar);
            Assert.AreEqual(0, clientData.IntVar);

            clientData.IntVar = 1;
            Update();
            Assert.AreEqual(1, clientData.IntVar);
            Assert.AreEqual(1, serverData.IntVar);
        }

        [Test]
        [OpenNetwork]
        public void ClientWriteError()
        {
            var serverData = serverManager.CreateObject<OwnerVarData>();
            serverData.Spawn();
            serverData.AddObserver(clientManager.LocalClientId);
            Update();
            var clientData = (OwnerVarData)client.Objects.First();

            Assert.Throws<InvalidOperationException>(() =>
            {
                clientData.IntVar = 1;
            });
        }

        [Test]
        [OpenNetwork]
        public void ServerWriteError()
        {
            var serverData = serverManager.CreateObject<OwnerVarData>();
            serverData.SpawnWithOwnership(clientManager.LocalClientId);
            Update();

            Assert.Throws<InvalidOperationException>(() => serverData.IntVar = 1);
        }


        [Test]
        [OpenNetwork]
        public void PrimitiveTypes_Server()
        {
            var serverData = server.CreateObject<OwnerVarData>();
            serverData.Spawn();
            serverData.AddObserver(clientManager.LocalClientId);
            Update();
            var clientData = (OwnerVarData)client.Objects.FirstOrDefault();

            serverData.IntVar = 1;
            Update();

            Assert.AreEqual(1, serverData.IntVar);
            Assert.AreEqual(1, clientData.IntVar);


            serverData.FloatVar = 1.2f;
            Update();

            Assert.AreEqual(1.2f, serverData.FloatVar);
            Assert.AreEqual(1.2f, clientData.FloatVar);
        }

        [Test]
        [OpenNetwork]
        public void PrimitiveTypes_Client()
        {
            var serverData = server.CreateObject<OwnerVarData>();
            serverData.SpawnWithOwnership(clientManager.LocalClientId);

            Update();
            var clientData = (OwnerVarData)client.Objects.FirstOrDefault();

            clientData.IntVar = 1;
            Update();
            Assert.AreEqual(1, serverData.IntVar);
            Assert.AreEqual(1, clientData.IntVar);

            clientData.FloatVar = 1.2f;
            Update();
            Assert.AreEqual(1.2f, serverData.FloatVar);
            Assert.AreEqual(1.2f, clientData.FloatVar);

        }

        [Test]
        [OpenNetwork]
        public void String_Server()
        {
            var serverData = server.CreateObject<OwnerVarData>();
            serverData.Spawn();
            serverData.AddObserver(clientManager.LocalClientId);
            Update();
            var clientData = (OwnerVarData)client.Objects.FirstOrDefault();

            serverData.StringVar = "abc";

            Update();
            Assert.AreEqual("abc", serverData.StringVar);
            Assert.AreEqual("abc", clientData.StringVar);
        }


        [Test]
        [OpenNetwork]
        public void String_Client()
        {
            var serverData = server.CreateObject<OwnerVarData>();
            serverData.SpawnWithOwnership(clientManager.LocalClientId);

            Update();
            var clientData = (OwnerVarData)client.Objects.FirstOrDefault();
            clientData.StringVar = "abc";

            Update();
            Assert.AreEqual("abc", serverData.StringVar);
            Assert.AreEqual("abc", clientData.StringVar);
        }


    }
}