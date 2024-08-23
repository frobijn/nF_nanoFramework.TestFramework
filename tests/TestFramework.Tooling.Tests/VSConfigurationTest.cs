// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using nanoFramework.TestFramework.Tooling;
using TestFramework.Tooling.Tests.Helpers;

namespace TestFramework.Tooling.Tests
{
    [TestClass]
    [TestCategory("Test execution")]
    public sealed class VSTestConfigurationTest
    {
        public TestContext TestContext { get; set; }

        [TestMethod]
        [TestCategory("Migration v2 to v3")]
        public void MigrateToCustomSettings()
        {
            string testBaseDirectory = TestDirectoryHelper.GetTestDirectory(TestContext);

            #region nano.runsettings does not exist
            var logger = new LogMessengerMock();
            var actual = VSTestConfiguration.Create(Path.Combine(testBaseDirectory, "DoesNotExist.runsettings"), logger);
            logger.AssertEqual("", LoggingLevel.Error);
            Assert.IsNotNull(actual);

            string expectedFilePath = Path.Combine(testBaseDirectory, VSTestConfiguration.VSTestSettingsFileName);
            logger = new LogMessengerMock();
            actual.SaveCustomSettings(testBaseDirectory, logger);
            logger.AssertEqual("", LoggingLevel.Error);
            Assert.IsFalse(File.Exists(expectedFilePath));
            #endregion

            #region nano.runsettings only has properties assigned by the framework and nanoFrameworkAdapter that are not kept
            string noCustomSettingsPath = Path.Combine(testBaseDirectory, "NoCustomSettings.runsettings");
            File.WriteAllText(noCustomSettingsPath, @"<?xml version=""1.0"" encoding=""utf-8""?>
<RunSettings>
    <RunConfiguration>
        <MaxCpuCount>1</MaxCpuCount>
        <TargetFrameworkVersion>net48</TargetFrameworkVersion>
        <TargetPlatform>x64</TargetPlatform>
        <TestAdaptersPaths>E:\GitHub\nf-nanoFramework.TestFramework\source\TestAdapter\bin\Debug\net4.8;E:\GitHub\nf-nanoFramework.TestFramework\source\UnitTestLauncher\bin\Debug</TestAdaptersPaths>
    </RunConfiguration>
    <nanoFrameworkAdapter>
        <Logging>None</Logging>
        <IsRealHardware>False</IsRealHardware>
    </nanoFrameworkAdapter>
</RunSettings>");

            actual = VSTestConfiguration.Create(noCustomSettingsPath, logger);
            logger.AssertEqual("", LoggingLevel.Error);
            Assert.IsNotNull(actual);

            File.WriteAllText(expectedFilePath, "Assert that the file is deleted");

            logger = new LogMessengerMock();
            actual.SaveCustomSettings(testBaseDirectory, logger);
            logger.AssertEqual("", LoggingLevel.Error);
            Assert.IsFalse(File.Exists(expectedFilePath));
            #endregion

            #region nano.runsettings has custom properties
            string customSettingsPath = Path.Combine(testBaseDirectory, "CustomSettings.runsettings");
            File.WriteAllText(customSettingsPath, $@"<?xml version=""1.0"" encoding=""utf-8""?>
<RunSettings>
    <RunConfiguration>
        <ResultsDirectory>{Path.Combine(testBaseDirectory, "..", "TestResults")}</ResultsDirectory>
        <TestSessionTimeout>120000</TestSessionTimeout>
    </RunConfiguration>
</RunSettings>");

            actual = VSTestConfiguration.Create(customSettingsPath, logger);
            logger.AssertEqual("", LoggingLevel.Error);
            Assert.IsNotNull(actual);

            logger = new LogMessengerMock();
            string testSubDirectory = Path.Combine(testBaseDirectory, "sub");
            expectedFilePath = Path.Combine(testSubDirectory, VSTestConfiguration.VSTestSettingsFileName);
            actual.SaveCustomSettings(testSubDirectory, logger);
            logger.AssertEqual("", LoggingLevel.Error);
            Assert.IsTrue(File.Exists(expectedFilePath));
            Assert.AreEqual(@"<?xml version=""1.0"" encoding=""utf-8""?>
<RunSettings>
    <RunConfiguration>
        <ResultsDirectory>..\..\TestResults</ResultsDirectory>
        <TestSessionTimeout>120000</TestSessionTimeout>
    </RunConfiguration>
</RunSettings>
".Replace("\r\n", "\n"),
                File.ReadAllText(expectedFilePath).Trim().Replace("\r\n", "\n") + '\n'
            );
            #endregion
        }

        [TestMethod]
        public void ReadSettingsFromHierarchyAndSaveSettings()
        {
            string testBaseDirectory = TestDirectoryHelper.GetTestDirectory(TestContext);

            string settingsDirectoryTop = Path.Combine(testBaseDirectory, "Repository");
            Directory.CreateDirectory(settingsDirectoryTop);
            File.WriteAllText(Path.Combine(settingsDirectoryTop, VSTestConfiguration.VSTestSettingsFileName), $@"<?xml version=""1.0"" encoding=""utf-8""?>
<RunSettings>
    <RunConfiguration>
        <ResultsDirectory>{Path.Combine(settingsDirectoryTop, "TestResults")}</ResultsDirectory>
        <TestSessionTimeout>1000</TestSessionTimeout>
    </RunConfiguration>
</RunSettings>");

            string settingsDirectorySolution = Path.Combine(testBaseDirectory, "Solution");
            Directory.CreateDirectory(settingsDirectorySolution);
            File.WriteAllText(Path.Combine(settingsDirectorySolution, VSTestConfiguration.VSTestSettingsFileName), @"<?xml version=""1.0"" encoding=""utf-8""?>
<RunSettings>
    <RunConfiguration>
        <TestCaseFilter>Filter_Solution</TestCaseFilter>
        <TestSessionTimeout>2000</TestSessionTimeout>
    </RunConfiguration>
</RunSettings>");

            string settingsDirectoryProject = Path.Combine(testBaseDirectory, "Solution", "Project");
            Directory.CreateDirectory(settingsDirectoryProject);
            File.WriteAllText(Path.Combine(settingsDirectoryProject, VSTestConfiguration.VSTestSettingsFileName), @"<?xml version=""1.0"" encoding=""utf-8""?>
<RunSettings>
    <RunConfiguration>
        <TreatNoTestsAsError>false</TreatNoTestsAsError>
    </RunConfiguration>
</RunSettings>");

            string outputDirectoryProject = Path.Combine(testBaseDirectory, "Solution", "Project", "bin", "Debug");
            string adapterPath = Path.Combine(testBaseDirectory, "TestAdapter", "ta.dll");
            string expectedFilePath = Path.Combine(outputDirectoryProject, VSTestConfiguration.VSTestSettingsFileName);

            var logger = new LogMessengerMock();
            var actual = VSTestConfiguration.Create(new string[] { settingsDirectoryTop, settingsDirectorySolution, settingsDirectoryProject }, logger);
            logger.AssertEqual("", LoggingLevel.Error);
            Assert.IsNotNull(actual);

            actual.SaveAllSettings(outputDirectoryProject, adapterPath);
            Assert.IsTrue(File.Exists(expectedFilePath));
            Assert.AreEqual($@"<?xml version=""1.0"" encoding=""utf-8""?>
<RunSettings>
    <RunConfiguration>
        <ResultsDirectory>{Path.Combine(settingsDirectoryTop, "TestResults")}</ResultsDirectory>
        <TestCaseFilter>Filter_Solution</TestCaseFilter>
        <TestSessionTimeout>2000</TestSessionTimeout>
        <TreatNoTestsAsError>false</TreatNoTestsAsError>
        <MaxCpuCount>1</MaxCpuCount>
        <TargetFrameworkVersion>net48</TargetFrameworkVersion>
        <TargetPlatform>x64</TargetPlatform>
        <TestAdaptersPaths>{adapterPath}</TestAdaptersPaths>
    </RunConfiguration>
</RunSettings>
".Replace("\r\n", "\n"),
                File.ReadAllText(expectedFilePath).Trim().Replace("\r\n", "\n") + '\n'
            );
        }
    }
}
