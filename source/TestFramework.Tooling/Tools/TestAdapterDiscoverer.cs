// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;

namespace nanoFramework.TestFramework.Tooling.Tools
{
	/// <summary>
	/// Implementation of the test adapter's <c>ITestDiscoverer.DiscoverTests</c> method,
	/// as executed in the test host process.
	/// </summary>
	public static class TestAdapterDiscoverer
	{
		/// <summary>
		/// Find the available test cases
		/// </summary>
		/// <param name="parameters">The parameters to the <c>ITestDiscoverer.DiscoverTests</c> method.</param>
		/// <param name="sendMessage">Method to send messages with the results to the test adapter.</param>
		/// <param name="logger">Logger to pass messages to the test host/test adapter</param>
		public static void Run (TestDiscoverer_Parameters parameters, Action<InterProcessCommunicator.IMessage> sendMessage, LogMessenger logger)
		{
			var testCases = new TestCaseCollection (
				parameters.Sources,
				(a) => ProjectSourceInventory.FindProjectFilePath (a, logger),
				true,
				logger);

			void SendTestCases (IEnumerable<TestCaseSelection> testCases)
			{
				foreach (TestCaseSelection testSelection in testCases)
				{
					var testCasesData = new TestDiscoverer_DiscoveredTests ()
					{
						Source = testSelection.AssemblyFilePath,
						TestCases = new List<TestDiscoverer_DiscoveredTests.TestCase>
							(
								from tc in testSelection.TestCases
								select new TestDiscoverer_DiscoveredTests.TestCase ()
								{
									CodeFilePath = tc.testCase.TestMethodSourceCodeLocation?.SourceFilePath,
									DisplayName = tc.testCase.DisplayName,
									FullyQualifiedName = tc.testCase.FullyQualifiedName,
									LineNumber = tc.testCase.TestMethodSourceCodeLocation?.LineNumber ?? 0,
									Traits = tc.testCase.Traits.ToList ()
								}
							)
					};
					sendMessage (testCasesData);
				}
			}
			SendTestCases (testCases.TestOnVirtualDevice);
			SendTestCases (testCases.TestOnRealHardware);
		}
	}
}
