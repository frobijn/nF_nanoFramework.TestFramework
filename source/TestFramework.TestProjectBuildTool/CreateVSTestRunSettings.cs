// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace nanoFramework.TestFramework.TestProjectBuildTool
{
    public class CreateVSTestRunSettings : Task
    {
        #region Input parameters
        /// <summary>
        /// The full path to the test adapter *.dll file
        /// </summary>
        [Required]
        public string TestAdapterFilePath { get; set; }

        /// <summary>
        /// The full path to the project directory.
        /// Corresponds to the MSBuild $(MSBuildProjectDirectory) property.
        /// </summary>
        [Required]
        public string ProjectDirectory { get; set; }

        /// <summary>
        /// The path of the output directory, relative to the project directory.
        /// Corresponds to the MSBuild $(OutputPath) property.
        /// </summary>
        [Required]
        public string OutputPath { get; set; }

        /// <summary>
        /// The name of the .runsettings file to produce.
        /// </summary>
        public string RunSettingsFileName { get; set; } = "nano.vstest.runsettings";
        #endregion

        #region Task execution
        /// <inheritdoc/>
        public override bool Execute()
        {
            string fullPath = Path.GetFullPath(TestAdapterFilePath);
            if (!File.Exists(fullPath))
            {
                Log.LogError($"The test adapter is not found: '{fullPath}'");
                return false;
            }
            else
            {
                string runSettingsFilePath = Path.Combine(ProjectDirectory, OutputPath, RunSettingsFileName);
                Directory.CreateDirectory(Path.GetDirectoryName(runSettingsFilePath));
                File.WriteAllText(runSettingsFilePath,
$@"<?xml version=""1.0"" encoding=""utf-8""?>
<RunSettings>
    <RunConfiguration>
        <MaxCpuCount>1</MaxCpuCount>
        <TargetFrameworkVersion>net48</TargetFrameworkVersion>
        <TargetPlatform>x64</TargetPlatform>
        <TestAdaptersPaths>{fullPath}</TestAdaptersPaths>
    </RunConfiguration>
</RunSettings>");
                return true;
            }
        }
        #endregion
    }
}
