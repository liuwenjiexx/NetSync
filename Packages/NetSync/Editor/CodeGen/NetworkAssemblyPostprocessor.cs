using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using Yanmonet.AssemblyPostprocessing.Editor;
using Yanmonet.Injection.Assembly.Editor;

namespace Yanmonet.NetSync.Editor.CodeGen
{


    /// <summary>
    /// 网络程序集注入器
    /// </summary>
    [InitializeOnLoad]
    class NetworkAssemblyPostprocessor : IAssemblyPostprocessor
    {
        public int callbackOrder => 0;


        static HashSet<string> excludeAssemblies;

        #region Rpc 方法

        static string BeginServerRpcMethodName = "__BeginServerRpc__";
        static string EndServerRpcMethodName = "__EndServerRpc__";
        static string ReturnServerRpcMethodName = "__ReturnServerRpc__";
        static string BeginClientRpcMethodName = "__BeginClientRpc__";
        static string EndClientRpcMethodName = "__EndClientRpc__";
        static string ReturnClientRpcMethodName = "__ReturnClientRpc__";

        static MethodInfo BeginServerRpcMethod;
        static MethodInfo EndServerRpcMethod;
        static MethodInfo ReturnServerRpcMethod;
        static MethodInfo BeginClientRpcMethod;
        static MethodInfo EndClientRpcMethod;
        static MethodInfo ReturnClientRpcMethod;

        static Type NetworkObjectType = typeof(NetworkObject);
        static TypeReference NetworkObjectRef;
        static Type ServerRpcParamsType = typeof(ServerRpcParams);
        static Type ClientRpcParamsType = typeof(ClientRpcParams);

        static TypeReference ServerRpcAttrRef;
        static TypeReference ServerRpcParamsRef;
        static MethodReference BeginServerRpcMethodRef;
        static MethodReference EndServerRpcMethodRef;
        static MethodReference ReturnServerRpcMethodRef;

        static TypeReference ClientRpcAttrRef;
        static TypeReference ClientRpcParamsRef;
        static MethodReference BeginClientRpcMethodRef;
        static MethodReference EndClientRpcMethodRef;
        static MethodReference ReturnClientRpcMethodRef;

        static MethodReference NewServerRpcParamsRef;
        static MethodReference NewClientRpcParamsRef;


        #endregion


        public static bool IsEnabled
        {
            get => true;
        }

        public int Order => 0;


        static HashSet<string> GetAllNetworkAssemblyNames()
        {
            HashSet<string> assemblyNames = new();
            foreach (var assembly in NetworkUtility.ReferencedAssemblies(typeof(RpcAttribute).Assembly))
            {
                try
                {
                    string path = assembly.Location;
                    if (string.IsNullOrEmpty(path))
                        continue;
                    string assemblyName = Path.GetFileNameWithoutExtension(path);
                    if (IsExcludeAssembly(assemblyName))
                        continue;
                    assemblyNames.Add(assemblyName);
                }
                catch { }
            }

            return assemblyNames;
        }



        public static bool IsExcludeAssembly(string assemblyName)
        {
            if (excludeAssemblies == null)
            {
                excludeAssemblies = new HashSet<string>
                {
                    typeof(NetworkAssemblyPostprocessor).Assembly.GetName().Name,
                    typeof(RpcAttribute).Assembly.GetName().Name
                };
            }

            return excludeAssemblies.Contains(assemblyName);
        }

        public bool CanPostprocessAssembly(string assemblyName, string assemblyPath)
        {
            if (!IsEnabled)
                return false;

            if (IsExcludeAssembly(assemblyName))
                return false;

            var allAssembly = GetAllNetworkAssemblyNames();
            return allAssembly.Contains(assemblyName);
        }



        public bool CanPostprocessAssembly(IAssembly buildAssembly)
        {
            string networkAssemblyName = typeof(RpcAttribute).Assembly.GetName().Name;
            if (!buildAssembly.References.Any(o => o == networkAssemblyName))
                return false;

            return true;
        }


        public void BeginPostprocessAssembly(IAssembly buildAssembly)
        {
            AssemblyDefinition assemblyDef = buildAssembly.AssemblyDefinition;
            var mainModule = assemblyDef.MainModule;

            if (BeginServerRpcMethod == null)
            {
                BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
                BeginServerRpcMethod = NetworkObjectType.GetMethod(BeginServerRpcMethodName, bindingFlags);
                EndServerRpcMethod = NetworkObjectType.GetMethod(EndServerRpcMethodName, bindingFlags);
                ReturnServerRpcMethod = NetworkObjectType.GetMethod(ReturnServerRpcMethodName, bindingFlags);
                BeginClientRpcMethod = NetworkObjectType.GetMethod(BeginClientRpcMethodName, bindingFlags);
                EndClientRpcMethod = NetworkObjectType.GetMethod(EndClientRpcMethodName, bindingFlags);
                ReturnClientRpcMethod = NetworkObjectType.GetMethod(ReturnClientRpcMethodName, bindingFlags);

            }


            ServerRpcAttrRef = mainModule.ImportReference(typeof(ServerRpcAttribute));
            ServerRpcParamsRef = mainModule.ImportReference(typeof(ServerRpcParams));
            ClientRpcAttrRef = mainModule.ImportReference(typeof(ClientRpcAttribute));
            ClientRpcParamsRef = mainModule.ImportReference(typeof(ClientRpcParams));
            NetworkObjectRef = mainModule.ImportReference(typeof(NetworkObject));

            BeginServerRpcMethodRef = mainModule.ImportReference(BeginServerRpcMethod);
            EndServerRpcMethodRef = mainModule.ImportReference(EndServerRpcMethod);
            ReturnServerRpcMethodRef = mainModule.ImportReference(ReturnServerRpcMethod);
            BeginClientRpcMethodRef = mainModule.ImportReference(BeginClientRpcMethod);
            EndClientRpcMethodRef = mainModule.ImportReference(EndClientRpcMethod);
            ReturnClientRpcMethodRef = mainModule.ImportReference(ReturnClientRpcMethod);
        }

        public void EndPostprocessAssembly(IAssembly buildAssembly)
        {

        }

        public bool PostprocessAssembly(IAssembly buildAssembly)
        {
            //Debug.Log($"Inject NetSync assembly path: '{path}', Editor: {Application.isEditor}");

            AssemblyDefinition assemblyDef = buildAssembly.AssemblyDefinition;
            var mainModule = assemblyDef.MainModule;

            bool changed = false;

            //assembly.MainModule.Types 不包含嵌套类型
            foreach (var type in assemblyDef.MainModule.GetAllTypes())
            {
                if (!type.IsClass)
                    continue;
                bool isNetObjType = false;
                var bt = type.BaseType;

                if (!type.IsSubclassOf(NetworkObjectRef))
                    continue;


                foreach (var method in type.GetMethods())
                {
                    var il = method.Body.GetILProcessor();
                    var builder = new ILBuilder(il, method);
                    if (ProcessServerRpcMethod(assemblyDef, method, builder))
                    {
                        changed = true;
                    }
                    else if (ProcessClientRpcMethod(assemblyDef, method, builder))
                    {
                        changed = true;
                    }

                }
            }

            return changed;
        }

        static bool ProcessServerRpcMethod(AssemblyDefinition assembly, MethodDefinition method, ILBuilder builder)
        {
            var mainModule = assembly.MainModule;

            CustomAttribute serverRpcAttr = null;
            bool hasClientRpc = false;
            if (method.HasCustomAttributes)
            {
                foreach (var attr in method.CustomAttributes)
                {
                    if (attr.AttributeType.FullName == ServerRpcAttrRef.FullName)
                    {
                        if (serverRpcAttr != null)
                            throw new Exception($"Method [{method.FullName}] Repeat use [{nameof(ServerRpcAttribute)}] Attribute");
                        serverRpcAttr = attr;
                    }
                    else if (attr.AttributeType.FullName == ClientRpcAttrRef.FullName)
                    {
                        hasClientRpc = true;
                    }
                }
            }

            if (serverRpcAttr == null)
                return false;

            if (hasClientRpc)
                throw new Exception($"Server Rpc Method [{method.FullName}]  can't use [{nameof(ClientRpcAttribute)}] attribute");

            builder.InsertPoint = builder.FirstOrCreate();
            builder.Nop();

            int argCount = 0;
            var ps = method.Parameters;
            int rpcParamsIndex = -1;
            for (int i = 0; i < ps.Count; i++)
            {
                var p = ps[i];
                if (p.ParameterType.FullName == ServerRpcParamsRef.FullName)
                {
                    if (rpcParamsIndex != -1)
                        throw new Exception($"Server Rpc Method [{method.FullName}] Repeat [{nameof(ServerRpcParams)}] parameter");
                    rpcParamsIndex = i;
                    continue;
                }
                else if (p.ParameterType.FullName == ClientRpcAttrRef.FullName)
                {
                    throw new Exception($"Server Rpc Method [{method.FullName}] can't use [{nameof(ClientRpcParams)}] parameter");
                }
                argCount++;
            }
            var argsVar = builder.NewArrayVariable(mainModule.TypeSystem.Object, argCount);
            for (int i = 0, j = 0; i < ps.Count; i++)
            {
                var p = ps[i];
                if (p.ParameterType.FullName == ServerRpcParamsRef.FullName)
                {
                    continue;
                }
                builder.Emit(OpCodes.Ldloc, argsVar);
                builder.Emit(OpCodes.Ldc_I4, j);
                builder.Emit(OpCodes.Ldarg, i + 1);
                if (p.ParameterType.IsValueType)
                {
                    builder.Emit(OpCodes.Box, p.ParameterType);
                }
                else
                {
                    builder.Emit(OpCodes.Castclass, builder.TypeSystem.Object);
                }
                builder.Emit(OpCodes.Stelem_Ref);
                j++;
            }

            //call: _BeginServerRpc_
            if (rpcParamsIndex == -1)
            {
                var rpcParamsVar = builder.NewVariable(ServerRpcParamsRef);
                //结构体变量使用地址：Ldloca_S
                builder.Emit(OpCodes.Ldloca_S, rpcParamsVar);
                builder.Emit(OpCodes.Initobj, ServerRpcParamsRef);

                builder.LoadThis()
                    .Load(method.Name)
                    .Load(rpcParamsVar)
                    .Load(argsVar)
                    .Emit(OpCodes.Call, BeginServerRpcMethodRef);
            }
            else
            {
                builder.LoadThis()
                    .Load(method.Name)
                    .Emit(OpCodes.Ldarg, rpcParamsIndex + 1)
                    .Load(argsVar)
                    .Emit(OpCodes.Call, BeginServerRpcMethodRef);
            }
            builder.Nop();

            //call: _EndServerRpc_
            builder.LoadThis()
                .Call(EndServerRpcMethodRef);
            builder.Nop();

            builder.BeginBlock();
            {
                //call: _ReturnServerRpc_
                var isReturnVar = builder.NewVariable(mainModule.TypeSystem.Boolean);
                builder.LoadThis()
                    .Call(ReturnServerRpcMethodRef);
                builder.Set(isReturnVar);

                builder.Load(isReturnVar);
                builder.IfTrueBreakBlock();
                var returnPoint = builder.Body.Instructions.First(o => o.OpCode == OpCodes.Ret);
                builder.Emit(OpCodes.Br, returnPoint);
            }
            builder.EndBlock();

            return true;
        }

        static bool ProcessClientRpcMethod(AssemblyDefinition assembly, MethodDefinition method, ILBuilder builder)
        {
            var mainModule = assembly.MainModule;

            CustomAttribute clientRpcAttr = null;
            if (!method.HasCustomAttributes)
                return false;
            bool hasServerRpc = false;
            foreach (var attr in method.CustomAttributes)
            {
                if (attr.AttributeType.FullName == ClientRpcAttrRef.FullName)
                {
                    if (clientRpcAttr != null)
                        throw new Exception($"Method [{method.FullName}] Repeat use [{nameof(ClientRpcAttribute)}]");
                    clientRpcAttr = attr;
                }
                else if (attr.AttributeType.FullName == ServerRpcAttrRef.FullName)
                {
                    hasServerRpc = true;
                }

            }

            if (clientRpcAttr == null) return false;

            if (hasServerRpc) throw new Exception($"Client Rpc Method [{method.FullName}] can't use [{nameof(ServerRpcAttribute)}] attribute");

            builder.InsertPoint = builder.FirstOrCreate();
            builder.Nop();

            int argCount = 0;
            var ps = method.Parameters;
            int rpcParamsIndex = -1;
            for (int i = 0; i < ps.Count; i++)
            {
                var p = ps[i];
                if (p.ParameterType.FullName == ClientRpcParamsRef.FullName)
                {
                    if (rpcParamsIndex != -1)
                        throw new Exception($"Method [{method.FullName}] Repeat [{nameof(ClientRpcParams)}] parameter");
                    rpcParamsIndex = i;
                    continue;
                }
                else if (p.ParameterType.FullName == ServerRpcAttrRef.FullName)
                {
                    throw new Exception($"Client Rpc Method [{method.FullName}] can't use [{nameof(ServerRpcParams)}] parameter");
                }
                argCount++;
            }
            var argsVar = builder.NewArrayVariable(mainModule.TypeSystem.Object, argCount);
            for (int i = 0, j = 0; i < ps.Count; i++)
            {
                var p = ps[i];
                if (p.ParameterType.FullName == ClientRpcParamsRef.FullName)
                {
                    continue;
                }
                builder.Emit(OpCodes.Ldloc, argsVar);
                builder.Emit(OpCodes.Ldc_I4, j);
                builder.Emit(OpCodes.Ldarg, i + 1);
                if (p.ParameterType.IsValueType)
                {
                    builder.Emit(OpCodes.Box, p.ParameterType);
                }
                else
                {
                    builder.Emit(OpCodes.Castclass, builder.TypeSystem.Object);
                }
                builder.Emit(OpCodes.Stelem_Ref);
                j++;
            }

            //call: _BeginClientRpc_
            if (rpcParamsIndex == -1)
            {
                var rpcParamsVar = builder.NewVariable(ClientRpcParamsRef);
                builder.Emit(OpCodes.Ldloca_S, rpcParamsVar);
                builder.Emit(OpCodes.Initobj, ServerRpcParamsRef);

                builder.LoadThis()
                    .Load(method.Name)
                    .Load(rpcParamsVar)
                    .Load(argsVar)
                    .Emit(OpCodes.Call, BeginClientRpcMethodRef);
            }
            else
            {
                builder.LoadThis()
                    .Load(method.Name)
                    .Emit(OpCodes.Ldarg, rpcParamsIndex + 1)
                    .Load(argsVar)
                    .Emit(OpCodes.Call, BeginClientRpcMethodRef);
            }
            builder.Nop();

            //call: _EndClientRpc_
            builder.LoadThis()
                .Call(EndClientRpcMethodRef);
            builder.Nop();


            //call: _ReturnClientRpc_
            builder.BeginBlock();
            {
                var isReturnVar = builder.NewVariable(mainModule.TypeSystem.Boolean);
                builder.LoadThis()
                    .Call(ReturnClientRpcMethodRef);
                builder.Set(isReturnVar);

                builder.Load(isReturnVar);
                builder.IfTrueBreakBlock();
                var returnPoint = builder.Body.Instructions.First(o => o.OpCode == OpCodes.Ret);
                builder.Emit(OpCodes.Br_S, returnPoint);
            }
            builder.EndBlock();


            return true;
        }
    }
}