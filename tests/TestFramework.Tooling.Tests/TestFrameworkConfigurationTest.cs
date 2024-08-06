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
        #region Configuration from XML, save/read as runsettings
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

            // Save to XML and read back
            string xml = actual.CreateRunSettings("TestAdapter");
            var read = TestFrameworkConfiguration.Read(xml, null, null);
            AssertConfiguration(actual, read);
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
                        <DeployToRealHardware>
                            <TargetName>esp32</TargetName>
                            <DeployAssembliesOneByOne>false</DeployAssembliesOneByOne>
                        </DeployToRealHardware>
                        <DeployToRealHardware>
                            <DeployAssembliesOneByOne>true</DeployAssembliesOneByOne>
                        </DeployToRealHardware>
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
                DeployToRealHardware = new TestFrameworkConfiguration.DeployToDeviceConfiguration[]
                {
                    new TestFrameworkConfiguration.DeployToDeviceConfiguration()
                    {
                        TargetName = "esp32",
                        DeployAssembliesOneByOne = false
                    },
                    new TestFrameworkConfiguration.DeployToDeviceConfiguration()
                    {
                        DeployAssembliesOneByOne = true
                    }
                },
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

            // Save to XML and read back
            string xml = actual.CreateRunSettings("TestAdapter");
            var read = TestFrameworkConfiguration.Read(xml, null, null);
            AssertConfiguration(actual, read);
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

            // Save to XML and read back
            string xml = actual.CreateRunSettings("TestAdapter");
            var read = TestFrameworkConfiguration.Read(xml, null, null);
            AssertConfiguration(actual, read);
        }
        #endregion

        #region Resolution of relative paths
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
        #endregion

        #region Construction of runsettings
        [TestMethod]
        [TestCategory("Test execution")]
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
        [TestCategory("Test execution")]
        [DataRow(true)]
        [DataRow(false)]
        public void RunSettingsWithRunConfiguration(bool withLogger)
        {
            LogMessengerMock logger = withLogger ? new LogMessengerMock() : null;

            var actual = TestFrameworkConfiguration.Read(
                    $@"<RunSettings>
                        <RunConfiguration>
                            <TestSessionTimeout>1200000</TestSessionTimeout>
                            <TestAdaptersPaths>OtherTestAdapter</TestAdaptersPaths>
                            <MaxCpuCount>10</MaxCpuCount>
                        </RunConfiguration>
                        <{TestFrameworkConfiguration.SettingsName}>
                            <AllowRealHardware>false</AllowRealHardware>
                            <RealHardwarePort>COM30;COM42</RealHardwarePort>
                            <DeployToRealHardware>
                                <TargetName>esp32</TargetName>
                                <DeployAssembliesOneByOne>false</DeployAssembliesOneByOne>
                            </DeployToRealHardware>
                            <DeployToRealHardware>
                                <DeployAssembliesOneByOne>true</DeployAssembliesOneByOne>
                            </DeployToRealHardware>
                            <PathToLocalCLRInstance>New</PathToLocalCLRInstance>
                            <CLRVersion>3.2.1</CLRVersion>
                            <AllowLocalCLRParallelExecution>true</AllowLocalCLRParallelExecution>
                            <Logging>Detailed</Logging>
                        </{TestFrameworkConfiguration.SettingsName}>
                    </RunSettings>",
                    null,
                    logger
                );
            if (withLogger)
            {
                Assert.AreEqual(0, logger.Messages.Count);
            }

            AssertConfiguration(new TestFrameworkConfiguration()
            {
                AllowRealHardware = false,
                RealHardwarePort = new string[] { "COM30", "COM42" },
                DeployToRealHardware = new TestFrameworkConfiguration.DeployToDeviceConfiguration[]
                {
                    new TestFrameworkConfiguration.DeployToDeviceConfiguration()
                    {
                        TargetName = "esp32",
                        DeployAssembliesOneByOne = false
                    },
                    new TestFrameworkConfiguration.DeployToDeviceConfiguration()
                    {
                        DeployAssembliesOneByOne = true
                    }
                },
                PathToLocalCLRInstance = "New",
                CLRVersion = "3.2.1",
                AllowLocalCLRParallelExecution = true,
                Logging = LoggingLevel.Detailed
            }, actual);

            // Save to XML
            string xml = actual.CreateRunSettings("TestAdapter");
            Assert.AreEqual(
@"<?xml version=""1.0"" encoding=""utf-8""?>
<RunSettings>
    <RunConfiguration>
        <TestSessionTimeout>1200000</TestSessionTimeout>
        <TestAdaptersPaths>OtherTestAdapter</TestAdaptersPaths>
        <MaxCpuCount>1</MaxCpuCount>
        <TargetFrameworkVersion>net48</TargetFrameworkVersion>
        <TargetPlatform>x64</TargetPlatform>
    </RunConfiguration>
    <nanoFrameworkAdapter>
        <AllowRealHardware>false</AllowRealHardware>
        <RealHardwarePort>COM30;COM42</RealHardwarePort>
        <DeployToRealHardware>
            <TargetName>esp32</TargetName>
            <DeployAssembliesOneByOne>false</DeployAssembliesOneByOne>
        </DeployToRealHardware>
        <DeployToRealHardware>
            <TargetName>
            </TargetName>
            <DeployAssembliesOneByOne>true</DeployAssembliesOneByOne>
        </DeployToRealHardware>
        <PathToLocalCLRInstance>New</PathToLocalCLRInstance>
        <CLRVersion>3.2.1</CLRVersion>
        <Logging>Detailed</Logging>
    </nanoFrameworkAdapter>
</RunSettings>
".Replace("\r\n", "\n"),
            xml.Replace("\r\n", "\n") + '\n');
        }

        [TestMethod]
        [TestCategory("Test execution")]
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
                            <DeployToRealHardware>
                                <TargetName>esp32</TargetName>
                                <DeployAssembliesOneByOne>false</DeployAssembliesOneByOne>
                            </DeployToRealHardware>
                            <DeployToRealHardware>
                                <TargetName>other</TargetName>
                                <DeployAssembliesOneByOne>false</DeployAssembliesOneByOne>
                            </DeployToRealHardware>
                            <PathToLocalCLRInstance>Old</PathToLocalCLRInstance>
                            <CLRVersion>1.2.3</CLRVersion>
                            <AllowLocalCLRParallelExecution>true</AllowLocalCLRParallelExecution>
                            <Logging>Verbose</Logging>
                        </{TestFrameworkConfiguration.SettingsName}>
                    </RunSettings>",
                    null,
                    logger);
            if (withLogger)
            {
                Assert.AreEqual(0, logger.Messages.Count);
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
                            <DeployToRealHardware>
                                <TargetName>esp32</TargetName>
                                <DeployAssembliesOneByOne>true</DeployAssembliesOneByOne>
                            </DeployToRealHardware>
                            <DeployToRealHardware>
                                <DeployAssembliesOneByOne>true</DeployAssembliesOneByOne>
                            </DeployToRealHardware>
                            <PathToLocalCLRInstance>New</PathToLocalCLRInstance>
                            <CLRVersion>3.2.1</CLRVersion>
                            <AllowLocalCLRParallelExecution>false</AllowLocalCLRParallelExecution>
                            <Logging>Detailed</Logging>
                        </{TestFrameworkConfiguration.SettingsName}>
                    </RunSettings>",
                    toMerge,
                    logger
                );
            if (withLogger)
            {
                Assert.AreEqual(0, logger.Messages.Count);
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
        <DeployToRealHardware>
            <TargetName>esp32</TargetName>
            <DeployAssembliesOneByOne>true</DeployAssembliesOneByOne>
        </DeployToRealHardware>
        <DeployToRealHardware>
            <TargetName>
            </TargetName>
            <DeployAssembliesOneByOne>true</DeployAssembliesOneByOne>
        </DeployToRealHardware>
        <DeployToRealHardware>
            <TargetName>other</TargetName>
            <DeployAssembliesOneByOne>false</DeployAssembliesOneByOne>
        </DeployToRealHardware>
        <PathToLocalCLRInstance>New</PathToLocalCLRInstance>
        <CLRVersion>3.2.1</CLRVersion>
        <AllowLocalCLRParallelExecution>false</AllowLocalCLRParallelExecution>
        <Logging>Detailed</Logging>
    </{TestFrameworkConfiguration.SettingsName}>
</RunSettings>
".Replace("\r\n", "\n"),
            xml.Replace("\r\n", "\n") + '\n');
        }

        [TestMethod]
        [TestCategory("Test execution")]
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
                Assert.AreEqual(
$@"Error: The .runsettings configuration is not valid XML: Data at the root level is invalid. Line 1, position 1.
".Replace("\r\n", "\n"),
                    string.Join("\n",
                            from m in logger.Messages
                            select $"{m.level}: {m.message}"
                        ) + '\n'
                );
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
                Assert.AreEqual(
$@"Error: The .runsettings configuration is not valid XML: Data at the root level is invalid. Line 1, position 1.
".Replace("\r\n", "\n"),
                    string.Join("\n",
                            from m in logger.Messages
                            select $"{m.level}: {m.message}"
                        ) + '\n'
                );
            }
        }
        #endregion

        #region Helpers
        private static void AssertConfiguration(TestFrameworkConfiguration expected, TestFrameworkConfiguration actual)
        {
            Assert.IsNotNull(actual);
            Assert.AreEqual(expected.AllowRealHardware, actual.AllowRealHardware);
            Assert.AreEqual(string.Join(",", expected.RealHardwarePort), string.Join(",", actual.RealHardwarePort));
            if (actual.DeployToRealHardware is null)
            {
                Assert.IsNull(expected.DeployToRealHardware);
            }
            else
            {
                Assert.AreEqual(
                    string.Join(",", from d in expected.DeployToRealHardware select $"'{d.TargetName}:{d.DeployAssembliesOneByOne}'"),
                    string.Join(",", from d in actual.DeployToRealHardware select $"'{d.TargetName}:{d.DeployAssembliesOneByOne}'"));
            }
            Assert.AreEqual(expected.PathToLocalCLRInstance, actual.PathToLocalCLRInstance);
            Assert.AreEqual(expected.CLRVersion, actual.CLRVersion);
            Assert.AreEqual(expected.AllowLocalCLRParallelExecution, actual.AllowLocalCLRParallelExecution);
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
