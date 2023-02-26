using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using System.Reflection;

namespace Yanmonet.NetSync
{
    public static class NetworkUtility
    {

        public static string GetMethodSignature(MethodInfo method)
        {
            Type type = method.DeclaringType;
            string assemblyName = type.Assembly.GetName().Name;
            return $"{assemblyName}.dll / {method.ReturnType.FullName} {type.FullName}::{method.Name}({string.Join(",", method.GetParameters().Select(o => o.ParameterType.FullName))})";
        }

        public static uint GetMethodSignatureHash(MethodInfo method)
        {
            string sign = GetMethodSignature(method);
            return XXHash.Hash32(sign);
        }
    }
}
