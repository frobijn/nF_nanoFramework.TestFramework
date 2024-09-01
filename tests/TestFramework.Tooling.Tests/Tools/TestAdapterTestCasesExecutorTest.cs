// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using nanoFramework.TestFramework.Tooling;
using nanoFramework.TestFramework.Tooling.Tools;
using TestFramework.Tooling.Tests.Helpers;

namespace TestFramework.Tooling.Tests.Tools
{
    [TestClass]
    [TestCategory("Visual Studio/VSTest")]
    public sealed class TestAdapterTestCasesExecutorTest
    {
        [TestMethod]
        public void TestAdapterTestCasesExecutor_VirtualDeviceOnly()
        {
            #region Set up the configurations and create the parameters
            var configuration = new TestFrameworkConfiguration()
            {
                AllowRealHardware = false
            };

            (TestExecutor_TestCases_Parameters parameters, string testDirectoryPath) = CreateParameters(
                ("TestFramework.Tooling.Tests.Execution.v3", configuration)
            );
            #endregion

            #region Execute the TestAdapterTestCasesExecutor
            var logger = new LogMessengerMock();
            var testResults = new List<TestExecutor_TestResults>();

            TestAdapterTestCasesExecutor.Run(
                parameters,
                (message) =>
                {
                    lock (testResults)
                    {
                        if (message is TestExecutor_TestResults results)
                        {
                            testResults.Add(results);
                        }
                        else
                        {
                            ((LogMessenger)logger)(LoggingLevel.Error, $"Unexpected message type: {message.GetType()}");
                        }
                    }
                },
            logger,
                new CancellationTokenSource().Token);
            #endregion

            #region Asserts
            logger.AssertEqual("");

            // We do not need to assert the proper working of TestsRunner etc.
            // Just make sure the tests have been executed.

            // NonFailingTest should have been executed
            var genericTests = (from result in testResults
                                where (from tr in result.TestResults
                                       where (from m in tr.Messages
                                              where m.Contains("NonFailingTest")
                                              select m).Any()
                                       select tr).Any()
                                select result.ComputerName).ToList();
            Assert.AreEqual
            (
                "Virtual nanoDevice\n",
                string.Join("\n", from t in genericTests
                                  orderby t
                                  select t) + '\n'
            );
            #endregion
        }

        /// <summary>
        /// This test method requires that one or more real hardware devices are connected.
        /// Any type of device will do. Actual results depend on whether there is a esp32 device present.
        /// If possible, test with two or more devices, including two esp32 devices.
        /// </summary>
        [TestMethod]
        [TestCategory("@Real hardware")]
        public void TestAdapterTestCasesExecutor_IncludingHardware()
        {
            #region Verify that real hardware devices are available
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
            #endregion

            #region Set up the configurations and create the parameters
            var configuration = new TestFrameworkConfiguration();
            foreach (RealHardwareDeviceHelper device in realHardwareDevices)
            {
                configuration.SetDeploymentConfigurationFilePath(device.SerialPort, $"../deployment_configuration_{device.SerialPort}.json");
            }

            (TestExecutor_TestCases_Parameters parameters, string testDirectoryPath) = CreateParameters(
                ("TestFramework.Tooling.Tests.Execution.v3", configuration),
                ("TestFramework.Tooling.Tests.Hardware_esp32.v3", new TestFrameworkConfiguration())
            );

            foreach (RealHardwareDeviceHelper device in realHardwareDevices)
            {
                File.WriteAllText(Path.Combine(testDirectoryPath, $"deployment_configuration_{device.SerialPort}.json"),
$@"{{
    ""xyzzy"": ""Value unique for the device connected to {device.SerialPort} to ensure the test has to run on every real hardware device."",
    ""RGB LED pin"": 42,
    ""data.bin"": {{ ""File"": ""data.bin"" }},
    ""data.txt"": ""Content of the data.txt file""
}}");
            }
            File.WriteAllText(Path.Combine(testDirectoryPath, $"data.bin"), "Binary data");
            #endregion

            #region Execute the TestAdapterTestCasesExecutor
            logger = new LogMessengerMock();
            var testResults = new List<TestExecutor_TestResults>();

            TestAdapterTestCasesExecutor.Run(
                parameters,
                (message) =>
                {
                    lock (testResults)
                    {
                        if (message is TestExecutor_TestResults results)
                        {
                            testResults.Add(results);
                        }
                        else
                        {
                            ((LogMessenger)logger)(LoggingLevel.Error, $"Unexpected message type: {message.GetType()}");
                        }
                    }
                },
                logger,
                new CancellationTokenSource().Token);
            #endregion

            #region Asserts
            logger.AssertEqual("");

            // We do not need to assert the proper working of TestsRunner etc.
            // Just make sure the tests have been executed, especially on the real hardware devices,
            // as that is not tested for TestsRunner.

            // NonFailingTest should have been executed on each device
            var genericTests = (from result in testResults
                                where (from tr in result.TestResults
                                       where (from m in tr.Messages
                                              where m.Contains("NonFailingTest")
                                              select m).Any()
                                       select tr).Any()
                                select result.ComputerName).ToList();
            Assert.AreEqual
            (
                string.Join("\n", from device in realHardwareDevices
                                  orderby device.SerialPort
                                  select $"nanoDevice connected to {device.SerialPort}") + '\n'
                + "Virtual nanoDevice\n",
                string.Join("\n", from t in genericTests
                                  orderby t
                                  select t) + '\n'
            );

            // xyzzy test should have been executed on each real hardware device
            var xyzzyTests = (from result in testResults
                              where (from tr in result.TestResults
                                     where (from m in tr.Messages
                                            where m.Contains("xyzzy")
                                            select m).Any()
                                     select tr).Any()
                              select result.ComputerName).ToList();
            Assert.AreEqual
            (
                string.Join("\n", from device in realHardwareDevices
                                  orderby device.SerialPort
                                  select $"nanoDevice connected to {device.SerialPort}") + '\n',
                string.Join("\n", from t in xyzzyTests
                                  orderby t
                                  select t) + '\n'
            );

            // The esp32-tests should have been executed only on one device.
            var esp32Tests = (from result in testResults
                              where (from tr in result.TestResults
                                     where (from m in tr.Messages
                                            where m.Contains("esp32")
                                            select m).Any()
                                     select tr).Any()
                              select result.ComputerName).ToList();
            if ((from device in realHardwareDevices
                 where device.Platform == "esp32"
                 select device).Any())
            {
                Assert.AreEqual(1, esp32Tests.Count);
            }
            else
            {
                Assert.AreEqual(0, esp32Tests.Count);
            }

            // The hardware specific esp32-test should have been executed only on one device.
            esp32Tests = (from result in testResults
                          where (from tr in result.TestResults
                                 where (from m in tr.Messages
                                        where m.Contains("Use esp32 native assembly")
                                        select m).Any()
                                 select tr).Any()
                          select result.ComputerName).ToList();
            if ((from device in realHardwareDevices
                 where device.Platform == "esp32"
                 select device).Any())
            {
                Assert.AreEqual(1, esp32Tests.Count);
            }
            else
            {
                Assert.AreEqual(0, esp32Tests.Count);
            }
            #endregion
        }

        #region Helpers
        public TestContext TestContext { get; set; }

        private (TestExecutor_TestCases_Parameters parameters, string testDirectoryPath) CreateParameters(params (string projectName, TestFrameworkConfiguration configuration)[] projectNameAndConfiguration)
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
                                    select a).First());
            }

            var testCases = new TestCaseCollection(testAssemblies, (a) => ProjectSourceInventory.FindProjectFilePath(a, setupLogger), true, setupLogger);
            setupLogger.AssertEqual("", LoggingLevel.Error);
            #endregion

            var parameters = new TestExecutor_TestCases_Parameters()
            {
                LogLevel = (int)LoggingLevel.Detailed,
                TestCases = new List<TestExecutor_TestCases_Parameters.TestCase>()
            };

            parameters.TestCases.AddRange(from tc in testCases.TestCases
                                          select new TestExecutor_TestCases_Parameters.TestCase()
                                          {
                                              AssemblyFilePath = tc.AssemblyFilePath,
                                              DisplayName = tc.DisplayName,
                                              FullyQualifiedName = tc.FullyQualifiedName
                                          });
            return (parameters, testDirectoryPath);
        }
        #endregion
    }
}
