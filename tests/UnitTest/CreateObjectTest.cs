using Microsoft.VisualStudio.TestTools.UnitTesting;
using Net;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UnitTest
{
    [TestClass]
    public class CreateObjectTest : TestBase
    {

        [TestMethod]
        public void CreateObject()
        {
            Run(_CreateObject());
        }
        public IEnumerator _CreateObject()
        {
            using (MyServer server = new MyServer())
            {
                server.Start(localPort);

                using (MyClient client = new MyClient())
                {
                    client.Connect(localAddress, localPort);

                    foreach (var o in Wait()) yield return null;


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
                    foreach (var o in Wait()) yield return null;

                    var clientData = client.Objects.FirstOrDefault();
                    Assert.IsNull(clientData);
                    server.AddObserver(serverData, server.Connections.First());
                    foreach (var o in Wait()) yield return null;
                    clientData = client.Objects.FirstOrDefault();

                    Assert.IsNotNull(clientData);
                    Assert.IsFalse(clientData.IsServer);
                    Assert.IsTrue(clientData.IsClient);
                    Assert.AreEqual(clientData.InstanceId, serverData.InstanceId);
                    Assert.IsFalse(object.ReferenceEquals(clientData, serverData));
                    Assert.AreEqual(clientData, serverData);

                    server.DestroyObject(serverData);
                    Assert.AreEqual(server.Objects.Count(), 0);

                    foreach (var o in Wait()) yield return null;
                    Assert.AreEqual(client.Objects.Count(), 0);

                }
            }
        }



        [TestMethod]
        public void DestroyObject()
        {
            Run(_DestroyObject());
        }
        public IEnumerator _DestroyObject()
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
                    foreach (var o in Wait()) yield return null;

                    server.AddObserver(serverData,server.Connections.First());
                    foreach (var o in Wait()) yield return null;

                    server.DestroyObject(serverData);
                    Assert.AreEqual(server.Objects.Count(), 0);

                    foreach (var o in Wait()) yield return null;
                    Assert.AreEqual(client.Objects.Count(), 0);
                }
            }
        }


        [TestMethod]
        public void HostObject()
        {
            Run(_HostObject());
        }
        public IEnumerator _HostObject()
        {

            using (MyServer server = new MyServer())
            {
                server.Start(localPort);

                using (MyClient client = new MyClient(server, null, false))
                {
                    client.Connect(localAddress, localPort);

                    foreach (var o in Wait()) yield return null;

                    NetworkClient.RegisterObject<MySyncVarData>((id) =>
                    {
                        return new MySyncVarData();
                    });
                    Assert.AreEqual(client.Objects.Count(), 0);
                    Assert.AreEqual(server.Objects.Count(), 0);

                    var serverData = server.CreateObject<MySyncVarData>();
                    Assert.AreEqual(server.Objects.Count(), 1);
                    Assert.IsNotNull(serverData);
                    foreach (var o in Wait()) yield return null;

                    var clientData = client.Objects.FirstOrDefault();
                    Assert.IsNull(clientData);

                    server.AddObserver(serverData,server.Connections.First());
                    foreach (var o in Wait()) yield return null;
                    clientData = client.Objects.FirstOrDefault();

                    Assert.IsTrue(object.ReferenceEquals(clientData, serverData));

                    server.DestroyObject(serverData);
                    Assert.AreEqual(server.Objects.Count(), 0);

                    foreach (var o in Wait()) yield return null;
                    Assert.AreEqual(client.Objects.Count(), 0);
                }
            }
        }


    }
}
