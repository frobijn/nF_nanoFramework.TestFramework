// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace nanoFramework.TestFramework.Tooling
{
    /// <summary>
    /// A test case group has tests cases as members that share a test execution
    /// context. At the moment, a test group corresponds to a test class.
    /// </summary>
    public sealed class TestCaseGroup
    {
        #region Construction
        /// <summary>
        /// Create the instance
        /// </summary>
        /// <param name="testGroupIndex"></param>
        internal TestCaseGroup(int testGroupIndex, string fullyQualifiedName, bool isStatic)
        {
            TestGroupIndex = testGroupIndex;
            FullyQualifiedName = fullyQualifiedName;
            IsStatic = isStatic;
        }
        #endregion

        #region Properties
        /// <summary>
        /// The fully qualified name of the test class within its assembly
        /// </summary>
        public string FullyQualifiedName
        {
            get;
        }

        /// <summary>
        /// Indicates whether the test class is a static class.
        /// </summary>
        public bool IsStatic
        {
            get;
        }

        /// <summary>
        /// Get a 0-based index of the group of test cases (in the set of all test cases in a collection of test assemblies).
        /// The index does not have to be contiguous.
        /// </summary>
        /// <remarks>
        /// In the current implementation, the index matches the index of the related test class
        /// in the list of classes in the assembly. That is not the same index when run on the
        /// nanoFramework/nanoCLR.
        /// </remarks>
        public int TestGroupIndex
        {
            get;
        }

        //// <summary>
        /// Get the name of the setup method.
        /// If there is no setup method, the value is <c>null</c>>.
        /// </summary>
        public string SetupMethodName
        {
            get;
            internal set;
        }

        // <summary>
        /// Get the index of the setup method within the methods of its class.
        /// If there is no setup method, the value is -1.
        /// </summary>
        public int SetupMethodIndex
        {
            get;
            internal set;
        } = -1;

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
        /// Get the name of the cleanup method.
        /// If there is no cleanup method, the value is <c>null</c>>.
        /// </summary>
        public string CleanupMethodName
        {
            get;
            internal set;
        }

        /// <summary>
        /// Get the index of the cleanup method within the methods of its class.
        /// If there is no setup method, the value is -1.
        /// </summary>
        public int CleanupMethodIndex
        {
            get;
            internal set;
        } = -1;

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
