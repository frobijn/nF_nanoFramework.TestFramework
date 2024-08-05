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
        private readonly TestFrameworkImplementation _framework;
        #endregion

        #region Construction
        /// <summary>
        /// Create the proxy
        /// </summary>
        /// <param name="testClass">Test class type for the nanoCLR platform</param>
        /// <param name="attribute">Matching attribute of the nanoCLR platform</param>
        /// <param name="framework">Information about the implementation of the test framework</param>
        /// <param name="interfaceType">Matching interface for the nanoCLR platform</param>
        internal TestClassProxy(Type testClass, Attribute attribute, TestFrameworkImplementation framework, Type interfaceType)
        {
            _framework = framework;
            if (_framework._property_ITestClass_InstantiatePerMethod is null)
            {
                _framework._property_ITestClass_InstantiatePerMethod = interfaceType.GetProperty(nameof(ITestClass.InstantiatePerMethod));
                if (_framework._property_ITestClass_InstantiatePerMethod is null
                    || _framework._property_ITestClass_InstantiatePerMethod.PropertyType != typeof(bool))
                {
                    _framework._property_ITestClass_InstantiatePerMethod = null;
                    throw new FrameworkMismatchException($"Mismatch in definition of ${nameof(ITestClass)}.${nameof(ITestClass.InstantiatePerMethod)}");
                }
            }

            if (_framework._property_ITestClass_RunClassMethodsInParallel is null)
            {
                _framework._property_ITestClass_RunClassMethodsInParallel = interfaceType.GetProperty(nameof(ITestClass.RunClassMethodsInParallel));
                if (_framework._property_ITestClass_RunClassMethodsInParallel is null
                    || _framework._property_ITestClass_RunClassMethodsInParallel.PropertyType != typeof(bool))
                {
                    _framework._property_ITestClass_RunClassMethodsInParallel = null;
                    throw new FrameworkMismatchException($"Mismatch in definition of ${nameof(ITestClass)}.${nameof(ITestClass.RunClassMethodsInParallel)}");
                }
            }

            if (testClass.IsAbstract)
            {
                // Must be a static class
                Instantiation = TestClassInstantiation.Never;
            }
            else
            {
                Instantiation =
                    (bool)_framework._property_ITestClass_InstantiatePerMethod.GetValue(attribute, null) ? TestClassInstantiation.PerMethod
                    : TestClassInstantiation.PerClass;
            }
            RunTestMethodsOneAfterTheOther = !(bool)_framework._property_ITestClass_RunClassMethodsInParallel.GetValue(attribute, null);
        }
        #endregion

        #region Properties
        /// <summary>
        /// Type of instantiation of a test class
        /// </summary>
        public enum TestClassInstantiation
        {
            Never,
            PerClass,
            PerMethod
        }

        /// <summary>
        /// Indicates how the class has to be instantiated
        /// </summary>
        public TestClassInstantiation Instantiation
        {
            get;
        }

        /// <summary>
        /// Indicates how the test methods in the class should be run
        /// </summary>
        public bool RunTestMethodsOneAfterTheOther
        {
            get;
        }
        #endregion
    }
}
