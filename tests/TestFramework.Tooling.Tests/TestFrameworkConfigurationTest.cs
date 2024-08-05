// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Linq;
using System.Xml;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using nanoFramework.TestFramework.Tooling;
using TestFramework.Tooling.Tests.Helpers;

namespace TestFramework.Tooling.Tests
{
    [TestClass]
    public sealed class TestFrameworkConfigurationTest
    {
        [TestMethod]
        [TestCategory("Test cases")]
        [TestCategory("Test execution")]
        public void DefaultConfigurationAndValidation()
        {
            // No configuration
            var actual = TestFrameworkConfiguration.Extract(null);
            AssertConfiguration(new TestFrameworkConfiguration(), actual);
            Assert.IsTrue(actual.Validate(null, null, null));

            // Empty configuration
            actual = TestFrameworkConfiguration.Extract(
                ReadXml(
                    $@"<{TestFrameworkConfiguration.SettingsName}>
                    </{TestFrameworkConfiguration.SettingsName}>"
                ));
            AssertConfiguration(new TestFrameworkConfiguration(), actual);

            // Validation without logger
            Assert.IsTrue(actual.Validate(null, null, null));

            // Validation with logger
            var logger = new LogMessengerMock();
            Assert.IsTrue(actual.Validate(null, null, logger));
            Assert.AreEqual(
$@"
".Replace("\r\n", "\n"),
                string.Join("\n",
                        from m in logger.Messages
                        select $"{m.level}: {m.message}"
                    ) + '\n'
            );
        }

        [TestMethod]
        [TestCategory("Test cases")]
        [TestCategory("Test execution")]
        [DataRow(true)]
        [DataRow(false)]
        public void CustomConfigurationAndValidation(bool withLogger)
        {
            string mockCLRInstanceFilePath = typeof(TestFrameworkConfiguration).Assembly.Location;
            string mockCLRInstanceDirectoryPath = Path.GetDirectoryName(mockCLRInstanceFilePath);

            var actual = TestFrameworkConfiguration.Extract(
                ReadXml(
                    $@"<{TestFrameworkConfiguration.SettingsName}>
                        <AllowRealHardware>false</AllowRealHardware>
                        <RealHardwarePort>COM30;COM42</RealHardwarePort>
                        <DeploySingleAssemblyToRealHardware>false</DeploySingleAssemblyToRealHardware>
                        <PathToLocalCLRInstance>{mockCLRInstanceFilePath}</PathToLocalCLRInstance>
                        <CLRVersion>1.2.3</CLRVersion>
                        <AllowLocalCLRParallelExecution>false</AllowLocalCLRParallelExecution>
                        <Logging>Verbose</Logging>
                    </{TestFrameworkConfiguration.SettingsName}>"
                ));
            AssertConfiguration(new TestFrameworkConfiguration()
            {
                AllowRealHardware = false,
                RealHardwarePort = new string[] { "COM30", "COM42" },
                DeploySingleAssemblyToRealHardware = false,
                PathToLocalCLRInstance = mockCLRInstanceFilePath,
                CLRVersion = "1.2.3",
                AllowLocalCLRParallelExecution = false,
                Logging = LoggingLevel.Verbose
            }, actual);

            // Validation without logger
            Assert.IsTrue(actual.Validate(null, null, null));

            // Validation with log messages
            LogMessengerMock logger = withLogger ? new LogMessengerMock() : null;
            Assert.IsTrue(actual.Validate(mockCLRInstanceDirectoryPath, mockCLRInstanceFilePath, logger));
            if (withLogger)
            {
                Assert.AreEqual(
$@"Verbose: Tests on real hardware are disabled; RealHardwarePort is ignored.'
Verbose: CLRVersion is ignored because the path to a local CLR instance is specified.
".Replace("\r\n", "\n"),
                    string.Join("\n",
                            from m in logger.Messages
                            select $"{m.level}: {m.message}"
                        ) + '\n'
                );
            }
        }

        [TestMethod]
        [TestCategory("Test cases")]
        [TestCategory("Test execution")]
        [DataRow(true)]
        [DataRow(false)]
        public void ConfigurationWithRelativePath_Unresolved(bool withLogger)
        {
            string mockCLRInstanceFilePath = typeof(TestFrameworkConfiguration).Assembly.Location;

            var actual = TestFrameworkConfiguration.Extract(
                ReadXml(
                    $@"<{TestFrameworkConfiguration.SettingsName}>
                        <PathToLocalCLRInstance>{Path.GetFileName(mockCLRInstanceFilePath)}</PathToLocalCLRInstance>
                    </{TestFrameworkConfiguration.SettingsName}>"
                ));
            AssertConfiguration(new TestFrameworkConfiguration()
            {
                PathToLocalCLRInstance = Path.GetFileName(mockCLRInstanceFilePath)
            }, actual);

            // Validation with log messages
            LogMessengerMock logger = withLogger ? new LogMessengerMock() : null;
            Assert.IsFalse(actual.Validate(null, null, logger));
            if (withLogger)
            {
                Assert.AreEqual(
$@"Error: Local CLR instance not found at PathToLocalCLRInstance = '{Path.GetFileName(mockCLRInstanceFilePath)}'
".Replace("\r\n", "\n"),
                    string.Join("\n",
                            from m in logger.Messages
                            select $"{m.level}: {m.message}"
                        ) + '\n'
                );
            }
        }



        [TestMethod]
        [TestCategory("Test cases")]
        [TestCategory("Test execution")]
        [DataRow(true)]
        [DataRow(false)]
        public void ConfigurationWithRelativePath_NotFound(bool withLogger)
        {
            string mockCLRInstanceFilePath = typeof(TestFrameworkConfiguration).Assembly.Location;
            string mockCLRInstanceDirectoryPath = Path.GetDirectoryName(mockCLRInstanceFilePath);

            var actual = TestFrameworkConfiguration.Extract(
                ReadXml(
                    $@"<{TestFrameworkConfiguration.SettingsName}>
                        <PathToLocalCLRInstance>{Path.GetFileName(mockCLRInstanceFilePath)}</PathToLocalCLRInstance>
                    </{TestFrameworkConfiguration.SettingsName}>"
                ));
            AssertConfiguration(new TestFrameworkConfiguration()
            {
                PathToLocalCLRInstance = Path.GetFileName(mockCLRInstanceFilePath)
            }, actual);

            // Validation with log messages
            LogMessengerMock logger = withLogger ? new LogMessengerMock() : null;
            Assert.IsFalse(actual.Validate($"{mockCLRInstanceDirectoryPath}_NotHereEither", Path.Combine($"{mockCLRInstanceDirectoryPath}_NotHere", "NFUnitTest.dll"), logger));
            if (withLogger)
            {
                Assert.AreEqual(
$@"Detailed: PathToLocalCLRInstance '{Path.GetFileName(mockCLRInstanceFilePath)}' is not relative to the assembly directory '{mockCLRInstanceDirectoryPath}_NotHere'
Detailed: PathToLocalCLRInstance '{Path.GetFileName(mockCLRInstanceFilePath)}' is not relative to the solution directory '{mockCLRInstanceDirectoryPath}_NotHereEither'
Error: Local CLR instance not found at PathToLocalCLRInstance = '{Path.GetFileName(mockCLRInstanceFilePath)}'
".Replace("\r\n", "\n"),
                    string.Join("\n",
                            from m in logger.Messages
                            select $"{m.level}: {m.message}"
                        ) + '\n'
                );
            }
        }

        [TestMethod]
        [TestCategory("Test cases")]
        [TestCategory("Test execution")]
        [DataRow(true)]
        [DataRow(false)]
        public void ConfigurationWithRelativePath_InSolutionDirectory(bool withLogger)
        {
            string mockCLRInstanceFilePath = typeof(TestFrameworkConfiguration).Assembly.Location;
            string mockCLRInstanceDirectoryPath = Path.GetDirectoryName(mockCLRInstanceFilePath);

            var actual = TestFrameworkConfiguration.Extract(
                ReadXml(
                    $@"<{TestFrameworkConfiguration.SettingsName}>
                        <PathToLocalCLRInstance>{Path.GetFileName(mockCLRInstanceFilePath)}</PathToLocalCLRInstance>
                    </{TestFrameworkConfiguration.SettingsName}>"
                ));
            AssertConfiguration(new TestFrameworkConfiguration()
            {
                PathToLocalCLRInstance = Path.GetFileName(mockCLRInstanceFilePath)
            }, actual);

            // Validation with log messages
            LogMessengerMock logger = withLogger ? new LogMessengerMock() : null;
            Assert.IsTrue(actual.Validate(mockCLRInstanceDirectoryPath, Path.Combine($"{mockCLRInstanceDirectoryPath}_NotHere", "NFUnitTest.dll"), logger));
            if (withLogger)
            {
                Assert.AreEqual(
$@"Detailed: PathToLocalCLRInstance '{Path.GetFileName(mockCLRInstanceFilePath)}' is not relative to the assembly directory '{mockCLRInstanceDirectoryPath}_NotHere'
Detailed: PathToLocalCLRInstance: found at '{mockCLRInstanceFilePath}'
".Replace("\r\n", "\n"),
                    string.Join("\n",
                            from m in logger.Messages
                            select $"{m.level}: {m.message}"
                        ) + '\n'
                );
            }
        }



        [TestMethod]
        [TestCategory("Test cases")]
        [TestCategory("Test execution")]
        [DataRow(true)]
        [DataRow(false)]
        public void ConfigurationWithRelativePath_InAssemblyDirectory(bool withLogger)
        {
            string mockCLRInstanceFilePath = typeof(TestFrameworkConfiguration).Assembly.Location;
            string mockCLRInstanceDirectoryPath = Path.GetDirectoryName(mockCLRInstanceFilePath);

            var actual = TestFrameworkConfiguration.Extract(
                ReadXml(
                    $@"<{TestFrameworkConfiguration.SettingsName}>
                        <PathToLocalCLRInstance>{Path.GetFileName(mockCLRInstanceFilePath)}</PathToLocalCLRInstance>
                    </{TestFrameworkConfiguration.SettingsName}>"
                ));
            AssertConfiguration(new TestFrameworkConfiguration()
            {
                PathToLocalCLRInstance = Path.GetFileName(mockCLRInstanceFilePath)
            }, actual);

            // Validation with log messages
            LogMessengerMock logger = withLogger ? new LogMessengerMock() : null;
            Assert.IsTrue(actual.Validate(mockCLRInstanceDirectoryPath, Path.Combine(mockCLRInstanceDirectoryPath, "NFUnitTest.dll"), logger));
            if (withLogger)
            {
                Assert.AreEqual(
$@"Detailed: PathToLocalCLRInstance: found at '{mockCLRInstanceFilePath}'
".Replace("\r\n", "\n"),
                    string.Join("\n",
                            from m in logger.Messages
                            select $"{m.level}: {m.message}"
                        ) + '\n'
                );
            }
        }

        [TestMethod]
        [TestCategory("Test cases")]
        [TestCategory("Test execution")]
        public void BackwardCompatibleConfiguration()
        {
            var actual = TestFrameworkConfiguration.Extract(
                ReadXml(
                    $@"<{TestFrameworkConfiguration.SettingsName}>
                        <IsRealHardware>true</IsRealHardware>
                    </{TestFrameworkConfiguration.SettingsName}>"
                ));
            AssertConfiguration(new TestFrameworkConfiguration(), actual);
            Assert.IsTrue(actual.Validate(null, null, null));
        }

        [TestMethod]
        [TestCategory("Test cases")]
        [TestCategory("Test execution")]
        public void ExtensibilityConfiguration()
        {
            var actual = TestFrameworkConfiguration.Extract(
                ReadXml(
                    $@"<MyTooling>
                        <RealHardwarePort>COM30;COM42</RealHardwarePort>
                    </MyTooling>"
                ), "MyTooling");
            AssertConfiguration(new TestFrameworkConfiguration()
            {
                RealHardwarePort = new string[] { "COM30", "COM42" },
            }, actual);
        }

        #region Helpers
        private static void AssertConfiguration(TestFrameworkConfiguration expected, TestFrameworkConfiguration actual)
        {
            Assert.IsNotNull(actual);
            Assert.AreEqual(expected.AllowLocalCLRParallelExecution, actual.AllowLocalCLRParallelExecution);
            Assert.AreEqual(expected.AllowRealHardware, actual.AllowRealHardware);
            Assert.AreEqual(expected.CLRVersion, actual.CLRVersion);
            Assert.AreEqual(expected.DeploySingleAssemblyToRealHardware, actual.DeploySingleAssemblyToRealHardware);
            Assert.AreEqual(expected.Logging, actual.Logging);
            Assert.AreEqual(expected.PathToLocalCLRInstance, actual.PathToLocalCLRInstance);
            Assert.AreEqual(string.Join(",", expected.RealHardwarePort), string.Join(",", actual.RealHardwarePort));
        }

        private static XmlNode ReadXml(string xml)
        {
            var doc = new XmlDocument();
            try
            {
                doc.LoadXml(xml);
            }
            catch (Exception ex)
            {
                Assert.Inconclusive($"Cannot parse XML: {ex.Message}\n{xml}");
            }
            return doc.DocumentElement;
        }
        #endregion
    }
}
