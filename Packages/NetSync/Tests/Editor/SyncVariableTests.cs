using NUnit.Framework;
using System;
using System.Collections.Generic;
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

            Assert.AreEqual(1, clientData.IntVar, "Client");
            Assert.AreEqual(1, serverData.IntVar, "Server");
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

        #region List

        class ListVarData : NetworkObject
        {
            private SyncList<string> stringList = new();
            private SyncList<int> intList = new();

            public IList<string> StringList => stringList;

            public IList<int> IntList => intList;
        }

        [Test]
        [OpenNetwork]
        public void List_Int()
        {
            var serverData = serverManager.CreateObject<ListVarData>();
            serverData.Spawn();
            serverData.AddObserver(clientManager.LocalClientId);
            Update();
            var clientData = (ListVarData)client.Objects.First();

            Assert.AreEqual(0, serverData.IntList.Count);
            Assert.AreEqual(0, clientData.IntList.Count);

            serverData.IntList.Add(1);
            Update();

            string value;
            Assert.AreEqual(1, serverData.IntList.Count);
            Assert.AreEqual(1, serverData.IntList[0]);
            Assert.AreEqual(1, clientData.IntList.Count);
            Assert.AreEqual(1, clientData.IntList[0]);

            serverData.IntList[0] = 2;
            Update();
            Assert.AreEqual(1, serverData.IntList.Count);
            Assert.AreEqual(2, serverData.IntList[0]);
            Assert.AreEqual(1, clientData.IntList.Count);
            Assert.AreEqual(2, clientData.IntList[0]);

            serverData.IntList.Add(3);
            Update();
            Assert.AreEqual(2, serverData.IntList.Count);
            Assert.AreEqual(3, serverData.IntList[1]);
            Assert.AreEqual(2, clientData.IntList.Count);
            Assert.AreEqual(3, clientData.IntList[1]);


            serverData.IntList.RemoveAt(0);
            Update();
            Assert.AreEqual(1, serverData.IntList.Count);
            Assert.AreEqual(3, serverData.IntList[0]);
            Assert.AreEqual(1, clientData.IntList.Count);
            Assert.AreEqual(3, clientData.IntList[0]);

            serverData.IntList.Clear();
            Update();
            Assert.AreEqual(0, serverData.IntList.Count);
            Assert.AreEqual(0, clientData.IntList.Count);

        }

        #endregion

        #region Dictionary


        class DictionaryVarData : NetworkObject
        {
            private SyncDictionary<string, string> stringDic = new();
            private SyncDictionary<int, int> intDic = new();

            public IDictionary<string, string> StringDic => stringDic;

            public IDictionary<int, int> IntDic => intDic;
        }



        [Test]
        [OpenNetwork]
        public void Dictionary_String_String()
        {
            var serverData = serverManager.CreateObject<DictionaryVarData>();
            serverData.Spawn();
            serverData.AddObserver(clientManager.LocalClientId);
            Update();
            var clientData = (DictionaryVarData)client.Objects.First();

            Assert.AreEqual(0, serverData.StringDic.Count);
            Assert.AreEqual(0, clientData.StringDic.Count);

            serverData.StringDic["abc"] = "123";
            Update();

            string value;
            Assert.IsTrue(serverData.StringDic.TryGetValue("abc", out value));
            Assert.AreEqual("123", value);
            Assert.IsTrue(clientData.StringDic.TryGetValue("abc", out value));
            Assert.AreEqual("123", value);

            Assert.Throws<InvalidOperationException>(() =>
            {
                serverData.StringDic.Add("abc", "456");
            });


            serverData.StringDic["abc"] = "123456";
            Update();
            Assert.IsTrue(serverData.StringDic.TryGetValue("abc", out value));
            Assert.AreEqual("123456", value);
            Assert.IsTrue(clientData.StringDic.TryGetValue("abc", out value));
            Assert.AreEqual("123456", value);

            serverData.StringDic.Add("def", "456");
            Update();
            Assert.IsTrue(serverData.StringDic.TryGetValue("def", out value));
            Assert.AreEqual("456", value);
            Assert.IsTrue(clientData.StringDic.TryGetValue("def", out value));
            Assert.AreEqual("456", value);


            serverData.StringDic.Remove("def");
            Update();
            Assert.IsFalse(serverData.StringDic.ContainsKey("def"));
            Assert.IsFalse(clientData.StringDic.ContainsKey("def"));

            serverData.StringDic.Clear();
            Update();
            Assert.AreEqual(0, serverData.StringDic.Count);
            Assert.AreEqual(0, clientData.StringDic.Count);

        }

        [Test]
        [OpenNetwork]
        public void Dictionary_Int_Int()
        {
            var serverData = serverManager.CreateObject<DictionaryVarData>();
            serverData.Spawn();
            serverData.AddObserver(clientManager.LocalClientId);
            Update();
            var clientData = (DictionaryVarData)client.Objects.First();

            Assert.AreEqual(0, serverData.IntDic.Count);
            Assert.AreEqual(0, clientData.IntDic.Count);

            serverData.IntDic[1] = 123;
            Update();

            int value;
            Assert.IsTrue(serverData.IntDic.TryGetValue(1, out value));
            Assert.AreEqual(123, value);

            Assert.IsTrue(clientData.IntDic.TryGetValue(1, out value));
            Assert.AreEqual(123, value);
        }

        #endregion
    }
}