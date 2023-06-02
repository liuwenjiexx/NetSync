using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace Yanmonet.Network.Sync.Editor.Tests
{
    public class RpcTests : TestBase
    {
        public class RpcTest : NetworkObject
        {
            public int result;

            public void AA(int a, int b)
            {
                object[] array;
                array = new object[2];
                array[0] = a;
                array[1] = b;

            }

            [ServerRpc]
            public void ServerRpc(int a, int b)
            {
                this.result = a + b;
            }

            [ServerRpc]
            private void PrivateServerRpc()
            {
            }

            [ServerRpc]
            public void ServerRpc2(int a, int b, ServerRpcParams rpcParams)
            {
                this.result = a + b;
            }
            [ClientRpc]
            public void ClientRpc(int a, int b)
            {

                this.result = a + b;
            }

            public void ServerRpc3(int a, int b, ServerRpcParams rpcParams)
            {
                __BeginServerRpc__(nameof(ServerRpc3), rpcParams, a, b);
                __EndServerRpc__();
                if (__ReturnServerRpc__())
                {
                    return;
                }

                this.result = a + b;
            }

            public void ServerRpc4(int a, int b)
            {
                object[] array;
                array = new object[2];
                array[0] = a;
                array[1] = b;
                ServerRpcParams rpcParams = new ServerRpcParams();
                __BeginServerRpc__(nameof(ServerRpc4), rpcParams, array);
                __EndServerRpc__();
                if (__ReturnServerRpc__())
                {
                    return;
                }

                this.result = a + b;
            }
        }



        [Test]
        [OpenNetwork]
        public void ServerRpc_OnServer()
        {
            var serverData = serverManager.CreateObject<RpcTest>();
            serverData.Spawn();
            serverData.AddObserver(client.LocalClientId);
            Update();

            var clientData = (RpcTest)client.SpawnedObjects.FirstOrDefault();

            serverData.result = 0;
            clientData.result = 0;
            serverData.ServerRpc(1, 2);
            Update();

            Assert.AreEqual(3, serverData.result);
            Assert.AreEqual(0, clientData.result);
        }

        [Test]
        [OpenNetwork]
        public void ServerRpc_OnClient()
        {
            var serverData = serverManager.CreateObject<RpcTest>();
            serverData.SpawnWithOwnership(client.LocalClientId);
            Update();

            var clientData = (RpcTest)client.SpawnedObjects.FirstOrDefault();

            serverData.result = 0;
            clientData.result = 0;
            clientData.ServerRpc(1, 2);
            Update();

            Assert.AreEqual(3, serverData.result);
            Assert.AreEqual(0, clientData.result);
        }

        [Test]
        [OpenNetwork]
        public void ClientRpc_OnServer()
        {
            var serverData = serverManager.CreateObject<RpcTest>();
            serverData.Spawn();
            serverData.AddObserver(client.LocalClientId);
            Update();

            var clientData = (RpcTest)client.SpawnedObjects.FirstOrDefault();

            serverData.result = 0;
            clientData.result = 0;
            serverData.ClientRpc(1, 2);
            Update();
             
            Assert.AreEqual(0, serverData.result);
            Assert.AreEqual(3, clientData.result);
        }

        [Test]
        [OpenNetwork]
        public void ClientRpc_OnClient()
        {
            var serverData = serverManager.CreateObject<RpcTest>();
            serverData.Spawn();
            serverData.AddObserver(client.LocalClientId);
            Update();

            var clientData = (RpcTest)client.SpawnedObjects.FirstOrDefault();

            serverData.result = 0;
            clientData.result = 0;
            clientData.ClientRpc(1, 2);
            Update();

            Assert.AreEqual(0, serverData.result);
            Assert.AreEqual(3, clientData.result);
        }


        [Test]
        [OpenNetwork(IsHost = true)]
        public void ServerRpc_OnClient_Host()
        {
            Assert.IsTrue(server.IsHost);

            var serverData = serverManager.CreateObject<RpcTest>();
            serverData.Spawn();
            serverData.AddObserver(client.LocalClientId);
            Update();

            var clientData = (RpcTest)client.SpawnedObjects.FirstOrDefault();

            serverData.result = 0;
            clientData.result = 0;
            clientData.ServerRpc(1, 2);
            Update();

            Assert.AreEqual(3, serverData.result);
            Assert.AreEqual(0, clientData.result);
        }


        [Test]
        [OpenNetwork(IsHost = true)]
        public void HostToHost()
        {
            Assert.IsTrue(server.IsHost);
            var serverData = serverManager.CreateObject<RpcTest>();
            serverData.Spawn();
            serverData.AddObserver(client.LocalClientId);
            Update();

            var clientData = (RpcTest)client.SpawnedObjects.FirstOrDefault();

            serverData.result = 0;
            clientData.result = 0;
            serverData.ServerRpc(1, 2);
            Update();

            Assert.AreEqual(3, serverData.result);
            Assert.AreEqual(0, clientData.result);
        }

        [Test]
        [OpenNetwork(IsHost = true)]
        public void HostToClient()
        {
            Assert.IsTrue(server.IsHost);
            var serverData = server.CreateObject<RpcTest>();
            serverData.Spawn();
            serverData.AddObserver(client.LocalClientId);
            Update();

            var clientData = (RpcTest)client.SpawnedObjects.FirstOrDefault();
            Assert.IsNotNull(clientData);

            serverData.result = 0;
            clientData.result = 0;
            serverData.ClientRpc(1, 2);
            Update();

            Assert.AreEqual(3, serverData.result);
            Assert.AreEqual(3, clientData.result);
        }


        [Test]
        [OpenNetwork()]
        public void ServerToClient_Immediate()
        {
            var serverData = serverManager.CreateObject<RpcTestObject>();
            serverData.SpawnWithOwnership(client.LocalClientId);
            serverData.ClientRpc(1, 2);
            Update();

            var clientData = (RpcTestObject)client.SpawnedObjects.FirstOrDefault();

            Assert.AreEqual(3, serverData.ClientRpcResult);
            Assert.AreEqual(3, clientData.ClientRpcResult);
        }


        //[Test]
        //[OpenNetwork()]
        //public void ClientToServer_Immediate()
        //{
        //    var serverData = serverManager.CreateObject<ClientRpcObjec>();
        //    serverData.Spawn();
        //    serverData.AddObserver(serverClient.ClientId);
        //    serverData.ClientRpc(1, 2);
        //    Update();

        //    var clientData = (RpcTest)client.Objects.FirstOrDefault();

        //    Assert.AreEqual(0, serverData.result);
        //    Assert.AreEqual(3, clientData.result);
        //}

        class RpcTestObject : NetworkObject
        {
            private Sync<int> serverRpcResult = new Sync<int>(writePermission: SyncWritePermission.Server);

            private Sync<int> clientRpcResult = new Sync<int>(writePermission: SyncWritePermission.Owner);

            public int ServerRpcResult { get => serverRpcResult.Value; set => serverRpcResult.Value = value; }

            public int ClientRpcResult { get => clientRpcResult.Value; set => clientRpcResult.Value = value; }


            [ServerRpc]
            public void ServerRpc(int a, int b)
            {
                ServerRpcResult = a + b;
            }


            [ClientRpc]
            public void ClientRpc(int a, int b)
            {
                ClientRpcResult = a + b;
            }
        }

    }
}
