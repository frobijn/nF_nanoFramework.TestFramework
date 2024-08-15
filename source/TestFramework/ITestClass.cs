// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace nanoFramework.TestFramework
{
    /// <summary>
    /// Interface implemented by attributes that mark a class as having test methods.
    /// </summary>
#if NFTF_REFERENCED_SOURCE_FILE
    internal
#else
    public
#endif
     interface ITestClass
    {
        /// <summary>
        /// Indicates whether a new instance of the test class should be created for each
        /// test method of the test class, rather than a single instance for all test methods. Ignored if the
        /// test class is a static class.
        /// </summary>
        bool CreateInstancePerTestMethod
        {
            get;
        }

        /// <summary>
        /// Indicates whether the setup and cleanup methods of the test class should be
        /// called for each test method of the test class, rather than once for all test methods. If
        /// <see cref="CreateInstancePerTestMethod"/> is <c>true</c> for a non-static class,
        /// a value of <c>true</c> is implied for this property.
        /// </summary>
        bool SetupCleanupPerTestMethod
        {
            get;
        }
    }
}
