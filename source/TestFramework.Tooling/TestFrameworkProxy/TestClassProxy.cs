// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace nanoFramework.TestFramework.Tooling.TestFrameworkProxy
{
    /// <summary>
    /// Proxy for an <see cref="ITestClass"/> implementation
    /// </summary>
    public sealed class TestClassProxy : AttributeProxy
    {
        #region Fields
        private readonly object _attribute;
        private readonly TestFrameworkImplementation _framework;
        #endregion

        #region Construction
        /// <summary>
        /// Create the proxy
        /// </summary>
        /// <param name="attribute">Matching attribute of the nanoCLR platform</param>
        /// <param name="framework">Information about the implementation of the test framework</param>
        /// <param name="interfaceType">Matching interface for the nanoCLR platform</param>
        internal TestClassProxy(object attribute, TestFrameworkImplementation framework, Type interfaceType)
        {
            _attribute = attribute;
            _framework = framework;
            _framework.AddProperty<bool>(typeof(ITestClass).FullName, interfaceType, nameof(ITestClass.CreateInstancePerTestMethod));
            _framework.AddProperty<bool>(typeof(ITestClass).FullName, interfaceType, nameof(ITestClass.SetupCleanupPerTestMethod));
        }
        #endregion

        #region Properties
        /// <summary>
        /// Indicates whether a new instance of the test class should be created for each
        /// test method of the test class, rather than a single instance for all test methods. Ignored if the
        /// test class is a static class.
        /// </summary>
        public bool CreateInstancePerTestMethod
            => _framework.GetPropertyValue<bool>(typeof(ITestClass).FullName, nameof(ITestClass.CreateInstancePerTestMethod), _attribute);

        /// <summary>
        /// Indicates whether the setup and cleanup methods of the test class should be
        /// called for each test method of the test class, rather than once for all test methods. If
        /// <see cref="CreateInstancePerTestMethod"/> is <c>true</c> for a non-static class,
        /// a value of <c>true</c> is implied for this property.
        /// </summary>
        public bool SetupCleanupPerTestMethod
            => _framework.GetPropertyValue<bool>(typeof(ITestClass).FullName, nameof(ITestClass.SetupCleanupPerTestMethod), _attribute);
        #endregion
    }
}
