// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;

namespace nanoFramework.TestFramework.Tooling
{
    /// <summary>
    /// Test run settings for VSTest that are required for the .NET nanoFramework Test Framework tooling.
    /// The settings configure VSTest to find the test adapter.
    /// </summary>
    public sealed class VSTestConfiguration
    {
        #region Fields and constants
        /// <summary>
        /// File name to store the VSTest configuration in.
        /// </summary>
        public const string VSTestSettingsFileName = "nano.vstest.runsettings";

        /// <summary>
        /// Settings in RunConfiguration that determine the behaviour of the Visual Studio test runner
        /// and that we case so much about that a user cannot assign them.
        /// </summary>
        private static readonly Dictionary<string, string> s_requiredRunConfigurationNodes = new Dictionary<string, string>()
        {
            // The Visual Studio Test Explorer implements a "poor man's parallelization" method.
            // If "Run Tests In Parallel" is selected, for each test assembly a new test host is
            // instantiated and the test hosts run in parallel. We don't want that, as it could be that
            // all those parallel hosts want to run tests on the same connected hardware device.
            // We would have to implement some inter-process locking mechanism to ensure only one
            // test host can access the device.
            // VSTest has a way to override the parallelization by specifying the maximum number
            // of test hosts that can run in parallel - specify 1 to effectively turn off the
            // parallelization option.
            { "MaxCpuCount", "1" },

            // It is possible to run tests for many types of platforms (.NET Framework 4.8,
            // .NET 8, ...) from the Visual Studio test infrastructure. VSTest uses auto-discovery
            // to determine the platform and select the test host version that matches the platform.
            // Assemblies created to run on the nanoCLR are marked for platform .NETnanoframework v1.0
            // and VSTest does not know what test host version to use. That must be a host that can run
            // the TestAdapter and this tooling.
            // Fortunately there is a way to tell VSTest which test host to use: 
            { "TargetFrameworkVersion", "net48" },
            { "TargetPlatform", "x64" }
            // Unfortunately there is no way to make that specific for the nanoCLR tests. For a solution
            // that has both nanoFramework tests and other .NET tests, putting the TargetFrameworkVersion/
            // TargetPlatform in a solution-wide .runsettings file would force all other tests to run
            // on the same platform. That is why a separate set of configs with SettingsFileName
            // are employed.
        };

        /// <summary>
        /// Settings in RunConfiguration that may contain a relative path
        /// </summary>
        private static readonly HashSet<string> s_pathSettings = new HashSet<string>
        {
            "ResultsDirectory"
        };

        /// <summary>
        /// The setting in RunConfiguration that instructs the Visual Studio test infrastructure
        /// where the test adapter lives. This must be set to ensure VSTest is using the correct
        /// one. The user can set it in the <see cref="VSTestSettingsFileName"/>-file, which is useful
        /// when developing or testing a different (version of the) TestAdapter.
        /// </summary>
        private const string TestAdaptersPaths = "TestAdaptersPaths";

        /// <summary>
        /// The document contains all settings except the required ones.
        /// </summary>
        private readonly XmlDocument _settingsDocument;
        private XmlNode _runConfiguration;
        #endregion

        #region Construction
        /// <summary>
        /// Get the settings from reading the settings file
        /// </summary>
        /// <param name="configurationFilePath">The path to the .runsettings file with the VSTest configuration. Only the VSTest configuration
        /// is read (and configurations for other test adapters that are not recognised as such); all other configurations are 
        /// Files in any of the directories are combined </param>
        /// <param name="logger">Method to pass processing information to the caller. Pass <c>null</c> if no log information is required.</param>
        /// <returns>The parsed and merged settings, or the default settings if no configuration file exists or none contains valid XML.
        public static VSTestConfiguration Create(string configurationFilePath, LogMessenger logger)
        {
            var vsTestConfiguration = new VSTestConfiguration(true);
            vsTestConfiguration.MergeSettingsFrom(Path.GetDirectoryName(configurationFilePath), Path.GetFileName(configurationFilePath), logger);
            return vsTestConfiguration;
        }

        /// <summary>
        /// Get the settings from merging the <see cref="VSTestSettingsFileName"/> files in the configuration directories.
        /// </summary>
        /// <param name="configurationFilePath">The path to the directory(ies) that may contain the file with name <see cref="VSTestSettingsFileName"/>.
        /// Files in any of the directories are combined </param>
        /// <param name="logger">Method to pass processing information to the caller. Pass <c>null</c> if no log information is required.</param>
        /// <returns>The parsed and merged settings, or the default settings if no configuration file exists or none contains valid XML.
        public static VSTestConfiguration Create(IEnumerable<string> configurationDirectoryPath, LogMessenger logger)
        {
            var vsTestConfiguration = new VSTestConfiguration(true);
            foreach (string path in configurationDirectoryPath)
            {
                vsTestConfiguration.MergeSettingsFrom(path, null, logger);
            }
            return vsTestConfiguration;
        }

        private VSTestConfiguration(bool createRunConfiguration)
        {
            _settingsDocument = new XmlDocument();
            _settingsDocument.AppendChild(_settingsDocument.CreateXmlDeclaration("1.0", "utf-8", null));
            XmlElement root = _settingsDocument.CreateElement("RunSettings");
            _settingsDocument.AppendChild(root);
            if (createRunConfiguration)
            {
                _runConfiguration = _settingsDocument.CreateElement("RunConfiguration");
                root.AppendChild(_runConfiguration);
            }
        }

        /// <summary>
        /// Create a clone of the settings
        /// </summary>
        private VSTestConfiguration Clone()
        {
            var clone = new VSTestConfiguration(false);
            clone._runConfiguration = clone._settingsDocument.ImportNode(_runConfiguration, true);
            clone._settingsDocument.DocumentElement.AppendChild(clone._runConfiguration);
            return clone;
        }

        /// <summary>
        /// Read a <see cref="VSTestSettingsFileName"/> and overwrite the current configuration
        /// </summary>
        /// <param name="configurationDirectoryPath"></param>
        /// <param name="logger"></param>
        private void MergeSettingsFrom(string configurationDirectoryPath, string fileName, LogMessenger logger)
        {
            string configurationFilePath = Path.Combine(configurationDirectoryPath, fileName ?? VSTestSettingsFileName);
            if (!File.Exists(configurationFilePath))
            {
                return;
            }
            var doc = new XmlDocument();
            try
            {
                doc.Load(configurationFilePath);
            }
            catch (Exception ex)
            {
                logger?.Invoke(LoggingLevel.Error, $"The configuration in '{configurationFilePath}' cannot be parsed: {ex.Message}");
                return;
            }

            foreach (XmlNode runConfiguration in doc.DocumentElement.SelectNodes("RunConfiguration"))
            {
                // Merge settings required for the nF TestFramework
                foreach (XmlNode node in runConfiguration.ChildNodes)
                {
                    if (node.NodeType == XmlNodeType.Element
                        && !s_requiredRunConfigurationNodes.ContainsKey(node.Name)
                        && node.Name != TestAdaptersPaths)
                    {
                        XmlNode old = _runConfiguration?.SelectSingleNode(node.Name);
                        if (!(old is null))
                        {
                            _runConfiguration.RemoveChild(old);
                        }
                        XmlNode copy = _settingsDocument.ImportNode(node, true);
                        _runConfiguration.AppendChild(copy);

                        if (s_pathSettings.Contains(node.Name))
                        {
                            // Save the absolute path as it is yet unknown where the settings will be saved
                            copy.InnerText = Path.GetFullPath(Path.Combine(configurationDirectoryPath, node.InnerText));
                        }
                    }
                }
            }
        }
        #endregion

        #region Methods
        /// <summary>
        /// Save the current settings (excluding the ones dictated by nanoFramework) to a directory.
        /// An existing <see cref="VSTestSettingsFileName"/> is overwritten. If the file exists and there are no custom settings,
        /// the file is deleted.
        /// </summary>
        /// <param name="configurationDirectoryPath">Path to the directory to save the file in</param>
        /// <param name="logger">Method to pass processing information to the caller. Pass <c>null</c> if no log information is required.</param>
        public void SaveCustomSettings(string configurationDirectoryPath, LogMessenger logger)
        {
            configurationDirectoryPath = Path.GetFullPath(configurationDirectoryPath);
            string configurationFilePath = Path.Combine(configurationDirectoryPath, VSTestSettingsFileName);
            if (_runConfiguration.HasChildNodes)
            {
                VSTestConfiguration clone = Clone();
                // Make paths relative
                foreach (string pathSetting in s_pathSettings)
                {
                    XmlNode node = clone._runConfiguration.SelectSingleNode(pathSetting)?.FirstChild;
                    if (!(node is null))
                    {
                        node.Value = PathHelper.GetRelativePath(configurationDirectoryPath, node.Value);
                    }
                }
                // Write formatted XML
                Directory.CreateDirectory(configurationDirectoryPath);
                using (var writer = new XmlTextWriter(configurationFilePath, new UTF8Encoding(false)))
                {
                    writer.Indentation = 4;
                    writer.Formatting = Formatting.Indented;
                    writer.IndentChar = ' ';
                    clone._settingsDocument.WriteContentTo(writer);
                }
            }
            else
            {
                if (File.Exists(configurationFilePath))
                {
                    try
                    {
                        File.Delete(configurationFilePath);
                    }
                    catch (Exception ex)
                    {
                        logger?.Invoke(LoggingLevel.Error, $"Cannot delete '{configurationFilePath}': {ex.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// Create a full <see cref="VSTestSettingsFileName"/> configuration, including the required RunConfiguration nodes.
        /// All paths in the configuration are absolute paths.
        /// </summary>
        /// <param name="configurationDirectoryPath">Path to the directory to save the file in</param>
        /// <param name="testAdapterDirectoryPath">The path to the directory that holds the TestAdapter.</param>
        public void SaveAllSettings(string configurationDirectoryPath, string testAdapterDirectoryPath)
        {
            VSTestConfiguration clone = Clone();

            // Complete the RunConfiguration section
            foreach (string requiredNodeName in s_requiredRunConfigurationNodes.Keys)
            {
                XmlElement node = clone._settingsDocument.CreateElement(requiredNodeName);
                clone._runConfiguration.AppendChild(node);
                XmlText value = clone._settingsDocument.CreateTextNode(s_requiredRunConfigurationNodes[requiredNodeName]);
                node.AppendChild(value);
            }
            {
                XmlNode adapterPath = clone._settingsDocument.CreateElement(TestAdaptersPaths);
                clone._runConfiguration.AppendChild(adapterPath);
                XmlText value = clone._settingsDocument.CreateTextNode(Path.GetFullPath(testAdapterDirectoryPath ?? throw new ArgumentNullException(nameof(testAdapterDirectoryPath))));
                adapterPath.AppendChild(value);
            }

            // Write formatted XML
            Directory.CreateDirectory(configurationDirectoryPath);
            using (var writer = new XmlTextWriter(Path.Combine(configurationDirectoryPath, VSTestSettingsFileName), new UTF8Encoding(false)))
            {
                writer.Indentation = 4;
                writer.Formatting = Formatting.Indented;
                writer.IndentChar = ' ';
                clone._settingsDocument.WriteContentTo(writer);
            }
        }
        #endregion
    }
}
