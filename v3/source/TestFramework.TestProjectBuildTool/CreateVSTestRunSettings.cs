// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using nanoFramework.TestFramework.Tooling;

#if DEBUG
#if LAUNCHDEBUGGER
using System.Diagnostics;
#endif
#endif

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
        /// The path of the .runsettings file(s) to produce.
        /// </summary>
        [Required]
        public ITaskItem[] RunSettings { get; set; }
        #endregion

        #region Task execution
        /// <inheritdoc/>
        public override bool Execute()
        {
#if DEBUG
#if LAUNCHDEBUGGER
            if (!Debugger.IsAttached)
            {
                Debugger.Launch();
            }
#endif
#endif
            void logger(LoggingLevel level, string message)
            {
                if (level == LoggingLevel.Error)
                {
                    Log.LogError(message);
                }
            }
            return Execute(logger);
        }

        /// <summary>
        /// Execute the task
        /// </summary>
        /// <param name="logError">Method to log an error</param>
        public bool Execute(LogMessenger logger)
        {
            string fullPath = Path.GetFullPath(TestAdapterFilePath);
            if (!File.Exists(fullPath))
            {
                Log.LogError($"The test adapter is not found: {nameof(TestAdapterFilePath)} = '{fullPath}'");
                return false;
            }
            else if (RunSettings.Length == 0)
            {
                Log.LogError($"No {nameof(RunSettings)} specified");
                return false;
            }
            else
            {
                foreach (var runSettingPath in from r in RunSettings
                                               select r.ItemSpec)
                {
                    string runSettingsFilePath = Path.Combine(ProjectDirectory, runSettingPath);
                    Directory.CreateDirectory(Path.GetDirectoryName(runSettingsFilePath));
                    File.WriteAllText(runSettingsFilePath,
    $@"<?xml version=""1.0"" encoding=""utf-8""?>
<RunSettings>
    <RunConfiguration>
        <MaxCpuCount>1</MaxCpuCount>
        <TargetFrameworkVersion>net48</TargetFrameworkVersion>
        <TargetPlatform>x64</TargetPlatform>
        <TestAdaptersPaths>{Path.GetDirectoryName(fullPath)}</TestAdaptersPaths>
    </RunConfiguration>
</RunSettings>");
                }
                return true;
            }
        }

        #endregion
    }
}
