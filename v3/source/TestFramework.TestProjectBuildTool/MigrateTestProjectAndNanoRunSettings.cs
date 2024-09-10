// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
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
    /// <summary>
    /// The test framework v2 uses a nano.runsettings file in the project directory of the test project.
    /// This task migrates the <see cref="TestFrameworkConfiguration.SettingsFileName"/> and
    /// <see cref="TestFrameworkConfiguration.UserSettingsFileName"/> in the project directory.
    /// It also modifies the <c>nfproj</c> project file to remove the reference to the nano.runsettings file
    /// and the fixed assembly name <c>NFUnitTest</c> 
    /// </summary>
    public class MigrateTestProjectAndNanoRunSettings : Task
    {
        #region Input parameters
        /// <summary>
        /// The full path to the project file.
        /// Corresponds to the MSBuild $(MSBuildProjectFullPath) property.
        /// </summary>
        [Required]
        public string ProjectFilePath { get; set; }

        /// <summary>
        /// The full path to the test adapter *.dll file
        /// </summary>
        public string TestAdapterFilePath { get; set; }

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
        /// <returns>Indicates whether the project was already migrated.
        /// Returns <c>false</c> if the project file has been changed.</returns>
        public bool Execute(LogMessenger logger)
        {
            #region Migrate nano.runsettings
            // The nano.runsettings file has to be migrated if it still contains a RunConfiguration
            bool migrateRunSettings = false;
            string projectDirectoryPath = Path.GetDirectoryName(ProjectFilePath);
            string oldNanoSettingsFilePath = Path.Combine(projectDirectoryPath, "nano.runsettings");
            if (File.Exists(oldNanoSettingsFilePath))
            {
                XDocument settings = null;
                try
                {
                    settings = XDocument.Load(oldNanoSettingsFilePath);
                }
                catch (Exception ex)
                {
                    logger?.Invoke(LoggingLevel.Warning, $"The file '{oldNanoSettingsFilePath}' cannot be parsed - migration is skipped: {ex.Message}");
                }
                if (!(settings?.Root is null))
                {
                    migrateRunSettings = settings.Root.Elements(settings.Root.Name.Namespace + "RunConfiguration").Any();
                }
            }
            if (migrateRunSettings)
            {
                var frameworkSettings = TestFrameworkConfiguration.Read(projectDirectoryPath, true, logger);
                frameworkSettings.SaveEffectiveSettings(projectDirectoryPath, logger);
            }
            #endregion

            #region Update the project file
            XDocument doc = null;
            try
            {
                doc = XDocument.Load(ProjectFilePath);
            }
            catch (Exception ex)
            {
                logger?.Invoke(LoggingLevel.Error, $"The project '{ProjectFilePath}' cannot be parsed - migration is skipped: {ex.Message}");
                return true;
            }
            bool isUpdated = false;
            foreach (XElement propertyGroup in doc.Root.Elements(doc.Root.Name.Namespace + "PropertyGroup").ToList())
            {
                foreach (XElement property in (from p in propertyGroup.Elements(doc.Root.Name.Namespace + "AssemblyName")
                                               where p.Value == "NFUnitTest"
                                               select p).ToList())
                {
                    property.Remove();
                    isUpdated = true;
                }
                foreach (XElement property in (from p in propertyGroup.Elements(doc.Root.Name.Namespace + "RunSettingsFilePath")
                                               select p).ToList())
                {
                    property.Remove();
                    isUpdated = true;
                }
                if (!propertyGroup.HasElements)
                {
                    propertyGroup.Remove();
                    isUpdated = true;
                }
            }
            if (isUpdated)
            {
                doc.Save(ProjectFilePath);
                logger?.Invoke(LoggingLevel.Error, $"The project '{ProjectFilePath}' has been updated - restart the build.");
                return false;
            }
            #endregion

            if (TestAdapterFilePath is null || (RunSettings?.Length ?? 0) == 0)
            {
                return true;
            }
            else
            {
                var task = new CreateVSTestRunSettings()
                {
                    TestAdapterFilePath = TestAdapterFilePath,
                    RunSettings = RunSettings,
                    ProjectDirectory = projectDirectoryPath,
                };
                return task.Execute(logger);
            }
        }
        #endregion
    }
}
