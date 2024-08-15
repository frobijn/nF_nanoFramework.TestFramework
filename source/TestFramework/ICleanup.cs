// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace nanoFramework.TestFramework
{
    /// <summary>
    /// Interface implemented by attributes that mark a method that should be called to clean up after the tests.
    /// The <see cref="ITestClass"/>/<see cref="TestClassAttribute"/> determines whether
    /// a cleanup is done after each test method or once all test methods have been executed.
    /// A test class can have at most one cleanup method.
    /// </summary>
#if NFTF_REFERENCED_SOURCE_FILE
    internal
#else
    public
#endif
    interface ICleanup
    {
    }
}
