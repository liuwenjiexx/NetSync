using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Yanmonet.NetSync.Editor.CodeGen
{
    class AssemblyPostBuild
#if UNITY_2019_1_OR_NEWER
      : IPostBuildPlayerScriptDLLs
#endif
    {
        public int callbackOrder => 0;
#if UNITY_2019_1_OR_NEWER
        public void OnPostBuildPlayerScriptDLLs(BuildReport report)
        {

            var items = report.files.Where(file => file.path.EndsWith("Assembly-CSharp.dll")).ToArray();
            if (items.Length > 0)
            {
                var dir = Path.GetDirectoryName(items[0].path);
                List<string> paths = new();
                foreach (var assembly in NetworkUtility.ReferencedAssemblies(typeof(RpcAttribute).Assembly))
                {
                    string assemblyName = assembly.GetName().Name;

                    string file = Path.Combine(dir, assemblyName + ".dll");

                    if (File.Exists(file))
                    {
                        paths.Add(file);
                    }
                }

                EditorILUtililty.InjectAssembly(paths.ToArray());
            }
        }
#else
        [PostProcessScene]
        static void PostProcessScene()
        {
            if (EditorAssemblyInjectionUtility.IsEnabled())
            {
                Debug.Log("Inject all assembly [PostProcessScene]");
                EditorAssemblyInjectionUtility.InjectAssembly(EditorAssemblyInjectionUtility.EditorAssemblyDirectory, false, EditorAssemblyInjectionUtility.IsDebug);
            }
        }
#endif

    }
}