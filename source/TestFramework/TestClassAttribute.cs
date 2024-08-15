// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace nanoFramework.TestFramework
{
    /// <summary>
    /// The attribute indicates that a class contains test methods.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class TestClassAttribute : Attribute, ITestClass
    {
        #region Fields
        private readonly bool _createInstancePerTestMethod;
        private readonly bool _setupCleanupPerTestMethod;
        #endregion

        #region Construction
        /// <summary>
        /// Indicate that the class is a test class.
        /// </summary>
        /// <param name="setupCleanupPerTestMethod">Indicates whether the setup and cleanup methods of the test class should be
        /// called for each test method of the test class, rather than once for all test methods. If
        /// <paramref name="createInstancePerTestMethod"/> is <c>true</c> for a non-static test class, a value of <c>true</c>
        /// is used for <paramref name="setupCleanupPerTestMethod"/> is implied.</param>
        /// <param name="createInstancePerTestMethod">Indicates whether a new instance of the test class should be created for each
        /// test test method of the test class, rather than a single instance for all test methods. Ignored if the
        /// test class is a static class.</param>
        public TestClassAttribute(bool setupCleanupPerTestMethod = false, bool createInstancePerTestMethod = false)
        {
            _createInstancePerTestMethod = createInstancePerTestMethod;
            _setupCleanupPerTestMethod = setupCleanupPerTestMethod;
        }
        #endregion

        #region ITestClass implementation
        /// <inheritdoc/>
        bool ITestClass.CreateInstancePerTestMethod
            => _createInstancePerTestMethod;

        /// <inheritdoc/>
        bool ITestClass.SetupCleanupPerTestMethod
            => _setupCleanupPerTestMethod;
        #endregion
    }
}
