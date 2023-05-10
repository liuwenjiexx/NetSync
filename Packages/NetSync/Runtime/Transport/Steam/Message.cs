#if !STEAMWORKSNET
#define DISABLESTEAMWORKS
#endif
#if !(UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX || STEAMWORKS_WIN || STEAMWORKS_LIN_OSX)
#define DISABLESTEAMWORKS
#endif

#if !DISABLESTEAMWORKS

using Steamworks;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Yanmonet.Network.Sync;

namespace Yanmonet.Network.Transport.SteamNetworking
{

    class LobbyChatMessage : INetworkSerializable
    {
        public CSteamID SteamIDClient;
        public ArraySegment<byte> Data;
        public NetworkDelivery delivery;

        public void NetworkSerialize(IReaderWriter readerWriter)
        {
            ulong int64 = 0;
            byte int8 = 0;

            byte[] data = null;
            int dataLength = 0;
            if (readerWriter.IsReader)
            {
                readerWriter.SerializeValue(ref int64);
                SteamIDClient = new CSteamID(int64);

                readerWriter.SerializeValue(ref int8);
                delivery = (NetworkDelivery)int8;

                readerWriter.SerializeValue(ref data, 0, ref dataLength);
                Data = new ArraySegment<byte>(data);
            }
            else
            {
                int64 = SteamIDClient.m_SteamID;
                readerWriter.SerializeValue(ref int64);

                int8 = Convert.ToByte((int)delivery);
                readerWriter.SerializeValue(ref int8);

                data = Data.Array;
                dataLength = data.Length;
                readerWriter.SerializeValue(ref data, Data.Offset, ref dataLength);
            }


        }
    }

}

#endif