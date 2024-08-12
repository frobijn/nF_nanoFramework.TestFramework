// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Xml;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using nanoFramework.TestFramework.Tooling;
using TestFramework.Tooling.Tests.Helpers;

namespace TestFramework.Tooling.Tests
{
    [TestClass]
    [TestCategory("Test execution")]
    public sealed class TestFrameworkConfigurationTest
    {
        #region Configuration from XML, save/read as runsettings
        [TestMethod]
        [TestCategory("Test cases")]
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
            logger.AssertEqual("");

            // Save to XML and read back
            string xml = actual.CreateRunSettings("TestAdapter");
            var read = TestFrameworkConfiguration.Read(xml, null, null);
            AssertConfiguration(actual, read);
        }

        [TestMethod]
        [TestCategory("Test cases")]
        [DataRow(true)]
        [DataRow(false)]
        public void CustomConfigurationAndValidation(bool withLogger)
        {
            string mockNanoCLRFilePath = typeof(TestFrameworkConfiguration).Assembly.Location;
            string mockCLRInstanceFilePath = Path.ChangeExtension(typeof(TestFrameworkConfiguration).Assembly.Location, ".pdb");
            string mockCLRInstanceDirectoryPath = Path.GetDirectoryName(mockNanoCLRFilePath);

            var actual = TestFrameworkConfiguration.Extract(
                ReadXml(
                    $@"<{TestFrameworkConfiguration.SettingsName}>
                        <AllowRealHardware>false</AllowRealHardware>
                        <RealHardwarePort>COM30;COM42</RealHardwarePort>
                        <PathToLocalNanoCLR>{mockNanoCLRFilePath}</PathToLocalNanoCLR>
                        <CLRVersion>1.2.3</CLRVersion>
                        <PathToLocalCLRInstance>{mockCLRInstanceFilePath}</PathToLocalCLRInstance>
                        <MaxVirtualDevices>1</MaxVirtualDevices>
                        <Logging>Verbose</Logging>
                    </{TestFrameworkConfiguration.SettingsName}>"
                ));
            AssertConfiguration(new TestFrameworkConfiguration()
            {
                AllowRealHardware = false,
                RealHardwarePort = new string[] { "COM30", "COM42" },
                PathToLocalNanoCLR = mockNanoCLRFilePath,
                CLRVersion = "1.2.3",
                PathToLocalCLRInstance = mockCLRInstanceFilePath,
                MaxVirtualDevices = 1,
                Logging = LoggingLevel.Verbose
            }, actual);

            // Validation without logger
            Assert.IsTrue(actual.Validate(null, null, null));

            // Validation with log messages
            LogMessengerMock logger = withLogger ? new LogMessengerMock() : null;
            Assert.IsTrue(actual.Validate(mockCLRInstanceDirectoryPath, mockNanoCLRFilePath, logger));
            if (withLogger)
            {
                logger.AssertEqual(
$@"Verbose: Tests on real hardware are disabled; RealHardwarePort is ignored.'
Verbose: CLRVersion is ignored because the path to a local CLR instance is specified.");
            }

            // Save to XML and read back
            string xml = actual.CreateRunSettings("TestAdapter");
            var read = TestFrameworkConfiguration.Read(xml, null, null);
            AssertConfiguration(actual, read);
        }

        [TestMethod]
        [TestCategory("Test cases")]
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

            // Save to XML and read back
            string xml = actual.CreateRunSettings("TestAdapter");
            var read = TestFrameworkConfiguration.Read(xml, null, null);
            AssertConfiguration(actual, read);
        }
        #endregion

        #region Resolution of relative paths
        [TestMethod]
        [TestCategory("Test cases")]
        [DataRow(true)]
        [DataRow(false)]
        public void ConfigurationWithRelativePath_Unresolved(bool withLogger)
        {
            string mockNanoCLRFilePath = typeof(TestFrameworkConfiguration).Assembly.Location;
            string mockCLRInstanceFilePath = Path.ChangeExtension(typeof(TestFrameworkConfiguration).Assembly.Location, ".pdb");

            var actual = TestFrameworkConfiguration.Extract(
                ReadXml(
                    $@"<{TestFrameworkConfiguration.SettingsName}>
                        <PathToLocalNanoCLR>{Path.GetFileName(mockNanoCLRFilePath)}</PathToLocalNanoCLR>
                        <PathToLocalCLRInstance>{Path.GetFileName(mockCLRInstanceFilePath)}</PathToLocalCLRInstance>
                    </{TestFrameworkConfiguration.SettingsName}>"
                ));
            AssertConfiguration(new TestFrameworkConfiguration()
            {
                PathToLocalNanoCLR = Path.GetFileName(mockNanoCLRFilePath),
                PathToLocalCLRInstance = Path.GetFileName(mockCLRInstanceFilePath)
            }, actual);

            // Validation with log messages
            LogMessengerMock logger = withLogger ? new LogMessengerMock() : null;
            Assert.IsFalse(actual.Validate(null, null, logger));
            if (withLogger)
            {
                logger.AssertEqual(
$@"Error: Local nanoclr.exe not found at PathToLocalNanoCLR = '{Path.GetFileName(mockNanoCLRFilePath)}'
Error: Local CLR instance not found at PathToLocalCLRInstance = '{Path.GetFileName(mockCLRInstanceFilePath)}'");
            }
        }


        [TestMethod]
        [TestCategory("Test cases")]
        [DataRow(true)]
        [DataRow(false)]
        public void ConfigurationWithRelativePath_NotFound(bool withLogger)
        {
            string mockNanoCLRFilePath = typeof(TestFrameworkConfiguration).Assembly.Location;
            string mockCLRInstanceFilePath = Path.ChangeExtension(typeof(TestFrameworkConfiguration).Assembly.Location, ".pdb");
            string mockCLRInstanceDirectoryPath = Path.GetDirectoryName(mockCLRInstanceFilePath);

            var actual = TestFrameworkConfiguration.Extract(
                ReadXml(
                    $@"<{TestFrameworkConfiguration.SettingsName}>
                        <PathToLocalNanoCLR>{Path.GetFileName(mockNanoCLRFilePath)}</PathToLocalNanoCLR>
                        <PathToLocalCLRInstance>{Path.GetFileName(mockCLRInstanceFilePath)}</PathToLocalCLRInstance>
                    </{TestFrameworkConfiguration.SettingsName}>"
                ));
            AssertConfiguration(new TestFrameworkConfiguration()
            {
                PathToLocalNanoCLR = Path.GetFileName(mockNanoCLRFilePath),
                PathToLocalCLRInstance = Path.GetFileName(mockCLRInstanceFilePath),
            }, actual);

            // Validation with log messages
            LogMessengerMock logger = withLogger ? new LogMessengerMock() : null;
            Assert.IsFalse(actual.Validate($"{mockCLRInstanceDirectoryPath}_NotHereEither", Path.Combine($"{mockCLRInstanceDirectoryPath}_NotHere", "NFUnitTest.dll"), logger));
            if (withLogger)
            {
                logger.AssertEqual(
$@"Detailed: PathToLocalNanoCLR '{Path.GetFileName(mockNanoCLRFilePath)}' is not relative to the assembly directory '{mockCLRInstanceDirectoryPath}_NotHere'
Detailed: PathToLocalNanoCLR '{Path.GetFileName(mockNanoCLRFilePath)}' is not relative to the solution directory '{mockCLRInstanceDirectoryPath}_NotHereEither'
Error: Local nanoclr.exe not found at PathToLocalNanoCLR = '{Path.GetFileName(mockNanoCLRFilePath)}'
Detailed: PathToLocalCLRInstance '{Path.GetFileName(mockCLRInstanceFilePath)}' is not relative to the assembly directory '{mockCLRInstanceDirectoryPath}_NotHere'
Detailed: PathToLocalCLRInstance '{Path.GetFileName(mockCLRInstanceFilePath)}' is not relative to the solution directory '{mockCLRInstanceDirectoryPath}_NotHereEither'
Error: Local CLR instance not found at PathToLocalCLRInstance = '{Path.GetFileName(mockCLRInstanceFilePath)}'");
            }
        }

        [TestMethod]
        [TestCategory("Test cases")]
        [DataRow(true)]
        [DataRow(false)]
        public void ConfigurationWithRelativePath_InSolutionDirectory(bool withLogger)
        {
            string mockNanoCLRFilePath = typeof(TestFrameworkConfiguration).Assembly.Location;
            string mockCLRInstanceFilePath = Path.ChangeExtension(typeof(TestFrameworkConfiguration).Assembly.Location, ".pdb");
            string mockCLRInstanceDirectoryPath = Path.GetDirectoryName(mockCLRInstanceFilePath);

            var actual = TestFrameworkConfiguration.Extract(
                ReadXml(
                    $@"<{TestFrameworkConfiguration.SettingsName}>
                        <PathToLocalNanoCLR>{Path.GetFileName(mockNanoCLRFilePath)}</PathToLocalNanoCLR>
                        <PathToLocalCLRInstance>{Path.GetFileName(mockCLRInstanceFilePath)}</PathToLocalCLRInstance>
                    </{TestFrameworkConfiguration.SettingsName}>"
                ));
            AssertConfiguration(new TestFrameworkConfiguration()
            {
                PathToLocalNanoCLR = Path.GetFileName(mockNanoCLRFilePath),
                PathToLocalCLRInstance = Path.GetFileName(mockCLRInstanceFilePath),
            }, actual);

            // Validation with log messages
            LogMessengerMock logger = withLogger ? new LogMessengerMock() : null;
            Assert.IsTrue(actual.Validate(mockCLRInstanceDirectoryPath, Path.Combine($"{mockCLRInstanceDirectoryPath}_NotHere", "NFUnitTest.dll"), logger));
            if (withLogger)
            {
                logger.AssertEqual(
$@"Detailed: PathToLocalNanoCLR '{Path.GetFileName(mockNanoCLRFilePath)}' is not relative to the assembly directory '{mockCLRInstanceDirectoryPath}_NotHere'
Detailed: PathToLocalNanoCLR: found at '{mockNanoCLRFilePath}'
Detailed: PathToLocalCLRInstance '{Path.GetFileName(mockCLRInstanceFilePath)}' is not relative to the assembly directory '{mockCLRInstanceDirectoryPath}_NotHere'
Detailed: PathToLocalCLRInstance: found at '{mockCLRInstanceFilePath}'");
            }
        }


        [TestMethod]
        [TestCategory("Test cases")]
        [DataRow(true)]
        [DataRow(false)]
        public void ConfigurationWithRelativePath_InAssemblyDirectory(bool withLogger)
        {
            string mockNanoCLRFilePath = typeof(TestFrameworkConfiguration).Assembly.Location;
            string mockCLRInstanceFilePath = Path.ChangeExtension(typeof(TestFrameworkConfiguration).Assembly.Location, ".pdb");
            string mockCLRInstanceDirectoryPath = Path.GetDirectoryName(mockCLRInstanceFilePath);

            var actual = TestFrameworkConfiguration.Extract(
                ReadXml(
                    $@"<{TestFrameworkConfiguration.SettingsName}>
                        <PathToLocalNanoCLR>{Path.GetFileName(mockNanoCLRFilePath)}</PathToLocalNanoCLR>
                        <PathToLocalCLRInstance>{Path.GetFileName(mockCLRInstanceFilePath)}</PathToLocalCLRInstance>
                    </{TestFrameworkConfiguration.SettingsName}>"
                ));
            AssertConfiguration(new TestFrameworkConfiguration()
            {
                PathToLocalNanoCLR = Path.GetFileName(mockNanoCLRFilePath),
                PathToLocalCLRInstance = Path.GetFileName(mockCLRInstanceFilePath)
            }, actual);

            // Validation with log messages
            LogMessengerMock logger = withLogger ? new LogMessengerMock() : null;
            Assert.IsTrue(actual.Validate(mockCLRInstanceDirectoryPath, Path.Combine(mockCLRInstanceDirectoryPath, "NFUnitTest.dll"), logger));
            if (withLogger)
            {
                logger.AssertEqual(
$@"Detailed: PathToLocalNanoCLR: found at '{mockNanoCLRFilePath}'
Detailed: PathToLocalCLRInstance: found at '{mockCLRInstanceFilePath}'");
            }
        }
        #endregion

        #region Construction of runsettings
        [TestMethod]
        public void RunSettingsDefaultConfiguration()
        {
            string actual = new TestFrameworkConfiguration().CreateRunSettings("TestAdapter");
            Assert.AreEqual(
@"<?xml version=""1.0"" encoding=""utf-8""?>
<RunSettings>
    <RunConfiguration>
        <MaxCpuCount>1</MaxCpuCount>
        <TargetFrameworkVersion>net48</TargetFrameworkVersion>
        <TargetPlatform>x64</TargetPlatform>
        <TestAdaptersPaths>TestAdapter</TestAdaptersPaths>
    </RunConfiguration>
    <nanoFrameworkAdapter />
</RunSettings>
".Replace("\r\n", "\n"),
            actual.Replace("\r\n", "\n") + '\n');
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void RunSettingsWithRunConfiguration(bool withLogger)
        {
            LogMessengerMock logger = withLogger ? new LogMessengerMock() : null;

            var actual = TestFrameworkConfiguration.Read(
                    $@"<RunSettings>
                        <RunConfiguration>
                            <SomeSetting>42</SomeSetting>
                            <TestAdaptersPaths>OtherTestAdapter</TestAdaptersPaths>
                            <MaxCpuCount>10</MaxCpuCount>
                            <TestSessionTimeout>1000</TestSessionTimeout>
                        </RunConfiguration>
                        <{TestFrameworkConfiguration.SettingsName}>
                            <AllowRealHardware>false</AllowRealHardware>
                            <RealHardwarePort>COM30;COM42</RealHardwarePort>
                            <PathToLocalNanoCLR>New</PathToLocalNanoCLR>
                            <CLRVersion>3.2.1</CLRVersion>
                            <MaxVirtualDevices>10</MaxVirtualDevices>
                            <Logging>Detailed</Logging>
                        </{TestFrameworkConfiguration.SettingsName}>
                    </RunSettings>",
                    null,
                    logger
                );
            if (withLogger)
            {
                logger.AssertEqual("");
            }

            AssertConfiguration(new TestFrameworkConfiguration()
            {
                AllowRealHardware = false,
                RealHardwarePort = new string[] { "COM30", "COM42" },
                PathToLocalNanoCLR = "New",
                CLRVersion = "3.2.1",
                MaxVirtualDevices = 10,
                Logging = LoggingLevel.Detailed
            }, actual, 1000);

            // Save to XML
            string xml = actual.CreateRunSettings("TestAdapter");
            Assert.AreEqual(
@"<?xml version=""1.0"" encoding=""utf-8""?>
<RunSettings>
    <RunConfiguration>
        <SomeSetting>42</SomeSetting>
        <TestAdaptersPaths>OtherTestAdapter</TestAdaptersPaths>
        <TestSessionTimeout>1000</TestSessionTimeout>
        <MaxCpuCount>1</MaxCpuCount>
        <TargetFrameworkVersion>net48</TargetFrameworkVersion>
        <TargetPlatform>x64</TargetPlatform>
    </RunConfiguration>
    <nanoFrameworkAdapter>
        <AllowRealHardware>false</AllowRealHardware>
        <RealHardwarePort>COM30;COM42</RealHardwarePort>
        <PathToLocalNanoCLR>New</PathToLocalNanoCLR>
        <CLRVersion>3.2.1</CLRVersion>
        <MaxVirtualDevices>10</MaxVirtualDevices>
        <Logging>Detailed</Logging>
    </nanoFrameworkAdapter>
</RunSettings>
".Replace("\r\n", "\n"),
            xml.Replace("\r\n", "\n") + '\n');
        }

        [TestMethod]
        [DataRow(true, false)]
        [DataRow(false, false)]
        [DataRow(true, true)]
        public void MergeRunSettingsOverwriteMany(bool withLogger, bool withAdapterPath)
        {
            LogMessengerMock logger = withLogger ? new LogMessengerMock() : null;

            var toMerge = TestFrameworkConfiguration.Read(
                    $@"<RunSettings>
                        <RunConfiguration>
                            <MaxCpuCount>25</MaxCpuCount>
                            <ResultsDirectory>.\TestResults</ResultsDirectory>
                            <TestSessionTimeout>1000</TestSessionTimeout>
                            {(withAdapterPath ? "<TestAdaptersPaths>OldTestAdapter</TestAdaptersPaths>" : "")}
                            <MaxCpuCount>10</MaxCpuCount>
                        </RunConfiguration>
                        <{TestFrameworkConfiguration.SettingsName}>
                            <AllowRealHardware>false</AllowRealHardware>
                            <RealHardwarePort>COM30;COM42</RealHardwarePort>
                            <PathToLocalNanoCLR>Old</PathToLocalNanoCLR>
                            <CLRVersion>1.2.3</CLRVersion>
                            <MaxVirtualDevices>1</MaxVirtualDevices>
                            <Logging>Verbose</Logging>
                        </{TestFrameworkConfiguration.SettingsName}>
                    </RunSettings>",
                    null,
                    logger);
            if (withLogger)
            {
                logger.AssertEqual("");
            }

            logger = withLogger ? new LogMessengerMock() : null;
            var actual = TestFrameworkConfiguration.Read(
                    $@"<RunSettings>
                        <RunConfiguration>
                            <TestSessionTimeout>1200000</TestSessionTimeout>
                            {(withAdapterPath ? "<TestAdaptersPaths>NewTestAdapter</TestAdaptersPaths>" : "")}
                            <TargetFrameworkVersion>v1.0</TargetFrameworkVersion>
                        </RunConfiguration>
                        <{TestFrameworkConfiguration.SettingsName}>
                            <AllowRealHardware>false</AllowRealHardware>
                            <RealHardwarePort>COM11;COM31</RealHardwarePort>
                            <PathToLocalNanoCLR>New</PathToLocalNanoCLR>
                            <CLRVersion>3.2.1</CLRVersion>
                            <MaxVirtualDevices>1</MaxVirtualDevices>
                            <Logging>Detailed</Logging>
                        </{TestFrameworkConfiguration.SettingsName}>
                    </RunSettings>",
                    toMerge,
                    logger
                );
            if (withLogger)
            {
                logger.AssertEqual("");
            }

            // Save to XML and read back
            string xml = actual.CreateRunSettings("FinalTestAdapter");
            Assert.AreEqual(
$@"<?xml version=""1.0"" encoding=""utf-8""?>
<RunSettings>
    <RunConfiguration>
        <ResultsDirectory>.\TestResults</ResultsDirectory>
        <TestSessionTimeout>1200000</TestSessionTimeout>{(withAdapterPath ? @"
        <TestAdaptersPaths>NewTestAdapter</TestAdaptersPaths>" : "")}
        <MaxCpuCount>1</MaxCpuCount>
        <TargetFrameworkVersion>net48</TargetFrameworkVersion>
        <TargetPlatform>x64</TargetPlatform>{(withAdapterPath ? "" : @"
        <TestAdaptersPaths>FinalTestAdapter</TestAdaptersPaths>")}
    </RunConfiguration>
    <{TestFrameworkConfiguration.SettingsName}>
        <AllowRealHardware>false</AllowRealHardware>
        <RealHardwarePort>COM11;COM31</RealHardwarePort>
        <PathToLocalNanoCLR>New</PathToLocalNanoCLR>
        <CLRVersion>3.2.1</CLRVersion>
        <MaxVirtualDevices>1</MaxVirtualDevices>
        <Logging>Detailed</Logging>
    </{TestFrameworkConfiguration.SettingsName}>
</RunSettings>
".Replace("\r\n", "\n"),
            xml.Replace("\r\n", "\n") + '\n');
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void InvalidRunSettings(bool withLogger)
        {
            LogMessengerMock logger = withLogger ? new LogMessengerMock() : null;
            var actual = TestFrameworkConfiguration.Read("This is no XML", null, logger);

            Assert.IsNotNull(actual);
            AssertConfiguration(new TestFrameworkConfiguration(), actual);
            if (withLogger)
            {
                logger.AssertEqual(
$@"Error: The .runsettings configuration is not valid XML: Data at the root level is invalid. Line 1, position 1.");
            }

            logger = withLogger ? new LogMessengerMock() : null;
            var toModify = new TestFrameworkConfiguration()
            {
                CLRVersion = "42"
            };
            actual = TestFrameworkConfiguration.Read("This is no XML", toModify, logger);
            Assert.IsTrue(object.ReferenceEquals(toModify, actual));
            if (withLogger)
            {
                logger.AssertEqual(
$@"Error: The .runsettings configuration is not valid XML: Data at the root level is invalid. Line 1, position 1.");
            }
        }
        #endregion

        #region Helpers
        private static void AssertConfiguration(TestFrameworkConfiguration expected, TestFrameworkConfiguration actual, int? expectedTestSessionTimeout = null)
        {
            Assert.IsNotNull(actual);
            Assert.AreEqual(expectedTestSessionTimeout, actual.TestSessionTimeout);
            Assert.AreEqual(expected.AllowRealHardware, actual.AllowRealHardware);
            Assert.AreEqual(string.Join(",", expected.RealHardwarePort), string.Join(",", actual.RealHardwarePort));
            Assert.AreEqual(expected.PathToLocalNanoCLR, actual.PathToLocalNanoCLR);
            Assert.AreEqual(expected.CLRVersion, actual.CLRVersion);
            Assert.AreEqual(expected.PathToLocalCLRInstance, actual.PathToLocalCLRInstance);
            Assert.AreEqual(expected.MaxVirtualDevices, actual.MaxVirtualDevices);
            Assert.AreEqual(expected.Logging, actual.Logging);
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
