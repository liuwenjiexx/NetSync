using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Composition.Primitives;
using System.Linq;
using System.Reflection;
using System.Threading;
using UnityEngine;

namespace Yanmonet.NetSync.Editor.Tests
{
    public class TestBase
    {

        protected string localAddress = "127.0.0.1";
        static int port = 7777;
        protected NetworkServer server;
        protected NetworkClient serverClient;
        protected NetworkClient client;

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

        protected void _OpenNetwork(bool isHost = false)
        {
            Debug.Log("===== Open Network =====");
            int port = NextPort();
            Assert.IsNull(serverManager, "serverManager not null, Last Method: " + lastMethod);
            Assert.IsNull(clientManager, "clientManager not null, Last Method: " + lastMethod);

            serverManager = new NetworkManager();
            clientManager = new NetworkManager();

            serverManager.port = port;
            clientManager.port = port;
            if (isHost)
            {
                serverManager.StartHost();
            }
            else
            {
                serverManager.StartServer();
            }
            clientManager.StartClient();
            server = serverManager.Server;
            client = clientManager.LocalClient;
            Update(serverManager, clientManager);

            CollectionAssert.IsNotEmpty(serverManager.ConnnectedClientList, "ConnnectedClientList empty");
            serverClient = serverManager.ConnnectedClientList.First();

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
                clientManager.Dispose();
                clientManager = null;
            }

            if (serverManager != null)
            {
                serverManager.Dispose();
                serverManager = null;
            }
        }

        protected void Update()
        {
            Update(serverManager, clientManager);
        }

        protected void Update(params NetworkManager[] manager)
        {
            for (int i = 0; i < 5; i++)
            {
                foreach (var mgr in manager)
                {
                    mgr.Update();
                }
            }
        }

        protected void Update(params NetworkConnection[] connections)
        {
            for (int i = 0; i < 3; i++)
            {
                foreach (var conn in connections)
                {
                    conn.Update();
                }
            }
        }
    }


    public class OpenNetworkAttribute : Attribute
    {
        public bool IsHost { get; set; }
    }

}
