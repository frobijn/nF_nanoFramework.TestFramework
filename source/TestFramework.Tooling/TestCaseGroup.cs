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
        /// <param name="runInParallel"></param>
        /// <param name="runOneAfterTheOther"></param>
        internal TestCaseGroup(int testGroupIndex, bool runInParallel, bool runOneAfterTheOther)
        {
            TestGroupIndex = testGroupIndex;
            RunInParallel = runInParallel;
            RunOneAfterTheOther = runOneAfterTheOther;
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
        /// Indicates whether the setup and cleanup methods are executed for each of the tests in the group,
        /// rather than the setup before the execution of the first test and a cleanup after the last test.
        /// If there are no setup and cleanup procedures, the value is <c>false</c>.
        /// </summary>
        public bool SetupCleanupForEachTest
        {
            get;
            internal set;
        }

        /// <summary>
        /// Indicates whether the tests from this group can be run in parallel with tests from other groups,
        /// provided the device the tests are running on can provide that feature.
        /// </summary>
        public bool RunInParallel
        {
            get;
        }

        /// <summary>
        /// Indicates whether the tests from this group should be run one after the other. If the value is <c>false</c>
        /// the tests in the group can be run in parallel with each other, provided the device the tests are running
        /// on can provide that feature.
        /// </summary>
        public bool RunOneAfterTheOther
        {
            get;
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
