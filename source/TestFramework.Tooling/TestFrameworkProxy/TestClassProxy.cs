// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;
using nanoFramework.TestFramework;

namespace nanoFramework.TestFramework.Tooling.TestFrameworkProxy
{
    /// <summary>
    /// Proxy for an <see cref="ITestClass"/> implementation
    /// </summary>
    public sealed class TestClassProxy : AttributeProxy
    {
        #region Fields
        private static PropertyInfo s_instantiatePerMethod;
        private static PropertyInfo s_runClassMethodsInParallel;
        #endregion

        #region Construction
        /// <summary>
        /// Create the proxy
        /// </summary>
        /// <param name="testClass">Test class type for the nanoCLR platform</param>
        /// <param name="attribute">Matching attribute of the nanoCLR platform</param>
        /// <param name="interfaceType">Matching interface for the nanoCLR platform</param>
        internal TestClassProxy(Type testClass, Attribute attribute, Type interfaceType)
        {
            if (s_instantiatePerMethod is null)
            {
                s_instantiatePerMethod = interfaceType.GetProperty(nameof(ITestClass.InstantiatePerMethod));
                if (s_instantiatePerMethod is null
                    || s_instantiatePerMethod.PropertyType != typeof(bool))
                {
                    s_instantiatePerMethod = null;
                    throw new FrameworkMismatchException($"Mismatch in definition of ${nameof(ITestClass)}.${nameof(ITestClass.InstantiatePerMethod)}");
                }
            }

            if (s_runClassMethodsInParallel is null)
            {
                s_runClassMethodsInParallel = interfaceType.GetProperty(nameof(ITestClass.RunClassMethodsInParallel));
                if (s_runClassMethodsInParallel is null
                    || s_runClassMethodsInParallel.PropertyType != typeof(bool))
                {
                    s_runClassMethodsInParallel = null;
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
                    (bool)s_instantiatePerMethod.GetValue(attribute, null) ? TestClassInstantiation.PerMethod
                    : TestClassInstantiation.PerClass;
            }
            RunTestMethodsOneAfterTheOther = (bool)s_runClassMethodsInParallel.GetValue(attribute, null);
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
