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
    /// The assemblies are loaded in the current <see cref="AppDomain"/> and are not unloaded. It is assumed
    /// that this functionality is used in short-lived processes.
    /// </summary>
    public sealed class TestCaseCollection
    {
        #region Fields
        private static readonly HashSet<string> s_assemblyLocations = new HashSet<string>();
        private readonly List<string> _assemblyFilePaths = new List<string>();
        private readonly Dictionary<string, int> _assemblyTestMethods = new Dictionary<string, int>();
        private readonly List<TestCase> _testCases = new List<TestCase>();
        private static readonly string s_realHardwareTrait = $"@{(new TestOnRealHardwareAttribute(false) as ITestOnRealHardware).Description}";
        #endregion

        #region Construction
        /// <summary>
        /// Get all test cases from a single assembly
        /// </summary>
        /// <param name="testAssemblyFilePath">Path of the assembly (*.dll) that may contain tests that use the nanoFramework test framework.</param>
        /// <param name="projectFilePath">Path of the project file that produced the assembly. If <c>null</c>
        /// is passed, the <see cref="TestCase"/> does not have the locations of tests in the source code. See also <see cref="ProjectSourceInventory.FindProjectFilePath"/>.</param>
        /// <param name="allowTestOnRealHardware">Indicates whether to include test cases that run on real hardware.
        /// See also <see cref="TestFrameworkConfiguration.AllowRealHardware"/>.</param>
        /// <param name="logger">Method to pass information about the discovery process to the caller.</param>
        /// <remarks>
        /// If this method is used to discover test cases just before they are executed and the test cases originate from multiple
        /// assemblies, use <see cref="TestCaseCollection(IEnumerable{string}, Func{string, string}, bool, LogMessenger)"/> instead for all
        /// assemblies at once.
        /// </remarks>
        public TestCaseCollection(string testAssemblyFilePath, bool allowTestOnRealHardware, string projectFilePath, LogMessenger logger)
            : this(new string[] { testAssemblyFilePath }, (a) => projectFilePath, allowTestOnRealHardware, logger)
        {
        }

        /// <summary>
        /// Get all test cases from a collection of assemblies
        /// </summary>
        /// <param name="testAssemblyFilePaths">Path of the assembly (*.dll) that may contain tests that use the nanoFramework test framework.</param>
        /// <param name="getProjectFilePath">Method that provides the path of the project file that produced the assembly. If <c>null</c>
        /// is passed for this argument or <c>null</c> is returned from the function, the <see cref="TestCase"/>s from that assembly do not provide
        /// the locations of tests in the source code. See also <see cref="ProjectSourceInventory.FindProjectFilePath"/>.</param>
        /// <param name="allowTestOnRealHardware">Indicates whether a test case for which no information is available on what device it should be run,
        /// is allowed to be executed on real hardware.
        /// See also <see cref="TestFrameworkConfiguration.AllowRealHardware"/>.</param>
        /// <param name="logger">Method to pass information about the discovery process to the caller.</param>
        /// <returns>A description of the tests in the assemblies, or <c>null</c> if the assembly does not contain tests.</returns>
        public TestCaseCollection(IEnumerable<string> testAssemblyFilePaths, Func<string, string> getProjectFilePath, bool allowTestOnRealHardware, LogMessenger logger)
        {
            foreach (string assemblyFilePath in from a in testAssemblyFilePaths // Sort the assemblies to make the order of test cases predictable
                                                orderby a
                                                select a)
            {
                string projectFilePath = getProjectFilePath?.Invoke(assemblyFilePath);
                if (projectFilePath is null)
                {
                    logger?.Invoke(LoggingLevel.Verbose, $"Project file for assembly '{assemblyFilePath}' not found");
                }
                ProjectSourceInventory sourceCode = projectFilePath is null ? null : new ProjectSourceInventory(projectFilePath, logger);

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

                AddTestClasses(assemblyFilePath, test, sourceCode, allowTestOnRealHardware, out int numTestMethods, logger);

                if (numTestMethods > 0)
                {
                    // This assembly has test cases
                    _assemblyFilePaths.Add(assemblyFilePath);
                    _assemblyTestMethods[assemblyFilePath] = numTestMethods;
                }
            }
        }

        /// <summary>
        /// Get a selection of test cases from one or more assemblies
        /// </summary>
        /// <param name="testCaseSelection">Enumeration of the selected test cases. The display name and fully qualified name of the test case
        /// must match the <see cref="TestCase.DisplayName"/> and <see cref="TestCase.FullyQualifiedName"/> of the previously collected test cases.</param>
        /// <param name="getProjectFilePath">Method that provides the path of the project file that produced the assembly. If <c>null</c>
        /// is passed for this argument or <c>null</c> is returned from the function, the <see cref="TestCase"/>s from that assembly do not provide
        /// the locations of tests in the source code. See also <see cref="ProjectSourceInventory.FindProjectFilePath"/>.</param>
        /// <param name="allowTestOnRealHardware">Indicates whether a test case for which no information is available on what device it should be run,
        /// is allowed to be executed on real hardware. The value of the parameter may be different from the value used to collect the
        /// test cases previously. 
        /// See also <see cref="TestFrameworkConfiguration.AllowRealHardware"/>.</param>
        /// <param name="logger">Method to pass information about the discovery process to the caller.</param>
        public TestCaseCollection(
            IEnumerable<(string testAssemblyPath, string testCaseDisplayName, string testCaseFullyQualifiedName)> testCaseSelection,
            Func<string, string> getProjectFilePath,
            bool allowTestOnRealHardware,
            LogMessenger logger)
            : this(testCaseSelection, getProjectFilePath, allowTestOnRealHardware, false, out Dictionary<int, int> testCaseForSelection, logger)
        {
        }

        /// <summary>
        /// Get a selection of test cases from one or more assemblies
        /// </summary>
        /// <param name="testCaseSelection">Enumeration of the selected test cases. The display name and fully qualified name of the test case
        /// must match the <see cref="TestCase.DisplayName"/> and <see cref="TestCase.FullyQualifiedName"/> of the previously collected test cases.</param>
        /// <param name="getProjectFilePath">Method that provides the path of the project file that produced the assembly. If <c>null</c>
        /// is passed for this argument or <c>null</c> is returned from the function, the <see cref="TestCase"/>s from that assembly do not provide
        /// the locations of tests in the source code. See also <see cref="ProjectSourceInventory.FindProjectFilePath"/>.</param>
        /// <param name="allowTestOnRealHardware">Indicates whether a test case for which no information is available on what device it should be run,
        /// is allowed to be executed on real hardware. The value of the parameter may be different from the value used to collect the
        /// test cases previously.</param>
        /// <param name="testCaseForSelection">The index of the test case in <see cref="TestCases"/> (value) for
        /// each test case in <paramref name="testCaseSelection"/> (index is the key) that has been found.</param>
        /// <param name="logger">Method to pass information about the discovery process to the caller.</param>
        public TestCaseCollection(
            IEnumerable<(string testAssemblyPath, string testCaseDisplayName, string testCaseFullyQualifiedName)> testCaseSelection,
            Func<string, string> getProjectFilePath,
            bool allowTestOnRealHardware,
            out Dictionary<int, int> testCaseForSelection,
            LogMessenger logger)
            : this(testCaseSelection, getProjectFilePath, allowTestOnRealHardware, true, out testCaseForSelection, logger)
        {
        }

        /// <summary>
        /// Get a selection of test cases from one or more assemblies
        /// </summary>
        /// <param name="testCaseSelection">Enumeration of the selected test cases. The display name and fully qualified name of the test case
        /// must match the <see cref="TestCase.DisplayName"/> and <see cref="TestCase.FullyQualifiedName"/> of the previously collected test cases.</param>
        /// <param name="getProjectFilePath">Method that provides the path of the project file that produced the assembly. If <c>null</c>
        /// is passed for this argument or <c>null</c> is returned from the function, the <see cref="TestCase"/>s from that assembly do not provide
        /// the locations of tests in the source code. See also <see cref="ProjectSourceInventory.FindProjectFilePath"/>.</param>
        /// <param name="allowTestOnRealHardware">Indicates whether a test case for which no information is available on what device it should be run,
        /// is allowed to be executed on real hardware. The value of the parameter may be different from the value used to collect the
        /// test cases previously.</param>
        /// <param name="testCaseForSelection">The index of the test case in <see cref="TestCases"/> (value) for
        /// each test case in <paramref name="testCaseSelection"/> (index is the key) that has been found.</param>
        /// <param name="logger">Method to pass information about the discovery process to the caller.</param>
        private TestCaseCollection(
                IEnumerable<(string testAssemblyPath, string testCaseDisplayName, string testCaseFullyQualifiedName)> testCaseSelection,
                Func<string, string> getProjectFilePath,
                bool allowTestOnRealHardware,
                bool createTestCaseForSelection, out Dictionary<int, int> testCaseForSelection,
                LogMessenger logger)
            : this(
                  new HashSet<string>(from tc in testCaseSelection
                                      select tc.testAssemblyPath),
                  getProjectFilePath,
                  allowTestOnRealHardware,
                  logger)
        {
            var selected = new HashSet<int>();
            testCaseForSelection = createTestCaseForSelection ? new Dictionary<int, int>() : null;

            int selectionIndex = 0;
            foreach ((string testAssemblyPath, string testCaseDisplayName, string testCaseFullyQualifiedName) in testCaseSelection)
            {
                string displayBaseName;
                string displayNameForVirtualDevice = null;
                if (testCaseDisplayName.EndsWith("]"))
                {
                    displayBaseName = testCaseDisplayName.Substring(0, testCaseDisplayName.IndexOf('[')).Trim();
                }
                else
                {
                    displayBaseName = testCaseDisplayName;
                    if (allowTestOnRealHardware)
                    {
                        displayNameForVirtualDevice = $"{displayBaseName} [{VIRTUALDEVICE}]";
                    }
                }

                bool testCaseFound = false;
                for (int i = 0; i < _testCases.Count; i++)
                {
                    TestCase testCase = _testCases[i];
                    if (testCase.AssemblyFilePath == testAssemblyPath
                        && testCase.FullyQualifiedName == testCaseFullyQualifiedName
                        && (
                                testCase.DisplayName == testCaseDisplayName // test cases then and now created
                                                                            // with the same allowTestOnRealHardware
                                ||
                                (
                                    allowTestOnRealHardware // now the test may have a device name,
                                                            // but then not when created with allowTestOnRealHardware=false
                                    && testCase.DisplayName == displayNameForVirtualDevice
                                )
                                ||
                                (
                                    !allowTestOnRealHardware // now the name does not include the device name,
                                                             // but then it did when created with allowTestOnRealHardware=true
                                    && testCase.DisplayName == displayBaseName
                                )
                            )
                        )
                    {
                        selected.Add(i);
                        if (createTestCaseForSelection)
                        {
                            testCaseForSelection[selectionIndex] = i;
                        }
                        testCaseFound = true;
                        break;
                    }
                }

                if (!testCaseFound)
                {
                    logger?.Invoke(LoggingLevel.Verbose, $"Test case '{testCaseDisplayName}' ({testCaseFullyQualifiedName}) from '{testAssemblyPath}' is no longer available");
                }
                selectionIndex++;
            }

            if (selected.Count < _testCases.Count)
            {
                // Keep the same order for the selected tests as discovered in the assemblies
                var testCases = new List<TestCase>();
                for (int i = 0; i < _testCases.Count; i++)
                {
                    if (selected.Contains(i))
                    {
                        testCases.Add(_testCases[i]);
                    }
                }
                _testCases = testCases;
            }
        }
        #endregion

        #region Properties
        /// <summary>
        /// Get the file paths of the assemblies for which test cases have been found.
        /// </summary>
        public IReadOnlyList<string> AssemblyFilePaths
            => _assemblyFilePaths;

        /// <summary>
        /// Get the total number of test methods in the assembly. Not all test methods
        /// may result in a test case, and some test methods result in multiple test cases.
        /// </summary>
        /// <param name="assemblyFilePath">One of the <see cref="AssemblyFilePaths"/>.</param>
        /// <returns></returns>
        public int TestMethodsInAssembly(string assemblyFilePath)
        {
            _assemblyTestMethods.TryGetValue(assemblyFilePath, out int result);
            return result;
        }

        /// <summary>
        /// Get all test cases in the collection. The test cases are sorted by assembly
        /// first (same order as <see cref="AssemblyFilePaths"/>), then by the order in
        /// which they are discovered in the assembly.
        /// </summary>
        public IReadOnlyList<TestCase> TestCases
            => _testCases;
        #endregion

        #region Helpers for test case discovery
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

        private void AddTestClasses(string assemblyFilePath, Assembly assembly, ProjectSourceInventory sourceCode, bool allowTestOnRealHardware, out int testCaseIndex, LogMessenger logger)
        {
            testCaseIndex = 0;
            int testGroupIndex = 0;
            var framework = new TestFrameworkImplementation();

            // Default for all tests
            TestOnRealHardwareProxy defaultRealHardwareProxy = allowTestOnRealHardware
                ? new TestOnRealHardwareProxy(new TestOnRealHardwareAttribute(false), new TestFrameworkImplementation(), typeof(ITestOnRealHardware))
                : null;

            // Defaults for the assembly
            List<AttributeProxy> assemblyAttributes = AttributeProxy.GetAttributeProxies(assembly, framework, logger);
            bool testAllOnVirtualDevice = assemblyAttributes.OfType<TestOnVirtualDeviceProxy>().Any();
            Dictionary<string, List<TestOnRealHardwareProxy>> testAllOnRealHardware = allowTestOnRealHardware
                ? TestOnRealHardwareProxy.Collect(null, assemblyAttributes.OfType<TestOnRealHardwareProxy>())
                : null;

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
                List<AttributeProxy> classAttributes = AttributeProxy.GetAttributeProxies(classType, framework, classSourceLocation?.Attributes, logger);
                TestClassProxy testClassAttribute = classAttributes.OfType<TestClassProxy>().FirstOrDefault();
                if (testClassAttribute is null)
                {
                    continue;
                }
                foreach (TestClassProxy attribute in classAttributes.OfType<TestClassProxy>())
                {
                    if (attribute != testClassAttribute)
                    {
                        logger?.Invoke(LoggingLevel.Verbose, $"{attribute.Source?.ForMessage() ?? classType.FullName}: Only one attribute that implements '{nameof(ITestClass)}' is allowed. Only the first one is used, subsequent attributes are ignored.");
                    }
                }

                bool testClassTestOnVirtualDevice = testAllOnVirtualDevice || classAttributes.OfType<TestOnVirtualDeviceProxy>().Any();
                Dictionary<string, List<TestOnRealHardwareProxy>> testClassTestOnRealHardware = allowTestOnRealHardware
                    ? TestOnRealHardwareProxy.Collect(testAllOnRealHardware, classAttributes.OfType<TestOnRealHardwareProxy>())
                    : null;

                var group = new TestCaseGroup(++testGroupIndex);
                #endregion

                #region A method is turned into zero or more test cases
                bool hasSetup = false;
                bool hasCleanup = false;
                var previousDisplayNames = new HashSet<string>();
                foreach ((MethodInfo method, ProjectSourceInventory.MethodDeclaration sourceLocation) in enumerateMethods())
                {
                    List<AttributeProxy> methodAttributes = AttributeProxy.GetAttributeProxies(method, framework, sourceLocation?.Attributes, logger);
                    if (methodAttributes.Count == 0)
                    {
                        continue;
                    }

                    #region Setup / cleanup
                    SetupProxy setup = methodAttributes.OfType<SetupProxy>().FirstOrDefault();
                    if (!(setup is null))
                    {
                        if (hasSetup)
                        {
                            logger?.Invoke(LoggingLevel.Verbose, $"{setup.Source?.ForMessage() ?? $"{classType.FullName}.{method.Name}"}: Only one method of a class can have attribute implements '{nameof(ISetup)}'. Subsequent attribute is ignored.");
                        }
                        else
                        {
                            hasSetup = true;
                            group.SetupSourceCodeLocation = setup.Source;
                        }
                    }
                    CleanupProxy cleanup = methodAttributes.OfType<CleanupProxy>().FirstOrDefault();
                    if (!(cleanup is null))
                    {
                        if (hasCleanup)
                        {
                            logger?.Invoke(LoggingLevel.Verbose, $"{cleanup.Source?.ForMessage() ?? $"{classType.FullName}.{method.Name}"}: Only one method of a class can have attribute that implements '{nameof(ICleanup)}'. Subsequent attribute is ignored.");
                        }
                        else
                        {
                            hasCleanup = true;
                            group.CleanupSourceCodeLocation = cleanup.Source;
                        }
                    }
                    #endregion

                    if (setup is null && cleanup is null)
                    {
                        #region Create test cases from the test method
                        var traits = new HashSet<string>();
                        foreach (TraitsProxy attribute in methodAttributes.OfType<TraitsProxy>())
                        {
                            traits.UnionWith(attribute.Traits);
                        }

                        bool testOnVirtualDevice = testClassTestOnVirtualDevice || methodAttributes.OfType<TestOnVirtualDeviceProxy>().Any();
                        Dictionary<string, List<TestOnRealHardwareProxy>> testOnRealHardware = allowTestOnRealHardware
                            ? TestOnRealHardwareProxy.Collect(testClassTestOnRealHardware, methodAttributes.OfType<TestOnRealHardwareProxy>())
                            : null;

                        int deviceTypeCount = (testOnRealHardware?.Count ?? 0) + (testOnVirtualDevice ? 1 : 0);
                        if (deviceTypeCount == 0)
                        {
                            string methodInSource = sourceLocation is null
                                ? $"{method.ReflectedType.Assembly.GetName().Name}:{method.ReflectedType.FullName}.{method.Name}"
                                : sourceLocation.ForMessage();
                            logger?.Invoke(LoggingLevel.Detailed, $"{methodInSource}: Method, class and assembly have no attributes to indicate on what device the test should be run.");

                            // The defaults are:
                            testOnVirtualDevice = true;
                            if (!(defaultRealHardwareProxy is null))
                            {
                                testOnRealHardware = TestOnRealHardwareProxy.Collect(null, new TestOnRealHardwareProxy[] { defaultRealHardwareProxy });
                            }
                            deviceTypeCount = (testOnRealHardware?.Count ?? 0) + (testOnVirtualDevice ? 1 : 0);
                        }

                        var dataRowParameters = (from dataRow in methodAttributes.OfType<DataRowProxy>()
                                                 select (dataRow.Source, dataRow.MethodParametersAsString)).ToList();
                        if (dataRowParameters.Count == 0)
                        {
                            dataRowParameters.Add((sourceLocation, ""));
                        }

                        foreach ((ProjectSourceInventory.ElementDeclaration testCaseSource, string methodParametersAsString) in dataRowParameters)
                        {
                            ++testCaseIndex;
                            string displayNameBase = $"{method.Name}{methodParametersAsString}";
                            for (int i = 2; true; i++)
                            {
                                if (previousDisplayNames.Add(displayNameBase))
                                {
                                    break;
                                }
                                displayNameBase = $"{method.Name}{methodParametersAsString} #{i}";
                            }

                            if (testOnVirtualDevice)
                            {
                                _testCases.Add(new TestCase(
                                    testCaseIndex,
                                    assemblyFilePath,
                                    group,
                                    method, $"{displayNameBase}{(deviceTypeCount > 1 ? $" [{VIRTUALDEVICE}]" : "")}",
                                    testCaseSource,
                                    true, null,
                                    traits, $"@{VIRTUALDEVICE}"
                                ));
                            }
                            if (!(testOnRealHardware is null))
                            {
                                foreach (KeyValuePair<string, List<TestOnRealHardwareProxy>> device in testOnRealHardware)
                                {
                                    _testCases.Add(new TestCase(
                                        testCaseIndex,
                                        assemblyFilePath,
                                        group,
                                        method, $"{displayNameBase}{(deviceTypeCount > 1 ? $" [{device.Key}]" : "")}",
                                        testCaseSource,
                                        false, device.Value,
                                        traits, $"@{device.Key}", s_realHardwareTrait
                                    ));
                                }
                            }
                        }
                        #endregion
                    }
                    else if (methodAttributes.Count > (setup is null ? 0 : 1) + (cleanup is null ? 0 : 1))
                    {
                        logger?.Invoke(LoggingLevel.Verbose, $"{sourceLocation?.ForMessage() ?? $"{classType.FullName}.{method.Name}"}: No other attributes are allowed when the attributes that implement '{nameof(ICleanup)}'/'{nameof(ISetup)}' are present. Extra attributes are ignored.");
                    }
                }
                #endregion
            }
        }
        private const string VIRTUALDEVICE = "Virtual Device";
        #endregion
    }
}
