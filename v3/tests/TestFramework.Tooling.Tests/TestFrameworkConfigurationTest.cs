// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using nanoFramework.TestFramework.Tooling;
using TestFramework.Tooling.Tests.Helpers;

namespace TestFramework.Tooling.Tests
{
    [TestClass]
    [TestCategory("Test execution")]
    public sealed class TestFrameworkConfigurationTest
    {
        public TestContext TestContext { get; set; }

        private static readonly string[] s_knownSerialPorts = new string[]
        {
            "COM9", "COM11", "COM42"
        };


        [TestMethod]
        [TestCategory("Migration v2 to v3")]
        public void TestFrameworkConfiguration_MigrateToCustomSettings()
        {
            string testBaseDirectory = TestDirectoryHelper.GetTestDirectory(TestContext);

            #region nano.runsettings does not exist
            var logger = new LogMessengerMock();
            var actual = TestFrameworkConfiguration.Read(testBaseDirectory, true, logger);
            logger.AssertEqual("", LoggingLevel.Error);
            Assert.IsNotNull(actual);

            string expectedFilePath = Path.Combine(testBaseDirectory, TestFrameworkConfiguration.SettingsFileName);
            string expectedUserFilePath = Path.Combine(testBaseDirectory, TestFrameworkConfiguration.UserSettingsFileName);
            File.WriteAllText(expectedFilePath, "Assert that the file is deleted");
            File.WriteAllText(expectedUserFilePath, "Assert that the file is deleted");

            AssertEffectiveSettings(actual, testBaseDirectory, null, null);
            #endregion

            #region nano.runsettings has all v2 properties for real hardware
            File.WriteAllText(expectedFilePath, @"<?xml version=""1.0"" encoding=""utf-8""?>
<RunSettings>
    <nanoFrameworkAdapter>
        <CLRVersion>v1.0</CLRVersion>
        <PathToLocalCLRInstance>..\clr\custom.bin</PathToLocalCLRInstance>
        <Logging>Verbose</Logging>
        <RealHardwarePort>COM9</RealHardwarePort>
        <IsRealHardware>true</IsRealHardware>
    </nanoFrameworkAdapter>
</RunSettings>");

            actual = TestFrameworkConfiguration.Read(testBaseDirectory, true, logger);
            logger.AssertEqual("", LoggingLevel.Error);
            Assert.IsNotNull(actual);

            AssertConfiguration(new TestFrameworkConfiguration()
            {
                AllowRealHardware = true,
                AllowSerialPorts = new List<string>() { "COM9" },
                CLRVersion = "v1.0",
                Logging = LoggingLevel.Verbose,
                PathToLocalCLRInstance = Path.GetFullPath(Path.Combine(testBaseDirectory, "..", "clr", "custom.bin"))
            },
            actual);

            string testSubDirectory = Path.Combine(testBaseDirectory, "sub");
            AssertEffectiveSettings(actual, Path.Combine(testBaseDirectory, "sub"),
                $@"<?xml version=""1.0"" encoding=""utf-8""?>
<RunSettings>
    <nanoFrameworkAdapter>
        <CLRVersion>v1.0</CLRVersion>
        <PathToLocalCLRInstance>..\..\clr\custom.bin</PathToLocalCLRInstance>
        <Logging>Verbose</Logging>
    </nanoFrameworkAdapter>
</RunSettings>",
                $@"<?xml version=""1.0"" encoding=""utf-8""?>
<RunSettings>
    <nanoFrameworkAdapter>
        <AllowSerialPorts>COM9</AllowSerialPorts>
    </nanoFrameworkAdapter>
</RunSettings>");
            #endregion

            #region nano.runsettings has all v2 properties for virtual device
            expectedFilePath = Path.Combine(testSubDirectory, TestFrameworkConfiguration.SettingsFileName);
            expectedUserFilePath = Path.Combine(testSubDirectory, TestFrameworkConfiguration.UserSettingsFileName);
            File.WriteAllText(expectedFilePath, @"<?xml version=""1.0"" encoding=""utf-8""?>
<RunSettings>
    <nanoFrameworkAdapter>
        <CLRVersion>v1.0</CLRVersion>
        <PathToLocalCLRInstance>clr\custom.bin</PathToLocalCLRInstance>
        <Logging>Verbose</Logging>
        <RealHardwarePort>COM9</RealHardwarePort>
        <IsRealHardware>false</IsRealHardware>
    </nanoFrameworkAdapter>
</RunSettings>");
            File.WriteAllText(expectedUserFilePath, @"<?xml version=""1.0"" encoding=""utf-8""?>
<RunSettings>
    <nanoFrameworkAdapter>
        <CLRVersion>Should not be read as this file does not exist in v2</CLRVersion>
        <PathToLocalCLRInstance>Should not be read</PathToLocalCLRInstance>
        <Logging>None</Logging>
        <RealHardwarePort>Should not be read</RealHardwarePort>
        <IsRealHardware>true</IsRealHardware>
    </nanoFrameworkAdapter>
</RunSettings>");

            actual = TestFrameworkConfiguration.Read(testSubDirectory, true, logger);
            logger.AssertEqual("", LoggingLevel.Error);
            Assert.IsNotNull(actual);

            AssertConfiguration(new TestFrameworkConfiguration()
            {
                AllowRealHardware = false,
                AllowSerialPorts = new List<string>() { "COM9" },
                CLRVersion = "v1.0",
                Logging = LoggingLevel.Verbose,
                PathToLocalCLRInstance = Path.GetFullPath(Path.Combine(testSubDirectory, "clr", "custom.bin"))
            },
            actual);

            AssertEffectiveSettings(actual, testBaseDirectory,
                $@"<?xml version=""1.0"" encoding=""utf-8""?>
<RunSettings>
    <nanoFrameworkAdapter>
        <AllowRealHardware>false</AllowRealHardware>
        <CLRVersion>v1.0</CLRVersion>
        <PathToLocalCLRInstance>sub\clr\custom.bin</PathToLocalCLRInstance>
        <Logging>Verbose</Logging>
    </nanoFrameworkAdapter>
</RunSettings>",
                null
            );
            #endregion
        }

        [TestMethod]
        public void TestFrameworkConfiguration_ReadSettingsFromHierarchy()
        {
            #region Setup
            string testBaseDirectory = TestDirectoryHelper.GetTestDirectory(TestContext);
            string globalDirectory = Path.Combine(testBaseDirectory, "nanoFramework");
            Directory.CreateDirectory(globalDirectory);
            File.WriteAllText(Path.Combine(globalDirectory, TestFrameworkConfiguration.SettingsFileName), @"<?xml version=""1.0"" encoding=""utf-8""?>
<RunSettings>
    <nanoFrameworkAdapter>
        <AllowRealHardware>false</AllowRealHardware>
        <RealHardwareTimeout>120000</RealHardwareTimeout>
        <PathToLocalNanoCLR>clr\nanoclr.exe</PathToLocalNanoCLR>
        <CLRVersion>v1.0</CLRVersion>
        <PathToLocalCLRInstance>clr\custom.bin</PathToLocalCLRInstance>
        <MaxVirtualDevices>4</MaxVirtualDevices>
        <VirtualDeviceTimeout>60000</VirtualDeviceTimeout>
        <Logging>Verbose</Logging>
    </nanoFrameworkAdapter>
</RunSettings>");
            File.WriteAllText(Path.Combine(globalDirectory, TestFrameworkConfiguration.UserSettingsFileName), @"<?xml version=""1.0"" encoding=""utf-8""?>
<RunSettings>
    <nanoFrameworkAdapter>
        <AllowSerialPorts>COM9;COM11,COM42</AllowSerialPorts>
        <ExcludeSerialPorts>COM30;COM31</ExcludeSerialPorts>
        <DeploymentConfiguration>
            <SerialPort>COM9</SerialPort>
            <File>..\DeploymentConfiguration\DevBoard_MCU_ESP32S3_N8R32V.json</File>
        </DeploymentConfiguration>
    </nanoFrameworkAdapter>
</RunSettings>");
            string solutionDirectory = Path.Combine(testBaseDirectory, "solution");
            Directory.CreateDirectory(solutionDirectory);
            File.WriteAllText(Path.Combine(solutionDirectory, TestFrameworkConfiguration.SettingsFileName), @"<?xml version=""1.0"" encoding=""utf-8""?>
<RunSettings>
    <nanoFrameworkAdapter>
        <GlobalSettingsDirectoryPath>..\nanoFramework</GlobalSettingsDirectoryPath>
        <MaxVirtualDevices>8</MaxVirtualDevices>
        <VirtualDeviceTimeout>10000</VirtualDeviceTimeout>
        <Logging>Detailed</Logging>
    </nanoFrameworkAdapter>
</RunSettings>");
            File.WriteAllText(Path.Combine(solutionDirectory, TestFrameworkConfiguration.UserSettingsFileName), @"<?xml version=""1.0"" encoding=""utf-8""?>
<RunSettings>
    <nanoFrameworkAdapter>
        <AllowSerialPorts>COM42</AllowSerialPorts>
        <DeploymentConfiguration>
            <SerialPort>COM11</SerialPort>
            <File>..\DeploymentConfiguration\DevBoard_MCU_ESP32S3_DevKitM.json</File>
        </DeploymentConfiguration>
    </nanoFrameworkAdapter>
</RunSettings>");
            string projectDirectory = Path.Combine(solutionDirectory, "project");
            Directory.CreateDirectory(projectDirectory);
            File.WriteAllText(Path.Combine(projectDirectory, TestFrameworkConfiguration.SettingsFileName), @"<?xml version=""1.0"" encoding=""utf-8""?>
<RunSettings>
    <nanoFrameworkAdapter>
        <GlobalSettingsDirectoryPath>..</GlobalSettingsDirectoryPath>
        <AllowRealHardware>true</AllowRealHardware>
    </nanoFrameworkAdapter>
</RunSettings>");
            File.WriteAllText(Path.Combine(projectDirectory, TestFrameworkConfiguration.UserSettingsFileName), @"<?xml version=""1.0"" encoding=""utf-8""?>
<RunSettings>
    <nanoFrameworkAdapter>
        <AllowSerialPorts>COM11;COM42</AllowSerialPorts>
        <DeploymentConfiguration>
            <SerialPort>COM9</SerialPort>
            <File></File>
        </DeploymentConfiguration>
        <DeploymentConfiguration>
            <SerialPort>COM42</SerialPort>
            <File>..\..\DeploymentConfiguration\DevBoard_OV2640.json</File>
        </DeploymentConfiguration>
    </nanoFrameworkAdapter>
</RunSettings>");
            #endregion

            #region Only global directory
            var logger = new LogMessengerMock();
            var actual = TestFrameworkConfiguration.Read(globalDirectory, false, logger);
            logger.AssertEqual("", LoggingLevel.Verbose);
            AssertConfiguration(
                AddDeploymentConfiguration("COM9", Path.Combine(testBaseDirectory, "DeploymentConfiguration", "DevBoard_MCU_ESP32S3_N8R32V.json"),
                new TestFrameworkConfiguration()
                {
                    AllowRealHardware = false,
                    AllowSerialPorts = new List<string>() { "COM9", "COM11", "COM42" },
                    ExcludeSerialPorts = new List<string>() { "COM30", "COM31" },
                    RealHardwareTimeout = 120000,
                    PathToLocalNanoCLR = Path.Combine(globalDirectory, "clr", "nanoclr.exe"),
                    CLRVersion = "v1.0",
                    PathToLocalCLRInstance = Path.Combine(globalDirectory, "clr", "custom.bin"),
                    MaxVirtualDevices = 4,
                    VirtualDeviceTimeout = 60000,
                    Logging = LoggingLevel.Verbose
                }),
                actual,
                globalDirectory
            );

            AssertEffectiveSettings(actual, Path.Combine(testBaseDirectory, "sub"),
                $@"<?xml version=""1.0"" encoding=""utf-8""?>
<RunSettings>
    <nanoFrameworkAdapter>
        <AllowRealHardware>false</AllowRealHardware>
        <PathToLocalNanoCLR>..\nanoFramework\clr\nanoclr.exe</PathToLocalNanoCLR>
        <CLRVersion>v1.0</CLRVersion>
        <PathToLocalCLRInstance>..\nanoFramework\clr\custom.bin</PathToLocalCLRInstance>
        <MaxVirtualDevices>4</MaxVirtualDevices>
        <VirtualDeviceTimeout>60000</VirtualDeviceTimeout>
        <Logging>Verbose</Logging>
    </nanoFrameworkAdapter>
</RunSettings>",
                null);
            #endregion

            #region Global + solution directory
            logger = new LogMessengerMock();
            actual = TestFrameworkConfiguration.Read(solutionDirectory, false, logger);
            logger.AssertEqual("", LoggingLevel.Verbose);
            AssertConfiguration(
                AddDeploymentConfiguration("COM9", Path.Combine(testBaseDirectory, "DeploymentConfiguration", "DevBoard_MCU_ESP32S3_N8R32V.json"),
                AddDeploymentConfiguration("COM11", Path.Combine(testBaseDirectory, "DeploymentConfiguration", "DevBoard_MCU_ESP32S3_DevKitM.json"),
                new TestFrameworkConfiguration()
                {
                    AllowRealHardware = false,
                    AllowSerialPorts = new List<string>() { "COM42" },
                    ExcludeSerialPorts = new List<string>() { "COM30", "COM31" },
                    RealHardwareTimeout = 120000,
                    PathToLocalNanoCLR = Path.Combine(globalDirectory, "clr", "nanoclr.exe"),
                    CLRVersion = "v1.0",
                    PathToLocalCLRInstance = Path.Combine(globalDirectory, "clr", "custom.bin"),
                    MaxVirtualDevices = 8,
                    VirtualDeviceTimeout = 10000,
                    Logging = LoggingLevel.Detailed
                })),
                actual,
                globalDirectory, solutionDirectory
                );

            AssertEffectiveSettings(actual, Path.Combine(testBaseDirectory, "sub", "sub"),
                $@"<?xml version=""1.0"" encoding=""utf-8""?>
<RunSettings>
    <nanoFrameworkAdapter>
        <AllowRealHardware>false</AllowRealHardware>
        <PathToLocalNanoCLR>..\..\nanoFramework\clr\nanoclr.exe</PathToLocalNanoCLR>
        <CLRVersion>v1.0</CLRVersion>
        <PathToLocalCLRInstance>..\..\nanoFramework\clr\custom.bin</PathToLocalCLRInstance>
        <MaxVirtualDevices>8</MaxVirtualDevices>
        <VirtualDeviceTimeout>10000</VirtualDeviceTimeout>
        <Logging>Detailed</Logging>
    </nanoFrameworkAdapter>
</RunSettings>",
                null);
            #endregion

            #region Global + solution + project directory
            logger = new LogMessengerMock();
            actual = TestFrameworkConfiguration.Read(projectDirectory, false, logger);
            logger.AssertEqual("", LoggingLevel.Verbose);
            AssertConfiguration(
                AddDeploymentConfiguration("COM11", Path.Combine(testBaseDirectory, "DeploymentConfiguration", "DevBoard_MCU_ESP32S3_DevKitM.json"),
                AddDeploymentConfiguration("COM42", Path.Combine(testBaseDirectory, "DeploymentConfiguration", "DevBoard_OV2640.json"),
                new TestFrameworkConfiguration()
                {
                    AllowRealHardware = true,
                    AllowSerialPorts = new List<string>() { "COM11", "COM42" },
                    ExcludeSerialPorts = new List<string>() { "COM30", "COM31" },
                    RealHardwareTimeout = 120000,
                    PathToLocalNanoCLR = Path.Combine(globalDirectory, "clr", "nanoclr.exe"),
                    CLRVersion = "v1.0",
                    PathToLocalCLRInstance = Path.Combine(globalDirectory, "clr", "custom.bin"),
                    MaxVirtualDevices = 8,
                    VirtualDeviceTimeout = 10000,
                    Logging = LoggingLevel.Detailed
                })),
                actual,
                globalDirectory, solutionDirectory, projectDirectory
                );

            AssertEffectiveSettings(actual, testBaseDirectory,
                $@"<?xml version=""1.0"" encoding=""utf-8""?>
<RunSettings>
    <nanoFrameworkAdapter>
        <RealHardwareTimeout>120000</RealHardwareTimeout>
        <PathToLocalNanoCLR>nanoFramework\clr\nanoclr.exe</PathToLocalNanoCLR>
        <CLRVersion>v1.0</CLRVersion>
        <PathToLocalCLRInstance>nanoFramework\clr\custom.bin</PathToLocalCLRInstance>
        <MaxVirtualDevices>8</MaxVirtualDevices>
        <VirtualDeviceTimeout>10000</VirtualDeviceTimeout>
        <Logging>Detailed</Logging>
    </nanoFrameworkAdapter>
</RunSettings>",
                $@"<?xml version=""1.0"" encoding=""utf-8""?>
<RunSettings>
    <nanoFrameworkAdapter>
        <AllowSerialPorts>COM11;COM42</AllowSerialPorts>
        <ExcludeSerialPorts>COM30;COM31</ExcludeSerialPorts>
        <DeploymentConfiguration>
            <SerialPort>COM11</SerialPort>
            <File>DeploymentConfiguration\DevBoard_MCU_ESP32S3_DevKitM.json</File>
        </DeploymentConfiguration>
        <DeploymentConfiguration>
            <SerialPort>COM42</SerialPort>
            <File>DeploymentConfiguration\DevBoard_OV2640.json</File>
        </DeploymentConfiguration>
    </nanoFrameworkAdapter>
</RunSettings>");
            #endregion
        }

        [TestMethod]
        public void TestFrameworkConfiguration_SaveEffectiveSettings()
        {
            string testDirectory = TestDirectoryHelper.GetTestDirectory(TestContext);

            #region No hardware; hardware-related settings are removed
            TestFrameworkConfiguration actual = new TestFrameworkConfiguration()
            {
                AllowRealHardware = false,
                AllowSerialPorts = new List<string>() { "COM9", "COM42" },
                ExcludeSerialPorts = new List<string>() { "COM32", "COM31", "COM42" },
                RealHardwareTimeout = 10000,
                PathToLocalNanoCLR = "nanoclr.exe",
                CLRVersion = "v1.0",
                PathToLocalCLRInstance = "nanoclr.bin",
                MaxVirtualDevices = 4,
                VirtualDeviceTimeout = 5000,
                Logging = LoggingLevel.Detailed
            }.SetDeploymentConfigurationFilePath("COM42", "dc_com42.json")
            .SetDeploymentConfigurationFilePath("COM1", "dc_com1.json");
            var logger = new LogMessengerMock();
            actual.SaveEffectiveSettings(testDirectory, logger);

            logger.AssertEqual("");
            Assert.IsTrue(File.Exists(Path.Combine(testDirectory, TestFrameworkConfiguration.SettingsFileName)));
            Assert.IsFalse(File.Exists(Path.Combine(testDirectory, TestFrameworkConfiguration.UserSettingsFileName)));

            logger = new LogMessengerMock();
            var read = TestFrameworkConfiguration.Read(testDirectory, false, logger);
            logger.AssertEqual("");

            AssertConfiguration(new TestFrameworkConfiguration()
            {
                AllowRealHardware = false,
                PathToLocalNanoCLR = Path.Combine(testDirectory, "nanoclr.exe"),
                CLRVersion = "v1.0",
                PathToLocalCLRInstance = Path.Combine(testDirectory, "nanoclr.bin"),
                MaxVirtualDevices = 4,
                VirtualDeviceTimeout = 5000,
                Logging = LoggingLevel.Detailed
            },
            read);
            #endregion

            #region With hardware; hardware-related settings are removed
            actual.AllowRealHardware = true;
            logger = new LogMessengerMock();
            actual.SaveEffectiveSettings(testDirectory, logger);

            logger.AssertEqual("");
            Assert.IsTrue(File.Exists(Path.Combine(testDirectory, TestFrameworkConfiguration.SettingsFileName)));
            Assert.IsTrue(File.Exists(Path.Combine(testDirectory, TestFrameworkConfiguration.UserSettingsFileName)));

            logger = new LogMessengerMock();
            read = TestFrameworkConfiguration.Read(testDirectory, false, logger);
            logger.AssertEqual("");

            AssertConfiguration(new TestFrameworkConfiguration()
            {
                AllowRealHardware = true,
                AllowSerialPorts = new List<string>() { "COM9", "COM42" },
                ExcludeSerialPorts = new List<string>() { "COM32", "COM31" },
                RealHardwareTimeout = 10000,
                PathToLocalNanoCLR = Path.Combine(testDirectory, "nanoclr.exe"),
                CLRVersion = "v1.0",
                PathToLocalCLRInstance = Path.Combine(testDirectory, "nanoclr.bin"),
                MaxVirtualDevices = 4,
                VirtualDeviceTimeout = 5000,
                Logging = LoggingLevel.Detailed
            }.SetDeploymentConfigurationFilePath("COM42", Path.Combine(testDirectory, "dc_com42.json")),
            read);
            #endregion

            #region Default settings: no files; existing files are removed
            actual = new TestFrameworkConfiguration();
            logger = new LogMessengerMock();
            actual.SaveEffectiveSettings(testDirectory, logger);

            logger.AssertEqual("");
            Assert.IsFalse(File.Exists(Path.Combine(testDirectory, TestFrameworkConfiguration.SettingsFileName)));
            Assert.IsFalse(File.Exists(Path.Combine(testDirectory, TestFrameworkConfiguration.UserSettingsFileName)));
            #endregion
        }

        [TestMethod]
        public void TestFrameworkConfiguration_SaveSettings()
        {
            string testDirectoryPath = TestDirectoryHelper.GetTestDirectory(TestContext);
            string globalRelativePath = "global";
            string globalDirectoryPath = Path.Combine(testDirectoryPath, globalRelativePath);
            string projectRelativePath = "project";
            string projectDirectoryPath = Path.Combine(testDirectoryPath, projectRelativePath);

            #region Save global settings
            TestFrameworkConfiguration globalSettings = new TestFrameworkConfiguration()
            {
                AllowRealHardware = false,
                AllowSerialPorts = new List<string>() { "COM9", "COM42" },
                ExcludeSerialPorts = new List<string>() { "COM32", "COM31", "COM42" },
                RealHardwareTimeout = 10000,
                PathToLocalNanoCLR = "nanoclr.exe",
                CLRVersion = "v1.0",
                PathToLocalCLRInstance = "nanoclr.bin",
                MaxVirtualDevices = 4,
                VirtualDeviceTimeout = 5000,
                Logging = LoggingLevel.Detailed
            }.SetDeploymentConfigurationFilePath("COM42", "dc_com42.json")
            .SetDeploymentConfigurationFilePath("COM11", "dc_com11.json");

            var logger = new LogMessengerMock();
            globalSettings.SaveSettings(globalDirectoryPath, null, logger);

            logger.AssertEqual("");
            Assert.IsTrue(File.Exists(Path.Combine(globalDirectoryPath, TestFrameworkConfiguration.SettingsFileName)));
            Assert.IsTrue(File.Exists(Path.Combine(globalDirectoryPath, TestFrameworkConfiguration.UserSettingsFileName)));

            logger = new LogMessengerMock();
            var read = TestFrameworkConfiguration.Read(globalDirectoryPath, false, logger);
            logger.AssertEqual("");

            AssertConfiguration(new TestFrameworkConfiguration()
            {
                AllowRealHardware = false,
                AllowSerialPorts = new List<string>() { "COM9", "COM42" },
                ExcludeSerialPorts = new List<string>() { "COM32", "COM31", "COM42" },
                RealHardwareTimeout = 10000,
                PathToLocalNanoCLR = Path.Combine(globalDirectoryPath, "nanoclr.exe"),
                CLRVersion = "v1.0",
                PathToLocalCLRInstance = Path.Combine(globalDirectoryPath, "nanoclr.bin"),
                MaxVirtualDevices = 4,
                VirtualDeviceTimeout = 5000,
                Logging = LoggingLevel.Detailed
            }.SetDeploymentConfigurationFilePath("COM42", Path.Combine(globalDirectoryPath, "dc_com42.json"))
            .SetDeploymentConfigurationFilePath("COM11", Path.Combine(globalDirectoryPath, "dc_com11.json")),
            read);
            #endregion

            #region Save same settings as project settings
            logger = new LogMessengerMock();
            TestFrameworkConfiguration projectSettings = new TestFrameworkConfiguration()
            {
                AllowRealHardware = false,
                AllowSerialPorts = new List<string>() { "COM9", "COM42" },
                ExcludeSerialPorts = new List<string>() { "COM32", "COM31", "COM42" },
                RealHardwareTimeout = 10000,
                PathToLocalNanoCLR = $"../{globalRelativePath}/nanoclr.exe",
                CLRVersion = "v1.0",
                PathToLocalCLRInstance = $"../{globalRelativePath}/nanoclr.bin",
                MaxVirtualDevices = 4,
                VirtualDeviceTimeout = 5000,
                Logging = LoggingLevel.Detailed
            }.SetDeploymentConfigurationFilePath("COM42", $"../{globalRelativePath}/dc_com42.json")
            .SetDeploymentConfigurationFilePath("COM11", $"../{globalRelativePath}/dc_com11.json");

            projectSettings.SaveSettings(projectDirectoryPath, globalDirectoryPath, logger);

            logger.AssertEqual("");
            Assert.IsTrue(File.Exists(Path.Combine(projectDirectoryPath, TestFrameworkConfiguration.SettingsFileName)));
            Assert.IsFalse(File.Exists(Path.Combine(projectDirectoryPath, TestFrameworkConfiguration.UserSettingsFileName)));

            logger = new LogMessengerMock();
            read = TestFrameworkConfiguration.Read(projectDirectoryPath, false, logger);
            logger.AssertEqual("");

            AssertConfiguration(new TestFrameworkConfiguration()
            {
                AllowRealHardware = false,
                AllowSerialPorts = new List<string>() { "COM9", "COM42" },
                ExcludeSerialPorts = new List<string>() { "COM32", "COM31", "COM42" },
                RealHardwareTimeout = 10000,
                PathToLocalNanoCLR = Path.Combine(globalDirectoryPath, "nanoclr.exe"),
                CLRVersion = "v1.0",
                PathToLocalCLRInstance = Path.Combine(globalDirectoryPath, "nanoclr.bin"),
                MaxVirtualDevices = 4,
                VirtualDeviceTimeout = 5000,
                Logging = LoggingLevel.Detailed
            }.SetDeploymentConfigurationFilePath("COM42", Path.Combine(globalDirectoryPath, "dc_com42.json"))
            .SetDeploymentConfigurationFilePath("COM11", Path.Combine(globalDirectoryPath, "dc_com11.json")),
            read);
            #endregion

            #region Overwrite global settings with defaults
            new TestFrameworkConfiguration();
            logger = new LogMessengerMock();
            new TestFrameworkConfiguration().SaveSettings(globalDirectoryPath, null, logger);

            logger.AssertEqual("");
            Assert.IsFalse(File.Exists(Path.Combine(testDirectoryPath, TestFrameworkConfiguration.SettingsFileName)));
            Assert.IsFalse(File.Exists(Path.Combine(testDirectoryPath, TestFrameworkConfiguration.UserSettingsFileName)));

            // Read the defaults back via the project-specific settings
            logger = new LogMessengerMock();
            read = TestFrameworkConfiguration.Read(projectDirectoryPath, false, logger);
            logger.AssertEqual("");

            AssertConfiguration(new TestFrameworkConfiguration(), read);
            #endregion

            #region Save different settings as project settings
            // Restore the global settings
            globalSettings.SaveSettings(globalDirectoryPath, null, null);

            logger = new LogMessengerMock();
            projectSettings = new TestFrameworkConfiguration()
            {
                AllowRealHardware = true,
                AllowSerialPorts = new List<string>() { "COM11", "COM42" },
                ExcludeSerialPorts = new List<string>() { "COM32", "COM31" },
                RealHardwareTimeout = 8000,
                PathToLocalNanoCLR = "nanoclr.exe",
                CLRVersion = "v1.1",
                PathToLocalCLRInstance = "nanoclr.bin",
                MaxVirtualDevices = 0,
                VirtualDeviceTimeout = 7000,
                Logging = LoggingLevel.Verbose
            }.SetDeploymentConfigurationFilePath("COM42", "dc_com42.json")
            .SetDeploymentConfigurationFilePath("COM9", "dc_com9.json");

            projectSettings.SaveSettings(projectDirectoryPath, globalDirectoryPath, logger);

            logger = new LogMessengerMock();
            read = TestFrameworkConfiguration.Read(projectDirectoryPath, false, logger);
            logger.AssertEqual("");

            AssertConfiguration(new TestFrameworkConfiguration()
            {
                AllowRealHardware = true,
                AllowSerialPorts = new List<string>() { "COM11", "COM42" },
                ExcludeSerialPorts = new List<string>() { "COM32", "COM31" },
                RealHardwareTimeout = 8000,
                PathToLocalNanoCLR = Path.Combine(projectDirectoryPath, "nanoclr.exe"),
                CLRVersion = "v1.1",
                PathToLocalCLRInstance = Path.Combine(projectDirectoryPath, "nanoclr.bin"),
                MaxVirtualDevices = 0,
                VirtualDeviceTimeout = 7000,
                Logging = LoggingLevel.Verbose
            }.SetDeploymentConfigurationFilePath("COM42", Path.Combine(projectDirectoryPath, "dc_com42.json"))
            .SetDeploymentConfigurationFilePath("COM9", Path.Combine(projectDirectoryPath, "dc_com9.json")),
            read);
            #endregion

            #region Overwrite global settings with defaults
            new TestFrameworkConfiguration();
            logger = new LogMessengerMock();
            new TestFrameworkConfiguration().SaveSettings(globalDirectoryPath, null, logger);

            logger.AssertEqual("");
            Assert.IsFalse(File.Exists(Path.Combine(testDirectoryPath, TestFrameworkConfiguration.SettingsFileName)));
            Assert.IsFalse(File.Exists(Path.Combine(testDirectoryPath, TestFrameworkConfiguration.UserSettingsFileName)));

            // Read the project-specific settings
            logger = new LogMessengerMock();
            read = TestFrameworkConfiguration.Read(projectDirectoryPath, false, logger);
            logger.AssertEqual("");

            AssertConfiguration(new TestFrameworkConfiguration()
            {
                AllowRealHardware = true,
                AllowSerialPorts = new List<string>() { "COM11", "COM42" },
                ExcludeSerialPorts = new List<string>() { "COM32", "COM31" },
                RealHardwareTimeout = 8000,
                PathToLocalNanoCLR = Path.Combine(projectDirectoryPath, "nanoclr.exe"),
                CLRVersion = "v1.1",
                PathToLocalCLRInstance = Path.Combine(projectDirectoryPath, "nanoclr.bin"),
                MaxVirtualDevices = 0,
                VirtualDeviceTimeout = 7000,
                Logging = LoggingLevel.Verbose
            }.SetDeploymentConfigurationFilePath("COM42", Path.Combine(projectDirectoryPath, "dc_com42.json"))
            .SetDeploymentConfigurationFilePath("COM9", Path.Combine(projectDirectoryPath, "dc_com9.json")),
            read);
            #endregion
        }

        #region Helpers
        private static void AssertConfiguration(TestFrameworkConfiguration expected, TestFrameworkConfiguration actual, params string[] expectedHierarchyDirectoryPaths)
        {
            Assert.IsNotNull(actual);
            Assert.AreEqual(expected.AllowRealHardware, actual.AllowRealHardware);
            Assert.AreEqual(
                string.Join(",", from sp in expected.AllowSerialPorts
                                 orderby sp
                                 select sp),
                string.Join(",", from sp in actual.AllowSerialPorts
                                 orderby sp
                                 select sp)
            );
            Assert.AreEqual(
                string.Join(",", from sp in expected.ExcludeSerialPorts
                                 orderby sp
                                 select sp),
                string.Join(",", from sp in actual.ExcludeSerialPorts
                                 orderby sp
                                 select sp)
            );
            Assert.AreEqual(expected.RealHardwareTimeout, actual.RealHardwareTimeout);
            Assert.AreEqual(expected.PathToLocalNanoCLR, actual.PathToLocalNanoCLR);
            Assert.AreEqual(expected.CLRVersion, actual.CLRVersion);
            Assert.AreEqual(expected.PathToLocalCLRInstance, actual.PathToLocalCLRInstance);
            Assert.AreEqual(expected.MaxVirtualDevices, actual.MaxVirtualDevices);
            Assert.AreEqual(expected.VirtualDeviceTimeout, actual.VirtualDeviceTimeout);
            Assert.AreEqual(expected.Logging, actual.Logging);
            foreach (string port in s_knownSerialPorts)
            {
                Assert.AreEqual(expected.DeploymentConfigurationFilePath(port), actual.DeploymentConfigurationFilePath(port));
            }
            if (expectedHierarchyDirectoryPaths.Length > 0)
            {
                Assert.AreEqual(
                    string.Join("", from p in expectedHierarchyDirectoryPaths select $"{p}\n"),
                    string.Join("", from p in actual.ConfigurationHierarchyDirectoryPaths select $"{p}\n")
                );
            }
        }

        private static TestFrameworkConfiguration AddDeploymentConfiguration(string serialPort, string filePath, TestFrameworkConfiguration expected)
        {
            expected.SetDeploymentConfigurationFilePath(serialPort, filePath);
            return expected;
        }

        private static void AssertEffectiveSettings(TestFrameworkConfiguration actual, string saveInDirectoryPath, string expectedSettingsFileXml, string expectedUserSettingsFileXml)
        {
            var logger = new LogMessengerMock();
            actual.SaveEffectiveSettings(saveInDirectoryPath, logger);
            logger.AssertEqual("", LoggingLevel.Error);

            string expectedFilePath = Path.Combine(saveInDirectoryPath, TestFrameworkConfiguration.SettingsFileName);
            string expectedUserFilePath = Path.Combine(saveInDirectoryPath, TestFrameworkConfiguration.UserSettingsFileName);

            Assert.AreEqual(!(expectedSettingsFileXml is null), File.Exists(expectedFilePath));
            Assert.AreEqual(!(expectedUserSettingsFileXml is null), File.Exists(expectedUserFilePath));

            if (!(expectedSettingsFileXml is null))
            {
                Assert.AreEqual(
                    expectedSettingsFileXml.Trim().Replace("\r\n", "\n") + '\n',
                    File.ReadAllText(expectedFilePath).Trim().Replace("\r\n", "\n") + '\n'
                );
            }

            if (!(expectedUserSettingsFileXml is null))
            {
                Assert.AreEqual(
                    expectedUserSettingsFileXml.Trim().Replace("\r\n", "\n") + '\n',
                    File.ReadAllText(expectedUserFilePath).Trim().Replace("\r\n", "\n") + '\n'
                );
            }
        }
        #endregion
    }
}
