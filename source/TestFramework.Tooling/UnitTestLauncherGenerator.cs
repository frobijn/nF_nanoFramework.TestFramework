// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using CliWrap;
using CliWrap.Buffered;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace nanoFramework.TestFramework.Tooling
{
    public sealed class UnitTestLauncherGenerator
    {
        #region Fields
        private readonly Dictionary<string, string> _sourceFiles = new Dictionary<string, string>();
        private const string MAIN_SOURCEFILE = "UnitTestLauncher.Main.cs";
        private const string ASSEMBLY_NAME = "nanoFramework.UnitTestLauncher";
        #endregion

        #region Construction
        /// <summary>
        /// Generate the unit test launcher for a selection of unit tests
        /// from a single assembly.
        /// </summary>
        /// <param name="selection">Selection of unit tests</param>
        /// <param name="configuration">Deployment configuration to generated the code for; can be <c>null</c>.</param>
        /// <param name="communicateByNames">Indicates whether the information about running the test cases should
        /// use names rather than numbers. Pass <c>true</c> to make the output better understandable for humans.</param>
        /// <param name="logger">Method to pass process information to the caller.</param>
        public UnitTestLauncherGenerator(TestCaseSelection selection, DeploymentConfiguration configuration, bool communicateByNames, LogMessenger logger)
            : this(new TestCaseSelection[] { selection }, configuration, communicateByNames, logger)
        {
        }

        /// <summary>
        /// Generate the unit test launcher for a selection of unit tests
        /// from multiple assemblies. This is only possible if the full names
        /// of the test classes from different assemblies are distinct.
        /// </summary>
        /// <param name="selection">Selection of unit tests</param>
        /// <param name="configuration">Deployment configuration to generated the code for; can be <c>null</c>.</param>
        /// <param name="communicateByNames">Indicates whether the information about running the test cases should
        /// use names rather than numbers. Pass <c>true</c> to make the output better understandable for humans.</param>
        /// <param name="logger">Method to pass process information to the caller.</param>
        public UnitTestLauncherGenerator(IEnumerable<TestCaseSelection> selection, DeploymentConfiguration configuration, bool communicateByNames, LogMessenger logger)
        {
            string code = GetSourceCode("UnitTestLauncher.cs", logger);
            if (communicateByNames)
            {
                if (!(code is null))
                {
                    code = s_replaceCommunicationValues.Replace(code, "${value}");
                }
            }
            else
            {
                _sourceFiles["UnitTestLauncher.Communication.cs"] = GetSourceCode("UnitTestLauncher.Communication.cs", logger);
            }
            _sourceFiles["UnitTestLauncher.TestClassInitialisation.cs"] = GetSourceCode("UnitTestLauncher.TestClassInitialisation.cs", logger);
            _sourceFiles["UnitTestLauncher.cs"] = code;
            _sourceFiles[RUNUNITTESTS_SOURCEFILENAME] = GetSourceCode(RUNUNITTESTS_SOURCEFILENAME, logger);

            AddTestCases(selection, configuration);
        }
        private static readonly Regex s_replaceCommunicationValues = new Regex(@"\{Communication\.(?<value>[A-Z0-9_]+)\}", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private string GetSourceCode(string sourceFile, LogMessenger logger)
        {
            using (Stream stream = GetType().Assembly.GetManifestResourceStream($"{GetType().FullName}.{sourceFile}"))
            {
                if (stream is null)
                {
                    logger?.Invoke(LoggingLevel.Error, $"Source file '{sourceFile}' is not found among the embedded resources");
                }
                else
                {
                    using (var reader = new StreamReader(stream))
                    {
                        return reader.ReadToEnd();
                    }
                }
            }
            return null;
        }
        #endregion

        #region Properties
        /// <summary>
        /// Get the source files that implement the unit test launcher.
        /// </summary>
        public IReadOnlyDictionary<string, string> SourceFiles
            => _sourceFiles;

        /// <summary>
        /// The name of the generated source file that contains the unit tests to run.
        /// </summary>
        public const string RUNUNITTESTS_SOURCEFILENAME = "UnitTestLauncher.RunUnitTests.cs";
        #endregion

        public sealed class Application
        {
            #region Construction
            /// <summary>
            /// Create the application information
            /// </summary>
            /// <param name="assemblies">List of assemblies to load for the unit test</param>
            public Application(IReadOnlyList<string> assemblies)
            {
                Assemblies = assemblies;
            }
            #endregion

            #region Properties
            /// <summary>
            /// List of assemblies to load for the unit test
            /// </summary>
            public IReadOnlyList<string> Assemblies
            {
                get;
            }

            /// <summary>
            /// The prefix used to output messages about the unit test execution.
            /// </summary>
            public string ReportPrefix
            {
                get;
            } = Guid.NewGuid().ToString("N");
            #endregion
        }

        /// <summary>
        /// Generate the unit test launcher as an application that will be deployed to
        /// the device to execute the tests.
        /// </summary>
        /// <param name="assemblyDirectoryPath">Path to the directory with the unit test
        /// assembly and all its dependencies.</param>
        /// <param name="logger">Method to pass process information to the caller.</param>
        /// <returns>The information about the created assembly, or <c>null</c> if the
        /// application could not be created.</returns>
        public Application GenerateAsApplication(string assemblyDirectoryPath, LogMessenger logger)
        {
            var assemblies = new List<string>();
            if (!FindAssemblies(assemblies, assemblyDirectoryPath, logger))
            {
                return null;
            }

            var result = new Application(assemblies);
            string mainSource = GetSourceCode(MAIN_SOURCEFILE, logger);
            if (mainSource is null)
            {
                return null;
            }
            mainSource = mainSource.Replace("@@@", result.ReportPrefix);

            if (!GenerateApplication(assemblies, assemblyDirectoryPath, mainSource, logger))
            {
                return null;
            }
            return result;
        }


        #region Implementation
        /// <summary>
        /// Implement the UnitTestLauncher.RunUnitTests method by generating
        /// the required code to execute the selected unit tests.
        /// </summary>
        /// <param name="selections">Selection of unit tests</param>
        /// <param name="configuration">Deployment configuration to generated the code for; can be <c>null</c>.</param>
        private void AddTestCases(IEnumerable<TestCaseSelection> selections, DeploymentConfiguration configuration)
        {
            if (!_sourceFiles.ContainsKey(RUNUNITTESTS_SOURCEFILENAME))
            {
                return;
            }

            #region Code for configuration data
            int staticDataIndex = 0;
            var configurationDataName = new Dictionary<string, string>();
            var staticData = new StringBuilder(@"#region Deployment configuration data");
            string AddConfigurationData(string configurationKey, Type valueType)
            {
                string key = $"{valueType.Name}_{configurationKey}";
                if (configurationDataName.TryGetValue(key, out string code))
                {
                    return code;
                }
                var data = configuration?.GetDeploymentConfigurationValue(configurationKey, valueType);
                if (data is null)
                {
                    configurationDataName[key] = null;
                    return null;
                }

                if (valueType == typeof(byte[]))
                {
                    string fieldName = $"s_cfg_{++staticDataIndex}";
                    configurationDataName[key] = fieldName;
                    var binaryData = (byte[])data;

                    staticData.Append($@"
{s_dataIndent}/// <summary>Value for deployment configuration key '{configurationKey}'</summary>
{s_dataIndent}private static readonly byte[] {fieldName} = new byte[] {{");
                    for (int i = 0; i < binaryData.Length;)
                    {
                        staticData.Append($@"
{s_dataIndent}    ");
                        for (; i < binaryData.Length; i++)
                        {
                            staticData.Append(binaryData[i]);
                            staticData.Append(',');
                        }
                    }
                    staticData.Append($@"
{s_dataIndent}}};");

                    return fieldName;
                }
                else
                {
                    string fieldName = $"CFG_{++staticDataIndex}";
                    configurationDataName[key] = fieldName;
                    var typeName = valueType == typeof(int) ? "int" : valueType == typeof(long) ? "long" : valueType == typeof(string) ? "string" : throw new NotSupportedException();

                    staticData.Append($@"
{s_dataIndent}/// <summary>Value for deployment configuration key '{configurationKey}'</summary>
{s_dataIndent}private const {typeName} {fieldName} = {(valueType == typeof(string) ? SymbolDisplay.FormatLiteral((string)data, true) : data)};");

                    return fieldName;
                }
            }
            string ConvertToArguments(IReadOnlyList<(string key, Type valueType)> requiredConfigurationKeys)
            {
                if ((requiredConfigurationKeys?.Count ?? 0) == 0)
                {
                    return "null";
                }
                var arguments = new List<string>();
                foreach ((string key, Type valueType) in requiredConfigurationKeys)
                {
                    string fieldName = AddConfigurationData(key, valueType);
                    if (fieldName is null)
                    {
                        arguments.Add(valueType == typeof(int) ? "-1" : valueType == typeof(long) ? "-1L" : "null");
                    }
                    else
                    {
                        arguments.Add(fieldName);
                    }
                }
                return $"new object[] {{ {string.Join(", ", arguments)} }}";
            }
            #endregion

            var code = new StringBuilder();
            foreach (TestCaseSelection selection in selections)
            {

                var perGroup = selection.TestCases
                                .GroupBy(tc => tc.testCase.Group)
                                .ToDictionary
                                (
                                    g => g.Key,
                                    g => (from tc in g
                                          orderby tc.selectionIndex
                                          select tc.testCase).ToList()
                                );

                foreach (KeyValuePair<TestCaseGroup, List<TestCase>> testGroup in perGroup)
                {
                    int instantiation = (int)testGroup.Key.Instantiation
                        + (int)(testGroup.Key.SetupCleanupPerTestMethod
                                    ? Tools.UnitTestLauncher.TestClassInitialisation.SetupCleanupPerTestMethod
                                    : Tools.UnitTestLauncher.TestClassInitialisation.SetupCleanupPerClass);

                    code.AppendLine($"{s_bodyIndent}ForClass(");
                    code.AppendLine($"{s_bodyIndent}    typeof(global::{testGroup.Key.FullyQualifiedName}), {instantiation},");
                    if (testGroup.Key.SetupMethodName is null)
                    {
                        code.AppendLine($"{s_bodyIndent}    null, null,");
                    }
                    else
                    {
                        code.AppendLine($"{s_bodyIndent}    nameof(global::{testGroup.Key.FullyQualifiedName}.{testGroup.Key.SetupMethodName}),");
                        code.AppendLine($"{s_bodyIndent}    {ConvertToArguments(testGroup.Key.RequiredConfigurationKeys)},");
                    }
                    if (testGroup.Key.CleanupMethodName is null)
                    {
                        code.AppendLine($"{s_bodyIndent}    null,");
                    }
                    else
                    {
                        code.AppendLine($"{s_bodyIndent}    nameof(global::{testGroup.Key.FullyQualifiedName}.{testGroup.Key.CleanupMethodName}),");
                    }

                    code.AppendLine($"{s_bodyIndent}    (frm, fdr) =>");
                    code.AppendLine($"{s_bodyIndent}    {{");
                    foreach (TestCase testCase in testGroup.Value)
                    {
                        if (testCase.DataRowIndex < 0)
                        {
                            code.AppendLine($"{s_bodyIndent}        frm(nameof(global::{testCase.FullyQualifiedName}), {ConvertToArguments(testCase.RequiredConfigurationKeys)});");
                        }
                    }

                    var perTestMethod = (from tc in testGroup.Value
                                         where tc.DataRowIndex >= 0
                                         select tc)
                                     .GroupBy(tc => tc.FullyQualifiedName)
                                     .ToDictionary(
                                        g => g.Key,
                                        g => (g.First(), (from tc in g
                                                          orderby tc.DataRowIndex
                                                          select tc.DataRowIndex).ToList())
                                     );
                    foreach (KeyValuePair<string, (TestCase, List<int>)> testMethod in perTestMethod)
                    {
                        code.AppendLine($"{s_bodyIndent}        fdr(nameof(global::{testMethod.Key}), {ConvertToArguments(testMethod.Value.Item1.RequiredConfigurationKeys)}, {string.Join(", ", testMethod.Value.Item2)});");
                    }

                    code.AppendLine($"{s_bodyIndent}    }}");
                    code.AppendLine($"{s_bodyIndent});");
                }
            }

            string sourceCode = _sourceFiles[RUNUNITTESTS_SOURCEFILENAME].Replace("@@@", code.ToString());
            if (configurationDataName.Count > 0)
            {
                staticData.Append(@"
#endregion
");
                sourceCode = sourceCode.Replace("$$$", staticData.ToString());
            }
            else
            {
                sourceCode = sourceCode.Replace("$$$", "");
            }
            _sourceFiles[RUNUNITTESTS_SOURCEFILENAME] = sourceCode;
        }
        private static readonly string s_bodyIndent = new string(' ', 12);
        private static readonly string s_dataIndent = new string(' ', 8);

        /// <summary>
        /// Get all assemblies for the unit tests. Assumption is that all these are the *.pe
        /// files the assembly directory. Delete <see cref="ASSEMBLY_NAME"/>-files as well.
        /// </summary>
        /// <param name="assemblies">List of assemblies to fill</param>
        /// <param name="assemblyDirectoryPath">Directory with all assemblies</param>
        /// <param name="logger">Method to pass process information to the caller.</param>
        private bool FindAssemblies(List<string> assemblies, string assemblyDirectoryPath, LogMessenger logger)
        {
            if (Directory.Exists(assemblyDirectoryPath))
            {
                foreach (string file in Directory.EnumerateFiles(assemblyDirectoryPath, "*.pe"))
                {
                    if (Path.GetFileNameWithoutExtension(file) != ASSEMBLY_NAME)
                    {
                        assemblies.Add(file);
                    }
                }
            }
            if (assemblies.Count == 0)
            {
                // Nothing to run!
                logger?.Invoke(LoggingLevel.Verbose, $"Application generation aborted: no unit test assemblies found");
                return false;
            }

            foreach (string file in Directory.EnumerateFiles(assemblyDirectoryPath, $"{ASSEMBLY_NAME}.*"))
            {
                try
                {
                    File.Delete(file);
                }
                catch (Exception ex)
                {
                    logger?.Invoke(LoggingLevel.Verbose, $"Application generation aborted: cannot delete {Path.GetFileName(file)}: {ex.Message}");
                    return false;
                }
            }
            return true;
        }

        private bool GenerateApplication(List<string> assemblies, string assemblyDirectoryPath, string mainSourceCode, LogMessenger logger)
        {
            string assemblyFilePath = Path.Combine(assemblyDirectoryPath, ASSEMBLY_NAME + ".dll");

            CSharpCompilation compilation = CSharpCompilation.Create(Path.GetFileName(assemblyFilePath))
                                            .WithOptions(new CSharpCompilationOptions(OutputKind.ConsoleApplication))
                                            .AddReferences(from filePath in assemblies
                                                           select MetadataReference.CreateFromFile(Path.ChangeExtension(filePath, ".dll")));

            var sources = new List<string>(SourceFiles.Values) {
                mainSourceCode
            };
            foreach (string source in sources)
            {
                compilation = compilation.AddSyntaxTrees(SyntaxFactory.ParseSyntaxTree(source, encoding: Encoding.UTF8));
            }

            bool success = true;
            Microsoft.CodeAnalysis.Emit.EmitResult compiled = compilation.Emit(assemblyFilePath);
            if (compiled.Diagnostics.Length > 0)
            {
                foreach (Diagnostic message in compiled.Diagnostics)
                {
                    if (message.Severity == DiagnosticSeverity.Error)
                    {
                        success = false;
                    }
                    string msg = $"{message.Id} {message.GetMessage(CultureInfo.InvariantCulture)}";
                    if (message.Location != Location.None)
                    {
                        msg += $" @ {message.Location}";
                    }
                    logger?.Invoke(message.Severity switch
                    {
                        DiagnosticSeverity.Error => LoggingLevel.Error,
                        DiagnosticSeverity.Warning => LoggingLevel.Verbose,
                        _ => LoggingLevel.Detailed
                    }, msg);
                }
            }
            if (!success)
            {
                return false;
            }

            // Convert exe to pe
            string peFilePath = Path.Combine(assemblyDirectoryPath, ASSEMBLY_NAME + ".pe");

            var arguments = new List<string>();
            foreach (string assembly in assemblies)
            {
                arguments.Add("-LoadHints");
                arguments.Add(Path.GetFileNameWithoutExtension(assembly));
                arguments.Add(Path.ChangeExtension(Path.GetFileName(assembly), ".dll"));
            }
            arguments.Add("-verbose");

            arguments.Add("-parse");
            arguments.Add(Path.GetFileName(assemblyFilePath));

            arguments.Add("-compile");
            arguments.Add(Path.GetFileName(peFilePath));
            arguments.Add("false");

            Command cmd = Cli.Wrap(Path.Combine(Path.GetDirectoryName(GetType().Assembly.Location), "MetaDataProcessor", "nanoFramework.Tools.MetaDataProcessor.exe"))
                .WithArguments(arguments)
                .WithWorkingDirectory(assemblyDirectoryPath)
                .WithValidation(CommandResultValidation.None);

            BufferedCommandResult cliResult = cmd.ExecuteBufferedAsync().Task.Result;
            if (cliResult.ExitCode != 0)
            {
                logger?.Invoke(LoggingLevel.Error, $"Compilation to nanoFramework assembly failed:{Environment.NewLine}{cliResult.StandardError}{Environment.NewLine}MetadataProcessor output:{Environment.NewLine}{cliResult.StandardOutput}");
                return false;
            }
            assemblies.Insert(0, peFilePath);
            return true;
        }

        #endregion
    }
}
