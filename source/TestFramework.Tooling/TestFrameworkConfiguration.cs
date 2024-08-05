// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Xml;

namespace nanoFramework.TestFramework.Tooling
{
    /// <summary>
    /// Settings specific to the .NET nanoFramework Test Framework tooling.
    /// </summary>
    public class TestFrameworkConfiguration
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
        public string[] RealHardwarePort { get; set; } = new string[] { };

        /// <summary>
        /// True (the default) to execute the tests of a single unit test assembly
        /// at a time on real hardware. If the value is <c>false</c>, multiple assemblies
        /// can be executed on the test assembly at once. Whether the setting is honoured
        /// depends on the way the tests are conducted.
        /// </summary>
        public bool DeploySingleAssemblyToRealHardware { get; set; } = true;

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
        #endregion

        #region Construction / validation
        /// <summary>
        /// Get settings from an XML node
        /// </summary>
        /// <param name="node">Noe th</param>
        /// <param name="nodeName">Name of the node to parse</param>
        /// <returns>The parsed settings</returns>
        public static TestFrameworkConfiguration Extract(XmlNode node, string nodeName = SettingsName)
        {
            TestFrameworkConfiguration settings = new TestFrameworkConfiguration();

            if (node?.Name == nodeName)
            {
                XmlNode allowRealHard = node.SelectSingleNode(nameof(AllowRealHardware))?.FirstChild
                                    ?? node.SelectSingleNode("IsRealHardware")?.FirstChild;
                if (allowRealHard != null && allowRealHard.NodeType == XmlNodeType.Text)
                {
                    settings.AllowRealHardware = allowRealHard.Value.ToLower() == "true";
                }

                XmlNode realHardPort = node.SelectSingleNode(nameof(RealHardwarePort))?.FirstChild;
                if (realHardPort != null && realHardPort.NodeType == XmlNodeType.Text)
                {
                    if (!string.IsNullOrWhiteSpace(realHardPort.Value))
                    {
                        settings.RealHardwarePort = realHardPort.Value.Split(',', ';');
                    }
                }


                XmlNode deploySingleAssemblyToRealHardware = node.SelectSingleNode(nameof(DeploySingleAssemblyToRealHardware))?.FirstChild;
                if (deploySingleAssemblyToRealHardware != null && deploySingleAssemblyToRealHardware.NodeType == XmlNodeType.Text)
                {
                    settings.DeploySingleAssemblyToRealHardware = deploySingleAssemblyToRealHardware.Value.ToLower() == "true";
                }

                XmlNode loggingLevel = node.SelectSingleNode(nameof(Logging))?.FirstChild;
                if (loggingLevel != null && loggingLevel.NodeType == XmlNodeType.Text)
                {
                    if (Enum.TryParse(loggingLevel.Value, out LoggingLevel logging))
                    {
                        settings.Logging = logging;
                    }
                }

                XmlNode clrVersion = node.SelectSingleNode(nameof(CLRVersion))?.FirstChild;
                if (clrVersion != null && clrVersion.NodeType == XmlNodeType.Text)
                {
                    settings.CLRVersion = clrVersion.Value;
                }

                XmlNode pathToLocalCLRInstance = node.SelectSingleNode(nameof(PathToLocalCLRInstance))?.FirstChild;
                if (pathToLocalCLRInstance != null && pathToLocalCLRInstance.NodeType == XmlNodeType.Text)
                {
                    settings.PathToLocalCLRInstance = pathToLocalCLRInstance.Value;
                }

                XmlNode allowLocalCLRParallelExecution = node.SelectSingleNode(nameof(AllowLocalCLRParallelExecution))?.FirstChild;
                if (allowLocalCLRParallelExecution != null && allowLocalCLRParallelExecution.NodeType == XmlNodeType.Text)
                {
                    settings.AllowLocalCLRParallelExecution = allowLocalCLRParallelExecution.Value.ToLower() == "true";
                }
            }

            return settings;
        }

        /// <summary>
        /// Validate the settings and resolve the relative paths
        /// </summary>
        /// <param name="solutionDirectory">Path to the solution directory. Pass <c>null</c> if this is not available.</param>
        /// <param name="assemblyFilePath">Path to the assembly containing the tests. Pass <c>null</c> if tests from multiple assemblies are being considered.</param>
        /// <param name="logger">Method to pass processing information to the caller. Pass <c>null</c> if no log information is required.</param>
        /// <returns>Indicates whether the settings are ready to use. Returns <c>false</c> if there is a problem,
        /// e.g., the files pointed to by paths doe not exist.</returns>
        public virtual bool Validate(string solutionDirectory, string assemblyFilePath, LogMessenger logger)
        {
            bool isValid = true;

            if (!AllowRealHardware)
            {
                if (RealHardwarePort.Length > 0)
                {
                    logger?.Invoke(LoggingLevel.Verbose, $"Tests on real hardware are disabled; {nameof(RealHardwarePort)} is ignored.'");
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
    }
}
