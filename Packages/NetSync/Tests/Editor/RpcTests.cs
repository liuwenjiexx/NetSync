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
        class RpcTest : NetworkObject
        {
            /*  [SyncVar(Bits = 0x1)]
              private string stringVar;
              [SyncVar(Bits = 0x2)]
              private int intVar;
              [SyncVar(Bits = 0x4)]
              private float floatVar;

              public string StringVar { get => stringVar; set => SetSyncVar(value, ref stringVar, 0x1); }
              public int IntVar { get => intVar; set => SetSyncVar(value, ref intVar, 0x2); }
              public float FloatVar { get => floatVar; set => SetSyncVar(value, ref floatVar, 0x4); }
              */
            public int result;

            [ServerRpc]
            public void ServerRpc(int a, int b)
            {
                BeginServerRpc(nameof(ServerRpc), new object[] { a, b });
                EndServerRpc();
                if (ReturnServerRpc())
                {
                    return;
                }

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
            private NetworkVariable<int> serverRpcResult = new NetworkVariable<int>(
                writePermission: NetworkVariableWritePermission.Server);

            private NetworkVariable<int> clientRpcResult = new NetworkVariable<int>(
               writePermission: NetworkVariableWritePermission.Owner);

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
