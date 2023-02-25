using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Yanmonet.NetSync
{
    public class NetworkVariable<T>
    {
        public event Action<T, T> OnValueChanged;
    }
}