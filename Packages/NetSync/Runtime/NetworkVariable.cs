using System;
using System.Collections;
using System.Collections.Generic;

namespace Yanmonet.NetSync
{
    public class NetworkVariable<T>
    {
        public event Action<T, T> OnValueChanged;
    }
}