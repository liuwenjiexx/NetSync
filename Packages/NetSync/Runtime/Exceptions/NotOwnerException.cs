using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yanmonet.Network.Sync
{
    public class NotOwnerException : Exception
    {
        public NotOwnerException() : this(null) { }
        public NotOwnerException(string message) : base(message) { }

        public NotOwnerException(string message, Exception innerException) : base(message, innerException) { }

    }
}
