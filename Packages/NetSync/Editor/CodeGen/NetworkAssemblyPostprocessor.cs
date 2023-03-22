using System.Collections;
using System.Collections.Generic;
using UnityEditor.Compilation;
using UnityEditor;
using UnityEngine;
using System.IO;
using System.Reflection;
using System.Diagnostics;
using System;
using Mono.Cecil;
using Mono.Cecil.Mdb;
using Mono.Cecil.Pdb;
using Debug = UnityEngine.Debug;
using Mono.Cecil.Rocks;
using System.Linq;
using Mono.Cecil.Cil;
using System.Text;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace Yanmonet.NetSync.Editor.CodeGen
{
    /// <summary>
    /// 网络程序集注入器
    /// </summary>
    [InitializeOnLoad]
    class NetworkAssemblyPostprocessor
#if UNITY_2019_1_OR_NEWER
      : IPostBuildPlayerScriptDLLs
#endif
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
         
         
        [InitializeOnLoadMethod]
        static void InitializeOnLoadMethod()
        {
            if (!IsEnabled)
                return;
             

            //CompilationPipeline.compilationStarted += CompilationPipeline_compilationStarted;
            CompilationPipeline.assemblyCompilationFinished += CompilationPipeline_assemblyCompilationFinished;
            //CompilationPipeline.compilationFinished += CompilationPipeline_compilationFinished;

        }

        static NetworkAssemblyPostprocessor()
        {
            if (!IsEnabled)
                return;

            AssemblyReloadEvents.beforeAssemblyReload += AssemblyReloadEvents_beforeAssemblyReload;

            if (IsEditorFirstStarted)
            {
                ProcessAllAssembly();
            }

        }


        private static void AssemblyReloadEvents_beforeAssemblyReload()
        {

            //List<string> paths = new();
            //foreach (var assembly in NetworkUtility.ReferencedAssemblies(typeof(RpcAttribute).Assembly))
            //{
            //    string assemblyName = assembly.GetName().Name;

            //    string file = assembly.Location;

            //    if (File.Exists(file))
            //    {
            //        paths.Add(file);
            //    }
            //}

            //ProcessAssembly(paths.ToArray());
            //ProcessAllAssembly();
        }

        private static List<string> assemblyPaths;



        private static void CompilationPipeline_compilationStarted(object obj)
        {
            if (assemblyPaths == null)
                assemblyPaths = new();
            assemblyPaths.Clear();
        }

        private static void CompilationPipeline_assemblyCompilationFinished(string path, CompilerMessage[] arg2)
        {
            string assemblyName = Path.GetFileNameWithoutExtension(path);
            if (IsNetworkAssembly(assemblyName))
            {
                ProcessAssembly(new string[] { path });
            }

            assemblyPaths?.Add(path);
        }

        private static void CompilationPipeline_compilationFinished(object obj)
        {
            //obj: Editor Compilation
            //EditorAssemblyInjectionUtility.Log("Compilation Finished:\n" + string.Join("\n", assemblyPaths));
            //    if (!Application.isPlaying)
            {
                //   if (EditorAssemblyInjectionUtility.IsEnabled())
                {

                    //InjectAssembly(assemblyPaths.ToArray());

                    // if (changed.Length > 0)
                    {
                        // if (EditorAssemblyInjectionUtility.IsEditor)
                        {
                            //EditorUtility.RequestScriptReload();
                        }
                    }
                }
            }
        }

        public static bool IsNetworkAssembly(string assemblyName)
        {
            return GetAllNetworkAssemblyNames().Contains(assemblyName);
        }

        static HashSet<string> GetAllNetworkAssemblyPaths()
        {
            HashSet<string> assemblies = new();
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
                    assemblies.Add(path);
                }
                catch { }
            }
            return assemblies;
        }
        static HashSet<string> GetAllNetworkAssemblyNames()
        {
            HashSet<string> assemblies = new();
            foreach (var assemblyPath in GetAllNetworkAssemblyPaths())
            {
                string assemblyName = Path.GetFileNameWithoutExtension(assemblyPath);
                assemblies.Add(assemblyName);
            }
            return assemblies;
        }


        #region 编辑器首次启动

        private static string EditorFirstStartLockfilePath = "Temp/EditorFirstStartLockfile";

        /// <summary>
        /// 编辑器首次启动判断
        /// </summary>
        private static bool IsEditorFirstStarted
        {
            get
            {
                if (File.Exists(EditorFirstStartLockfilePath))
                    return false;
                //为了可以多次访问该属性，延迟加锁
                EditorApplication.delayCall += LockEditorFirstStartLockfile;
                return true;
            }
        }

        /// <summary>
        /// 锁定启动锁
        /// </summary>
        private static void LockEditorFirstStartLockfile()
        {
            try
            {
                if (!File.Exists(EditorFirstStartLockfilePath))
                {
                    File.WriteAllBytes(EditorFirstStartLockfilePath, new byte[0]);
                }
            }
            catch { }
        }

        /// <summary>
        /// 解锁启动锁
        /// </summary>
        private static void UnlockEditorFirstStartLockfile()
        {
            try
            {
                if (File.Exists(EditorFirstStartLockfilePath))
                {
                    File.Delete(EditorFirstStartLockfilePath);
                }
            }
            catch { }
        }

        [InitializeOnLoadMethod]
        static void EditorFirstStart()
        {
            EditorApplication.quitting += UnlockEditorFirstStartLockfile;
        }

        #endregion


        #region 编辑器工程唯一ID，编辑器目录硬连接，支持编辑器多开

        private static string EditorProjectGuidPath = "Library/EditorProjectGuid";
        private static Guid? editorProjectGuid;

        public static Guid EditorProjectGuid
        {
            get
            {
                if (!editorProjectGuid.HasValue)
                {
                    try
                    {
                        if (File.Exists(EditorProjectGuidPath))
                        {
                            string line = File.ReadLines(EditorProjectGuidPath, Encoding.UTF8).FirstOrDefault();
                            if (!string.IsNullOrEmpty(line))
                            {
                                if (Guid.TryParse(line, out var guid))
                                {
                                    editorProjectGuid = guid;
                                }
                            }
                        }
                    }
                    catch { }

                    if (!editorProjectGuid.HasValue)
                    {
                        editorProjectGuid = Guid.NewGuid();
                        File.WriteAllText(EditorProjectGuidPath, editorProjectGuid.Value.ToString(), Encoding.UTF8);
                    }
                }

                return editorProjectGuid.Value;
            }
        }

        #endregion



        public static bool IsExcludeAssembly(string assemblyName)
        {
            if (excludeAssemblies == null)
            {
                excludeAssemblies = new HashSet<string>
                {
                    typeof(RpcAttribute).Assembly.GetName().Name,
                    typeof(NetworkAssemblyPostprocessor).Assembly.GetName().Name
                };
            }

            return excludeAssemblies.Contains(assemblyName);
        }

        public static void ProcessAllAssembly()
        {
            ProcessAssembly(GetAllNetworkAssemblyPaths());
        }

        public static void ProcessAssembly(IEnumerable<string> assemblyPaths)
        {

            //Debug.Log($"Inject NetSync assembly path: '{path}', Editor: {Application.isEditor}");

            Stopwatch sw = new Stopwatch();
            sw.Start();


            using (new LockReloadAssemblies())
            {
                DefaultAssemblyResolver assemblyResolver = new DefaultAssemblyResolver();
                List<string> searchPaths = new List<string>();

                assemblyPaths = assemblyPaths.Select(o => Path.GetFullPath(o));

                foreach (var assemblyPath in assemblyPaths)
                {
                    var dir = Path.GetDirectoryName(assemblyPath);
                    if (!searchPaths.Contains(dir))
                    {
                        assemblyResolver.AddSearchDirectory(dir);
                        searchPaths.Add(dir);
                        //Debug.Log($"AddSearchDirectory: {dir}");
                    } 
                }

                foreach (var ass in AppDomain.CurrentDomain.GetAssemblies())
                {
                    try
                    {
                        string dir = Path.GetDirectoryName(ass.Location);
                        if (!searchPaths.Contains(dir))
                        {
                            assemblyResolver.AddSearchDirectory(dir);
                            searchPaths.Add(dir);
                        }
                    }
                    catch
                    {
                    }
                } 

                var allAssembly = GetAllNetworkAssemblyNames();
                List<string> processedAssemblies = new();

                foreach (var assemblyPath in assemblyPaths)
                {
                    string assemblyName = Path.GetFileNameWithoutExtension(assemblyPath);

                    if (IsExcludeAssembly(assemblyName))
                        continue;

                    if (!allAssembly.Contains(assemblyName))
                        continue;

                    bool readSymbols = false, mdb = false;

                    if (File.Exists(Path.ChangeExtension(assemblyPath, "pdb")))
                    {
                        readSymbols = true;
                        mdb = false;
                    }
                    else if (File.Exists(Path.ChangeExtension(assemblyPath, "mdb")))
                    {
                        readSymbols = true;
                        mdb = true;
                    }
                    readSymbols = false;

                    ReaderParameters readerParameters = new ReaderParameters();
                    readerParameters.ReadWrite = true;
                    readerParameters.ReadingMode = ReadingMode.Immediate;
                    //readerParameters.ThrowIfSymbolsAreNotMatching = true;
                    readerParameters.ApplyWindowsRuntimeProjections = false;
                    readerParameters.AssemblyResolver = assemblyResolver;
                    readerParameters.ReadSymbols = readSymbols;
                    if (readerParameters.ReadSymbols)
                    {
                        if (mdb)
                        {
                            readerParameters.SymbolReaderProvider = new MdbReaderProvider();
                        }
                        else
                        {
                            readerParameters.SymbolReaderProvider = new PdbReaderProvider();
                        }
                    }

                    AssemblyDefinition assemblyDefinition = null;
                    try
                    {
                        try
                        {
                            assemblyDefinition = AssemblyDefinition.ReadAssembly(assemblyPath, readerParameters);
                        }
                        catch (Exception ex)
                        {
                            if (assemblyDefinition != null)
                            {
                                assemblyDefinition.Dispose();
                                assemblyDefinition = null;
                            }
                            if (readerParameters.ReadSymbols)
                            {
                                readerParameters.ReadSymbols = false;
                                //readerParameters.SymbolReaderProvider = null;

                                assemblyDefinition = AssemblyDefinition.ReadAssembly(assemblyPath, readerParameters);
                            }
                            else
                            {
                                Debug.Log($"Process assembly error '{assemblyPath}'");
                                throw ex;
                            }
                        }

                        if (ProcessAssembly(assemblyDefinition))
                        {
                            try
                            {
                                WriterParameters writerParameters = new WriterParameters();

                                if (readSymbols)
                                {
                                    writerParameters.WriteSymbols = true;
                                    if (mdb)
                                    {
                                        writerParameters.SymbolWriterProvider = new MdbWriterProvider();
                                    }
                                    else
                                    {
                                        writerParameters.SymbolWriterProvider = new PdbWriterProvider();
                                    }
                                }

                                //Debug.Log($"Writing assembly, Symbols: '{(writerParameters.WriteSymbols ? writerParameters.SymbolWriterProvider?.GetType().Name : "false")}'\n{assemblyPath}");

                                //报错: IOException: Sharing violation on path 
                                //assemblyDefinition.Write(assemblyPath, writerParameters);

                                assemblyDefinition.Write();

                                //Debug.Log($"Inject assembly  success\n{assemblyPath}");
                            }
                            catch
                            {
                                Debug.LogError($"Write assembly failed\n{assemblyPath}");
                                throw;
                            }
                            processedAssemblies.Add(assemblyPath);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogException(ex);
                        Debug.LogError($"Inject assembly error '{assemblyPath}'");
                        throw ex;
                    }
                    finally
                    {
                        if (assemblyDefinition != null)
                        {
                            assemblyDefinition.Dispose();
                            assemblyDefinition = null;
                        }
                    }
                }

                sw.Stop();
                if (processedAssemblies.Count > 0)
                {
                    //Debug.Log($"Process Network assembly complete , total assembly: {processedAssemblies.Count}. ({sw.Elapsed.TotalSeconds:0.##}s)\n{string.Join("\n", processedAssemblies)}");
                }

            }
        }


        static void InitalizeAssembly(AssemblyDefinition assembly)
        {
            if (BeginServerRpcMethodRef != null)
                return;

            var mainModule = assembly.MainModule;

            BeginServerRpcMethodRef = mainModule.ImportReference(BeginServerRpcMethod);
            EndServerRpcMethodRef = mainModule.ImportReference(EndServerRpcMethod);
            ReturnServerRpcMethodRef = mainModule.ImportReference(ReturnServerRpcMethod);
            BeginClientRpcMethodRef = mainModule.ImportReference(BeginClientRpcMethod);
            EndClientRpcMethodRef = mainModule.ImportReference(EndClientRpcMethod);
            ReturnClientRpcMethodRef = mainModule.ImportReference(ReturnClientRpcMethod);

        }

        static bool ProcessAssembly(AssemblyDefinition assembly)
        {
            if (IsAssemblyPostprocessed(assembly, typeof(NetworkAssemblyPostprocessor)))
            {
                return false;
            }
            var mainModule = assembly.MainModule;
            ServerRpcAttrRef = assembly.MainModule.ImportReference(typeof(ServerRpcAttribute));
            ServerRpcParamsRef = mainModule.ImportReference(typeof(ServerRpcParams));
            ClientRpcAttrRef = assembly.MainModule.ImportReference(typeof(ClientRpcAttribute));
            ClientRpcParamsRef = mainModule.ImportReference(typeof(ClientRpcParams));
            NetworkObjectRef = mainModule.ImportReference(typeof(NetworkObject));

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

            BeginServerRpcMethodRef = null;

            bool changed = false;

            //assembly.MainModule.Types 不包含嵌套类型
            foreach (var type in assembly.MainModule.GetAllTypes())
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
                    if (ProcessServerRpcMethod(assembly, method, builder))
                    {
                        changed = true;
                    }
                    else if (ProcessClientRpcMethod(assembly, method, builder))
                    {
                        changed = true;
                    }

                }
            }

            if (changed)
            {
                SetAssemblyPostprocessed(assembly, typeof(NetworkAssemblyPostprocessor));
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

            InitalizeAssembly(assembly);

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

            InitalizeAssembly(assembly);

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


        const string AssemblyPostprocessedMetadataKey = "AssemblyPostprocessed";
        static bool IsAssemblyPostprocessed(AssemblyDefinition assembly, Type assemblyPostprocessorType)
        {
            foreach (var item in GetAssemblyMetadatas(assembly))
            {
                if (item.key == AssemblyPostprocessedMetadataKey && item.value == assemblyPostprocessorType.FullName)
                {
                    return true;
                }
            }
            return false;
        }

        static void SetAssemblyPostprocessed(AssemblyDefinition assembly, Type assemblyPostprocessorType)
        {
            AddAssemblyMetadata(assembly, AssemblyPostprocessedMetadataKey, assemblyPostprocessorType.FullName);
        }

        static IEnumerable<(string key, string value)> GetAssemblyMetadatas(AssemblyDefinition assembly)
        {
            TypeReference assemblyMetadataAttrRef = assembly.MainModule.ImportReference(typeof(AssemblyMetadataAttribute));
            foreach (var attr in assembly.CustomAttributes)
            {
                //Debug.Log($"{assembly.Name}: {attr.AttributeType.Name}");
                if (attr.AttributeType.FullName == assemblyMetadataAttrRef.FullName)
                {
                    string key = null, value = null;
                    key = attr.ConstructorArguments[0].Value as string;
                    value = attr.ConstructorArguments[1].Value as string;
                    yield return (key, value);
                }
            }
        }

        static void AddAssemblyMetadata(AssemblyDefinition assembly, string key, string value)
        {
            var attributeConstructor = assembly.MainModule.ImportReference(typeof(AssemblyMetadataAttribute)
                .GetConstructor(new Type[] { typeof(string), typeof(string) }));
            var attr = new CustomAttribute(attributeConstructor);
            attr.ConstructorArguments.Add(new CustomAttributeArgument(assembly.MainModule.TypeSystem.String, key));
            attr.ConstructorArguments.Add(new CustomAttributeArgument(assembly.MainModule.TypeSystem.String, value));
            assembly.CustomAttributes.Add(attr);
        }


#if UNITY_2019_1_OR_NEWER
        public void OnPostBuildPlayerScriptDLLs(BuildReport report)
        {
            if (!IsEnabled)
                return;
            var items = report.files.Where(file => file.path.EndsWith($"{typeof(RpcAttribute).Assembly.GetName().Name}.dll")).ToArray();

            if (items.Length > 0)
            {
                var dir = Path.GetDirectoryName(items[0].path);
                List<string> paths = new();
                foreach (var file in Directory.GetFiles(dir, "*.dll"))
                {
                    paths.Add(file);
                }

                ProcessAssembly(paths);
            }
        }
#else

        [PostProcessScene]
        static void PostProcessScene()
        {
            if (!IsEnabled)
                return;
            string editorAssemblyDirectory = "Library/ScriptAssemblies";
            if (!Directory.Exists(editorAssemblyDirectory))
                return;
            List<string> files = new List<string>();
            foreach (var file in Directory.GetFiles(editorAssemblyDirectory, "*.dll"))
            {
                files.Add(file);
            }
            ProcessAssembly(files.ToArray());
        }

#endif

        class LockReloadAssemblies : IDisposable
        {
            public LockReloadAssemblies()
            {
                EditorApplication.LockReloadAssemblies();
            }

            public void Dispose()
            {
                EditorApplication.UnlockReloadAssemblies();
            }
        }

    }
    internal static class Extensions
    {
        public static bool IsSubclassOf(this TypeDefinition childType, TypeReference baseType)
        {
            if (childType.IsValueType != baseType.IsValueType)
                return false;
            TypeReference parentType = childType.BaseType;
            while (parentType != null)
            {
                if (parentType.FullName == baseType.FullName)
                    return true;
                if (parentType.IsDefinition)
                {
                    TypeDefinition def = parentType as TypeDefinition;
                    parentType = def.BaseType;
                }
                else
                {
                    parentType = null;
                }
            }
            return false;
        }
    }
}