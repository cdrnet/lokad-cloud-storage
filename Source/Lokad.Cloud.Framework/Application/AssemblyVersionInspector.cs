#region Copyright (c) Lokad 2009-2010
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Reflection;

namespace Lokad.Cloud.Application
{

    /// <summary>
    /// Allows to inspect assemblies in a separate AppDomain.
    /// </summary>
    /// <remarks>
    /// Use a <c>using</c> block so that <see cref="M:Dispose"/> is called.
    /// </remarks>
    internal class AssemblyVersionInspector : IDisposable
    {
        bool _disposed;
        readonly AppDomain _sandbox;
        readonly Wrapper _wrapper;

        /// <summary>
        /// Initializes a new instance of the <see cref="T:AssemblyVersionInspector"/> class.
        /// </summary>
        /// <param name="assemblyBytes">The assembly bytes.</param>
        /// <param name="symbolBytes">The symbol store bytes if available, else null.</param>
        public AssemblyVersionInspector(byte[] assemblyBytes, byte[] symbolBytes)
        {
            _sandbox = AppDomain.CreateDomain("AsmInspector", null, AppDomain.CurrentDomain.SetupInformation);
            _wrapper = _sandbox.CreateInstanceAndUnwrap(
                Assembly.GetExecutingAssembly().FullName,
                (typeof (Wrapper)).FullName,
                false,
                BindingFlags.CreateInstance,
                null,
                new object[] {assemblyBytes, symbolBytes},
                null,
                new object[0]) as Wrapper;
        }

        /// <summary>Gets the assembly version.</summary>
        public Version AssemblyVersion
        {
            get
            {
                if(_disposed)
                {
                    throw new ObjectDisposedException("AssemblyVersionInspector");
                }

                return _wrapper.Version;
            }
        }

        /// <summary>Disposes of the object and the wrapped <see cref="AppDomain"/>.</summary>
        public void Dispose()
        {
            if(!_disposed)
            {
                AppDomain.Unload(_sandbox);
                _disposed = true;
            }
        }

        /// <summary>
        /// Wraps an assembly (to be used from within a secondary AppDomain).
        /// </summary>
        public class Wrapper : MarshalByRefObject
        {
            readonly Assembly _wrappedAssembly;

            /// <summary>
            /// Initializes a new instance of the <see cref="T:Wrapper"/> class.
            /// </summary>
            /// <param name="assemblyBytes">The assembly bytes.</param>
            public Wrapper(byte[] assemblyBytes, byte[] symbolBytes)
            {
                _wrappedAssembly = symbolBytes == null
                    ? Assembly.Load(assemblyBytes)
                    : Assembly.Load(assemblyBytes, symbolBytes);
            }

            /// <summary>Gets the assembly version.</summary>
            public Version Version
            {
                get { return _wrappedAssembly.GetName().Version; }
            }
        }
    }
}
