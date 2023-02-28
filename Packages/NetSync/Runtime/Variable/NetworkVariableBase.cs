using System.Collections;
using System.Collections.Generic;
using System;

namespace Yanmonet.NetSync
{
    public abstract class NetworkVariableBase : IDisposable
    {
        private NetworkObject networkObject;
        private bool isDirty;
        public readonly NetworkVariableReadPermission ReadPermission = NetworkVariableReadPermission.Everyone;

        public readonly NetworkVariableWritePermission WritePermission = NetworkVariableWritePermission.Server;

        public const NetworkVariableReadPermission DefaultReadPermission = NetworkVariableReadPermission.Everyone;

        public const NetworkVariableWritePermission DefaultWritePermission = NetworkVariableWritePermission.Server;

        protected NetworkVariableBase(
           NetworkVariableReadPermission readPermission = DefaultReadPermission,
           NetworkVariableWritePermission writePermission = DefaultWritePermission)
        {
            ReadPermission = readPermission;
            WritePermission = writePermission;
        }

        protected NetworkObject NetworkObject => networkObject;

        public string Name { get; internal set; }


        public virtual void Initialize(NetworkObject networkObject)
        {
            this.networkObject = networkObject;
        }

        public virtual bool IsDirty()
        {
            return isDirty;
        }

        public virtual void SetDirty(bool isDirty)
        {
            this.isDirty = isDirty;

            if (this.isDirty)
            {
                if (networkObject == null)
                {
                    return;
                }
                NetworkObject.SetDirty();
            }
        }

        public virtual void ResetDirty()
        {
            isDirty = false;
        }

        public bool CanClientRead(ulong clientId)
        {
            switch (ReadPermission)
            {
                default:
                case NetworkVariableReadPermission.Everyone:
                    return true;
                case NetworkVariableReadPermission.Owner:
                    return NetworkObject.OwnerClientId == clientId || NetworkManager.ServerClientId == clientId;
            }
        }

        public bool CanClientWrite(ulong clientId)
        {
            switch (WritePermission)
            {
                default:
                case NetworkVariableWritePermission.Server:
                    return NetworkManager.ServerClientId == clientId;
                case NetworkVariableWritePermission.Owner:
                    return NetworkObject.OwnerClientId == clientId;
            }
        }

        public abstract void WriteDelta(IReaderWriter writer);

        public abstract void Write(IReaderWriter writer);

        public abstract void ReadDelta(IReaderWriter reader, bool keepDirtyDelta);

        public abstract void Read(IReaderWriter reader);

        public virtual void Dispose()
        {

        }

        public override string ToString()
        {
            return $"{Name}";
        }
    }
}
