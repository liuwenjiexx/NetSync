using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;

[assembly: Parallelize(Workers = 1, Scope = ExecutionScope.ClassLevel)]
namespace Yanmonet.NetSync.Test
{

    //class NetworkServer : NetworkServer
    //{
    //    public NetworkServer(NetworkManager manager)
    //        : base(manager)
    //    {
    //    }

    //    protected override NetworkClient AcceptTcpClient(TcpClient client, MessageBase extra)
    //    {
    //        var c = new MyClient(this, client.Client, true, true);
    //        return c;
    //    }
    //}

    //class MyClient : NetworkClient
    //{
    //    //[SyncVar(Bits = 0x1)]
    //    //private string stringVar;
    //    //[SyncVar(Bits = 0x2)]
    //    //private int intVar;
    //    //[SyncVar(Bits = 0x4)]
    //    //private float floatVar;

    //    public MyClient(NetworkManager manager)
    //        : base(manager)
    //    {
    //    }

    //    public MyClient(NetworkServer server, Socket socket, bool ownerSocket, bool isListen)
    //    : base(server, socket, ownerSocket, isListen)
    //    {
    //    }

    //    //public string StringVar { get => stringVar; set => SetSyncVar(value, ref stringVar, 0x1); }
    //    //public int IntVar { get => intVar; set => SetSyncVar(value, ref intVar, 0x2); }
    //    //public float FloatVar { get => floatVar; set => SetSyncVar(value, ref floatVar, 0x4); }
    //}

    class MySyncVarData : NetworkObject
    {

        private NetworkVariable<string> stringVar = new NetworkVariable<string>(
            writePermission: NetworkVariableWritePermission.Server);

        private NetworkVariable<int> intVar = new NetworkVariable<int>(
            writePermission: NetworkVariableWritePermission.Server);

        private NetworkVariable<float> floatVar = new NetworkVariable<float>(
            writePermission: NetworkVariableWritePermission.Server);

        public string StringVar { get => stringVar.Value; set => stringVar.Value = value; }

        public int IntVar { get => intVar.Value; set => intVar.Value = value; }

        public float FloatVar { get => floatVar.Value; set => floatVar.Value = value; }

    }

    class ClientMySyncVarData : NetworkObject
    {

        private NetworkVariable<string> stringVar = new NetworkVariable<string>(
            writePermission: NetworkVariableWritePermission.Owner);

        private NetworkVariable<int> intVar = new NetworkVariable<int>(
            writePermission: NetworkVariableWritePermission.Owner);

        private NetworkVariable<float> floatVar = new NetworkVariable<float>(
            writePermission: NetworkVariableWritePermission.Owner);

        public string StringVar { get => stringVar.Value; set => stringVar.Value = value; }

        public int IntVar { get => intVar.Value; set => intVar.Value = value; }

        public float FloatVar { get => floatVar.Value; set => floatVar.Value = value; }

    }

    [TestClass]
    public class SyncVarTest : TestBase
    {

        [TestInitialize]
        public override void TestInitialize()
        {
            //Console.WriteLine("TestInitialize");
            //NetworkManager manager = new NetworkManager();
            //server = new NetworkServer(manager);
            //server.Start(localPort);

            //client = new MyClient(manager);
            //client.Connect(localAddress, localPort);

            //             Update(serverManager, clientManager);
            //manager.Dispose();

        }



        public void Start()
        {
            Run(_Start());
        }
        IEnumerator _Start()
        {
            NetworkManager manager = new NetworkManager();
            using (NetworkServer server = new NetworkServer(manager))
            {
                server.Start(localPort);
                Assert.IsTrue(server.IsRunning);

                NetworkClient client = new NetworkClient(manager);
                client.Connect(localAddress, localPort);
                Assert.IsTrue(client.IsRunning);

                yield return null;
                //client.Stop();
                Assert.IsFalse(client.IsRunning);
                yield return null;
            }

            manager.Dispose();
        }



        //[NetworkObjectId("1b0518d9-8c72-4764-a463-6d6eba57cb83")]
        //class ErrorRepeatBitsData : NetworkObject
        //{
        //    [SyncVar(Bits = 0x1)]
        //    private string stringVar;
        //    [SyncVar(Bits = 0x1)]
        //    private int intVar;
        //}

        //[ExpectedException(typeof(Exception))]
        //[TestMethod]
        //public void RepeatBits()
        //{
        //    NetworkClient.RegisterObject<ErrorRepeatBitsData>(id => new ErrorRepeatBitsData());
        //}


        [TestMethod]
        public void SyncVar()
        {
            OpenNetwork<MySyncVarData>();

            //Assert.AreEqual(GetSigleBitsLength(0), 0);
            //Assert.AreEqual(GetSigleBitsLength(1<<0), 1);
            //Assert.AreEqual(GetSigleBitsLength(1 << 1), 2);
            //Assert.AreEqual(GetSigleBitsLength(1 << 2), 3);

            //using (NetworkServer server = new NetworkServer())
            //{
            //    server.Start(localPort);

            //    MyClient client = new MyClient();
            //    client.Connect(localAddress, localPort);

            //             Update(serverManager, clientManager);


            var serverData = server.CreateObject<MySyncVarData>();
            serverData.Spawn();
            serverManager.Server.AddObserver(serverData, clientManager.LocalClientId);
            Update(serverManager, clientManager);
            var clientData = (MySyncVarData)client.Objects.FirstOrDefault();


            serverData.IntVar = 1;

            Update(serverManager, clientManager);

            Assert.AreEqual(1, serverData.IntVar);
            Assert.AreEqual(1, clientData.IntVar);

            serverData.StringVar = "hello";

            Update(serverManager, clientManager);
            Assert.AreEqual(clientData.StringVar, serverData.StringVar);

            serverData.FloatVar = 0.1f;
            Update(serverManager, clientManager);

            Assert.AreEqual(clientData.FloatVar, serverData.FloatVar);

            Update(serverManager, clientManager);
            //client.Stop();

        }

        [TestMethod]
        public void SyncVar_Int32_1()
        {
            OpenNetwork<MySyncVarData>();
            //using (NetworkServer server = new NetworkServer())
            //{
            //    server.Start(localPort);
            //    using (MyClient client = new MyClient())
            //    {
            //        client.Connect(localAddress, localPort);
            //                     Update(serverManager, clientManager);

            var serverData = server.CreateObject<MySyncVarData>();
            serverData.Spawn();
            serverManager.Server.AddObserver(serverData, clientManager.LocalClientId);

            serverData.IntVar = 1;
            Update(serverManager, clientManager);
            var clientData = (MySyncVarData)client.Objects.FirstOrDefault();

            Update(serverManager, clientManager);
            Assert.AreEqual(clientData.IntVar, serverData.IntVar);
            //    }

        }
        [TestMethod]
        public void SyncVar_Int32_2()
        {
            OpenNetwork<MySyncVarData>();

            //using (NetworkServer server = new NetworkServer())
            //{
            //    server.Start(localPort);
            //    using (MyClient client = new MyClient())
            //    {
            //        client.Connect(localAddress, localPort);
            //                     Update(serverManager, clientManager);

            var serverData = server.CreateObject<MySyncVarData>();
            serverData.IntVar = 1;
            serverManager.Server.AddObserver(serverData, clientManager.LocalClientId);
            serverData.Spawn();
            Update(serverManager, clientManager);
            var clientData = (MySyncVarData)client.Objects.FirstOrDefault();

            Update(serverManager, clientManager);
            Assert.AreEqual(clientData.IntVar, serverData.IntVar);
            //}

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

        class StringWrap : INetworkSerializable
        {
            public string value;

            public void NetworkSerialize(IReaderWriter readerWriter)
            {
                readerWriter.SerializeValue(ref value);
            }
        }
        class SyncListString : NetworkListSerializable<StringWrap>
        {

        }

        class MySyncListData : NetworkObject
        {
            public SyncListString stringList = new();
            public SyncListString stringList2 = new();
            public NetworkListSerializable<MyStruct> structList1 = new();


        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 1)]
        struct MyStruct : INetworkSerializable
        {
            public int int32;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 10)]
            public string str;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 100)]
            public char[] chars;

            public void NetworkSerialize(IReaderWriter readerWriter)
            {
                readerWriter.SerializeValue(ref int32);
                readerWriter.SerializeValue(ref str);
                //readerWriter.SerializeValue(ref chars);
            }
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

        /*

        [TestMethod]
        public void SyncLisString_Test()
        {
            OpenNetwork<MySyncListData>();

            var serverData = server.CreateObject<MySyncListData>();
            serverData.Spawn();
            serverManager.Server.AddObserver(serverData, clientManager.LocalClientId);
            Update(serverManager, clientManager);

            var clientData = (MySyncListData)client.Objects.FirstOrDefault();

            Assert.AreEqual(serverData.stringList.Count, 0);
            serverData.stringList.Add("hello");
            Update(serverManager, clientManager);
            Assert.AreEqual(clientData.stringList.Count, 1);
            Assert.AreEqual(clientData.stringList[0], serverData.stringList[0]);

            serverData.stringList.Add("world");
            Update(serverManager, clientManager);
            Assert.AreEqual(string.Join(" ", clientData.stringList.ToArray()), "hello world");

            serverData.stringList.RemoveAt(0);
            Update(serverManager, clientManager);
            Assert.AreEqual(clientData.stringList[0], "world");

            serverData.stringList.Insert(0, "hello");
            Update(serverManager, clientManager);
            Assert.AreEqual(string.Join(" ", clientData.stringList.ToArray()), "hello world");

            serverData.stringList.Clear();
            Update(serverManager, clientManager);
            Assert.AreEqual(clientData.stringList.Count, 0);

            serverData.stringList.Add("a");
            serverData.stringList.Add("b");
            serverData.stringList.Add("c");
            serverData.stringList.RemoveAt(1);
            Update(serverManager, clientManager);
            Assert.AreEqual(string.Join("", clientData.stringList.ToArray()), "ac");


        }


        [TestMethod]
        public void SyncListAfter()
        {
            OpenNetwork<MySyncListData>();

            var serverData = serverManager.CreateObject<MySyncListData>();
            serverData.stringList.Add("hello");
            serverData.stringList.Add("world");

            //client = new MyClient();
            //client.Connect(localAddress, localPort);
            //             Update(serverManager, clientManager);

            serverData.Spawn(serverManager.ConnnectedClientIds.First());
            Update(serverManager, clientManager);
            var clientData = (MySyncListData)client.Objects.FirstOrDefault();

            Assert.AreEqual(serverData.stringList.Count, 2);
            Assert.AreEqual(clientData.stringList[0], "hello");
            Assert.AreEqual(clientData.stringList[1], "world");

        }

        [TestMethod]
        public void SyncList2()
        {
            OpenNetwork<MySyncListData>();


            var serverData = server.CreateObject<MySyncListData>();
            serverData.Spawn(serverManager.ConnnectedClientIds.First());
            Update(serverManager, clientManager);
            var clientData = (MySyncListData)client.Objects.FirstOrDefault();

            Assert.AreEqual(serverData.stringList.Count, 0);
            serverData.stringList.Add("hello");
            serverData.stringList2.Add("world");
            Update(serverManager, clientManager);

            Assert.AreEqual(clientData.stringList[0], "hello");
            Assert.AreEqual(clientData.stringList2[0], "world");

            serverData.stringList.Add("abc");
            serverData.stringList2.Add("123");
            Update(serverManager, clientManager);
            Assert.AreEqual(clientData.stringList[1], "abc");
            Assert.AreEqual(clientData.stringList2[1], "123");

            serverData.stringList.RemoveAt(1);
            serverData.stringList2.RemoveAt(1);
            serverData.stringList.Insert(1, "world");
            serverData.stringList2.Insert(0, "hello");
            Update(serverManager, clientManager);
            Assert.AreEqual(string.Join(" ", serverData.stringList.ToArray()), "hello world");
            Assert.AreEqual(string.Join(" ", clientData.stringList.ToArray()), "hello world");
            Assert.AreEqual(string.Join(" ", clientData.stringList2.ToArray()), "hello world");


            serverData.stringList2.Clear();
            Update(serverManager, clientManager);
            Assert.AreEqual(clientData.stringList2.Count, 0);
            //             Update(serverManager, clientManager);
            // client.Stop();

        }


        [TestMethod]
        public void SyncLisStruct_Test()
        {
            OpenNetwork<MySyncListData>();

            var itemSize2 = Marshal.SizeOf(new MyStruct() { int32 = 1, str = "hello" });
            var itemSize3 = Marshal.SizeOf(new MyStruct() { int32 = 1, str = "helloaa" });
            Assert.AreEqual(itemSize2, itemSize3);
            var serverData = serverManager.CreateObject<MySyncListData>();
            serverData.Spawn(serverManager.ConnnectedClientIds.First());
            Update(serverManager, clientManager);
            var clientData = (MySyncListData)client.Objects.FirstOrDefault();

            Assert.AreEqual(serverData.structList1.Count, 0);

            serverData.structList1.Add(new MyStruct() { int32 = 1, str = "hello" });
            GC.Collect();
            GC.WaitForPendingFinalizers();
            Update(serverManager, clientManager);
            Assert.AreEqual(clientData.structList1.Count, 1);
            Assert.AreEqual(clientData.structList1[0].int32, 1);
            Assert.AreEqual(clientData.structList1[0].str, "hello");

            serverData.structList1.Add(new MyStruct() { int32 = 2, str = "world" });
            Update(serverManager, clientManager);
            Assert.AreEqual(clientData.structList1[1].str, "world");

            serverData.structList1.RemoveAt(0);
            Update(serverManager, clientManager);
            Assert.AreEqual(clientData.structList1[0].str, "world");

            serverData.structList1.Insert(0, new MyStruct() { str = "a" });
            Update(serverManager, clientManager);
            Assert.AreEqual(clientData.structList1[0].str, "a");

            serverData.structList1.Clear();
            Update(serverManager, clientManager);
            Assert.AreEqual(clientData.structList1.Count, 0);

        }
        */
        [TestMethod]
        public void ServerVariable()
        {
            OpenNetwork<MySyncVarData>();

            var serverData = serverManager.CreateObject<MySyncVarData>();
            serverData.SpawnWithOwnership(clientManager.LocalClientId);
            serverData.AddObserver(clientManager.LocalClientId);
            Update();
            var clientData = (MySyncVarData)client.Objects.First();

            Assert.AreEqual(0, serverData.IntVar);
            Assert.AreEqual(0, clientData.IntVar);

            serverData.IntVar = 1;
            Update();
            Assert.AreEqual(1, serverData.IntVar);
            Assert.AreEqual(1, clientData.IntVar);
        }

        [TestMethod]
        public void ServerVariable_ClientWriteError()
        {
            OpenNetwork<MySyncVarData>();

            var serverData = serverManager.CreateObject<MySyncVarData>();
            serverData.SpawnWithOwnership(clientManager.LocalClientId);
            serverData.AddObserver(clientManager.LocalClientId);
            Update();
            var clientData = (MySyncVarData)client.Objects.First();

            Assert.ThrowsException<InvalidOperationException>(() => clientData.IntVar = 1);
        }

        [TestMethod]
        public void ClientVariable()
        {
            OpenNetwork<ClientMySyncVarData>();

            var serverData = serverManager.CreateObject<ClientMySyncVarData>();
            serverData.SpawnWithOwnership(clientManager.LocalClientId);
            Update();
            var clientData = (ClientMySyncVarData)client.Objects.First();

            Assert.AreEqual(0, serverData.IntVar);
            Assert.AreEqual(0, clientData.IntVar);

            clientData.IntVar = 1;
            Update();
            Update();
            Update();
            Assert.AreEqual(1, clientData.IntVar);
            Assert.AreEqual(1, serverData.IntVar);
        }
        [TestMethod]
        public void ClientVariable_ServerWriteError()
        {
            OpenNetwork<ClientMySyncVarData>();

            var serverData = serverManager.CreateObject<ClientMySyncVarData>();
            serverData.SpawnWithOwnership(clientManager.LocalClientId);
            Update();
            
            Assert.ThrowsException<InvalidOperationException>(() => serverData.IntVar = 1);

        }
    }
}
