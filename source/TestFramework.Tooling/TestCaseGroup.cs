// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using nanoFramework.TestFramework.Tooling.TestFrameworkProxy;

namespace nanoFramework.TestFramework.Tooling
{
    /// <summary>
    /// A test case group has tests cases as members that share a test execution
    /// context. At the moment, a test group corresponds to a test class.
    /// </summary>
    public sealed class TestCaseGroup
    {
        #region Fields
        internal readonly List<SetupMethod> _setupMethods = new List<SetupMethod>();
        internal readonly List<CleanupMethod> _cleanupMethods = new List<CleanupMethod>();
        #endregion

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
            NoInstantiation = TestFramework.Tools.UnitTestLauncher.TestClassInitialisation.NoInstantiation,
            /// <summary>
            /// One instance for all test methods
            /// </summary>
            InstantiateForAllMethods = TestFramework.Tools.UnitTestLauncher.TestClassInitialisation.InstantiateForAllMethods,
            /// <summary>
            /// One instance per test method
            /// </summary>
            InstantiatePerTestMethod = TestFramework.Tools.UnitTestLauncher.TestClassInitialisation.InstantiatePerTestMethod,
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

        /// <summary>
        /// Get the proxies for custom nanoFramework attributes of the test class.
        /// </summary>
        public IReadOnlyList<AttributeProxy> CustomAttributeProxies
        {
            get; internal set;
        } = new AttributeProxy[0];

        /// <summary>
        /// Information about a setup or cleanup method
        /// </summary>
        public sealed class SetupMethod : CleanupMethod
        {
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
        }
        /// <summary>
        /// Get the setup methods and parameters
        /// </summary>
        public IReadOnlyList<SetupMethod> SetupMethods
            => _setupMethods;

        /// <summary>
        /// Information about a setup or cleanup method
        /// </summary>
        public class CleanupMethod
        {
            //// <summary>
            /// Get the name of the method.
            /// </summary>
            public string MethodName
            {
                get;
                internal set;
            }

            /// <summary>
            /// Get the location in the source code of the method.
            /// Is <c>null</c> if the method cannot be found in the source code.
            /// </summary>
            public ProjectSourceInventory.ElementDeclaration SourceCodeLocation
            {
                get;
                internal set;
            }
        }
        /// <summary>
        /// Get the cleanup methods and parameters
        /// </summary>
        public IReadOnlyList<CleanupMethod> CleanupMethods
            => _cleanupMethods;

        /// <summary>
        /// Get the testcases that are part of the group
        /// </summary>
        public IReadOnlyList<TestCase> TestCases
            => _testCases;
        internal List<TestCase> _testCases = new List<TestCase>();
        #endregion
    }
}
