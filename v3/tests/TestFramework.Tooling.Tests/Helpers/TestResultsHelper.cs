// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using nanoFramework.TestFramework.Tooling;

using TestResult = nanoFramework.TestFramework.Tooling.TestResult;

namespace TestFramework.Tooling.Tests.Helpers
{
    public static class TestResultsHelper
    {
        /// <summary>
        /// Assert the test results
        /// </summary>
        /// <param name="actual">List of test results</param>
        /// <param name="selection">Test case selection the results are for</param>
        /// <param name="expected">Expected results</param>
        /// <param name="withMessagesAndDuration">Indicates whether to include messages and duration, the two parts
        /// of the test result that may be different from test run to test run.</param>
        public static void AssertResults(this List<TestResult> actual, TestCaseSelection selection, string expected, bool withMessagesAndDuration = true)
        {
            var actualAsString = new StringBuilder();
            foreach (TestResult tr in from r in actual
                                      orderby r.Index
                                      select r)
            {
                TestCase testCase = (from tc in selection.TestCases
                                     where tc.selectionIndex == tr.Index
                                     select tc.testCase).First();
                actualAsString.AppendLine("----------------------------------------");
                actualAsString.AppendLine($"Test        : {testCase.FullyQualifiedName}");
                actualAsString.AppendLine($"DisplayName : '{tr.DisplayName}'");
                if (withMessagesAndDuration)
                {
                    actualAsString.AppendLine($"Duration    : {tr.Duration.Ticks} ticks");
                }
                actualAsString.AppendLine($"Outcome     : {tr.Outcome}");
                actualAsString.AppendLine($"ErrorMessage: '{tr.ErrorMessage}'");
                if (withMessagesAndDuration && tr.Messages.Count > 0)
                {
                    actualAsString.AppendLine($"Messages    :");
                    foreach (string msg in tr.Messages)
                    {
                        actualAsString.AppendLine(msg.TrimEnd());
                    }
                }
                actualAsString.AppendLine("----------------------------------------");
                actualAsString.AppendLine();
                actualAsString.AppendLine();
            }
            var expectedCleaned = new StringBuilder();
            foreach (string msg in expected.Trim().Split('\n'))
            {
                expectedCleaned.Append(msg.TrimEnd());
                expectedCleaned.Append('\n');
            }

            Assert.AreEqual(
                expectedCleaned.ToString(),
                actualAsString.ToString().Trim().Replace("\r\n", "\n") + '\n'
            );
        }
    }
}
