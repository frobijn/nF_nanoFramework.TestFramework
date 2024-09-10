// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace nanoFramework.TestFramework.Tooling
{
    /// <summary>
    /// Helper to load the .NET nanoFramework assemblies that contain the unit tests.
    /// <para>
    /// Actually it can analyse every type of assembly, whether or not the assembly is targeted for the nanoFramework CLR or not.
    /// The assemblies are loaded in the current <see cref="AppDomain"/> and are not unloaded. It is assumed
    /// that this functionality is used in short-lived processes.
    /// </para>
    /// </summary>
    public static class AssemblyLoader
    {
        #region Fields
        private static readonly HashSet<string> s_assemblyLocations = new HashSet<string>();
        #endregion

        #region Methods
        /// <summary>
        /// Load a .NET nanoFramework assembly
        /// </summary>
        /// <param name="assemblyFilePath">Path to the assembly file to load</param>
        /// <returns></returns>
        public static Assembly LoadFile(string assemblyFilePath)
        {
            lock (s_assemblyLocations)
            {
                if (s_assemblyLocations.Count == 0)
                {
                    AppDomain.CurrentDomain.AssemblyResolve += AssemblyResolve;
                }
                s_assemblyLocations.Add(Path.GetDirectoryName(assemblyFilePath));
            }

            // developer note: we have to use LoadFile() and not Load() which loads the assembly into the caller domain
            Assembly test = Assembly.LoadFile(assemblyFilePath);
            AppDomain.CurrentDomain.Load(test.GetName());

            return test;
        }
        #endregion

        #region Helpers
        private static Assembly AssemblyResolve(object sender, ResolveEventArgs args)
        {
            string dllName = args.Name.Split(new[] { ',' })[0] + ".dll";

            // The args.RequestingAssembly can be null apparently.
            // Check the locations of the assemblies for the dll.
            // This method may be called after the AppDomain.CurrentDomain.Load call,
            // e.g., while constructing the test cases or evaluating the (extended)
            // test framework attributes, wo the requested dll can be in any of
            // the previously loaded assembly directories.
            string path = null;
            if (!(args.RequestingAssembly?.Location is null))
            {
                path = Path.Combine(args.RequestingAssembly.Location, dllName);
                if (!File.Exists(path))
                {
                    path = null;
                }
            }
            if (path is null)
            {
                lock (s_assemblyLocations)
                {
                    foreach (string directory in s_assemblyLocations)
                    {
                        path = Path.Combine(directory, dllName);
                        if (!File.Exists(path))
                        {
                            path = null;
                        }
                        else
                        {
                            break;
                        }
                    }
                }
            }
            if (!(path is null))
            {
                try
                {
                    return Assembly.LoadFrom(path);
                }
                catch
                {
                    // this is called on several occasions, some are not related with our types or assemblies
                    // therefore there are calls that can't be resolved and that's OK
                }
            }
            return null;
        }
        #endregion
    }
}
