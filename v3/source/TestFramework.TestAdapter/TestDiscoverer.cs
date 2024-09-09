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

#if DEBUG
#if LAUNCHDEBUGGER
using System.Diagnostics;
#endif
#endif

namespace nanoFramework.TestFramework.TestAdapter
{
    /// <summary>
    /// Called by Visual Studio to discover the tests that are present in assemblies.
    /// </summary>
    [DefaultExecutorUri(TestExecutor.NanoExecutor)]
    [FileExtension(".dll")] // Test assemblies with *.exe are not supported
    public sealed class TestDiscoverer : ITestDiscoverer
    {
        #region Fields
        private static readonly TestProperty s_DataRowProperty = TestProperty.Register("DataRow", "DataRow", typeof(int), TestPropertyAttributes.Hidden, typeof(TestCase));
        #endregion

        public void DiscoverTests(IEnumerable<string> sources, IDiscoveryContext discoveryContext, IMessageLogger logger, ITestCaseDiscoverySink discoverySink)
        {
#if DEBUG
#if LAUNCHDEBUGGER
            if (!Debugger.IsAttached)
            {
                Debugger.Launch();
            }
#endif
#endif
            var logMessenger = new TestAdapterLogger(logger);

            TestHost.Start(
                new TestDiscoverer_Parameters()
                {
                    AssemblyFilePaths = sources.ToList(),
                    LogLevel = (int)LoggingLevel.Verbose
                },
                (m, l, c) => ProcessTestHostMessage(m, discoverySink),
                logMessenger.LogMessage
            )
            .WaitUnitCompleted();
        }

        private static void ProcessTestHostMessage(InterProcessCommunicator.IMessage message, ITestCaseDiscoverySink discoverySink)
        {
            ProcessTestHostMessage(message, testCase => discoverySink.SendTestCase(testCase));
        }
        internal static void ProcessTestHostMessage(InterProcessCommunicator.IMessage message, Action<TestCase> addTestCase)
        {
            if (message is TestDiscoverer_DiscoveredTests tests)
            {
                var executor = new Uri(TestExecutor.NanoExecutor);
                foreach (TestDiscoverer_DiscoveredTests.TestCase testCaseData in tests.TestCases)
                {
                    var testCase = new TestCase(testCaseData.FullyQualifiedName, executor, tests.Source)
                    {
                        Id = testCaseData.ID,
                        CodeFilePath = testCaseData.CodeFilePath,
                        DisplayName = testCaseData.DisplayName,
                        LineNumber = testCaseData.LineNumber ?? 0,
                    };
                    if (testCaseData.DataRowIndex.HasValue)
                    {
                        testCase.SetPropertyValue(s_DataRowProperty, testCaseData.DataRowIndex.Value);
                    }
                    if ((testCaseData.Categories?.Count ?? 0) > 0)
                    {
                        testCase.Traits.AddRange(from category in testCaseData.Categories
                                                 select new Trait(category, ""));
                    }

                    addTestCase(testCase);
                }
            }
        }
    }
}
