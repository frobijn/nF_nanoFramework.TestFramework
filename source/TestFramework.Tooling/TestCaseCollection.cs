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
        public const string VirtualDeviceDescription = "Virtual Device";
        private static readonly string s_realHardwareDescription = (new TestOnRealHardwareAttribute() as ITestOnRealHardware).Description;
        private readonly List<TestCaseSelection> _testsOnVirtualDevice = new List<TestCaseSelection>();
        private readonly List<TestCaseSelection> _testsOnRealHardware = new List<TestCaseSelection>();
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
            foreach (string assemblyFilePath in from a in testAssemblyFilePaths // Sort the paths to make the messages predictable
                                                orderby a
                                                select a)
            {
                string projectFilePath = getProjectFilePath?.Invoke(assemblyFilePath);
                if (projectFilePath is null)
                {
                    logger?.Invoke(LoggingLevel.Verbose, $"Project file for assembly '{assemblyFilePath}' not found.");
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

                AddTestClasses(assemblyFilePath, test, sourceCode, allowTestOnRealHardware, logger);

                static void SetSelectionIndex(List<TestCaseSelection> selections)
                {
                    foreach (TestCaseSelection selection in selections)
                    {
                        for (int i = 0; i < selection._testCases.Count; i++)
                        {
                            selection._testCases[i] = (-i - 1, selection._testCases[i].testCase);
                        }
                    }
                }
                SetSelectionIndex(_testsOnVirtualDevice);
                SetSelectionIndex(_testsOnRealHardware);
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
            : this(
                  new HashSet<string>(from tc in testCaseSelection
                                      select tc.testAssemblyPath),
                  getProjectFilePath,
                  allowTestOnRealHardware,
                  logger)
        {
            int selectionIndex = 0;
            string currentAssemblyPath = null;
            TestCaseSelection testsOnVirtualDevice = null;
            TestCaseSelection testsOnRealHardware = null;
            foreach ((string testAssemblyPath, string testCaseDisplayName, string testCaseFullyQualifiedName) in testCaseSelection)
            {
                if (testAssemblyPath != currentAssemblyPath)
                {
                    currentAssemblyPath = testAssemblyPath;
                    testsOnVirtualDevice = (from tc in _testsOnVirtualDevice
                                            where tc.AssemblyFilePath == testAssemblyPath
                                            select tc).FirstOrDefault();
                    testsOnRealHardware = (from tc in _testsOnRealHardware
                                           where tc.AssemblyFilePath == testAssemblyPath
                                           select tc).FirstOrDefault();
                }

                bool testCaseFound = false;
                bool testCaseNotSelected = false;

                string displayBaseName = TestCase.DisplayNameWithoutDevice(testCaseDisplayName);
                string displayNameForVirtualDevice = null;
                string displayNameForRealHardware = null;
                if (displayBaseName == testCaseDisplayName)
                {
                    if (allowTestOnRealHardware)
                    {
                        displayNameForVirtualDevice = $"{displayBaseName} [{VirtualDeviceDescription}]";
                    }
                    else
                    {
                        displayNameForRealHardware = $"{displayBaseName} [{s_realHardwareDescription}]";
                    }
                }
                bool mustBeForVirtualDevice = testCaseDisplayName == displayNameForVirtualDevice;
                bool mustBeForRealHardware = testCaseDisplayName == displayNameForRealHardware;

                if (!(testsOnVirtualDevice is null))
                {
                    for (int i = 0; i < testsOnVirtualDevice.TestCases.Count; i++)
                    {
                        (int selIndex, TestCase testCase) = testsOnVirtualDevice.TestCases[i];
                        if (testCase.FullyQualifiedName == testCaseFullyQualifiedName)
                        {
                            if (
                                testCase.DisplayName == testCaseDisplayName // test cases then and now created
                                                                            // with the same allowTestOnRealHardware,
                                                                            // or test can only run on the virtual device
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
                                    && !mustBeForRealHardware // It is not the real hardware test case
                                    && testCase.DisplayName == displayBaseName
                                )
                            )
                            {
                                if (selIndex < 0) // if selIndex >= 0, the test case was already found, Ignore this one
                                {
                                    testsOnVirtualDevice._testCases[i] = (selectionIndex, testCase);
                                }
                                testCaseFound = true;
                                break;
                            }
                            else if (
                                    !allowTestOnRealHardware // now the name does not include the device name,
                                                             // but then it did when created with allowTestOnRealHardware=true
                                    && mustBeForRealHardware // It is the real hardware test case
                                    && testCase.DisplayName == displayBaseName
                                )
                            {
                                testCaseNotSelected = true;
                                break;
                            }
                        }
                    }
                }
                if (!(testCaseFound || testCaseNotSelected)
                    && !(testsOnRealHardware is null))
                {
                    // allowTestOnRealHardware must be true

                    for (int i = 0; i < testsOnRealHardware.TestCases.Count; i++)
                    {
                        (int selIndex, TestCase testCase) = testsOnRealHardware.TestCases[i];
                        if (testCase.FullyQualifiedName == testCaseFullyQualifiedName)
                        {
                            if (
                                testCase.DisplayName == testCaseDisplayName // test cases then and now created
                                                                            // with the same allowTestOnRealHardware = true,
                                                                            // or test can only run on real hardware
                            )
                            {
                                if (selIndex < 0) // if selIndex >= 0, the test case was already found, ignore this one
                                {
                                    testsOnRealHardware._testCases[i] = (selectionIndex, testCase);
                                }
                                testCaseFound = true;
                                break;
                            }
                            else if (
                                testCase.DisplayName == displayBaseName // the selection was created with allowTestOnRealHardware = false,
                                                                        // so this must be the virtual device test case
                                )
                            {
                                testCaseNotSelected = true;
                                break;
                            }
                        }
                    }
                }

                if (!testCaseFound && !testCaseNotSelected)
                {
                    logger?.Invoke(LoggingLevel.Verbose, $"Test case '{testCaseDisplayName}' ({testCaseFullyQualifiedName}) from '{testAssemblyPath}' is no longer available");
                }
                selectionIndex++;
            }

            static List<TestCaseSelection> KeepSelectionOnly(List<TestCaseSelection> all)
            {
                List<TestCaseSelection> result = new List<TestCaseSelection>();
                foreach (TestCaseSelection selection in all)
                {
                    selection._testCases = (from tc in selection._testCases
                                            where tc.selectionIndex >= 0
                                            select tc).ToList();
                    if (selection._testCases.Count > 0)
                    {
                        result.Add(selection);
                    }
                }
                return result;
            }
            _testsOnRealHardware = KeepSelectionOnly(_testsOnRealHardware);
            _testsOnVirtualDevice = KeepSelectionOnly(_testsOnVirtualDevice);
        }
        #endregion

        #region Properties
        /// <summary>
        /// Get the tests per assembly to be run on the virtual device
        /// </summary>
        public IReadOnlyList<TestCaseSelection> TestOnVirtualDevice
            => _testsOnVirtualDevice;

        /// <summary>
        /// Get the tests per assembly to be run on real hardware
        /// </summary>
        public IReadOnlyList<TestCaseSelection> TestOnRealHardware
            => _testsOnRealHardware;

        /// <summary>
        /// Get all test cases in the collection.
        /// </summary>
        public IEnumerable<TestCase> TestCases
        {
            get
            {
                foreach (TestCaseSelection selection in _testsOnVirtualDevice)
                {
                    foreach ((int _, TestCase testCase) in selection._testCases)
                    {
                        yield return testCase;
                    }
                }
                foreach (TestCaseSelection selection in _testsOnRealHardware)
                {
                    foreach ((int _, TestCase testCase) in selection._testCases)
                    {
                        yield return testCase;
                    }
                }
            }
        }
        #endregion

        #region Helpers for assembly loading
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

        #region Helpers for test case discovery
        /// <summary>
        /// Description of the test cases per assembly and per device type
        /// (Virtual Device or real hardware)
        /// </summary>
        private void AddTestClasses(string assemblyFilePath, Assembly assembly, ProjectSourceInventory sourceCode, bool allowTestOnRealHardware, LogMessenger logger)
        {
            var testsOnVirtualDevice = new TestCaseSelection(assemblyFilePath);
            var testsOnRealHardware = new TestCaseSelection(assemblyFilePath);

            var framework = new TestFrameworkImplementation();

            // Default for all tests
            TestOnRealHardwareProxy defaultRealHardwareProxy = allowTestOnRealHardware
                ? new TestOnRealHardwareProxy(new TestOnRealHardwareAttribute(), new TestFrameworkImplementation(), typeof(ITestOnRealHardware))
                : null;

            // Defaults for the assembly
            List<AttributeProxy> assemblyAttributes = AttributeProxy.GetAssemblyAttributeProxies(assembly, framework, logger);
            HashSet<string> allTestsTraits = TraitsProxy.Collect(null, assemblyAttributes.OfType<TraitsProxy>());
            bool testAllOnVirtualDevice = assemblyAttributes.OfType<TestOnVirtualDeviceProxy>().Any();
            (HashSet<string> descriptions, List<TestOnRealHardwareProxy> attributes) testAllOnRealHardware = allowTestOnRealHardware
                ? TestOnRealHardwareProxy.Collect((null, null), assemblyAttributes.OfType<TestOnRealHardwareProxy>())
                : (null, null);

            // Find the test classes
            foreach (
                (
                    int testGroupIndex,
                    Type classType,
                    ProjectSourceInventory.ClassDeclaration classSourceLocation,
                    Func<IEnumerable<(int methodIndex, MethodInfo method, ProjectSourceInventory.MethodDeclaration sourceLocation)>> enumerateMethods
                )
                in ProjectSourceInventory.EnumerateNonAbstractClasses(assembly, sourceCode))
            {
                #region A class is modelled as a group
                List<AttributeProxy> classAttributes = AttributeProxy.GetClassAttributeProxies(classType, framework, classSourceLocation?.Attributes, logger);
                TestClassProxy testClassAttribute = classAttributes.OfType<TestClassProxy>().FirstOrDefault();
                if (testClassAttribute is null)
                {
                    continue;
                }
                foreach (TestClassProxy attribute in classAttributes.OfType<TestClassProxy>())
                {
                    if (attribute != testClassAttribute)
                    {
                        logger?.Invoke(LoggingLevel.Verbose, $"{attribute.Source?.ForMessage() ?? classType.FullName}: Warning: Only one attribute that implements '{nameof(ITestClass)}' is allowed. Only the first one is used, subsequent attributes are ignored.");
                    }
                }

                HashSet<string> testClassTraits = TraitsProxy.Collect(allTestsTraits, assemblyAttributes.OfType<TraitsProxy>());
                bool testClassTestOnVirtualDevice = testAllOnVirtualDevice || classAttributes.OfType<TestOnVirtualDeviceProxy>().Any();
                (HashSet<string> descriptions, List<TestOnRealHardwareProxy> attributes) testClassTestOnRealHardware = allowTestOnRealHardware
                    ? TestOnRealHardwareProxy.Collect(testAllOnRealHardware, classAttributes.OfType<TestOnRealHardwareProxy>())
                    : (null, null);

                var group = new TestCaseGroup(
                    classType.FullName,
                    classType.IsAbstract && classType.IsSealed
                        ? TestCaseGroup.InstantiationType.NoInstantiation
                        : testClassAttribute.CreateInstancePerTestMethod
                            ? TestCaseGroup.InstantiationType.InstantiatePerTestMethod
                            : TestCaseGroup.InstantiationType.InstantiateForAllMethods,
                    testClassAttribute.SetupCleanupPerTestMethod);
                #endregion

                #region A method is turned into zero or more test cases
                bool hasSetup = false;
                bool hasCleanup = false;
                var previousDisplayNames = new HashSet<string>();
                foreach ((int methodIndex, MethodInfo method, ProjectSourceInventory.MethodDeclaration sourceLocation) in enumerateMethods())
                {
                    List<AttributeProxy> methodAttributes = AttributeProxy.GetMethodAttributeProxies(method, framework, sourceLocation?.Attributes, logger);
                    if (methodAttributes.Count == 0)
                    {
                        continue;
                    }
                    DeploymentConfigurationProxy deploymentProxy = methodAttributes.OfType<DeploymentConfigurationProxy>().FirstOrDefault();
                    foreach (DeploymentConfigurationProxy attribute in methodAttributes.OfType<DeploymentConfigurationProxy>())
                    {
                        if (attribute != deploymentProxy)
                        {
                            logger?.Invoke(LoggingLevel.Verbose, $"{attribute.Source?.ForMessage() ?? classType.FullName}: Warning: Only one attribute that implements '{nameof(IDeploymentConfiguration)}' is allowed. Only the first one is used, subsequent attributes are ignored.");
                        }
                    }

                    #region Setup / cleanup
                    SetupProxy setup = methodAttributes.OfType<SetupProxy>().FirstOrDefault();
                    if (!(setup is null))
                    {
                        if (hasSetup)
                        {
                            logger?.Invoke(LoggingLevel.Verbose, $"{setup.Source?.ForMessage() ?? $"{classType.FullName}.{method.Name}"}: Warning: Only one method of a class can have attribute implements '{nameof(ISetup)}'. Subsequent attribute is ignored.");
                        }
                        else
                        {
                            hasSetup = true;
                            group.SetupMethodName = method.Name;
                            group.SetupSourceCodeLocation = setup.Source;
                            group.RequiredConfigurationKeys = deploymentProxy?.GetDeploymentConfigurationArguments(method, false, logger)
                                ?? new (string key, bool asBytes)[] { };
                            deploymentProxy = null;
                        }
                    }
                    CleanupProxy cleanup = methodAttributes.OfType<CleanupProxy>().FirstOrDefault();
                    if (!(cleanup is null))
                    {
                        if (hasCleanup)
                        {
                            logger?.Invoke(LoggingLevel.Verbose, $"{cleanup.Source?.ForMessage() ?? $"{classType.FullName}.{method.Name}"}: Warning: Only one method of a class can have attribute that implements '{nameof(ICleanup)}'. Subsequent attribute is ignored.");
                        }
                        else
                        {
                            hasCleanup = true;
                            group.CleanupMethodName = method.Name;
                            group.CleanupSourceCodeLocation = cleanup.Source;
                        }
                        if (!(deploymentProxy is null))
                        {
                            logger?.Invoke(LoggingLevel.Error, $"{cleanup.Source?.ForMessage() ?? $"{classType.FullName}.{method.Name}"}: Error: A cleanup method cannot have an attribute that implements '{nameof(IDeploymentConfiguration)}' - the attribute is ignored.");
                            deploymentProxy = null;
                        }
                    }
                    #endregion

                    if (setup is null && cleanup is null)
                    {
                        #region Create test cases from the test method
                        HashSet<string> testTraits = TraitsProxy.Collect(testClassTraits, assemblyAttributes.OfType<TraitsProxy>());
                        bool testOnVirtualDevice = testClassTestOnVirtualDevice || methodAttributes.OfType<TestOnVirtualDeviceProxy>().Any();
                        (HashSet<string> descriptions, List<TestOnRealHardwareProxy> attributes) testOnRealHardware = allowTestOnRealHardware
                            ? TestOnRealHardwareProxy.Collect(testClassTestOnRealHardware, methodAttributes.OfType<TestOnRealHardwareProxy>())
                            : (null, null);

                        int deviceTypeCount = (testOnRealHardware.descriptions is null ? 0 : 1) + (testOnVirtualDevice ? 1 : 0);
                        if (deviceTypeCount == 0)
                        {
                            string methodInSource = sourceLocation is null
                                ? $"{method.ReflectedType.Assembly.GetName().Name}:{method.ReflectedType.FullName}.{method.Name}"
                                : sourceLocation.ForMessage();
                            logger?.Invoke(LoggingLevel.Detailed, $"{methodInSource}: Warning: Method, class and assembly have no attributes to indicate on what device the test should be run. The defaults will be used.");

                            // The defaults are:
                            testOnVirtualDevice = true;
                            if (!(defaultRealHardwareProxy is null))
                            {
                                testOnRealHardware = TestOnRealHardwareProxy.Collect((null, null), new TestOnRealHardwareProxy[] { defaultRealHardwareProxy });
                            }
                            deviceTypeCount = (testOnRealHardware.descriptions is null ? 0 : 1) + (testOnVirtualDevice ? 1 : 0);
                        }

                        var dataRowParameters = (from dataRow in methodAttributes.OfType<DataRowProxy>()
                                                 select (dataRow.Source, dataRow.MethodParametersAsString)).ToList();
                        int dataRowIndex = 0;
                        if (dataRowParameters.Count == 0)
                        {
                            dataRowParameters.Add((sourceLocation, ""));
                            dataRowIndex = -1;
                        }

                        foreach ((ProjectSourceInventory.ElementDeclaration testCaseSource, string methodParametersAsString) in dataRowParameters)
                        {

                            string displayNameBase = $"{method.Name}{methodParametersAsString}";
                            for (int i = 2; true; i++)
                            {
                                if (previousDisplayNames.Add(displayNameBase))
                                {
                                    break;
                                }
                                displayNameBase = $"{method.Name}{methodParametersAsString} #{i}";
                            }
                            string testCaseId = dataRowIndex < 0 ? $"G{testGroupIndex:000}T{methodIndex:000}" : $"G{testGroupIndex:000}T{methodIndex:000}D{dataRowIndex:00}";

                            IReadOnlyList<(string key, bool asBytes)> deploymentArguments = deploymentProxy?.GetDeploymentConfigurationArguments(method, dataRowIndex >= 0, logger);

                            if (testOnVirtualDevice)
                            {
                                testsOnVirtualDevice._testCases.Add((-1,
                                    new TestCase(
                                        testCaseId,
                                        dataRowIndex,
                                        assemblyFilePath,
                                        group,
                                        method, $"{displayNameBase}{(deviceTypeCount > 1 ? $" [{VirtualDeviceDescription}]" : "")}",
                                        testCaseSource,
                                        true, null,
                                        deploymentArguments,
                                        TraitsProxy.Collect(testTraits, null, new string[] { $"@{VirtualDeviceDescription}" })
                                    )));
                            }
                            if (!(testOnRealHardware.descriptions is null))
                            {
                                HashSet<string> traits = TraitsProxy.Collect(testTraits, null, from d in testOnRealHardware.descriptions
                                                                                               select $"@{d}");
                                traits.Add($"@{s_realHardwareDescription}");
                                testsOnRealHardware._testCases.Add((-1,
                                    new TestCase(
                                        testCaseId,
                                        dataRowIndex,
                                        assemblyFilePath,
                                        group,
                                        method, $"{displayNameBase}{(deviceTypeCount > 1 ? $" [{s_realHardwareDescription}]" : "")}",
                                        testCaseSource,
                                        false, testOnRealHardware.attributes,
                                        deploymentArguments,
                                        traits
                                    )));
                            }

                            dataRowIndex++;
                        }
                        #endregion
                    }
                    else
                    {
                        if ((from a in methodAttributes
                             where !(a is SetupProxy) && !(a is CleanupProxy) && !(a is DeploymentConfigurationProxy)
                             select a).Any())
                        {
                            logger?.Invoke(LoggingLevel.Verbose, $"{sourceLocation?.ForMessage() ?? $"{classType.FullName}.{method.Name}"}: Warning: No other attributes are allowed when the attributes that implement '{nameof(ICleanup)}'/'{nameof(IDeploymentConfiguration)}'/'{nameof(ISetup)}' are present. Extra attributes are ignored.");
                        }
                        if ((from a in methodAttributes
                             where a is DeploymentConfigurationProxy
                             select a).Count() > 1)
                        {
                            logger?.Invoke(LoggingLevel.Verbose, $"{sourceLocation?.ForMessage() ?? $"{classType.FullName}.{method.Name}"}: Warning: Only one attribute is allowed that implements '{nameof(IDeploymentConfiguration)}'. The first attribute will be used.");
                        }
                    }
                }
                #endregion
            }

            if (testsOnVirtualDevice.TestCases.Count > 0)
            {
                _testsOnVirtualDevice.Add(testsOnVirtualDevice);
            }
            if (testsOnRealHardware.TestCases.Count > 0)
            {
                _testsOnRealHardware.Add(testsOnRealHardware);
            }
        }
        #endregion
    }
}
