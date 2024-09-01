// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Threading;
using nanoFramework.TestFramework.Tooling;
using nanoFramework.TestFramework.Tooling.Tools;

namespace nanoFramework.TestFramework.TestHost
{
    internal class Program
    {
        static void Main(string[] args)
        {
            if (args.Length > 3 && args[3] == "debug" && !Debugger.IsAttached)
            {
                Debugger.Launch();
            }
            if (args.Length > 2)
            {
                var testHost = InterProcessChild.Start(args[0], args[1], args[2], TestAdapterMessages.Types, Process);
                testHost.WaitUntilProcessingIsCompleted();
            }
        }

        private static void Process(InterProcessCommunicator.IMessage message, Action<InterProcessCommunicator.IMessage> sendMessage, LogMessenger logger, CancellationToken token)
        {
            if (message is TestDiscoverer_Parameters discoverer)
            {
                TestAdapterDiscoverer.Run(discoverer, sendMessage, logger);
            }
            else if (message is TestExecutor_TestCases_Parameters executeTestCases)
            {
                TestAdapterTestCasesExecutor.Run(executeTestCases, sendMessage, logger, token);
            }
        }
    }
}
