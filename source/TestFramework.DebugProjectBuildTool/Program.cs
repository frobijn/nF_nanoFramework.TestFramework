// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using nanoFramework.TestFramework.Tooling;

namespace nanoFramework.TestFramework.DebugProjectBuildTool
{
    /// <summary>
    /// This task is coded as a console application. If not, it would be loaded in one of the
    /// long-running Visual Studio/MSBuild processes. The assemblies loaded to determine the
    /// test cases will then be kept in memory and thus be locked, and cannot be overwritten
    /// in a subsequent build.
    /// </summary>
    internal class Program
    {
        static void Main(string[] args)
        {
            var projectDirectoryPath = args.Length > 0 ? args[0] : null;
            var intermediateOutputDirectoryPath = args.Length > 1 ? args[1] : null;
            var outputDirectoryPath = args.Length > 2 ? args[2] : null;

            var invalid = projectDirectoryPath is null
                || intermediateOutputDirectoryPath is null
                || outputDirectoryPath is null;

            var verbose = false;
            if (args.Length > 3)
            {
                if (args[3].ToLower() == "-v" || args[3].ToLower() == "--verbose")
                {
                    verbose = true;
                }
                else
                {
                    invalid = true;
                }
            }
            invalid |= args.Length > 4;

            if (verbose || invalid)
            {
                FileVersionInfo fileVersion = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location);
                Console.WriteLine($"nanoFramework debug project build task v{fileVersion.ToString()}");
                Console.WriteLine("Copyright (c) 2024 nanoFramework project contributors");
            }
            if (invalid)
            {
                Console.WriteLine($"{Path.GetFileNameWithoutExtension(typeof(Program).Assembly.Location)} <project_directory_path> <intermediateOutputDirectory> <outputDirectory> [-v]");
                return;
            }

            LogMessenger logger = (level, message) =>
            {
                if (verbose || level >= LoggingLevel.Error)
                {
                    Console.WriteLine(message);
                }
            };
            UpdateSourceCodeAndSchema(projectDirectoryPath, intermediateOutputDirectoryPath, outputDirectoryPath, logger);
        }

        private static void UpdateSourceCodeAndSchema(string projectDirectoryPath, string intermediateOutputDirectoryPath, string outputDirectoryPath, LogMessenger logger)
        {
            if (!Directory.Exists(projectDirectoryPath))
            {
                logger(LoggingLevel.Error, $"Project directory does not exist: '{projectDirectoryPath}'");
                return;
            }

            // Get all test cases
            TestCaseCollection testCases = null;
            outputDirectoryPath = Path.Combine(projectDirectoryPath, outputDirectoryPath);
            if (Directory.Exists(outputDirectoryPath))
            {
                testCases = new TestCaseCollection(
                    Directory.EnumerateFiles(outputDirectoryPath, "*.dll", SearchOption.TopDirectoryOnly),
                    null,
                    true,
                    logger);
            }

            var selectionSourceFilePath = Path.Combine(projectDirectoryPath, UnitTestLauncherGenerator.RUNUNITTESTS_SOURCEFILENAME);
            var selectionSpecificationFilePath = Path.ChangeExtension(selectionSourceFilePath, ".json");

            var generatedFilesDirectoryPath = Path.GetFullPath(Path.Combine(projectDirectoryPath, intermediateOutputDirectoryPath, "nF"));
            Directory.CreateDirectory(generatedFilesDirectoryPath);

            // Generate the schema
            var schemaFilePath = Path.Combine(generatedFilesDirectoryPath, $"{Path.GetFileNameWithoutExtension(selectionSpecificationFilePath)}.schema.json");
            var schemaUri = new Uri(Path.Combine(projectDirectoryPath, "dummy")).MakeRelativeUri(new Uri(schemaFilePath));
            File.WriteAllText(schemaFilePath, DebugTestCasesSpecification.GenerateJsonSchema(testCases));

            // Get or create the specification of test cases to include
            DebugTestCasesSpecification specification;
            var json = File.Exists(selectionSpecificationFilePath) ? File.ReadAllText(selectionSpecificationFilePath) : null;
            if (string.IsNullOrWhiteSpace(json))
            {
                specification = new DebugTestCasesSpecification()
                {
                    SchemaUri = schemaUri.ToString()
                };
                File.WriteAllText(selectionSpecificationFilePath, specification.ToJson());
            }
            else
            {
                try
                {
                    specification = DebugTestCasesSpecification.Parse(json);
                }
                catch (Exception ex)
                {
                    logger(LoggingLevel.Error, $"{selectionSpecificationFilePath}(0,0): error: {ex.Message}");
                    specification = new DebugTestCasesSpecification();
                }
            }

            // Generate the unit test launcher code
            var generator = new UnitTestLauncherGenerator(specification.SelectTestCases(testCases), true, logger);
            generator.SourceFiles.TryGetValue(UnitTestLauncherGenerator.RUNUNITTESTS_SOURCEFILENAME, out string code);
            File.WriteAllText(selectionSourceFilePath, code ?? string.Empty);

            foreach (var source in generator.SourceFiles)
            {
                if (source.Key != UnitTestLauncherGenerator.RUNUNITTESTS_SOURCEFILENAME)
                {
                    File.WriteAllText(Path.Combine(generatedFilesDirectoryPath, source.Key), source.Value);
                }
            }
        }
    }
}
