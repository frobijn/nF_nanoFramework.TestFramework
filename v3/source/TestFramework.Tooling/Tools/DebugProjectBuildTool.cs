// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace nanoFramework.TestFramework.Tooling.Tools
{
    /// <summary>
    /// Core functionality for the MSBuild tool that is required for the project to debug tests from one or more test projects.
    /// </summary>
    public sealed class DebugProjectBuildTool
    {
        #region Fields
        private readonly string _projectDirectoryPath;
        #endregion

        #region Construction
        /// <summary>
        /// Create the tool
        /// </summary>
        /// <param name="projectDirectoryPath">Full path to the project directory</param>
        public DebugProjectBuildTool(string projectDirectoryPath)
        {
            _projectDirectoryPath = projectDirectoryPath;
        }
        #endregion

        /// <summary>
        /// Load all available test cases from the assemblies in the output directory.
        /// </summary>
        /// <param name="referencedAssemblyFilePaths">Absolute paths to the assemblies referenced by the debug project.</param>
        /// <param name="logger">Method to pass process information to the caller.</param>
        /// <returns>The test cases, or <c>null</c> if the test cases cannot be discovered.</returns>
        public TestCaseCollection LoadTestCases(IEnumerable<string> referencedAssemblyFilePaths, LogMessenger logger)
        {
            if (!Directory.Exists(_projectDirectoryPath))
            {
                logger(LoggingLevel.Error, $"Project directory does not exist: '{_projectDirectoryPath}'");
                return null;
            }

            // Get all test cases
            TestCaseCollection testCases = null;
            testCases = new TestCaseCollection(
                from fp in referencedAssemblyFilePaths
                where !string.IsNullOrWhiteSpace(fp) && File.Exists(fp)
                select fp,
                null,
                logger);
            return testCases;
        }

        /// <summary>
        /// Generate the JSON schema and, if it does not exist or is empty, the test case specification file.
        /// </summary>
        /// <param name="testCases">The available test cases</param>
        /// <param name="generatedSchemaDirectoryPath">The path to the directory where the JSON schema should reside.
        /// The path can be relative to the project directory.</param>
        public void GenerateTestCasesSpecificationAndSchema(TestCaseCollection testCases, string generatedSchemaDirectoryPath)
        {
            string selectionSpecificationFilePath = Path.Combine(_projectDirectoryPath, DebugTestCasesSpecification.SpecificationFileName);

            // Generate the schema
            string generatedFilesDirectoryPath = Path.GetFullPath(Path.Combine(_projectDirectoryPath, generatedSchemaDirectoryPath));
            Directory.CreateDirectory(generatedFilesDirectoryPath);

            string schemaFilePath = Path.Combine(generatedFilesDirectoryPath, $"{Path.GetFileNameWithoutExtension(selectionSpecificationFilePath)}.schema.json");
            Uri schemaUri = new Uri(Path.Combine(_projectDirectoryPath, "dummy")).MakeRelativeUri(new Uri(schemaFilePath));
            File.WriteAllText(schemaFilePath, DebugTestCasesSpecification.GenerateJsonSchema(testCases));

            // Ensure the file with the specification of test cases exists
            string json = File.Exists(selectionSpecificationFilePath) ? File.ReadAllText(selectionSpecificationFilePath) : null;
            if (string.IsNullOrWhiteSpace(json))
            {
                File.WriteAllText(selectionSpecificationFilePath, $@"{{
    ""$schema"": ""{schemaUri}""
}}");
            }
        }

        /// <summary>
        /// Generate the source code for the unit test launcher.
        /// </summary>
        /// <param name="testCases">The available test cases</param>
        /// <param name="generatedSourceDirectoryPath">The path to the directory where the JSON schema should reside.
        /// The path can be relative to the project directory.</param>
        /// <param name="logger">Method to pass process information to the caller.</param>
        public void GenerateUnitTestLauncherSourceCode(TestCaseCollection testCases, string generatedSourceDirectoryPath, LogMessenger logger)
        {
            #region Get the specification of test cases to include
            string selectionSpecificationFilePath = Path.Combine(_projectDirectoryPath, DebugTestCasesSpecification.SpecificationFileName);
            DebugTestCasesSpecification specification = null;

            try
            {
                specification = DebugTestCasesSpecification.Parse(selectionSpecificationFilePath);
            }
            catch (Exception ex)
            {
                logger(LoggingLevel.Error, $"{selectionSpecificationFilePath}(0,0): error: {ex.Message}");
            }
            specification ??= new DebugTestCasesSpecification();

            System.Collections.Generic.IEnumerable<TestCaseSelection> selection = specification.SelectTestCases(testCases, logger, true);
            if (!selection.Any())
            {
                // This may be caused by a mismatch of test project assemblies and the specification.
                // Do not report an error, the build process should continue!
                // If the build is aborted, the latest test project's assemblies are never
                // going to be copied to the output directory.
                logger(LoggingLevel.Warning, $"{selectionSpecificationFilePath}(0,0): warning: No test cases selected; nothing to debug.");
            }
            #endregion

            #region Read the deployment configuration
            DeploymentConfiguration configuration = null;
            if (!string.IsNullOrWhiteSpace(specification.DeploymentConfigurationFilePath))
            {
                try
                {
                    configuration = DeploymentConfiguration.Parse(specification.DeploymentConfigurationFilePath);
                }
                catch (Exception ex)
                {
                    logger(LoggingLevel.Error, $"{specification.DeploymentConfigurationFilePath}(0,0): error: {ex.Message}");
                }
            }
            #endregion

            #region Generate the unit test launcher code
            string generatedFilesDirectoryPath = Path.GetFullPath(Path.Combine(_projectDirectoryPath, generatedSourceDirectoryPath));
            Directory.CreateDirectory(generatedFilesDirectoryPath);

            var generator = new UnitTestLauncherGenerator(selection, configuration, true, logger);
            foreach (System.Collections.Generic.KeyValuePair<string, string> source in generator.SourceFiles)
            {
                File.WriteAllText(Path.Combine(generatedFilesDirectoryPath, source.Key), source.Value);
            }
            #endregion
        }
    }
}
