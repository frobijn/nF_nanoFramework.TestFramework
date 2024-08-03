// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace nanoFramework.TestFramework
{
    /// <summary>
    /// Interface implemented by attributes that mark a class as having test methods.
    /// </summary>
#if REFERENCED_IN_NFUNITMETADATA
    internal
#else
    public
#endif
     interface ITestClass
    {
        /// <summary>
        /// Indicates whether an instance of the test class should be instantiated
        /// for each test method, rather than once for all test methods defined by this class.
        /// If the class is static, it will never be instantiated.
        /// </summary>
        bool InstantiatePerMethod
        {
            get;
        }

        /// <summary>
        /// Indicates whether the methods in the test class can be run in parallel with each other.
        /// If the property is <c>false</c>, the methods in the test class must be run one after the
        /// other. That does not rule out whether the test methods can be run in parallel with
        /// test methods from other test classes - that is governed by <see cref="IRunInParallel"/>.
        /// </summary>
        bool RunClassMethodsInParallel
        {
            get;
        }
    }
}
