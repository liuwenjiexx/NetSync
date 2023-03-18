using NUnit.Framework;
using System;
using System.Linq;


namespace Yanmonet.NetSync.Editor.Tests
{
    public class SyncVariableTests : TestBase
    {
        class MySyncVarData : NetworkObject
        {

            private NetworkVariable<string> stringVar = new NetworkVariable<string>(
                writePermission: NetworkVariableWritePermission.Server);

            private NetworkVariable<int> intVar = new NetworkVariable<int>(
                writePermission: NetworkVariableWritePermission.Server);

            private NetworkVariable<float> floatVar = new NetworkVariable<float>(
                writePermission: NetworkVariableWritePermission.Server);

            public string StringVar { get => stringVar.Value; set => stringVar.Value = value; }

            public int IntVar { get => intVar.Value; set => intVar.Value = value; }

            public float FloatVar { get => floatVar.Value; set => floatVar.Value = value; }

        }

        class ClientMySyncVarData : NetworkObject
        {

            private NetworkVariable<string> stringVar = new NetworkVariable<string>(
                writePermission: NetworkVariableWritePermission.Owner);

            private NetworkVariable<int> intVar = new NetworkVariable<int>(
                writePermission: NetworkVariableWritePermission.Owner);

            private NetworkVariable<float> floatVar = new NetworkVariable<float>(
                writePermission: NetworkVariableWritePermission.Owner);

            public string StringVar { get => stringVar.Value; set => stringVar.Value = value; }

            public int IntVar { get => intVar.Value; set => intVar.Value = value; }

            public float FloatVar { get => floatVar.Value; set => floatVar.Value = value; }

        }



        [Test]
        public void ServerVariable()
        {
            OpenNetwork<MySyncVarData>();

            var serverData = serverManager.CreateObject<MySyncVarData>();
            serverData.SpawnWithOwnership(clientManager.LocalClientId);
            serverData.AddObserver(clientManager.LocalClientId);
            Update();
            var clientData = (MySyncVarData)client.Objects.First();

            Assert.AreEqual(0, serverData.IntVar);
            Assert.AreEqual(0, clientData.IntVar);

            serverData.IntVar = 1;
            Update();
            Assert.AreEqual(1, serverData.IntVar);
            Assert.AreEqual(1, clientData.IntVar);
            CloseNetwork();
        }

        [Test]
        public void ServerVariable_ClientWriteError()
        {
            OpenNetwork<MySyncVarData>();

            var serverData = serverManager.CreateObject<MySyncVarData>();
            serverData.SpawnWithOwnership(clientManager.LocalClientId);
            serverData.AddObserver(clientManager.LocalClientId);
            Update();
            var clientData = (MySyncVarData)client.Objects.First();

            Assert.Throws<InvalidOperationException>(() =>
            {
                clientData.IntVar = 1;
            });
            CloseNetwork();
        }

        [Test]
        public void ClientVariable()
        {
            OpenNetwork<ClientMySyncVarData>();

            var serverData = serverManager.CreateObject<ClientMySyncVarData>();
            serverData.SpawnWithOwnership(clientManager.LocalClientId);
            Update();
            var clientData = (ClientMySyncVarData)client.Objects.First();

            Assert.AreEqual(0, serverData.IntVar);
            Assert.AreEqual(0, clientData.IntVar);

            clientData.IntVar = 1;
            Update();
            Update();
            Update();
            Assert.AreEqual(1, clientData.IntVar);
            Assert.AreEqual(1, serverData.IntVar);
            CloseNetwork();
        }
        [Test]
        public void ClientVariable_ServerWriteError()
        {
            OpenNetwork<ClientMySyncVarData>();

            var serverData = serverManager.CreateObject<ClientMySyncVarData>();
            serverData.SpawnWithOwnership(clientManager.LocalClientId);
            Update();

            Assert.Throws<InvalidOperationException>(() => serverData.IntVar = 1);

            CloseNetwork();
        }


        [Test]
        public void SyncVar()
        {
            OpenNetwork<MySyncVarData>();

            var serverData = server.CreateObject<MySyncVarData>();
            serverData.Spawn();
            serverManager.Server.AddObserver(serverData, clientManager.LocalClientId);
            Update(serverManager, clientManager);
            var clientData = (MySyncVarData)client.Objects.FirstOrDefault();


            serverData.IntVar = 1;

            Update(serverManager, clientManager);

            Assert.AreEqual(1, serverData.IntVar);
            Assert.AreEqual(1, clientData.IntVar);

            serverData.StringVar = "abc";

            Update(serverManager, clientManager);
            Assert.AreEqual("abc", clientData.StringVar);

            serverData.FloatVar = 0.1f;
            Update(serverManager, clientManager);

            Assert.AreEqual(clientData.FloatVar, serverData.FloatVar);

            Update(serverManager, clientManager);

            CloseNetwork();
        }

        [Test]
        public void SyncVar_Int32_1()
        {
            OpenNetwork<MySyncVarData>();

            var serverData = server.CreateObject<MySyncVarData>();
            serverData.Spawn();
            serverManager.Server.AddObserver(serverData, clientManager.LocalClientId);

            serverData.IntVar = 1;
            Update(serverManager, clientManager);
            var clientData = (MySyncVarData)client.Objects.FirstOrDefault();

            Update(serverManager, clientManager);
            Assert.AreEqual(clientData.IntVar, serverData.IntVar);

            CloseNetwork();
        }
        [Test]
        public void SyncVar_Int32_2()
        {
            OpenNetwork<MySyncVarData>();

            var serverData = server.CreateObject<MySyncVarData>();
            serverData.IntVar = 1;
            serverManager.Server.AddObserver(serverData, clientManager.LocalClientId);
            serverData.Spawn();
            Update(serverManager, clientManager);
            var clientData = (MySyncVarData)client.Objects.FirstOrDefault();

            Update(serverManager, clientManager);
            Assert.AreEqual(clientData.IntVar, serverData.IntVar);

            CloseNetwork();
        }

    }
}