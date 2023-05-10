#if STEAMWORKSNET

using Steamworks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Yanmonet.Network.Sync.Transport.SteamNetworking;

namespace Yanmonet.Network.Sync.SteamNetworking
{
    public class LobbyTransport : INetworkTransport
    {
        private bool initalized;
        public CSteamID SteamIDLobby;
        public CSteamID SteamIDClient;
        private Callback<LobbyChatMsg_t> onLobbyChatMsgCallback;
        private Dictionary<ulong, LobbyClient> connectedClients;
        private LobbyClient serverClient;
        private DateTime startTime;
        private Queue<LobbyChatMsg> chatMsgQueue = new Queue<LobbyChatMsg>();
        private object lockObj = new object();
        private bool isServer = false;

        public bool IsSupported
        {
            get
            {
                try
                {
#if UNITY_SERVER
                    InteropHelp.TestIfAvailableGameServer();
#else
                    InteropHelp.TestIfAvailableClient();
#endif
                    return true;
                }
                catch
                {
                    return false;
                }
            }

        }
        public ulong ServerClientId => 0;

        private float NowTime => (float)DateTime.Now.Subtract(startTime).TotalSeconds;


        public void DisconnectLocalClient()
        {
            throw new NotImplementedException();
        }

        public void DisconnectRemoteClient(ulong clientId)
        {
            throw new NotImplementedException();
        }

        public void Initialize(NetworkManager networkManager)
        {
            if (!IsSupported)
            {
                if (networkManager.LogLevel <= LogLevel.Error)
                    networkManager.LogError(nameof(LobbyTransport) + " - Initialize - Steamworks.NET not ready");
                return;
            }

            if (!SteamIDLobby.IsValid())
                throw new Exception($"{nameof(SteamIDLobby)} field not set");
            startTime = DateTime.Now;
            connectedClients = new();
            if (onLobbyChatMsgCallback == null)
                onLobbyChatMsgCallback = Callback<LobbyChatMsg_t>.Create(OnLobbyChatMsg);

        }


        public bool StartServer()
        {
            if (!initalized)
            {
                Initialize(null);
            }
            isServer = true;
      

            int clientCount = SteamMatchmaking.GetNumLobbyMembers(SteamIDLobby);
            for (int i = 0; i < clientCount; i++)
            {
                var steamIDClient = SteamMatchmaking.GetLobbyMemberByIndex(SteamIDLobby, i);
                LobbyClient client = new LobbyClient(steamIDClient);
                
                connectedClients[client.SteamIDClient.m_SteamID] = client;

                chatMsgQueue.Enqueue(new LobbyChatMsg()
                {
                    SteamIDClient = client.SteamIDClient,
                    ChatEntryType = EChatEntryType.k_EChatEntryTypeInviteGame,
                });
            }

            return true;
        }

        public bool StartClient()
        {
            if (!initalized)
            {
                Initialize(null);
            }
            serverClient = new LobbyClient(SteamIDClient);
            return true;

        }
        public bool PollEvent(out NetworkEvent @event)
        {
            LobbyChatMsg msg = null;
            lock (lockObj)
            {
                if (chatMsgQueue.Count > 0)
                {
                    msg = chatMsgQueue.Dequeue();
                }
            }

            switch (msg.ChatEntryType)
            {
                case EChatEntryType.k_EChatEntryTypeInviteGame:
                    if (isServer)
                    {
                        if (!connectedClients.ContainsKey(msg.SteamIDClient.m_SteamID))
                        {
                            LobbyClient client = new LobbyClient(msg.SteamIDClient);
                            connectedClients[client.SteamIDClient.m_SteamID] = client;

                        }

                        @event = new NetworkEvent()
                        {
                            Type = NetworkEventType.Connect,
                            ClientId = msg.SteamIDClient.m_SteamID,
                            Payload = msg.Payload,
                            ReceiveTime = 0,
                        };
                        return true;
                    }
                    else
                    {
                        @event = new NetworkEvent()
                        {
                            Type = NetworkEventType.Connect,
                            ClientId = msg.SteamIDClient.m_SteamID,
                            Payload = msg.Payload,
                            ReceiveTime = 0,
                        };
                        return true;
                    }
                    break;
                case EChatEntryType.k_EChatEntryTypeDisconnected:
                    if (isServer)
                    {
                        if (connectedClients.TryGetValue(msg.SteamIDClient.m_SteamID, out var client))
                        {
                            connectedClients.Remove(client.SteamIDClient.m_SteamID);
                        }

                        @event = new NetworkEvent()
                        {
                            Type = NetworkEventType.Disconnect,
                            ClientId = msg.SteamIDClient.m_SteamID,
                            Payload = msg.Payload,
                            ReceiveTime = msg.ReceiveTime,
                        };
                        return true;
                    }
                    else
                    {
                        if (SteamIDClient == msg.SteamIDClient)
                        {
                            @event = new NetworkEvent()
                            {
                                Type = NetworkEventType.Disconnect,
                                ClientId = msg.SteamIDClient.m_SteamID,
                                Payload = msg.Payload,
                                ReceiveTime = msg.ReceiveTime,
                            };
                            return true;
                        }
                    }
                    break;
                case EChatEntryType.k_EChatEntryTypeChatMsg:
                    {
                        @event = new NetworkEvent()
                        {
                            Type = NetworkEventType.Data,
                            ClientId = msg.SteamIDClient.m_SteamID,
                            Payload = msg.Payload,
                            ReceiveTime = msg.ReceiveTime,
                        };
                        return true;
                    }
                    break;
            }


            @event = default;
            return false;
        }

        public void SendMessage(ulong clientId, ArraySegment<byte> payload, NetworkDelivery delivery)
        {
            if (clientId == 0)
                clientId = serverClient.SteamIDClient.m_SteamID;

            var msg = new LobbyChatMessage()
            {
                SteamIDClient = new CSteamID(clientId),
                Data = payload,
                delivery = delivery
            };

            byte[] writeBuffer = new byte[1024 * 1];
            MemoryStream writeSteam = new MemoryStream(writeBuffer, 0, writeBuffer.Length, true, true);
            NetworkWriter writer = new NetworkWriter(writeSteam);
            writeSteam.Position = 0;
            writeSteam.SetLength(0);
            msg.NetworkSerialize(writer);


            byte[] data = new byte[payload.Count + 1];
            payload.CopyTo(data, 0);
            data[payload.Count] = Convert.ToByte((int)delivery);

            SteamMatchmaking.SendLobbyChatMsg(SteamIDLobby, payload.Array, data.Length);
        }


        void OnLobbyChatMsg(LobbyChatMsg_t pCallback)
        {
            CSteamID SteamIDClient;

            byte[] readBuffer = new byte[1024];
            EChatEntryType ChatEntryType;
            int length = SteamMatchmaking.GetLobbyChatEntry((CSteamID)pCallback.m_ulSteamIDLobby, (int)pCallback.m_iChatID, out SteamIDClient, readBuffer, readBuffer.Length, out ChatEntryType);


            LobbyChatMsg msg = new LobbyChatMsg()
            {
                SteamIDClient = SteamIDClient,
                Payload = new ArraySegment<byte>(readBuffer, 0, length),
                ReceiveTime = NowTime,
                ChatEntryType = ChatEntryType,
            };
            lock (lockObj)
            {
                chatMsgQueue.Enqueue(msg);
            }

        }

        class LobbyChatMsg
        {
            public CSteamID SteamIDClient;
            public ArraySegment<byte> Payload;
            public float ReceiveTime;
            public EChatEntryType ChatEntryType;
        }

        public void Shutdown()
        {
            serverClient = null;
            if (onLobbyChatMsgCallback != null)
            {
                onLobbyChatMsgCallback.Dispose();
                onLobbyChatMsgCallback = null;
            }
            chatMsgQueue.Clear();
            isServer = false;
        }


        class LobbyClient
        {
            public CSteamID SteamIDClient;
            public LobbyClient(CSteamID steamIDClient)
            {
                this.SteamIDClient = steamIDClient;
            }
        }

    }
}

#endif