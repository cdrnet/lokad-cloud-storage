#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Collections.Generic;
using System.Reflection;

namespace Lokad.Cloud.Application
{
    /// <summary>Resolves assemblies by caching assemblies that were loaded.</summary>
    public sealed class AssemblyResolver
    {
        /// <summary>
        /// Holds the loaded assemblies.
        /// </summary>
        private readonly Dictionary<string, Assembly> _assemblyCache;

        /// <summary> 
        /// Initializes an instance of the <see cref="AssemblyResolver" />  class.
        /// </summary>
        public AssemblyResolver()
        {
            _assemblyCache = new Dictionary<string, Assembly>();
        }

        /// <summary> 
        /// Installs the assembly resolver by hooking up to the 
        /// <see cref="AppDomain.AssemblyResolve" /> event.
        /// </summary>
        public void Attach()
        {
            AppDomain.CurrentDomain.AssemblyResolve += AssemblyResolve;
            AppDomain.CurrentDomain.AssemblyLoad += AssemblyLoad;
        }

        /// <summary> 
        /// Uninstalls the assembly resolver.
        /// </summary>
        public void Detach()
        {
            AppDomain.CurrentDomain.AssemblyResolve -= AssemblyResolve;
            AppDomain.CurrentDomain.AssemblyLoad -= AssemblyLoad;

            _assemblyCache.Clear();
        }

        /// <summary> 
        /// Resolves an assembly not found by the system using the assembly cache.
        /// </summary>
        private Assembly AssemblyResolve(object sender, ResolveEventArgs args)
        {
            var isFullName = args.Name.IndexOf("Version=") != -1;

            // extract the simple name out of a qualified assembly name
            var nameOf = new Func<string, string>(qn => qn.Substring(0, qn.IndexOf(",")));

            // first try to find an already loaded assembly
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblies)
            {
                if (isFullName)
                {
                    if (assembly.FullName == args.Name ||
                        nameOf(assembly.FullName) == nameOf(args.Name))
                    {
                        // return assembly from AppDomain
                        return assembly;
                    }
                }
                else if (assembly.GetName(false).Name == args.Name)
                {
                    // return assembly from AppDomain
                    return assembly;
                }
            }

            // TODO: missing optimistic assembly resolution when it comes from the cache.

            // find assembly in cache
            if (isFullName)
            {
                if (_assemblyCache.ContainsKey(args.Name))
                {
                    // return assembly from cache
                    return _assemblyCache[args.Name];
                }
            }
            else
            {
                foreach (var assembly in _assemblyCache.Values)
                {
                    if (assembly.GetName(false).Name == args.Name)
                    {
                        // return assembly from cache
                        return assembly;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Occurs when an assembly is loaded. The loaded assembly is added 
        /// to the assembly cache.
        /// </summary>
        private void AssemblyLoad(object sender, AssemblyLoadEventArgs args)
        {
            // store assembly in cache
            _assemblyCache[args.LoadedAssembly.FullName] = args.LoadedAssembly;
        }
    }
}