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
    /// Settings specific to the .NET nanoFramework Test Framework tooling.
    /// </summary>
    public sealed class TestFrameworkConfiguration
    {
        #region Fields and constants
        /// <summary>
        /// TestFramework settings property XML node name
        /// </summary>
        public const string SettingsName = "nanoFrameworkAdapter";

        /// <summary>
        /// File name to store the nanoFramework.TestFramework configuration in.
        /// </summary>
        public const string SettingsFileName = "nano.runsettings";

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
        /// The setting in RunConfiguration that instructs the Visual Studio test infrastructure
        /// where the test adapter lives. This must be set to ensure VSTest is using the correct
        /// one. The user can set it in the <see cref="SettingsFileName"/>-file, which is useful
        /// when developing or testing a different (version of the) TestAdapter.
        /// </summary>
        private const string TestAdaptersPaths = "TestAdaptersPaths";

        private string _maxVirtualDevices;
        #endregion

        #region Properties
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

        /// <summary>
        /// Path to a local version of nanoclr.exe to use to run Unit Tests.
        /// If the path is specified as a relative path and the tests of a single assembly
        /// are being executed, the path is assumed to be relative to the assembly
        /// being executed. If no such file exists, the path is then assumed to be
        /// relative to the solution directory. If that file also does not exist, the
        /// test will not be executed.
        /// Is an empty string if no value has been specified.
        /// </summary>
        public string PathToLocalNanoCLR { get; set; } = string.Empty;

        /// <summary>
        /// Version of the global nanoCLR instance to use when running Unit Tests.
        /// This setting is ignored if <see cref="PathToLocalNanoCLR"/> is specified.
        /// Is an empty string if no value has been specified.
        /// </summary>
        public string CLRVersion { get; set; } = string.Empty;

        /// <summary>
        /// Path to a local CLR instance to use to run Unit Tests.
        /// If the path is specified as a relative path and the tests of a single assembly
        /// are being executed, the path is assumed to be relative to the assembly
        /// being executed. If no such file exists, the path is then assumed to be
        /// relative to the solution directory. If that file also does not exist, the
        /// test will not be executed on the Virtual Device.
        /// Is an empty string if no value has been specified.
        /// </summary>
        public string PathToLocalCLRInstance { get; set; } = string.Empty;

        /// <summary>
        /// Set to a number other than 1 tp allow the parallel execution of tests on a Virtual Device.
        /// Set to 0 (the default) to let the test framework decide how many virtual devices to spin up.
        /// </summary>
        public int MaxVirtualDevices { get; set; } = 0;

        /// <summary>
        /// Level of logging for Unit Test execution.
        /// </summary>
        public LoggingLevel Logging { get; set; } = LoggingLevel.None;

        /// <summary>
        /// Allows users to terminate a test session when it exceeds a given timeout, specified in milliseconds.
        /// Setting a timeout ensures that resources are well consumed and test sessions are constrained to a set time.
        /// </summary>
        public int? TestSessionTimeout
        {
            get;
            private set;
        }

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
            TestFrameworkConfiguration combinedConfiguration = defaultConfiguration ?? new TestFrameworkConfiguration();
            if (string.IsNullOrEmpty(configuration))
            {
                return combinedConfiguration;
            }
            var doc = new XmlDocument();
            try
            {
                doc.LoadXml(configuration);
            }
            catch (Exception ex)
            {
                logger?.Invoke(LoggingLevel.Error, $"The .runsettings configuration is not valid XML: {ex.Message}");
                return combinedConfiguration;
            }
            combinedConfiguration.ModifyConfiguration(doc.DocumentElement?.SelectSingleNode(SettingsName));

            // Also remember the RunConfiguration settings except the ones we care about.
            XmlNode runConfiguration = doc.DocumentElement.SelectSingleNode(nameof(combinedConfiguration.RunConfiguration));
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
                        XmlNode old = combinedConfiguration.RunConfiguration?.SelectSingleNode(node.Name);
                        if (!(old is null))
                        {
                            combinedConfiguration.RunConfiguration.RemoveChild(old);
                        }
                        if (combinedConfiguration.RunConfiguration is null)
                        {
                            var newDoc = new XmlDocument();
                            newDoc.AppendChild(newDoc.CreateElement(nameof(RunConfiguration)));
                            combinedConfiguration.RunConfiguration = newDoc.DocumentElement;
                        }
                        XmlNode copy = combinedConfiguration.RunConfiguration.OwnerDocument.ImportNode(node, true);
                        combinedConfiguration.RunConfiguration.AppendChild(copy);

                        if (node.Name == nameof(TestSessionTimeout))
                        {
                            if (int.TryParse(copy.InnerText, out int timeout))
                            {
                                combinedConfiguration.TestSessionTimeout = timeout;
                            }
                            else
                            {
                                combinedConfiguration.TestSessionTimeout = null;
                            }
                        }
                    }
                }
            }
            return combinedConfiguration;
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

                XmlNode loggingLevel = node.SelectSingleNode(nameof(Logging))?.FirstChild;
                if (loggingLevel != null && loggingLevel.NodeType == XmlNodeType.Text)
                {
                    if (Enum.TryParse(loggingLevel.Value, out LoggingLevel logging))
                    {
                        Logging = logging;
                    }
                }

                XmlNode pathToLocalNanoCLR = node.SelectSingleNode(nameof(PathToLocalNanoCLR))?.FirstChild;
                if (pathToLocalNanoCLR != null && pathToLocalNanoCLR.NodeType == XmlNodeType.Text)
                {
                    PathToLocalNanoCLR = pathToLocalNanoCLR.Value;
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

                XmlNode maxVirtualDevices = node.SelectSingleNode(nameof(MaxVirtualDevices))?.FirstChild;
                if (maxVirtualDevices != null && maxVirtualDevices.NodeType == XmlNodeType.Text)
                {
                    if (int.TryParse(maxVirtualDevices.Value.Trim(), out int value))
                    {
                        MaxVirtualDevices = value;
                        if (MaxVirtualDevices < 0)
                        {
                            _maxVirtualDevices = MaxVirtualDevices.ToString();
                        }
                    }
                    else
                    {
                        _maxVirtualDevices = maxVirtualDevices.Value;
                    }
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

            bool ResolveRelativePath(string name, string path)
            {
                bool found = false;
                if (!Path.IsPathRooted(path))
                {
                    if (!string.IsNullOrWhiteSpace(assemblyFilePath))
                    {
                        string assemblyDirectory = Path.GetDirectoryName(assemblyFilePath);
                        string candidate = Path.Combine(assemblyDirectory, path);
                        if (File.Exists(candidate))
                        {
                            path = candidate;
                            found = true;
                        }
                        else
                        {
                            logger?.Invoke(LoggingLevel.Detailed, $"{name} '{path}' is not relative to the assembly directory '{assemblyDirectory}'");
                        }
                    }

                    if (!found && !string.IsNullOrWhiteSpace(solutionDirectory))
                    {
                        string candidate = Path.Combine(solutionDirectory, path);
                        if (File.Exists(candidate))
                        {
                            path = candidate;
                            found = true;
                        }
                        else
                        {
                            logger?.Invoke(LoggingLevel.Detailed, $"{name} '{path}' is not relative to the solution directory '{solutionDirectory}'");
                        }
                    }

                    if (found)
                    {
                        logger?.Invoke(LoggingLevel.Detailed, $"{name}: found at '{path}'");
                    }
                }
                else
                {
                    found = File.Exists(path);
                }
                return found;
            }

            if (!string.IsNullOrWhiteSpace(PathToLocalNanoCLR))
            {
                if (!ResolveRelativePath(nameof(PathToLocalNanoCLR), PathToLocalNanoCLR))
                {
                    logger?.Invoke(LoggingLevel.Error, $"Local nanoclr.exe not found at {nameof(PathToLocalNanoCLR)} = '{PathToLocalNanoCLR}'");
                    isValid = false;
                }
                if (!string.IsNullOrWhiteSpace(CLRVersion))
                {
                    logger?.Invoke(LoggingLevel.Verbose, $"{nameof(CLRVersion)} is ignored because the path to a local CLR instance is specified.");
                }
            }
            if (!string.IsNullOrWhiteSpace(PathToLocalCLRInstance))
            {
                if (!ResolveRelativePath(nameof(PathToLocalCLRInstance), PathToLocalCLRInstance))
                {
                    logger?.Invoke(LoggingLevel.Error, $"Local CLR instance not found at {nameof(PathToLocalCLRInstance)} = '{PathToLocalCLRInstance}'");
                    isValid = false;
                }
            }

            if (!string.IsNullOrWhiteSpace(_maxVirtualDevices))
            {
                logger?.Invoke(LoggingLevel.Error, $"{nameof(MaxVirtualDevices)} must be an integer ≥ 0, not '{_maxVirtualDevices}'");
                isValid = false;
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
                runConfiguration = configuration.CreateElement(nameof(RunConfiguration));
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

            XmlNode adapterPath = runConfiguration.SelectSingleNode(TestAdaptersPaths);
            if (adapterPath is null)
            {
                adapterPath = configuration.CreateElement(TestAdaptersPaths);
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

            if (PathToLocalNanoCLR != defaultConfiguration.PathToLocalNanoCLR)
            {
                AddNode(nameof(PathToLocalNanoCLR), PathToLocalNanoCLR);
            }

            if (CLRVersion != defaultConfiguration.CLRVersion)
            {
                AddNode(nameof(CLRVersion), CLRVersion);
            }

            if (PathToLocalCLRInstance != defaultConfiguration.PathToLocalCLRInstance)
            {
                AddNode(nameof(PathToLocalCLRInstance), PathToLocalCLRInstance);
            }

            if (MaxVirtualDevices != defaultConfiguration.MaxVirtualDevices)
            {
                AddNode(nameof(MaxVirtualDevices), MaxVirtualDevices.ToString());
            }

            if (Logging != defaultConfiguration.Logging)
            {
                AddNode(nameof(Logging), Logging.ToString());
            }
        }
        #endregion
    }
}
