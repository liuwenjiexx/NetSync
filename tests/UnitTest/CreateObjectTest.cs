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
                 
                clientData.SpawnWithOwnership(serverManager.clientIds.First());
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
