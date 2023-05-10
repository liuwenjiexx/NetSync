using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Yanmonet.Network.Sync.Editor.Tests;
namespace Yanmonet.Network.Sync.Editor.Tests
{
    public class CreateObjectTests : TestBase
    {
        class TestObject : NetworkObject
        {

            private Sync<int> serverVar = new Sync<int>(
                writePermission: SyncWritePermission.Server);

            public int ServerVar { get => serverVar.Value; set => serverVar.Value = value; }

            public bool isOnSpawned;
            public bool isOnDespawned;
            public bool isOnDestrory;
            protected internal override void OnSpawned()
            {
                isOnSpawned = true;
            }

            protected internal override void OnDespawned()
            {
                isOnDespawned = true;
            }
            internal protected override void OnDestrory()
            {
                isOnDestrory = true;
            }
        }

        [Test]
        [OpenNetwork]
        public void Spawn()
        {
            Assert.AreEqual(client.SpawnedObjects.Count(), 0);
            Assert.AreEqual(server.SpawnedObjects.Count(), 0);

            var serverData = serverManager.CreateObject<TestObject>();
            Assert.IsNotNull(serverData);

            Update(serverManager, clientManager);

            Assert.AreEqual(0, client.SpawnedObjects.Count());

            serverData.Spawn();
            Assert.IsTrue(serverData.IsSpawned);
            Assert.AreEqual(NetworkManager.ServerClientId, serverData.OwnerClientId);
            Assert.IsTrue(serverData.IsOwnedByServer);
            Assert.AreEqual(1, server.SpawnedObjects.Count());
            Assert.AreEqual(0, client.SpawnedObjects.Count());

            Assert.IsTrue(serverData.isOnSpawned);
            Assert.IsFalse(serverData.isOnDespawned);

            serverData.AddObserver(client.LocalClientId);
            Update(serverManager, clientManager);

            Assert.AreEqual(1, client.SpawnedObjects.Count());
            var clientData = client.SpawnedObjects.FirstOrDefault() as TestObject;
            Assert.IsNotNull(clientData);
            Assert.IsTrue(clientData.IsSpawned);
            Assert.IsFalse(clientData.IsOwner);
            Assert.IsTrue(clientData.IsOwnedByServer);
            Assert.AreEqual(NetworkManager.ServerClientId, clientData.OwnerClientId);
            Assert.AreEqual(server.SpawnedObjects.Count(), 1);
            Assert.AreEqual(clientData.InstanceId, serverData.InstanceId);
            Assert.IsFalse(object.ReferenceEquals(clientData, serverData));

            Assert.IsTrue(clientData.isOnSpawned);
            Assert.IsFalse(clientData.isOnDespawned);
        }

        [Test]
        [OpenNetwork]
        public void SpawnWithOwnership()
        {
            Assert.AreEqual(client.SpawnedObjects.Count(), 0);
            Assert.AreEqual(server.SpawnedObjects.Count(), 0);

            var serverData = serverManager.CreateObject<TestObject>();
            Assert.IsNotNull(serverData);

            Update(serverManager, clientManager);

            Assert.AreEqual(0, client.SpawnedObjects.Count());

            serverData.SpawnWithOwnership(serverManager.clientIds.First());
            Assert.IsTrue(serverData.IsSpawned);
            Assert.IsFalse(serverData.IsOwnedByServer);
            Assert.AreNotEqual(NetworkManager.ServerClientId, serverData.OwnerClientId);

            Update(serverManager, clientManager);

            var clientData = client.SpawnedObjects.FirstOrDefault();
            Assert.IsNotNull(clientData);
            Assert.IsTrue(clientData.IsSpawned);
            Assert.IsTrue(clientData.IsOwner);
            Assert.IsFalse(clientData.IsOwnedByServer);
            Assert.AreEqual(client.LocalClientId, clientData.OwnerClientId);
            Assert.AreEqual(server.SpawnedObjects.Count(), 1);
            Assert.AreEqual(clientData.InstanceId, serverData.InstanceId);
            Assert.IsFalse(object.ReferenceEquals(clientData, serverData));

        }


        [Test]
        [OpenNetwork]
        public void Despawn()
        {
            var serverData = serverManager.CreateObject<TestObject>();
            serverData.Spawn();
            serverData.AddObserver(clientManager.LocalClientId);
            Update(serverManager, clientManager);
            var clientData = client.SpawnedObjects.FirstOrDefault() as TestObject;


            serverData.Despawn();
            Update(serverManager, clientManager);

            Assert.AreEqual(0, server.SpawnedObjects.Count());
            Assert.AreEqual(0, client.SpawnedObjects.Count());

            Assert.IsTrue(serverData.isOnDespawned);
            Assert.IsTrue(serverData.isOnDestrory);
            Assert.IsTrue(clientData.isOnDespawned);
            Assert.IsTrue(clientData.isOnDestrory);
        }

        [Test]
        [OpenNetwork]
        public void DespawnNotDestrory()
        {
            var serverData = serverManager.CreateObject<TestObject>();
            serverData.SpawnWithOwnership(serverManager.clientIds.First());
            Update(serverManager, clientManager);
            var clientData = client.SpawnedObjects.FirstOrDefault() as TestObject;

            serverData.Despawn(false);
            Update(serverManager, clientManager);

            Assert.AreEqual(0, server.SpawnedObjects.Count());
            Assert.AreEqual(0, client.SpawnedObjects.Count());

            Assert.IsTrue(serverData.isOnDespawned);
            Assert.IsFalse(serverData.isOnDestrory);
            Assert.IsTrue(clientData.isOnDespawned);
            Assert.IsFalse(clientData.isOnDestrory);
        }

        public void AddObserver()
        {

        }

    }
}
