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
    /// Settings for the execution of unit tests that are specified in files with name
    /// <see cref="SettingsFileName"/>/<see cref="UserSettingsFileName"/>.
    /// </summary>
    public sealed class TestFrameworkConfiguration
    {
        #region Fields and constants
        /// <summary>
        /// TestFramework settings property XML node name
        /// </summary>
        public const string SettingsName = "nanoFrameworkAdapter";

        /// <summary>
        /// File name to store the nanoFramework.TestFramework configuration in
        /// that contains the settings for all computers/servers that host the
        /// tests runners. This file is typically added to version control / git.
        /// </summary>
        public const string SettingsFileName = "nano.runsettings";

        /// <summary>
        /// File name to store the nanoFramework.TestFramework configuration in
        /// that is specific for the current user on the current computer.
        /// This file is not intended to be added to version control / git.
        /// </summary>
        public const string UserSettingsFileName = "nano.runsettings.user";

        private readonly List<string> _configurationHierarchyDirectoryPaths = new List<string>();
        private readonly Dictionary<string, string> _deploymentConfiguration = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private const string GlobalSettingsDirectoryPath = "GlobalSettingsDirectoryPath";
        private const string DeploymentConfiguration = "DeploymentConfiguration";
        private const string DeploymentConfiguration_SerialPort = "SerialPort";
        private const string DeploymentConfiguration_File = "File";
        #endregion

        #region Properties
        /// <summary>
        /// The hierarchy of locations where the configuration files are located. The last entry
        /// is the one specific for the unit test assembly.
        /// </summary>
        public IReadOnlyList<string> ConfigurationHierarchyDirectoryPaths
            => _configurationHierarchyDirectoryPaths;

        /// <summary>
        /// True (the default) to allow the tests to be executed on real hardware.
        /// </summary>
        /// <remarks>
        /// This setting can be specified in a .runsettings file both as
        /// <c>&lt;AllowRealHardware&gt;</c> and <c>&lt;IsRealHardware&gt;</c>,
        /// the latter to be backward compatible with earlier versions of the
        /// framework.
        /// </remarks>
        public bool AllowRealHardware { get; set; } = true;

        /// <summary>
        /// Limit the real hardware devices to run the tests on to
        /// the ones that communicate via the serial port(s).
        /// In the configuration file, this is a list of COM ports separated
        /// by a comma or semicolon. The value is an empty array
        /// if there are no limitations. The setting is ignored if <see cref="AllowRealHardware"/>
        /// is <c>false</c>.
        /// </summary>
        public IReadOnlyList<string> AllowSerialPorts { get; set; } = new string[] { };

        /// <summary>
        /// Exclude the serial port(s) from the search for real hardware devices to run the tests on.
        /// In the configuration file, this is a list of COM ports separated
        /// by a comma or semicolon. The value is an empty array
        /// if there are no ports to exclude. The setting is ignored if <see cref="AllowRealHardware"/>
        /// is <c>false</c>.
        /// </summary>
        public IReadOnlyList<string> ExcludeSerialPorts { get; set; } = new string[] { };

        /// <summary>
        /// The maximum time in milliseconds the execution of the tests in a single test assembly on real hardware is allowed to take.
        /// </summary>
        public int? RealHardwareTimeout { get; set; }

        /// <summary>
        /// Path to a local version of nanoclr.exe to use to run unit Tests.
        /// The value is the full path to the file, and <c>null</c> if no value has been specified.
        /// It is possible to assign a path relative to the directory where the settings file resides that provided
        /// these settings.
        /// </summary>
        public string PathToLocalNanoCLR { get; set; }

        /// <summary>
        /// Version of the global nanoCLR instance to use when running Unit Tests.
        /// This setting is ignored if <see cref="PathToLocalNanoCLR"/> is specified.
        /// Is <c>null</c> if no value has been specified.
        /// </summary>
        public string CLRVersion { get; set; }

        /// <summary>
        /// Path to a local CLR instance to use to run the unit tests.
        /// The value is the full path to the file, and <c>null</c> if no value has been specified.
        /// It is possible to assign a path relative to the directory where the settings file resides that provided
        /// these settings.
        /// </summary>
        public string PathToLocalCLRInstance { get; set; }

        /// <summary>
        /// Set to a number other than 1 to allow the parallel execution of tests on a Virtual Device.
        /// Set to 0 (the default) to let the test framework decide how many virtual devices to spin up.
        /// </summary>
        public int? MaxVirtualDevices { get; set; }

        /// <summary>
        /// The maximum time in milliseconds the execution of the tests in a single test assembly on the virtual device is allowed to take.
        /// </summary>
        public int? VirtualDeviceTimeout { get; set; }

        /// <summary>
        /// Level of logging for Unit Test execution.
        /// </summary>
        public LoggingLevel Logging { get; set; } = LoggingLevel.None;

        /// <summary>
        /// Get the path to the file with deployment configuration for the device connected to the specified serial port.
        /// </summary>
        /// <param name="serialPort">Name of the serial port (e.g., COM9)</param>
        /// <returns>The full path to the file, and <c>null</c> if no value has been specified.</returns>
        public string DeploymentConfigurationFilePath(string serialPort)
        {
            _deploymentConfiguration.TryGetValue(serialPort, out string path);
            return path;
        }

        /// <summary>
        /// Assign the path to the file with deployment configuration for the device connected to the specified serial port.
        /// </summary>
        /// <param name="serialPort">Name of the serial port (e.g., COM9)</param>
        /// <param name="path">The full path to the file, or a path relative to the directory where the settings file resides that provided
        /// these settings. Pass <c>null</c> if no deployment configuration is available.</param>
        public void SetDeploymentConfigurationFilePath(string serialPort, string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                _deploymentConfiguration.Remove(serialPort);
            }
            else
            {
                _deploymentConfiguration[serialPort] = path;
            }
        }
        #endregion

        #region Construction
        /// <summary>
        /// Get settings from (a template for) a .runsettings file
        /// </summary>
        /// <param name="settingsDirectoryPath">The path where the .runsettings file(s) are located that are most relevant for the unit test assembly.</param>
        /// <param name="backwardCompatible">Indicates whether the .runsettings file is a nano.runsettings from the previous version of the test platform.</param>
        /// <param name="logger">Method to pass processing information to the caller. Pass <c>null</c> if no log information is required.</param>
        /// <returns>The parsed settings. Returns the default settings if no configuration files are found, one of the configuration files is not valid XML
        /// or none of the configuration files contain a node with name <see cref="SettingsName"/>.</returns>
        public static TestFrameworkConfiguration Read(string settingsDirectoryPath, bool backwardCompatible, LogMessenger logger)
        {
            TestFrameworkConfiguration combinedConfiguration = new TestFrameworkConfiguration();

            var visitedDirectoryPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            combinedConfiguration.Read(visitedDirectoryPaths, settingsDirectoryPath, backwardCompatible, logger);
            return combinedConfiguration;
        }
        private void Read(HashSet<string> visitedDirectoryPaths, string settingsDirectoryPath, bool backwardCompatible, LogMessenger logger)
        {
            if (string.IsNullOrWhiteSpace(settingsDirectoryPath))
            {
                return;
            }
            settingsDirectoryPath = Path.GetFullPath(settingsDirectoryPath);
            if (!visitedDirectoryPaths.Add(settingsDirectoryPath))
            {
                logger?.Invoke(LoggingLevel.Verbose, $"The path '{settingsDirectoryPath}' is encountered multiple times as value for '{GlobalSettingsDirectoryPath}'. The '{SettingsFileName}/{UserSettingsFileName}' files from that directory are only read once.");
                return;
            }

            _configurationHierarchyDirectoryPaths.Insert(0, settingsDirectoryPath);
            ReadXml(visitedDirectoryPaths, Path.Combine(settingsDirectoryPath, SettingsFileName), backwardCompatible, logger);
            if (backwardCompatible)
            {
                return;
            }
            ReadXml(visitedDirectoryPaths, Path.Combine(settingsDirectoryPath, UserSettingsFileName), false, logger);
        }
        private void ReadXml(HashSet<string> visitedDirectoryPaths, string settingsFilePath, bool backwardCompatible, LogMessenger logger)
        {
            if (!File.Exists(settingsFilePath))
            {
                return;
            }
            var doc = new XmlDocument();
            try
            {
                doc.Load(settingsFilePath);
            }
            catch (Exception ex)
            {
                logger?.Invoke(LoggingLevel.Error, $"The '{settingsFilePath}' configuration does not contain valid XML: {ex.Message}");
                return;
            }


            foreach (XmlNode node in doc.DocumentElement.SelectNodes(SettingsName))
            {
                #region Helpers
                int? ReadXmlInteger(string elementName, int? defaultValue)
                {
                    XmlNode integerValue = node.SelectSingleNode(elementName)?.FirstChild;
                    if (integerValue != null && integerValue.NodeType == XmlNodeType.Text)
                    {
                        if (int.TryParse(integerValue.Value.Trim(), out int value))
                        {
                            if (value < 0)
                            {
                                logger?.Invoke(LoggingLevel.Warning, $"'{elementName}' must 0 or larger, but is {value} in '{settingsFilePath}'. Setting is ignored.");
                            }
                            else
                            {
                                return value;
                            }
                        }
                        else
                        {
                            logger?.Invoke(LoggingLevel.Warning, $"'{nameof(MaxVirtualDevices)}' must be an integer, but is '{integerValue.Value}' in '{settingsFilePath}'. Setting is ignored.");
                        }
                    }
                    return defaultValue;
                }
                #endregion

                if (!backwardCompatible)
                {
                    XmlNode globalSettingsDirectoryPath = node.SelectSingleNode(GlobalSettingsDirectoryPath)?.FirstChild;
                    if (globalSettingsDirectoryPath?.NodeType == XmlNodeType.Text)
                    {
                        if (!string.IsNullOrWhiteSpace(globalSettingsDirectoryPath.Value))
                        {
                            // First read the global settings 
                            Read(visitedDirectoryPaths, Path.GetFullPath(Path.Combine(Path.GetDirectoryName(settingsFilePath), globalSettingsDirectoryPath.Value)), false, logger);
                        }
                    }
                }

                XmlNode allowRealHard = node.SelectSingleNode(backwardCompatible ? "IsRealHardware" : nameof(AllowRealHardware))?.FirstChild;
                if (allowRealHard != null && allowRealHard.NodeType == XmlNodeType.Text)
                {
                    AllowRealHardware = allowRealHard.Value.ToLower() == "true";
                }

                XmlNode allowSerialPorts = node.SelectSingleNode(backwardCompatible ? "RealHardwarePort" : nameof(AllowSerialPorts))?.FirstChild;
                if (allowSerialPorts != null && allowSerialPorts.NodeType == XmlNodeType.Text)
                {
                    if (!string.IsNullOrWhiteSpace(allowSerialPorts.Value))
                    {
                        AllowSerialPorts = allowSerialPorts.Value.Split(',', ';');
                    }
                }

                if (!backwardCompatible)
                {
                    XmlNode excludeSerialPorts = node.SelectSingleNode(nameof(ExcludeSerialPorts))?.FirstChild;
                    if (excludeSerialPorts != null && excludeSerialPorts.NodeType == XmlNodeType.Text)
                    {
                        if (!string.IsNullOrWhiteSpace(excludeSerialPorts.Value))
                        {
                            ExcludeSerialPorts = excludeSerialPorts.Value.Split(',', ';');
                        }
                    }

                    RealHardwareTimeout = ReadXmlInteger(nameof(RealHardwareTimeout), RealHardwareTimeout);
                }


                if (!backwardCompatible)
                {
                    XmlNode pathToLocalNanoCLR = node.SelectSingleNode(nameof(PathToLocalNanoCLR))?.FirstChild;
                    if (pathToLocalNanoCLR != null && pathToLocalNanoCLR.NodeType == XmlNodeType.Text)
                    {
                        PathToLocalNanoCLR = string.IsNullOrWhiteSpace(pathToLocalNanoCLR.Value)
                            ? null
                            : Path.GetFullPath(Path.Combine(Path.GetDirectoryName(settingsFilePath), pathToLocalNanoCLR.Value));
                    }
                }

                XmlNode clrVersion = node.SelectSingleNode(nameof(CLRVersion))?.FirstChild;
                if (clrVersion != null && clrVersion.NodeType == XmlNodeType.Text)
                {
                    CLRVersion = string.IsNullOrWhiteSpace(clrVersion.Value) ? null : clrVersion.Value;
                }

                XmlNode pathToLocalCLRInstance = node.SelectSingleNode(nameof(PathToLocalCLRInstance))?.FirstChild;
                if (pathToLocalCLRInstance != null && pathToLocalCLRInstance.NodeType == XmlNodeType.Text)
                {
                    PathToLocalCLRInstance = string.IsNullOrWhiteSpace(pathToLocalCLRInstance.Value)
                            ? null
                            : Path.GetFullPath(Path.Combine(Path.GetDirectoryName(settingsFilePath), pathToLocalCLRInstance.Value));
                }

                if (!backwardCompatible)
                {
                    MaxVirtualDevices = (int)ReadXmlInteger(nameof(MaxVirtualDevices), MaxVirtualDevices);

                    VirtualDeviceTimeout = ReadXmlInteger(nameof(VirtualDeviceTimeout), VirtualDeviceTimeout);
                }

                XmlNode loggingLevel = node.SelectSingleNode(nameof(Logging))?.FirstChild;
                if (loggingLevel != null && loggingLevel.NodeType == XmlNodeType.Text)
                {
                    if (Enum.TryParse(loggingLevel.Value, out LoggingLevel logging))
                    {
                        Logging = logging;
                    }
                    else
                    {
                        logger?.Invoke(LoggingLevel.Warning, $"'{nameof(Logging)}' = '{loggingLevel.Value}' is not a valid value in '{settingsFilePath}'. Setting is ignored.");
                    }
                }

                if (!backwardCompatible)
                {
                    foreach (XmlNode deploymentConfiguration in node.SelectNodes(DeploymentConfiguration))
                    {
                        if (deploymentConfiguration.NodeType != XmlNodeType.Element)
                        {
                            continue;
                        }
                        XmlNode serialPort = deploymentConfiguration.SelectSingleNode(DeploymentConfiguration_SerialPort)?.FirstChild;
                        XmlNode filePath = deploymentConfiguration.SelectSingleNode(DeploymentConfiguration_File).FirstChild;
                        if (serialPort?.NodeType != XmlNodeType.Text)
                        {
                            logger?.Invoke(LoggingLevel.Warning, $"'{DeploymentConfiguration}' must have a child element '{DeploymentConfiguration_SerialPort}' in '{settingsFilePath}'. Setting is ignored.");
                        }
                        else
                        {
                            SetDeploymentConfigurationFilePath(serialPort.Value, string.IsNullOrWhiteSpace(filePath?.Value)
                                                                                ? null
                                                                                : Path.GetFullPath(Path.Combine(Path.GetDirectoryName(settingsFilePath), filePath.Value)));
                        }
                    }
                }
            }
        }
        #endregion

        #region Methods
        /// <summary>
        /// Create a <see cref="SettingsFileName"/>/<see cref="UserSettingsFileName"/> pair of configuration files
        /// that contains the minimal number of settings that are functionally equivalent to these settings.
        /// Existing files will be overwritten or, if not needed, deleted.
        /// </summary>
        /// <param name="settingsDirectoryPath">The path to the directory where the settings should be saved</param>
        /// <param name="logger">Method to pass processing information to the caller. Pass <c>null</c> if no log information is required.</param>
        public void SaveEffectiveSettings(string settingsDirectoryPath, LogMessenger logger)
        {
            SaveEffectiveSettings(Path.Combine(settingsDirectoryPath, SettingsFileName), false, logger);
            SaveEffectiveSettings(Path.Combine(settingsDirectoryPath, UserSettingsFileName), true, logger);
        }

        private void SaveEffectiveSettings(string settingsFilePath, bool isUserFile, LogMessenger logger)
        {
            XmlDocument document = null;
            XmlNode runSettings = null;

            #region Shorthand
            XmlDocument Document()
            {
                if (document is null)
                {
                    document = new XmlDocument();
                    document.AppendChild(document.CreateXmlDeclaration("1.0", "utf-8", null));
                    XmlElement root = document.CreateElement("RunSettings");
                    document.AppendChild(root);
                    runSettings = document.CreateElement(SettingsName);
                    root.AppendChild(runSettings);
                }
                return document;
            }
            void AddNode(string name, string value, XmlElement parent = null)
            {
                XmlElement node = Document().CreateElement(name);
                (parent ?? runSettings).AppendChild(node);
                XmlText valueNode = document.CreateTextNode(value);
                node.AppendChild(valueNode);
            }
            void AddBooleanNode(string name, bool value, XmlElement parent = null)
            {
                AddNode(name, value ? "true" : "false", parent);
            }
            #endregion

            #region Save settings
            var defaultConfiguration = new TestFrameworkConfiguration();

            if (isUserFile)
            {
                if (AllowRealHardware && AllowSerialPorts.Count > 0)
                {
                    AddNode(nameof(AllowSerialPorts), string.Join(";", AllowSerialPorts));

                    foreach (string serialPort in AllowSerialPorts)
                    {
                        if (_deploymentConfiguration.TryGetValue(serialPort, out string filePath))
                        {
                            XmlElement node = Document().CreateElement(DeploymentConfiguration);
                            runSettings.AppendChild(node);
                            AddNode(DeploymentConfiguration_SerialPort, serialPort, node);
                            AddNode(DeploymentConfiguration_File, PathHelper.GetRelativePath(Path.GetDirectoryName(settingsFilePath), filePath), node);
                        }
                    }
                }
            }
            else
            {
                if (AllowRealHardware != defaultConfiguration.AllowRealHardware)
                {
                    AddBooleanNode(nameof(AllowRealHardware), AllowRealHardware);
                }

                if (AllowRealHardware)
                {
                    if (RealHardwareTimeout != defaultConfiguration.RealHardwareTimeout)
                    {
                        AddNode(nameof(RealHardwareTimeout), RealHardwareTimeout.ToString());
                    }
                }

                if (PathToLocalNanoCLR != defaultConfiguration.PathToLocalNanoCLR)
                {
                    AddNode(nameof(PathToLocalNanoCLR), PathHelper.GetRelativePath(Path.GetDirectoryName(settingsFilePath), PathToLocalNanoCLR));
                }

                if (CLRVersion != defaultConfiguration.CLRVersion)
                {
                    AddNode(nameof(CLRVersion), CLRVersion);
                }

                if (PathToLocalCLRInstance != defaultConfiguration.PathToLocalCLRInstance)
                {
                    AddNode(nameof(PathToLocalCLRInstance), PathHelper.GetRelativePath(Path.GetDirectoryName(settingsFilePath), PathToLocalCLRInstance));
                }

                if (MaxVirtualDevices != defaultConfiguration.MaxVirtualDevices)
                {
                    AddNode(nameof(MaxVirtualDevices), MaxVirtualDevices.ToString());
                }

                if (VirtualDeviceTimeout != defaultConfiguration.VirtualDeviceTimeout)
                {
                    AddNode(nameof(VirtualDeviceTimeout), VirtualDeviceTimeout.ToString());
                }

                if (Logging != defaultConfiguration.Logging)
                {
                    AddNode(nameof(Logging), Logging.ToString());
                }
            }
            #endregion

            #region Save/delete file
            if (document is null)
            {
                try
                {
                    File.Delete(settingsFilePath);
                }
                catch (Exception ex)
                {
                    logger?.Invoke(LoggingLevel.Error, $"Cannot delete '{settingsFilePath}': {ex.Message}");
                }
            }
            else
            {
                // Write formatted XML
                Directory.CreateDirectory(Path.GetDirectoryName(settingsFilePath));
                using (var writer = new XmlTextWriter(settingsFilePath, new UTF8Encoding(false)))
                {
                    writer.Indentation = 4;
                    writer.Formatting = Formatting.Indented;
                    writer.IndentChar = ' ';
                    document.WriteContentTo(writer);
                }
            }
            #endregion
        }
        #endregion
    }
}
