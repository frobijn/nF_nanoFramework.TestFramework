// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace nanoFramework.TestFramework.Tooling.MigrationFromV2
{
    /// <summary>
    /// The test framework v2 uses a nano.runsettings file in the project directory of the test project.
    /// This version uses a <see cref="VSTestConfiguration.VSTestSettingsFileName"/> file in the project directory
    /// to control the VSTest host, a <see cref="TestFrameworkConfiguration.SettingsFileName"/> and
    /// <see cref="TestFrameworkConfiguration.UserSettingsFileName"/> in the project directory to control the nanoFramework
    /// test platform. This class provides a migration script. It also modifies the <c>nfproj</c>
    /// project file to remove the reference to the nano.runsettings file and the fixed assembly name <c>NFUnitTest</c> 
    /// </summary>
    public static class MigrateUnitTestsProjectAndNanoRunsettings
    {
        /// <summary>
        /// The test framework v2 uses a nano.runsettings file in the project directory of the test project.
        /// This version uses a <see cref="VSTestConfiguration.VSTestSettingsFileName"/> file in the project directory
        /// to control the VSTest host, a <see cref="TestFrameworkConfiguration.SettingsFileName"/> and
        /// <see cref="TestFrameworkConfiguration.UserSettingsFileName"/> in the project directory to control the nanoFramework
        /// test platform.
        /// <para>
        /// This migration script reads an existing nano.runsettings file if the
        /// <see cref="VSTestConfiguration.VSTestSettingsFileName"/> does not exist, and splits its content in
        /// VSTest and nanoFramework-related settings. The settings are stored in the solution directory.
        /// If there are already configuration files in the solution directory, configuration files are
        /// kept in the project directory if settings are different. If <paramref name="solutionDirectoryPath"/>
        /// is <c>null</c>, all settings are stored in files in the project directory.
        /// </para>
        /// <para>
        /// In addition, the project file is scanned and the RunSettingsFilePath element is removed. That element
        /// is now set in the MSBuild file that is part of the TestFramework package. It also removes the AssemblyName element.
        /// </para>
        /// </summary>
        /// <param name="projectFilePath">The path to the project file for the </param>
        /// <param name="logger">Method to pass processing information to the caller. Pass <c>null</c> if no log information is required.</param>
        public static void Run(string projectFilePath, LogMessenger logger)
        {
            // No migration necessary if the VSTestConfiguration already exists
            string projectDirectoryPath = Path.GetDirectoryName(projectFilePath
                ?? throw new ArgumentNullException(nameof(projectFilePath)));

            string vsTestSettingsFilePath = Path.Combine(projectDirectoryPath, VSTestConfiguration.VSTestSettingsFileName);
            if (File.Exists(vsTestSettingsFilePath))
            {
                return;
            }

            #region Update the project file
            if (!File.Exists(projectFilePath))
            {
                logger?.Invoke(LoggingLevel.Error, $"Project file '{projectFilePath}' does not exist - migration is skipped");
                return;
            }
            XDocument doc = null;
            try
            {
                doc = XDocument.Load(vsTestSettingsFilePath);
            }
            catch (Exception ex)
            {
                logger?.Invoke(LoggingLevel.Error, $"The project '{projectFilePath}' cannot be parsed - migration is skipped: {ex.Message}");
                return;
            }
            bool isUpdated = false;
            foreach (XElement propertyGroup in doc.Root.Elements(doc.Root.Name.Namespace + "PropertyGroup").ToList())
            {
                foreach (XElement property in (from p in propertyGroup.Elements(doc.Root.Name.Namespace + "AssemblyName")
                                               where p.Name == "NFUnitTest"
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
                doc.Save(projectFilePath);
            }
            #endregion


            #region Migrate nano.runsettings
            var frameworkSettings = TestFrameworkConfiguration.Read(projectFilePath, true, logger);
            var vsTestSettings = VSTestConfiguration.Create(Path.Combine(projectFilePath, TestFrameworkConfiguration.SettingsName), logger);

            frameworkSettings.SaveEffectiveSettings(projectFilePath, logger);
            vsTestSettings.SaveCustomSettings(projectFilePath, logger);
            #endregion
        }
    }
}
