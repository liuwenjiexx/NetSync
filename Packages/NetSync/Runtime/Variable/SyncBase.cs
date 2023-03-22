using System.Collections;
using System.Collections.Generic;
using System;

namespace Yanmonet.NetSync
{
    public abstract class SyncBase : IDisposable
    {
        internal NetworkObject networkObject;
        private bool isDirty;
        public readonly SyncReadPermission ReadPermission;

        public readonly SyncWritePermission WritePermission;

        public const SyncReadPermission DefaultReadPermission = SyncReadPermission.Everyone;

        public const SyncWritePermission DefaultWritePermission = SyncWritePermission.Server;

        protected SyncBase(
           SyncReadPermission readPermission = DefaultReadPermission,
           SyncWritePermission writePermission = DefaultWritePermission)
        {
            ReadPermission = readPermission;
            WritePermission = writePermission;
        }

        protected NetworkObject NetworkObject => networkObject;

        public string Name { get; internal set; }


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
                case SyncReadPermission.Everyone:
                    return true;
                case SyncReadPermission.Owner:
                    return NetworkObject.OwnerClientId == clientId || NetworkManager.ServerClientId == clientId;
            }
        }

        public bool CanClientWrite(ulong clientId)
        {
            switch (WritePermission)
            {
                default:
                case SyncWritePermission.Server:
                    return NetworkManager.ServerClientId == clientId;
                case SyncWritePermission.Owner:
                    return NetworkObject.OwnerClientId == clientId;
            }
        }

        protected void CheckWrite()
        {
            if (NetworkObject != null && NetworkObject.IsSpawned && !CanClientWrite(NetworkObject.NetworkManager.LocalClientId))
            {
                throw new InvalidOperationException("Client is not allowed to write to this Sync Variable");
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
