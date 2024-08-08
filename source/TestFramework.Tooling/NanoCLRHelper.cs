// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using CliWrap;
using CliWrap.Buffered;
using Newtonsoft.Json;

namespace nanoFramework.TestFramework.Tooling
{
    /// <summary>
    /// Helper to ensure the correct version of the nanoCLR tool is available
    /// </summary>
    public class NanoCLRHelper
    {
        #region Construction
        /// <summary>
        /// Create the helper for a specific test framework configuration
        /// </summary>
        /// <param name="configuration">Test framework configuration</param>
        /// <param name="logger">Method to pass process information to the caller.</param>
        public NanoCLRHelper(TestFrameworkConfiguration configuration, LogMessenger logger)
            : this(
                  configuration.PathToLocalCLRInstance,
                  string.IsNullOrWhiteSpace(configuration.PathToLocalCLRInstance) ? configuration.CLRVersion : null,
                  string.IsNullOrWhiteSpace(configuration.PathToLocalCLRInstance) && string.IsNullOrWhiteSpace(configuration.CLRVersion),
                  logger
              )
        {
        }

        /// <summary>
        /// Create the helper for a specific nanoCLR version
        /// </summary>
        /// <param name="nanoCLRFilePath">Path to nanoCLR.exe. Pass <c>null</c> to use the global tool.</param>
        /// <param name="requiredNanoCLRVersion">Required version of nanoCLR. If the global tool is of an earlier version,
        /// update the tool. Pass <c>null</c> for the current/latest version of the tool.</param>
        /// <param name="checkVersion">Check whether the global tool is up to date, and if not update the global tool.
        /// Pass <c>false</c> to keep using the current version.</param>
        /// <param name="logger">Method to pass process information to the caller.</param>
        public NanoCLRHelper(string nanoCLRFilePath, string requiredNanoCLRVersion, bool checkVersion, LogMessenger logger)
        {
            if (!string.IsNullOrWhiteSpace(nanoCLRFilePath))
            {
                NanoCLRFilePath = nanoCLRFilePath;
                NanoClrIsInstalled = File.Exists(nanoCLRFilePath);
                if (!NanoClrIsInstalled && !checkVersion)
                {
                    logger?.Invoke(LoggingLevel.Error, $"*** Failed to locate nanoCLR instance '{nanoCLRFilePath}' ***");
                }
                else
                {
                    InstallNanoClr(Path.GetDirectoryName(NanoCLRFilePath), checkVersion, logger);
                }
            }
            else
            {
                NanoCLRFilePath = "nanoclr";
                InstallNanoClr(null, checkVersion, logger);
            }
            if (!string.IsNullOrWhiteSpace(requiredNanoCLRVersion))
            {
                UpdateNanoCLRInstance(NanoCLRFilePath, requiredNanoCLRVersion, logger);
            }
        }
        #endregion

        #region Properties
        /// <summary>
        /// Get the path to use when running the nanoCLR tool
        /// </summary>
        public string NanoCLRFilePath
        {
            get;
        }


        /// <summary>
        /// Flag to report if nanoCLR CLI .NET tool is installed.
        /// </summary>
        public bool NanoClrIsInstalled
        {
            get;
            private set;
        } = false;
        #endregion

        #region Version update
        private bool InstallNanoClr(string localPath, bool checkForUpdate, LogMessenger logger)
        {
            logger?.Invoke(LoggingLevel.Verbose, "Install/update nanoclr tool");

            // get installed tool version (if installed)
            Command cmd = Cli.Wrap(localPath is null ? "nanoclr" : localPath)
                .WithArguments("--help")
                .WithValidation(CommandResultValidation.None);

            bool performInstallUpdate = false;

            // setup cancellation token with a timeout of 10 seconds
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

            try
            {
                BufferedCommandResult cliResult = cmd.ExecuteBufferedAsync(cts.Token).Task.Result;

                if (cliResult.ExitCode == 0)
                {
                    Match regexResult = Regex.Match(cliResult.StandardOutput, @"(?'version'\d+\.\d+\.\d+)", RegexOptions.RightToLeft);

                    if (regexResult.Success)
                    {
                        NanoClrIsInstalled = true;
                        logger?.Invoke(LoggingLevel.Verbose, $"Running nanoclr v{regexResult.Groups["version"].Value}");

                        if (checkForUpdate)
                        {
                            // compose version
                            Version installedVersion = new Version(regexResult.Groups[1].Value);

                            string responseContent = null;

                            // check latest version
                            using (System.Net.WebClient client = new WebClient())
                            {
                                try
                                {
                                    // Set the user agent string to identify the client.
                                    client.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/58.0.3029.110 Safari/537.36");

                                    // Set any additional headers, if needed.
                                    client.Headers.Add("Content-Type", "application/json");

                                    // Set the URL to request.
                                    string url = "https://api.nuget.org/v3-flatcontainer/nanoclr/index.json";

                                    // Make the HTTP request and retrieve the response.
                                    responseContent = client.DownloadString(url);
                                }
                                catch (WebException e)
                                {
                                    // Handle any exceptions that occurred during the request.
                                    Console.WriteLine(e.Message);
                                }
                            }

                            NuGetPackage package = JsonConvert.DeserializeObject<NuGetPackage>(responseContent);
                            Version latestPackageVersion = new Version(package.Versions[package.Versions.Length - 1]);

                            // check if we are running the latest one
                            if (latestPackageVersion > installedVersion)
                            {
                                // need to update
                                performInstallUpdate = true;
                            }
                            else
                            {
                                logger?.Invoke(LoggingLevel.Verbose, $"No need to update. Running v{latestPackageVersion}");

                                performInstallUpdate = false;
                            }
                        }
                    }
                    else
                    {
                        // something wrong with the output, can't proceed
                        logger?.Invoke(LoggingLevel.Error, "Failed to parse current nanoCLR CLI version!");
                    }
                }
            }
            catch (Win32Exception)
            {
                // nanoclr doesn't seem to be installed
                performInstallUpdate = true;
                NanoClrIsInstalled = false;
            }

            if (performInstallUpdate)
            {
                cmd = Cli.Wrap("dotnet")
                .WithArguments($"tool update {(string.IsNullOrWhiteSpace(localPath) ? "-g" : $"--tool-path \"{localPath}\"")}  nanoclr")
                .WithValidation(CommandResultValidation.None);

                // setup cancellation token with a timeout of 1 minute
                using (var cts1 = new CancellationTokenSource(TimeSpan.FromMinutes(1)))
                {
                    BufferedCommandResult cliResult = cmd.ExecuteBufferedAsync(cts1.Token).Task.Result;

                    if (cliResult.ExitCode == 0)
                    {
                        // this will be either (on update): 
                        // Tool 'nanoclr' was successfully updated from version '1.0.205' to version '1.0.208'.
                        // or (update becoming reinstall with same version, if there is no new version):
                        // Tool 'nanoclr' was reinstalled with the latest stable version (version '1.0.208').
                        Match regexResult = Regex.Match(cliResult.StandardOutput, @"((?>version ')(?'version'\d+\.\d+\.\d+)(?>'))");

                        if (regexResult.Success)
                        {
                            logger?.Invoke(LoggingLevel.Verbose, $"Install/update successful. Running v{regexResult.Groups["version"].Value}");

                            NanoClrIsInstalled = true;
                        }
                        else
                        {
                            logger?.Invoke(LoggingLevel.Error, $"*** Failed to install/update nanoclr *** {Environment.NewLine} {cliResult.StandardOutput}");

                            NanoClrIsInstalled = false;
                        }
                    }
                    else
                    {
                        logger?.Invoke(LoggingLevel.Error,
                            $"Failed to install/update nanoclr. Exit code {cliResult.ExitCode}."
                            + Environment.NewLine
                            + Environment.NewLine
                            + "****************************************"
                            + Environment.NewLine
                            + "*** WON'T BE ABLE TO RUN UNITS TESTS ***"
                            + Environment.NewLine
                            + "****************************************");

                        NanoClrIsInstalled = false;
                    }
                }
            }

            // report outcome
            return NanoClrIsInstalled;
        }

        private void UpdateNanoCLRInstance(
            string localPath,
            string clrVersion,
            LogMessenger logger)
        {
            logger?.Invoke(LoggingLevel.Verbose, "Update nanoCLR instance");

            string arguments = "instance --update";

            if (!string.IsNullOrEmpty(clrVersion))
            {
                arguments += $" --clrversion {clrVersion}";
            }

            Command cmd = Cli.Wrap(localPath is null ? "nanoclr" : localPath)
                .WithArguments(arguments)
                .WithValidation(CommandResultValidation.None);

            // setup cancellation token with a timeout of 1 minute
            using (var cts = new CancellationTokenSource(TimeSpan.FromMinutes(1)))
            {
                BufferedCommandResult cliResult = cmd.ExecuteBufferedAsync(cts.Token).Task.Result;

                if (cliResult.ExitCode == 0)
                {
                    // this will be either (on update): 
                    // Updated to v1.8.1.102
                    // or (on same version):
                    // Already at v1.8.1.102
                    Match regexResult = Regex.Match(cliResult.StandardOutput, @"((?>v)(?'version'\d+\.\d+\.\d+\.\d+))");

                    if (regexResult.Success)
                    {
                        logger?.Invoke(LoggingLevel.Verbose,
                            $"nanoCLR instance updated to v{regexResult.Groups["version"].Value}");
                    }
                    else
                    {
                        logger?.Invoke(LoggingLevel.Error, $"*** Failed to update nanoCLR instance ***");
                    }
                }
                else
                {
                    logger?.Invoke(LoggingLevel.Detailed,
                        $"Failed to update nanoCLR instance. Exit code {cliResult.ExitCode}.");
                }
            }
        }
        private class NuGetPackage
        {
            public string[] Versions { get; set; }
        }
        #endregion
    }
}
