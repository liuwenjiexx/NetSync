using System.Collections;
using System.Collections.Generic;

namespace Yanmonet.NetSync.Transport.Socket
{
    class EmptyMessage : INetworkSerializable
    {
        public bool isRequest;
        public void NetworkSerialize(IReaderWriter readerWriter)
        {
            readerWriter.SerializeValue(ref isRequest);
        }
    }

    class ConnectRequestMessage : INetworkSerializable
    {
        public byte[] Payload;

        public void NetworkSerialize(IReaderWriter readerWriter)
        {
            if (readerWriter.IsReader)
            {
                Payload = null;
                int length = 0;
                readerWriter.SerializeValue(ref Payload, 0, ref length);
            }
            else
            {
                int length = 0;
                if (Payload != null)
                    length = Payload.Length;
                readerWriter.SerializeValue(ref Payload, 0, ref length);
            }
        }
    }

    class ConnectResponseMessage : INetworkSerializable
    {
        public bool Success;
        public string Reson;
        public ulong ClientId;

        public void NetworkSerialize(IReaderWriter readerWriter)
        {
            readerWriter.SerializeValue(ref Success);
            readerWriter.SerializeValue(ref Reson);
            readerWriter.SerializeValue(ref ClientId);

        }
    }


}
