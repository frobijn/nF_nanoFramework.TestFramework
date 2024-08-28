// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using nanoFramework.TestFramework.Tooling;
using nanoFramework.TestFramework.Tooling.Tools;

namespace nanoFramework.TestFramework.TestAdapter
{
    /// <summary>
    /// Called by Visual Studio to discover the tests that are present in assemblies.
    /// </summary>
    [DefaultExecutorUri(TestExecutor.NanoExecutor)]
    [FileExtension(".dll")] // Test assemblies with *.exe are not supported
    public sealed class TestDiscoverer : ITestDiscoverer
    {
        public void DiscoverTests(IEnumerable<string> sources, IDiscoveryContext discoveryContext, IMessageLogger logger, ITestCaseDiscoverySink discoverySink)
        {
            var logMessenger = new TestAdapterLogger(logger);

            TestHost.Start(
                new TestDiscoverer_Parameters()
                {
                    Sources = sources.ToList(),
                    LogLevel = (int)LoggingLevel.Verbose
                },
                (m, l, c) => ProcessTestHostMessage(m, discoverySink),
                logMessenger.LogMessage
            )
            .WaitUnitCompleted();
        }

        private static void ProcessTestHostMessage(Communicator.IMessage message, ITestCaseDiscoverySink discoverySink)
        {
            if (message is TestDiscoverer_DiscoveredTests tests)
            {
                var executor = new Uri(TestExecutor.NanoExecutor);
                foreach (TestDiscoverer_DiscoveredTests.TestCase testCaseData in tests.TestCases)
                {
                    var testCase = new TestCase(testCaseData.FullyQualifiedName, executor, tests.Source)
                    {
                        CodeFilePath = testCaseData.CodeFilePath,
                        DisplayName = testCaseData.DisplayName,
                        LineNumber = testCaseData.LineNumber ?? 0,
                    };

                    if (!(testCase.Traits is null))
                    {
                        foreach (Trait trait in testCase.Traits)
                        {
                            testCase.Traits.Add(trait);
                        }
                    }

                    discoverySink.SendTestCase(testCase);
                }
            }
        }
    }
}
