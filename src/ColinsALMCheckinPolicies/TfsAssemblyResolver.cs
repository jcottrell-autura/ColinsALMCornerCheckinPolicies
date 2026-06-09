using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace ColinsALMCheckinPolicies
{
    /// <summary>
    /// Lets one VSIX load in multiple Visual Studio versions whose Team Foundation client
    /// assemblies carry different assembly versions (16.x in VS 2022, 20.x in VS 2026).
    /// The policy is compiled against whatever IDE builds it; at runtime this redirects any
    /// Microsoft.TeamFoundation.* request to the copy the host has already loaded.
    /// </summary>
    internal static class TfsAssemblyResolver
    {
        [ModuleInitializer]   // runs when Team Explorer loads this DLL, before any policy type is resolved
        internal static void Init() => AppDomain.CurrentDomain.AssemblyResolve += Resolve;

        private static Assembly Resolve(object sender, ResolveEventArgs args)
        {
            var name = new AssemblyName(args.Name).Name;
            if (!name.StartsWith("Microsoft.TeamFoundation", StringComparison.OrdinalIgnoreCase))
                return null;

            // 1) Reuse whatever version devenv already loaded (16.x in VS 2022, 20.x in VS 2026).
            var loaded = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => string.Equals(a.GetName().Name, name, StringComparison.OrdinalIgnoreCase));
            if (loaded != null) return loaded;

            // 2) Fall back to the copy next to an already-loaded TF assembly (the Team Explorer folder).
            var anchor = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a =>
                !a.IsDynamic &&
                a.GetName().Name.StartsWith("Microsoft.TeamFoundation", StringComparison.OrdinalIgnoreCase) &&
                !string.IsNullOrEmpty(a.Location));
            if (anchor != null)
            {
                var candidate = Path.Combine(Path.GetDirectoryName(anchor.Location), name + ".dll");
                if (File.Exists(candidate)) return Assembly.LoadFrom(candidate);
            }

            return null;
        }
    }
}

namespace System.Runtime.CompilerServices
{
    // .NET Framework 4.8 has no ModuleInitializerAttribute; this internal polyfill lets the
    // C# 9 [ModuleInitializer] feature compile. Safe because no referenced assembly defines it.
    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    internal sealed class ModuleInitializerAttribute : Attribute { }
}
