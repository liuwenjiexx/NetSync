using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Yanmonet.NetSync.Test
{
    [TestClass]
    public class CreateObjectTest : TestBase
    {

        [TestMethod]
        public void Spawn()
        {
            NetworkManager serverManager = new NetworkManager();
            NetworkManager clientManager = new NetworkManager();
            try
            {
                serverManager.RegisterObject<MySyncVarData>((id) =>
                {
                    return new MySyncVarData();
                });
                clientManager.RegisterObject<MySyncVarData>((id) =>
                {
                    return new MySyncVarData();
                });
                serverManager.StartServer();
                clientManager.StartClient();
                var server = serverManager.Server;
                var client = clientManager.LocalClient;

                Update(serverManager, clientManager);

                Assert.AreEqual(client.Objects.Count(), 0);
                Assert.AreEqual(server.Objects.Count(), 0);

                var serverData = serverManager.CreateObject<MySyncVarData>();
                Assert.IsNotNull(serverData);
                Assert.IsTrue(serverData.IsOwner);
                Assert.IsTrue(serverData.IsOwnedByServer);

                Update(serverManager, clientManager);

                Assert.AreEqual(0, client.Objects.Count());

                serverData.Spawn(serverManager.clientIds.First());
                Assert.IsTrue(serverData.IsSpawned);
                Update(serverManager, clientManager);

                var clientData = client.Objects.FirstOrDefault();
                Assert.IsNotNull(clientData);
                Assert.IsTrue(clientData.IsSpawned);
                Assert.IsTrue(clientData.IsOwner);
                Assert.IsFalse(clientData.IsOwnedByServer);
                Assert.AreEqual(server.Objects.Count(), 1);
                Assert.AreEqual(clientData.InstanceId, serverData.InstanceId);
                Assert.IsFalse(object.ReferenceEquals(clientData, serverData));
            }
            finally
            {
                clientManager.Dispose();
                serverManager.Dispose();
            }
        }




        [TestMethod]
        public void Despawn()
        {

            NetworkManager serverManager = new NetworkManager();
            NetworkManager clientManager = new NetworkManager();

            try
            {
                serverManager.RegisterObject<MySyncVarData>((id) =>
                {
                    return new MySyncVarData();
                });
                clientManager.RegisterObject<MySyncVarData>((id) =>
                {
                    return new MySyncVarData();
                });

                serverManager.StartServer();
                clientManager.StartClient();
                Update(serverManager, clientManager);

                var server = serverManager.Server;
                var client = clientManager.LocalClient;
                 
                var serverData = serverManager.CreateObject<MySyncVarData>();
                serverData.Spawn(serverManager.clientIds.First());
                Update(serverManager, clientManager);

                serverData.Despawn();
                Update(serverManager, clientManager);

                Assert.AreEqual(server.Objects.Count(), 0);
                Assert.AreEqual(client.Objects.Count(), 0);
            }
            finally
            {
                clientManager.Dispose();
                serverManager.Dispose();
            }

        }


    //    [TestMethod]
        public void HostObject()
        {

            NetworkManager serverManager = new NetworkManager();
            NetworkManager clientManager = new NetworkManager();

            try
            {

                serverManager.RegisterObject<MySyncVarData>((id) =>
                {
                    return new MySyncVarData();
                });
                clientManager.RegisterObject<MySyncVarData>((id) =>
            {
                return new MySyncVarData();
            });

                serverManager.StartServer();
                clientManager.StartClient();

                var server = serverManager.Server;
                var client = clientManager.LocalClient;

                Update(serverManager, clientManager);

                Assert.AreEqual(client.Objects.Count(), 0);
                Assert.AreEqual(server.Objects.Count(), 0);

                var serverData = serverManager.CreateObject<MySyncVarData>();
                Assert.AreEqual(server.Objects.Count(), 1);
                Assert.IsNotNull(serverData);

                Update(serverManager, clientManager);

                var clientData = client.Objects.FirstOrDefault();
                Assert.IsNull(clientData);
                 
                clientData.Spawn(serverManager.clientIds.First());
                Update(serverManager, clientManager);
                clientData = client.Objects.FirstOrDefault();

                Assert.IsTrue(object.ReferenceEquals(clientData, serverData));

                serverData.Despawn();
                Assert.AreEqual(server.Objects.Count(), 0);


                Update(serverManager, clientManager);
                Assert.AreEqual(client.Objects.Count(), 0);
            }
            finally
            {
                clientManager.Dispose();
                serverManager.Dispose();
            }

        }


    }
}
