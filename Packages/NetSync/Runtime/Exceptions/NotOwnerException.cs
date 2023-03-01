using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yanmonet.NetSync
{
    public class NotOwnerException : Exception
    {
        public NotOwnerException(string message) : base(message) { }

        public NotOwnerException(string message, Exception innerException) : base(message, innerException) { }

    }
}
