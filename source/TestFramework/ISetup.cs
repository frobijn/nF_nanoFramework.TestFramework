// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace nanoFramework.TestFramework
{
    /// <summary>
    /// Interface implemented by attributes that mark a method that should be called to setup
    /// the test context before the tests. The <see cref="ITestClass"/> determines whether a
    /// setup method is called before each test method or once before all test methods.
    /// A test class can have at most one setup method.
    /// </summary>
#if REFERENCED_IN_NFUNITMETADATA
    internal
#else
    public
#endif
    interface ISetup
    {
    }
}
