// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;

namespace nanoFramework.TestFramework.Tooling
{
    /// <summary>
    /// Settings specific to the .NET nanoFramework Test Framework tooling.
    /// </summary>
    public sealed class TestFrameworkConfiguration
    {
        #region Properties
        /// <summary>
        /// Settings property XML node name
        /// </summary>
        public const string SettingsName = "nanoFrameworkAdapter";

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
        /// the ones that communicate via the serial port number(s).
        /// In the configuration file, this is a list of COM ports separated
        /// by a comma or semicolon. The value is an empty array
        /// if there are no limitations. The setting is ignored if <see cref="AllowRealHardware"/>
        /// is <c>false</c>.
        /// </summary>
        public IReadOnlyList<string> RealHardwarePort { get; set; } = new string[] { };

        public sealed class DeployToDeviceConfiguration
        {
            /// <summary>
            /// The name of the target for which the configuration is specified.
            /// If the target is omitted or is value left blank, the configuration
            /// is applied for any device that does not match the target of the other
            /// DeployToRealHardware configurations (if those exist).
            /// </summary>
            public string TargetName { get; set; } = string.Empty;

            /// <summary>
            /// True to execute the tests of a single unit test assembly
            /// at a time on real hardware. If the value is <c>false</c>, multiple assemblies
            /// can be executed on the test assembly at once. Whether the setting is honoured
            /// depends on the way the tests are conducted.
            /// The default value is <c>false</c>.
            /// </summary>
            public bool DeployAssembliesOneByOne { get; set; } = false;
        }

        /// <summary>
        /// Configuration of the deployment of test assemblies to real hardware.
        /// </summary>
        public IReadOnlyList<DeployToDeviceConfiguration> DeployToRealHardware { get; set; } = null;

        /// <summary>
        /// The default for <see cref="DeployToRealHardware"/> is <see cref="DeployToDeviceConfiguration.DeployAssembliesOneByOne"/>
        /// = <c>true</c> for all devices. This is used if <see cref="DeployToDeviceConfiguration"/> is not specified,
        /// or if a device has a target that does not match any of the specified configurations.
        /// </summary>
        public static IReadOnlyList<DeployToDeviceConfiguration> DefaultDeployToRealHardware
        {
            get;
        } = new DeployToDeviceConfiguration[]
            {
                new DeployToDeviceConfiguration ()
                {
                    DeployAssembliesOneByOne = true
                }
            };

        /// <summary>
        /// Get the deployment configuration for a device.
        /// </summary>
        /// <param name="device">(Real hardware) device that is available to run tests on.</param>
        /// <returns>The deployment configuration for the device.</returns>
        public DeployToDeviceConfiguration DeployToDevice(ITestDevice device)
        {
            DeployToDeviceConfiguration FindConfiguration(IReadOnlyList<DeployToDeviceConfiguration> configurations)
            {
                DeployToDeviceConfiguration defaultConfiguration = null;
                foreach (DeployToDeviceConfiguration configuration in configurations)
                {
                    if (string.IsNullOrWhiteSpace(configuration.TargetName))
                    {
                        defaultConfiguration = configuration;
                    }
                    else if (configuration.TargetName == device.TargetName())
                    {
                        return configuration;
                    }
                }
                if (!(defaultConfiguration is null))
                {
                    return defaultConfiguration;
                }
                return null;
            }

            DeployToDeviceConfiguration result = null;
            if (!(DeployToRealHardware is null))
            {
                result = FindConfiguration(DeployToRealHardware);
            }
            result ??= FindConfiguration(DefaultDeployToRealHardware);
            return result;
        }

        /// <summary>
        /// Path to a local nanoCLR instance to use to run Unit Tests.
        /// If the path is specified as a relative path and the tests of a single assembly
        /// are being executed, the path is assumed to be relative to the assembly
        /// being executed. If no such file exists, the path is then assumed to be
        /// relative to the solution directory. If that file also does not exist, the
        /// test will not be executed on the Virtual Device.
        /// Is an empty string if no value has been specified.
        /// </summary>
        public string PathToLocalCLRInstance { get; set; } = string.Empty;

        /// <summary>
        /// Version of the global nanoCLR instance to use when running Unit Tests.
        /// This setting is ignored if <see cref="PathToLocalCLRInstance"/> is specified.
        /// Is an empty string if no value has been specified.
        /// </summary>
        public string CLRVersion { get; set; } = string.Empty;

        /// <summary>
        /// Allow the parallel execution of tests o the Virtual Device, provided
        /// that is enabled by the Test Framework attributes in the code of the test assembly.
        /// The default is <c>true</c>.
        /// </summary>
        public bool AllowLocalCLRParallelExecution { get; set; } = true;

        /// <summary>
        /// Level of logging for Unit Test execution.
        /// </summary>
        public LoggingLevel Logging { get; set; } = LoggingLevel.None;

        /// <summary>
        /// Get the run configuration settings that have been read from the (template for)
        /// .runsettings using <see cref="Read(string, LogMessenger, string)"/>
        /// </summary>
        private XmlNode RunConfiguration
        {
            get;
            set;
        } = null;
        #endregion

        #region Construction / validation
        /// <summary>
        /// Get settings from (a template for) a .runsettings file
        /// </summary>
        /// <param name="configuration">The content of a .runsettings file.</param>
        /// <param name="defaultConfiguration">The configuration that will be modified by the content of the .runsettings file.
        /// Pass <c>null</c> to start with a configuration without any user settings.</param>
        /// <param name="logger">Method to pass processing information to the caller. Pass <c>null</c> if no log information is required.</param>
        /// <returns>The parsed settings, or the default settings if the <paramref name="configuration"/> is
        /// <c>null</c>, is not valid XML or does not contain a node with name <see cref="SettingsName"/>.
        /// If <paramref name="defaultConfiguration"/> is not <c>null</c>, the result is a modified version of that configuration.</returns>
        public static TestFrameworkConfiguration Read(string configuration, TestFrameworkConfiguration defaultConfiguration, LogMessenger logger)
        {
            defaultConfiguration ??= new TestFrameworkConfiguration();
            if (string.IsNullOrEmpty(configuration))
            {
                return defaultConfiguration;
            }
            var doc = new XmlDocument();
            try
            {
                doc.LoadXml(configuration);
            }
            catch (Exception ex)
            {
                logger?.Invoke(LoggingLevel.Error, $"The .runsettings configuration is not valid XML: {ex.Message}");
                return defaultConfiguration;
            }
            defaultConfiguration.ModifyConfiguration(doc.DocumentElement?.SelectSingleNode(SettingsName));

            // Also remember the RunConfiguration settings except the ones we care about.
            XmlNode runConfiguration = doc.DocumentElement.SelectSingleNode(nameof(defaultConfiguration.RunConfiguration));
            if (!(runConfiguration is null))
            {
                // Remove fixed settings
                foreach (string requiredNodeName in s_requiredRunConfigurationNodes.Keys)
                {
                    XmlNode node = runConfiguration.SelectSingleNode(requiredNodeName);
                    node?.ParentNode.RemoveChild(node);
                }
                // Merge settings
                foreach (XmlNode node in runConfiguration.ChildNodes)
                {
                    if (node.NodeType == XmlNodeType.Element && !s_requiredRunConfigurationNodes.ContainsKey(node.Name))
                    {
                        XmlNode old = defaultConfiguration.RunConfiguration?.SelectSingleNode(node.Name);
                        if (!(old is null))
                        {
                            defaultConfiguration.RunConfiguration.RemoveChild(old);
                        }
                        if (defaultConfiguration.RunConfiguration is null)
                        {
                            var newDoc = new XmlDocument();
                            newDoc.AppendChild(newDoc.CreateElement(nameof(RunConfiguration)));
                            defaultConfiguration.RunConfiguration = newDoc.DocumentElement;
                        }
                        XmlNode copy = defaultConfiguration.RunConfiguration.OwnerDocument.ImportNode(node, true);
                        defaultConfiguration.RunConfiguration.AppendChild(copy);
                    }
                }
            }
            return defaultConfiguration;
        }

        /// <summary>
        /// Get settings from an XML node
        /// </summary>
        /// <param name="node">Relevant node from the XML configuration. This should be a node with the name
        /// <see cref="SettingsName"/>.</param>
        /// <returns>The parsed settings</returns>
        public static TestFrameworkConfiguration Extract(XmlNode node)
        {
            TestFrameworkConfiguration settings = new TestFrameworkConfiguration();
            settings.ModifyConfiguration(node);
            return settings;
        }

        /// <summary>
        /// Modify the current configuration with the settings in the XML.
        /// </summary>
        /// <param name="node">Relevant node from the XML configuration. This should be a node with the name
        /// <see cref="SettingsName"/>.</param>
        private void ModifyConfiguration(XmlNode node)
        {
            if (node?.Name == SettingsName)
            {
                XmlNode allowRealHard = node.SelectSingleNode(nameof(AllowRealHardware))?.FirstChild
                                    ?? node.SelectSingleNode("IsRealHardware")?.FirstChild;
                if (allowRealHard != null && allowRealHard.NodeType == XmlNodeType.Text)
                {
                    AllowRealHardware = allowRealHard.Value.ToLower() == "true";
                }

                XmlNode realHardPort = node.SelectSingleNode(nameof(RealHardwarePort))?.FirstChild;
                if (realHardPort != null && realHardPort.NodeType == XmlNodeType.Text)
                {
                    if (!string.IsNullOrWhiteSpace(realHardPort.Value))
                    {
                        RealHardwarePort = realHardPort.Value.Split(',', ';');
                    }
                }

                XmlNodeList deployToRealHardware = node.SelectNodes(nameof(DeployToRealHardware));
                if (deployToRealHardware != null)
                {
                    var configurations = new List<DeployToDeviceConfiguration>();
                    var targets = new HashSet<string>();

                    foreach (XmlNode deviceConfiguration in deployToRealHardware)
                    {
                        if (deviceConfiguration.NodeType == XmlNodeType.Element)
                        {
                            var configuration = new DeployToDeviceConfiguration();

                            XmlNode targetName = deviceConfiguration.SelectSingleNode(nameof(DeployToDeviceConfiguration.TargetName))?.FirstChild;
                            if (targetName != null && targetName.NodeType == XmlNodeType.Text)
                            {
                                configuration.TargetName = targetName.Value;
                            }

                            XmlNode deployAssembliesOneByOne = deviceConfiguration.SelectSingleNode(nameof(DeployToDeviceConfiguration.DeployAssembliesOneByOne))?.FirstChild;
                            if (deployAssembliesOneByOne != null && deployAssembliesOneByOne.NodeType == XmlNodeType.Text)
                            {
                                configuration.DeployAssembliesOneByOne = deployAssembliesOneByOne.Value.ToLower() == "true";
                            }

                            targets.Add(configuration.TargetName);
                            configurations.Add(configuration);
                        }
                    }

                    if (!(DeployToRealHardware is null))
                    {
                        configurations.AddRange(from c in DeployToRealHardware
                                                where !targets.Contains(c.TargetName)
                                                select c);
                    }
                    if (configurations.Count > 0)
                    {
                        DeployToRealHardware = configurations;
                    }
                }

                XmlNode loggingLevel = node.SelectSingleNode(nameof(Logging))?.FirstChild;
                if (loggingLevel != null && loggingLevel.NodeType == XmlNodeType.Text)
                {
                    if (Enum.TryParse(loggingLevel.Value, out LoggingLevel logging))
                    {
                        Logging = logging;
                    }
                }

                XmlNode clrVersion = node.SelectSingleNode(nameof(CLRVersion))?.FirstChild;
                if (clrVersion != null && clrVersion.NodeType == XmlNodeType.Text)
                {
                    CLRVersion = clrVersion.Value;
                }

                XmlNode pathToLocalCLRInstance = node.SelectSingleNode(nameof(PathToLocalCLRInstance))?.FirstChild;
                if (pathToLocalCLRInstance != null && pathToLocalCLRInstance.NodeType == XmlNodeType.Text)
                {
                    PathToLocalCLRInstance = pathToLocalCLRInstance.Value;
                }

                XmlNode allowLocalCLRParallelExecution = node.SelectSingleNode(nameof(AllowLocalCLRParallelExecution))?.FirstChild;
                if (allowLocalCLRParallelExecution != null && allowLocalCLRParallelExecution.NodeType == XmlNodeType.Text)
                {
                    AllowLocalCLRParallelExecution = allowLocalCLRParallelExecution.Value.ToLower() == "true";
                }
            }
        }

        /// <summary>
        /// Validate the settings and resolve the relative paths
        /// </summary>
        /// <param name="solutionDirectory">Path to the solution directory. Pass <c>null</c> if this is not available.</param>
        /// <param name="assemblyFilePath">Path to the assembly containing the tests. Pass <c>null</c> if tests from multiple assemblies are being considered.</param>
        /// <param name="logger">Method to pass processing information to the caller. Pass <c>null</c> if no log information is required.</param>
        /// <returns>Indicates whether the settings are ready to use. Returns <c>false</c> if there is a problem,
        /// e.g., the files pointed to by paths doe not exist.</returns>
        public bool Validate(string solutionDirectory, string assemblyFilePath, LogMessenger logger)
        {
            bool isValid = true;

            if (!AllowRealHardware)
            {
                if (RealHardwarePort.Count > 0)
                {
                    logger?.Invoke(LoggingLevel.Verbose, $"Tests on real hardware are disabled; {nameof(RealHardwarePort)} is ignored.'");
                }
            }

            if (!(DeployToRealHardware is null))
            {
                var deployAssembliesOneByOne = new Dictionary<string, (bool asTrue, bool asFalse)>();
                foreach (DeployToDeviceConfiguration configuration in DeployToRealHardware)
                {
                    if (!deployAssembliesOneByOne.TryGetValue(configuration.TargetName, out (bool asTrue, bool asFalse) asWhich))
                    {
                        asWhich = (false, false);
                    }
                    deployAssembliesOneByOne[configuration.TargetName] = (asWhich.asTrue || configuration.DeployAssembliesOneByOne, asWhich.asFalse || !configuration.DeployAssembliesOneByOne);
                }
                foreach (string targetName in from c in deployAssembliesOneByOne
                                              where c.Value.asTrue && c.Value.asFalse
                                              select c.Key)
                {
                    logger?.Invoke(LoggingLevel.Error, $"{nameof(DeployToDeviceConfiguration.DeployAssembliesOneByOne)} is specified as both true and false for {nameof(DeployToDeviceConfiguration.TargetName)} = '{targetName}'");
                }
            }


            if (!string.IsNullOrWhiteSpace(PathToLocalCLRInstance))
            {
                bool found = false;
                if (!Path.IsPathRooted(PathToLocalCLRInstance))
                {
                    if (!string.IsNullOrWhiteSpace(assemblyFilePath))
                    {
                        string assemblyDirectory = Path.GetDirectoryName(assemblyFilePath);
                        string candidate = Path.Combine(assemblyDirectory, PathToLocalCLRInstance);
                        if (File.Exists(candidate))
                        {
                            PathToLocalCLRInstance = candidate;
                            found = true;
                        }
                        else
                        {
                            logger?.Invoke(LoggingLevel.Detailed, $"{nameof(PathToLocalCLRInstance)} '{PathToLocalCLRInstance}' is not relative to the assembly directory '{assemblyDirectory}'");
                        }
                    }

                    if (!found && !string.IsNullOrWhiteSpace(solutionDirectory))
                    {
                        string candidate = Path.Combine(solutionDirectory, PathToLocalCLRInstance);
                        if (File.Exists(candidate))
                        {
                            PathToLocalCLRInstance = candidate;
                            found = true;
                        }
                        else
                        {
                            logger?.Invoke(LoggingLevel.Detailed, $"{nameof(PathToLocalCLRInstance)} '{PathToLocalCLRInstance}' is not relative to the solution directory '{solutionDirectory}'");
                        }
                    }

                    if (found)
                    {
                        logger?.Invoke(LoggingLevel.Detailed, $"{nameof(PathToLocalCLRInstance)}: found at '{PathToLocalCLRInstance}'");
                    }
                }
                else
                {
                    found = File.Exists(PathToLocalCLRInstance);
                }
                if (!found)
                {
                    logger?.Invoke(LoggingLevel.Error, $"Local CLR instance not found at {nameof(PathToLocalCLRInstance)} = '{PathToLocalCLRInstance}'");
                    isValid = false;
                }
                if (!string.IsNullOrWhiteSpace(CLRVersion))
                {
                    logger?.Invoke(LoggingLevel.Verbose, $"{nameof(CLRVersion)} is ignored because the path to a local CLR instance is specified.");
                }
            }

            return isValid;
        }
        #endregion

        #region Methods
        /// <summary>
        /// Create a full .runsettings configuration, including the required RunConfiguration nodes.
        /// </summary>
        /// <param name="testAdapterDirectoryPath">The path to the directory that holds the TestAdapter.
        /// This value is used of the user has not yet specified a path for the adapter.</param>
        /// <returns>The XML of the full configuration.</returns>
        public string CreateRunSettings(string testAdapterDirectoryPath)
        {
            XmlDocument configuration = CreateRunSettings();

            // Complete the RunConfiguration section
            XmlNode runConfiguration;
            if (RunConfiguration is null)
            {
                runConfiguration = configuration.CreateElement("RunConfiguration");
            }
            else
            {
                runConfiguration = configuration.ImportNode(RunConfiguration, true);
            }
            configuration.DocumentElement.AppendChild(runConfiguration);
            foreach (string requiredNodeName in s_requiredRunConfigurationNodes.Keys)
            {
                XmlElement node = configuration.CreateElement(requiredNodeName);
                runConfiguration.AppendChild(node);
                XmlText value = configuration.CreateTextNode(s_requiredRunConfigurationNodes[requiredNodeName]);
                node.AppendChild(value);
            }

            XmlNode adapterPath = runConfiguration.SelectSingleNode("TestAdaptersPaths");
            if (adapterPath is null)
            {
                adapterPath = configuration.CreateElement("TestAdaptersPaths");
                runConfiguration.AppendChild(adapterPath);
                XmlText value = configuration.CreateTextNode(testAdapterDirectoryPath);
                adapterPath.AppendChild(value);
            }

            // Add the framework configuration
            AddFrameworkConfiguration(configuration);

            // Return formatted XML
            using (var buffer = new MemoryStream())
            {
                using (var writer = new XmlTextWriter(buffer, new UTF8Encoding(false)))
                {
                    writer.Indentation = 4;
                    writer.Formatting = Formatting.Indented;
                    writer.IndentChar = ' ';
                    configuration.WriteContentTo(writer);
                }
                return Encoding.UTF8.GetString(buffer.ToArray());
            }
        }
        private static readonly Dictionary<string, string> s_requiredRunConfigurationNodes = new Dictionary<string, string>()
        {
            { "MaxCpuCount", "1" },
            { "TargetFrameworkVersion", "net48" },
            { "TargetPlatform", "x64" }
        };

        private static XmlDocument CreateRunSettings()
        {
            var configuration = new XmlDocument();
            configuration.AppendChild(configuration.CreateXmlDeclaration("1.0", "utf-8", null));
            configuration.AppendChild(configuration.CreateElement("RunSettings"));
            return configuration;
        }

        private void AddFrameworkConfiguration(XmlDocument configuration)
        {
            // Add the framework nodes
            XmlElement frameworkConfiguration = configuration.CreateElement(SettingsName);
            configuration.DocumentElement.AppendChild(frameworkConfiguration);

            #region Shorthand
            void AddNode(string name, string value, XmlElement parent = null)
            {
                XmlElement node = configuration.CreateElement(name);
                (parent ?? frameworkConfiguration).AppendChild(node);
                XmlText valueNode = configuration.CreateTextNode(value);
                node.AppendChild(valueNode);
            }
            void AddBooleanNode(string name, bool value, XmlElement parent = null)
            {
                AddNode(name, value ? "true" : "false", parent);
            }
            #endregion

            var defaultConfiguration = new TestFrameworkConfiguration();

            if (AllowRealHardware != defaultConfiguration.AllowRealHardware)
            {
                AddBooleanNode(nameof(AllowRealHardware), AllowRealHardware);
            }

            if (RealHardwarePort.Count > 0)
            {
                AddNode(nameof(RealHardwarePort), string.Join(";", RealHardwarePort));
            }

            if (!(DeployToRealHardware is null))
            {
                foreach (DeployToDeviceConfiguration device in DeployToRealHardware)
                {
                    XmlElement deviceNode = configuration.CreateElement(nameof(DeployToRealHardware));
                    frameworkConfiguration.AppendChild(deviceNode);

                    AddNode(nameof(device.TargetName), device.TargetName, deviceNode);
                    AddBooleanNode(nameof(device.DeployAssembliesOneByOne), device.DeployAssembliesOneByOne, deviceNode);
                }
            }

            if (PathToLocalCLRInstance != defaultConfiguration.PathToLocalCLRInstance)
            {
                AddNode(nameof(PathToLocalCLRInstance), PathToLocalCLRInstance);
            }

            if (CLRVersion != defaultConfiguration.CLRVersion)
            {
                AddNode(nameof(CLRVersion), CLRVersion);
            }

            if (AllowLocalCLRParallelExecution != defaultConfiguration.AllowLocalCLRParallelExecution)
            {
                AddBooleanNode(nameof(AllowLocalCLRParallelExecution), AllowLocalCLRParallelExecution);
            }

            if (Logging != defaultConfiguration.Logging)
            {
                AddNode(nameof(Logging), Logging.ToString());
            }
        }
        #endregion
    }
}
