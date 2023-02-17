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

        [NetworkObjectId("6ebd8e0c-4c32-4c9d-bd3e-a60bb2f68af3")]
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

            public string rpcMsg;

            public MyData()
            {

            }
            public void SetRpcMsg(string msg)
            {
                this.rpcMsg = msg;
            }
            [Rpc]
            public void RpcClient(string msg)
            {
                if (!IsClient)
                {
                    Rpc("RpcClient", new object[] { msg });
                    return;
                }
                this.rpcMsg = msg;
            }
            [Rpc]
            public void RpcServer(string msg)
            {

                if (!IsServer)
                {
                    Rpc("RpcServer", new object[] { msg });
                    return;
                }
                this.rpcMsg = msg;
            }

        }

        [TestMethod]
        public void Test1()
        {
            Type type = typeof(TestStruct);
            var typeCode = Type.GetTypeCode(type);
            type = typeof(string);
            type = typeof(int);
        }

        [TestMethod]
        public void NewGuid()
        {
            Console.WriteLine(Guid.NewGuid().ToString());
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

            using (NetworkServer server = new NetworkServer())
            {
                server.Start(localPort);

                NetworkClient client = NewClient();

                //await Update(server, client);
                Update2(server, client);

                var serverClient = server.Clients.FirstOrDefault();

                NetworkClient.RegisterObject<MyData>((id) =>
                {
                    return new MyData();
                });

                var serverData = server.CreateObject<MyData>();
                server.AddObserver(serverData, server.Connections.First());

                //await Update(server, client);
                Update2(server, client);
                var clientData = (MyData)client.Objects.FirstOrDefault();

                serverData.rpcMsg = null;
                clientData.rpcMsg = null;
                serverData.RpcClient("server to client");

                //await Update(server, client);
                Update2(server, client);

                Assert.AreEqual(serverData.rpcMsg, null);
                Assert.AreEqual(clientData.rpcMsg, "server to client");

                serverData.rpcMsg = null;
                clientData.rpcMsg = null;
                serverData.RpcServer("server to server");
                
                //await Update(server, client);
                Update2(server, client);

                Assert.AreEqual(serverData.rpcMsg, "server to server");
                Assert.AreEqual(clientData.rpcMsg, null);

                serverData.rpcMsg = null;
                clientData.rpcMsg = null;
                clientData.RpcClient("client to client");

                //await Update(server, client);
                Update2(server, client);

                Assert.AreEqual(serverData.rpcMsg, null);
                Assert.AreEqual(clientData.rpcMsg, "client to client");

                serverData.rpcMsg = null;
                clientData.rpcMsg = null;
                serverData.RpcServer("client to server");

                //await Update(server, client);
                Update2(server, client);
                Assert.AreEqual(serverData.rpcMsg, "client to server");
                Assert.AreEqual(clientData.rpcMsg, null);

                //await Update(server, client);
                Update2(server, client);
                //  client.Stop();
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
