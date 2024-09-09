// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using nanoFramework.TestFramework.TestProjectBuildTool;
using nanoFramework.TestFramework.Tooling;
using TestFramework.Tooling.BuildTools.Tests.Helpers;

namespace TestFramework.Tooling.BuildTools.Tests
{
    [TestClass]
    [TestCategory("Test execution")]
    [TestCategory("MSBuild")]
    public class TestProjectBuildToolTest
    {
        public TestContext TestContext { get; set; }

        [TestMethod]
        public void TestProject_MigrateTestProjectAndNanoRunSettings_Project()
        {
            #region Setup
            string testDirectory = TestDirectoryHelper.GetTestDirectory(TestContext);
            string projectFilePath = Path.Combine(testDirectory, "TestProject.nfproj");
            File.WriteAllText(projectFilePath, @"<?xml version=""1.0"" encoding=""utf-8""?>
<Project ToolsVersion=""Current"" DefaultTargets=""Build"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
  <PropertyGroup Label=""Globals"">
    <NanoFrameworkProjectSystemPath>$(MSBuildExtensionsPath)\nanoFramework\v1.0\</NanoFrameworkProjectSystemPath>
  </PropertyGroup>
  <Import Project=""$(NanoFrameworkProjectSystemPath)NFProjectSystem.Default.props"" Condition=""Exists('$(NanoFrameworkProjectSystemPath)NFProjectSystem.Default.props')"" />
  <ItemGroup>
    <ProjectCapability Include=""TestContainer"" />
  </ItemGroup>
  <PropertyGroup>
    <Configuration Condition="" '$(Configuration)' == '' "">Debug</Configuration>
    <Platform Condition="" '$(Platform)' == '' "">AnyCPU</Platform>
    <ProjectTypeGuids>{11A8DD76-328B-46DF-9F39-F559912D0360};{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}</ProjectTypeGuids>
    <ProjectGuid>fbd29c49-d7dc-425e-bad1-1ae63484a6cd</ProjectGuid>
    <OutputType>Library</OutputType>
    <AppDesignerFolder>Properties</AppDesignerFolder>
    <FileAlignment>512</FileAlignment>
    <RootNamespace>NFUnitTest</RootNamespace>
    <AssemblyName>NFUnitTest</AssemblyName>
    <IsCodedUITest>False</IsCodedUITest>
    <IsTestProject>true</IsTestProject>
    <TestProjectType>UnitTest</TestProjectType>
    <TargetFrameworkVersion>v4.0</TargetFrameworkVersion>
  </PropertyGroup>
  <Import Project=""$(NanoFrameworkProjectSystemPath)NFProjectSystem.props"" Condition=""Exists('$(NanoFrameworkProjectSystemPath)NFProjectSystem.props')"" />
  <PropertyGroup>
    <RunSettingsFilePath>$(MSBuildProjectDirectory)\nano.runsettings</RunSettingsFilePath>
  </PropertyGroup>
  <ItemGroup>
    <Compile Include=""Test.cs"" />
    <Compile Include=""Properties\AssemblyInfo.cs"" />
  </ItemGroup>
  <Import Project=""$(NanoFrameworkProjectSystemPath)NFProjectSystem.CSharp.targets"" Condition=""Exists('$(NanoFrameworkProjectSystemPath)NFProjectSystem.CSharp.targets')"" />
</Project>");
            #endregion

            var actual = new MigrateTestProjectAndNanoRunSettings()
            {
                ProjectFilePath = projectFilePath
            };
            var logger = new LogMessengerMock();

            Assert.IsFalse(actual.Execute(logger));

            #region Asserts
            logger.AssertEqual(
    $@"Error: The project '{projectFilePath}' has been updated - restart the build.",
                    LoggingLevel.Error);
            Assert.AreEqual(@"<?xml version=""1.0"" encoding=""utf-8""?>
<Project ToolsVersion=""Current"" DefaultTargets=""Build"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
  <PropertyGroup Label=""Globals"">
    <NanoFrameworkProjectSystemPath>$(MSBuildExtensionsPath)\nanoFramework\v1.0\</NanoFrameworkProjectSystemPath>
  </PropertyGroup>
  <Import Project=""$(NanoFrameworkProjectSystemPath)NFProjectSystem.Default.props"" Condition=""Exists('$(NanoFrameworkProjectSystemPath)NFProjectSystem.Default.props')"" />
  <ItemGroup>
    <ProjectCapability Include=""TestContainer"" />
  </ItemGroup>
  <PropertyGroup>
    <Configuration Condition="" '$(Configuration)' == '' "">Debug</Configuration>
    <Platform Condition="" '$(Platform)' == '' "">AnyCPU</Platform>
    <ProjectTypeGuids>{11A8DD76-328B-46DF-9F39-F559912D0360};{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}</ProjectTypeGuids>
    <ProjectGuid>fbd29c49-d7dc-425e-bad1-1ae63484a6cd</ProjectGuid>
    <OutputType>Library</OutputType>
    <AppDesignerFolder>Properties</AppDesignerFolder>
    <FileAlignment>512</FileAlignment>
    <RootNamespace>NFUnitTest</RootNamespace>
    <IsCodedUITest>False</IsCodedUITest>
    <IsTestProject>true</IsTestProject>
    <TestProjectType>UnitTest</TestProjectType>
    <TargetFrameworkVersion>v4.0</TargetFrameworkVersion>
  </PropertyGroup>
  <Import Project=""$(NanoFrameworkProjectSystemPath)NFProjectSystem.props"" Condition=""Exists('$(NanoFrameworkProjectSystemPath)NFProjectSystem.props')"" />
  <ItemGroup>
    <Compile Include=""Test.cs"" />
    <Compile Include=""Properties\AssemblyInfo.cs"" />
  </ItemGroup>
  <Import Project=""$(NanoFrameworkProjectSystemPath)NFProjectSystem.CSharp.targets"" Condition=""Exists('$(NanoFrameworkProjectSystemPath)NFProjectSystem.CSharp.targets')"" />
</Project>"
.Trim().Replace("\r\n", "\n") + '\n',
            File.ReadAllText(projectFilePath).Trim().Replace("\r\n", "\n") + '\n'
            );
            #endregion
        }

        [TestMethod]
        public void TestProject_MigrateTestProjectAndNanoRunSettings_NanoSettings()
        {
            #region Setup
            string testDirectory = TestDirectoryHelper.GetTestDirectory(TestContext);
            string projectFilePath = Path.Combine(testDirectory, "TestProject.nfproj");
            File.WriteAllText(projectFilePath, @"<?xml version=""1.0"" encoding=""utf-8""?>
<Project ToolsVersion=""Current"" DefaultTargets=""Build"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
</Project>");
            string nanoSettingsFilePath = Path.Combine(testDirectory, TestFrameworkConfiguration.SettingsFileName);
            File.WriteAllText(nanoSettingsFilePath, @"<?xml version=""1.0"" encoding=""utf-8""?>
<RunSettings>
   <RunConfiguration>
       <ResultsDirectory>.\TestResults</ResultsDirectory><!-- Path relative to solution directory -->
       <TestSessionTimeout>120000</TestSessionTimeout><!-- Milliseconds -->
       <TargetFrameworkVersion>net48</TargetFrameworkVersion>
       <TargetPlatform>x64</TargetPlatform>
   </RunConfiguration>
   <nanoFrameworkAdapter>
       <Logging>Verbose</Logging> <!--Set to the desired level of logging for Unit Test execution. Possible values are: None, Detailed, Verbose, Error. -->
       <IsRealHardware>False</IsRealHardware><!--Set to true to run tests on real hardware. -->
   </nanoFrameworkAdapter>
</RunSettings>");
            #endregion

            var actual = new MigrateTestProjectAndNanoRunSettings()
            {
                ProjectFilePath = projectFilePath
            };
            var logger = new LogMessengerMock();

            Assert.IsTrue(actual.Execute(logger));

            logger.AssertEqual("", LoggingLevel.Error);
            Assert.AreEqual(@"<?xml version=""1.0"" encoding=""utf-8""?>
<RunSettings>
    <nanoFrameworkAdapter>
        <AllowRealHardware>false</AllowRealHardware>
        <Logging>Verbose</Logging>
    </nanoFrameworkAdapter>
</RunSettings>
".Trim().Replace("\r\n", "\n") + '\n',
                File.ReadAllText(nanoSettingsFilePath).Trim().Replace("\r\n", "\n") + '\n'
            );
        }


        [TestMethod]
        public void TestProject_MigrateTestProjectAndNanoRunSettings_NoMigration()
        {
            #region Setup
            string testDirectory = TestDirectoryHelper.GetTestDirectory(TestContext);
            string projectFilePath = Path.Combine(testDirectory, "TestProject.nfproj");
            string projectFileXml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Project ToolsVersion=""Current"" DefaultTargets=""Build"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
  <PropertyGroup Label=""Globals"">
    <NanoFrameworkProjectSystemPath>$(MSBuildExtensionsPath)\nanoFramework\v1.0\</NanoFrameworkProjectSystemPath>
  </PropertyGroup>
  <Import Project=""$(NanoFrameworkProjectSystemPath)NFProjectSystem.Default.props"" Condition=""Exists('$(NanoFrameworkProjectSystemPath)NFProjectSystem.Default.props')"" />
  <ItemGroup>
    <ProjectCapability Include=""TestContainer"" />
  </ItemGroup>
  <PropertyGroup>
    <Configuration Condition="" '$(Configuration)' == '' "">Debug</Configuration>
    <Platform Condition="" '$(Platform)' == '' "">AnyCPU</Platform>
    <ProjectTypeGuids>{11A8DD76-328B-46DF-9F39-F559912D0360};{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}</ProjectTypeGuids>
    <ProjectGuid>fbd29c49-d7dc-425e-bad1-1ae63484a6cd</ProjectGuid>
    <OutputType>Library</OutputType>
    <AppDesignerFolder>Properties</AppDesignerFolder>
    <FileAlignment>512</FileAlignment>
    <RootNamespace>NFUnitTest</RootNamespace>
    <IsCodedUITest>False</IsCodedUITest>
    <IsTestProject>true</IsTestProject>
    <TestProjectType>UnitTest</TestProjectType>
    <TargetFrameworkVersion>v4.0</TargetFrameworkVersion>
  </PropertyGroup>
  <Import Project=""$(NanoFrameworkProjectSystemPath)NFProjectSystem.props"" Condition=""Exists('$(NanoFrameworkProjectSystemPath)NFProjectSystem.props')"" />
  <ItemGroup>
    <Compile Include=""Test.cs"" />
    <Compile Include=""Properties\AssemblyInfo.cs"" />
  </ItemGroup>
  <Import Project=""$(NanoFrameworkProjectSystemPath)NFProjectSystem.CSharp.targets"" Condition=""Exists('$(NanoFrameworkProjectSystemPath)NFProjectSystem.CSharp.targets')"" />
</Project>";
            File.WriteAllText(projectFilePath, projectFileXml);
            string nanoSettingsFilePath = Path.Combine(testDirectory, TestFrameworkConfiguration.SettingsName);
            string nanoSettingsXml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<RunSettings>
   <RunConfiguration>
       <ResultsDirectory>.\TestResults</ResultsDirectory><!-- Path relative to solution directory -->
       <TestSessionTimeout>120000</TestSessionTimeout><!-- Milliseconds -->
       <TargetFrameworkVersion>net48</TargetFrameworkVersion>
       <TargetPlatform>x64</TargetPlatform>
   </RunConfiguration>
   <nanoFrameworkAdapter>
       <Logging>None</Logging> <!--Set to the desired level of logging for Unit Test execution. Possible values are: None, Detailed, Verbose, Error. -->
       <IsRealHardware>False</IsRealHardware><!--Set to true to run tests on real hardware. -->
   </nanoFrameworkAdapter>
</RunSettings>";
            File.WriteAllText(nanoSettingsFilePath, nanoSettingsXml);
            #endregion

            var actual = new MigrateTestProjectAndNanoRunSettings()
            {
                ProjectFilePath = projectFilePath
            };
            var logger = new LogMessengerMock();

            Assert.IsTrue(actual.Execute(logger));

            logger.AssertEqual("", LoggingLevel.Error);
            Assert.AreEqual(projectFileXml, File.ReadAllText(projectFilePath));
            Assert.AreEqual(nanoSettingsXml, File.ReadAllText(nanoSettingsFilePath));
        }
    }
}
