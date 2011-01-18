#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Reflection;

namespace Lokad.Cloud.Application
{
    /// <summary>
    /// Utility to inspect assemblies in an isolated AppDomain.
    /// </summary>
    internal static class AssemblyVersionInspector
    {
        internal static AssemblyVersionInspectionResult Inspect(byte[] assemblyBytes)
        {
            var sandbox = AppDomain.CreateDomain("AssemblyInspector", null, AppDomain.CurrentDomain.SetupInformation);
            try
            {
                var wrapper = (Wrapper)sandbox.CreateInstanceAndUnwrap(
                    Assembly.GetExecutingAssembly().FullName,
                    (typeof(Wrapper)).FullName,
                    false,
                    BindingFlags.CreateInstance,
                    null,
                    new object[] { assemblyBytes },
                    null,
                    new object[0]);

                return wrapper.Result;
            }
            finally
            {
                AppDomain.Unload(sandbox);
            }
        }

        [Serializable]
        internal class AssemblyVersionInspectionResult
        {
            public Version Version { get; set; }
        }

        /// <summary>
        /// Wraps an assembly (to be used from within a secondary AppDomain).
        /// </summary>
        private class Wrapper : MarshalByRefObject
        {
            internal AssemblyVersionInspectionResult Result { get; private set; }

            /// <summary>
            /// Initializes a new instance of the <see cref="Wrapper"/> class.
            /// </summary>
            /// <param name="assemblyBytes">The assembly bytes.</param>
            public Wrapper(byte[] assemblyBytes)
            {
                Result = new AssemblyVersionInspectionResult
                    {
                        Version = Assembly.ReflectionOnlyLoad(assemblyBytes).GetName().Version
                    };
            }
        }
    }
}
