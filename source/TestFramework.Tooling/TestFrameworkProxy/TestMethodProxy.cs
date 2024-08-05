// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace nanoFramework.TestFramework.Tooling.TestFrameworkProxy
{
    /// <summary>
    /// Proxy for an <see cref="ITestMethod"/> implementation
    /// </summary>
    public sealed class TestMethodProxy : AttributeProxy
    {
        #region Fields
        private readonly Attribute _attribute;
        private readonly TestFrameworkImplementation _framework;
        #endregion

        #region Construction
        /// <summary>
        /// Create the proxy
        /// </summary>
        /// <param name="attribute">Matching attribute of the nanoCLR platform</param>
        /// <param name="framework">Information about the implementation of the test framework</param>
        /// <param name="interfaceType">Matching interface for the nanoCLR platform</param>
        internal TestMethodProxy(Attribute attribute, TestFrameworkImplementation framework, Type interfaceType)
        {
            _attribute = attribute;
            _framework = framework;

            if (_framework._property_ITestMethod_CanBeRun is null)
            {
                _framework._property_ITestMethod_CanBeRun = interfaceType.GetProperty(nameof(ITestMethod.CanBeRun));
                if (_framework._property_ITestMethod_CanBeRun is null
                    || _framework._property_ITestMethod_CanBeRun.PropertyType != typeof(bool))
                {
                    _framework._property_ITestMethod_CanBeRun = null;
                    throw new FrameworkMismatchException($"Mismatch in definition of ${nameof(ITestMethod)}.${nameof(ITestMethod.CanBeRun)}");
                }
            }
        }
        #endregion

        #region Properties
        // <summary>
        /// Indicates whether the method can be run in parallel with other test methods.
        /// If multiple attributes that implement this interface are applied to the same
        /// assembly, class or method, the result is <c>false</c> if this property is
        /// <c>false</c> for any of the attributes.
        /// </summary>
        public bool CanBeRun
            => (bool)_framework._property_ITestMethod_CanBeRun.GetValue(_attribute, null);
        #endregion
    }
}
