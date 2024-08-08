// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace nanoFramework.TestFramework.Tooling
{
    public sealed class TestCaseGroup
    {
        #region Construction
        /// <summary>
        /// Create the instance
        /// </summary>
        /// <param name="testGroupIndex"></param>
        internal TestCaseGroup(int testGroupIndex)
        {
            TestGroupIndex = testGroupIndex;
        }
        #endregion

        #region Properties
        /// <summary>
        /// Get the 1-based index of the group of test cases (in the set of all test cases in a collection of test assemblies).
        /// All tests in a group share setup/cleanup code.
        /// The index matches the index that as determined by the test runner when it enumerates the tests
        /// in the assemblies (in the same order).
        /// </summary>
        public int TestGroupIndex
        {
            get;
        }

        /// <summary>
        /// Get the location in the source code of the setup method that is run before the test(s).
        /// Is <c>null</c> if there is no setup method or if the method cannot be found in the source code.
        /// </summary>
        public ProjectSourceInventory.ElementDeclaration SetupSourceCodeLocation
        {
            get;
            internal set;
        }

        /// <summary>
        /// Get the location in the source code of the cleanup method that is run after the test(s).
        /// Is <c>null</c> if there is no setup method or if the method cannot be found in the source code.
        /// </summary>
        public ProjectSourceInventory.ElementDeclaration CleanupSourceCodeLocation
        {
            get;
            internal set;
        }

        /// <summary>
        /// Get the testcases that are part of the group
        /// </summary>
        public IReadOnlyList<TestCase> TestCases
            => _testCases;
        internal List<TestCase> _testCases = new List<TestCase>();
        #endregion
    }
}
