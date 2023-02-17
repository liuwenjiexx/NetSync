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
        public void CreateObject()
        {
            using (MyServer server = new MyServer())
            {
                server.Start(localPort);

                using (MyClient client = new MyClient())
                {
                    client.Connect(localAddress, localPort);

                    Update2(server, client);

                    NetworkClient.RegisterObject<MySyncVarData>((id) =>
                    {
                        return new MySyncVarData();
                    });
                    Assert.AreEqual(client.Objects.Count(), 0);
                    Assert.AreEqual(server.Objects.Count(), 0);

                    var serverData = server.CreateObject<MySyncVarData>();
                    Assert.IsNotNull(serverData);
                    Assert.AreEqual(server.Objects.Count(), 1);
                    Assert.IsTrue(serverData.IsServer);
                    Assert.IsFalse(serverData.IsClient);
                    Update2(server, client);

                    var clientData = client.Objects.FirstOrDefault();
                    Assert.IsNull(clientData);
                    server.AddObserver(serverData, server.Connections.First());
                    Update2(server, client);
                    clientData = client.Objects.FirstOrDefault();

                    Assert.IsNotNull(clientData);
                    Assert.IsFalse(clientData.IsServer);
                    Assert.IsTrue(clientData.IsClient);
                    Assert.AreEqual(clientData.InstanceId, serverData.InstanceId);
                    Assert.IsFalse(object.ReferenceEquals(clientData, serverData));
                    Assert.AreEqual(clientData, serverData);

                    server.DestroyObject(serverData);
                    Assert.AreEqual(server.Objects.Count(), 0);
                    Update2(server, client);
                    Assert.AreEqual(client.Objects.Count(), 0);

                }
            }
        }



        [TestMethod]
        public void DestroyObject()
        {

            using (MyServer server = new MyServer())
            {
                server.Start(localPort);

                using (MyClient client = new MyClient())
                {
                    client.Connect(localAddress, localPort);

                    NetworkClient.RegisterObject<MySyncVarData>((id) =>
                    {
                        return new MySyncVarData();
                    });
                    Assert.AreEqual(client.Objects.Count(), 0);
                    Assert.AreEqual(server.Objects.Count(), 0);

                    var serverData = server.CreateObject<MySyncVarData>();
                    Update2(server, client);

                    server.AddObserver(serverData, server.Connections.First());
                    Update2(server, client);

                    server.DestroyObject(serverData);
                    Assert.AreEqual(server.Objects.Count(), 0);

                    Update2(server, client);
                    Assert.AreEqual(client.Objects.Count(), 0);
                }
            }
        }


        [TestMethod]
        public void HostObject()
        { 

            using (MyServer server = new MyServer())
            {
                server.Start(localPort);

                using (MyClient client = new MyClient(server, null, true, false))
                {
                    client.Connect(localAddress, localPort);

                    Update2(server, client);

                    NetworkClient.RegisterObject<MySyncVarData>((id) =>
                    {
                        return new MySyncVarData();
                    });
                    Assert.AreEqual(client.Objects.Count(), 0);
                    Assert.AreEqual(server.Objects.Count(), 0);

                    var serverData = server.CreateObject<MySyncVarData>();
                    Assert.AreEqual(server.Objects.Count(), 1);
                    Assert.IsNotNull(serverData);
                    Update2(server, client);

                    var clientData = client.Objects.FirstOrDefault();
                    Assert.IsNull(clientData);

                    server.AddObserver(serverData, server.Connections.First());
                    Update2(server, client);
                    clientData = client.Objects.FirstOrDefault();

                    Assert.IsTrue(object.ReferenceEquals(clientData, serverData));

                    server.DestroyObject(serverData);
                    Assert.AreEqual(server.Objects.Count(), 0);

                    Update2(server, client);
                    Assert.AreEqual(client.Objects.Count(), 0);
                }
            }
        }


    }
}
