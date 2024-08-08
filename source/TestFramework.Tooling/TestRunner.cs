// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace nanoFramework.TestFramework.Tooling
{
    public sealed class TestRunner
    {
        #region Fields
        private readonly TestFrameworkConfiguration _settings;
        private readonly List<TestResult> _testResults = new List<TestResult>();
        private readonly Dictionary<string, TestCasesForDevice> _runOnVirtualDevice = new Dictionary<string, TestCasesForDevice>();
        private readonly Dictionary<string, TestCasesForDevice> _runOnRealHardware = new Dictionary<string, TestCasesForDevice>();
        #endregion

        #region Entry point
        /// <summary>
        /// Execute the test cases
        /// </summary>
        /// <param name="testCaseSelection">Enumeration of the selected test cases. The display name and fully qualified name of the test case
        /// must match the <see cref="TestCase.DisplayName"/> and <see cref="TestCase.FullyQualifiedName"/> of the previously collected test cases.</param>
        /// <param name="getProjectFilePath">Method that provides the path of the project file that produced the assembly. If <c>null</c>
        /// is passed for this argument or <c>null</c> is returned from the function, the <see cref="TestCase"/>s from that assembly do not provide
        /// the locations of tests in the source code. See also <see cref="ProjectSourceInventory.FindProjectFilePath"/>.</param>
        /// <param name="settings">Configuration for the execution of the tests.</param>
        /// <param name="logger">Method to pass information about the discovery process to the caller.</param>
        /// <returns>The test results. For each test case from <paramref name="testCaseSelection"/> there is at least one test result; there can be more than one
        /// if the test case has been run on multiple devices.</returns>
        public static IReadOnlyList<TestResult> Execute(
            IEnumerable<(string testAssemblyPath, string testCaseDisplayName, string testCaseFullyQualifiedName)> testCaseSelection,
            Func<string, string> getProjectFilePath,
            TestFrameworkConfiguration settings,
            LogMessenger logger)
        {
            var testCases = new TestCaseCollection(testCaseSelection, getProjectFilePath,
                settings.AllowRealHardware,
                out Dictionary<int, int> testCaseForSelection,
                logger);

            var runner = new TestRunner(settings, testCases, testCaseForSelection, testCaseSelection.Count());
            runner.RunTestCasesAsync().GetAwaiter().GetResult();

            return runner._testResults;
        }
        #endregion

        #region Construction
        private TestRunner(TestFrameworkConfiguration settings, TestCaseCollection testCases, Dictionary<int, int> testCaseForSelection, int selectionCount)
        {
            _settings = settings;
            for (int i = 0; i < selectionCount; i++)
            {
                if (!testCaseForSelection.ContainsKey(i))
                {
                    // Selected test case was not found
                    _testResults.Add(new TestResult(i, false));
                }
            }
            foreach (KeyValuePair<int, int> selected in testCaseForSelection)
            {
                TestCase testCase = testCases.TestCases[selected.Value];
                Dictionary<string, TestCasesForDevice> runOnDevice = testCase.ShouldRunOnRealHardware ? _runOnRealHardware : _runOnVirtualDevice;
                if (!runOnDevice.TryGetValue(testCase.AssemblyFilePath, out TestCasesForDevice toRun))
                {
                    runOnDevice[testCase.AssemblyFilePath] = toRun = new TestCasesForDevice(testCase.AssemblyFilePath, testCases.TestMethodsInAssembly(testCase.AssemblyFilePath));
                }
                toRun.TestCases.Add(testCase);
                toRun.SelectionIndexForTestCaseIndex[testCase.TestIndex] = selected.Key;
            }
        }
        #endregion

        #region Helper classes
        private sealed class TestCasesForDevice
        {
            internal TestCasesForDevice(string assemblyFilePath, int testMethodsInAssembly)
            {
                AssemblyFilePath = assemblyFilePath;
                TestMethodsInAssembly = testMethodsInAssembly;
            }

            public string AssemblyFilePath
            {
                get;
            }

            public int TestMethodsInAssembly
            {
                get;
            }

            public List<TestCase> TestCases
            {
                get;
            } = new List<TestCase>();

            public Dictionary<int, int> SelectionIndexForTestCaseIndex
            {
                get;
            } = new Dictionary<int, int>();
        }
        #endregion


        #region Runner implementation
        private async Task RunTestCasesAsync()
        {
            if (_runOnVirtualDevice.Count > 0)
            {
                int virtualDeviceRunners = _settings.MaxVirtualDevices == 0
                    ? Environment.ProcessorCount
                    : _settings.MaxVirtualDevices;
                if (virtualDeviceRunners > _runOnVirtualDevice.Count)
                {
                    virtualDeviceRunners = _runOnVirtualDevice.Count;
                }
            }
        }
        #endregion

    }
}
