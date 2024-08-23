// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
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
        internal TestCaseGroup(string fullyQualifiedName, InstantiationType instantiation, bool setupCleanupPerTestMethod)
        {
            FullyQualifiedName = fullyQualifiedName;
            Instantiation = instantiation;
            SetupCleanupPerTestMethod = setupCleanupPerTestMethod
                || Instantiation == InstantiationType.InstantiatePerTestMethod;
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
        /// Indicates how the test class should be instantiated
        /// </summary>
        public enum InstantiationType
        {
            /// <summary>
            /// Not - it is a static test class
            /// </summary>
            NoInstantiation = Tools.UnitTestLauncher.TestClassInitialisation.NoInstantiation,
            /// <summary>
            /// One instance for all test methods
            /// </summary>
            InstantiateForAllMethods = Tools.UnitTestLauncher.TestClassInitialisation.InstantiateForAllMethods,
            /// <summary>
            /// One instance per test method
            /// </summary>
            InstantiatePerTestMethod = Tools.UnitTestLauncher.TestClassInitialisation.InstantiatePerTestMethod,
        }
        /// <summary>
        /// Indicates whether the test class is a static class.
        /// </summary>
        public InstantiationType Instantiation
        {
            get;
        }

        /// <summary>
        /// Indicates whether the setup and cleanup methods of the test class should be
        /// called for each test method of the test class, rather than once for all test methods.
        /// </summary>
        public bool SetupCleanupPerTestMethod
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

        /// <summary>
        /// Get the keys that identify what part of the deployment configuration
        /// should be passed to the setup method. Each key should have a corresponding
        /// argument of the setup method that is of type <c>byte[]</c>, <c>int</c>, <c>long</c> or <c>string</c>,
        /// as indicated for the key.
        /// </summary>
        public IReadOnlyList<(string key, Type valueType)> RequiredConfigurationKeys
        {
            get;
            internal set;
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
        /// Get the name of the cleanup method.
        /// If there is no cleanup method, the value is <c>null</c>>.
        /// </summary>
        public string CleanupMethodName
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
