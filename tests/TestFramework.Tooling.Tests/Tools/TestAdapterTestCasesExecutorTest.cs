// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using nanoFramework.TestFramework.Tooling;
using nanoFramework.TestFramework.Tooling.Tools;
using TestFramework.Tooling.Tests.Helpers;
using TestResult = nanoFramework.TestFramework.Tooling.TestResult;

namespace TestFramework.Tooling.Tests.Tools
{
    [TestClass]
    [TestCategory("Visual Studio/VSTest")]
    public sealed class TestAdapterTestCasesExecutorTest
    {
        /// <summary>
        /// The purpose of this test is to verify that the method
        /// <see cref="TestAdapterTestCasesExecutor.AddTestResults"/> is working correctly
        /// for tests run on the virtual device.
        /// The correct working of running tests on a Virtual Device is already tested
        /// by <see cref="TestsRunnerExecutionTest"/>.
        /// </summary>
        /// <param name="allowRealHardware">Indicates whether the real hardware is allowed to be used.
        /// If <c>false</c>, the <see cref="TestsRunner"/> will not attempt to find any real hardware devices.
        /// If <c>true</c>, it will try but (by design of this test) it will fail to find any. The difference is
        /// the result outcome and test result details.</param>
        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void TestAdapterTestCasesExecutor_VirtualDeviceOnly(bool allowRealHardware)
        {
            #region Set up the configurations and create the parameters
            var configuration = new TestFrameworkConfiguration()
            {
                AllowRealHardware = allowRealHardware
            };
            if (allowRealHardware)
            {
                // Exclude all existing ports
                configuration.ExcludeSerialPorts.AddRange(SerialPort.GetPortNames());
            }

            (TestExecutor_TestCases_Parameters parameters, string testDirectoryPath, TestResultCollection testResults) = CreateParameters(
                ("TestFramework.Tooling.Tests.Execution.v3", configuration)
            );
            #endregion

            #region Execute the TestAdapterTestCasesExecutor
            var logger = new LogMessengerMock();

            using (var cancellationToken = new CancellationTokenSource())
            {
                TestAdapterTestCasesExecutor.Run(
                    parameters,
                    (message) =>
                    {
                        lock (testResults)
                        {
                            if (message is TestExecutor_TestResults results)
                            {
                                testResults.AddTestResults(results, logger);
                            }
                            else
                            {
                                ((LogMessenger)logger)(LoggingLevel.Error, $"Unexpected message type: {message.GetType()}");
                            }
                        }
                    },
                    logger,
                    cancellationToken.Token);
            }
            #endregion

            #region Asserts
            logger.AssertEqual("", LoggingLevel.Warning);

            // We do not need to assert the proper working of TestsRunner etc.
            // Just make sure the tests have been executed.

            foreach (TestCase testCase in testResults.TestCases)
            {
                // Make sure every test has a result
                Assert.IsTrue(testResults.TestResults.ContainsKey(testCase), $"{testCase.FullyQualifiedName} - {testCase.DisplayName}");
                Assert.AreEqual(1, testResults.TestResults[testCase].Count);

                (string computerName, TestExecutor_TestResults.TestResult testResult) = testResults.TestResults[testCase][0];

                // Check that the other properties are assigned
                Assert.IsNotNull(testResult.DisplayName, $"{testCase.FullyQualifiedName} - {testCase.DisplayName}");
                Assert.AreEqual(testCase.ShouldRunOnRealHardware, testResult.ForRealHardware ?? false, $"{testCase.FullyQualifiedName} - {testCase.DisplayName}");
                Assert.IsNotNull(testResult.Messages, $"{testCase.FullyQualifiedName} - {testCase.DisplayName}");
                Assert.AreNotEqual(0, testResult.Messages.Count, $"{testCase.FullyQualifiedName} - {testCase.DisplayName}");
                if (testResult.Outcome != (int)TestResult.TestOutcome.Passed)
                {
                    Assert.IsNotNull(testResult.ErrorMessage, $"{testCase.FullyQualifiedName} - {testCase.DisplayName}");
                }

                if (testCase.ShouldRunOnVirtualDevice)
                {
                    // Should have been run
                    Assert.AreNotEqual((int)TestResult.TestOutcome.None, testResult.Outcome, $"{testCase.FullyQualifiedName} - {testCase.DisplayName}");
                    Assert.AreEqual("Virtual nanoDevice", computerName, $"{testCase.FullyQualifiedName} - {testCase.DisplayName}");

                    if (testCase.FullyQualifiedName == "TestFramework.Tooling.Execution.Tests.FailInTest.Test")
                    {
                        Assert.IsTrue(testResult.Duration.TotalMilliseconds > 0);
                    }
                }
                else if (allowRealHardware)
                {
                    // Should not have been run
                    Assert.AreEqual((int)TestResult.TestOutcome.None, testResult.Outcome, $"{testCase.FullyQualifiedName} - {testCase.DisplayName}");
                    Assert.IsNull(computerName, $"{testCase.FullyQualifiedName} - {testCase.DisplayName}");
                }
                else
                {
                    // Should have been skipped
                    Assert.AreEqual((int)TestResult.TestOutcome.Skipped, testResult.Outcome, $"{testCase.FullyQualifiedName} - {testCase.DisplayName}");
                    Assert.IsNull(computerName, $"{testCase.FullyQualifiedName} - {testCase.DisplayName}");
                }
            }
            #endregion
        }

        /// <summary>
        /// The purpose of this test is to verify the working of the interaction between
        /// <see cref="TestsRunner"/> and <see cref="RealHardwareDeviceHelper"/>, and
        /// that the method <see cref="TestAdapterTestCasesExecutor.AddTestResults"/> is working correctly
        /// for tests run on a real hardware device.
        /// This test method requires that one or more real hardware devices are connected.
        /// Any type of device will do. Actual results depend on whether there is a esp32 device present.
        /// If possible, test with two or more devices, including two esp32 devices.
        /// Make sure to run this test at least once with a esp32-device connected.
        /// </summary>
        [TestMethod]
        [TestCategory("@Real hardware")]
        [DoNotParallelize]
        public void TestAdapterTestCasesExecutor_IncludingHardware_AllTests()
        {
            #region Set up the configurations and create the parameters
            List<RealHardwareDeviceHelper> realHardwareDevices = GetRealHardwareDevices();

            var configuration = new TestFrameworkConfiguration();
            foreach (RealHardwareDeviceHelper device in realHardwareDevices)
            {
                configuration.SetDeploymentConfigurationFilePath(device.SerialPort, $"../deployment_configuration_{device.SerialPort}.json");
            }

            (TestExecutor_TestCases_Parameters parameters, string testDirectoryPath, TestResultCollection testResults) = CreateParameters(
                ("TestFramework.Tooling.Tests.Execution.v3", configuration),
                ("TestFramework.Tooling.Tests.Hardware_esp32.v3", configuration)
            );

            foreach (RealHardwareDeviceHelper device in realHardwareDevices)
            {
                File.WriteAllText(Path.Combine(testDirectoryPath, $"deployment_configuration_{device.SerialPort}.json"),
$@"{{
    ""DisplayName"": ""Configuration for device connected to {device.SerialPort}"",
    ""Configuration"":
    {{
        ""xyzzy"": ""Value unique for the device connected to {device.SerialPort} to ensure the test has to run on every real hardware device."",
        ""RGB LED pin"": 42,
        ""data.bin"": {{ ""File"": ""data.bin"" }},
        ""data.txt"": ""Content of the data.txt file""
    }}
}}");
            }
            File.WriteAllText(Path.Combine(testDirectoryPath, $"data.bin"), "Binary data");
            #endregion

            #region Execute the TestAdapterTestCasesExecutor
            var logger = new LogMessengerMock();

            using (var cancellationToken = new CancellationTokenSource())
            {
                TestAdapterTestCasesExecutor.Run(
                parameters,
                (message) =>
                {
                    lock (testResults)
                    {
                        if (message is TestExecutor_TestResults results)
                        {
                            testResults.AddTestResults(results, logger);
                        }
                        else
                        {
                            ((LogMessenger)logger)(LoggingLevel.Error, $"Unexpected message type: {message.GetType()}");
                        }
                    }
                },
                logger,
                cancellationToken.Token);
            }
            #endregion

            #region Asserts
            logger.AssertEqual("", LoggingLevel.Warning);

            // We do not need to assert the proper working of TestsRunner in detail.
            // Just make sure the tests have been executed, especially on the real hardware devices,
            // as that is not tested for TestsRunner.
            foreach (TestCase testCase in testResults.TestCases)
            {
                // Make sure every test has a result
                Assert.IsTrue(testResults.TestResults.ContainsKey(testCase), $"{testCase.FullyQualifiedName} - {testCase.DisplayName}");

                (string computerName, TestExecutor_TestResults.TestResult testResult) = testResults.TestResults[testCase][0];
                if (testCase.ShouldRunOnVirtualDevice)
                {
                    // Should have one result
                    Assert.AreEqual(1, testResults.TestResults[testCase].Count);
                    Assert.AreNotEqual((int)TestResult.TestOutcome.None, testResult.Outcome, $"{testCase.FullyQualifiedName} - {testCase.DisplayName}");
                }
                else if (testCase.FullyQualifiedName == "TestFramework.Tooling.Execution.Tests.TestWithFrameworkExtensions.TestDeviceWithSomeFile")
                {
                    // Should have been run on every real hardware device
                    Assert.AreEqual
                    (
                        string.Join("\n", from device in realHardwareDevices
                                          orderby device.SerialPort
                                          select $"nanoDevice connected to {device.SerialPort}") + '\n',
                        string.Join("\n", from t in testResults.TestResults[testCase]
                                          orderby t.computerName
                                          select t.computerName) + '\n'
                    );

                    // Should have passed everywhere, otherwise there is a problem with the deployment configuration.
                    foreach ((string computerName, TestExecutor_TestResults.TestResult testResult) result in testResults.TestResults[testCase])
                    {
                        // This assert may fail because the deployment to the device failed.
                        Assert.AreEqual((int)TestResult.TestOutcome.Passed, result.testResult.Outcome, result.computerName);
                    }
                }
                else if (testCase.FullyQualifiedName == "TestFramework.Tooling.Execution.Tests.TestWithFrameworkExtensions.TestOnDeviceWithProgrammingError_ShouldTestOnDevice")
                {
                    // Should have generated an error for every real hardware device
                    // This is also tested in a TestsRunner unit test.
                    Assert.AreEqual
                    (
                        string.Join("\n", from device in realHardwareDevices
                                          orderby device.SerialPort
                                          select $"nanoDevice connected to {device.SerialPort}") + '\n',
                        string.Join("\n", from t in testResults.TestResults[testCase]
                                          orderby t.computerName
                                          select t.computerName) + '\n'
                    );
                    foreach ((string computerName, TestExecutor_TestResults.TestResult testResult) result in testResults.TestResults[testCase])
                    {
                        Assert.AreEqual((int)TestResult.TestOutcome.Skipped, result.testResult.Outcome, result.computerName);
                    }
                }
                else if (testCase.FullyQualifiedName == "TestFramework.Tooling.Execution.Tests.TestWithFrameworkExtensions.TestOnDeviceWithProgrammingError_AreDevicesEqual")
                {
                    // Should have generated an error for all but one real hardware device
                    // This is also tested in a TestsRunner unit test.
                    Assert.AreEqual
                    (
                        string.Join("\n", from device in realHardwareDevices
                                          orderby device.SerialPort
                                          select $"nanoDevice connected to {device.SerialPort}") + '\n',
                        string.Join("\n", from t in testResults.TestResults[testCase]
                                          orderby t.computerName
                                          select t.computerName) + '\n'
                    );
                    Assert.AreEqual(realHardwareDevices.Count - 1, (from t in testResults.TestResults[testCase]
                                                                    where t.testResult.Outcome == (int)TestResult.TestOutcome.Skipped
                                                                    select t).Count());
                    Assert.AreEqual(1, (from t in testResults.TestResults[testCase]
                                        where t.testResult.Outcome == (int)TestResult.TestOutcome.Passed
                                        select t).Count());
                }
                else if (testCase.Traits.Contains("@ESP32"))
                {
                    // All ESP32-tests should have been run on a single of the ESP32 devices.
                    // or not at all
                    Assert.AreEqual(1, testResults.TestResults[testCase].Count);

                    if ((from device in realHardwareDevices
                         where device.Platform == "ESP32"
                         select device).Any())
                    {
                        Assert.AreNotEqual((int)TestResult.TestOutcome.None, testResult.Outcome, $"{testCase.FullyQualifiedName} - {testCase.DisplayName}");
                    }
                    else
                    {
                        Assert.AreEqual((int)TestResult.TestOutcome.None, testResult.Outcome, $"{testCase.FullyQualifiedName} - {testCase.DisplayName}");
                    }
                }
                else
                {
                    // Generic hardware test (are there any present?)
                    // Should have been run on a single device
                    Assert.AreEqual(1, testResults.TestResults[testCase].Count);
                    Assert.AreNotEqual((int)TestResult.TestOutcome.None, testResult.Outcome, $"{testCase.FullyQualifiedName} - {testCase.DisplayName}");
                }
            }
            #endregion
        }

        /// <summary>
        /// The purpose of this test is to verify the working of the interaction between
        /// <see cref="TestsRunner"/> and <see cref="RealHardwareDeviceHelper"/> in case the
        /// execution of tests is cancelled. Other cancel-related functionality is already tested in
        /// <see cref="TestsRunnerOrchestrationTest"/>.
        /// This test method requires that one or more real hardware devices are connected
        /// (one is enough). Any type of device will do.
        /// </summary>
        [TestMethod]
        [TestCategory("@Real hardware")]
        [DoNotParallelize]
        public void TestAdapterTestCasesExecutor_IncludingHardware_Cancel()
        {
            #region Set up the configurations and create the parameters
            List<RealHardwareDeviceHelper> realHardwareDevices = GetRealHardwareDevices();

            var configuration = new TestFrameworkConfiguration();
            foreach (RealHardwareDeviceHelper device in realHardwareDevices)
            {
                configuration.SetDeploymentConfigurationFilePath(device.SerialPort, $"../deployment_configuration_{device.SerialPort}.json");
            }

            (TestExecutor_TestCases_Parameters parameters, string testDirectoryPath, TestResultCollection testResults) = CreateParameters(
                ("TestFramework.Tooling.Tests.Execution.v3", configuration)
            );

            foreach (RealHardwareDeviceHelper device in realHardwareDevices)
            {
                File.WriteAllText(Path.Combine(testDirectoryPath, $"deployment_configuration_{device.SerialPort}.json"),
$@"{{
    ""DisplayName"": ""Configuration for device connected to {device.SerialPort}"",
    ""Configuration"":
    {{
        ""xyzzy"": ""Value unique for the device connected to {device.SerialPort} to ensure the test has to run on every real hardware device."",
        ""RGB LED pin"": 42,
        ""data.bin"": {{ ""File"": ""data.bin"" }},
        ""data.txt"": ""Content of the data.txt file""
    }}
}}");
            }
            File.WriteAllText(Path.Combine(testDirectoryPath, $"data.bin"), "Binary data");
            #endregion

            #region Execute the TestAdapterTestCasesExecutor
            var logger = new LogMessengerMock();

            using (var cancellationToken = new CancellationTokenSource())
            {
                bool firstHardwareMessage = true;

                TestAdapterTestCasesExecutor.Run(
                    parameters,
                    (message) =>
                    {
                        lock (testResults)
                        {
                            if (message is TestExecutor_TestResults results)
                            {
                                if (firstHardwareMessage && !results.ComputerName.Contains("Virtual"))
                                {
                                    // Must be a message about a test that will not be executed
                                    // (TestOnDeviceWithProgrammingError_ShouldTestOnDevice)
                                    firstHardwareMessage = false;

                                    // Cancel after a few seconds, in the midst of the device initialization
                                    // (Assumption: no nanoDevice can be initialized including assembly upload in that short a time)
                                    cancellationToken.CancelAfter(2000);
                                }

                                testResults.AddTestResults(results, logger);
                            }
                            else
                            {
                                ((LogMessenger)logger)(LoggingLevel.Error, $"Unexpected message type: {message.GetType()}");
                            }
                        }
                    },
                    logger,
                    cancellationToken.Token);
            }
            #endregion

            #region Asserts
            logger.AssertEqual("", LoggingLevel.Warning);

            // We do not need to assert the proper working of TestsRunner in detail.
            // Just make sure the tests have been executed, especially on the real hardware devices,
            // as that is not tested for TestsRunner.
            foreach (TestCase testCase in testResults.TestCases)
            {
                // Make sure every test has a result
                Assert.IsTrue(testResults.TestResults.ContainsKey(testCase), $"{testCase.FullyQualifiedName} - {testCase.DisplayName}");

                if (!testCase.ShouldRunOnVirtualDevice
                    && testCase.FullyQualifiedName == "TestFramework.Tooling.Execution.Tests.TestWithFrameworkExtensions.TestDeviceWithSomeFile")
                {
                    /// Should have been run on every real hardware device
                    Assert.AreEqual
                    (
                        string.Join("\n", from device in realHardwareDevices
                                          orderby device.SerialPort
                                          select $"nanoDevice connected to {device.SerialPort}") + '\n',
                        string.Join("\n", from t in testResults.TestResults[testCase]
                                          orderby t.computerName
                                          select t.computerName) + '\n'
                    );

                    // Test should not have been run, but as it was selected to be run, it is listed as skipped 
                    foreach ((string computerName, TestExecutor_TestResults.TestResult testResult) in testResults.TestResults[testCase])
                    {
                        Assert.AreEqual((int)TestResult.TestOutcome.Skipped, testResult.Outcome, computerName);
                    }
                }
            }
            #endregion
        }

        /// <summary>
        /// The purpose of this test is to verify the working of the interaction between
        /// <see cref="TestsRunner"/> and <see cref="RealHardwareDeviceHelper"/> in case the
        /// execution of tests times out. Other timeout-related functionality is already tested in
        /// <see cref="TestsRunnerOrchestrationTest"/>.
        /// This test method requires that one or more real hardware devices are connected
        /// (one is enough). Any type of device will do.
        /// </summary>
        [TestMethod]
        [TestCategory("@Real hardware")]
        [DoNotParallelize]
        public void TestAdapterTestCasesExecutor_IncludingHardware_Timeout()
        {
            #region Set up the configurations and create the parameters
            List<RealHardwareDeviceHelper> realHardwareDevices = GetRealHardwareDevices();

            var configuration = new TestFrameworkConfiguration()
            {
                RealHardwareTimeout = 1
            };
            foreach (RealHardwareDeviceHelper device in realHardwareDevices)
            {
                configuration.SetDeploymentConfigurationFilePath(device.SerialPort, $"../deployment_configuration_{device.SerialPort}.json");
            }

            (TestExecutor_TestCases_Parameters parameters, string testDirectoryPath, TestResultCollection testResults) = CreateParameters(
                ("TestFramework.Tooling.Tests.Execution.v3", configuration)
            );

            foreach (RealHardwareDeviceHelper device in realHardwareDevices)
            {
                File.WriteAllText(Path.Combine(testDirectoryPath, $"deployment_configuration_{device.SerialPort}.json"),
$@"{{
    ""DisplayName"": ""Configuration for device connected to {device.SerialPort}"",
    ""Configuration"":
    {{
        ""xyzzy"": ""Value unique for the device connected to {device.SerialPort} to ensure the test has to run on every real hardware device."",
        ""RGB LED pin"": 42,
        ""data.bin"": {{ ""File"": ""data.bin"" }},
        ""data.txt"": ""Content of the data.txt file""
    }}
}}");
            }
            File.WriteAllText(Path.Combine(testDirectoryPath, $"data.bin"), "Binary data");
            #endregion

            #region Execute the TestAdapterTestCasesExecutor
            var logger = new LogMessengerMock();

            using (var cancellationToken = new CancellationTokenSource())
            {
                TestAdapterTestCasesExecutor.Run(
                    parameters,
                    (message) =>
                    {
                        lock (testResults)
                        {
                            if (message is TestExecutor_TestResults results)
                            {
                                testResults.AddTestResults(results, logger);
                            }
                            else
                            {
                                ((LogMessenger)logger)(LoggingLevel.Error, $"Unexpected message type: {message.GetType()}");
                            }
                        }
                    },
                    logger,
                    cancellationToken.Token);
            }
            #endregion

            #region Asserts
            logger.AssertEqual("", LoggingLevel.Warning);

            // We do not need to assert the proper working of TestsRunner in detail.
            // Just make sure the tests have been executed, especially on the real hardware devices,
            // as that is not tested for TestsRunner.
            foreach (TestCase testCase in testResults.TestCases)
            {
                // Make sure every test has a result
                Assert.IsTrue(testResults.TestResults.ContainsKey(testCase), $"{testCase.FullyQualifiedName} - {testCase.DisplayName}");

                if (!testCase.ShouldRunOnVirtualDevice
                    && testCase.FullyQualifiedName == "TestFramework.Tooling.Execution.Tests.TestWithFrameworkExtensions.TestDeviceWithSomeFile")
                {
                    /// Should have been run on every real hardware device
                    Assert.AreEqual
                    (
                        string.Join("\n", from device in realHardwareDevices
                                          orderby device.SerialPort
                                          select $"nanoDevice connected to {device.SerialPort}") + '\n',
                        string.Join("\n", from t in testResults.TestResults[testCase]
                                          orderby t.computerName
                                          select t.computerName) + '\n'
                    );

                    // Test should not have been run, but as it was selected to be run, it is listed as skipped 
                    foreach ((string computerName, TestExecutor_TestResults.TestResult testResult) in testResults.TestResults[testCase])
                    {
                        Assert.AreEqual((int)TestResult.TestOutcome.Skipped, testResult.Outcome, computerName);
                    }
                }
            }
            #endregion
        }

        /// <summary>
        /// The purpose of this test is to verify the working of the interaction between
        /// <see cref="TestsRunner"/> and <see cref="RealHardwareDeviceHelper"/> in case the
        /// logging of the execution of tests is set to detailed. Other logging-related
        /// functionality is already tested in <see cref="TestsRunnerOrchestrationTest"/>.
        /// This test method requires that one or more real hardware devices are connected
        /// (one is enough). Any type of device will do.
        /// </summary>
        [TestMethod]
        [TestCategory("@Real hardware")]
        [DoNotParallelize]
        public void TestAdapterTestCasesExecutor_IncludingHardware_DetailedLogging()
        {
            #region Set up the configurations and create the parameters
            List<RealHardwareDeviceHelper> realHardwareDevices = GetRealHardwareDevices();

            var configuration = new TestFrameworkConfiguration()
            {
                Logging = LoggingLevel.Detailed
            };
            foreach (RealHardwareDeviceHelper device in realHardwareDevices)
            {
                configuration.SetDeploymentConfigurationFilePath(device.SerialPort, $"../deployment_configuration_{device.SerialPort}.json");
            }

            (TestExecutor_TestCases_Parameters parameters, string testDirectoryPath, TestResultCollection testResults) = CreateParameters(
                ("TestFramework.Tooling.Tests.Execution.v3", configuration)
            );

            foreach (RealHardwareDeviceHelper device in realHardwareDevices)
            {
                File.WriteAllText(Path.Combine(testDirectoryPath, $"deployment_configuration_{device.SerialPort}.json"),
$@"{{
    ""DisplayName"": ""Configuration for device connected to {device.SerialPort}"",
    ""Configuration"":
    {{
        ""xyzzy"": ""Value unique for the device connected to {device.SerialPort} to ensure the test has to run on every real hardware device."",
        ""RGB LED pin"": 42,
        ""data.bin"": {{ ""File"": ""data.bin"" }},
        ""data.txt"": ""Content of the data.txt file""
    }}
}}");
            }
            File.WriteAllText(Path.Combine(testDirectoryPath, $"data.bin"), "Binary data");
            #endregion

            #region Execute the TestAdapterTestCasesExecutor
            var logger = new LogMessengerMock();

            using (var cancellationToken = new CancellationTokenSource())
            {
                TestAdapterTestCasesExecutor.Run(
                    parameters,
                    (message) =>
                    {
                        lock (testResults)
                        {
                            if (message is TestExecutor_TestResults results)
                            {
                                testResults.AddTestResults(results, logger);
                            }
                            else
                            {
                                ((LogMessenger)logger)(LoggingLevel.Error, $"Unexpected message type: {message.GetType()}");
                            }
                        }
                    },
                    logger,
                    cancellationToken.Token);
            }
            #endregion

            #region Asserts
            logger.AssertEqual("", LoggingLevel.Warning);

            // We do not need to assert the proper working of TestsRunner in detail.
            // Just make sure the tests have been executed, especially on the real hardware devices,
            // as that is not tested for TestsRunner.
            foreach (TestCase testCase in testResults.TestCases)
            {
                // Make sure every test has a result
                Assert.IsTrue(testResults.TestResults.ContainsKey(testCase), $"{testCase.FullyQualifiedName} - {testCase.DisplayName}");

                if (!testCase.ShouldRunOnVirtualDevice)
                {
                    /// There should be a result for every device
                    Assert.AreEqual
                    (
                        string.Join("\n", from device in realHardwareDevices
                                          orderby device.SerialPort
                                          select $"nanoDevice connected to {device.SerialPort}") + '\n',
                        string.Join("\n", from t in testResults.TestResults[testCase]
                                          orderby t.computerName
                                          select t.computerName) + '\n'
                    );
                }
            }
            #endregion
        }

        #region Helpers
        public TestContext TestContext { get; set; }

        /// <summary>
        /// Get the real hardware devices present, and abort the test if there are none.
        /// </summary>
        /// <returns></returns>
        private List<RealHardwareDeviceHelper> GetRealHardwareDevices()
        {
            var realHardwarePorts = RealHardwareSerialPorts.GetAllSerialPortNames().ToList();
            if (realHardwarePorts.Count == 0)
            {
                Assert.Inconclusive("This test requires that one or more real hardware devices are connected");
            }

            var logger = new LogMessengerMock();
            var realHardwareDevices = new List<RealHardwareDeviceHelper>();
            void AddDevice(RealHardwareDeviceHelper device)
            {
                lock (realHardwareDevices)
                {
                    realHardwareDevices.Add(device);
                }
            }

            RealHardwareDeviceHelper.GetAllAvailable(RealHardwareSerialPorts.ExcludeSerialPorts, AddDevice, logger)
                .GetAwaiter().GetResult();
            if (realHardwareDevices.Count == 0)
            {
                Assert.Inconclusive("This test requires that one or more real hardware devices are connected");
            }

            logger.AssertEqual("", LoggingLevel.Error);

            return realHardwareDevices;
        }

        /// <summary>
        /// Collector of test results.
        /// </summary>
        private sealed class TestResultCollection
        {
            public List<TestCase> TestCases
            {
                get;
            } = new List<TestCase>();

            public Dictionary<TestCase, List<(string computerName, TestExecutor_TestResults.TestResult testResult)>> TestResults
            {
                get;
            } = new Dictionary<TestCase, List<(string, TestExecutor_TestResults.TestResult)>>();

            public void AddTestResults(TestExecutor_TestResults results, LogMessenger logger)
            {
                foreach (TestExecutor_TestResults.TestResult result in results.TestResults)
                {
                    if (result.Index < 0 || result.Index >= TestCases.Count)
                    {
                        logger(LoggingLevel.Error, $"Invalid index {result.Index} for test result '{result.DisplayName}' => {result.ErrorMessage}");
                    }
                    else
                    {
                        TestCase testCase = TestCases[result.Index];
                        if (!TestResults.TryGetValue(testCase, out List<(string computerName, TestExecutor_TestResults.TestResult)> list))
                        {
                            TestResults[testCase] = list = new List<(string computerName, TestExecutor_TestResults.TestResult)>();
                        }
                        list.Add((results.ComputerName, result));
                    }
                }
            }
        }

        private (TestExecutor_TestCases_Parameters parameters, string testDirectoryPath, TestResultCollection results) CreateParameters(params (string projectName, TestFrameworkConfiguration configuration)[] projectNameAndConfiguration)
        {
            #region Copy the assembles and project files and get all test cases
            // ... as the test needs a copy of the project structure to create the unit test launcher and custom test framework configurations.
            string testDirectoryPath = TestDirectoryHelper.GetTestDirectory(TestContext);

            var setupLogger = new LogMessengerMock();
            var testAssemblies = new List<string>();
            foreach ((string projectName, TestFrameworkConfiguration configuration) in projectNameAndConfiguration)
            {
                string projectDirectoryPath = Path.Combine(testDirectoryPath, projectName);

                (configuration ?? new TestFrameworkConfiguration()).SaveEffectiveSettings(projectDirectoryPath, setupLogger);

                testAssemblies.Add((from a in AssemblyHelper.CopyAssembliesAndProjectFile(projectDirectoryPath, "bin", projectName)
                                    where Path.GetFileNameWithoutExtension(a) == projectName
                                    select Path.ChangeExtension(a, ".dll")).First());
            }

            var testCases = new TestCaseCollection(testAssemblies, (a) => ProjectSourceInventory.FindProjectFilePath(a, setupLogger), true, setupLogger);
            setupLogger.AssertEqual("", LoggingLevel.Error);
            #endregion

            var results = new TestResultCollection();
            results.TestCases.AddRange(testCases.TestCases);


            var parameters = new TestExecutor_TestCases_Parameters()
            {
                LogLevel = (int)LoggingLevel.Detailed,
                TestCases = new List<TestExecutor_TestCases_Parameters.TestCase>()
            };
            parameters.TestCases.AddRange(from tc in results.TestCases
                                          select new TestExecutor_TestCases_Parameters.TestCase()
                                          {
                                              AssemblyFilePath = tc.AssemblyFilePath,
                                              DisplayName = tc.DisplayName,
                                              FullyQualifiedName = tc.FullyQualifiedName
                                          });

            return (parameters, testDirectoryPath, results);
        }
        #endregion
    }
}
