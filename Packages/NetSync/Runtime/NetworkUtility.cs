#if UNITY_2021
using UnityEngine;
using YMFramework;
#endif
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yanmonet.NetSync
{
    public class NetworkUtility
    {
        public static Action<string> LogCallback;

        public static void Log(string msg)
        {
            if (LogCallback != null)
            {
                LogCallback(msg);
                return;
            }

#if UNITY_2021
            Debug.Log(msg);
#else
            Console.WriteLine(msg);
#endif

        }

        public static void Log(Exception ex)
        {
            Log(ex.Message);
        }

    }
}
