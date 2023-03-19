using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace Yanmonet.NetSync.Editor.Tests
{
    public class RpcTests : TestBase
    {
        public class RpcTest : NetworkObject
        {
            public int result;

            [ServerRpc]
            public void ServerRpc(int a, int b)
            {
                //BeginServerRpc(nameof(ServerRpc), new object[] { a, b });
                //EndServerRpc();
                //if (ReturnServerRpc())
                //{
                //    return;
                //} 
                   
                this.result = a + b;
            }  
            [ServerRpc]
            public void ServerRpc2(int a, int b, ServerRpcParams rpcParams)
            {  
                //BeginServerRpc(nameof(ServerRpc), new object[] { a, b });
                //EndServerRpc();   
                //if (ReturnServerRpc())
                //{  
                //    return;
                //}
                 
                this.result = a + b;
            }
            [ClientRpc]
            public void ClientRpc(int a, int b)
            {
                BeginClientRpc(nameof(ClientRpc), new object[] { a, b });
                EndClientRpc();
                if (ReturnClientRpc())
                {
                    return;
                }

                this.result = a + b;
            }
            /*
            public void ServerRpc3(int a, int b, ServerRpcParams rpcParams)
            {
                BeginServerRpc(nameof(ServerRpc3), rpcParams, , a, b);
                EndServerRpc();
                if (ReturnServerRpc())
                {
                    return;
                }

                this.result = a + b;
            }
            */
            public void ServerRpc4(int a, int b)
            { 
                ServerRpcParams rpcParams = new ServerRpcParams();
                //BeginServerRpc(nameof(ServerRpc4), rpcParams, , new object[] { a, b });
                //EndServerRpc();
                //if (ReturnServerRpc())
                //{
                //    return;
                //}

                //this.result = a + b;
            }
        } 
           
            

        [Test]
        [OpenNetwork]
        public void ServerToServer()
        {
            var serverData = serverManager.CreateObject<RpcTest>();
            serverData.Spawn();
            serverData.AddObserver(serverClient.ClientId);
            Update();

            var clientData = (RpcTest)client.Objects.FirstOrDefault();

            serverData.result = 0;
            clientData.result = 0;
            serverData.ServerRpc(1, 2);
            Update();

            Assert.AreEqual(3, serverData.result);
            Assert.AreEqual(0, clientData.result);
        }

        [Test]
        [OpenNetwork]
        public void ClientToServer()
        {
            var serverData = serverManager.CreateObject<RpcTest>();
            serverData.SpawnWithOwnership(serverClient.ClientId);
            Update();

            var clientData = (RpcTest)client.Objects.FirstOrDefault();

            serverData.result = 0;
            clientData.result = 0;
            clientData.ServerRpc(1, 2);
            Update();

            Assert.AreEqual(3, serverData.result);
            Assert.AreEqual(0, clientData.result);
        }

        [Test]
        [OpenNetwork]
        public void ServerToClient()
        {
            var serverData = serverManager.CreateObject<RpcTest>();
            serverData.Spawn();
            serverData.AddObserver(serverClient.ClientId);
            Update();

            var clientData = (RpcTest)client.Objects.FirstOrDefault();

            serverData.result = 0;
            clientData.result = 0;
            serverData.ClientRpc(1, 2);
            Update();

            Assert.AreEqual(0, serverData.result);
            Assert.AreEqual(3, clientData.result);
        }

        [Test]
        [OpenNetwork]
        public void ClientToClient()
        {
            var serverData = serverManager.CreateObject<RpcTest>();
            serverData.Spawn();
            serverData.AddObserver(serverClient.ClientId);
            Update();

            var clientData = (RpcTest)client.Objects.FirstOrDefault();

            serverData.result = 0;
            clientData.result = 0;
            clientData.ClientRpc(1, 2);
            Update();

            Assert.AreEqual(0, serverData.result);
            Assert.AreEqual(3, clientData.result);
        }


        [Test]
        [OpenNetwork]
        public void ClientToHost()
        {
            var serverData = serverManager.CreateObject<RpcTest>();
            serverData.Spawn();
            serverData.AddObserver(serverClient.ClientId);
            Update();

            var clientData = (RpcTest)client.Objects.FirstOrDefault();

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
            var serverData = serverManager.CreateObject<RpcTest>();
            serverData.Spawn();
            serverData.AddObserver(serverClient.ClientId);
            Update();

            var clientData = (RpcTest)client.Objects.FirstOrDefault();

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
            var serverData = serverManager.CreateObject<RpcTest>();
            serverData.Spawn();
            serverData.AddObserver(serverClient.ClientId);
            Update();

            var clientData = (RpcTest)client.Objects.FirstOrDefault();

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
            serverData.SpawnWithOwnership(serverClient.ClientId);
            serverData.ClientRpc(1, 2);
            Update();

            var clientData = (RpcTestObject)client.Objects.FirstOrDefault();

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
            private Sync<int> serverRpcResult = new Sync<int>(
                writePermission: SyncWritePermission.Server);

            private Sync<int> clientRpcResult = new Sync<int>(
               writePermission: SyncWritePermission.Owner);

            public int ServerRpcResult { get => serverRpcResult.Value; set => serverRpcResult.Value = value; }

            public int ClientRpcResult { get => clientRpcResult.Value; set => clientRpcResult.Value = value; }


            [ServerRpc]
            public void ServerRpc(int a, int b)
            {
                BeginServerRpc(nameof(ServerRpc), new object[] { a, b });
                EndServerRpc();
                if (ReturnServerRpc())
                {
                    return;
                }

                ServerRpcResult = a + b;
            }


            [ClientRpc]
            public void ClientRpc(int a, int b)
            {
                BeginClientRpc(nameof(ClientRpc), new object[] { a, b });
                EndClientRpc();
                if (ReturnClientRpc())
                {
                    return;
                }

                ClientRpcResult = a + b;
            }
        }

    }
}
