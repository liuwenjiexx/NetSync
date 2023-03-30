using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Composition.Primitives;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Yanmonet.NetSync.Transport.Socket;

namespace Yanmonet.NetSync.Editor.Tests
{
    public class TestBase
    {

        protected string localAddress = "127.0.0.1";
        static int port = 7777;
        protected NetworkManager server;
        protected ulong serverClientId;
        protected NetworkManager client;

        protected NetworkManager serverManager;
        protected NetworkManager clientManager;
        private static string lastMethod;
        [NUnit.Framework.SetUp]
        public virtual void SetUp()
        {
            var methodName = TestContext.CurrentContext.Test.MethodName;
            var methodInfo = GetType().GetMethod(methodName);
            bool isHost = false;
            OpenNetworkAttribute attr = methodInfo.GetCustomAttribute<OpenNetworkAttribute>(true);
            if (attr != null)
            {
                isHost = attr.IsHost;
                OpenNetwork(isHost);
            }
            Thread.Sleep(10);
        }

        [TearDown]
        public virtual void TearDown()
        {
            CloseNetwork();
            lastMethod = TestContext.CurrentContext.Test.MethodName;
        }


        protected int NextPort()
        {
            return Interlocked.Increment(ref port);
        }

        static HashSet<Type> networkObjectTypes;
        protected void OpenNetwork(bool isHost = false)
        {

            _OpenNetwork(isHost);
            RegisterAllTypes(serverManager);
            RegisterAllTypes(clientManager);
        }

        protected void RegisterAllTypes(NetworkManager networkManager)
        {
            if (networkObjectTypes == null)
            {
                networkObjectTypes = new HashSet<Type>();
                foreach (var type in NetworkUtility.ReferencedAssemblies(typeof(NetworkObject).Assembly)
                    .SelectMany(o => o.GetTypes())
                    .Where(o => o.IsClass && !o.IsAbstract && o.IsSubclassOf(typeof(NetworkObject))))
                {
                    networkObjectTypes.Add(type);
                }
            }

            foreach (var type in networkObjectTypes)
            {
                networkManager.RegisterObject(type, (id) =>
                {
                    return Activator.CreateInstance(type) as NetworkObject;
                });
            }
        }
        public NetworkManager CreateNetworkManager(int port)
        {
            NetworkManager netMgr = new NetworkManager();
            SocketTransport transport = new SocketTransport();
            transport.port = port;
            netMgr.Transport = transport;

            return netMgr;
        }

        Task serverTask;
        CancellationTokenSource cancellationTokenSource;

        protected void _OpenNetwork(bool isHost = false)
        {
            Debug.Log("===== Open Network =====");
            int port = NextPort();
            Assert.IsNull(serverManager, "serverManager not null, Last Method: " + lastMethod);
            Assert.IsNull(clientManager, "clientManager not null, Last Method: " + lastMethod);

            serverManager = CreateNetworkManager(port);
            clientManager = CreateNetworkManager(port);

            if (isHost)
            {
                serverManager.StartHost();
            }
            else
            {
                serverManager.StartServer();
            }
            cancellationTokenSource = new CancellationTokenSource();
            serverTask = Task.Run(() =>
            {
                try
                {
                    while (serverManager.IsServer)
                    {
                        if (cancellationTokenSource.IsCancellationRequested)
                            break;
                        serverManager.Update();
                        Thread.Sleep(5);
                    }
                    serverManager.Shutdown();
                }catch(Exception ex)
                {
                    if (cancellationTokenSource.IsCancellationRequested)
                        return;
                    throw ex;
                }

            }, cancellationTokenSource.Token);



            clientManager.StartClient();
            server = serverManager;
            client = clientManager;
            Update(serverManager, clientManager);

            CollectionAssert.IsNotEmpty(serverManager.ConnnectedClientIds, "ConnnectedClientList empty");
            serverClientId = serverManager.ConnnectedClientIds.First();

        }

        protected void OpenNetwork<T>()
            where T : NetworkObject, new()
        {
            _OpenNetwork();
            RegisterObject<T>();
        }

        protected void RegisterObject<T>()
            where T : NetworkObject, new()
        {
            serverManager.RegisterObject<T>((id) =>
            {
                return new T();
            });
            clientManager.RegisterObject<T>((id) =>
            {
                return new T();
            });
        }
        protected void RegisterObject(Type type)
        {
            serverManager.RegisterObject(type, (id) =>
            {
                return Activator.CreateInstance(type) as NetworkObject;
            });
            clientManager.RegisterObject(type, (id) =>
            {
                return Activator.CreateInstance(type) as NetworkObject;
            });
        }

        protected void CloseNetwork()
        {
            Debug.Log("===== Close Network =====");
            if (clientManager != null)
            {
                clientManager.Shutdown();
                clientManager = null;
            }
 
            if (serverManager != null)
            {
                cancellationTokenSource.Cancel();
                serverTask.Wait();
                serverTask = null;
                serverManager = null;
            }
        }

        protected void Update()
        {
            Update(serverManager, clientManager);
        }

        protected void Update(params NetworkManager[] manager)
        {
            for (int i = 0; i < 10; i++)
            {
                foreach (var mgr in manager)
                {
                    if (mgr == serverManager)
                        continue;
                    mgr.Update();
                }
                Thread.Sleep(5);
            }
        }


    }


    public class OpenNetworkAttribute : Attribute
    {
        public bool IsHost { get; set; }
    }

}
