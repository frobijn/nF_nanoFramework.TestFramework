// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using nanoFramework.TestFramework;
using nanoFramework.TestFramework.Tooling;
using nanoFramework.TestFramework.Tools;
using TestFramework.Tooling.Tests.Helpers;
using TestResult = nanoFramework.TestFramework.Tooling.TestResult;

namespace TestFramework.Tooling.Tests
{
    /// <summary>
    /// Tests for the orchestration functionality of <see cref="TestsRunner"/>:
    /// which tests are selected to run on which device, parallel execution of tests,
    /// timeouts, cancel. The tests use mocks for devices that do not run any tests, the
    /// outputs of the devices and hence test results are test data. These unit tests still
    /// can be used to verify the correct output processing and completeness of test results.
    /// </summary>
    [TestClass]
    [TestCategory("Visual Studio/VSTest")]
    [TestCategory("Test execution")]
    public sealed class TestsRunnerOrchestrationTest
    {
        /// <summary>
        /// Verifies that the tests are run and the test results are correct. Simulated is test execution
        /// of a single test assembly on a single Virtual Device and three real hardware
        /// devices: two esp32 and one other. This test cannot verify whether the selection of tests
        /// on each device is correct; that is done in <see cref="TestsRunnerExecutionTest.TestsRunner_TestSelection"/>.
        /// </summary>
        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void TestsRunner_TestsAreRun(bool withDeviceInitializationLogging)
        {
            #region Setup and "run" tests
            TestsRunnerTester actual = CreateFor(2, 3);

            string id = Guid.NewGuid().ToString("N");
            if (withDeviceInitializationLogging)
            {
                foreach (DeviceMock device in actual.Devices)
                {
                    device.InitializationLog = (l) => l(LoggingLevel.Warning, id);
                }
            }

            actual.Run();
            #endregion

            #region Assert the test results
            actual.Logger.AssertEqual("");
            actual.AssertAtLeastOneResultPerTestCase();

            // No test should have an outcome None => all tests should have been run (or skipped)
            // Except on one test, as the simulated output does not contain the result of that test.
            Assert.IsFalse((from tr in actual.TestResults
                            where tr.Outcome == TestResult.TestOutcome.None
                                && !(
                                    tr.TestCase.ShouldRunOnRealHardware
                                    && tr.TestCase.FullyQualifiedName.StartsWith("TestFramework.Tooling.Execution.Tests.TestWithNewTestMethodsAttributes.MethodToRunOnRealHardware")
                                    )
                            select tr).Any());
            Assert.IsTrue((from tr in actual.TestResults
                           where tr.Outcome != TestResult.TestOutcome.None
                               && (
                                   tr.TestCase.ShouldRunOnRealHardware
                                   && tr.TestCase.FullyQualifiedName.StartsWith("TestFramework.Tooling.Execution.Tests.TestWithNewTestMethodsAttributes.MethodToRunOnRealHardware")
                                   )
                           select tr).Any());

            if (withDeviceInitializationLogging)
            {
                // All tests should have the device initialization message,
                // except the hardware ones that are not run and the ones that are skipped because of
                // exceptions when evaluating the ITestOnRealHardware methods
                Assert.IsFalse((from tr in actual.TestResults
                                where !(
                                        tr.TestCase.ShouldRunOnRealHardware
                                        && (tr.TestCase.FullyQualifiedName.StartsWith("TestFramework.Tooling.Execution.Tests.TestWithNewTestMethodsAttributes.MethodToRunOnRealHardware")
                                            ||
                                            tr.TestCase.FullyQualifiedName.StartsWith("TestFramework.Tooling.Execution.Tests.TestWithFrameworkExtensions.TestOnDeviceWithProgrammingError_")
                                            )
                                       )
                                      && !(from m in tr.Messages
                                           where m.Contains(id)
                                           select m).Any()
                                select tr).Any());
            }

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

                    // Not all deployment information was present; check that a message about that is present
                    if (!(from m in testResult.Messages
                          where m.Contains("data.bin")
                          select m).Any())
                    {
                        Assert.Fail($"Expected message about missing 'data.bin', but instead:\n{string.Join("\n", testResult.Messages)}");
                    }
                }
                else if (testCase.Key.FullyQualifiedName.StartsWith("TestFramework.Tooling.Execution.Tests.TestWithFrameworkExtensions.TestOnDeviceWithProgrammingError_"))
                {
                    // Exceptions occur when evaluating the method of the ITestOnRealHardware attribute.
                    Assert.AreEqual(3, testCase.Value.Count, testCase.Key.FullyQualifiedName);

                    TestResult testResult = testCase.Value[0];

                    if (testCase.Key.FullyQualifiedName.StartsWith("TestFramework.Tooling.Execution.Tests.TestWithFrameworkExtensions.TestOnDeviceWithProgrammingError_ShouldTestOnDevice"))
                    {
                        // This test should be skipped and an error message should be present
                        Assert.AreEqual(TestResult.TestOutcome.Skipped, testResult.Outcome);
                        Assert.IsTrue((from m in testResult.Messages
                                       where m.Contains(nameof(ITestOnRealHardware.ShouldTestOnDevice))
                                       select m).Any());
                        Assert.IsFalse((from m in testResult.Messages
                                        where m.Contains(nameof(ITestOnRealHardware.AreDevicesEqual))
                                        select m).Any());
                    }
                    else if (testCase.Key.FullyQualifiedName.StartsWith("TestFramework.Tooling.Execution.Tests.TestWithFrameworkExtensions.TestOnDeviceWithProgrammingError_AreDevicesEqual"))
                    {
                        // This test should be skipped and an error message should be present
                        Assert.AreEqual(TestResult.TestOutcome.Skipped, testResult.Outcome);
                        Assert.IsFalse((from m in testResult.Messages
                                        where m.Contains(nameof(ITestOnRealHardware.ShouldTestOnDevice))
                                        select m).Any());
                        Assert.IsTrue((from m in testResult.Messages
                                       where m.Contains(nameof(ITestOnRealHardware.AreDevicesEqual))
                                       select m).Any());
                    }
                }
                else
                {
                    // Other real hardware test
                    Assert.AreEqual(1, testCase.Value.Count, testCase.Key.FullyQualifiedName);

                    TestResult testResult = testCase.Value[0];

                    // The missing deployment information for xyzzy should not be reported here
                    Assert.IsFalse((from m in testResult.Messages
                                    where m.Contains("data.bin")
                                    select m).Any(), testCase.Key.FullyQualifiedName);
                }
            }
            #endregion
        }

        /// <summary>
        /// Verify that virtual devices and real hardware devices are run in parallel.
        /// This test is inconclusive if there's only 1 logical processor available to run this test.
        /// </summary>
        [TestMethod]
        [TestCategory("@Multiple logical processors")]
        public void TestsRunner_RunInParallel()
        {
            if (Environment.ProcessorCount == 1)
            {
                Assert.Inconclusive($"Only 1 logical processor available");
            }

            TestsRunnerTester actual = CreateFor(2, 3);

            // If the devices are not run in parallel, the next line will never return.
            (Task testRunner, CancellationTokenSource paused) = actual.RunAndWaitUntilAllDevicesAreRunning();

            paused.Cancel(); // To end waiting async tasks
            testRunner.Wait();
            paused?.Dispose();
        }

        /// <summary>
        /// Verify that <see cref="TestFrameworkConfiguration.AllowRealHardware"/> and
        /// <see cref="TestFrameworkConfiguration.ExcludeSerialPorts"/> are honoured.
        /// </summary>
        [TestMethod]
        public void TestsRunner_AllowRealHardware_AllowSerialPorts_ExcludeSerialPorts()
        {
            #region AllowRealHardware = false
            TestsRunnerTester actual = CreateFor(1, 3, (configuration) =>
                {
                    configuration.AllowRealHardware = false;
                });
            actual.Run();
            Assert.AreEqual("", string.Join(", ", from rh in actual.RealHardwareDevices
                                                  where rh.IsSelectedToRun
                                                  orderby rh.SerialPort
                                                  select rh.SerialPort));
            #endregion

            #region AllowRealHardware = true with ExcludeSerialPorts
            actual = CreateFor(1, 3, (configuration) =>
                {
                    configuration.AllowRealHardware = true;
                    configuration.ExcludeSerialPorts.Add("COM33");
                });
            actual.Run();
            Assert.AreEqual("COM32, COM42", string.Join(", ", from rh in actual.RealHardwareDevices
                                                              where rh.IsSelectedToRun
                                                              orderby rh.SerialPort
                                                              select rh.SerialPort));
            #endregion

            #region AllowRealHardware = false with AllowSerialPorts
            actual = CreateFor(1, 3, (configuration) =>
            {
                configuration.AllowRealHardware = false;
                configuration.AllowSerialPorts.Add("COM32");
                configuration.AllowSerialPorts.Add("COM42");
            });
            actual.Run();
            Assert.AreEqual("", string.Join(", ", from rh in actual.RealHardwareDevices
                                                  where rh.IsSelectedToRun
                                                  orderby rh.SerialPort
                                                  select rh.SerialPort));
            #endregion

            #region AllowRealHardware = true with AllowSerialPorts and ExcludeSerialPorts
            actual = CreateFor(1, 3, (configuration) =>
            {
                configuration.AllowRealHardware = true;
                configuration.ExcludeSerialPorts.Add("COM42");
                configuration.AllowSerialPorts.Add("COM32");
                configuration.AllowSerialPorts.Add("COM42");
            });
            actual.Run();
            Assert.AreEqual("COM32, COM42", string.Join(", ", from rh in actual.RealHardwareDevices
                                                              where rh.IsSelectedToRun
                                                              orderby rh.SerialPort
                                                              select rh.SerialPort));
            #endregion
        }

        /// <summary>
        /// Verify that <see cref="TestFrameworkConfiguration.MaxVirtualDevices"/> is honoured.
        /// This test is inconclusive if there's only 1 logical processor available to run this test.
        /// </summary>
        [TestMethod]
        [TestCategory("@Multiple logical processors")]
        public void TestsRunner_MaxVirtualDevices()
        {
            // Setup
            if (Environment.ProcessorCount == 1)
            {
                Assert.Inconclusive($"Only 1 logical processor available");
            }

            TestsRunnerTester actual = CreateFor(2, 0,
                (configuration) =>
                {
                    configuration.MaxVirtualDevices = 1;
                }
            );

            // Run the tests
            actual.Run();

            // Assert
            Assert.AreEqual(1, actual.MaxVirtualDevices);
        }

        /// <summary>
        /// Verify that <see cref="TestFrameworkConfiguration.RealHardwareTimeout"/> and
        /// <see cref="TestFrameworkConfiguration.VirtualDeviceTimeout"/> are honoured.
        /// </summary>
        [TestMethod]
        public void TestsRunner_Timeouts()
        {
            // Setup
            TestsRunnerTester actual = CreateFor(1, 1, (configuration) =>
            {
                configuration.RealHardwareTimeout = 100;
                configuration.VirtualDeviceTimeout = 100;
            });
            foreach (DeviceMock device in actual.Devices)
            {
                // This will prevent sending output before the timeout occurs
                device.TriggerTimeout = true;
            }

            actual.Run();

            // No test should have been run
            Assert.IsFalse((from tr in actual.TestResults
                            where tr.Outcome != TestResult.TestOutcome.Skipped
                            select tr).Any());

            // Quick check for completeness of the test results:

            // ... all test cases should have a result
            actual.AssertAtLeastOneResultPerTestCase();

            // ... there should be at least one test per device.
            foreach (RealHardwareDeviceMock device in actual.RealHardwareDevices)
            {
                Assert.IsTrue((from tr in actual.TestResults
                               where tr.SerialPort == device.SerialPort
                               select tr).Any());
            }
            Assert.IsTrue((from tr in actual.TestResults
                           where tr.SerialPort is null
                           select tr).Any());
        }

        /// <summary>
        /// Verify that the running of tests can be cancelled.
        /// </summary>
        [TestMethod]
        public void TestsRunner_Cancel()
        {
            // Setup
            TestsRunnerTester actual = CreateFor(1, 1);

            // Start running the tests
            (Task testRunner, CancellationTokenSource pauseRunning) = actual.RunAndWaitUntilAllDevicesAreRunning();

            // Cancel running the tests
            actual.Cancel();

            // Let the devices continue to end this test
            pauseRunning.Cancel();
            testRunner.Wait();
            pauseRunning.Dispose();

            // No test should have been run
            Assert.IsFalse((from tr in actual.TestResults
                            where tr.Outcome != TestResult.TestOutcome.Skipped
                            select tr).Any());

            // Quick check for completeness of the test results:

            // ... all test cases should have a result
            actual.AssertAtLeastOneResultPerTestCase();

            // ... there should be at least one test per device.
            foreach (RealHardwareDeviceMock device in actual.RealHardwareDevices)
            {
                Assert.IsTrue((from tr in actual.TestResults
                               where tr.SerialPort == device.SerialPort
                               select tr).Any());
            }
            Assert.IsTrue((from tr in actual.TestResults
                           where tr.SerialPort is null
                           select tr).Any());
        }



        /// <summary>
        /// Verify that in case of detailed logging, a test result for a test case is available
        /// for each real hardware device.
        /// </summary>
        [TestMethod]
        public void TestsRunner_DetailedLogging()
        {
            // Setup
            TestsRunnerTester actual = CreateFor(1, 3, (configuration) =>
            {
                configuration.Logging = LoggingLevel.Detailed;
            });

            // Run the tests
            actual.Run();

            #region Assert the test results
            actual.Logger.AssertEqual("", LoggingLevel.Warning);
            actual.AssertAtLeastOneResultPerTestCase();

            // All hardware tests should have one result per device,
            // even if the test is not run on that device.
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
                Assert.AreEqual(3, testCase.Value.Count, testCase.Key.FullyQualifiedName);
            }
            #endregion
        }

        #region Helpers

        public TestContext TestContext { get; set; }

        #region Test setup
        private TestsRunnerTester CreateFor(int numProjects, int numRealHardwareDevices, Action<TestFrameworkConfiguration> modifyConfiguration = null)
        {
            if (numRealHardwareDevices < 0 || numRealHardwareDevices > 3)
            {
                throw new ArgumentException();
            }

            var configuration = new TestFrameworkConfiguration();
            configuration.SetDeploymentConfigurationFilePath("COM32", "../deployment_configuration_32.json");
            configuration.SetDeploymentConfigurationFilePath("COM33", "../deployment_configuration_33.json");
            configuration.SetDeploymentConfigurationFilePath("COM42", "../deployment_configuration_42.json");
            modifyConfiguration?.Invoke(configuration);

            TestsRunnerTester actual;
            if (numProjects == 1)
            {
                actual = TestsRunnerTester.Create(TestContext,
                    ("TestFramework.Tooling.Tests.Execution.v3", configuration)
                )
                .AddVirtualDevice(new VirtualDeviceMock()
                {
                    GetOutput = (a) => TestFrameworkToolingTestsExecution_v3.OutputFromVirtualDevice
                                        + Output_AllTestsDone
                });

            }
            else if (numProjects == 2)
            {
                actual = TestsRunnerTester.Create(TestContext,
                    ("TestFramework.Tooling.Tests.Execution.v3", configuration),
                    ("TestFramework.Tooling.Tests.Discovery.v2", configuration)
                )
                .AddVirtualDevice(new VirtualDeviceMock()
                {
                    GetOutput = (a) => a == "TestFramework.Tooling.Tests.Execution.v3"
                                    ? TestFrameworkToolingTestsExecution_v3.OutputFromVirtualDevice
                                        + Output_AllTestsDone
                                    : TestFrameworkToolingTestsDiscovery_v2.Output
                                        + Output_AllTestsDone
                })
                .AddVirtualDevice(new VirtualDeviceMock()
                {
                    GetOutput = (a) => a == "TestFramework.Tooling.Tests.Execution.v3"
                                    ? TestFrameworkToolingTestsExecution_v3.OutputFromVirtualDevice
                                        + Output_AllTestsDone
                                    : TestFrameworkToolingTestsDiscovery_v2.Output
                                        + Output_AllTestsDone
                });
            }
            else
            {
                throw new ArgumentException();
            }
            if (numRealHardwareDevices >= 1)
            {
                actual.AddRealHardwareDevice(new RealHardwareDeviceMock()
                {
                    SerialPort = "COM32",
                    GetOutput = (a) =>
                            a == "TestFramework.Tooling.Tests.Execution.v3" ?
                                TestFrameworkToolingTestsExecution_v3.Output_Device_xyzzy
                                + TestFrameworkToolingTestsExecution_v3.Output_Device_esp32
                                + Output_AllTestsDone
                            : TestFrameworkToolingTestsDiscovery_v2.Output
                                + Output_AllTestsDone,
                    Platform = "ESP32",
                    Target = "SomeTarget"
                })
                .AddDeploymentConfiguration("deployment_configuration_32.json",
                    @"{ ""DisplayName"": ""Device A"", ""Configuration"": { ""xyzzy"": ""Data for device A"", ""RGB LED pin"": 32 } }"
                );
            }
            if (numRealHardwareDevices >= 3)
            {
                actual.AddRealHardwareDevice(new RealHardwareDeviceMock()
                {
                    SerialPort = "COM33",
                    GetOutput = (a) =>
                            a == "TestFramework.Tooling.Tests.Execution.v3" ?
                                TestFrameworkToolingTestsExecution_v3.Output_Device_xyzzy
                                + TestFrameworkToolingTestsExecution_v3.Output_Device_esp32
                                + Output_AllTestsDone
                            : TestFrameworkToolingTestsDiscovery_v2.Output
                                + Output_AllTestsDone,
                    Platform = "ESP32",
                    Target = "SomeTarget"
                })
                .AddDeploymentConfiguration("deployment_configuration_33.json",
                    @"{ ""DisplayName"": ""Device B"", ""Configuration"": { ""xyzzy"": ""Data for device B"", ""RGB LED pin"": 33 } }"
                );
            }
            if (numRealHardwareDevices >= 2)
            {
                actual.AddRealHardwareDevice(new RealHardwareDeviceMock()
                {
                    SerialPort = "COM42",
                    GetOutput = (a) =>
                            a == "TestFramework.Tooling.Tests.Execution.v3" ?
                                TestFrameworkToolingTestsExecution_v3.Output_Device_xyzzy
                                + Output_AllTestsDone
                            : TestFrameworkToolingTestsDiscovery_v2.Output
                                + Output_AllTestsDone,
                    Platform = "other",
                    Target = "SomeTarget"
                })
                .AddDeploymentConfiguration("deployment_configuration_42.json",
                    @"{ ""DisplayName"": ""Device C"", ""Configuration"": { ""xyzzy"": ""Data for device C"", ""RGB LED pin"": 42 } }"
                );
            }
            return actual;
        }
        #endregion

        #region Mocks for this test class
        private sealed class TestsRunnerTester : TestsRunner
        {
            #region Fields
            private readonly Dictionary<string, RealHardwareDeviceMock> _realHardwareDevices = new Dictionary<string, RealHardwareDeviceMock>();
            private readonly List<VirtualDeviceMock> _virtualDevices = new List<VirtualDeviceMock>();
            private int _nextVirtualDevice = 0;
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

                return new TestsRunnerTester(testDirectoryPath, selection, new LogMessengerMock(), new CancellationTokenSource());
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
            public TestsRunnerTester AddRealHardwareDevice(RealHardwareDeviceMock realHardwareDevice)
            {
                _realHardwareDevices.Add(realHardwareDevice.SerialPort, realHardwareDevice);
                return this;
            }

            /// <summary>
            /// Add a virtual device instance. Add as many as there are test assemblies
            /// that have to be run on a virtual device.
            /// </summary>
            /// <param name="virtualDevice"></param>
            /// <returns></returns>
            public TestsRunnerTester AddVirtualDevice(VirtualDeviceMock virtualDevice)
            {
                _virtualDevices.Add(virtualDevice);
                virtualDevice.InstanceNumber = _virtualDevices.Count;
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

            public void Cancel()
            {
                _cancellationTokenSource.Cancel();
            }

            public new int MaxVirtualDevices
                => base.MaxVirtualDevices;


            public int NumberOfDevicesRunning
            {
                get; set;
            }

            public int MaximumNumberOfDevicesRunningSimultaneously
            {
                get; set;
            }

            public (Task run, CancellationTokenSource pauseRunning) RunAndWaitUntilAllDevicesAreRunning()
            {
                var pauseRunning = new CancellationTokenSource();
                foreach (DeviceMock device in Devices)
                {
                    device.WaitToSendOutput = pauseRunning.Token;
                }

                var run = Task.Run(() => Run());

                for (bool allRunning = false; !allRunning;)
                {
                    Task.Delay(100).GetAwaiter().GetResult();
                    lock (this)
                    {
                        allRunning = NumberOfDevicesRunning == _realHardwareDevices.Count + _virtualDevices.Count;
                    }
                }
                return (run, pauseRunning);
            }

            /// <summary>
            /// Logger for messages that are not added to the test results.
            /// </summary>
            public new LogMessengerMock Logger
            {
                get;
            }

            /// <summary>
            /// Get all devices
            /// </summary>
            public IEnumerable<DeviceMock> Devices
            {
                get
                {
                    foreach (RealHardwareDeviceMock rh in RealHardwareDevices)
                    {
                        yield return rh;
                    }
                    foreach (VirtualDeviceMock vd in _virtualDevices)
                    {
                        yield return vd;
                    }
                }

            }

            /// <summary>
            /// Get the real hardware devices
            /// </summary>
            public IEnumerable<RealHardwareDeviceMock> RealHardwareDevices
                => from rh in _realHardwareDevices
                   orderby rh.Key
                   select rh.Value;

            /// <summary>
            /// Get the virtual devices
            /// </summary>
            public IReadOnlyList<VirtualDeviceMock> VirtualDevices
                => _virtualDevices;

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
                foreach (KeyValuePair<string, RealHardwareDeviceMock> device in _realHardwareDevices)
                {
                    if (!excludeSerialPorts.Contains(device.Key))
                    {
                        device.Value.Tester = this;
                        deviceFound(device.Value);
                    }
                }
            }

            protected override async Task DiscoverSelectedRealHardware(IEnumerable<string> serialPorts, Action<IRealHardwareDevice> deviceFound)
            {
                await Task.Yield(); // This forces the method to be executed asynchronously
                foreach (KeyValuePair<string, RealHardwareDeviceMock> device in _realHardwareDevices)
                {
                    if (serialPorts.Contains(device.Key))
                    {
                        device.Value.Tester = this;
                        deviceFound(device.Value);
                    }
                }
            }

            protected override IVirtualDevice CreateVirtualDevice(TestFrameworkConfiguration configuration, LogMessenger logger)
            {
                if (_nextVirtualDevice >= _virtualDevices.Count)
                {
                    logger(LoggingLevel.Error, $"Cannot create Virtual Device");
                    return null;
                }
                VirtualDeviceMock device = _virtualDevices[_nextVirtualDevice++];
                device.Configuration = configuration;
                device.Tester = this;
                device.InitializationLog?.Invoke(logger);
                return device;
            }
            #endregion
        }

        private abstract class DeviceMock
        {
            #region Test support
            /// <summary>
            /// Get the tester the device is used for
            /// </summary>
            internal TestsRunnerTester Tester
            {
                get; set;
            }

            /// <summary>
            /// Indicates whether the device was selected to run tests on.
            /// </summary>
            public bool IsSelectedToRun
            {
                get; set;
            }

            /// <summary>
            /// Indicates whether the device is currently running tests.
            /// </summary>
            public bool IsRunning
            {
                get; set;
            }

            /// <summary>
            /// Method called in the initialization of the device to log something about the device initialization.
            /// </summary>
            public Action<LogMessenger> InitializationLog
            {
                get; set;
            }

            /// <summary>
            /// Get the output to return to the tests runner. Required to generate test results.
            /// Use "@@@" in lieu of the report prefix. The parameter is the name of the test assembly.
            /// </summary>
            public Func<string, string> GetOutput
            {
                get; set;
            }

            /// <summary>
            /// Indicate whether to simulate an error in the initialization of the device
            /// </summary>
            public bool SimulateDeviceInitializationError
            {
                get; set;
            }

            /// <summary>
            /// Indicates whether the execution of the tests should be prolonged so that a
            /// timeout is triggered. No output will be sent.
            /// </summary>
            public bool TriggerTimeout
            {
                get; set;
            }

            /// <summary>
            /// When set, the device will not send any output before the token is cancelled.
            /// </summary>
            public CancellationToken? WaitToSendOutput
            {
                get; set;
            }
            #endregion

            #region Mock implementation
            public abstract string DeviceName
            {
                get;
            }

            protected async Task<bool> RunAssembliesAsync(
                IEnumerable<AssemblyMetadata> assemblies,
                string reportPrefix, Action<string> processOutput,
                LogMessenger logger,
                CancellationToken cancellationToken)
            {
                await Task.Yield(); // This forces the method to be executed asynchronously

                lock (this)
                {
                    IsSelectedToRun = true;
                    IsRunning = true;
                }
                lock (Tester)
                {
                    Tester.NumberOfDevicesRunning++;
                    if (Tester.NumberOfDevicesRunning > Tester.MaximumNumberOfDevicesRunningSimultaneously)
                    {
                        Tester.MaximumNumberOfDevicesRunningSimultaneously = Tester.NumberOfDevicesRunning;
                    }
                }

                if (SimulateDeviceInitializationError)
                {
                    logger(LoggingLevel.Error, $"Mock: Initialization failed of {DeviceName}");
                    return false;
                }

                WaitToSendOutput?.WaitHandle.WaitOne();

                if (TriggerTimeout)
                {
                    cancellationToken.WaitHandle.WaitOne();
                }
                else if (!(GetOutput is null))
                {
                    string testAssembly = (from a in assemblies
                                           where Path.GetFileName(a.AssemblyFilePath).StartsWith("TestFramework.Tooling.Tests.")
                                               || Path.GetFileName(a.AssemblyFilePath).StartsWith("NFUnitTest")
                                           select Path.GetFileNameWithoutExtension(a.AssemblyFilePath)).First();
                    string output = GetOutput(testAssembly);
                    processOutput(output.Replace("@@@", reportPrefix));
                }

                lock (this)
                {
                    IsRunning = false;
                }
                lock (Tester)
                {
                    Tester.NumberOfDevicesRunning--;
                }
                return true;
            }
            #endregion
        }

        /// <summary>
        /// Mock to simulate running tests on a real hardware device
        /// </summary>
        private sealed class RealHardwareDeviceMock : DeviceMock, TestsRunner.IRealHardwareDevice
        {
            #region Test support


            public string SerialPort
            {
                get; set;
            }

            public string Target
            {
                get; set;
            }

            public string Platform
            {
                get; set;
            }
            #endregion

            #region Mock implementation
            public override string DeviceName
                => $"{Constants.RealHardware_Description} connected to {SerialPort}";


            async Task<bool> TestsRunner.IRealHardwareDevice.RunAssembliesAsync(
                IEnumerable<AssemblyMetadata> assemblies,
                LoggingLevel logging,
                string reportPrefix, Action<string> processOutput,
                LogMessenger logger,
                Func<CancellationToken?> createRunCancellationToken,
                CancellationToken cancellationToken)
            {
                InitializationLog?.Invoke(logger);
                return await RunAssembliesAsync(assemblies, reportPrefix, processOutput, logger, createRunCancellationToken() ?? cancellationToken);
            }
            #endregion
        }

        /// <summary>
        /// Mock to simulate running tests on a virtual device
        /// </summary>
        private sealed class VirtualDeviceMock : DeviceMock, TestsRunner.IVirtualDevice
        {
            #region Mock implementation
            /// <summary>
            /// Configuration; assigned in <see cref="TestsRunnerTester.CreateVirtualDevice"/>.
            /// </summary>
            public TestFrameworkConfiguration Configuration
            {
                get; set;
            }

            /// <summary>
            /// Sequential number to label the Virtual Device; assigned in <see cref="TestsRunnerTester.CreateVirtualDevice"/>.
            /// </summary>
            public int InstanceNumber
            {
                get; set;
            }

            public override string DeviceName
                => $"Virtual Device instance #{InstanceNumber}";

            async Task<bool> TestsRunner.IVirtualDevice.RunAssembliesAsync(
                IEnumerable<AssemblyMetadata> assemblies,
                string localCLRInstanceFilePath,
                LoggingLevel logging,
                string reportPrefix, Action<string> processOutput,
                LogMessenger logger,
                CancellationToken cancellationToken)
            {
                await Task.Yield(); // This forces the method to be executed asynchronously

                if (Configuration.PathToLocalCLRInstance != localCLRInstanceFilePath)
                {
                    logger(LoggingLevel.Error, $"Mock: Configuration.PathToLocalCLRInstance '{Configuration.PathToLocalCLRInstance}' != localCLRInstanceFilePath '{localCLRInstanceFilePath}'");
                }
                if (Configuration.Logging != logging)
                {
                    logger(LoggingLevel.Error, $"Mock: Configuration.Logging (${Configuration.Logging}) != logging ({logging})");
                }

                return await RunAssembliesAsync(assemblies, reportPrefix, processOutput, logger, cancellationToken);
            }
            #endregion
        }
        #endregion

        #region Mocks for test assemblies
        private static class TestFrameworkToolingTestsDiscovery_v2
        {
            /// <summary>
            /// The output required to reproduce the outcome of the test cases in the "TestFramework.Tooling.Tests.Discovery.v2"
            /// test project. The details of the test results (messages) are different.
            /// </summary>
            public static readonly string Output =
$@"@@@:C:TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes:0:{UnitTestLauncher.Communication.Start}
@@@:M:TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod:0:{UnitTestLauncher.Communication.Start}
@@@:M:TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod:0:{UnitTestLauncher.Communication.Pass}
@@@:D:TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod1#0:0:{UnitTestLauncher.Communication.Start}
@@@:D:TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod1#0:0:{UnitTestLauncher.Communication.Pass}
@@@:D:TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod1#1:0:{UnitTestLauncher.Communication.Start}
@@@:D:TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod1#1:0:{UnitTestLauncher.Communication.Pass}
@@@:C:TestFramework.Tooling.Tests.NFUnitTest.TestWithMethods:0:{UnitTestLauncher.Communication.Start}
@@@:M:TestFramework.Tooling.Tests.NFUnitTest.TestWithMethods.Test:0:{UnitTestLauncher.Communication.Start}
@@@:M:TestFramework.Tooling.Tests.NFUnitTest.TestWithMethods.Test:0:{UnitTestLauncher.Communication.Pass}
@@@:M:TestFramework.Tooling.Tests.NFUnitTest.TestWithMethods.Test2:0:{UnitTestLauncher.Communication.Start}
@@@:M:TestFramework.Tooling.Tests.NFUnitTest.TestWithMethods.Test2:0:{UnitTestLauncher.Communication.Pass}
@@@:C:TestFramework.Tooling.Tests.NFUnitTest.TestWithMethods:0:{UnitTestLauncher.Communication.Done}
";
        }

        private static class TestFrameworkToolingTestsExecution_v3
        {
            /// <summary>
            /// The output required to reproduce the outcome of the test cases in the "TestFramework.Tooling.Tests.Execution.v3"
            /// test project as executed on a virtual device. The details of the test results (messages) are different.
            /// </summary>
            public static readonly string OutputFromVirtualDevice =
$@"@@@:C:TestFramework.Tooling.Execution.Tests.FailInConstructor:0:{UnitTestLauncher.Communication.Start}
@@@:C:TestFramework.Tooling.Execution.Tests.FailInConstructor:0:{UnitTestLauncher.Communication.Setup}
@@@:C:TestFramework.Tooling.Execution.Tests.FailInConstructor:0:{UnitTestLauncher.Communication.SetupFail}:Exception
@@@:C:TestFramework.Tooling.Execution.Tests.FailInSetup:0:{UnitTestLauncher.Communication.Start}
@@@:C:TestFramework.Tooling.Execution.Tests.FailInSetup:0:{UnitTestLauncher.Communication.Setup}
@@@:C:TestFramework.Tooling.Execution.Tests.FailInSetup:0:{UnitTestLauncher.Communication.SetupFail}:Exception
@@@:C:TestFramework.Tooling.Execution.Tests.FailInTest:0:{UnitTestLauncher.Communication.Start}
@@@:M:TestFramework.Tooling.Execution.Tests.FailInTest.Test:0:{UnitTestLauncher.Communication.Start}
@@@:M:TestFramework.Tooling.Execution.Tests.FailInTest.Test:0:{UnitTestLauncher.Communication.Fail}:Exception
@@@:C:TestFramework.Tooling.Execution.Tests.SkippedInTest:0:{UnitTestLauncher.Communication.Start}
@@@:M:TestFramework.Tooling.Execution.Tests.SkippedInTest.Test:0:{UnitTestLauncher.Communication.Start}
@@@:M:TestFramework.Tooling.Execution.Tests.SkippedInTest.Test:0:{UnitTestLauncher.Communication.Skipped}:Skipped
@@@:C:TestFramework.Tooling.Execution.Tests.CleanupFailedInTest:0:{UnitTestLauncher.Communication.Start}
@@@:M:TestFramework.Tooling.Execution.Tests.CleanupFailedInTest.Test:0:{UnitTestLauncher.Communication.Start}
@@@:M:TestFramework.Tooling.Execution.Tests.CleanupFailedInTest.Test:0:{UnitTestLauncher.Communication.Pass}
@@@:C:TestFramework.Tooling.Execution.Tests.CleanupFailedInTest:0:{UnitTestLauncher.Communication.CleanupFail}:Exception
@@@:C:TestFramework.Tooling.Execution.Tests.FailInCleanUp:0:{UnitTestLauncher.Communication.Start}
@@@:M:TestFramework.Tooling.Execution.Tests.FailInCleanUp.Test:0:{UnitTestLauncher.Communication.Start}
@@@:M:TestFramework.Tooling.Execution.Tests.FailInCleanUp.Test:0:{UnitTestLauncher.Communication.Pass}
@@@:C:TestFramework.Tooling.Execution.Tests.FailInCleanUp:0:{UnitTestLauncher.Communication.Cleanup}
@@@:C:TestFramework.Tooling.Execution.Tests.FailInCleanUp:0:{UnitTestLauncher.Communication.CleanupFail}:Exception
@@@:C:TestFramework.Tooling.Execution.Tests.FailInDispose:0:{UnitTestLauncher.Communication.Start}
@@@:M:TestFramework.Tooling.Execution.Tests.FailInDispose.Test:0:{UnitTestLauncher.Communication.Start}
@@@:M:TestFramework.Tooling.Execution.Tests.FailInDispose.Test:0:{UnitTestLauncher.Communication.Pass}
@@@:C:TestFramework.Tooling.Execution.Tests.FailInDispose:0:{UnitTestLauncher.Communication.Cleanup}
@@@:C:TestFramework.Tooling.Execution.Tests.FailInDispose:0:{UnitTestLauncher.Communication.CleanupFail}:Exception
@@@:C:TestFramework.Tooling.Execution.Tests.NonFailingTest:0:{UnitTestLauncher.Communication.Start}
@@@:M:TestFramework.Tooling.Execution.Tests.NonFailingTest.Test:0:{UnitTestLauncher.Communication.Start}
@@@:M:TestFramework.Tooling.Execution.Tests.NonFailingTest.Test:0:{UnitTestLauncher.Communication.Pass}
@@@:C:TestFramework.Tooling.Execution.Tests.SkippedInConstructor:0:{UnitTestLauncher.Communication.Start}
@@@:C:TestFramework.Tooling.Execution.Tests.SkippedInConstructor:0:{UnitTestLauncher.Communication.Setup}
@@@:C:TestFramework.Tooling.Execution.Tests.SkippedInConstructor:0:{UnitTestLauncher.Communication.Skipped}:Skipped
@@@:C:TestFramework.Tooling.Execution.Tests.SkippedInSetup:0:{UnitTestLauncher.Communication.Start}
@@@:C:TestFramework.Tooling.Execution.Tests.SkippedInSetup:0:{UnitTestLauncher.Communication.Setup}
@@@:C:TestFramework.Tooling.Execution.Tests.SkippedInSetup:0:{UnitTestLauncher.Communication.Skipped}
@@@:C:TestFramework.Tooling.Execution.Tests.FailInFirstSetup:0:{UnitTestLauncher.Communication.Start}
@@@:C:TestFramework.Tooling.Execution.Tests.FailInFirstSetup:0:{UnitTestLauncher.Communication.Setup}
@@@:C:TestFramework.Tooling.Execution.Tests.FailInFirstSetup:0:{UnitTestLauncher.Communication.SetupFail}:Exception
@@@:C:TestFramework.Tooling.Execution.Tests.FailInFirstCleanUp:0:{UnitTestLauncher.Communication.Start}
@@@:M:TestFramework.Tooling.Execution.Tests.FailInFirstCleanUp.Test:0:{UnitTestLauncher.Communication.Start}
@@@:M:TestFramework.Tooling.Execution.Tests.FailInFirstCleanUp.Test:0:{UnitTestLauncher.Communication.Pass}
@@@:C:TestFramework.Tooling.Execution.Tests.FailInFirstCleanUp:0:{UnitTestLauncher.Communication.Cleanup}
@@@:C:TestFramework.Tooling.Execution.Tests.FailInFirstCleanUp:0:{UnitTestLauncher.Communication.CleanupFail}:Exception
@@@:C:TestFramework.Tooling.Execution.Tests.StaticTestClass:0:{UnitTestLauncher.Communication.Start}
@@@:M:TestFramework.Tooling.Execution.Tests.StaticTestClass.Method1:0:{UnitTestLauncher.Communication.Start}
@@@:M:TestFramework.Tooling.Execution.Tests.StaticTestClass.Method1:0:{UnitTestLauncher.Communication.Pass}
@@@:M:TestFramework.Tooling.Execution.Tests.StaticTestClass.Method2:0:{UnitTestLauncher.Communication.Start}
@@@:M:TestFramework.Tooling.Execution.Tests.StaticTestClass.Method2:0:{UnitTestLauncher.Communication.Pass}
@@@:C:TestFramework.Tooling.Execution.Tests.StaticTestClassSetupCleanupPerMethod:0:{UnitTestLauncher.Communication.Start}
@@@:M:TestFramework.Tooling.Execution.Tests.StaticTestClassSetupCleanupPerMethod.Method1:0:{UnitTestLauncher.Communication.Start}
@@@:M:TestFramework.Tooling.Execution.Tests.StaticTestClassSetupCleanupPerMethod.Method1:0:{UnitTestLauncher.Communication.Pass}
@@@:M:TestFramework.Tooling.Execution.Tests.StaticTestClassSetupCleanupPerMethod.Method2:0:{UnitTestLauncher.Communication.Start}
@@@:M:TestFramework.Tooling.Execution.Tests.StaticTestClassSetupCleanupPerMethod.Method2:0:{UnitTestLauncher.Communication.Pass}
@@@:C:TestFramework.Tooling.Execution.Tests.NonStaticTestClass:0:{UnitTestLauncher.Communication.Start}
@@@:M:TestFramework.Tooling.Execution.Tests.NonStaticTestClass.Method1:0:{UnitTestLauncher.Communication.Start}
@@@:M:TestFramework.Tooling.Execution.Tests.NonStaticTestClass.Method1:0:{UnitTestLauncher.Communication.Pass}
@@@:M:TestFramework.Tooling.Execution.Tests.NonStaticTestClass.Method2:0:{UnitTestLauncher.Communication.Start}
@@@:M:TestFramework.Tooling.Execution.Tests.NonStaticTestClass.Method2:0:{UnitTestLauncher.Communication.Pass}
@@@:C:TestFramework.Tooling.Execution.Tests.NonStaticTestClassSetupCleanupPerMethod:0:{UnitTestLauncher.Communication.Start}
@@@:M:TestFramework.Tooling.Execution.Tests.NonStaticTestClassSetupCleanupPerMethod.Method1:0:{UnitTestLauncher.Communication.Start}
@@@:M:TestFramework.Tooling.Execution.Tests.NonStaticTestClassSetupCleanupPerMethod.Method1:0:{UnitTestLauncher.Communication.Pass}
@@@:M:TestFramework.Tooling.Execution.Tests.NonStaticTestClassSetupCleanupPerMethod.Method2:0:{UnitTestLauncher.Communication.Start}
@@@:M:TestFramework.Tooling.Execution.Tests.NonStaticTestClassSetupCleanupPerMethod.Method2:0:{UnitTestLauncher.Communication.Pass}
@@@:C:TestFramework.Tooling.Execution.Tests.NonStaticTestClassInstancePerMethod:0:{UnitTestLauncher.Communication.Start}
@@@:M:TestFramework.Tooling.Execution.Tests.NonStaticTestClassInstancePerMethod.Method1:0:{UnitTestLauncher.Communication.Start}
@@@:M:TestFramework.Tooling.Execution.Tests.NonStaticTestClassInstancePerMethod.Method1:0:{UnitTestLauncher.Communication.Pass}
@@@:M:TestFramework.Tooling.Execution.Tests.NonStaticTestClassInstancePerMethod.Method2:0:{UnitTestLauncher.Communication.Start}
@@@:M:TestFramework.Tooling.Execution.Tests.NonStaticTestClassInstancePerMethod.Method2:0:{UnitTestLauncher.Communication.Pass}
@@@:C:TestFramework.Tooling.Execution.Tests.TestClassWithMultipleSetupCleanup:0:{UnitTestLauncher.Communication.Start}
@@@:M:TestFramework.Tooling.Execution.Tests.TestClassWithMultipleSetupCleanup.Test:0:{UnitTestLauncher.Communication.Start}
@@@:M:TestFramework.Tooling.Execution.Tests.TestClassWithMultipleSetupCleanup.Test:0:{UnitTestLauncher.Communication.Pass}
@@@:C:TestFramework.Tooling.Execution.Tests.TestWithFrameworkExtensions:0:{UnitTestLauncher.Communication.Start}
@@@:C:TestFramework.Tooling.Execution.Tests.TestWithFrameworkExtensions:0:{UnitTestLauncher.Communication.Setup}
@@@:C:TestFramework.Tooling.Execution.Tests.TestWithFrameworkExtensions:0:{UnitTestLauncher.Communication.SetupFail}:Exception
@@@:C:TestFramework.Tooling.Execution.Tests.TestWithMethods:0:{UnitTestLauncher.Communication.Start}
@@@:D:TestFramework.Tooling.Execution.Tests.TestWithMethods.Test1#0:0:{UnitTestLauncher.Communication.Start}
@@@:D:TestFramework.Tooling.Execution.Tests.TestWithMethods.Test1#0:0:{UnitTestLauncher.Communication.Pass}
@@@:D:TestFramework.Tooling.Execution.Tests.TestWithMethods.Test1#1:0:{UnitTestLauncher.Communication.Start}
@@@:D:TestFramework.Tooling.Execution.Tests.TestWithMethods.Test1#1:0:{UnitTestLauncher.Communication.Pass}
@@@:M:TestFramework.Tooling.Execution.Tests.TestWithMethods.Test2:0:{UnitTestLauncher.Communication.Start}
@@@:M:TestFramework.Tooling.Execution.Tests.TestWithMethods.Test2:0:{UnitTestLauncher.Communication.Pass}
@@@:C:TestFramework.Tooling.Execution.Tests.TestWithNewTestMethodsAttributes:0:{UnitTestLauncher.Communication.Start}
@@@:M:TestFramework.Tooling.Execution.Tests.TestWithNewTestMethodsAttributes.MethodWithCategories:0:{UnitTestLauncher.Communication.Start}
@@@:M:TestFramework.Tooling.Execution.Tests.TestWithNewTestMethodsAttributes.MethodWithCategories:0:{UnitTestLauncher.Communication.Pass}
@@@:M:TestFramework.Tooling.Execution.Tests.TestWithNewTestMethodsAttributes.MethodToRunOnRealHardware:0:{UnitTestLauncher.Communication.Start}
@@@:M:TestFramework.Tooling.Execution.Tests.TestWithNewTestMethodsAttributes.MethodToRunOnRealHardware:0:{UnitTestLauncher.Communication.Fail}:Exception
@@@:D:TestFramework.Tooling.Execution.Tests.TestWithNewTestMethodsAttributes.MethodToRunOnRealHardwareWithData#0:0:{UnitTestLauncher.Communication.Start}
@@@:D:TestFramework.Tooling.Execution.Tests.TestWithNewTestMethodsAttributes.MethodToRunOnRealHardwareWithData#0:0:{UnitTestLauncher.Communication.Fail}:Exception
@@@:C:TestFramework.Tooling.Execution.Tests.TestWithNewTestMethodsAttributes:0:{UnitTestLauncher.Communication.Done}
";
            /// <summary>
            /// The output required to reproduce the outcome of the test cases in the "TestFramework.Tooling.Tests.Execution.v3"
            /// test project as executed on a real hardware device that has a value for deployment configuration key "xyzzy".
            /// </summary>
            public static readonly string Output_Device_xyzzy =
$@"@@@:C:TestFramework.Tooling.Execution.Tests.TestWithFrameworkExtensions:0:{UnitTestLauncher.Communication.Start}
@@@:M:TestFramework.Tooling.Execution.Tests.TestWithFrameworkExtensions.TestDeviceWithSomeFile:0:{UnitTestLauncher.Communication.Start}
@@@:M:TestFramework.Tooling.Execution.Tests.TestWithFrameworkExtensions.TestDeviceWithSomeFile:0:{UnitTestLauncher.Communication.Pass}
@@@:C:TestFramework.Tooling.Execution.Tests.TestWithFrameworkExtensions:0:{UnitTestLauncher.Communication.Done}
";
            /// <summary>
            /// The output required to reproduce the outcome of the test cases in the "TestFramework.Tooling.Tests.Execution.v3"
            /// test project as executed on a real hardware device esp32 device that has an integer value for "RGB LED pin"
            /// in the deployment configuration.
            /// </summary>
            /// <remarks>
            /// Output for TestFramework.Tooling.Execution.Tests.TestWithNewTestMethodsAttributes.MethodToRunOnRealHardware
            /// left out on purpose, so that it will be reported as not run.
            /// </remarks>
            public static readonly string Output_Device_esp32 =
$@"@@@:C:TestFramework.Tooling.Execution.Tests.TestWithNewTestMethodsAttributes:0:{UnitTestLauncher.Communication.Start}
Not in output:::@@@:M:TestFramework.Tooling.Execution.Tests.TestWithNewTestMethodsAttributes.MethodToRunOnRealHardware:0:{UnitTestLauncher.Communication.Start}
Not in output:::@@@:M:TestFramework.Tooling.Execution.Tests.TestWithNewTestMethodsAttributes.MethodToRunOnRealHardware:0:{UnitTestLauncher.Communication.Pass}
@@@:D:TestFramework.Tooling.Execution.Tests.TestWithNewTestMethodsAttributes.MethodToRunOnRealHardwareWithData#0:0:{UnitTestLauncher.Communication.Start}
@@@:D:TestFramework.Tooling.Execution.Tests.TestWithNewTestMethodsAttributes.MethodToRunOnRealHardwareWithData#0:0:{UnitTestLauncher.Communication.Pass}
@@@:C:TestFramework.Tooling.Execution.Tests.TestWithNewTestMethodsAttributes:0:{UnitTestLauncher.Communication.Done}
";

        }
        #endregion

        /// <summary>
        /// Last line of the output, trigger for the output parser to stop the tests execution.
        /// </summary>
        public static readonly string Output_AllTestsDone =
$@"@@@:{UnitTestLauncher.Communication.AllTestsDone}
";

        #endregion
    }
}
