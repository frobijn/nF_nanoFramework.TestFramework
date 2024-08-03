// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using nanoFramework.TestFramework.Tooling.TestFrameworkProxy;

namespace nanoFramework.TestFramework.Tooling
{
    /// <summary>
    /// The analyser examines an assembly for tests coded with the nanoFramework TestFramework.
    /// It can analyse every type of assembly, whether or not the assembly is targeted for the nanoFramework CLR or not.
    /// </summary>
    public static class AssemblyAnalyzer
    {
        #region Get the test cases
        /// <summary>
        /// Examine an assembly and return the information about the tests in that assembly.
        /// </summary>
        /// <param name="testAssemblyFilePath">Path of the assembly (*.dll) that may contain tests that use the nanoFramework test framework.</param>
        /// <param name="projectDirectoryPath">Directory where the project resides that produced the assembly. If <c>null</c>
        /// is passed, the <see cref="TestCase"/> does not have the locations of tests in the source code. See also <see cref="ProjectSourceInventory.FindProjectFilePath"/>.</param>
        /// <param name="inCurrentAppDomain">Indicates whether to load the assembly in the current <see cref="AppDomain"/>. Pass <c>true</c> for faster
        /// execution, but the assemblies will be kept loaded.</param>
        /// <param name="logger">Method to pass information about the discovery process to the caller.</param>
        /// <returns>A description of the tests in the assembly, or <c>null</c> if the assembly does not contain tests.</returns>
        /// <remarks>
        /// If this method is used to discover test cases just before they are executed and the test cases originate from multiple
        /// assemblies, use <see cref="FindTestCases(IEnumerable{string}, Func{string, string}, bool, LogMessenger)"/> instead for all
        /// assemblies at once.
        /// </remarks>
        public static List<TestCase> FindTestCases(string testAssemblyFilePath, string projectDirectoryPath, bool inCurrentAppDomain, LogMessenger logger)
        {
            return FindTestCases(new string[] { testAssemblyFilePath }, (a) => projectDirectoryPath, inCurrentAppDomain, logger);
        }

        /// <summary>
        /// Examine an assembly and return the information about the tests in that assembly.
        /// </summary>
        /// <param name="testAssemblyFilePaths">Path of the assembly (*.dll) that may contain tests that use the nanoFramework test framework.</param>
        /// <param name="getProjectDirectoryPath">Method that provides the directory where the project resides that produced the assembly. If <c>null</c>
        /// is passed for this argument or <c>null</c> is returned from the function, the <see cref="TestCase"/>s from that assembly do not provide
        /// the locations of tests in the source code. See also <see cref="ProjectSourceInventory.FindProjectFilePath"/>.</param>
        /// <param name="inCurrentAppDomain">Indicates whether to load the assembly in the current <see cref="AppDomain"/>. Pass <c>true</c> for faster
        /// execution, but the assemblies will be kept loaded.</param>
        /// <param name="logger">Method to pass information about the discovery process to the caller.</param>
        /// <returns>A description of the tests in the assemblies, or <c>null</c> if the assembly does not contain tests.</returns>
        public static List<TestCase> FindTestCases(IEnumerable<string> testAssemblyFilePaths, Func<string, string> getProjectDirectoryPath, bool inCurrentAppDomain, LogMessenger logger)
        {
            var result = new List<TestCase>();

            foreach (string assemblyFilePath in testAssemblyFilePaths)
            {
                string projectFilePath = getProjectDirectoryPath?.Invoke(assemblyFilePath);
                ProjectSourceInventory sourceCode = projectFilePath is null ? null : new ProjectSourceInventory(projectFilePath, logger);

                Assembly test = Assembly.LoadFile(assemblyFilePath);
                AppDomain appDomain = AppDomain.CurrentDomain;
                if (!inCurrentAppDomain)
                {
                    appDomain = AppDomain.CreateDomain(typeof(AssemblyAnalyzer).Name);
                }
                appDomain.AssemblyResolve += App_AssemblyResolve;
                try
                {
                    Assembly assembly = appDomain.Load(test.GetName());
                    AddTestClasses(result, assembly, sourceCode, logger);
                }
                finally
                {
                    appDomain.AssemblyResolve -= App_AssemblyResolve;
                }
            }

            return result;
        }
        private static Assembly App_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            try
            {
                string dllName = args.Name.Split(new[] { ',' })[0] + ".dll";
                string path = Path.GetDirectoryName(args.RequestingAssembly.Location);
                return Assembly.LoadFrom(Path.Combine(path, dllName));
            }
            catch
            {
                // this is called on several occasions, some are not related with our types or assemblies
                // therefore there are calls that can't be resolved and that's OK
                return null;
            }
        }

        private static void AddTestClasses(List<TestCase> testCases, Assembly assembly, ProjectSourceInventory sourceCode, LogMessenger logger)
        {
            int testGroupIndex = 0;
            int testCaseIndex = 0;

            // Defaults for the assembly
            List<AttributeProxy> assemblyAttributes = AttributeProxy.GetAttributeProxies(assembly, logger);
            bool testAllOnVirtualDevice = assemblyAttributes.OfType<TestOnVirtualDeviceProxy>().Any();
            Dictionary<string, List<TestOnRealHardwareProxy>> testAllOnRealHardware = TestOnRealHardwareProxy.Collect(null, assemblyAttributes.OfType<TestOnRealHardwareProxy>());
            bool runAllInParallel = RunInParallelProxy.RunInParallel(assemblyAttributes.OfType<RunInParallelProxy>());

            // Find the test classes
            foreach (
                (
                    Type classType,
                    ProjectSourceInventory.ClassDeclaration classSourceLocation,
                    Func<IEnumerable<(MethodInfo method, ProjectSourceInventory.MethodDeclaration sourceLocation)>> enumerateMethods
                )
                in ProjectSourceInventory.EnumerateNonAbstractClasses(assembly, sourceCode))
            {
                #region A class is modelled as a group
                List<AttributeProxy> classAttributes = AttributeProxy.GetAttributeProxies(classType, classSourceLocation?.Attributes, logger);
                TestClassProxy testClassAttribute = classAttributes.OfType<TestClassProxy>().FirstOrDefault();
                if (testClassAttribute is null)
                {
                    continue;
                }

                bool testClassOnVirtualDevice = testAllOnVirtualDevice || classAttributes.OfType<TestOnVirtualDeviceProxy>().Any();
                Dictionary<string, List<TestOnRealHardwareProxy>> testClassOnRealHardware = TestOnRealHardwareProxy.Collect(testAllOnRealHardware, assemblyAttributes.OfType<TestOnRealHardwareProxy>());
                bool testClassRunInParallel = RunInParallelProxy.RunInParallel(classAttributes.OfType<RunInParallelProxy>(), runAllInParallel);

                var group = new TestCaseGroup(++testGroupIndex, testClassRunInParallel, testClassAttribute.RunTestMethodsOneAfterTheOther);
                #endregion

                #region A method is turned into zero or more test cases
                bool hasSetup = false;
                bool hasCleanup = false;
                foreach ((MethodInfo method, ProjectSourceInventory.MethodDeclaration sourceLocation) in enumerateMethods())
                {
                    List<AttributeProxy> methodAttributes = AttributeProxy.GetAttributeProxies(method, sourceLocation?.Attributes, logger);

                    #region Setup / cleanup
                    SetupProxy setup = methodAttributes.OfType<SetupProxy>().FirstOrDefault();
                    if (!(setup is null))
                    {
                        if (hasSetup)
                        {
                            logger?.Invoke(LoggingLevel.Verbose, $"{setup.Source?.ForMessage() ?? $"{classType.FullName}.{method.Name}"}: only one method of a class can have attribute '{nameof(SetupAttribute)}'. Subsequent attribute is ignored.");
                        }
                        else
                        {
                            hasSetup = true;
                            group.SetupSourceCodeLocation = setup.Source;
                            group.SetupCleanupForEachTest = testClassAttribute.Instantiation == TestClassProxy.TestClassInstantiation.PerMethod;
                        }
                    }
                    CleanupProxy cleanup = methodAttributes.OfType<CleanupProxy>().FirstOrDefault();
                    if (!(cleanup is null))
                    {
                        if (hasCleanup)
                        {
                            logger?.Invoke(LoggingLevel.Verbose, $"{cleanup.Source?.ForMessage() ?? $"{classType.FullName}.{method.Name}"}: only one method of a class can have attribute '{nameof(CleanupAttribute)}'. Subsequent attribute is ignored.");
                        }
                        else
                        {
                            hasCleanup = true;
                            group.CleanupSourceCodeLocation = setup.Source;
                            group.SetupCleanupForEachTest = testClassAttribute.Instantiation == TestClassProxy.TestClassInstantiation.PerMethod;
                        }
                    }
                    #endregion

                    if (setup is null && cleanup is null)
                    {
                        testCases.AddRange(
                            TestCase.Create(method, group, methodAttributes,
                                testClassOnVirtualDevice, testClassOnRealHardware, testClassRunInParallel,
                                sourceLocation,
                                ref testCaseIndex,
                                logger)
                        );
                    }
                    else if (methodAttributes.Count > (setup is null ? 0 : 1) + (cleanup is null ? 0 : 1))
                    {
                        logger?.Invoke(LoggingLevel.Verbose, $"{sourceLocation?.ForMessage() ?? $"{classType.FullName}.{method.Name}"}: no other attributes are allowed when the attributes '{nameof(CleanupAttribute)}'/'{nameof(SetupAttribute)}' are present. Extra attributes are ignored.");
                    }
                }
                #endregion
            }
        }
        #endregion
    }
}
