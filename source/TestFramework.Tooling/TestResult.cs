// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace nanoFramework.TestFramework.Tooling
{
    public sealed class TestResult
    {
        #region Construction
        /// <summary>
        /// Create a new result
        /// </summary>
        /// <param name="index"></param>
        /// <param name="testCasePresent">Indicates whether the test case is present and will be run</param>
        internal TestResult(int index, bool testCasePresent)
        {
            Index = index;
            Outcome = testCasePresent ? TestOutcome.None : TestOutcome.NotFound;
        }
        #endregion

        #region Properties
        /// <summary>
        /// Get the index of the test case in the selection
        /// </summary>
        public int Index
        {
            get;
        }

        public enum TestOutcome
        {
            /// <summary>
            /// Test Case Does Not Have an outcome.
            /// </summary>
            None = 0,

            /// <summary>
            /// Test Case Passed
            /// </summary>
            Passed = 1,

            /// <summary>
            /// Test Case Failed
            /// </summary>
            Failed = 2,

            /// <summary>
            /// Test Case Skipped
            /// </summary>
            Skipped = 3,

            /// <summary>
            /// Test Case Not found
            /// </summary>
            NotFound = 4,
        }
        /// <summary>
        /// Get the outcome of the test
        /// </summary>
        public TestOutcome Outcome
        {
            get;
            private set;
        }
        #endregion
    }
}
