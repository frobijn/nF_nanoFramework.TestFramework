// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace nanoFramework.TestFramework
{
    /// <summary>
    /// Interface implemented by a class that has attributes that provide defaults for all tests in the assembly.
    /// It is supported to have multiple classes implementing the <see cref="IAssemblyAttributes"/> interface
    /// in a single assembly.
    /// </summary>
    /// <remarks>
    /// The class may be abstract; it is never instantiated. The class will not be interpreted as
    /// a test class, even if it is marked with an attribute implementing <see cref="ITestClass"/>.
    /// </remarks>
#if NFTF_REFERENCED_SOURCE_FILE
    internal
#else
    public
#endif
    interface IAssemblyAttributes
    {
    }
}
