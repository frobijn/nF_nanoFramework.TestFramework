// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace nanoFramework.TestFramework.Tooling
{
    public sealed class TestRunner
    {
        #region Fields
        private readonly TestFrameworkConfiguration _settings;
        private readonly List<TestResult> _testResults;
        private readonly IReadOnlyList<TestCaseSelection> _testCaseSelections;
        private int _testCaseSelectionIndex = -1;
        private int _testCaseRunners;
        private readonly LogMessenger _logger;
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
            IEnumerable<string> testAssemblyPaths,
            Func<string, string> getProjectFilePath,
            TestFrameworkConfiguration settings,
            LogMessenger logger)
        {
            // Get all test cases from the assemblies
            var testCases = new TestCaseCollection(testAssemblyPaths, getProjectFilePath,
                settings.AllowRealHardware,
                logger);
            return Execute(testCases, settings, logger).GetAwaiter().GetResult();
        }

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
            // Get the test cases based on the selection
            var testCases = new TestCaseCollection(testCaseSelection, getProjectFilePath,
                settings.AllowRealHardware,
                logger);
            return Execute(testCases, settings, logger).GetAwaiter().GetResult();
        }

        private static async Task<IReadOnlyList<TestResult>> Execute(
            TestCaseCollection testCases,
            TestFrameworkConfiguration settings,
            LogMessenger logger
        )
        {
            LogMessenger syncLogger = null;
            if (!(logger is null))
            {
                syncLogger = (l, m) =>
                {
                    lock (logger)
                    {
                        logger(l, m);
                    }
                };
            }

            var testResults = new List<TestResult>();

            var tasks = new List<Task>();
            if (testCases.TestOnVirtualDevice.Count > 0)
            {
                tasks.Add(
                    new TestRunner(
                        testResults,
                        testCases.TestOnVirtualDevice,
                        settings,
                        syncLogger
                    ).ExecuteOnVirtualDevice()
                );
            }
            if (testCases.TestOnRealHardware.Count > 0)
            {
                //tasks.Add(
                //    new TestRunner(
                //        testResults,
                //        testCases.TestOnRealHardware,
                //        settings,
                //        syncLogger
                //    ).ExecuteOnRealHardware()
                //);
            }
            if (tasks.Count > 0)
            {
                await Task.WhenAll(tasks);
            }
            return testResults;
        }
        #endregion

        #region Construction and queue
        private TestRunner(
            List<TestResult> testResults,
            IReadOnlyList<TestCaseSelection> testCaseSelections,
            TestFrameworkConfiguration settings,
            LogMessenger logger)
        {
            _testResults = testResults;
            _testCaseSelections = testCaseSelections;
            _settings = settings;
            _logger = logger;
        }

        private async Task RunInParallel(Func<TestCaseSelection, Task> action)
        {
            if (_testCaseRunners > _testCaseSelections.Count)
            {
                _testCaseRunners = _testCaseSelections.Count;
            }
            var queue = new List<Task>();
            for (int i = 0; i < _testCaseRunners; i++)
            {
                queue.Add(RunSingleTask(action));
            }
            await Task.WhenAll(queue);
        }

        private async Task RunSingleTask(Func<TestCaseSelection, Task> action)
        {
            while (true)
            {
                int testCaseSelectionIndex = 0;
                lock (this)
                {
                    testCaseSelectionIndex = ++_testCaseSelectionIndex;
                }
                if (testCaseSelectionIndex >= _testCaseSelections.Count)
                {
                    return;
                }
                await action(_testCaseSelections[testCaseSelectionIndex]);
            }
        }
        #endregion


        #region Execute on Virtual Device
        private async Task ExecuteOnVirtualDevice()
        {
            var nanoClr = new NanoCLRHelper(_settings, _logger);
            if (!nanoClr.NanoClrIsInstalled)
            {
                return;
            }
            _testCaseRunners = _settings.MaxVirtualDevices == 0
                                    ? Environment.ProcessorCount
                                    : _settings.MaxVirtualDevices;
            await RunInParallel(RunTestCaseSelectionOnVirtualDevice);
        }

        private async Task RunTestCaseSelectionOnVirtualDevice(TestCaseSelection selection)
        {

        }
        #endregion

    }
}
