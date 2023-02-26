using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Yanmonet.NetSync.Test
{
    [TestClass]
    public class RpcTest : TestBase
    {

        class MyData : NetworkObject
        {
            [SyncVar(Bits = 0x1)]
            private string stringVar;
            [SyncVar(Bits = 0x2)]
            private int intVar;
            [SyncVar(Bits = 0x4)]
            private float floatVar;

            public string StringVar { get => stringVar; set => SetSyncVar(value, ref stringVar, 0x1); }
            public int IntVar { get => intVar; set => SetSyncVar(value, ref intVar, 0x2); }
            public float FloatVar { get => floatVar; set => SetSyncVar(value, ref floatVar, 0x4); }

            public string result;

            public MyData()
            {

            }
            public void SetRpcMsg(string msg)
            {
                this.result = msg;
            }
            [ClientRpc]
            public void ClientRpc(string msg)
            {
                if (!IsClient)
                {
                    Rpc(nameof(ClientRpc), new object[] { msg });
                    return;
                }
                this.result = msg;
            }
            [ServerRpc]
            public void ServerRpc(string msg)
            {

                if (!IsServer)
                {
                    Rpc(nameof(ServerRpc), new object[] { msg });
                    return;
                }
                this.result = msg;
            }

        }

        [TestMethod]
        public void Test1()
        {
            Type type = typeof(TestStruct);
            var typeCode = Type.GetTypeCode(type);
            type = typeof(string);
            type = typeof(int);
            bool b= typeof(string).IsPrimitive;
        }


        [TestMethod]
        public void RefPerformance()
        {
            int max = 10000000;
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();

            TestStruct s;
            s = new TestStruct();
            sw.Start();
            for (int i = 0; i < max; i++)
            {
                Serialize(s);
            }
            sw.Stop();
            Console.WriteLine("time:" + sw.ElapsedMilliseconds);
            sw.Reset();
            sw.Start();
            for (int i = 0; i < max; i++)
            {
                SerializeRef(ref s);
            }
            sw.Stop();
            Console.WriteLine("ref time:" + sw.ElapsedMilliseconds);

        }
        struct TestStruct
        {
            public float f1;
            public int i1;
            public float f2;
            //public float f3;
            //public float f4;
            //public float f5;
            //public float f6;
            //public float f7;
            //public float f8;
            //public float f9;
            //public float f10;
        }
        void SerializeRef(ref TestStruct value)
        {
            value.f1 = 1f;
            //value.i1 = 2;
            //value.f1 = 3f;
        }

        void Serialize(TestStruct value)
        {
            value.f1 = 1f;
            //value.i1 = 2;
            //value.f1 = 3f;
        }

        [TestMethod]
        public void Rpc_Test()
        {
            _Rpc_Test().Wait();
        }



        public async Task _Rpc_Test()
        {
            NetworkManager serverManager = new NetworkManager();
            NetworkManager clientManager = new NetworkManager();
            try
            {
                serverManager.RegisterObject<MyData>((id) =>
                {
                    return new MyData();
                });
                clientManager.RegisterObject<MyData>((id) =>
                {
                    return new MyData();
                });
                serverManager.StartServer();
                clientManager.StartClient();

                var server = serverManager.Server;
                var client = clientManager.LocalClient;

                Update(serverManager, clientManager);

                var serverClient = serverManager.ConnnectedClientList.First();
                 
                var serverData = serverManager.CreateObject<MyData>();
                serverData.Spawn(serverClient.ClientId);             
                Update(serverManager, clientManager);

                var clientData = (MyData)client.Objects.FirstOrDefault();

                serverData.result = null;
                clientData.result = null;
                serverData.ClientRpc("server to client");                 
                Update(serverManager, clientManager);
                Update(serverManager, clientManager);

                Assert.AreEqual(serverData.result, null);
                Assert.AreEqual(clientData.result, "server to client");

                serverData.result = null;
                clientData.result = null;
                serverData.ServerRpc("server to server");

                Update(serverManager, clientManager);

                Assert.AreEqual(serverData.result, "server to server");
                Assert.AreEqual(clientData.result, null);

                serverData.result = null;
                clientData.result = null;
                clientData.ClientRpc("client to client");

                Update(serverManager, clientManager);

                Assert.AreEqual(serverData.result, null);
                Assert.AreEqual(clientData.result, "client to client");

                serverData.result = null;
                clientData.result = null;
                serverData.ServerRpc("client to server");

                Update(serverManager, clientManager);
                Assert.AreEqual(serverData.result, "client to server");
                Assert.AreEqual(clientData.result, null);

                Update(serverManager, clientManager);
                //  client.Stop();
            }
            finally
            {
                serverManager.Dispose();
                clientManager.Dispose();
            }
        }

        [TestMethod]
        public async Task ThreadId_Test()
        {
            Console.WriteLine("Yield Before ThreadId: " + Thread.CurrentThread.ManagedThreadId);
            await Task.Yield();
            Console.WriteLine("Yield After ThreadId: " + Thread.CurrentThread.ManagedThreadId);
            Console.WriteLine("Delay Before ThreadId: " + Thread.CurrentThread.ManagedThreadId);
            await Task.Delay(10);
            Console.WriteLine("Delay After ThreadId: " + Thread.CurrentThread.ManagedThreadId);


            await Task.Delay(10);
            Console.WriteLine("ThreadId: " + Thread.CurrentThread.ManagedThreadId);

            await Task.Delay(10);
            Console.WriteLine("ThreadId: " + Thread.CurrentThread.ManagedThreadId);

            await Task.Delay(10);
            Console.WriteLine("ThreadId: " + Thread.CurrentThread.ManagedThreadId);
            await Task.Delay(10);
            Console.WriteLine("ThreadId: " + Thread.CurrentThread.ManagedThreadId);
            await Task.Delay(10);
            Console.WriteLine("ThreadId: " + Thread.CurrentThread.ManagedThreadId);
            await Task.Delay(10);
            Console.WriteLine("ThreadId: " + Thread.CurrentThread.ManagedThreadId);
        }


    }

    sealed class SingleThreadSynchronizationContext : SynchronizationContext
    {
        private readonly BlockingCollection<KeyValuePair<SendOrPostCallback, object>> m_queue = new();

        public override void Post(SendOrPostCallback d, object state)
        {
            m_queue.Add(new KeyValuePair<SendOrPostCallback, object>(d, state));
        }

        public void RunOnCurrentThread()
        {
            KeyValuePair<SendOrPostCallback, object> workItem;

            while (m_queue.TryTake(out workItem, Timeout.Infinite))
                workItem.Key(workItem.Value);
        }

        public void Complete() { m_queue.CompleteAdding(); }

    }
}
