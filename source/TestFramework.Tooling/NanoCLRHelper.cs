// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using CliWrap;
using CliWrap.Buffered;
using Newtonsoft.Json;

namespace nanoFramework.TestFramework.Tooling
{
    /// <summary>
    /// Helper to ensure the correct version of the nanoCLR tool is available
    /// </summary>
    public class NanoCLRHelper : TestsRunner.IVirtualDevice
    {
        #region Fields
        private const string LockFileDirectoryName = ".nF";
        private const string LockModifications_FileName = "nanoclr_install.user";
        private const string LockUse_FileName = "nanoclr_use.user";
        private readonly string _lock_directory;
        #endregion

        #region Construction
        /// <summary>
        /// Create the helper for a specific test framework configuration
        /// </summary>
        /// <param name="configuration">Test framework configuration</param>
        /// <param name="logger">Method to pass process information to the caller.</param>
        public NanoCLRHelper(TestFrameworkConfiguration configuration, LogMessenger logger)
            : this(
                  configuration.PathToLocalNanoCLR,
                  string.IsNullOrWhiteSpace(configuration.PathToLocalNanoCLR) ? configuration.CLRVersion : null,
                  string.IsNullOrWhiteSpace(configuration.PathToLocalNanoCLR) && string.IsNullOrWhiteSpace(configuration.CLRVersion),
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
            _lock_directory = GetLockDirectory(nanoCLRFilePath);

            if (!string.IsNullOrWhiteSpace(nanoCLRFilePath))
            {
                NanoCLRFilePath = Path.GetFullPath(nanoCLRFilePath);
                NanoClrIsInstalled = File.Exists(nanoCLRFilePath);
                if (!NanoClrIsInstalled && !checkVersion)
                {
                    logger?.Invoke(LoggingLevel.Error, $"*** Failed to locate nanoCLR instance '{NanoCLRFilePath}' ***");
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
                LockNanoCLR(
                    () => UpdateNanoCLRInstance(NanoCLRFilePath, requiredNanoCLRVersion, logger),
                    true
                );
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

        #region Methods
        /// <summary>
        /// Execute an application consisting of a set of assemblies on a new instance of the Virtual Device.
        /// </summary>
        /// <param name="assemblies">The assemblies to execute. One of the assemblies must be a program.</param>
        /// <param name="localCLRInstanceFilePath">Path to a local instance of the nanoFramework CLR. Pass <c>null</c> tio use the default CLR.</param>
        /// <param name="logging">Level of logging in the virtual device.</param>
        /// <param name="processOutput">Action to process the output that is provided in chunks. Pass <c>null</c> if the output is not required.</param>
        /// <param name="logger">Logger for information about starting/executing the nanoCLR tool.</param>
        /// <param name="cancellationToken">Cancellation token that can be cancelled to abort the application.</param>
        /// <returns>Indicates whether the execution of nanoCLR was successful and did not result in an error.</returns>
        public async Task<bool> RunAssembliesAsync(
            IEnumerable<AssemblyMetadata> assemblies,
            string localCLRInstanceFilePath,
            LoggingLevel logging,
            Action<string> processOutput,
            LogMessenger logger,
            CancellationToken? cancellationToken)
        {
            bool result = false;

            await Task.Run(() =>
            {
                LockNanoCLR(
                    () =>
                    {
                        result = DoRunAssembliesAsync(assemblies, localCLRInstanceFilePath, logging, processOutput, logger, cancellationToken)
                                    .GetAwaiter().GetResult();
                    },
                    false,
                    cancellationToken: cancellationToken
                );
            });

            return result;
        }
        Task<bool> TestsRunner.IVirtualDevice.RunAssembliesAsync(
            IEnumerable<AssemblyMetadata> assemblies,
            string localCLRInstanceFilePath,
            LoggingLevel logging,
            string reportPrefix,
            Action<string> processOutput,
            LogMessenger logger,
            CancellationToken cancellationToken)
        {
            return RunAssembliesAsync(assemblies, localCLRInstanceFilePath, logging, processOutput, logger, cancellationToken);
        }
        #endregion

        #region Access mechanism
        /// <summary>
        /// Get access to a nanoclr.exe instance for modification (exclusive access) or use (shared access).
        /// At this moment it only protects against other test platform code, as other parts of the nanoFramework
        /// do not yet use this protection.
        /// </summary>
        /// <param name="action">Code to execute while having exclusive or shared access to the nanoclr.exe file.</param>
        /// <param name="forModification">Indicates whether the lock is obtained to install/update nanoclr.exe rather than use the version as is.</param>
        /// <param name="millisecondsTimeout">Maximum time in milliseconds to wait for access.</param>
        /// <param name="cancellationToken">Cancellation token that can be cancelled to stop/abort running the <paramref name="action"/>.
        /// This method does not stop/abort execution of <paramref name="action"/> after it has been started.</param>
        /// <returns>Indicates whether the <paramref name="action"/> has been executed. Returns <c>false</c> if the requested access
        /// cannot be obtained within <paramref name="millisecondsTimeout"/>, or if <paramref name="cancellationToken"/> was
        /// cancelled before <paramref name="action"/> has been started.</returns>
        public bool LockNanoCLR(Action action, bool forModification, int millisecondsTimeout = Timeout.Infinite, CancellationToken? cancellationToken = null)
        {
            return DoLockNanoCLR(_lock_directory, action, forModification, millisecondsTimeout, cancellationToken);
        }

        /// <summary>
        /// Get access to a nanoclr.exe instance for modification (exclusive access) or use (shared access).
        /// At this moment it only protects against other test platform code, as other parts of the nanoFramework
        /// do not yet use this protection.
        /// </summary>
        /// <param name="nanoCLRFilePath">Path to nanoCLR.exe. Pass <c>null</c> to use the global tool.</param>
        /// <param name="action">Code to execute while having exclusive or shared access to the nanoclr.exe file.</param>
        /// <param name="forModification">Indicates whether the lock is obtained to install/update nanoclr.exe rather than use the version as is.</param>
        /// <param name="millisecondsTimeout">Maximum time in milliseconds to wait for access.</param>
        /// <param name="cancellationToken">Cancellation token that can be cancelled to stop/abort running the <paramref name="action"/>.
        /// This method does not stop/abort execution of <paramref name="action"/> after it has been started.</param>
        /// <returns>Indicates whether the <paramref name="action"/> has been executed. Returns <c>false</c> if the requested access
        /// cannot be obtained within <paramref name="millisecondsTimeout"/>, or if <paramref name="cancellationToken"/> was
        /// cancelled before <paramref name="action"/> has been started.</returns>
        public static bool LockNanoCLR(string nanoCLRFilePath, Action action, bool forModification, int millisecondsTimeout = Timeout.Infinite, CancellationToken? cancellationToken = null)
        {
            return DoLockNanoCLR(GetLockDirectory(nanoCLRFilePath), action, forModification, millisecondsTimeout, cancellationToken);
        }

        private static bool DoLockNanoCLR(string lockDirectoryPath, Action action, bool forModification, int millisecondsTimeout = Timeout.Infinite, CancellationToken? cancellationToken = null)
        {
            Directory.CreateDirectory(lockDirectoryPath);

            FileStream lockFile = null;
            if (!WaitFor(
                    (cancellationToken) => !((lockFile = WaitForAccess(lockDirectoryPath, forModification, cancellationToken)) is null),
                    millisecondsTimeout,
                    cancellationToken
                ))
            {
                return false;
            }
            try
            {
                action?.Invoke();
            }
            finally
            {
                if (forModification)
                {
                    lockFile.Dispose();
                }
                else
                {
                    FileStream protectSharedFile = WaitForExclusiveAccess(lockDirectoryPath);
                    try
                    {
                        CloseSharedAccessFile(lockDirectoryPath, lockFile);
                    }
                    finally
                    {
                        protectSharedFile.Dispose();
                    }
                }
            }
            return true;
        }

        private static string GetLockDirectory(string nanoCLRFilePath)
        {
            if (!string.IsNullOrWhiteSpace(nanoCLRFilePath))
            {
                return Path.Combine(Path.GetDirectoryName(nanoCLRFilePath), LockFileDirectoryName);
            }
            else
            {
                return Path.Combine(Environment.GetEnvironmentVariable("USERPROFILE"), ".dotnet", "tools", LockFileDirectoryName);
            }
        }

        private static FileStream WaitForAccess(string lockDirectoryPath, bool forModification, CancellationToken? cancellationToken)
        {
            if (forModification)
            {
                while (true)
                {
                    FileStream lockFile = WaitForExclusiveAccess(lockDirectoryPath, cancellationToken);
                    if (lockFile is null)
                    {
                        return null;
                    }
                    if (File.Exists(Path.Combine(lockDirectoryPath, LockUse_FileName)))
                    {
                        CloseSharedAccessFile(lockDirectoryPath, OpenSharedAccessFile(lockDirectoryPath));
                    }
                    if (!File.Exists(Path.Combine(lockDirectoryPath, LockUse_FileName)))
                    {
                        return lockFile;
                    }
                    lockFile?.Dispose();
                    Task.Delay(100).GetAwaiter().GetResult();
                    if (cancellationToken?.IsCancellationRequested ?? false)
                    {
                        return null;
                    }
                }
            }
            else
            {
                FileStream protectSharedFile = WaitForExclusiveAccess(lockDirectoryPath, cancellationToken);
                if (protectSharedFile is null)
                {
                    return null;
                }
                try
                {
                    return OpenSharedAccessFile(lockDirectoryPath);
                }
                finally
                {
                    protectSharedFile.Dispose();
                }
            }
        }

        private static FileStream WaitForExclusiveAccess(string lockDirectoryPath, CancellationToken? cancellationToken = null)
        {
            while (true)
            {
                try
                {
                    return new FileStream(
                                    Path.Combine(lockDirectoryPath, LockModifications_FileName),
                                    FileMode.OpenOrCreate, FileAccess.ReadWrite,
                                    FileShare.None,
                                    16,
                                    FileOptions.DeleteOnClose
                                );
                }
                catch
                {
                    Task.Delay(10).GetAwaiter().GetResult();
                    if (cancellationToken?.IsCancellationRequested ?? false)
                    {
                        return null;
                    }
                }
            }
        }

        private static bool WaitFor(Func<CancellationToken?, bool> action, int millisecondsTimeout, CancellationToken? cancellationToken = null)
        {
            CancellationTokenSource cancel = null;
            if (millisecondsTimeout != Timeout.Infinite)
            {
                cancel = cancellationToken is null
                    ? new CancellationTokenSource()
                    : CancellationTokenSource.CreateLinkedTokenSource(cancellationToken.Value);
                cancel.CancelAfter(millisecondsTimeout);
            }
            while (true)
            {
                if (action(cancel?.Token ?? cancellationToken))
                {
                    return true;
                }
                Task.Delay(10).GetAwaiter().GetResult();
                if ((cancel?.Token ?? cancellationToken)?.IsCancellationRequested ?? false)
                {
                    return false;
                }
            }
        }

        private static FileStream OpenSharedAccessFile(string lockDirectoryPath)
        {
            var lockFile = new FileStream(
                                    Path.Combine(lockDirectoryPath, LockUse_FileName),
                                    FileMode.OpenOrCreate, FileAccess.ReadWrite,
                                    FileShare.ReadWrite
                                );

            var processes = new List<string>();

            using (var reader = new StreamReader(lockFile, Encoding.ASCII, false, 1024, true))
            {
                string currentId = Process.GetCurrentProcess().Id.ToString();
                bool alreadyPresent = false;
                while (!reader.EndOfStream)
                {
                    string line = reader.ReadLine();
                    if (line.StartsWith(currentId + ':'))
                    {
                        alreadyPresent = true;
                        string[] processCount = line.Split(':');
                        processes.Add($"{currentId}:{int.Parse(processCount[1]) + 1}");
                    }
                    else
                    {
                        processes.Add(line);
                    }
                }
                if (!alreadyPresent)
                {
                    processes.Add($"{currentId}:1");
                }
            }

            lockFile.Position = 0;
            using (var writer = new StreamWriter(lockFile, Encoding.ASCII, 1024, true))
            {
                foreach (string process in processes)
                {
                    writer.WriteLine(process);
                }
            }

            return lockFile;
        }

        private static void CloseSharedAccessFile(string lockDirectoryPath, FileStream lockFile)
        {
            lockFile.Position = 0;

            var processes = new List<string>();

            using (var reader = new StreamReader(lockFile, Encoding.ASCII, false, 1024, true))
            {
                string currentId = Process.GetCurrentProcess().Id.ToString();
                while (!reader.EndOfStream)
                {
                    string line = reader.ReadLine();
                    string[] processCount = line.Split(':');
                    if (processCount[0] == currentId)
                    {
                        int count = int.Parse(processCount[1]);
                        if (count > 1)
                        {
                            processes.Add($"{currentId}:{count - 1}");
                        }
                    }
                    else
                    {
                        try
                        {
                            if (!(Process.GetProcessById(int.Parse(processCount[0])) is null))
                            {
                                processes.Add(line);
                            }
                        }
                        catch
                        {
                        }
                    }
                }
            }

            if (processes.Count > 0)
            {
                lockFile.Position = 0;
                using (var writer = new StreamWriter(lockFile, Encoding.ASCII, 1024, true))
                {
                    foreach (string process in processes)
                    {
                        writer.WriteLine(process);
                    }
                }
                lockFile.SetLength(lockFile.Position);
                lockFile.Close();
            }
            else
            {
                lockFile.Close();
                File.Delete(Path.Combine(lockDirectoryPath, LockUse_FileName));
            }
        }
        #endregion

        #region Version update
        private bool InstallNanoClr(string localPath, bool checkForUpdate, LogMessenger logger)
        {
            logger?.Invoke(LoggingLevel.Detailed, "Install/update nanoclr tool");

            string versionInfo = null;
            LockNanoCLR(() => versionInfo = GetNanoClrVersionInfo(localPath), false);

            bool performInstallUpdate = versionInfo is null;
            if (!performInstallUpdate)
            {
                Match regexResult = Regex.Match(versionInfo, @"(?'version'\d+\.\d+\.\d+)", RegexOptions.RightToLeft);

                if (regexResult.Success)
                {
                    NanoClrIsInstalled = true;
                    logger?.Invoke(LoggingLevel.Detailed, $"Running nanoclr v{regexResult.Groups["version"].Value}");

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
                            logger?.Invoke(LoggingLevel.Detailed, $"No need to update. Running v{latestPackageVersion}");

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

            if (performInstallUpdate)
            {
                LockNanoCLR(() => DoInstallUpdate(localPath, logger), true);
            }

            // report outcome
            return NanoClrIsInstalled;
        }

        private string GetNanoClrVersionInfo(string localPath)
        {
            // get installed tool version (if installed)
            Command cmd = Cli.Wrap(localPath is null ? "nanoclr" : localPath)
                .WithArguments("--help")
                .WithValidation(CommandResultValidation.None);

            // setup cancellation token with a timeout of 10 seconds
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

            try
            {
                BufferedCommandResult cliResult = cmd.ExecuteBufferedAsync(cts.Token).Task.Result;

                if (cliResult.ExitCode == 0)
                {
                    return cliResult.StandardOutput;
                }
            }
            catch (Win32Exception)
            {
            }
            // nanoclr doesn't seem to be installed
            NanoClrIsInstalled = false;
            return null;
        }

        private void DoInstallUpdate(string localPath, LogMessenger logger)
        {
            Command cmd = Cli.Wrap("dotnet")
                .WithArguments($"tool update {(string.IsNullOrWhiteSpace(localPath) ? "-g" : $"--tool-path \"{localPath}\"")} nanoclr")
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
                        logger?.Invoke(LoggingLevel.Detailed, $"Install/update successful. Running v{regexResult.Groups["version"].Value}");

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


        private void UpdateNanoCLRInstance(
            string localPath,
            string clrVersion,
            LogMessenger logger)
        {
            logger?.Invoke(LoggingLevel.Detailed, "Update nanoCLR instance");

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
                        logger?.Invoke(LoggingLevel.Detailed,
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

        #region Running assemblies
        /// <summary>
        /// Execute an application consisting of a set of assemblies on a new instance of the Virtual Device.
        /// </summary>
        /// <param name="assemblies">The assemblies to execute. One of the assemblies must be a program.</param>
        /// <param name="localCLRInstanceFilePath">Path to a local instance of the nanoFramework CLR. Pass <c>null</c> tio use the default CLR.</param>
        /// <param name="logging">Level of logging in the virtual device.</param>
        /// <param name="processOutput">Action to process the output that is provided in chunks. Pass <c>null</c> if the output is not required.</param>
        /// <param name="logger">Logger for information about starting/executing the nanoCLR tool.</param>
        /// <param name="cancellationToken">Cancellation token that can be cancelled to abort the application.</param>
        /// <returns>Indicates whether the execution of nanoCLR was successful and did not result in an error.</returns>
        private async Task<bool> DoRunAssembliesAsync(
            IEnumerable<AssemblyMetadata> assemblies,
            string localCLRInstanceFilePath,
            LoggingLevel logging,
            Action<string> processOutput,
            LogMessenger logger,
            CancellationToken? cancellationToken)
        {
            // prepare launch of nanoCLR CLI
            StringBuilder arguments = new StringBuilder();

            // assemblies to load
            arguments.Append($"run --assemblies \"{string.Join("\" \"", from a in assemblies select a.NanoFrameworkAssemblyFilePath)}\"");

            // should we use a local nanoCLR instance?
            if (!string.IsNullOrEmpty(localCLRInstanceFilePath))
            {
                arguments.Append($"  --localinstance \"{localCLRInstanceFilePath}\"");
            }

            // if requested, set diagnostic output
            if (logging == LoggingLevel.Detailed)
            {
                arguments.Append(" -v diag");
            }

            logger?.Invoke(LoggingLevel.Detailed,
                $"Launching nanoCLR with these arguments: '{arguments}'");

            // launch nanoCLR
            var output = new StringBuilder();
            Command cmd = Cli.Wrap("nanoclr")
                 .WithArguments(arguments.ToString())
                 .WithValidation(CommandResultValidation.None)
                 .WithStandardOutputPipe(PipeTarget.ToDelegate((o) =>
                 {
                     output.AppendLine(o);
                     processOutput?.Invoke($"{o}\n");
                 })
            );

            try
            {
                CommandResult cliResult = await cmd.ExecuteAsync(cancellationToken ?? default);
                int exitCode = cliResult.ExitCode;

                if (exitCode != 0)
                {
                    logger?.Invoke(LoggingLevel.Error, $"nanoCLR ended with exit code '{exitCode}'.");
                    logger?.Invoke(LoggingLevel.Verbose, output.ToString());
                    return false;
                }
                return true;
            }
            catch (OperationCanceledException)
            {
                // Timeout 
                logger?.Invoke(LoggingLevel.Warning, $"nanoCLR aborted on request or after timeout.");
                return false;
            }
        }
        #endregion
    }
}
