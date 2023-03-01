using System.Collections;
using System.Collections.Generic;
using System;


namespace Yanmonet.NetSync
{
    public class NotServerException : Exception
    {
        public NotServerException() { }

        public NotServerException(string message) : base(message) { }
        public NotServerException(string message, Exception innerException) : base(message, innerException) { }

    }
}
