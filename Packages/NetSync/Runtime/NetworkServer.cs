using Yanmonet.NetSync.Messages;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Runtime.Serialization;
using Yanmonet.NetSync.Transport.Socket;

namespace Yanmonet.NetSync
{
    public class NetworkServer : IDisposable
    {
        private bool isRunning;
        INetworkTransport transport;


        internal uint nextObjectId;
        private Type clientType;
        Task acceptClientTask;
        private int mainThreadId;
        private object lockObj;
        CancellationTokenSource cancellationTokenSource;

        public NetworkServer(NetworkManager manager)
        {
            lockObj = new object();
            this.networkManager = manager;
            transport = networkManager.Transport;
        }

        protected bool IsThreadSafe
        {
            get { return mainThreadId == 0 || mainThreadId == Thread.CurrentThread.ManagedThreadId; }
        }

        public bool IsRunning
        {
            get { return isRunning; }
        }


        public Type ClientType { get => clientType; set => clientType = value; }
        private NetworkManager networkManager;

        public NetworkManager NetworkManager => networkManager ?? NetworkManager.Singleton;
        public event Action<NetworkServer> Started;
        public event Action<NetworkServer> Stoped;


        private NetworkClient client;


        public virtual void Stop()
        {
            CheckThreadSafe();
            if (!isRunning)
                return;

            isRunning = false;


            cancellationTokenSource.Cancel();
              
            NetworkManager.clients.Clear();
            NetworkManager.clientIds.Clear();

            try
            {
                NetworkManager.Log("Server Stop");

                transport.Shutdown();
            }
            catch { }
            try
            {
                if (Stoped != null)
                    Stoped(this);
            }
            catch (Exception ex)
            {
                NetworkManager.LogException(ex);
            }
            NetworkManager.Log("Network Server Stoped");
        }



        public void Update()
        {
            if (!isRunning)
                throw new Exception("Server not start");
             
            //while (node != null)
            //{
            //    client = node.Value;
            //    if (client == null || !(client.IsConnecting || client.IsConnected))
            //    {
            //        node = node.RemoveAndNext();
            //        //NetworkManager.Log($"Client {client.ClientId} Disconnected, IsConnecting: {client.Connection.IsConnecting}, IsConnected: {client.Connection.IsConnected}");
            //        OnClientDisconnect(client);
            //        ClientDisconnected?.Invoke(this, client);
            //        continue;
            //    }

            //    client.Update();
            //    node = node.Next;
            //}




        }




        //public NetworkClient FindClient(ulong clientId)
        //{
        //    if (clientId == NetworkManager.LocalClientId)
        //        return NetworkManager.LocalClient;

        //    return networkManager.GetClient(clientId);
        //}

  
         
         


        //public void RemoveObject(ulong instanceId)
        //{
        //    destoryObjIds.Add(instanceId);
        //}



        protected void CheckThreadSafe()
        {
            return;
            //if (mainThreadId == 0)
            //    return;
            //if (!IsThreadSafe)
            //{               
            //    throw new Exception("Not main thread " + mainThreadId + "," + Thread.CurrentThread.ManagedThreadId);
            //}
        }
 
        public virtual void Dispose()
        {
            Stop();
        }

        ~NetworkServer()
        {
            Dispose();
        }

    }
}

