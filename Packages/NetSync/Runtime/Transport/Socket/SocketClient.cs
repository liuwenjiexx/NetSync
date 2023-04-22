using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Yanmonet.NetSync.Transport.Socket
{
    using Socket = System.Net.Sockets.Socket;

    internal class SocketClient
    {
        public ulong ClientId;
        public Socket socket;
        public bool IsAccept;
        public bool IsConnected;
        public bool WillDisconnect;
        public bool IsLocalClient;
        public bool processConnectEvent;
        public bool socketDisposed;
        public CancellationTokenSource cancellationTokenSource;
        public int pingCount;

        public AutoResetEvent sendEvent;
        public AutoResetEvent receiveEvent;
        public Task sendWorkerTask;
        public Task receiveWorkerTask;
        public Queue<Packet> sendPacketQueue;
        public Queue<Packet> receivePacketQueue;


        public SocketClient(Socket socket)
        {
            this.socket = socket;
            sendEvent = new AutoResetEvent(false);
            receiveEvent = new AutoResetEvent(false);
            sendPacketQueue = new Queue<Packet>();
            receivePacketQueue = new Queue<Packet>();
            IsLocalClient = false;
            IsAccept = false;
            IsConnected = false;
            ClientId = ulong.MaxValue;
        }

        public void Send(byte[] buffer)
        {
            if (buffer == null) return;
            Send(buffer, 0, buffer.Length);
        }

        public void Send(byte[] buffer, int offset, int length)
        {
            for (int i = 0; i < length;)
            {
                int sendCount = socket.Send(buffer, offset + i, length - i, SocketFlags.None);
                if (sendCount > 0)
                {
                    i += sendCount;
                }
            }
        }


    }
}
