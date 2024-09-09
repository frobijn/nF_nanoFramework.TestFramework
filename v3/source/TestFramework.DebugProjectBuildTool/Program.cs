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
#if DEBUG
#if LAUNCHDEBUGGER
            if (!Debugger.IsAttached)
            {
                Debugger.Launch();
            }
#endif
#endif
            string projectDirectoryPath = args.Length > 0 ? args[0] : null;
            string generatedSourceDirectoryPath = args.Length > 1 ? args[1] : null;
            string outputDirectoryPath = args.Length > 2 ? args[2] : null;

            bool invalid = string.IsNullOrWhiteSpace(projectDirectoryPath)
                || generatedSourceDirectoryPath is null
                || outputDirectoryPath is null;

            bool verbose = false;
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
                Console.WriteLine($"nanoFramework debug project build task v{fileVersion}");
                Console.WriteLine("Copyright (c) 2024 nanoFramework project contributors");
            }
            if (invalid)
            {
                Console.WriteLine($"{Path.GetFileNameWithoutExtension(typeof(Program).Assembly.Location)} <project_directory> <generated_source_directory> <output_directory> [-v]");
                return;
            }

            void logger(LoggingLevel level, string message)
            {
                if (verbose || level >= LoggingLevel.Error)
                {
                    Console.WriteLine(message);
                }
            }

            var tool = new Tooling.Tools.DebugProjectBuildTool(projectDirectoryPath);
            TestCaseCollection testCases = tool.LoadTestCases(outputDirectoryPath, logger);
            if (!(testCases is null))
            {
                tool.GenerateTestCasesSpecificationAndSchema(testCases, generatedSourceDirectoryPath);
                tool.GenerateUnitTestLauncherSourceCode(testCases, generatedSourceDirectoryPath, logger);
            }
        }
    }
}
