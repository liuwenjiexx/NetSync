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
using static UnityEngine.ParticleSystem;
using static UnityEngine.GraphicsBuffer;

namespace Yanmonet.NetSync.Editor.CodeGen
{
    [InitializeOnLoad]
    public static class EditorILUtililty
    {
        private static List<string> assemblyPaths;

        static HashSet<string> excludeAssemblies = new HashSet<string>() {
            "Yanmonet.NetSync",
            "Yanmonet.NetSync.Editor",
            "Yanmonet.NetSync.Editor.CodeGen"};

        [InitializeOnLoadMethod]
        static void InitializeOnLoadMethod()
        {

            CompilationPipeline.compilationStarted += CompilationPipeline_compilationStarted;
            CompilationPipeline.assemblyCompilationFinished += CompilationPipeline_assemblyCompilationFinished;
            CompilationPipeline.compilationFinished += CompilationPipeline_compilationFinished;

        }

        static EditorILUtililty()
        {
            AssemblyReloadEvents.beforeAssemblyReload += AssemblyReloadEvents_beforeAssemblyReload;
        }

        private static void AssemblyReloadEvents_beforeAssemblyReload()
        {

            List<string> paths = new();
            foreach (var assembly in NetworkUtility.ReferencedAssemblies(typeof(RpcAttribute).Assembly))
            {
                string assemblyName = assembly.GetName().Name;

                string file = assembly.Location; //Path.Combine(dir, assemblyName + ".dll");

                if (File.Exists(file))
                {
                    paths.Add(file);
                }
            }

            InjectAssembly(paths.ToArray());
        }

        private static void CompilationPipeline_compilationStarted(object obj)
        {
            if (assemblyPaths == null)
                assemblyPaths = new();
            assemblyPaths.Clear();
        }

        private static void CompilationPipeline_assemblyCompilationFinished(string arg1, CompilerMessage[] arg2)
        {
            assemblyPaths.Add(arg1);
        }

        private static void CompilationPipeline_compilationFinished(object obj)
        {
            //obj: Editor Compilation
            //EditorAssemblyInjectionUtility.Log("Compilation Finished:\n" + string.Join("\n", assemblyPaths));
            //    if (!Application.isPlaying)
            {
                //   if (EditorAssemblyInjectionUtility.IsEnabled())
                {

                    InjectAssembly(assemblyPaths.ToArray());

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
            foreach (var assembly in NetworkUtility.ReferencedAssemblies(typeof(RpcAttribute).Assembly))
            {
                if (assembly.GetName().Name == assemblyName)
                    return true;
            }
            return false;
        }

        public static bool IsExcludeAssembly(string assemblyName)
        {
            return excludeAssemblies.Contains(assemblyName);
        }

        public static void InjectAssembly(string[] assemblyPaths)
        {

            //Debug.Log($"Inject NetSync assembly path: '{path}', Editor: {Application.isEditor}");

            Stopwatch sw = new Stopwatch();
            sw.Start();


            using (new LockReloadAssemblies())
            {
                DefaultAssemblyResolver assemblyResolver = new DefaultAssemblyResolver();
                HashSet<string> searchPaths = new HashSet<string>();
                for (int i = 0; i < assemblyPaths.Length; i++)
                {
                    assemblyPaths[i] = Path.GetFullPath(assemblyPaths[i]);
                }
                foreach (var path in assemblyPaths)
                {
                    var dir = Path.GetDirectoryName(path);
                    if (!searchPaths.Contains(dir))
                    {
                        assemblyResolver.AddSearchDirectory(dir);
                        searchPaths.Add(dir);
                        //Debug.Log($"AddSearchDirectory: {dir}");
                    }
                }

                foreach (var assemblyPath in assemblyPaths)
                {
                    string assemblyName = Path.GetFileNameWithoutExtension(assemblyPath);

                    if (IsExcludeAssembly(assemblyName))
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
                                //assemblyDefinition.Write(assemblyPath, writerParameters,);

                                assemblyDefinition.Write();

                                //Debug.Log($"Inject assembly  success\n{assemblyPath}");
                            }
                            catch
                            {
                                Debug.LogError($"Write assembly failed\n{assemblyPath}");
                                throw;
                            }
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
                //Debug.Log($"Inject assembly complete , total assembly: {changedAssemblies.Count}. ({sw.Elapsed.TotalSeconds:0.##}s)\n{string.Join("\n", changedAssemblies)}");

            }
        }

        static MethodInfo BeginServerRpcMethod;
        static MethodInfo EndServerRpcMethod;
        static MethodInfo ReturnServerRpcMethod;
        static MethodInfo BeginClientRpcMethod;
        static MethodInfo EndClientRpcMethod;
        static MethodInfo ReturnClientRpcMethod;
        static Type NetworkObjectType = typeof(NetworkObject);
        static Type ServerRpcParamsType = typeof(ServerRpcParams);
        static Type ClientRpcParamsType = typeof(ClientRpcParams);

        static MethodReference BeginServerRpcMethodRef;
        static MethodReference EndServerRpcMethodRef;
        static MethodReference ReturnServerRpcMethodRef;
        static MethodReference BeginClientRpcMethodRef;
        static MethodReference EndClientRpcMethodRef;
        static MethodReference ReturnClientRpcMethodRef;
        static TypeReference ServerRpcParamsRef;
        static TypeReference ClientRpcParamsRef;
        static MethodReference NewServerRpcParamsRef;
        static MethodReference NewClientRpcParamsRef;

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
            ServerRpcParamsRef = mainModule.ImportReference(typeof(ServerRpcParams));
            ClientRpcParamsRef = mainModule.ImportReference(typeof(ClientRpcParams));

            //NewServerRpcParamsRef = mainModule.ImportReference(typeof(ServerRpcParams).GetConstructors()[0]);
            //NewClientRpcParamsRef = mainModule.ImportReference(typeof(ClientRpcParams).GetConstructors()[0]);


        }

        static bool ProcessAssembly(AssemblyDefinition assembly)
        {
            if (IsAssemblyPostprocessed(assembly))
            {
                return false;
            }
            var mainModule = assembly.MainModule;
            var serverRpcAttrRef = assembly.MainModule.ImportReference(typeof(ServerRpcAttribute));
            var clientRpcAttrRef = assembly.MainModule.ImportReference(typeof(ClientRpcAttribute));
            var serverRpcParamsRef = assembly.MainModule.ImportReference(typeof(ServerRpcParams));
            var clientRpcParamsRef = assembly.MainModule.ImportReference(typeof(ClientRpcParams));

            if (BeginServerRpcMethod == null)
            {
                BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
                BeginServerRpcMethod = NetworkObjectType.GetMethod("BeginServerRpc", new Type[] { typeof(string), typeof(ServerRpcParams), typeof(object[]) });

                EndServerRpcMethod = NetworkObjectType.GetMethod("EndServerRpc", bindingFlags);
                ReturnServerRpcMethod = NetworkObjectType.GetMethod("ReturnServerRpc", bindingFlags);
                //BeginClientRpcMethod = NetworkObjectType.GetMethod("BeginClientRpc", bindingFlags);  
                EndClientRpcMethod = NetworkObjectType.GetMethod("EndClientRpc", bindingFlags);
                ReturnClientRpcMethod = NetworkObjectType.GetMethod("ReturnClientRpc", bindingFlags);
                foreach (var m in NetworkObjectType.GetMethods(bindingFlags))
                {
                    switch (m.Name)
                    {
                        case "BeginServerRpc":
                            if (m.GetParameters().Length == 3)
                                BeginServerRpcMethod = m;
                            break;
                        case "BeginClientRpc":
                            if (m.GetParameters().Length == 3)
                                BeginClientRpcMethod = m;
                            break;
                    }
                }
            }

            BeginServerRpcMethodRef = null;

            CustomAttribute serverRpcAttr = null;
            CustomAttribute clientRpcAttr = null;
            bool changed = false;

            //assembly.MainModule.Types 不包含嵌套类型
            foreach (var type in assembly.MainModule.GetAllTypes())
            {
                if (!type.IsClass)
                    continue;
                bool isNetObjType = false;
                var bt = type.BaseType;

                if (type.BaseType?.FullName != typeof(NetworkObject).FullName)
                    continue;

                foreach (var method in type.GetMethods())
                {
                    switch (method.Name)
                    {
                        case "BeginServerRpc":
                            if (method.Parameters.Count == 3)
                            {
                                BeginServerRpcMethodRef = method;
                            }
                            break;
                    }
                }

                foreach (var method in type.GetMethods())
                {
                    var il = method.Body.GetILProcessor();
                    serverRpcAttr = null;
                    clientRpcAttr = null;
                    if (method.HasCustomAttributes)
                    {
                        foreach (var attr in method.CustomAttributes)
                        {
                            if (attr.AttributeType.FullName == serverRpcAttrRef.FullName)
                            {
                                serverRpcAttr = attr;
                            }
                            else if (attr.AttributeType.FullName == clientRpcAttrRef.FullName)
                            {
                                clientRpcAttr = attr;
                            }
                        }
                    }

                    if (serverRpcAttr != null && clientRpcAttr != null)
                        throw new Exception($"{nameof(ServerRpcAttribute)} and {nameof(ClientRpcAttribute)} only use one");

                    ILBuilder builder = null;
                    if (serverRpcAttr != null)
                    {
                        InitalizeAssembly(assembly);

                        if (builder == null)
                        {
                            builder = new ILBuilder(il, method);
                        }

                        builder.InsertPoint = builder.FirstOrCreate();
                        builder.Nop();

                        int argCount = 0;
                        var ps = method.Parameters;
                        int serverRpcParamsIndex = -1;
                        for (int i = 0; i < ps.Count; i++)
                        {
                            var p = ps[i];
                            if (p.ParameterType.FullName == serverRpcParamsRef.FullName)
                            {
                                serverRpcParamsIndex = i;
                                continue;
                            }
                            argCount++;
                        }
                        var argsVar = builder.NewArrayVariable(mainModule.TypeSystem.Object, argCount);
                        for (int i = 0, j = 0; i < ps.Count; i++)
                        {
                            var p = ps[i];
                            if (p.ParameterType.FullName == serverRpcParamsRef.FullName)
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

                        var AddRef = typeof(NetworkObject).GetMethod("Add", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                        //  BeginServerRpcMethodRef= typeof(NetworkObject).GetMethod("Add", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                        //builder.LoadThis()
                        //    .Call(AddRef);

                        if (serverRpcParamsIndex == -1)
                        {
                            var rpcParamsVar = builder.NewVariable(ServerRpcParamsRef);
                            //builder.Load(rpcParamsVar);
                            builder.Emit(OpCodes.Ldloca_S, rpcParamsVar.Index);
                            builder.Emit(OpCodes.Initobj, ServerRpcParamsRef);

                            builder.LoadThis()
                                .Load(method.Name)
                                //.Emit(OpCodes.Ldloca_S, ServerRpcParamsRef)
                                .Load(rpcParamsVar)
                                .Load(argsVar)
                                .Emit(OpCodes.Call, BeginServerRpcMethodRef);
                            //.Call(BeginServerRpcMethod);
                        }
                        else
                        {
                            builder.LoadThis()
                                .Load(method.Name)
                                .Emit(OpCodes.Ldarg, serverRpcParamsIndex + 1)
                                .Load(argsVar)
                              .Emit(OpCodes.Call, BeginServerRpcMethodRef);
                            //.Call(BeginServerRpcMethod);
                        }
                        builder.Nop();

                        /*
                       builder.LoadThis()
                           .Call(EndServerRpcMethodRef);
                       builder.Nop();

                       var isReturnVar = builder.NewVariable(mainModule.TypeSystem.Boolean);
                       builder.LoadThis()
                           .Call(ReturnServerRpcMethodRef);
                       builder.Set(isReturnVar);

                       builder.BeginBlock();
                       builder.Load(isReturnVar);
                       builder.IfTrueBreakBlock();
                       var returnPoint = builder.Body.Instructions.First(o => o.OpCode == OpCodes.Ret);
                       builder.Emit(OpCodes.Br_S, returnPoint);
                       builder.EndBlock();
                       */
                        changed = true;
                    }
                }
            }

            if (changed)
            {
                SetAssemblyPostprocessed(assembly);
            }

            return changed;
        }

        const string AssemblyPostprocessedMetadataKey = "AssemblyPostprocessed";
        static bool IsAssemblyPostprocessed(AssemblyDefinition assembly)
        {
            foreach (var item in GetAssemblyMetadatas(assembly))
            {
                if (item.key == AssemblyPostprocessedMetadataKey && item.value == typeof(RpcAttribute).Assembly.GetName().Name)
                {
                    return true;
                }
            }
            return false;
        }

        static void SetAssemblyPostprocessed(AssemblyDefinition assembly)
        {
            AddAssemblyMetadata(assembly, AssemblyPostprocessedMetadataKey, typeof(RpcAttribute).Assembly.GetName().Name);
        }

        static IEnumerable<(string key, string value)> GetAssemblyMetadatas(AssemblyDefinition assembly)
        {
            TypeReference assemblyMetadataAttrRef = assembly.MainModule.ImportReference(typeof(AssemblyMetadataAttribute));
            foreach (var attr in assembly.CustomAttributes)
            {
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
}