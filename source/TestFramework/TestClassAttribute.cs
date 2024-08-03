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
        private readonly bool _instantiatePerMethod;
        private readonly bool _runClassMethodsInParallel;
        #endregion

        #region Construction
        /// <summary>
        /// Indicate that the class is a test class.
        /// </summary>
        /// <param name="runClassMethodsInParallel">Indicates whether the test methods of the class can be run
        /// in parallel with each other, provided the device that runs the test allows for that. Pass <c>false</c>
        /// to run the tests within the class one after the other.</param>
        /// <param name="instantiatePerMethod">Indicates whether an instance of the test class should be instantiated
        /// for each test method, rather than once for all test methods defined by this class.
        /// If the class is static, it will never be instantiated.</param>
        /// <remarks>
        /// If the test methods cannot be run in parallel with other test methods (as expressed by <see cref="DoNotRunInParallelAttribute"/>
        /// or an implementation of <see cref="IRunInParallel"/>), <paramref name="runClassMethodsInParallel"/> is ignored.
        /// </remarks>
        public TestClassAttribute(bool runClassMethodsInParallel = false, bool instantiatePerMethod = false)
        {
            _instantiatePerMethod = instantiatePerMethod;
            _runClassMethodsInParallel = runClassMethodsInParallel;
        }
        #endregion

        #region ITestClass implementation
        /// <inheritdoc/>
        bool ITestClass.InstantiatePerMethod
            => _instantiatePerMethod;

        /// <inheritdoc/>
        bool ITestClass.RunClassMethodsInParallel
            => _runClassMethodsInParallel;
        #endregion
    }
}
