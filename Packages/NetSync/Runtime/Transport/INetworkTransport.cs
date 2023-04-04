using System;
using System.Collections;
using System.Collections.Generic;


namespace Yanmonet.NetSync
{
    public interface INetworkTransport
    {
        bool IsSupported { get; }

        ulong ServerClientId { get; }

        abstract void Initialize(NetworkManager networkManager = null);

        abstract bool StartServer();

        abstract bool StartClient();


        abstract void Send(ulong clientId, ArraySegment<byte> payload, NetworkDelivery delivery);

        abstract bool PollEvent(out NetworkEvent @event);

        abstract void DisconnectRemoteClient(ulong clientId);

        abstract void DisconnectLocalClient();

        abstract void Shutdown();

    }




}