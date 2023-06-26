using System;
using System.Collections;
using System.Collections.Generic;
using Yanmonet.Network.Sync;

namespace Yanmonet.Network.Transport
{
    public interface INetworkTransport
    {
        bool IsSupported { get; }

        ulong ServerClientId { get; }

        abstract void Initialize(NetworkManager networkManager = null);

        abstract void StartServer();

        abstract void StartClient();


        abstract void SendMessage(ulong clientId, ArraySegment<byte> payload, NetworkDelivery delivery);

        abstract bool PollEvent(out NetworkEvent @event);

        abstract void DisconnectRemoteClient(ulong clientId);

        abstract void DisconnectLocalClient();
         

    }




}