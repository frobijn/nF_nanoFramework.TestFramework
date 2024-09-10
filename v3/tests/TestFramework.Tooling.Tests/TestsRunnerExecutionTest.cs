// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using nanoFramework.TestFramework.Tooling;
using TestFramework.Tooling.Tests.Helpers;
using TestResult = nanoFramework.TestFramework.Tooling.TestResult;

namespace TestFramework.Tooling.Tests
{
    /// <summary>
    /// Tests for the functionality of <see cref="TestsRunner"/> involving actually
    /// running the tests: creation of the unit test launcher, working with <see cref="NanoCLRHelper"/>.
    /// The tests use instances of the Virtual Device to actually run the tests.
    /// The real hardware devices are simulated with Virtual Devices as well.
    /// There are no tests for <see cref="TestsRunner"/> involving real hardware; use
    /// the <see cref="Tools.TestAdapter_RunTests_TestCases_Test"/> for that.
    /// </summary>
    [TestClass]
    [TestCategory("Visual Studio/VSTest")]
    [TestCategory("Test execution")]
    public sealed class TestsRunnerExecutionTest
    {
        /// <summary>
        /// Verify that the correct selection of tests is run.  Simulated is test execution
        /// of a single test assembly on a single Virtual Device and three real hardware
        /// devices: two ESP32 and one other. 
        /// </summary>
        [TestMethod]
        public void TestsRunner_RunAllOnVirtualDevice()
        {
            // Setup
            TestsRunnerTester actual = CreateFor(2, 3);

            // Test
            actual.Run();

            // Asserts
            actual.Logger.AssertEqual("", LoggingLevel.Warning);
            actual.AssertAtLeastOneResultPerTestCase();

            // No test should have an outcome None => all tests should have been run
            Assert.IsFalse((from tr in actual.TestResults
                            where tr.Outcome == TestResult.TestOutcome.None
                            select tr).Any());

            // All hardware tests should have been run on exactly one device,
            // except the one based on the xyzzy-deployment configuration
            var devicePerTest = (from tr in actual.TestResults
                                 where tr.TestCase.ShouldRunOnRealHardware
                                 select tr)
                                 .GroupBy(tr => tr.TestCase)
                                 .ToDictionary(
                                    g => g.Key,
                                    g => g.ToList()
                                    );
            foreach (KeyValuePair<TestCase, List<TestResult>> testCase in devicePerTest)
            {
                if (testCase.Key.FullyQualifiedName.StartsWith("TestFramework.Tooling.Execution.Tests.TestWithFrameworkExtensions.TestDeviceWithSomeFile"))
                {
                    // This is the test requiring xyzzy-deployment configuration
                    Assert.AreEqual(3, testCase.Value.Count, testCase.Key.FullyQualifiedName);

                    TestResult testResult = testCase.Value[0];

                    // This test should succeed, otherwise there is a problem with the deployment information
                    Assert.AreEqual(TestResult.TestOutcome.Passed, testResult.Outcome);
                }
                else if (testCase.Key.FullyQualifiedName.StartsWith("TestFramework.Tooling.Execution.Tests.TestWithFrameworkExtensions.TestOnDeviceWithProgrammingError_"))
                {
                    // This is a test that throws an exception when calling ITestOnRealHardware methods
                    Assert.AreEqual(3, testCase.Value.Count, testCase.Key.FullyQualifiedName);

                    TestResult testResult = testCase.Value[0];

                    // This test should succeed, otherwise there is a problem with the deployment information
                    Assert.AreEqual(TestResult.TestOutcome.Skipped, testResult.Outcome);
                }
                else
                {
                    // Other real hardware test
                    Assert.AreEqual(1, testCase.Value.Count, testCase.Key.FullyQualifiedName);

                    TestResult testResult = testCase.Value[0];

                    // This test should succeed, otherwise there may a problem with the deployment information
                    Assert.AreEqual(TestResult.TestOutcome.Passed, testResult.Outcome);
                }
            }
        }

        #region Test setup
        public TestContext TestContext { get; set; }

        private TestsRunnerTester CreateFor(int numProjects, int numRealHardwareDevices)
        {
            if (numRealHardwareDevices < 0 || numRealHardwareDevices > 3)
            {
                throw new ArgumentException();
            }

            var configuration = new TestFrameworkConfiguration();
            configuration.SetDeploymentConfigurationFilePath("COM32", "../deployment_configuration_32.json");
            configuration.SetDeploymentConfigurationFilePath("COM33", "../deployment_configuration_33.json");
            configuration.SetDeploymentConfigurationFilePath("COM42", "../deployment_configuration_42.json");

            TestsRunnerTester actual;
            if (numProjects == 1)
            {
                actual = TestsRunnerTester.Create(TestContext,
                    ("TestFramework.Tooling.Tests.Execution.v3", configuration)
                );
            }
            else if (numProjects == 2)
            {
                actual = TestsRunnerTester.Create(TestContext,
                    ("TestFramework.Tooling.Tests.Execution.v3", configuration),
                    ("TestFramework.Tooling.Tests.Discovery.v2", configuration)
                );
            }
            else
            {
                throw new ArgumentException();
            }
            if (numRealHardwareDevices >= 1)
            {
                actual.AddRealHardwareDevice(new RealHardwareDeviceSimulatedWithVirtualDevice()
                {
                    SerialPort = "COM32",
                    Platform = "ESP32",
                    Target = "SomeTarget"
                })
                .AddDeploymentConfiguration("deployment_configuration_32.json",
                    @"{ ""DisplayName"": ""Device A"", ""Configuration"": { ""xyzzy"": ""Data for device A"", ""RGB LED pin"": 32, ""data.txt"": ""data.txt"", ""data.bin"" : { ""File"": ""data.bin"" } } }"
                )
                .AddDeploymentConfiguration("data.bin", @"This is a binary data file");
            }
            if (numRealHardwareDevices >= 3)
            {
                actual.AddRealHardwareDevice(new RealHardwareDeviceSimulatedWithVirtualDevice()
                {
                    SerialPort = "COM33",
                    Platform = "ESP32",
                    Target = "SomeTarget"
                })
                .AddDeploymentConfiguration("deployment_configuration_33.json",
                    @"{ ""DisplayName"": ""Device B"", ""Configuration"": { ""xyzzy"": ""Data for device B"", ""RGB LED pin"": 33, ""data.txt"": ""data.txt"", ""data.bin"" : { ""File"": ""data.bin"" } } }"
                );
            }
            if (numRealHardwareDevices >= 2)
            {
                actual.AddRealHardwareDevice(new RealHardwareDeviceSimulatedWithVirtualDevice()
                {
                    SerialPort = "COM42",
                    Platform = "other",
                    Target = "SomeTarget"
                })
                .AddDeploymentConfiguration("deployment_configuration_42.json",
                    @"{ ""DisplayName"": ""Device C"", ""Configuration"": { ""xyzzy"": ""Data for device C"", ""RGB LED pin"": 42, ""data.txt"": ""data.txt"", ""data.bin"" : { ""File"": ""data.bin"" } } }"
                );
            }
            return actual;
        }
        #endregion

        #region Mocks for this test
        private sealed class TestsRunnerTester : TestsRunner
        {
            #region Fields
            private readonly Dictionary<string, RealHardwareDeviceSimulatedWithVirtualDevice> _realHardwareDevices = new Dictionary<string, RealHardwareDeviceSimulatedWithVirtualDevice>();
            private readonly List<TestResult> _testResults = new List<TestResult>();
            private readonly string _testDirectoryPath;
            private readonly CancellationTokenSource _cancellationTokenSource;
            #endregion

            #region Construction
            public static TestsRunnerTester Create(TestContext context, params (string projectName, TestFrameworkConfiguration configuration)[] projectNameAndConfiguration)
            {
                #region Copy the assembles and project files
                // ... as the test needs a copy of the project structure to create the unit test launcher and custom test framework configurations.
                string testDirectoryPath = TestDirectoryHelper.GetTestDirectory(context);

                var setupLogger = new LogMessengerMock();
                var testAssemblies = new List<string>();
                foreach ((string projectName, TestFrameworkConfiguration configuration) in projectNameAndConfiguration)
                {
                    string projectDirectoryPath = Path.Combine(testDirectoryPath, projectName);

                    (configuration ?? new TestFrameworkConfiguration()).SaveEffectiveSettings(projectDirectoryPath, setupLogger);

                    List<string> copiedAssemblies = AssemblyHelper.CopyAssembliesAndProjectFile(projectDirectoryPath, "bin", projectName);
                    testAssemblies.Add((from a in copiedAssemblies
                                        where Path.GetFileNameWithoutExtension(a) == projectName || Path.GetFileNameWithoutExtension(a) == "NFUnitTest"
                                        select Path.ChangeExtension(a, ".dll")).First());
                }
                #endregion

                #region Select all available test cases
                var testCases = new TestCaseCollection(testAssemblies, (a) => ProjectSourceInventory.FindProjectFilePath(a, setupLogger), setupLogger);
                var selection = new TestCaseCollection(from tc in testCases.TestCases
                                                       select (tc.AssemblyFilePath, tc.FullyQualifiedName),
                                                       (a) => ProjectSourceInventory.FindProjectFilePath(a, setupLogger),
                                                       setupLogger);
                setupLogger.AssertEqual("", LoggingLevel.Error);
                #endregion

                using (var cancellationToken = new CancellationTokenSource())
                {
                    return new TestsRunnerTester(testDirectoryPath, selection, new LogMessengerMock(), cancellationToken);
                }
            }

            private TestsRunnerTester(string testDirectoryPath, TestCaseCollection selection, LogMessengerMock logger, CancellationTokenSource cancellationTokenSource)
                : base(selection, logger, cancellationTokenSource.Token)
            {
                _cancellationTokenSource = cancellationTokenSource;
                _testDirectoryPath = testDirectoryPath;
                Logger = logger;
                Selection = selection;
            }

            /// <summary>
            /// Add a real hardware device available for testing.
            /// </summary>
            /// <param name="realHardwareDevice"></param>
            /// <returns></returns>
            public TestsRunnerTester AddRealHardwareDevice(RealHardwareDeviceSimulatedWithVirtualDevice realHardwareDevice)
            {
                _realHardwareDevices.Add(realHardwareDevice.SerialPort, realHardwareDevice);
                return this;
            }

            /// <summary>
            /// Add deployment configuration to the project directory
            /// </summary>
            /// <param name="relativeFilePath">Path relative to the project directory</param>
            /// <param name="json">JSON of the file</param>
            /// <returns></returns>
            public TestsRunnerTester AddDeploymentConfiguration(string relativeFilePath, string json)
            {
                File.WriteAllText(Path.Combine(_testDirectoryPath, relativeFilePath), json);
                return this;
            }
            #endregion

            #region Test support
            /// <summary>
            /// Get the selection of test cases
            /// </summary>
            public TestCaseCollection Selection
            {
                get;
            }

            /// <summary>
            /// Logger for messages that are not added to the test results.
            /// </summary>
            public new LogMessengerMock Logger
            {
                get;
            }

            /// <summary>
            /// Not used in this test class
            /// </summary>
            public void Cancel()
            {
                _cancellationTokenSource.Cancel();
            }

            /// <summary>
            /// Get the test results
            /// </summary>
            public IReadOnlyList<TestResult> TestResults
                => _testResults;

            /// <summary>
            /// Assert that there is at least one result per test case.
            /// </summary>
            public void AssertAtLeastOneResultPerTestCase()
            {
                var testCases = new HashSet<TestCase>(Selection.TestCases);
                testCases.ExceptWith(from tr in TestResults
                                     select tr.TestCase);
                if (testCases.Count > 0)
                {
                    Assert.Fail($@"No results for the test cases:\n{string.Join("\n", from t in testCases
                                                                                      orderby t.FullyQualifiedName, t.DisplayName
                                                                                      select $"{t.FullyQualifiedName} {t.DisplayName}")}");
                }
            }
            #endregion

            #region Mock implementation
            protected override void AddTestResults(IEnumerable<TestResult> results, string executedOnDeviceName)
            {
                lock (_testResults)
                {
                    _testResults.AddRange(results);
                }
            }

            protected override async Task DiscoverAllRealHardware(IEnumerable<string> excludeSerialPorts, Action<IRealHardwareDevice> deviceFound)
            {
                await Task.Yield(); // This forces the method to be executed asynchronously
                foreach (KeyValuePair<string, RealHardwareDeviceSimulatedWithVirtualDevice> device in _realHardwareDevices)
                {
                    if (!excludeSerialPorts.Contains(device.Key))
                    {
                        deviceFound(device.Value);
                    }
                }
            }

            protected override async Task DiscoverSelectedRealHardware(IEnumerable<string> serialPorts, Action<IRealHardwareDevice> deviceFound)
            {
                await Task.Yield(); // This forces the method to be executed asynchronously
                foreach (KeyValuePair<string, RealHardwareDeviceSimulatedWithVirtualDevice> device in _realHardwareDevices)
                {
                    if (serialPorts.Contains(device.Key))
                    {
                        deviceFound(device.Value);
                    }
                }
            }
            #endregion
        }

        /// <summary>
        /// Simulate a real hardware device by running the tests on a virtual device.
        /// </summary>
        private sealed class RealHardwareDeviceSimulatedWithVirtualDevice : TestsRunner.IRealHardwareDevice
        {
            public string SerialPort { get; set; }

            public string Target { get; set; }

            public string Platform { get; set; }

            public Task<bool> RunAssembliesAsync(
                IEnumerable<AssemblyMetadata> assemblies,
                LoggingLevel level,
                string reportPrefix,
                Action<string> processOutput,
                LogMessenger logger,
                Func<CancellationToken?> createRunCancellationToken,
                CancellationToken cancellationToken)
            {
                void ProcessOutput(string output)
                {
                    if (output.Contains(reportPrefix))
                    {
                        processOutput(output);
                    }
                    else
                    {
                        processOutput(output);
                    }
                }

                var virtualDevice = new NanoCLRHelper(null, null, false, logger);
                return virtualDevice.RunAssembliesAsync(assemblies, null, level, ProcessOutput, logger, createRunCancellationToken() ?? cancellationToken);
            }
        }
        #endregion
    }
}
