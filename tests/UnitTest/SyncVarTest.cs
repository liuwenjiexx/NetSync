using Microsoft.VisualStudio.TestTools.UnitTesting;
using Net;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

namespace UnitTest
{

    class MyServer : NetworkServer
    {
        public MyServer(TcpListener server)
            : base(server)
        { }

        protected override NetworkClient AcceptClient(TcpClient client)
        {
            var c = new MyClient(this, client.Client, true);
            c.Start();
            return c;
        }
    }

    class MyClient : NetworkClient
    {
        //[SyncVar(Bits = 0x1)]
        //private string stringVar;
        //[SyncVar(Bits = 0x2)]
        //private int intVar;
        //[SyncVar(Bits = 0x4)]
        //private float floatVar;


        public MyClient(MyServer server, Socket socket, bool isListen)
        : base(server, socket, isListen)
        {
        }

        //public string StringVar { get => stringVar; set => SetSyncVar(value, ref stringVar, 0x1); }
        //public int IntVar { get => intVar; set => SetSyncVar(value, ref intVar, 0x2); }
        //public float FloatVar { get => floatVar; set => SetSyncVar(value, ref floatVar, 0x4); }
    }

    [TestClass]
    public class SyncVarTest : TestBase
    {

        public void Start()
        {
            Run(_Start());
        }
        IEnumerator _Start()
        {
            int port = 1000;
            TcpListener tcpListener = new TcpListener(new IPEndPoint(IPAddress.Any, port));
            tcpListener.Start();
            MyServer server = new MyServer(tcpListener);
            server.Start();
            Assert.IsTrue(server.IsRunning);

            TcpClient tcpClient = new TcpClient();

            tcpClient.Connect("127.0.0.1", port);
            MyClient client = new MyClient(null, tcpClient.Client, false);
            client.Start();
            Assert.IsTrue(client.IsRunning);

            yield return null;
            client.Stop();
            Assert.IsFalse(client.IsRunning);
            yield return null;
            server.Stop();
            Assert.IsFalse(server.IsRunning);

        }


        [NetworkObjectId("cb0518d9-8c72-4764-a463-6d6eba57cb83")]
        class MySyncVarData : NetworkObject
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

            //   [RpcClient]
            public void SetIntVarClient(int n)
            {
                intVar = n;
            }
            //   [RpcServer]
            public void SetIntVarServer(int n)
            {
                intVar = n;
            }



        }

        [NetworkObjectId("1b0518d9-8c72-4764-a463-6d6eba57cb83")]
        class ErrorRepeatBitsData : NetworkObject
        {
            [SyncVar(Bits = 0x1)]
            private string stringVar;
            [SyncVar(Bits = 0x1)]
            private int intVar;
        }

        [ExpectedException(typeof(Exception))]
        [TestMethod]
        public void RepeatBits()
        {
            NetworkClient.RegisterObject<ErrorRepeatBitsData>(id => new ErrorRepeatBitsData());
        }

        [TestMethod]
        public void CreateNetworkObject()
        {
            Run(_CreateNetworkObject());
        }
        public IEnumerator _CreateNetworkObject()
        {
            int port = 1000;
            TcpListener tcpListener = new TcpListener(new IPEndPoint(IPAddress.Any, port));
            tcpListener.Start();
            MyServer server = new MyServer(tcpListener);
            server.Start();

            TcpClient tcpClient = new TcpClient();
            tcpClient.Connect("127.0.0.1", port);
            MyClient client = new MyClient(null, tcpClient.Client, false);
            client.Start();

            yield return null;
            yield return null;


            NetworkClient.RegisterObject<MySyncVarData>((id) =>
           {
               return new MySyncVarData();
           });
            Assert.AreEqual(client.Objects.Count(), 0);
            Assert.AreEqual(server.Objects.Count(), 0);

            var serverData = server.CreateObject<MySyncVarData>();
            Assert.AreEqual(server.Objects.Count(), 1);
            Assert.IsNotNull(serverData);
            Assert.IsTrue(serverData.IsServer);
            Assert.IsFalse(serverData.IsClient);
            yield return null;
            yield return null;

            var clientData = client.Objects.FirstOrDefault();
            Assert.IsNull(clientData);
            serverData.AddObserver(server.Connections.First());
            yield return null;
            yield return null;
            clientData = client.Objects.FirstOrDefault();

            Assert.IsNotNull(clientData);
            Assert.IsFalse(clientData.IsServer);
            Assert.IsTrue(clientData.IsClient);
            Assert.AreEqual(clientData.InstanceId, serverData.InstanceId);
            Assert.IsFalse(object.ReferenceEquals(clientData, serverData));
            Assert.AreEqual(clientData, serverData);

            server.DestroyObject(serverData);
            Assert.AreEqual(server.Objects.Count(), 0);

            yield return null;
            yield return null;
            Assert.AreEqual(client.Objects.Count(), 0);

            client.Stop();
            server.Stop();
            tcpListener.Stop();
        }



        [TestMethod]
        public void DestroyNetworkObject()
        {
            Run(_DestroyNetworkObject());
        }
        public IEnumerator _DestroyNetworkObject()
        {
            int port = 1000;
            TcpListener tcpListener = new TcpListener(new IPEndPoint(IPAddress.Any, port));
            tcpListener.Start();
            MyServer server = new MyServer(tcpListener);
            server.Start();

            TcpClient tcpClient = new TcpClient();
            tcpClient.Connect("127.0.0.1", port);
            MyClient client = new MyClient(null, tcpClient.Client, false);
            client.Start();

            NetworkClient.RegisterObject<MySyncVarData>((id) =>
            {
                return new MySyncVarData();
            });
            Assert.AreEqual(client.Objects.Count(), 0);
            Assert.AreEqual(server.Objects.Count(), 0);

            var serverData = server.CreateObject<MySyncVarData>();
            yield return null;

            serverData.AddObserver(server.Connections.First());
            yield return null;
            yield return null;

            server.DestroyObject(serverData);
            Assert.AreEqual(server.Objects.Count(), 0);

            yield return null;
            yield return null;
            Assert.AreEqual(client.Objects.Count(), 0);

            client.Stop();
            server.Stop();
            tcpListener.Stop();
        }


        [TestMethod]
        public void HostNetworkObject()
        {
            Run(_HostNetworkObject());
        }
        public IEnumerator _HostNetworkObject()
        {
            int port = 1000;
            TcpListener tcpListener = new TcpListener(new IPEndPoint(IPAddress.Any, port));
            tcpListener.Start();
            MyServer server = new MyServer(tcpListener);
            server.Start();

            TcpClient tcpClient = new TcpClient();
            tcpClient.Connect("127.0.0.1", port);
            MyClient client = new MyClient(server, tcpClient.Client, false);
            client.Start();

            yield return null;
            yield return null;

            NetworkClient.RegisterObject<MySyncVarData>((id) =>
           {
               return new MySyncVarData();
           });
            Assert.AreEqual(client.Objects.Count(), 0);
            Assert.AreEqual(server.Objects.Count(), 0);

            var serverData = server.CreateObject<MySyncVarData>();
            Assert.AreEqual(server.Objects.Count(), 1);
            Assert.IsNotNull(serverData);

            yield return null;
            yield return null;

            var clientData = client.Objects.FirstOrDefault();
            Assert.IsNull(clientData);

            serverData.AddObserver(server.Connections.First());
            yield return null;
            yield return null;
            clientData = client.Objects.FirstOrDefault();

            Assert.IsTrue(object.ReferenceEquals(clientData, serverData));

            server.DestroyObject(serverData);
            Assert.AreEqual(server.Objects.Count(), 0);

            yield return null;
            yield return null;
            Assert.AreEqual(client.Objects.Count(), 0);

            client.Stop();
            server.Stop();
            tcpListener.Stop();
        }

        [TestMethod]
        public void SyncVar()
        {
            //Assert.AreEqual(GetSigleBitsLength(0), 0);
            //Assert.AreEqual(GetSigleBitsLength(1<<0), 1);
            //Assert.AreEqual(GetSigleBitsLength(1 << 1), 2);
            //Assert.AreEqual(GetSigleBitsLength(1 << 2), 3);
            Run(_SyncVar());
        }
        public IEnumerator _SyncVar()
        {
            int port = 1000;
            TcpListener tcpListener = new TcpListener(new IPEndPoint(IPAddress.Any, port));
            tcpListener.Start();
            MyServer server = new MyServer(tcpListener);
            server.Start();

            TcpClient tcpClient = new TcpClient();
            tcpClient.Connect("127.0.0.1", port);
            MyClient client = new MyClient(null, tcpClient.Client, false);
            client.Start();

            yield return null;
            yield return null;


            MyClient serverClient = (MyClient)server.Clients.FirstOrDefault();
            Assert.IsNotNull(serverClient);


            NetworkClient.RegisterObject<MySyncVarData>((id) =>
           {
               return new MySyncVarData();
           });
            var serverData = server.CreateObject<MySyncVarData>();
            serverData.AddObserver(server.Connections.First());
            yield return null;
            yield return null;
            var clientData = (MySyncVarData)client.Objects.FirstOrDefault();


            serverData.IntVar = 1;
            yield return null;
            yield return null;
            Assert.AreEqual(clientData.IntVar, serverData.IntVar);

            serverData.StringVar = "hello";
            yield return null;
            yield return null;
            Assert.AreEqual(clientData.StringVar, serverData.StringVar);

            serverData.FloatVar = 0.1f;
            yield return null;
            yield return null;
            Assert.AreEqual(clientData.FloatVar, serverData.FloatVar);


            yield return null;
            client.Stop();
            server.Stop();
            tcpListener.Stop();
        }

        byte GetSigleBitsLength(uint bits)
        {
            if (bits == 0)
                return 0;
            byte length = 0;
            while (bits != 0)
            {
                bits >>= 1;
                length++;
            }
            return length;
        }


        [NetworkObjectId("a549c043-089e-4d8a-a909-1ecb8f8f10bc")]
        class MySyncListData : NetworkObject
        {
            public SyncListString stringList = new SyncListString();
            public SyncListString stringList2 = new SyncListString();
            public SyncListStruct<MyStruct> structList1 = new SyncListStruct<MyStruct>();
        }
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 1)]
        struct MyStruct
        {
            public int int32;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 10)]
            public string str;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 100)]
            public char[] chars;
            //public string str
            //{
            //    get { return new string(chars); }
            //    set
            //    {
            //        chars = new char[100];
            //        int length = Math.Min(value.Length+1, chars.Length) - 1;
            //        value.CopyTo(0, chars, 0, length);
            //        chars[length] = '\0';
            //    }
            //}
        }


        [TestMethod]
        public void SyncLisString_Test()
        {
            Run(_SyncListString_Test());
        }
        public IEnumerator _SyncListString_Test()
        {
            int port = 1000;
            TcpListener tcpListener = new TcpListener(new IPEndPoint(IPAddress.Any, port));
            tcpListener.Start();
            MyServer server = new MyServer(tcpListener);
            server.Start();

            TcpClient tcpClient = new TcpClient();
            tcpClient.Connect("127.0.0.1", port);
            MyClient client = new MyClient(null, tcpClient.Client, false);
            client.Start();

            yield return null;
            MyClient serverClient = (MyClient)server.Clients.FirstOrDefault();


            NetworkClient.RegisterObject<MySyncListData>((id) =>
           {
               return new MySyncListData();
           });

            var serverData = (MySyncListData)server.CreateObject<MySyncListData>();
            serverData.AddObserver(server.Connections.First());
            yield return null;
            yield return null;
            var clientData = (MySyncListData)client.Objects.FirstOrDefault();

            Assert.AreEqual(serverData.stringList.Count, 0);
            serverData.stringList.Add("hello");
            yield return null;
            yield return null;
            Assert.AreEqual(clientData.stringList.Count, 1);
            Assert.AreEqual(clientData.stringList[0], serverData.stringList[0]);

            serverData.stringList.Add("world");
            yield return null;
            yield return null;
            yield return null;
            yield return null;
            Assert.AreEqual(string.Join(" ", clientData.stringList.ToArray()), "hello world");

            serverData.stringList.RemoveAt(0);
            yield return null;
            yield return null;
            Assert.AreEqual(clientData.stringList[0], "world");

            serverData.stringList.Insert(0, "hello");
            yield return null;
            yield return null;
            Assert.AreEqual(string.Join(" ", clientData.stringList.ToArray()), "hello world");

            serverData.stringList.Clear();
            yield return null;
            yield return null;
            Assert.AreEqual(clientData.stringList.Count, 0);

            serverData.stringList.Add("a");
            serverData.stringList.Add("b");
            serverData.stringList.Add("c");
            serverData.stringList.RemoveAt(1);
            yield return null;
            yield return null;
            Assert.AreEqual(string.Join("", clientData.stringList.ToArray()), "ac");

            yield return null;
            client.Stop();
            server.Stop();
            tcpListener.Stop();
        }


        [TestMethod]
        public void SyncListAfter()
        {
            Run(_SyncListAfter());
        }
        public IEnumerator _SyncListAfter()
        {
            int port = 1000;
            TcpListener tcpListener = new TcpListener(new IPEndPoint(IPAddress.Any, port));
            tcpListener.Start();
            MyServer server = new MyServer(tcpListener);
            server.Start();


            NetworkClient.RegisterObject<MySyncListData>((id) =>
           {
               return new MySyncListData();
           });

            var serverData = (MySyncListData)server.CreateObject<MySyncListData>();
            serverData.stringList.Add("hello");
            serverData.stringList.Add("world");
            yield return null;


            TcpClient tcpClient = new TcpClient();
            tcpClient.Connect("127.0.0.1", port);
            MyClient client = new MyClient(null, tcpClient.Client, false);
            client.Start();

            yield return null;
            MyClient serverClient = (MyClient)server.Clients.FirstOrDefault();
            serverData.AddObserver(server.Connections.First());
            yield return null;
            yield return null;
            var clientData = (MySyncListData)client.Objects.FirstOrDefault();

            Assert.AreEqual(serverData.stringList.Count, 2);
            Assert.AreEqual(clientData.stringList[0], "hello");
            Assert.AreEqual(clientData.stringList[1], "world");

            client.Stop();
            server.Stop();
            tcpListener.Stop();
        }

        [TestMethod]
        public void SyncList2()
        {
            Run(_SyncList2());
        }
        public IEnumerator _SyncList2()
        {
            int port = 1000;
            TcpListener tcpListener = new TcpListener(new IPEndPoint(IPAddress.Any, port));
            tcpListener.Start();
            MyServer server = new MyServer(tcpListener);
            server.Start();

            TcpClient tcpClient = new TcpClient();
            tcpClient.Connect("127.0.0.1", port);
            MyClient client = new MyClient(null, tcpClient.Client, false);
            client.Start();

            yield return null;
            MyClient serverClient = (MyClient)server.Clients.FirstOrDefault();


            NetworkClient.RegisterObject<MySyncListData>((id) =>
           {
               return new MySyncListData();
           });

            var serverData = (MySyncListData)server.CreateObject<MySyncListData>();
            serverData.AddObserver(server.Connections.First());
            yield return null;
            yield return null;
            var clientData = (MySyncListData)client.Objects.FirstOrDefault();

            Assert.AreEqual(serverData.stringList.Count, 0);
            serverData.stringList.Add("hello");
            serverData.stringList2.Add("world");
            yield return null;
            yield return null;

            Assert.AreEqual(clientData.stringList[0], "hello");
            Assert.AreEqual(clientData.stringList2[0], "world");

            serverData.stringList.Add("abc");
            serverData.stringList2.Add("123");
            yield return null;
            yield return null;
            Assert.AreEqual(clientData.stringList[1], "abc");
            Assert.AreEqual(clientData.stringList2[1], "123");

            serverData.stringList.RemoveAt(1);
            serverData.stringList2.RemoveAt(1);
            serverData.stringList.Insert(1, "world");
            serverData.stringList2.Insert(0, "hello");
            yield return null;
            yield return null;
            Assert.AreEqual(string.Join(" ", serverData.stringList.ToArray()), "hello world");
            Assert.AreEqual(string.Join(" ", clientData.stringList.ToArray()), "hello world");
            Assert.AreEqual(string.Join(" ", clientData.stringList2.ToArray()), "hello world");


            serverData.stringList2.Clear();
            yield return null;
            yield return null;
            Assert.AreEqual(clientData.stringList2.Count, 0);

            yield return null;
            client.Stop();
            server.Stop();
            tcpListener.Stop();
        }


        [TestMethod]
        public void SyncLisStruct_Test()
        {
            Run(_SyncListStruct_Test());
        }
        public IEnumerator _SyncListStruct_Test()
        {
            int port = 1000;
            TcpListener tcpListener = new TcpListener(new IPEndPoint(IPAddress.Any, port));
            tcpListener.Start();
            MyServer server = new MyServer(tcpListener);
            server.Start();

            TcpClient tcpClient = new TcpClient();
            tcpClient.Connect("127.0.0.1", port);
            MyClient client = new MyClient(null, tcpClient.Client, false);
            client.Start();

            yield return null;
            MyClient serverClient = (MyClient)server.Clients.FirstOrDefault();


            NetworkClient.RegisterObject<MySyncListData>((id) =>
           {
               return new MySyncListData();
           });
            var itemSize2 = System.Runtime.InteropServices.Marshal.SizeOf(new MyStruct() { int32 = 1, str = "hello" });
            var itemSize3 = System.Runtime.InteropServices.Marshal.SizeOf(new MyStruct() { int32 = 1, str = "helloaa" });
            Assert.AreEqual(itemSize2, itemSize3);
            var serverData = (MySyncListData)server.CreateObject<MySyncListData>();
            serverData.AddObserver(server.Connections.First());
            yield return null;
            yield return null;
            var clientData = (MySyncListData)client.Objects.FirstOrDefault();

            Assert.AreEqual(serverData.structList1.Count, 0);

            serverData.structList1.Add(new MyStruct() { int32 = 1, str = "hello" });
            GC.Collect();
            GC.WaitForPendingFinalizers();
            yield return null;
            yield return null;
            Assert.AreEqual(clientData.structList1.Count, 1);
            Assert.AreEqual(clientData.structList1[0].int32, 1);
            Assert.AreEqual(clientData.structList1[0].str, "hello");

            serverData.structList1.Add(new MyStruct() { int32 = 2, str = "world" });
            yield return null;
            yield return null;
            yield return null;
            yield return null;
            Assert.AreEqual(clientData.structList1[1].str, "world");

            serverData.structList1.RemoveAt(0);
            yield return null;
            yield return null;
            Assert.AreEqual(clientData.structList1[0].str, "world");

            serverData.structList1.Insert(0, new MyStruct() { str = "a" });
            yield return null;
            yield return null;
            Assert.AreEqual(clientData.structList1[0].str, "a");

            serverData.structList1.Clear();
            yield return null;
            yield return null;
            Assert.AreEqual(clientData.structList1.Count, 0);

            yield return null;
            client.Stop();
            server.Stop();
            tcpListener.Stop();
        }


    }
}
